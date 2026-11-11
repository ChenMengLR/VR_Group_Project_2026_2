using Oculus.Interaction.Locomotion;
using UnityEngine;
using MetaCharacter = Oculus.Interaction.Locomotion.CharacterController;

namespace CourseProject
{
    /// <summary>Small grounded boosts with controllable air motion through the existing collision capsule.</summary>
    [DisallowMultipleComponent]
    public sealed class StationLowGravity : MonoBehaviour
    {
        [SerializeField] private FirstPersonLocomotor locomotor;
        [SerializeField] private MetaCharacter character;
        [SerializeField, Range(.08f, .25f)] private float maximumLift = .20f;
        [SerializeField, Range(.4f, 1f)] private float moveSpeed = .9f;
        [SerializeField, Range(.05f, .2f)] private float gravityFactor = .10f;
        private const float GroundDamping = 8f;
        private const float AirDamping = 2.5f;
        private const float BrakeDescentSpeed = .22f;
        private bool initialized;
        private bool boostWasHeld;
        private bool boostActive;
        private float launchFeetHeight;
        private float launchTime;
        private float oldGravity, oldGroundDamping, oldAirDamping, oldJumpDamping, oldJumpForce, oldSpeed;

        public float MaximumLift => maximumLift;
        public float MoveSpeed => moveSpeed;
        public bool IsBoosting => boostActive;
        public bool BrakeHeld { get; private set; }
        public int BoostCount { get; private set; }
        public float FeetHeight => character.Pose.position.y - character.Height * .5f - character.SkinWidth;

        public void Configure(FirstPersonLocomotor mover, MetaCharacter capsule)
        {
            locomotor = mover;
            character = capsule;
        }

        public void Initialize()
        {
            if (initialized || locomotor == null || character == null) return;
            oldGravity = locomotor.GravityFactor;
            oldGroundDamping = locomotor.GroundDamping;
            oldAirDamping = locomotor.AirDamping;
            oldJumpDamping = locomotor.JumpDamping;
            oldJumpForce = locomotor.JumpForce;
            oldSpeed = locomotor.SpeedFactor;
            locomotor.GravityFactor = gravityFactor;
            locomotor.GroundDamping = GroundDamping;
            locomotor.AirDamping = AirDamping;
            locomotor.JumpDamping = 0f;
            locomotor.JumpForce = Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y) * gravityFactor * maximumLift);
            locomotor.SpeedFactor = moveSpeed * GroundDamping / locomotor.Acceleration;
            locomotor.WhenLocomotionEventHandled += OnLocomotion;
            initialized = true;
        }

        private void OnEnable()
        {
            if (Application.isPlaying) Initialize();
        }

        public void ProcessMotion(Vector3 worldInput, bool boostHeld, bool brakeHeld)
        {
            Initialize();
            if (!initialized) return;
            bool boostPressed = boostHeld && !boostWasHeld;
            boostWasHeld = boostHeld;
            BrakeHeld = brakeHeld;
            float delta = Mathf.Min(Time.deltaTime, .05f);
            Vector3 velocity = locomotor.Velocity;

            if (boostActive && locomotor.IsGrounded && velocity.y <= 0f) boostActive = false;
            if (brakeHeld)
            {
                BrakeMotion(delta);
                return;
            }

            if (boostPressed && locomotor.IsGrounded && !boostActive && !locomotor.IgnoringVelocity)
            {
                launchFeetHeight = FeetHeight;
                launchTime = Time.time;
                locomotor.Jump();
                boostActive = locomotor.Velocity.y > 0f;
                if (boostActive) BoostCount++;
            }

            velocity = locomotor.Velocity;
            if (boostActive && (FeetHeight >= launchFeetHeight + maximumLift || Time.time - launchTime > 1.8f))
                velocity.y = Mathf.Min(velocity.y, 0f);

            Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
            worldInput = Vector3.ClampMagnitude(Vector3.ProjectOnPlane(worldInput, Vector3.up), 1f);
            if (!locomotor.IsGrounded || velocity.y > 0f)
            {
                // The stock locomotor ignores acceleration events while airborne.
                // Set only its velocity, allowing its capsule to resolve every collision.
                if (worldInput.sqrMagnitude > 0f)
                {
                    Vector3 target = worldInput * moveSpeed * (1f + AirDamping * delta);
                    horizontal = Vector3.MoveTowards(horizontal, target, 2.4f * delta);
                }
                horizontal = Vector3.ClampMagnitude(horizontal, moveSpeed * (1f + AirDamping * delta));
            }
            velocity.x = horizontal.x;
            velocity.z = horizontal.z;
            velocity.y = Mathf.Max(velocity.y, -.65f);
            locomotor.Velocity = velocity;
            if (locomotor.IsGrounded && velocity.y <= 0f && worldInput.sqrMagnitude > 0f)
                locomotor.HandleLocomotionEvent(new LocomotionEvent(GetInstanceID(), worldInput,
                    LocomotionEvent.TranslationType.Velocity));
        }

        private void BrakeMotion(float delta)
        {
            // Compensate for the gravity step which the locomotor applies after input.
            float descent = Mathf.MoveTowards(Mathf.Min(locomotor.Velocity.y, 0f), -BrakeDescentSpeed, .8f * delta);
            descent = Mathf.Max(descent, -BrakeDescentSpeed);
            locomotor.Velocity = new Vector3(0f, descent + Mathf.Abs(Physics.gravity.y) * gravityFactor * delta, 0f);
            if (locomotor.IsGrounded) locomotor.Velocity = Vector3.zero;
            boostActive = false;
        }

        public void CancelMotion()
        {
            if (locomotor == null) return;
            boostActive = false;
            boostWasHeld = true;
            BrakeHeld = false;
            // Cancel upward impulses and horizontal coast, while retaining a gentle natural landing.
            locomotor.Velocity = new Vector3(0f, Mathf.Clamp(locomotor.Velocity.y, -BrakeDescentSpeed, 0f), 0f);
        }

        private void OnLocomotion(LocomotionEvent movement, Pose delta)
        {
            if (movement.Translation != LocomotionEvent.TranslationType.Absolute &&
                movement.Translation != LocomotionEvent.TranslationType.AbsoluteEyeLevel) return;
            CancelMotion();
            locomotor.Velocity = Vector3.zero;
        }

        private void OnDisable()
        {
            if (!initialized) return;
            CancelMotion();
            locomotor.WhenLocomotionEventHandled -= OnLocomotion;
            locomotor.GravityFactor = oldGravity;
            locomotor.GroundDamping = oldGroundDamping;
            locomotor.AirDamping = oldAirDamping;
            locomotor.JumpDamping = oldJumpDamping;
            locomotor.JumpForce = oldJumpForce;
            locomotor.SpeedFactor = oldSpeed;
            initialized = false;
        }
    }
}
