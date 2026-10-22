using Oculus.Interaction.Locomotion;
using TMPro;
using UnityEngine;
using UnityEngine.XR;

namespace CourseProject
{
    /// <summary>Quest Touch bindings. Meta's existing locomotor owns collision, gravity and HMD pivot.</summary>
    [DefaultExecutionOrder(-50)]
    public sealed class SpaceStationControls : MonoBehaviour
    {
        public const string Help = "LEFT STICK  Move / strafe\nRIGHT STICK  Left / right: turn 30 degrees\nRIGHT STICK  Forward: aim teleport; release: travel\nGRIP  Hold core; release in docking socket\nTRIGGER  Point and click UI\nLEFT MENU  Controls / reset     B  Close menu";
        [SerializeField] private FirstPersonLocomotor locomotor;
        [SerializeField] private Transform head;
        [SerializeField] private GameObject rightTeleport;
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private TMP_Text menuStatus;
        [SerializeField] private SpaceStationMission mission;
        [SerializeField, Range(.1f, .4f)] private float moveDeadzone = .2f;
        [SerializeField, Range(.5f, 2f)] private float moveSpeed = 1.5f;
        [SerializeField, Range(15f, 45f)] private float snapDegrees = 30f;
        private bool turnArmed;
        private bool requireNeutral = true;
        private bool menuVisible;
        public bool MenuVisible => menuVisible;
        public FirstPersonLocomotor Locomotor => locomotor;
        public GameObject RightTeleport => rightTeleport;
        public GameObject MenuPanel => menuPanel;

        private void Awake()
        {
            // Velocity events are acceleration input in Meta ISDK, not metres per second.
            // With input before locomotor.Update, steady speed = acceleration * speedFactor / damping.
            locomotor.Acceleration = 5f;
            locomotor.GroundDamping = 40f;
            locomotor.SpeedFactor = moveSpeed * locomotor.GroundDamping / locomotor.Acceleration;
            locomotor.FlattenInputVelocity = false; // Already flattened below, including straight up/down gaze.
            menuPanel.SetActive(false);
            SetTeleportEnabled(false);
        }

        private void Update()
        {
            bool focused = XRSettings.isDeviceActive && OVRManager.hasInputFocus;
            if (focused)
            {
                if (OVRInput.GetDown(OVRInput.Button.Start, OVRInput.Controller.LTouch)) ToggleMenu();
                if (menuVisible && OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch)) CloseMenu();
            }
            ProcessInput(OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch),
                OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch), focused);
            if (menuVisible && menuStatus != null) menuStatus.text = mission.CurrentStatus;
        }

        /// <summary>Shared input path used by hardware and the deterministic editor regression check.</summary>
        public void ProcessInput(Vector2 left, Vector2 right, bool focused)
        {
            if (!focused || menuVisible)
            {
                requireNeutral = true;
                turnArmed = false;
                StopHorizontalMotion();
                SetTeleportEnabled(false);
                return;
            }
            if (requireNeutral)
            {
                if (left.magnitude > moveDeadzone || right.magnitude > .25f) return;
                requireNeutral = false;
                turnArmed = true;
                SetTeleportEnabled(true);
            }

            // While aiming a teleport, avoid drifting or turning the target beneath the player.
            if (right.y > .5f)
            {
                StopHorizontalMotion();
                turnArmed = false;
                return;
            }
            Vector2 input = ApplyDeadzone(left, moveDeadzone);
            if (input.sqrMagnitude > 0f)
            {
                Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
                if (forward.sqrMagnitude < .001f)
                    forward = Vector3.Cross(head.right, Vector3.up);
                forward.Normalize();
                Vector3 velocityInput = forward * input.y + Vector3.Cross(Vector3.up, forward) * input.x;
                locomotor.HandleLocomotionEvent(new LocomotionEvent(GetInstanceID(), velocityInput,
                    LocomotionEvent.TranslationType.Velocity));
            }
            if (right.magnitude < .25f) turnArmed = true;
            if (turnArmed && Mathf.Abs(right.x) >= .7f && Mathf.Abs(right.y) < .5f)
            {
                turnArmed = false;
                locomotor.HandleLocomotionEvent(new LocomotionEvent(GetInstanceID(),
                    Quaternion.Euler(0f, Mathf.Sign(right.x) * snapDegrees, 0f), LocomotionEvent.RotationType.Relative));
            }
        }

        public static Vector2 ApplyDeadzone(Vector2 input, float deadzone)
        {
            float magnitude = input.magnitude;
            if (magnitude <= deadzone) return Vector2.zero;
            return input / magnitude * Mathf.Clamp01((magnitude - deadzone) / (1f - deadzone));
        }

        public void ToggleMenu()
        {
            if (menuVisible) { CloseMenu(); return; }
            var forward = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < .01f) forward = Vector3.Cross(head.right, Vector3.up).normalized;
            menuPanel.transform.SetPositionAndRotation(head.position + forward * 1.1f - Vector3.up * .12f,
                Quaternion.LookRotation(forward, Vector3.up));
            menuVisible = true;
            menuPanel.SetActive(true);
            requireNeutral = true;
            StopHorizontalMotion();
            SetTeleportEnabled(false);
        }

        public void CloseMenu()
        {
            menuVisible = false;
            menuPanel.SetActive(false);
            requireNeutral = true;
            turnArmed = false;
        }

        public void RequestReset()
        {
            mission.ResetMission();
            if (menuStatus != null) menuStatus.text = mission.CurrentStatus;
        }

        private void StopHorizontalMotion()
        {
            var velocity = locomotor.Velocity;
            locomotor.Velocity = new Vector3(0f, velocity.y, 0f);
        }

        private void SetTeleportEnabled(bool value)
        {
            if (rightTeleport.activeSelf != value) rightTeleport.SetActive(value);
        }

        private void OnDisable()
        {
            if (locomotor != null) StopHorizontalMotion();
            if (rightTeleport != null) SetTeleportEnabled(false);
            if (menuPanel != null) menuPanel.SetActive(false);
            menuVisible = false;
            requireNeutral = true;
            turnArmed = false;
        }
    }
}
