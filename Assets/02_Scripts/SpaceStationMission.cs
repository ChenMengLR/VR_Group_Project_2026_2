using System.Collections;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CourseProject
{
    /// <summary>
    /// One-room solo mission: grab the energy core, release it in the socket,
    /// then walk through the opened hatch. The scene builder assigns references.
    /// </summary>
    public sealed class SpaceStationMission : MonoBehaviour
    {
        [Header("Interaction")]
        [SerializeField] private Grabbable coreGrab;
        [SerializeField] private Rigidbody coreBody;
        [SerializeField] private Transform socket;
        [SerializeField] private Transform playerHead;
        [SerializeField] private Transform exitMarker;

        [Header("Feedback")]
        [SerializeField] private Transform hatch;
        [SerializeField] private SpaceStationHardware hardware;
        [SerializeField] private Light socketLight;
        [SerializeField] private Light hatchLight;
        [SerializeField] private Renderer socketIndicator;
        [SerializeField] private Material standbyMaterial;
        [SerializeField] private Material successMaterial;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private AudioSource humSource;
        [SerializeField] private AudioSource cueSource;
        [SerializeField] private AudioClip powerRestoredClip;
        [SerializeField] private AudioSource exitCueSource;
        [SerializeField] private AudioClip missionCompleteClip;

        private Vector3 coreStartPosition;
        private Quaternion coreStartRotation;
        private Vector3 hatchClosedPosition;
        private bool hasBeenGrabbed;
        private bool inserted;
        private bool escaped;
        private bool exitArmed;
        private bool resetPending;
        private bool hatchOpen;
        private bool legacyControllerReset;
        private string currentStatus;
        private Coroutine hatchMotion;
        private GrabInteractable[] controllerGrabTargets;
        private HandGrabInteractable[] handGrabTargets;

        public bool IsInserted => inserted;
        public bool IsEscaped => escaped;
        public bool IsHatchOpen => hatchOpen;
        public string CurrentStatus => currentStatus;
        public AudioSource HumSource => humSource;
        public AudioSource CueSource => cueSource;
        public AudioClip PowerRestoredClip => powerRestoredClip;
        public AudioSource ExitCueSource => exitCueSource;
        public AudioClip MissionCompleteClip => missionCompleteClip;

        private void Awake()
        {
            legacyControllerReset = GetComponent<SpaceStationControls>() == null;
            coreStartPosition = coreBody.position;
            coreStartRotation = coreBody.rotation;
            hatchClosedPosition = hatch.localPosition;
            controllerGrabTargets = coreGrab.GetComponentsInChildren<GrabInteractable>(true);
            handGrabTargets = coreGrab.GetComponentsInChildren<HandGrabInteractable>(true);
        }

        private void Start()
        {
            if (humSource != null && !humSource.isPlaying) humSource.Play();
            ApplyStandby();
        }

        private void Update()
        {
            if (resetPending)
            {
                if (coreGrab.GrabPoints.Count == 0) ResetMission();
                return;
            }
            // Current team scene uses B as Back/Close; keep archived Week 05 scenes playable.
            if ((Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) ||
                (legacyControllerReset && OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch)))
            {
                ResetMission();
                return;
            }

            if (!inserted)
            {
                if (coreGrab.GrabPoints.Count > 0)
                {
                    hasBeenGrabbed = true;
                    SetStatus("CORE ACQUIRED\nPLACE IN SOCKET");
                }

                if (hasBeenGrabbed && coreGrab.GrabPoints.Count == 0 &&
                    Vector3.Distance(coreBody.position, socket.position) <= 0.20f)
                {
                    InsertCore();
                }
            }
            else if (hatchOpen && !escaped && playerHead != null && exitMarker != null)
            {
                var player = playerHead.position;
                var exit = exitMarker.position;
                float exitDistance = Vector2.Distance(new Vector2(player.x, player.z), new Vector2(exit.x, exit.z));
                if (exitDistance > 1.1f) exitArmed = true;
                if (exitArmed && exitDistance < 0.75f)
                {
                    escaped = true;
                    SetStatus("MISSION COMPLETE\nRETURN FOR REPLAY");
                    PlayCue(exitCueSource, missionCompleteClip);
                }
            }
        }

        private void InsertCore()
        {
            inserted = true;
            coreBody.linearVelocity = Vector3.zero;
            coreBody.angularVelocity = Vector3.zero;
            coreBody.isKinematic = true;
            coreBody.position = socket.position;
            coreBody.rotation = socket.rotation;
            foreach (var target in controllerGrabTargets) target.enabled = false;
            foreach (var target in handGrabTargets) target.enabled = false;
            coreGrab.enabled = false;
            socketIndicator.sharedMaterial = successMaterial;
            socketLight.color = new Color(0.20f, 1f, 0.42f);
            hatchLight.color = socketLight.color;
            SetStatus("POWER RESTORED\nSECURING CORE");
            PlayCue(cueSource, powerRestoredClip);
            StartCoroutine(PulseControllers());
            hatchMotion = StartCoroutine(OpenHatchSequence());
        }

        [ContextMenu("Reset Solo Mission")]
        public void ResetMission()
        {
            if (inserted && playerHead != null && playerHead.position.x >= 3.0f)
            {
                SetStatus("RETURN THROUGH HATCH\nTO RESET MISSION");
                return;
            }
            if (coreGrab.GrabPoints.Count > 0)
            {
                resetPending = true;
                SetStatus("RELEASE CORE\nTO RESET MISSION");
                return;
            }
            if (hatchMotion != null)
            {
                StopCoroutine(hatchMotion);
                hatchMotion = null;
            }

            inserted = false;
            escaped = false;
            exitArmed = false;
            resetPending = false;
            hatchOpen = false;
            hasBeenGrabbed = false;
            coreGrab.enabled = true;
            foreach (var target in controllerGrabTargets) target.enabled = true;
            foreach (var target in handGrabTargets) target.enabled = true;
            coreBody.isKinematic = false;
            coreBody.linearVelocity = Vector3.zero;
            coreBody.angularVelocity = Vector3.zero;
            coreBody.position = coreStartPosition;
            coreBody.rotation = coreStartRotation;
            hatch.localPosition = hatchClosedPosition;
            ApplyStandby();
        }

        private void ApplyStandby()
        {
            if (hardware != null) hardware.ResetHardware();
            socketIndicator.sharedMaterial = standbyMaterial;
            socketLight.color = new Color(1f, 0.46f, 0.10f);
            hatchLight.color = new Color(0.12f, 0.40f, 1f);
            SetStatus("STATION OFFLINE\nGRAB THE CORE");
        }

        private IEnumerator OpenHatchSequence()
        {
            if (hardware != null)
                yield return hardware.OpenHatch(SetStatus);
            else
                yield return MoveHatch(hatchClosedPosition + Vector3.up * 2.5f);
            hatchOpen = true;
            SetStatus("HATCH OPEN\nPROCEED TO EXIT");
            hatchMotion = null;
        }

        private IEnumerator MoveHatch(Vector3 openPosition)
        {
            var from = hatchClosedPosition;
            const float duration = 1.25f;
            for (float elapsed = 0; elapsed < duration; elapsed += Time.deltaTime)
            {
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                hatch.localPosition = Vector3.LerpUnclamped(from, openPosition, t);
                yield return null;
            }
            hatch.localPosition = openPosition;
        }

        private IEnumerator PulseControllers()
        {
            OVRInput.SetControllerVibration(0.45f, 0.25f, OVRInput.Controller.LTouch);
            OVRInput.SetControllerVibration(0.45f, 0.25f, OVRInput.Controller.RTouch);
            yield return new WaitForSeconds(0.18f);
            OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.LTouch);
            OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.RTouch);
        }

        private void SetStatus(string message)
        {
            currentStatus = message;
            if (statusText != null) statusText.text = message;
        }

        private static void PlayCue(AudioSource source, AudioClip clip)
        {
            if (source != null && clip != null)
            {
                source.PlayOneShot(clip);
                Debug.Log("WEEK05_CUE_PLAYED: " + clip.name + " @ " + source.name);
            }
        }
    }
}
