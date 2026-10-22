#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CourseProject;
using Oculus.Interaction;
using Oculus.Interaction.Locomotion;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using MetaCharacter = Oculus.Interaction.Locomotion.CharacterController;

namespace CourseSetup
{
    /// <summary>Deterministic input + real scene physics. Not a live XR/Quest acceptance test.</summary>
    public static class StationControlsValidation
    {
        private static readonly List<string> checks = new List<string>();
        private static SpaceStationControls controls;
        private static FirstPersonLocomotor locomotor;
        private static MetaCharacter character;
        private static Transform origin, eyes, inputHead;
        private static float dt;
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;

        [MenuItem("Course/Controls/Validate Controls And Scene Collision")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play first.");
            checks.Clear();
            string error = null;
            try
            {
                var scene = EditorSceneManager.OpenScene(Week06TeamUIBuilder.ScenePath);
                var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<MonoBehaviour>(true)).ToArray();
                controls = all.OfType<SpaceStationControls>().Single();
                locomotor = controls.Locomotor;
                character = Get<MetaCharacter>(locomotor, "_characterController");
                origin = Get<Transform>(locomotor, "_playerOrigin");
                eyes = Get<Transform>(locomotor, "_playerEyes");
                inputHead = Get<Transform>(controls, "head");
                Check(all.OfType<FirstPersonLocomotor>().Count() == 1, "Exactly one Meta locomotor");
                Check((character.LayerMask.value & 1) != 0, "Default-layer floor, walls and hatch included in collision mask");
                Check(!all.Any(b => b != null && b.enabled && (b is SlideLocomotionBroadcaster ||
                    b is StepLocomotionBroadcaster || b is TurnerEventBroadcaster || b is TurnLocomotionBroadcaster ||
                    b is LocomotionActionsBroadcaster || b is MovingSetting || b is TurningSetting)), "Competing sample locomotion inputs disabled");
                var teleports = all.OfType<TeleportInteractor>().Where(t => t.name == "TeleportControllerInteractor").ToArray();
                Check(teleports.Length == 2 && teleports.Count(t => t.gameObject.activeSelf) == 1 &&
                    controls.RightTeleport.activeInHierarchy, "Only right controller teleport active");
                Check(all.OfType<EventSystem>().Count() == 1 && all.OfType<PointableCanvasModule>().Count() == 1,
                    "One EventSystem and one Meta canvas module");
                Check(controls.MenuPanel.GetComponentInChildren<PointableCanvas>(true) != null &&
                    controls.MenuPanel.GetComponentsInChildren<Button>(true).All(b => b.onClick.GetPersistentEventCount() == 1),
                    "Controls menu has Meta ray canvas and wired resume/reset buttons");
                Check(all.OfType<SpaceStationMission>().Single().GetComponentInChildren<SpaceStationMissionConsole>(true) != null &&
                    all.OfType<GrabInteractable>().Any(g => g.enabled && g.gameObject.activeInHierarchy), "Mission console and controller grab retained");
                Invoke(controls, "Awake");
                locomotor.SetDeltaTimeProvider(() => dt);
                locomotor.SetTimeProvider(() => 1f);
                Check(SpaceStationControls.ApplyDeadzone(new Vector2(.1f,.1f),.2f) == Vector2.zero, "Stick drift deadzone");
                Check(Mathf.Abs(SpaceStationControls.ApplyDeadzone(Vector2.one,.2f).magnitude-1) < .001f,
                    "Diagonal movement normalized");

                float d60 = DistanceAtRate(60), d120 = DistanceAtRate(120);
                Check(d60 > 2.85f && d60 < 3.1f && Mathf.Abs(d60-d120) < .04f,
                    $"Forward speed ~1.5 m/s; two-second distance 60 Hz={d60:F3}, 120 Hz={d120:F3}");
                Reset(new Vector3(-2,0,-1), 90);
                Vector3 start = eyes.position;
                RunFrames(Vector2.up, Vector2.zero, 60);
                Check(eyes.position.x > start.x + 1.3f && Mathf.Abs(eyes.position.z-start.z) < .05f,
                    "Head-relative forward movement");
                Reset(new Vector3(-2,0,-1), 0);
                start = eyes.position;
                RunFrames(Vector2.left, Vector2.zero, 60);
                Check(eyes.position.x < start.x-1.3f && Mathf.Abs(eyes.position.z-start.z) < .05f,
                    "Left stick horizontal axis strafes rather than turns");
                Reset(new Vector3(-2,0,-1), 0);
                RunFrames(Vector2.down, Vector2.zero, 30);
                Check(eyes.position.z < -1.6f, "Left stick backward movement");

                Reset(new Vector3(-2,0,-1), 0);
                inputHead.rotation = Quaternion.Euler(-90,0,0);
                RunFrames(Vector2.up,Vector2.zero,30);
                Check(float.IsFinite(origin.position.x) && eyes.position.z > -.5f && origin.position.y < .1f,
                    "Vertical gaze remains horizontal and finite");

                Reset(new Vector3(-2,0,-1), 0);
                start = eyes.position;
                RunFrames(Vector2.zero,Vector2.right,30);
                Check(Mathf.Abs(Mathf.DeltaAngle(0,origin.eulerAngles.y)-30) < .1f &&
                    Vector2.Distance(new Vector2(start.x,start.z), new Vector2(eyes.position.x,eyes.position.z)) < .06f,
                    "Held right stick turns only once, around head pivot");
                Tick(Vector2.zero,Vector2.zero);
                Tick(Vector2.zero,Vector2.right);
                Check(Mathf.Abs(Mathf.DeltaAngle(0,origin.eulerAngles.y)-60) < .1f, "Recenter rearms snap turn");

                Reset(new Vector3(-2,0,-1), 0);
                start = eyes.position;
                RunFrames(Vector2.up,new Vector2(.8f,.8f),60);
                Check(Vector3.Distance(start,eyes.position) < .02f && Mathf.Abs(origin.eulerAngles.y)<.1f,
                    "Teleport aiming suppresses walk and snap turn");
                controls.ProcessInput(Vector2.up,Vector2.right,false);
                RunFrames(Vector2.up,Vector2.right,30);
                Check(Vector3.Distance(start,eyes.position)<.02f && !controls.RightTeleport.activeSelf,
                    "Lost focus stops movement and requires neutral input before resuming");
                Tick(Vector2.zero,Vector2.zero);
                controls.ToggleMenu();
                RunFrames(Vector2.up,Vector2.right,30);
                Check(controls.MenuVisible && controls.MenuPanel.activeSelf && !controls.RightTeleport.activeSelf &&
                    Vector3.Distance(start,eyes.position)<.02f, "Menu blocks walking, turning and teleport");
                controls.CloseMenu();
                RunFrames(Vector2.up,Vector2.right,30);
                Check(Vector3.Distance(start,eyes.position)<.02f, "Closing menu does not apply held sticks");
                Tick(Vector2.zero,Vector2.zero);
                Check(controls.RightTeleport.activeSelf, "Neutral sticks restore right teleport");

                Reset(new Vector3(-2,0,2.6f), 0);
                RunFrames(Vector2.up,Vector2.zero,180);
                Check(eyes.position.z < 3.8f && eyes.position.z > 3f, "Front wall blocks sustained walking");
                Reset(new Vector3(1.2f,0,.1f), 0);
                RunFrames(Vector2.up,Vector2.zero,120);
                Check(eyes.position.z < .95f && eyes.position.z > .2f, "Workbench blocks walking");
                var hardware = all.OfType<SpaceStationHardware>().Single();
                hardware.ResetHardware();
                Reset(new Vector3(2,0,.75f),90);
                RunFrames(Vector2.up,Vector2.zero,120);
                Check(eyes.position.x < 3.1f && eyes.position.x > 2.5f, "Closed hatch blocks passage");
                hardware.ApplySequenceTime(SpaceStationHardware.SequenceDuration);
                Physics.SyncTransforms();
                RunFrames(Vector2.up,Vector2.zero,90);
                Check(eyes.position.x > 3.8f && !hardware.PassageBlocked, "Fully opened hatch permits exit traversal");
                hardware.ResetHardware();
                Reset(new Vector3(2,0,.75f),90);
                RunFrames(Vector2.up,Vector2.zero,120);
                Check(eyes.position.x < 3.1f && hardware.PassageBlocked, "Reset restores hatch collision");

                Reset(new Vector3(-2,1,-1), 0, false);
                RunFrames(Vector2.up,Vector2.zero,60);
                Check(origin.position.y < .05f && character.IsGrounded, "Gravity lands player while movement input is held");
                Reset(new Vector3(-2,0,-1),0);
                RunFrames(Vector2.up,Vector2.zero,60);
                start = eyes.position;
                RunFrames(Vector2.zero,Vector2.zero,30);
                Check(Vector3.Distance(start,eyes.position)<.06f && locomotor.Velocity.magnitude<.001f,
                    "Releasing left stick decelerates to rest within six centimetres");
            }
            catch (Exception ex) { error = ex.ToString(); Debug.LogError(error); }
            finally
            {
                Directory.CreateDirectory("CourseValidation/Runtime");
                File.WriteAllText("CourseValidation/Runtime/StationControls_Validation.json", JsonUtility.ToJson(new Report {
                    generatedUtc = DateTime.UtcNow.ToString("O"), passed = error == null,
                    checks = checks.ToArray(), error = error
                },true));
                // Never persist test poses or temporarily opened hardware.
                EditorSceneManager.OpenScene(Week06TeamUIBuilder.ScenePath);
            }
            Debug.Log("STATION_CONTROLS_VALIDATION_"+(error == null ? "PASS" : "FAIL")+" | checks="+checks.Count);
            if (Application.isBatchMode) EditorApplication.Exit(error == null ? 0 : 1);
        }

