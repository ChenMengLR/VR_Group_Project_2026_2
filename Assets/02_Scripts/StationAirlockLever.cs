using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using TMPro;
using UnityEngine;

namespace CourseProject
{
    /// <summary>A fixed-axis grab interaction that releases a powered airlock.</summary>
    [DisallowMultipleComponent]
    public sealed class StationAirlockLever : MonoBehaviour, ITransformer
    {
        public const float PullDegrees = 65f;
        public const float TriggerDegrees = 55f;
        [SerializeField] private SpaceStationMission mission;
        [SerializeField] private Grabbable grab;
        [SerializeField] private Transform pivot;
        [SerializeField] private Transform handle;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Renderer statusLamp;
        [SerializeField] private Material lockedMaterial;
        [SerializeField] private Material readyMaterial;
        [SerializeField] private Material activatedMaterial;
        [SerializeField] private AudioSource latchAudio;
        private GrabInteractable[] controllerTargets;
        private HandGrabInteractable[] handTargets;
        private Vector3 initialPointerDirection;
        private float initialAngle;
        private float angleDegrees;
        private bool latched;
        private bool transforming;
        private bool interactionEnabled = true;
        private int feedbackState = -1;

        public Grabbable Grab => grab;
        public Transform Pivot => pivot;
        public Transform Handle => handle;
        public float AngleDegrees => angleDegrees;
        public bool IsHeld => grab != null && grab.GrabPoints != null && grab.GrabPoints.Count > 0;
        public bool IsLatched => latched;
        public bool IsAvailable => isActiveAndEnabled && mission != null && mission.CanOpenHatch && !latched;

        public void Configure(SpaceStationMission task, Grabbable grabbable, Transform hinge,
            Transform rotatingHandle, TMP_Text caption, Renderer lamp,
            Material locked, Material ready, Material activated, AudioSource audio)
        {
            mission = task;
            grab = grabbable;
            pivot = hinge;
            handle = rotatingHandle;
            label = caption;
            statusLamp = lamp;
            lockedMaterial = locked;
            readyMaterial = ready;
            activatedMaterial = activated;
            latchAudio = audio;
            grab.MaxGrabPoints = 1;
            grab.InjectOptionalOneGrabTransformer(this);
            grab.InjectOptionalTwoGrabTransformer(this);
            grab.InjectOptionalTargetTransform(handle);
            grab.InjectOptionalThrowWhenUnselected(false);
            SetAngle(0f);
        }

        private void Awake()
        {
            controllerTargets = grab.GetComponentsInChildren<GrabInteractable>(true);
            handTargets = grab.GetComponentsInChildren<HandGrabInteractable>(true);
        }

        private void LateUpdate()
        {
            bool available = IsAvailable;
            if (available && IsHeld && angleDegrees >= TriggerDegrees && mission.RequestHatchOpen(this))
            {
                latched = true;
                SetAngle(PullDegrees);
                if (latchAudio != null && latchAudio.clip != null) latchAudio.Play();
                available = false;
            }
            SetInteractionEnabled(available);
            if (!latched && !IsHeld && angleDegrees > 0f)
                SetAngle(Mathf.MoveTowards(angleDegrees, 0f, 90f * Time.deltaTime));
            RefreshFeedback();
        }

        public void ResetLever()
        {
            SetInteractionEnabled(false);
            transforming = false;
            latched = false;
            SetAngle(0f);
            if (latchAudio != null) latchAudio.Stop();
            feedbackState = -1;
            RefreshFeedback();
        }

        private void SetInteractionEnabled(bool enabled)
        {
            if (interactionEnabled == enabled) return;
            interactionEnabled = enabled;
            if (controllerTargets != null)
                foreach (var target in controllerTargets) target.enabled = enabled;
            if (handTargets != null)
                foreach (var target in handTargets) target.enabled = enabled;
            // Grabbable.OnDisable sends Cancel for every active pointer, including synthetic input.
            // Both the SDK interaction and its accumulated grab motion end before a reset pose is set.
            if (grab != null) grab.enabled = enabled;
            if (!enabled) transforming = false;
        }

        private void RefreshFeedback()
        {
            if (mission == null) return;
            int state = mission.IsHatchOpen ? 4 : latched ? 3 : mission.IsPowerReady ? 2 : mission.IsInserted ? 1 : 0;
            if (feedbackState == state) return;
            feedbackState = state;
            if (statusLamp != null)
                statusLamp.sharedMaterial = state >= 3 ? activatedMaterial : state == 2 ? readyMaterial : lockedMaterial;
            if (label != null)
                label.text = state == 0 ? "MANUAL RELEASE\nPOWER OFFLINE" :
                    state == 1 ? "POWER LINKING\nPLEASE WAIT" :
                    state == 2 ? "HOLD GRIP\nPULL HANDLE DOWN" :
                    state == 3 ? "AIRLOCK OPENING\nPLEASE WAIT" : "AIRLOCK OPEN\nPROCEED TO EXIT";
        }

        private void SetAngle(float degrees)
        {
            angleDegrees = Mathf.Clamp(degrees, 0f, PullDegrees);
            handle.localRotation = Quaternion.AngleAxis(angleDegrees, Vector3.right);
        }

        public void Initialize(IGrabbable grabbable) { }

        public void BeginTransform()
        {
            transforming = IsAvailable && IsHeld;
            if (!transforming) return;
            initialAngle = angleDegrees;
            initialPointerDirection = Vector3.ProjectOnPlane(grab.GrabPoints[0].position - pivot.position, pivot.right);
            if (initialPointerDirection.sqrMagnitude < .0001f) transforming = false;
        }

        public void UpdateTransform()
        {
            if (!transforming || !IsAvailable || !IsHeld) return;
            Vector3 pointerDirection = Vector3.ProjectOnPlane(grab.GrabPoints[0].position - pivot.position, pivot.right);
            if (pointerDirection.sqrMagnitude < .0001f) return;
            float turn = Vector3.SignedAngle(initialPointerDirection, pointerDirection, pivot.right);
            SetAngle(initialAngle + turn);
        }

        public void EndTransform() => transforming = false;

        private void OnDisable() => SetInteractionEnabled(false);
    }
}
