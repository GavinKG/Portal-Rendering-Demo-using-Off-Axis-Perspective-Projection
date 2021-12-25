using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Portal. Purely a passive, display-only object.
/// Portal's transform will be handled by Portal Gun.
/// </summary>
public class Portal : MonoBehaviour {
    // ----------- editor:

    [Tooltip("Portal Size in X-Y plane.")]
    public Vector2 sizeXY = new Vector2(1f, 1f);

    [Tooltip("This GameObject is used to present the other side of the portal. Its material will be used when using RenderTexture method to draw portal.")]
    public GameObject portalMeshObject;

    [Tooltip("Hide the portal mesh object when not connected to target portal. Check this if you only use portal mesh for displaying portal image.")]
    public bool hidePortalMeshWhenDisconnected = false;

    public bool drawDebugInfo = false;

    // ----------- public:

    // ref to portal prefab object's Portal component. Null if already a prefab.
    public Portal PortalPrefab { get; set; }

    // portal's mounting point to the wall, used for detect if portal is close to the wall.
    public List<Vector3> DetectionPoints { get; private set; }

    public Vector3 LocalUpperLeftCorner { get { return new Vector3(-sizeXY.x / 2, sizeXY.y / 2); } }
    public Vector3 LocalUpperRightCorner { get { return new Vector3(sizeXY.x / 2, sizeXY.y / 2); } }
    public Vector3 LocalLowerLeftCorner { get { return new Vector3(-sizeXY.x / 2, -sizeXY.y / 2); } }
    public Vector3 LocalLowerRightCorner { get { return new Vector3(sizeXY.x / 2, -sizeXY.y / 2); } }

    public Vector3 WorldUpperLeftCorner { get { return transform.TransformPoint(LocalUpperLeftCorner); } }
    public Vector3 WorldUpperRightCorner { get { return transform.TransformPoint(LocalUpperRightCorner); } }
    public Vector3 WorldLowerLeftCorner { get { return transform.TransformPoint(LocalLowerLeftCorner); } }
    public Vector3 WorldLowerRightCorner { get { return transform.TransformPoint(LocalLowerRightCorner); } }

    public Vector3 FacingDirection { get { return transform.TransformDirection(Vector3.forward); } } // facing +Z

    // ----------- private:

    Portal connectedPortal;

    Texture portalMeshObjectOriginalTexture;





    private void OnDrawGizmos() {
        if (!drawDebugInfo) {
            return;
        }

        FillDetectionPoints();

        Gizmos.color = Color.white;
        foreach (Vector3 p in DetectionPoints) {
            Gizmos.DrawSphere(transform.TransformPoint(p), 0.05f);
        }

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.TransformDirection(Vector3.forward));
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, transform.TransformDirection(Vector3.up));
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, transform.TransformDirection(Vector3.right));
    }

    private void Start() {

        if (portalMeshObject != null && hidePortalMeshWhenDisconnected && connectedPortal == null) {
            portalMeshObject.SetActive(false);
        }

        // Fill detection points.
        FillDetectionPoints();
    }

    #region Presentation layer (View)

    public void DisplayPortalCreated() {

    }

    public void DisplayPortalBeginRemoval() {

    }

    public void DisplayPortalRelocated(Vector3 prevPosition, Quaternion prevRotation) {

    }

    public void DisplayPortalConnected() {

    }

    public void DisplayPortalChangeConnection() {

    }

    public void DisplayPortalDisconnected() {

    }

    #endregion

    // Set the "root" object of this portal instance.
    public void SetPrefabPortal(Portal portal) {
        if (PortalUtils.IsPortalPrefabObject(gameObject)) {
            throw new System.Exception("Cannot set portal prefab object on a prefab!");
        }
        if (!PortalUtils.IsPortalPrefabObject(portal.gameObject)) {
            throw new System.ArgumentException("Target game object is not a prefab!");
        }
        PortalPrefab = portal;
    }

    public bool IsSameKindAs(GameObject otherPortalObject) {

        if (PortalPrefab == null) {
            // this gameobject might be a prefab, or someone forgot to set it up so we cannot get the answer...
            return false;
        }

        if (PortalUtils.IsPortalInstanceObject(otherPortalObject)) {
            // Portal prefab objects cannot be of the same kind as any other portal objects exists.
            // Since "portal prefab object" are not "portal instance object", and there should be only one prefab object exists.
            return false;
        }

        return PortalPrefab == otherPortalObject.GetComponent<Portal>().PortalPrefab;

    }

    public void ConnectTo(Portal portal) {
        if (portal == null) {
            throw new System.ArgumentNullException("portal", "You may want to call Disconnect() to set connected portal to null.");
        }

        if (connectedPortal == null) {
            // ...
            DisplayPortalConnected();
        } else {
            // ...
            DisplayPortalChangeConnection();
        }

        connectedPortal = portal;

        if (hidePortalMeshWhenDisconnected && portalMeshObject != null) {
            portalMeshObject.SetActive(true);
        }
    }

    public void Disconnect() {
        if (connectedPortal == null) {
            return;
        }
        // ...
        DisplayPortalDisconnected();

        connectedPortal = null;

        if (hidePortalMeshWhenDisconnected && portalMeshObject != null) {
            portalMeshObject.SetActive(false);
        }
    }

    public void SetPortalTexture(RenderTexture rt) {
        if (PortalManager.Instance.renderMethod != PortalManager.PortalRenderMethod.RenderTexture) {
            print("This function is not intended to be called when using other ways to draw portal camera.");
        }
        if (portalMeshObject == null) {
            throw new System.Exception("portalMeshObject not set.");
        }
        MeshRenderer renderer = portalMeshObject.GetComponent<MeshRenderer>();
        if (renderer == null) {
            throw new System.Exception("portalMeshObject has no MeshRenderer component.");
        }
        // this Mesh Renderer should only have one material, with a texture sampler named "_MainTex".
        // renderer.material.SetTexture(Shader.PropertyToID("Texture"), rt);
        renderer.material.mainTexture = rt;
    }

    public void RestorePortalTexture() {
        if (portalMeshObject == null) {
            throw new System.Exception("portalMeshObject not set.");
        }
        MeshRenderer renderer = portalMeshObject.GetComponent<MeshRenderer>();
        if (renderer == null) {
            throw new System.Exception("portalMeshObject has no MeshRenderer component.");
        }
        int textureParamId = Shader.PropertyToID("Texture");
        renderer.material.SetTexture(textureParamId, renderer.sharedMaterial.GetTexture(textureParamId));
    }

    public void FillDetectionPoints() {

        DetectionPoints = new List<Vector3>();

        // 4 corners of the portal.
        Vector2 extent = sizeXY / 2;
        DetectionPoints.Add(new Vector3(extent.x, extent.y, 0));
        DetectionPoints.Add(new Vector3(extent.x, -extent.y, 0));
        DetectionPoints.Add(new Vector3(-extent.x, extent.y, 0));
        DetectionPoints.Add(new Vector3(-extent.x, -extent.y, 0));
    }




}
