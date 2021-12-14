using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class PortalConnectionSetup {

    public Portal sourcePortal;
    public Portal targetPortal;
    public bool oneWayConnection = false;
}


/// <summary>
/// The singleton manager for managing portal, connection info, cameras, and directly talks to the renderer.
/// Similar to UE4's Game Instance or Game Mode.
/// All potential exceptions are not handled.
/// </summary>
public class PortalManager : MonoBehaviour {

    // ----------- enums:
    public enum PortalRenderMethod {
        RenderTexture,
        Stencil
    }

    public enum PortalRenderTextureSize {
        _128 = 128,
        _256 = 256,
        _512 = 512,
        _1024 = 1024,
        _2048 = 2048,
        _4096 = 4096
    }

    // ----------- classes:
    class PortalConnectionInfo {

        // member's lifecycle should be controlled by outer code.

        public Portal sourcePortal;
        public Portal targetPortal;
        public GameObject cameraObject;
        public RenderTexture renderTexture; // will only be used if renderMethod == .RenderTexture

    }

    // ----------- editor, should not change:

    [Tooltip("Portal prefab used as a reference to do calculations. Will never be instantiated. Should not be changed during gameplay.")]
    public Portal referencePortalPrefab;

    [Tooltip("Can contain arbitrary amount of portals. Should not be changed during gameplay.")]
    public List<Portal> portalPrefabs;

    [Tooltip("Defines how portals connect to each other. Should only contains prefabs. Should not be changed during gameplay.")]
    public List<PortalConnectionSetup> portalConnections;

    [Tooltip("Portal render method. Should not be changed during gameplay.")]
    public PortalRenderMethod renderMethod = PortalRenderMethod.RenderTexture;

    [Tooltip("Used only when renderMethod == PortalRenderMethod.RenderTexture. This represents the longer side. Should not be changed during gameplay.")]
    public PortalRenderTextureSize renderTextureSize = PortalRenderTextureSize._1024;

    // ----------- public singleton:

    public static PortalManager Instance { get; private set; }

    // ----------- public

    public PortalGun ActivePortalGun {
        private get {
            return activePortalGun;
        }
        set {
            if (value != null) {
                value.OnPortalCreated.AddListener(PortalCreated);
                value.OnPortalBeforeRemoval.AddListener(PortalBeforeRemoval);
                value.OnPortalRelocated.AddListener(PortalRelocated);
                print("[PortalManager] Portal gun registered.");
            } else {
                if (activePortalGun != null) {
                    activePortalGun.OnPortalCreated.RemoveListener(PortalCreated);
                    activePortalGun.OnPortalBeforeRemoval.RemoveListener(PortalBeforeRemoval);
                    activePortalGun.OnPortalRelocated.RemoveListener(PortalRelocated);
                    print("[PortalManager] Portal gun removed.");
                }
            }
            activePortalGun = value;
        }
    }

    public Vector2Int RenderTextureSize {
        get {
            return rtSize;
        }
    }


    // ----------- private:

    // There is no limitations on how many portal can be opened of one kind. This depends on the Portal Gun.
    HashSet<Portal> activePortals = new HashSet<Portal>();

    // Hashed version of List<PortalConnection> portalConnections, in two ways.
    Dictionary<Portal, Portal> portalPrefabConnectionCacheMap;

    // Active portal gun.
    PortalGun activePortalGun = null;

    // Existing connection info, used at runtime.
    List<PortalConnectionInfo> connectionInfo;

    // Render texture's size. Only useful when renderMethod == PortalRenderMethod.RenderTexture.
    Vector2Int rtSize;

    void InitConnectionCacheMap() {
        portalPrefabConnectionCacheMap = new Dictionary<Portal, Portal>();
        foreach (PortalConnectionSetup setup in portalConnections) {
            if (setup.sourcePortal == null || setup.targetPortal == null) {
                throw new Exception("Invalid portalConnections");
            }
            portalPrefabConnectionCacheMap.Add(setup.sourcePortal, setup.targetPortal);
            if (!setup.oneWayConnection) {
                portalPrefabConnectionCacheMap.Add(setup.targetPortal, setup.sourcePortal);
            }
        }
    }

    private void Start() {

        InitConnectionCacheMap();

        connectionInfo = new List<PortalConnectionInfo>();

        referencePortalPrefab.GetComponent<Portal>().FillDetectionPoints();

        // Calculate render texture's size.
        rtSize = Vector2Int.zero;
        Vector2 sizeXY = referencePortalPrefab.sizeXY;
        if (sizeXY.x >= sizeXY.y) {
            float ratio = sizeXY.y / sizeXY.x;
            rtSize.x = (int)renderTextureSize;
            rtSize.y = (int)(rtSize.x * ratio);
        } else {
            float ratio = sizeXY.x / sizeXY.y;
            rtSize.y = (int)renderTextureSize;
            rtSize.x = (int)(rtSize.y * ratio);
        }
    }

    private void Awake() {

        // singleton
        if (Instance != null) {
            Destroy(gameObject);
        } else {
            DontDestroyOnLoad(this);
            Instance = this;
        }
    }

    void PortalCreated(Portal portal) {
        activePortals.Add(portal);

        // Check connections.
        Portal portalPrefab = portal.PortalPrefab;
        if (portalPrefabConnectionCacheMap.ContainsKey(portalPrefab)) {
            Portal otherPortalPrefab = portalPrefabConnectionCacheMap[portalPrefab];
            foreach (Portal otherPortal in activePortals) {
                if (otherPortal.PortalPrefab == otherPortalPrefab) {
                    // find an available portal
                    InitPortalConnection(portal, otherPortal);
                    // two-way connection?
                    if (portalPrefabConnectionCacheMap.ContainsKey(otherPortalPrefab) && portalPrefabConnectionCacheMap[otherPortalPrefab] == portalPrefab) {
                        InitPortalConnection(otherPortal, portal);
                    }
                }
            }
        }
    }

    void PortalRelocated(Portal portal) {

    }

    void PortalBeforeRemoval(Portal portal) {
        activePortals.Remove(portal);
    }

    PortalConnectionInfo InitPortalConnection(Portal sourcePortal, Portal targetPortal) {

        PortalConnectionInfo info = new PortalConnectionInfo {
            sourcePortal = sourcePortal,
            targetPortal = targetPortal
        };

        info.cameraObject = new GameObject("Camera: " + sourcePortal.name + "->" + targetPortal.name);
        Camera camera = info.cameraObject.AddComponent<Camera>();

        if (renderMethod == PortalRenderMethod.RenderTexture) {
            info.renderTexture = new RenderTexture(rtSize.x, rtSize.y, 0);
            camera.targetTexture = info.renderTexture;
        } else if (renderMethod == PortalRenderMethod.Stencil) {
            throw new NotImplementedException();
        }

        sourcePortal.ConnectTo(targetPortal);

        if (renderMethod == PortalRenderMethod.RenderTexture) {
            sourcePortal.SetPortalTexture(info.renderTexture);
        }

        connectionInfo.Add(info);

        print("Connection created: " + sourcePortal.name + " -> " + targetPortal.name);

        return info;
    }
}

