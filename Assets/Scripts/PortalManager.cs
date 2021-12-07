using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class PortalConnection {

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


    // ----------- editor:

    [Tooltip("Portal prefab used to preview placement. Cannot be changed during gameplay.")]
    public Portal previewPortalPrefab;

    [Tooltip("Can contain arbitrary amount of portals. Cannot be changed during gameplay.")]
    public List<Portal> portalPrefabs;

    [Tooltip("Defines how portals connect to each other. Should only contains prefabs. Cannot be changed during gameplay.")]
    public List<PortalConnection> portalConnections;

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

    public Portal GetTargetPortalObjectFrom(Portal sourcePortal) {
        if (connectionCacheMap.ContainsKey(sourcePortal)) {
            return connectionCacheMap[sourcePortal];
        }
        return null;
    }

    // ----------- private:

    // There is no limitations on how many portal can be opened of one kind. This depends on the Portal Gun.
    HashSet<Portal> activePortals = new HashSet<Portal>();

    // Hashed version of List<PortalConnection> portalConnections, in two ways.
    Dictionary<Portal, Portal> connectionCacheMap;

    // Active portal gun.
    PortalGun activePortalGun = null;

    // Existing connections.
    Dictionary<Portal, Portal> existingConnections;

    void InitConnectionCacheMap() {
        connectionCacheMap = new Dictionary<Portal, Portal>();
        foreach (PortalConnection connection in portalConnections) {
            if (connection.sourcePortal == null || connection.targetPortal == null) {
                throw new Exception("Invalid portalConnections");
            }
            connectionCacheMap.Add(connection.sourcePortal, connection.targetPortal);
            if (!connection.oneWayConnection) {
                connectionCacheMap.Add(connection.targetPortal, connection.sourcePortal);
            }
        }
    }

    private void Start() {
        InitConnectionCacheMap();
    }

    private void Awake() {
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
        if (connectionCacheMap.ContainsKey(portal)) {
            Portal otherPortal = connectionCacheMap[portal];
            portal.ConnectTo(otherPortal);

            // check if two-way connection
            if (connectionCacheMap.ContainsKey(otherPortal)) {
                if (connectionCacheMap[otherPortal] == portal) {
                    otherPortal.ConnectTo(portal);
                } else {
                    throw new Exception("There is something wrong on portal connection field.");
                }
            }
        }
    }

    void PortalRelocated(Portal portal) {

    }

    void PortalBeforeRemoval(Portal portal) {
        activePortals.Remove(portal);
    }
}

