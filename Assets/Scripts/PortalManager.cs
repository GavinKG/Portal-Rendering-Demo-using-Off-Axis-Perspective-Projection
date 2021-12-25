using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class PortalConnectionSetup
{

    public Portal sourcePortal;
    public Portal targetPortal;
}


/// <summary>
/// The singleton manager for managing portal, connection info, cameras, and directly talks to the renderer.
/// Similar to UE4's Game Instance or Game Mode.
/// All potential exceptions are not handled.
/// </summary>
public class PortalManager : MonoBehaviour
{

    // ----------- enums:
    public enum PortalRenderMethod
    {
        RenderTexture,
        Stencil
    }

    public enum PortalRenderTextureSize
    {
        _128 = 128,
        _256 = 256,
        _512 = 512,
        _1024 = 1024,
        _2048 = 2048,
        _4096 = 4096
    }

    // ----------- classes:
    class PortalConnectionInfo
    {

        // member's lifecycle should be controlled by outer code.

        public Portal sourcePortal;
        public Portal targetPortal;
        public PortalRenderMethod renderMethod;
        public GameObject cameraObject;
        public RenderTexture renderTexture; // will only be used if renderMethod == .RenderTexture

    }

    // ----------- editor, should not change:

    [Header("References")]

    [Tooltip("Portal prefab used as a reference to do calculations. Will never be instantiated. Should not be changed during gameplay.")]
    public Portal referencePortalPrefab;

    [Tooltip("Can contain arbitrary amount of portals. Should not be changed during gameplay.")]
    public List<Portal> portalPrefabs;

    [Tooltip("Defines how portals connect to each other. Should only contains prefabs. Should not be changed during gameplay.")]
    public List<PortalConnectionSetup> portalConnections;

    [Header("Settings")]

    [Tooltip("Portal render method. Should not be changed during gameplay.")]
    public PortalRenderMethod renderMethod = PortalRenderMethod.RenderTexture;

    [Tooltip("Used only when renderMethod == PortalRenderMethod.RenderTexture. This represents the longer side. Should not be changed during gameplay.")]
    public PortalRenderTextureSize renderTextureSize = PortalRenderTextureSize._1024;

    [Tooltip("Should view through portal matches the rotation of target portal.\nIf not, only position of target portal is used, you will always see a straight-up view from portal.")]
    public bool matchRotation = false;

    [Tooltip("Draw Debug info.")]
    public bool debug;

    // ----------- public singleton:

    public static PortalManager Instance { get; private set; }

    // ----------- public

    public PortalGun ActivePortalGun
    {
        private get
        {
            return activePortalGun;
        }
        set
        {
            if (value != null)
            {
                value.OnPortalCreated.AddListener(PortalCreated);
                value.OnPortalBeforeRemoval.AddListener(PortalBeforeRemoval);
                value.OnPortalRelocated.AddListener(PortalRelocated);
                print("[PortalManager] Portal gun registered.");
            }
            else
            {
                if (activePortalGun != null)
                {
                    activePortalGun.OnPortalCreated.RemoveListener(PortalCreated);
                    activePortalGun.OnPortalBeforeRemoval.RemoveListener(PortalBeforeRemoval);
                    activePortalGun.OnPortalRelocated.RemoveListener(PortalRelocated);
                    print("[PortalManager] Portal gun removed.");
                }
            }
            activePortalGun = value;
        }
    }

    public Vector2Int RenderTextureSize
    {
        get
        {
            return rtSize;
        }
    }

    public Camera ViewCamera { get { return Camera.main; } }


    // ----------- private:

    // There is no limitations on how many portal can be opened of one kind. This depends on the Portal Gun.
    HashSet<Portal> activePortals = new HashSet<Portal>();

    // Active portal gun.
    PortalGun activePortalGun = null;

    // Existing connection info, used at runtime.
    List<PortalConnectionInfo> connectionInfo;

    // Render texture's size. Only useful when renderMethod == PortalRenderMethod.RenderTexture.
    Vector2Int rtSize;

    List<Tuple<Vector3, Vector3>> debugLineList = new List<Tuple<Vector3, Vector3>>();
    List<Vector3> debugPointList = new List<Vector3>();



    private void Start()
    {
        connectionInfo = new List<PortalConnectionInfo>();

        referencePortalPrefab.GetComponent<Portal>().FillDetectionPoints();

        // Calculate render texture's size.
        rtSize = Vector2Int.zero;
        Vector2 sizeXY = referencePortalPrefab.sizeXY;
        if (sizeXY.x >= sizeXY.y)
        {
            float ratio = sizeXY.y / sizeXY.x;
            rtSize.x = (int)renderTextureSize;
            rtSize.y = (int)(rtSize.x * ratio);
        }
        else
        {
            float ratio = sizeXY.x / sizeXY.y;
            rtSize.y = (int)renderTextureSize;
            rtSize.x = (int)(rtSize.y * ratio);
        }
    }

