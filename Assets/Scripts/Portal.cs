using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Portal. Purely a passive, display-only object.
/// Portal's transform will be handled by Portal Gun.
/// </summary>
public class Portal : MonoBehaviour {
    // ----------- editor:

    [Tooltip("Used to place portal onto the wall.")]
    public List<GameObject> DetectionPoints;

    [Tooltip("This GameObject is used to present the other side of the portal. Its material will be used when using RenderTexture method to draw portal.")]
    public GameObject portalMeshObject;

    [Tooltip("Hide the portal mesh object when not connected to target portal. Check this if you only use portal mesh for displaying portal image.")]
    public bool hidePortalMeshWhenDisconnected = false;

    public bool drawDebugInfo = false;

    // ----------- public:

    // ref to portal prefab object's Portal component.
    public Portal PortalPrefab { get; set; }

    // ----------- private:

    Portal connectedPortal;

    Texture portalMeshObjectOriginalTexture;

    

    private void OnDrawGizmos() {
        if (!drawDebugInfo) {
            return;
        }

        Gizmos.color = Color.white;
        foreach (GameObject point in DetectionPoints) {
            Gizmos.DrawWireSphere(point.transform.position, 0.1f);

        }
        Gizmos.DrawRay(transform.position, transform.TransformDirection(Vector3.forward));
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
    public void SetPortalPrefab(Portal portal) {
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
        // this Mesh Renderer should only have one material, with a texture sampler named "Texture".
        renderer.material.SetTexture(Shader.PropertyToID("Texture"), rt);
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




}
