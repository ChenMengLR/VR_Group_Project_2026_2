#if UNITY_EDITOR
using System;
using UnityEditor;

namespace CourseSetup
{
    /// <summary>Starts an interactive simulator session with the official Meta Operator.</summary>
    [InitializeOnLoad]
    public static class Week05OperatorSession
    {
        private const string PendingKey = "CourseSetup.Week05OperatorSession.Pending";

        static Week05OperatorSession()
        {
            EditorApplication.update += EnterPlayWhenReady;
        }

        [MenuItem("Course/Week 05/Start Simulator Sound Flow")]
        public static void Start()
        {
            SessionState.SetBool(PendingKey, false);
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.StandaloneWindows64)
                throw new InvalidOperationException("Switch active build target to Windows before starting the Meta XR Simulator Operator.");
            UnityEngine.Debug.Log("WEEK05_OPERATOR_BUILD_TARGET=" + EditorUserBuildSettings.activeBuildTarget);
            Week03SimulatorSetup.Prepare();
            CourseLauncher.OpenSoloStation();
            SessionState.SetBool(PendingKey, true);
            // executeMethod may finish before the first delayed Editor callback.
            // Request Play immediately; Unity carries that request through reload.
            if (!EditorApplication.isCompiling)
            {
                SessionState.SetBool(PendingKey, false);
                EditorApplication.isPlaying = true;
            }
        }

        private static void EnterPlayWhenReady()
        {
            if (!SessionState.GetBool(PendingKey, false) ||
                EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            SessionState.SetBool(PendingKey, false);
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Week 05 Operator session must start outside Play mode.");
            EditorApplication.isPlaying = true;
        }
    }
}
#endif
