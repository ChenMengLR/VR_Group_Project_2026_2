using Oculus.Interaction;
using UnityEngine;

namespace CourseProject
{
    /// <summary>Release a carried core into a bounded, slow drift; optional props float from scene start.</summary>
    [DisallowMultipleComponent]
    public sealed class StationFloatingBody : MonoBehaviour
    {
        [SerializeField] private Rigidbody body;
        [SerializeField] private Grabbable grab;
        [SerializeField] private bool activateAfterRelease = true;
        private bool initialized;
        private bool releasePending;
        private bool wasHeld;
        private bool oldGravity;
        private float oldLinearDamping, oldAngularDamping;
        private Vector3 anchor;
        public bool IsFloating { get; private set; }
        public Vector3 Anchor => anchor;

        public void Configure(Rigidbody target, Grabbable targetGrab, bool waitForRelease = true)
        {
            body = target;
            grab = targetGrab;
            activateAfterRelease = waitForRelease;
        }

        private void OnEnable()
        {
            if (body == null) return;
            oldGravity = body.useGravity;
            oldLinearDamping = body.linearDamping;
            oldAngularDamping = body.angularDamping;
            initialized = true;
            if (grab != null) grab.WhenPointerEventRaised += OnPointer;
            if (!activateAfterRelease) StartFloating();
        }

        private void OnPointer(PointerEvent pointer)
        {
            if (pointer.Type == PointerEventType.Select)
            {
                wasHeld = true;
                releasePending = false;
                IsFloating = false;
                RestoreBodySettings();
            }
            else if ((pointer.Type == PointerEventType.Unselect || pointer.Type == PointerEventType.Cancel) && wasHeld)
            {
                releasePending = grab.GrabPoints.Count == 0;
            }
        }

        private void FixedUpdate()
        {
            if (!initialized) return;
            if (body.isKinematic)
            {
                IsFloating = false;
                return;
            }
            if (grab != null && grab.GrabPoints.Count > 0) return;
            if (releasePending)
            {
                releasePending = false;
                wasHeld = false;
                StartFloating();
            }
            if (!IsFloating) return;

            // Soft tethering keeps the core within reach of its release point and clear of overhead space.
            Vector3 offset = body.position - anchor;
            Vector3 beyond = offset - Vector3.ClampMagnitude(offset, .28f);
            body.AddForce(Vector3.ClampMagnitude(-beyond * 2.5f, .7f), ForceMode.Acceleration);
            body.linearVelocity = Vector3.ClampMagnitude(body.linearVelocity, .28f);
            body.angularVelocity = Vector3.ClampMagnitude(body.angularVelocity, .28f);
            if (!activateAfterRelease)
            {
                Vector3 drift = new Vector3(0f, Mathf.Sin(Time.time * .7f) * .035f, 0f);
                body.AddForce((anchor + drift - body.position) * .4f, ForceMode.Acceleration);
                body.angularVelocity = Vector3.MoveTowards(body.angularVelocity,
                    new Vector3(.045f, .16f, .035f), .04f * Time.fixedDeltaTime);
            }
        }

        private void StartFloating()
        {
            if (body.isKinematic) return;
            anchor = body.position;
            if (activateAfterRelease) anchor.y = Mathf.Clamp(anchor.y, .55f, 2.35f);
            body.useGravity = false;
            body.linearDamping = .8f;
            body.angularDamping = .12f;
            body.linearVelocity = Vector3.ClampMagnitude(body.linearVelocity, .22f);
            body.angularVelocity = new Vector3(.045f, .16f, .035f);
            body.WakeUp();
            IsFloating = true;
        }

        /// <summary>Call after the mission has restored the core pose and dynamic body.</summary>
        public void ResetFloat()
        {
            releasePending = false;
            wasHeld = false;
            IsFloating = false;
            if (!initialized) return;
            RestoreBodySettings();
            anchor = body.position;
            if (!activateAfterRelease) StartFloating();
        }

        private void RestoreBodySettings()
        {
            body.useGravity = oldGravity;
            body.linearDamping = oldLinearDamping;
            body.angularDamping = oldAngularDamping;
        }

        private void OnDisable()
        {
            if (!initialized) return;
            if (grab != null) grab.WhenPointerEventRaised -= OnPointer;
            RestoreBodySettings();
            IsFloating = false;
            releasePending = false;
            wasHeld = false;
            initialized = false;
        }
    }
}
