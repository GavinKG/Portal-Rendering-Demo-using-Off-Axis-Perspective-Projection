using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;


/// <summary>
/// Portal gun.
/// Manage portal object creation and placement, just like a real gun's object-oriented model.
/// Does not manage how portals interact with each other.
/// Similar to UE's Player Controller.
/// </summary>
[RequireComponent(typeof(Camera))]
public class PortalGun : MonoBehaviour {

    // ----------- classes:

    [System.Serializable]
    public class PortalCreatedEvent : UnityEvent<Portal> { }

    [System.Serializable]
    public class PortalRelocatedEvent : UnityEvent<Portal> { }

    [System.Serializable]
    public class PortalBeginRemovalEvent : UnityEvent<Portal> { }

    // ----------- editor:

    [Tooltip("Error delta when detecting portal placement.")]
    public float detectionPointDelta = 0.1f;

    [Tooltip("Should we show the placement preview portal?")]
    public bool showPlacementPreview = true;

    public GameObject previewPortalPrefabObject;

    [Tooltip("If true, portal will not be placed across multiple gameobjects, even if they form a perfect wall.")]
    public bool shouldPlacedOnSingleObject = false;

    public PortalCreatedEvent OnPortalCreated;
    public PortalRelocatedEvent OnPortalRelocated;
    public PortalBeginRemovalEvent OnPortalBeforeRemoval;

    // ----------- public:



    // ----------- private:

    Camera camera;

    Portal previewPortal;

    List<Portal> portalClip; // size = PortalManager.Instance.portalPrefabs.Count, like an ammo clip.

    delegate bool ActionBinding(); // Func<bool>

    List<ActionBinding> actionBindingList;

    // Generated by ShouldPlacePortalUnderScreenCenter
    Vector3 portalPositionUnderScreenCenter;
    Quaternion portalRotationUnderScreenCenter;

