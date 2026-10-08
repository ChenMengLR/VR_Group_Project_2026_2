using UnityEngine;

namespace CourseProject
{
    /// <summary>Sounds attached to the moving core, called by Meta pointer events.</summary>
    public sealed class SpaceStationGrabAudio : MonoBehaviour
    {
        [SerializeField] private AudioSource output;
        [SerializeField] private AudioClip grabClip;
        [SerializeField] private AudioClip releaseClip;

        public AudioSource Output => output;
        public AudioClip GrabClip => grabClip;
        public AudioClip ReleaseClip => releaseClip;
        public int GrabCount { get; private set; }
        public int ReleaseCount { get; private set; }

        public void PlayGrab()
        {
            GrabCount++;
            if (output != null && grabClip != null)
            {
                output.PlayOneShot(grabClip);
                Debug.Log("WEEK05_GRAB_SOUND_PLAYED: " + grabClip.name);
            }
        }

        public void PlayRelease()
        {
            ReleaseCount++;
            if (output != null && releaseClip != null)
            {
                output.PlayOneShot(releaseClip);
                Debug.Log("WEEK05_RELEASE_SOUND_PLAYED: " + releaseClip.name);
            }
        }
    }
}