        private static float DistanceAtRate(int rate)
        {
            Reset(new Vector3(-2,0,-1.5f),0); dt=1f/rate;
            Vector3 start=eyes.position;
            RunFrames(Vector2.up,Vector2.zero,rate*2);
            return Vector3.Distance(start,eyes.position);
        }
        private static void Reset(Vector3 feet, float yaw, bool ground = true)
        {
            dt=1f/60;
            controls.CloseMenu();
            origin.SetPositionAndRotation(feet,Quaternion.Euler(0,yaw,0));
            eyes.SetPositionAndRotation(feet+Vector3.up*1.65f,origin.rotation);
            if (inputHead != eyes) inputHead.SetPositionAndRotation(eyes.position,eyes.rotation);
            character.SetPosition(feet + Vector3.up*(character.Height*.5f + character.SkinWidth));
            character.SetRotation(origin.rotation);
            locomotor.Velocity=Vector3.zero;
            typeof(FirstPersonLocomotor).GetField("_accumulatedDeltaFrame",Flags).SetValue(locomotor,Pose.identity);
            Physics.SyncTransforms();
            if (ground) character.TryGround(1f);
            Tick(Vector2.zero,Vector2.zero);
        }
        private static void RunFrames(Vector2 left,Vector2 right,int frames)
        { for(int i=0;i<frames;i++) Tick(left,right); }
        private static void Tick(Vector2 left,Vector2 right)
        {
            controls.ProcessInput(left,right,true);
            Invoke(locomotor,"Update"); Invoke(locomotor,"LateUpdate"); Invoke(locomotor,"LastUpdate");
            Physics.SyncTransforms();
        }
        private static void Check(bool value,string message)
        { if(!value) throw new InvalidOperationException("FAILED: "+message); checks.Add(message); }
        private static T Get<T>(Object obj,string field) where T:Object =>
            (T)new SerializedObject(obj).FindProperty(field).objectReferenceValue;
        private static void Invoke(object obj,string method) => obj.GetType().GetMethod(method,Flags).Invoke(obj,null);
        [Serializable] private class Report
        {
            public string generatedUtc;
            public string scope="Unity Editor deterministic input through production controls and Meta locomotor, real saved scene colliders; no XR session, controller-ray click, physical Quest input or human comfort test.";
            public bool passed;
            public string[] checks;
            public string error;
        }
    }
}
#endif
