using System.Collections;
using System.Collections.Generic;
using UnityEngine;

static class Utils {
    public static bool IsPrefab(GameObject gameObject) {
        return gameObject != null && gameObject.scene.name == null;
    }
}