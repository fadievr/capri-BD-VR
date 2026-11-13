// Your two hands, whether you're holding Touch controllers or using hand tracking.
// "Grabbing" is the grip button on a controller, or a pinch with a tracked hand.
// With tracked hands, Position is the middle of your palm (not your wrist), and Tip is your index fingertip, so pats and
// pokes land where your hand really is.
using UnityEngine;

namespace CapriRoom
{
    public sealed class HandState
    {
        public bool Left;
        public bool Tracked;
        public Vector3 Position;
        public Vector3 Velocity;
        public bool Grabbing, GrabStarted, GrabEnded;
        public float Thumbstick;        // up/down on this controller's stick (0 with tracked hands)
        public bool ButtonOneDown;      // A or X
        public bool ButtonTwoDown;      // B or Y
        // The laser pointer: where it starts and which way it points, and a click (trigger, or a pinch with a tracked hand).
        public Vector3 PointFrom, PointAlong;
        public bool Clicked;
        public bool Controller;
        public bool PcOneWas, PcTwoWas, PcTriggerWas;   // last frame's buttons, for PC VR presses
        public bool MiddlePinched, RingPinched;          // tracked hands: finger-to-thumb pinches held right now

        // What she gets poked with: your index fingertip with tracked hands, the controller itself otherwise.
        public Vector3 Tip, TipVelocity;

        private readonly Mover hand = new Mover(), tip = new Mover();

        public void Track(Vector3 position, Vector3 fingertip, float now)
        {
            Position = position;
            Velocity = hand.Move(position, now);
            Tip = fingertip;
            TipVelocity = tip.Move(fingertip, now);
        }

        // Speed over the last few frames, so one jittery frame doesn't count.
        private sealed class Mover
        {
            private readonly Vector3[] history = new Vector3[6];
            private readonly float[] times = new float[6];
            private int next;

            public Vector3 Move(Vector3 position, float now)
            {
                history[next] = position;
                times[next] = now;
                int oldest = (next + 1) % history.Length;
                next = oldest;
                float span = now - times[oldest];
                return span > 0.0001f && times[oldest] > 0 ? (position - history[oldest]) / span : Vector3.zero;
            }
        }
    }

    [DefaultExecutionOrder(-50)]   // read your hands before Capri reacts to them
    public class CapriHands : MonoBehaviour
    {
        public HandState LeftHand = new HandState { Left = true };
        public HandState RightHand = new HandState { Left = false };
        public Transform Head { get; private set; }

        private OVRCameraRig rig;
        private CapriPcRig pcRig;     // PC VR (SteamVR / Index) instead of Meta's rig
        private OVRHand leftTracked, rightTracked;
        private float nextLookForHands;

        public HandState[] Both { get { return new[] { LeftHand, RightHand }; } }

        private void Update()
        {
            if (rig == null)
            {
                rig = FindFirstObjectByType<OVRCameraRig>();
                if (rig == null)
                {
                    if (pcRig == null) pcRig = FindFirstObjectByType<CapriPcRig>();
                    if (pcRig == null || pcRig.head == null) return;
                    Head = pcRig.head.transform;
                    ReadPc(LeftHand, pcRig.leftHand, pcRig.Device(true));
                    ReadPc(RightHand, pcRig.rightHand, pcRig.Device(false));
                    return;
                }
            }
            Head = rig.centerEyeAnchor;
            if ((leftTracked == null || rightTracked == null) && Time.time >= nextLookForHands)
            {
                nextLookForHands = Time.time + 2f;
                foreach (OVRHand hand in FindObjectsByType<OVRHand>(FindObjectsSortMode.None))
                {
                    if (hand.GetHand() == OVRPlugin.Hand.HandLeft) leftTracked = hand;
                    else if (hand.GetHand() == OVRPlugin.Hand.HandRight) rightTracked = hand;
                }
            }
            Read(LeftHand, rig.leftHandAnchor, leftTracked, OVRInput.Controller.LTouch);
            Read(RightHand, rig.rightHandAnchor, rightTracked, OVRInput.Controller.RTouch);
        }