    private void Awake()
    {
        // singleton
        if (Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            DontDestroyOnLoad(this);
            Instance = this;
        }
    }

    private void Update()
    {
        foreach (PortalConnectionInfo connectionInfo in connectionInfo)
        {
            UpdateConnectionCamera(connectionInfo);
        }
    }

    private void OnDrawGizmos()
    {
        if (debug)
        {

            foreach (Tuple<Vector3, Vector3> line in debugLineList)
            {
                Gizmos.DrawRay(line.Item1, line.Item2);
            }

            foreach (Vector3 point in debugPointList)
            {
                Gizmos.DrawSphere(point, 0.2f);
            }

            debugLineList.Clear();
            debugPointList.Clear();
        }
    }

    void PortalCreated(Portal portal)
    {
        activePortals.Add(portal);

        // Check connections.
        Portal portalPrefab = portal.PortalPrefab;

        foreach (PortalConnectionSetup setup in portalConnections)
        {
            if (setup.sourcePortal == portalPrefab)
            {
                foreach (Portal targetPortal in activePortals)
                {
                    if (targetPortal.PortalPrefab == setup.targetPortal)
                    {
                        InitPortalConnection(portal, targetPortal);
                    }
                }
            }
            else if (setup.targetPortal == portalPrefab)
            {
                foreach (Portal sourcePortal in activePortals)
                {
                    if (sourcePortal.PortalPrefab == setup.sourcePortal)
                    {
                        InitPortalConnection(sourcePortal, portal);
                    }
                }
            }
        }
    }


    void PortalRelocated(Portal portal)
    {

    }

    void PortalBeforeRemoval(Portal portal)
    {
        activePortals.Remove(portal);
    }

    PortalConnectionInfo InitPortalConnection(Portal sourcePortal, Portal targetPortal)
    {
        if (sourcePortal == targetPortal)
        {
            throw new Exception("Cannot connect to source portal itself.");
        }

        PortalConnectionInfo info = new PortalConnectionInfo
        {
            sourcePortal = sourcePortal,
            targetPortal = targetPortal
        };

        info.cameraObject = new GameObject("Camera: " + sourcePortal.name + "->" + targetPortal.name);
        Camera camera = info.cameraObject.AddComponent<Camera>();

        info.renderMethod = renderMethod;

        if (renderMethod == PortalRenderMethod.RenderTexture)
        {
            info.renderTexture = new RenderTexture(rtSize.x, rtSize.y, 32);
            camera.targetTexture = info.renderTexture;
        }
        else if (renderMethod == PortalRenderMethod.Stencil)
        {
            throw new NotImplementedException();
        }

        sourcePortal.ConnectTo(targetPortal);

        if (renderMethod == PortalRenderMethod.RenderTexture)
        {
            sourcePortal.SetPortalTexture(info.renderTexture);
        }

        connectionInfo.Add(info);

        print("Connection created: " + sourcePortal.name + " -> " + targetPortal.name);

        return info;
    }

    void UpdateConnectionCamera(PortalConnectionInfo connectionInfo)
    {

        if (connectionInfo.renderMethod == PortalRenderMethod.RenderTexture)
        {
            UpdateConnectionCamera_UsingRenderTexture(connectionInfo);
        }
        else
        {
            // todo
        }

    }