    // Check portal detection point against scene geometry to find out whether a portal can be created under the mouse
    bool ShouldPlacePortalUnderScreenCenter() {

        // shoot a ray from center of the screen
        Ray ray = camera.ViewportPointToRay(new Vector2(0.5f, 0.5f));
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit)) {
            GameObject wall = hit.transform.gameObject;

            // get portal local coord (world basis)
            Vector3 forward = hit.normal; // z
            Vector3 up; // y
            Vector3 right; // x

            if (1 - Mathf.Abs(Vector3.Dot(forward, Vector3.up)) <= float.Epsilon) {
                // normal facing up or down
                up = camera.transform.forward;
                up.y = 0;
                up.Normalize();

                // camera facing up or down
                if (up.sqrMagnitude == 0.0f) {
                    up = camera.transform.up;
                }
                right = Vector3.Cross(forward, up);
            } else {
                up = Vector3.up;
                right = Vector3.Cross(forward, up);
                up = Vector3.Cross(forward, right);
            }

            // check detection points
            foreach (Vector3 detectionPoint in PortalManager.Instance.referencePortalPrefab.DetectionPoints) {
                // calculate detection point world position
                Vector3 worldPosition = right * detectionPoint.x + up * detectionPoint.y + forward * detectionPoint.z + hit.point;

                // shoot a camera->point ray
                Vector3 camToPoint = worldPosition - transform.position;
                Ray detectionRay = new Ray(transform.position, camToPoint.normalized);
                float camToPointLength = camToPoint.magnitude;
                RaycastHit detectionHit;
                if (Physics.Raycast(detectionRay, out detectionHit)) {
                    if (shouldPlacedOnSingleObject) {
                        if (detectionHit.transform.gameObject != hit.transform.gameObject) {
                            // not sharing same gameobject.
                            return false;
                        }
                    }

                    if (Mathf.Abs(detectionHit.distance - camToPointLength) <= detectionPointDelta) {
                        portalPositionUnderScreenCenter = hit.point;
                        portalRotationUnderScreenCenter = Quaternion.LookRotation(forward, up);
                    } else {
                        // detection point not attached to back wall.
                        return false;
                    }
                } else {
                    // detection point ray not hit.
                    return false;
                }

            }
        } else {
            // mouse ray not hit.
            return false;
        }
        return true;
    }

    void Start() {

        if (PortalManager.Instance == null) {
            throw new System.Exception("Scene does not contain a Portal Manager.");
        }

        PortalManager.Instance.ActivePortalGun = this;

        camera = GetComponent<Camera>();

        if (showPlacementPreview) {
            previewPortal = InstantiatePortalObject(previewPortalPrefabObject).GetComponent<Portal>();
            previewPortal.gameObject.SetActive(false);
        }

        portalClip = new List<Portal>(PortalManager.Instance.portalPrefabs.Count); // list of null
        for (int i = 0; i < PortalManager.Instance.portalPrefabs.Count; ++i) {
            portalClip.Add(null);
        }

        FillActionBindingList();
    }

    void Update() {

        // fetch action
        int portalIndex = GetPortalIndexFromAction();

        // check portal placement
        bool canPlace = false;
        if (portalIndex != -1 || showPlacementPreview) {
            canPlace = ShouldPlacePortalUnderScreenCenter();
        }

        // preview
        if (showPlacementPreview) {
            if (canPlace) {
                previewPortal.transform.position = portalPositionUnderScreenCenter;
                previewPortal.transform.rotation = portalRotationUnderScreenCenter;
                previewPortal.gameObject.SetActive(true);
            } else {
                previewPortal.gameObject.SetActive(false);
            }
        }

        // placement
        // todo: add logic to close portal...
        if (portalIndex != -1) {
            OpenPortal(portalIndex, portalPositionUnderScreenCenter, portalRotationUnderScreenCenter);
        }

    }

    // hardcoded action binding logic. Will be replaced by config files.
    // portal with lower idx will have priority on higher idx when both actions are detected.
    void FillActionBindingList() {

        actionBindingList = new List<ActionBinding> {

            // left click -> portal #0
            () => Input.GetMouseButtonDown(0),

            // right click -> portal #1
            () => Input.GetMouseButtonDown(1)
        };
    }

    int GetPortalIndexFromAction() {

        if (actionBindingList == null) {
            return -1;
        }

        for (int i = 0; i < actionBindingList.Count; ++i) {
            if (actionBindingList[i]()) {
                return i;
            }
        }
        return -1;
    }

    void OpenPortal(int idx, Vector3 position, Quaternion rotation) {
        if (portalClip[idx] == null) {
            portalClip[idx] = InstantiatePortalObject(PortalManager.Instance.portalPrefabs[idx]).GetComponent<Portal>();
            portalClip[idx].transform.position = position;
            portalClip[idx].transform.rotation = rotation;
            portalClip[idx].gameObject.SetActive(true);

            portalClip[idx].GetComponent<Portal>()?.DisplayPortalCreated();
            OnPortalCreated.Invoke(portalClip[idx]);

            print("[PortalGun] Portal #" + idx.ToString() + " created.");
        } else {
            RelocatePortal(idx, position, rotation);
        }


    }

    void ClosePortal(int idx) {
        portalClip[idx].gameObject.SetActive(false);
    }

    void RelocatePortal(int idx, Vector3 position, Quaternion rotation) {

        Vector3 oldPortalPosition = portalClip[idx].transform.position;
        Quaternion oldPortalRotation = portalClip[idx].transform.rotation;

        portalClip[idx].transform.position = position;
        portalClip[idx].transform.rotation = rotation;

        portalClip[idx].GetComponent<Portal>()?.DisplayPortalRelocated(oldPortalPosition, oldPortalRotation);
        OnPortalRelocated.Invoke(portalClip[idx]);

        print("[PortalGun] Portal #" + idx.ToString() + " relocated.");
    }


    bool IsPortalActive(int idx) {
        return portalClip[idx] != null && portalClip[idx].gameObject.activeInHierarchy;
    }

    GameObject InstantiatePortalObject(GameObject portalPrefabObject) {
        return InstantiatePortalObject(portalPrefabObject.GetComponent<Portal>());
    }

    GameObject InstantiatePortalObject(Portal portalPrefab) {
        // todo: should we use PrefabUtility?
        GameObject ret = Instantiate(portalPrefab.gameObject);
        ret.GetComponent<Portal>().SetPrefabPortal(portalPrefab);
        return ret;
    }

}
