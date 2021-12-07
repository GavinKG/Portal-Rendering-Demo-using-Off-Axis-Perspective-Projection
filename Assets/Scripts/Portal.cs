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

    public bool drawDebugInfo = false;

    // ----------- public:

    // ----------- private:

    Portal connectedPortal;

    // ref to portal prefab object's Portal component.
    Portal portalPrefab;

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
        portalPrefab = portal;
    }

    public bool IsSameKindAs(GameObject otherPortalObject) {

        if (portalPrefab == null) {
            // this gameobject might be a prefab, or someone forgot to set it up so we cannot get the answer...
            return false;
        }

        if (PortalUtils.IsPortalInstanceObject(otherPortalObject)) {
            // Portal prefab objects cannot be of the same kind as any other portal objects exists.
            // Since "portal prefab object" are not "portal instance object", and there should be only one prefab object exists.
            return false;
        }

        return portalPrefab == otherPortalObject.GetComponent<Portal>().portalPrefab;

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
    }

    public void Disconnect() {
        if (connectedPortal == null) {
            return;
        }
        // ...
        DisplayPortalDisconnected();
    }


}
