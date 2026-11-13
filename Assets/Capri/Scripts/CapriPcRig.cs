// For PC VR (SteamVR / Valve Index, or a Quest over Link) where Meta's camera rig isn't there:
// moves the camera with your headset and tracks both controllers through Unity's standard XR input.
// The Quest build doesn't use this; CapriHands only falls back to it when there's no OVRCameraRig in the scene.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace CapriRoom
{
    [DefaultExecutionOrder(-100)]   // before CapriHands reads the hands
    public class CapriPcRig : MonoBehaviour
    {
        public Camera head;
        public Transform leftHand, rightHand;

        private readonly List<XRInputSubsystem> subsystems = new List<XRInputSubsystem>();
        private bool floorSet;

        public InputDevice Device(bool left) { return InputDevices.GetDeviceAtXRNode(left ? XRNode.LeftHand : XRNode.RightHand); }

        private void Awake()
        {
            // SteamVR's menu and Room View take the desktop window's focus. By default Unity's Input System then switches
            // devices off until the window gets focus back, which it often doesn't, so keep them on regardless.
            if (UnityEngine.InputSystem.InputSystem.settings != null)
                UnityEngine.InputSystem.InputSystem.settings.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
            Application.runInBackground = true;
        }

        private void OnEnable() { Application.onBeforeRender += Track; }
        private void OnDisable() { Application.onBeforeRender -= Track; }

        private void Update()
        {
            if (!floorSet)
            {
                // Heights measured from your real floor, like on the Quest.
                SubsystemManager.GetSubsystems(subsystems);
                foreach (XRInputSubsystem subsystem in subsystems)
                    if (subsystem.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor)) floorSet = true;
            }
            Track();
        }

        private void Track()
        {
            Place(head != null ? head.transform : null, XRNode.Head);
            Place(leftHand, XRNode.LeftHand);
            Place(rightHand, XRNode.RightHand);
        }

        private static void Place(Transform target, XRNode node)
        {
            if (target == null) return;
            InputDevice device = InputDevices.GetDeviceAtXRNode(node);
            if (!device.isValid) return;
            Vector3 position;
            Quaternion rotation;
            bool hasPosition = node == XRNode.Head
                ? device.TryGetFeatureValue(CommonUsages.centerEyePosition, out position) || device.TryGetFeatureValue(CommonUsages.devicePosition, out position)
                : device.TryGetFeatureValue(CommonUsages.devicePosition, out position);
            bool hasRotation = node == XRNode.Head
                ? device.TryGetFeatureValue(CommonUsages.centerEyeRotation, out rotation) || device.TryGetFeatureValue(CommonUsages.deviceRotation, out rotation)
                : device.TryGetFeatureValue(CommonUsages.deviceRotation, out rotation);
            if (hasPosition) target.localPosition = position;
            if (hasRotation) target.localRotation = rotation;
        }
    }
}