        // PC VR controllers (Valve Index, Vive, Touch over Link) through Unity's standard XR input.
        // Index: grip squeeze grabs, trigger clicks, A/B are buttons one and two, the thumbstick scrolls.
        private void ReadPc(HandState state, Transform anchor, UnityEngine.XR.InputDevice device)
        {
            bool on = device.isValid && anchor != null;
            state.Tracked = on;
            state.Controller = on;
            if (anchor != null)
            {
                state.Track(anchor.position, anchor.position, Time.time);
                state.PointFrom = anchor.position;
                state.PointAlong = anchor.forward;
            }
            bool was = state.Grabbing;
            float grip = 0f, trigger = 0f;
            Vector2 stick = Vector2.zero;
            bool one = false, two = false;
            if (on)
            {
                device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.grip, out grip);
                device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out trigger);
                device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis, out stick);
                device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out one);
                device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton, out two);
            }
            state.Grabbing = on && (was ? grip > 0.35f : grip > 0.6f);
            state.Thumbstick = stick.y;
            state.ButtonOneDown = one && !state.PcOneWas;
            state.ButtonTwoDown = two && !state.PcTwoWas;
            state.Clicked = trigger > 0.7f && !state.PcTriggerWas;
            state.PcOneWas = one;
            state.PcTwoWas = two;
            state.PcTriggerWas = trigger > (state.PcTriggerWas ? 0.4f : 0.7f);
            state.GrabStarted = state.Grabbing && !was;
            state.GrabEnded = !state.Grabbing && was;
        }

        // The hand anchor sits at your wrist, a hand's length from where you'd touch her, so use the hand's own bones:
        // the palm (or the middle knuckle on older skeletons) and the index fingertip.
        private static void HandPoints(OVRHand tracked, ref Vector3 palm, ref Vector3 fingertip)
        {
            OVRSkeleton skeleton = tracked.GetComponent<OVRSkeleton>();
            if (skeleton == null || !skeleton.IsInitialized || !skeleton.IsDataValid || skeleton.Bones == null) return;
            Transform palmBone = null, knuckle = null, tipBone = null;
            foreach (OVRBone bone in skeleton.Bones)
            {
                if (bone == null || bone.Transform == null) continue;
                string name = bone.Id.ToString();
                if (name.EndsWith("Palm")) palmBone = bone.Transform;
                else if (name.EndsWith("Middle1") || name.EndsWith("MiddleProximal")) knuckle = bone.Transform;
                else if (name.EndsWith("IndexTip")) tipBone = bone.Transform;
            }
            if (palmBone != null) palm = palmBone.position;
            else if (knuckle != null) palm = knuckle.position;
            if (tipBone != null) fingertip = tipBone.position;
        }

        // A finger touching the thumb: firm to start (0.85), let go once it's clearly apart (below 0.5).
        private static bool Finger(bool on, OVRHand tracked, OVRHand.HandFinger finger, bool was)
        {
            if (!on) return false;
            float strength = tracked.GetFingerPinchStrength(finger);
            return was ? strength > 0.5f : strength > 0.85f;
        }

        private void Read(HandState state, Transform anchor, OVRHand tracked, OVRInput.Controller controller)
        {
            bool controllerOn = OVRInput.IsControllerConnected(controller);
            bool handOn = tracked != null && tracked.IsTracked && tracked.IsDataHighConfidence;
            state.Tracked = controllerOn || handOn;
            state.Controller = controllerOn;
            Vector3 palm = anchor.position, fingertip = anchor.position;
            if (!controllerOn && handOn) HandPoints(tracked, ref palm, ref fingertip);
            state.Track(palm, fingertip, Time.time);
            if (!controllerOn && handOn && tracked.IsPointerPoseValid && tracked.PointerPose != null)
            {
                state.PointFrom = tracked.PointerPose.position;
                state.PointAlong = tracked.PointerPose.forward;
            }
            else
            {
                state.PointFrom = anchor.position;
                state.PointAlong = anchor.forward;
            }

            bool was = state.Grabbing;
            if (controllerOn)
            {
                float grip = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, controller);
                state.Grabbing = was ? grip > 0.35f : grip > 0.6f;
                state.Thumbstick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, controller).y;
                state.ButtonOneDown = OVRInput.GetDown(OVRInput.Button.One, controller);
                state.ButtonTwoDown = OVRInput.GetDown(OVRInput.Button.Two, controller);
                state.Clicked = OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, controller);
            }
            else
            {
                float pinch = handOn ? tracked.GetFingerPinchStrength(OVRHand.HandFinger.Index) : 0f;
                state.Grabbing = was ? pinch > 0.5f : pinch > 0.85f;
                state.Thumbstick = 0;
                // Buttons with bare hands: tap your middle finger to your thumb for B (right) or Y (left), your ring
                // finger for A (right) or X (left). Ignored while your index finger is pinching, so grabbing her never
                // presses a button by accident.
                bool indexFree = pinch < 0.4f;
                bool middle = Finger(handOn && indexFree, tracked, OVRHand.HandFinger.Middle, state.MiddlePinched);
                bool ring = Finger(handOn && indexFree, tracked, OVRHand.HandFinger.Ring, state.RingPinched);
                state.ButtonTwoDown = middle && !state.MiddlePinched;
                state.ButtonOneDown = ring && !state.RingPinched && !middle;
                state.MiddlePinched = middle;
                state.RingPinched = ring;
            }
            if (!state.Tracked) state.Grabbing = false;
            state.GrabStarted = state.Grabbing && !was;
            state.GrabEnded = !state.Grabbing && was;
            if (!controllerOn) state.Clicked = state.GrabStarted;   // with tracked hands, a pinch clicks
        }
    }
}
