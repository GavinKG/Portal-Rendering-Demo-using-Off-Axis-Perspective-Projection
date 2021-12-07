using System.Collections;
using System.Collections.Generic;
using UnityEngine;

static class PortalUtils {

    public static bool IsPortalObject(GameObject gameObject) {
        return gameObject != null && gameObject.GetComponent<Portal>() != null;
    }

    public static bool IsPortalPrefabObject(GameObject gameObject) {
        return IsPortalObject(gameObject) && Utils.IsPrefab(gameObject);
    }

    public static bool IsPortalInstanceObject(GameObject gameObject) {
        return IsPortalObject(gameObject) && !Utils.IsPrefab(gameObject);
    }
}

// Some programming rules I have to say:
// * I trust myself when writting private code, so no valid check on parameters.
// * I do not trust anyone when writting public code, even if the caller code is also written by myself. So there will be extensive param checks.

// Some portal naming rules:
// * Portal: A MonoBehaviour instance of class `Portal`, attached to a GameObject.
// * Portal Prefab: A MonoBehaviour instance of class `Portal`, but it's attached GameObject is a prefab (therefore not presented in the scene, like CDO in Unreal)
// * Portal Object: The GameObject a `Portal` class instance is attached to.
// * Portal Prefab Object: The Prefab GameObject a `Portal` class instance is attached to.
// Most code use Portal instead of Portal Object, since Monobehaviour must attached to a GameObject.
