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
        [SerializeField] private Light socketLight;
        [SerializeField] private Light hatchLight;
        [SerializeField] private Renderer socketIndicator;
        [SerializeField] private Material standbyMaterial;
        [SerializeField] private Material successMaterial;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private AudioSource humSource;
        [SerializeField] private AudioSource cueSource;

        private Vector3 coreStartPosition;
        private Quaternion coreStartRotation;
        private Vector3 hatchClosedPosition;
        private bool hasBeenGrabbed;
        private bool inserted;
        private bool escaped;
        private bool exitArmed;
        private Coroutine hatchMotion;
        private GrabInteractable[] controllerGrabTargets;
        private HandGrabInteractable[] handGrabTargets;

        public bool IsInserted => inserted;
        public bool IsEscaped => escaped;

        private void Awake()
        {
            coreStartPosition = coreBody.position;
            coreStartRotation = coreBody.rotation;
            hatchClosedPosition = hatch.localPosition;
            controllerGrabTargets = coreGrab.GetComponentsInChildren<GrabInteractable>(true);
            handGrabTargets = coreGrab.GetComponentsInChildren<HandGrabInteractable>(true);
        }

        private void Start()
        {
            ConfigureAudio();
            ApplyStandby();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame ||
                OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch))
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
            else if (!escaped && playerHead != null && exitMarker != null)
            {
                var player = playerHead.position;
                var exit = exitMarker.position;
                float exitDistance = Vector2.Distance(new Vector2(player.x, player.z), new Vector2(exit.x, exit.z));
                if (exitDistance > 1.1f) exitArmed = true;
                if (exitArmed && exitDistance < 0.75f)
                {
                    escaped = true;
                    SetStatus("MISSION COMPLETE\nB / R TO REPLAY");
                    PlayCue(1046f, 1318f, 0.55f);
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
            SetStatus("POWER RESTORED\nHATCH OPEN - EXIT");
            PlayCue(659f, 988f, 0.5f);
            StartCoroutine(PulseControllers());
            hatchMotion = StartCoroutine(MoveHatch(hatchClosedPosition + Vector3.up * 2.5f));
        }

        [ContextMenu("Reset Solo Mission")]
        public void ResetMission()
        {
            if (inserted && playerHead != null && playerHead.position.x >= 3.0f)
            {
                SetStatus("RETURN THROUGH HATCH\nTHEN PRESS B / R");
                return;
            }
            if (coreGrab.GrabPoints.Count > 0)
            {
                SetStatus("RELEASE CORE\nTHEN PRESS B / R");
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
            socketIndicator.sharedMaterial = standbyMaterial;
            socketLight.color = new Color(1f, 0.46f, 0.10f);
            hatchLight.color = new Color(0.12f, 0.40f, 1f);
            SetStatus("STATION OFFLINE\nGRAB THE CORE");
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
            hatchMotion = null;
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
            if (statusText != null) statusText.text = message;
        }

        private void ConfigureAudio()
        {
            humSource.clip = CreateTone("Socket Hum", 110f, 220f, 1f, 0.045f, true);
            humSource.loop = true;
            humSource.spatialBlend = 1f;
            humSource.minDistance = 0.35f;
            humSource.maxDistance = 5f;
            humSource.Play();
            cueSource.spatialBlend = 1f;
            cueSource.minDistance = 0.5f;
            cueSource.maxDistance = 7f;
        }

        private void PlayCue(float low, float high, float seconds)
        {
            cueSource.PlayOneShot(CreateTone("Station Cue", low, high, seconds, 0.3f, false));
        }

        private static AudioClip CreateTone(string name, float low, float high,
            float seconds, float volume, bool seamless)
        {
            const int sampleRate = 22050;
            int count = Mathf.CeilToInt(sampleRate * seconds);
            var samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float time = i / (float)sampleRate;
                float envelope = seamless ? 1f : Mathf.Sin(Mathf.PI * i / (count - 1));
                samples[i] = volume * envelope *
                    (Mathf.Sin(2f * Mathf.PI * low * time) * 0.72f +
                     Mathf.Sin(2f * Mathf.PI * high * time) * 0.28f);
            }
            var clip = AudioClip.Create(name, count, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
