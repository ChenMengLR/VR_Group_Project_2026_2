using Oculus.Interaction;
using Oculus.Interaction.Locomotion;
using TMPro;
using UnityEngine;

namespace CourseProject
{
    /// <summary>Wayfinding and recovery for the expanded station; reuses the original mission.</summary>
    public sealed class SpaceStationRoute : MonoBehaviour
    {
        [SerializeField] private SpaceStationMission mission;
        [SerializeField] private SpaceStationControls controls;
        [SerializeField] private Rigidbody core;
        [SerializeField] private Grabbable grab;
        [SerializeField] private Transform startAnchor;
        [SerializeField] private BoxCollider exitVolume;
        [SerializeField] private Renderer[] outboundLights;
        [SerializeField] private Renderer[] returnLights;
        [SerializeField] private Material cyan;
        [SerializeField] private Material amber;
        [SerializeField] private Material green;
        [SerializeField] private Material dim;
        [SerializeField] private TMP_Text routeSign;
        private int lastStage = -1;
        public Transform StartAnchor => startAnchor;
        public BoxCollider ExitVolume => exitVolume;
        public Rigidbody Core => core;

        public void Configure(SpaceStationMission task, SpaceStationControls input, Rigidbody body,
            Transform spawn, BoxCollider exit, Renderer[] outbound, Renderer[] inbound,
            Material blue, Material orange, Material ready, Material off, TMP_Text sign)
        {
            mission = task; controls = input; core = body; grab = body.GetComponent<Grabbable>();
            startAnchor = spawn; exitVolume = exit; outboundLights = outbound; returnLights = inbound;
            cyan = blue; amber = orange; green = ready; dim = off; routeSign = sign;
        }

        private void Update()
        {
            if (!mission.IsInserted && grab.GrabPoints.Count == 0 && IsCoreOutsideReachableArea())
                mission.RecoverLostCore();
            int stage = mission.IsHatchOpen ? 5 : mission.IsHatchOpening ? 4 :
                mission.RequiresLever && mission.IsPowerReady ? 3 : mission.IsInserted ? 2 : mission.HasAcquiredCore ? 1 : 0;
            if (lastStage == stage) return;
            lastStage = stage;
            foreach (var lamp in outboundLights) lamp.sharedMaterial = stage == 0 ? cyan : dim;
            foreach (var lamp in returnLights) lamp.sharedMaterial = stage == 5 ? green : stage >= 1 ? amber : dim;
            routeSign.text = stage == 0 ? "01 / RECOVER\nFOLLOW BLUE TO UPPER BAY" :
                stage == 1 ? "02 / RESTORE\nFOLLOW AMBER TO COUPLER" :
                stage == 2 ? "POWER LINKING\nWAIT FOR LEVER LIGHT" :
                stage == 3 ? "03 / RELEASE\nPULL AIRLOCK LEVER" :
                stage == 4 ? "AIRLOCK OPENING\nPLEASE WAIT" : "04 / EVACUATE\nGREEN AIRLOCK IS OPEN";
        }

        public bool IsCoreOutsideReachableArea()
        {
            Vector3 p = core.position;
            return p.y < -.7f || p.y > 5.5f || p.x < -12.6f || p.x > 6f || p.z < -2.6f || p.z > 8.1f;
        }

        public bool IsInsideExit(Vector3 headPosition) => exitVolume.bounds.Contains(headPosition);

        public void ReturnPlayerToStart()
        {
            controls.CloseMenu();
            controls.StopMotion();
            var locomotor = controls.Locomotor;
            locomotor.Velocity = Vector3.zero;
            locomotor.HandleLocomotionEvent(new LocomotionEvent(GetInstanceID(),
                startAnchor.position, LocomotionEvent.TranslationType.Absolute));
            locomotor.HandleLocomotionEvent(new LocomotionEvent(GetInstanceID(),
                startAnchor.rotation, LocomotionEvent.RotationType.Absolute));
        }
    }
}
