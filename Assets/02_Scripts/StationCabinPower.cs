using System;
using UnityEngine;

namespace CourseProject
{
    /// <summary>Three local lighting circuits respond to the existing core socket.</summary>
    [DisallowMultipleComponent]
    public sealed class StationCabinPower : MonoBehaviour
    {
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        private static readonly int Emission = Shader.PropertyToID("_EmissionColor");
        private static readonly Color StandbyLens = new Color(.38f, .16f, .045f);
        private static readonly Color StandbyEmission = new Color(.42f, .12f, .025f);
        private static readonly Color RunningLens = new Color(.83f, .94f, 1f);
        private static readonly Color RunningEmission = new Color(.86f, 1.24f, 1.55f);
        private static readonly Color StandbyFill = new Color(1f, .56f, .30f);
        private const float StandbyIntensityFraction = .14f;
        public const float RestoreSeconds = 2.4f;

        [Serializable]
        public sealed class Bank
        {
            [SerializeField] private Light fill;
            [SerializeField] private Renderer[] fixtures;
            [SerializeField] private Color runningFillColor;
            [SerializeField] private float runningIntensity;

            [NonSerialized] private Material runtimeLens;
            [NonSerialized] private Material[] originalMaterials;
#if UNITY_EDITOR
            [NonSerialized] private MaterialPropertyBlock previewProperties;
#endif

            // Pass the light with its restored color and intensity already authored.
            // Fixtures in one circuit are single-material diffusers sharing one source material.
            public Bank(Light fillLight, Renderer[] lensRenderers)
            {
                fill = fillLight;
                fixtures = lensRenderers ?? Array.Empty<Renderer>();
                if (fill == null) return;
                runningFillColor = fill.color;
                runningIntensity = fill.intensity;
                fill.shadows = LightShadows.None;
            }

            internal void CreateRuntimeLens()
            {
                if (fixtures == null || runtimeLens != null) return;
                originalMaterials = new Material[fixtures.Length];
                for (int i = 0; i < fixtures.Length; i++)
                {
                    Renderer lens = fixtures[i];
                    if (lens == null || lens.sharedMaterial == null) continue;
                    originalMaterials[i] = lens.sharedMaterial;
                    if (runtimeLens == null)
                    {
                        runtimeLens = new Material(lens.sharedMaterial) { hideFlags = HideFlags.DontSave };
                        if (runtimeLens.HasProperty(Emission)) runtimeLens.EnableKeyword("_EMISSION");
                    }
                    lens.SetPropertyBlock(null);
                    lens.sharedMaterial = runtimeLens;
                }
            }

            internal void Apply(float level)
            {
                if (fill != null)
                {
                    fill.color = Color.Lerp(StandbyFill, runningFillColor, level);
                    fill.intensity = runningIntensity * Mathf.Lerp(StandbyIntensityFraction, 1f, level);
                }
                Color surface = Color.Lerp(StandbyLens, RunningLens, level);
                Color glow = Color.Lerp(StandbyEmission, RunningEmission, level);
                if (runtimeLens != null)
                {
                    if (runtimeLens.HasProperty(BaseColor)) runtimeLens.SetColor(BaseColor, surface);
                    if (runtimeLens.HasProperty(Emission)) runtimeLens.SetColor(Emission, glow);
                }
#if UNITY_EDITOR
                else if (!Application.isPlaying && fixtures != null)
                {
                    // A preview must not dirty or replace the source material asset.
                    previewProperties ??= new MaterialPropertyBlock();
                    previewProperties.SetColor(BaseColor, surface);
                    previewProperties.SetColor(Emission, glow);
                    foreach (Renderer lens in fixtures)
                        if (lens != null) lens.SetPropertyBlock(previewProperties);
                }
#endif
            }

            internal void ReleaseRuntimeLens()
            {
                if (runtimeLens == null) return;
                for (int i = 0; i < fixtures.Length; i++)
                    if (fixtures[i] != null && fixtures[i].sharedMaterial == runtimeLens)
                        fixtures[i].sharedMaterial = originalMaterials[i];
                UnityEngine.Object.Destroy(runtimeLens);
                runtimeLens = null;
                originalMaterials = null;
            }
        }

        [SerializeField] private SpaceStationMission mission;
        [SerializeField] private Bank[] banks = Array.Empty<Bank>();
        public float Progress { get; private set; }

        // The three arguments specify the physical direction of the recovery wave.
        public void Configure(SpaceStationMission task, Bank coupler, Bank spine, Bank bay)
        {
            if (Application.isPlaying)
                foreach (Bank bank in banks) bank?.ReleaseRuntimeLens();
            mission = task;
            banks = new[] { coupler, spine, bay };
            if (Application.isPlaying && isActiveAndEnabled) BindRuntime();
            else ApplyProgress(0f);
        }

        private void OnEnable()
        {
            if (!Application.isPlaying || mission == null) return;
            BindRuntime();
        }

        private void BindRuntime()
        {
            foreach (Bank bank in banks) bank?.CreateRuntimeLens();
            ApplyProgress(mission != null && mission.IsInserted ? 1f : 0f);
        }

        private void LateUpdate()
        {
            if (mission == null) return;
            float next = mission.IsInserted
                ? Mathf.MoveTowards(Progress, 1f, Time.deltaTime / RestoreSeconds)
                : 0f;
            if (next != Progress) ApplyProgress(next);
        }

        public float GetBankPower(int index)
        {
            if ((uint)index >= (uint)banks.Length) throw new ArgumentOutOfRangeException(nameof(index));
            float local = Mathf.Clamp01((Progress - .25f * index) * 2f);
            return Mathf.SmoothStep(0f, 1f, local);
        }

#if UNITY_EDITOR
        public void SetPreviewProgress(float normalizedProgress)
        {
            if (Application.isPlaying)
                throw new InvalidOperationException("In Play mode, dock or reset the core to change cabin power.");
            ApplyProgress(Mathf.Clamp01(normalizedProgress));
        }
#endif

        private void ApplyProgress(float normalized)
        {
            Progress = normalized;
            for (int i = 0; i < banks.Length; i++)
                banks[i]?.Apply(GetBankPower(i));
        }

        private void OnDisable()
        {
            if (!Application.isPlaying) return;
            ApplyProgress(0f);
            foreach (Bank bank in banks) bank?.ReleaseRuntimeLens();
        }
    }
}