    void UpdateConnectionCamera_UsingRenderTexture(PortalConnectionInfo connectionInfo)
    {

        // get corner view directions on source portal's world space (camera -> corner)
        Vector3 camPos = ViewCamera.transform.position;
        Vector3 upperLeftCornerViewDirWS = connectionInfo.sourcePortal.WorldUpperLeftCorner - camPos;
        Vector3 upperRightCornerViewDirWS = connectionInfo.sourcePortal.WorldUpperRightCorner - camPos;
        Vector3 lowerLeftCornerViewDirWS = connectionInfo.sourcePortal.WorldLowerLeftCorner - camPos;
        Vector3 lowerRightCornerViewDirWS = connectionInfo.sourcePortal.WorldLowerRightCorner - camPos;

        // ...and convert to source portal's local space, therefore becoming target portal's out ray direction
        Vector3 upperLeftCornerViewDirLS = connectionInfo.sourcePortal.transform.InverseTransformDirection(upperLeftCornerViewDirWS);
        Vector3 upperRightCornerViewDirLS = connectionInfo.sourcePortal.transform.InverseTransformDirection(upperRightCornerViewDirWS);
        Vector3 lowerLeftCornerViewDirLS = connectionInfo.sourcePortal.transform.InverseTransformDirection(lowerLeftCornerViewDirWS);
        Vector3 lowerRightCornerViewDirLS = connectionInfo.sourcePortal.transform.InverseTransformDirection(lowerRightCornerViewDirWS);

        // ...and convert to target portal's world space
        Vector3 targetUpperLeftCornerViewDirWS = connectionInfo.targetPortal.transform.TransformDirection(upperLeftCornerViewDirLS);
        Vector3 targetUpperRightCornerViewDirWS = connectionInfo.targetPortal.transform.TransformDirection(upperRightCornerViewDirLS);
        Vector3 targetLowerLeftCornerViewDirWS = connectionInfo.targetPortal.transform.TransformDirection(lowerLeftCornerViewDirLS);
        Vector3 targetLowerRightCornerViewDirWS = connectionInfo.targetPortal.transform.TransformDirection(lowerRightCornerViewDirLS);

        // Get 4 corners' positions on other portal's world space.
        Vector3 targetUpperLeftCornerWS = connectionInfo.targetPortal.transform.TransformPoint(connectionInfo.sourcePortal.LocalUpperLeftCorner);
        Vector3 targetUpperRightCornerWS = connectionInfo.targetPortal.transform.TransformPoint(connectionInfo.sourcePortal.LocalUpperRightCorner);
        Vector3 targetLowerLeftCornerWS = connectionInfo.targetPortal.transform.TransformPoint(connectionInfo.sourcePortal.LocalLowerLeftCorner);
        Vector3 targetLowerRightCornerWS = connectionInfo.targetPortal.transform.TransformPoint(connectionInfo.sourcePortal.LocalLowerRightCorner);

        if (debug)
        {
            debugLineList.Add(new Tuple<Vector3, Vector3>(targetUpperLeftCornerWS, targetUpperLeftCornerViewDirWS));
            debugLineList.Add(new Tuple<Vector3, Vector3>(targetUpperRightCornerWS, targetUpperRightCornerViewDirWS));
            debugLineList.Add(new Tuple<Vector3, Vector3>(targetLowerLeftCornerWS, targetLowerLeftCornerViewDirWS));
            debugLineList.Add(new Tuple<Vector3, Vector3>(targetLowerRightCornerWS, targetLowerRightCornerViewDirWS));
        }


        // Find target camera's rotation (same as target portal)
        Quaternion targetCamRot = connectionInfo.targetPortal.transform.rotation;

        // Find target camera's position
        Vector3 targetCamPos;
        if (!ViewCamera.orthographic)
        {
            // Perspective!
            bool success = Utils.LineLineIntersection(targetUpperLeftCornerWS, -targetUpperLeftCornerViewDirWS, targetUpperRightCornerWS, -targetUpperRightCornerViewDirWS, out targetCamPos);
            if (!success)
            {
                throw new Exception("Something weird happened. Maybe the universe is changing foundemental rules.");
            }

        }
        else
        {
            throw new NotImplementedException();
        }

        // Figure out perspective projection's center point on near plane
        Plane nearPlane = new Plane(connectionInfo.targetPortal.FacingDirection, connectionInfo.targetPortal.transform.position); // the target portal's sitting plane
        Vector3 centerPointWS = nearPlane.ClosestPointOnPlane(targetCamPos);
        Vector3 centerPointLS = connectionInfo.targetPortal.transform.InverseTransformPoint(centerPointWS); // x, y should be the offset for the frustum

        if (debug)
        {
            debugPointList.Add(centerPointWS);
        }

        // Get projection matrix
        float far = ViewCamera.farClipPlane; // follows the main camera.
        float near = Vector3.Distance(centerPointWS, targetCamPos);
        float left = connectionInfo.sourcePortal.LocalUpperLeftCorner.x + centerPointLS.x;
        float right = connectionInfo.sourcePortal.LocalUpperRightCorner.x + centerPointLS.x;
        float top = connectionInfo.sourcePortal.LocalUpperLeftCorner.y + centerPointLS.y;
        float bottom = connectionInfo.sourcePortal.LocalLowerLeftCorner.y + centerPointLS.y;
        Matrix4x4 proj = Matrix4x4.Frustum(left, right, bottom, top, near, far); // off-axis perspective projection.

        // Apply to the camera object
        connectionInfo.cameraObject.transform.position = targetCamPos;
        connectionInfo.cameraObject.transform.rotation = targetCamRot;
        Camera portalCamera = connectionInfo.cameraObject.GetComponent<Camera>();
        portalCamera.projectionMatrix = proj;
        // portalCamera.cullingMatrix = proj;



    }
    void UpdateConnectionCamera_UsingStencil(PortalConnectionInfo connectionInfo)
    {
        throw new NotImplementedException();
    }


}

