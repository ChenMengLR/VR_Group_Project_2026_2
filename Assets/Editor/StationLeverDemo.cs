#if UNITY_EDITOR
using System.Collections;
using CourseProject;
using Oculus.Interaction;
using UnityEngine;

namespace CourseSetup
{
    /// <summary>Automated demo input shared by the route capture and mission regression runner.</summary>
    internal static class StationLeverDemo
    {
        internal static IEnumerator Pull(StationAirlockLever lever, int pointerId)
        {
            if (lever == null) yield break;
            var collider = lever.Handle.GetComponent<BoxCollider>();
            Vector3 grip = lever.Handle.TransformPoint(collider.center) - lever.Pivot.position;
            var pose = new Pose(lever.Pivot.position + grip, lever.Handle.rotation);
            lever.Grab.ProcessPointerEvent(new PointerEvent(pointerId, PointerEventType.Hover, pose));
            lever.Grab.ProcessPointerEvent(new PointerEvent(pointerId, PointerEventType.Select, pose));
            for (float elapsed = 0f; elapsed < .8f; elapsed += Time.deltaTime)
            {
                var rotation = Quaternion.AngleAxis(StationAirlockLever.PullDegrees * Mathf.Clamp01(elapsed / .8f), lever.Pivot.right);
                pose = new Pose(lever.Pivot.position + rotation * grip, rotation * lever.Pivot.rotation);
                lever.Grab.ProcessPointerEvent(new PointerEvent(pointerId, PointerEventType.Move, pose));
                if (lever.IsLatched) break;
                yield return null;
            }
            lever.Grab.ProcessPointerEvent(new PointerEvent(pointerId, PointerEventType.Unselect, pose));
            lever.Grab.ProcessPointerEvent(new PointerEvent(pointerId, PointerEventType.Unhover, pose));
        }
    }
}
#endif
