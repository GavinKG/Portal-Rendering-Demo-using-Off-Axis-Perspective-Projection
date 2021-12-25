#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

using UnityEngine;

namespace UnityTemplateProjects {
    public class SimpleCameraController : MonoBehaviour {
        class CameraState {
            public float yaw;
            public float pitch;
            public float roll;
            public float x;
            public float y;
            public float z;

            public void SetFromTransform(Transform t) {
                pitch = t.eulerAngles.x;
                yaw = t.eulerAngles.y;
                roll = t.eulerAngles.z;
                x = t.position.x;
                y = t.position.y;
                z = t.position.z;
            }

            public void Translate(Vector3 translation) {
                Vector3 rotatedTranslation = Quaternion.Euler(pitch, yaw, roll) * translation;

                x += rotatedTranslation.x;
                y += rotatedTranslation.y;
                z += rotatedTranslation.z;
            }

            public void LerpTowards(CameraState target, float positionLerpPct, float rotationLerpPct) {
                yaw = Mathf.Lerp(yaw, target.yaw, rotationLerpPct);
                pitch = Mathf.Lerp(pitch, target.pitch, rotationLerpPct);
                roll = Mathf.Lerp(roll, target.roll, rotationLerpPct);

                x = Mathf.Lerp(x, target.x, positionLerpPct);
                y = Mathf.Lerp(y, target.y, positionLerpPct);
                z = Mathf.Lerp(z, target.z, positionLerpPct);
            }

            public void UpdateTransform(Transform t) {
                t.eulerAngles = new Vector3(pitch, yaw, roll);
                t.position = new Vector3(x, y, z);
            }
        }

        const float k_MouseSensitivityMultiplier = 0.01f;

        CameraState m_TargetCameraState = new CameraState();
        CameraState m_InterpolatingCameraState = new CameraState();

        [Header("View Mode")]
        public bool rightClickToRotate = false;

        [Header("Movement Settings")]
        [Tooltip("Exponential boost factor on translation, controllable by mouse wheel.")]
        public float boost = 3.5f;

        [Tooltip("Time it takes to interpolate camera position 99% of the way to the target."), Range(0.001f, 1f)]
        public float positionLerpTime = 0.2f;

        [Header("Rotation Settings")]
        [Tooltip("Multiplier for the sensitivity of the rotation.")]
        public float mouseSensitivity = 60.0f;

        [Tooltip("X = Change in mouse position.\nY = Multiplicative factor for camera rotation.")]
        public AnimationCurve mouseSensitivityCurve = new AnimationCurve(new Keyframe(0f, 0.5f, 0f, 5f), new Keyframe(1f, 2.5f, 0f, 0f));

        [Tooltip("Time it takes to interpolate camera rotation 99% of the way to the target."), Range(0.001f, 1f)]
        public float rotationLerpTime = 0.01f;

        [Tooltip("Whether or not to invert our Y axis for mouse input to rotation.")]
        public bool invertY = false;


        void Start() {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        void OnEnable() {
            m_TargetCameraState.SetFromTransform(transform);
            m_InterpolatingCameraState.SetFromTransform(transform);
        }

        Vector3 GetInputTranslationDirection() {
            Vector3 direction = Vector3.zero;

            if (Input.GetKey(KeyCode.W)) {
                direction += Vector3.forward;
            }
            if (Input.GetKey(KeyCode.S)) {
                direction += Vector3.back;
            }
            if (Input.GetKey(KeyCode.A)) {
                direction += Vector3.left;
            }
            if (Input.GetKey(KeyCode.D)) {
                direction += Vector3.right;
            }
            if (Input.GetKey(KeyCode.Q)) {
                direction += Vector3.down;
            }
            if (Input.GetKey(KeyCode.E)) {
                direction += Vector3.up;
            }
            return direction;
        }

        void Update() {
            // Exit Sample  

            if (IsEscapePressed()) {
                Application.Quit();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#endif
            }

            if (rightClickToRotate) {
                // Hide and lock cursor when right mouse button pressed
                if (IsRightMouseButtonDown()) {
                    Cursor.lockState = CursorLockMode.Locked;
                }

                // Unlock and show cursor when right mouse button released
                if (IsRightMouseButtonUp()) {
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                }
            } else {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }



            // Rotation
            if (IsCameraRotationAllowed()) {
                var mouseMovement = GetInputLookRotation() * k_MouseSensitivityMultiplier * mouseSensitivity;
                if (invertY)
                    mouseMovement.y = -mouseMovement.y;

                var mouseSensitivityFactor = mouseSensitivityCurve.Evaluate(mouseMovement.magnitude);

                m_TargetCameraState.yaw += mouseMovement.x * mouseSensitivityFactor;
                m_TargetCameraState.pitch += mouseMovement.y * mouseSensitivityFactor;
            }

            // Translation
            var translation = GetInputTranslationDirection() * Time.deltaTime;

            // Speed up movement when shift key held
            if (IsBoostPressed()) {
                translation *= 10.0f;
            }

            // Modify movement by a boost factor (defined in Inspector and modified in play mode through the mouse scroll wheel)
            boost += GetBoostFactor();
            translation *= Mathf.Pow(2.0f, boost);

            m_TargetCameraState.Translate(translation);

            // Framerate-independent interpolation
            // Calculate the lerp amount, such that we get 99% of the way to our target in the specified time
            var positionLerpPct = 1f - Mathf.Exp((Mathf.Log(1f - 0.99f) / positionLerpTime) * Time.deltaTime);
            var rotationLerpPct = 1f - Mathf.Exp((Mathf.Log(1f - 0.99f) / rotationLerpTime) * Time.deltaTime);
            m_InterpolatingCameraState.LerpTowards(m_TargetCameraState, positionLerpPct, rotationLerpPct);

            m_InterpolatingCameraState.UpdateTransform(transform);
        }

        float GetBoostFactor() {
            return Input.mouseScrollDelta.y * 0.01f;
        }

        Vector2 GetInputLookRotation() {
            // try to compensate the diff between the two input systems by multiplying with empirical values
            return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        }

        bool IsBoostPressed() {
            return Input.GetKey(KeyCode.LeftShift);

        }

        bool IsEscapePressed() {
            return Input.GetKey(KeyCode.Escape);
        }

        bool IsCameraRotationAllowed() {
            if (rightClickToRotate) {
                return Input.GetMouseButton(1);
            } else {
                return true;
            }
        }

        bool IsRightMouseButtonDown() {
            return Input.GetMouseButtonDown(1);
        }

        bool IsRightMouseButtonUp() {
            return Input.GetMouseButtonUp(1);
        }

    }

}
