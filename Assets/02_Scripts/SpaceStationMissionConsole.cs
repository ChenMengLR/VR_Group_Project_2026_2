using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CourseProject
{
    /// <summary>
    /// Week 06 world-space mission UI. The Meta canvas wizard connects its
    /// buttons to the existing controller ray interactors in the scene.
    /// </summary>
    public sealed class SpaceStationMissionConsole : MonoBehaviour
    {
        [SerializeField] private SpaceStationMission mission;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text helpText;
        [SerializeField] private TMP_Text helpButtonText;
        [SerializeField] private Button helpButton;
        [SerializeField] private Button resetButton;

        private string lastStatus;
        private bool helpVisible;

        public SpaceStationMission Mission => mission;
        public Button HelpButton => helpButton;
        public Button ResetButton => resetButton;
        public bool HelpVisible => helpVisible;
        public string VisibleStatus => statusText != null ? statusText.text : string.Empty;

        private void Start() => Refresh();

        private void Update()
        {
            if (mission != null && mission.CurrentStatus != lastStatus) Refresh();
        }

        public void ToggleHelp()
        {
            helpVisible = !helpVisible;
            Refresh();
        }

        public void RequestReset()
        {
            if (mission != null) mission.ResetMission();
            Refresh();
        }

        public void Refresh()
        {
            lastStatus = mission != null ? mission.CurrentStatus : null;
            if (statusText != null)
                statusText.text = string.IsNullOrEmpty(lastStatus)
                    ? "STATION OFFLINE\nGRAB THE CORE"
                    : lastStatus;
            if (helpText != null) helpText.gameObject.SetActive(helpVisible);
            if (helpButtonText != null)
                helpButtonText.text = helpVisible ? "HIDE HELP" : "SHOW HELP";
        }
    }
}
