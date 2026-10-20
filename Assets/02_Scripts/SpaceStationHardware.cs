using System;
using System.Collections;
using TMPro;
using UnityEngine;

namespace CourseProject
{
    /// <summary>Matched core coupler and interlocked, split-leaf airlock hardware.</summary>
    public sealed class SpaceStationHardware : MonoBehaviour
    {
        public const float SequenceDuration = 2.65f;
        [SerializeField] private Transform leftLeaf;
        [SerializeField] private Transform rightLeaf;
        [SerializeField] private Transform[] locks;
        [SerializeField] private Transform[] clamps;
        [SerializeField] private Vector3[] clampDirections;
        [SerializeField] private Renderer[] socketSegments;
        [SerializeField] private Renderer[] doorStrips;
        [SerializeField] private Material amber;
        [SerializeField] private Material cyan;
        [SerializeField] private Material green;
        [SerializeField] private BoxCollider passageBlocker;
        [SerializeField] private Transform coreRotor;
        [SerializeField] private TMP_Text hatchLabel;
        [SerializeField] private AudioSource mechanismAudio;

        private Vector3 leftClosed;
        private Vector3 rightClosed;
        private Vector3[] lockClosed;
        private Vector3[] clampOpen;
        private Quaternion rotorRest;
        private bool cached;
        private bool powered;

        public bool IsOpen { get; private set; }
        public float OpenFraction { get; private set; }
        public bool PassageBlocked => passageBlocker != null && passageBlocker.enabled;
        public Transform LeftLeaf => leftLeaf;
        public Transform RightLeaf => rightLeaf;
        public bool IsConfigured => leftLeaf != null && rightLeaf != null &&
            locks != null && locks.Length == 4 && clamps != null && clamps.Length == 3 &&
            socketSegments != null && socketSegments.Length == 12 &&
            doorStrips != null && doorStrips.Length > 0 && passageBlocker != null &&
            coreRotor != null && mechanismAudio != null && mechanismAudio.clip != null;

        public void Configure(Transform left, Transform right, Transform[] latchParts,
            Transform[] clampParts, Vector3[] inwardDirections, Renderer[] socketLeds,
            Renderer[] hatchLeds, Material standby, Material energised, Material ready,
            BoxCollider blocker, Transform rotor, TMP_Text label, AudioSource audio)
        {
            leftLeaf = left;
            rightLeaf = right;
            locks = latchParts;
            clamps = clampParts;
            clampDirections = inwardDirections;
            socketSegments = socketLeds;
            doorStrips = hatchLeds;
            amber = standby;
            cyan = energised;
            green = ready;
            passageBlocker = blocker;
            coreRotor = rotor;
            hatchLabel = label;
            mechanismAudio = audio;
        }

        private void Awake() => CacheRestPose();

        private void Update()
        {
            if (coreRotor != null)
                coreRotor.Rotate(Vector3.up, (powered ? 90f : 22f) * Time.deltaTime, Space.Self);
        }

        private void CacheRestPose()
        {
            if (cached || leftLeaf == null || rightLeaf == null) return;
            leftClosed = leftLeaf.localPosition;
            rightClosed = rightLeaf.localPosition;
            lockClosed = new Vector3[locks.Length];
            clampOpen = new Vector3[clamps.Length];
            for (int i = 0; i < locks.Length; i++) lockClosed[i] = locks[i].localPosition;
            for (int i = 0; i < clamps.Length; i++) clampOpen[i] = clamps[i].localPosition;
            rotorRest = coreRotor.localRotation;
            cached = true;
        }

        public IEnumerator OpenHatch(Action<string> setMissionStatus)
        {
            CacheRestPose();
            powered = true;
            mechanismAudio.Play();
            int previousStage = -1;
            for (float elapsed = 0; elapsed < SequenceDuration; elapsed += Time.deltaTime)
            {
                int stage = elapsed < 0.45f ? 0 : elapsed < 0.85f ? 1 : 2;
                if (stage != previousStage)
                {
                    setMissionStatus(stage == 0 ? "POWER RESTORED\nSECURING CORE" :
                        stage == 1 ? "POWER RESTORED\nUNLOCKING HATCH" : "HATCH OPENING\nPLEASE WAIT");
                    Debug.Log("STATION_HARDWARE_STAGE: " + stage);
                    previousStage = stage;
                }
                ApplySequenceTime(elapsed);
                yield return null;
            }
            ApplySequenceTime(SequenceDuration);
            Debug.Log("STATION_HARDWARE_OPEN: passage collider released");
        }

        /// <summary>Deterministic pose also used by the Editor's labelled previews.</summary>
        public void ApplySequenceTime(float seconds)
        {
            CacheRestPose();
            float clamp = Ease(seconds / 0.45f);
            float unlock = Ease((seconds - 0.45f) / 0.4f);
            float unseal = Ease((seconds - 0.85f) / 0.2f);
            OpenFraction = Ease((seconds - 1.05f) / 1.6f);
            for (int i = 0; i < clamps.Length; i++)
                clamps[i].localPosition = clampOpen[i] + clampDirections[i] * (0.058f * clamp);
            for (int i = 0; i < locks.Length; i++)
                locks[i].localPosition = lockClosed[i] +
                    new Vector3(0.10f, 0f, i % 2 == 0 ? -0.13f : 0.13f) * unlock;
            leftLeaf.localPosition = leftClosed + new Vector3(0.045f * unseal, 0, -1.02f * OpenFraction);
            rightLeaf.localPosition = rightClosed + new Vector3(0.045f * unseal, 0, 1.02f * OpenFraction);
            IsOpen = seconds >= SequenceDuration;
            passageBlocker.enabled = !IsOpen;
            for (int i = 0; i < socketSegments.Length; i++)
                socketSegments[i].sharedMaterial = IsOpen ? green :
                    (i < Mathf.CeilToInt(clamp * socketSegments.Length) ? cyan : amber);
            foreach (var strip in doorStrips)
                strip.sharedMaterial = IsOpen ? green : seconds >= 0.45f ? cyan : amber;
            hatchLabel.text = IsOpen ? "AIRLOCK 07  /  OPEN" :
                seconds >= 0.85f ? "AIRLOCK 07  /  OPENING" :
                seconds >= 0.45f ? "AIRLOCK 07  /  UNLOCKING" : "AIRLOCK 07  /  POWER LINK";
        }

        public void ResetHardware()
        {
            CacheRestPose();
            if (!cached) return;
            powered = false;
            IsOpen = false;
            OpenFraction = 0;
            leftLeaf.localPosition = leftClosed;
            rightLeaf.localPosition = rightClosed;
            for (int i = 0; i < locks.Length; i++) locks[i].localPosition = lockClosed[i];
            for (int i = 0; i < clamps.Length; i++) clamps[i].localPosition = clampOpen[i];
            coreRotor.localRotation = rotorRest;
            passageBlocker.enabled = true;
            foreach (var segment in socketSegments) segment.sharedMaterial = amber;
            foreach (var strip in doorStrips) strip.sharedMaterial = amber;
            hatchLabel.text = "AIRLOCK 07  /  SEALED";
            if (mechanismAudio != null) mechanismAudio.Stop();
        }

        private static float Ease(float value) => Mathf.SmoothStep(0, 1, Mathf.Clamp01(value));
    }
}
