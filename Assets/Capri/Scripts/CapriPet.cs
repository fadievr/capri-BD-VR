// Capri in your room: she glitches in near you, walks around your floor, sits on the edges of your furniture,
// naps when you ignore her, reacts when you pet, poke, grab or throw her, and talks with you through your headset's mic.
//
// Controls
//   Grab her:    grip button (controllers) or pinch (hand tracking) with your hand near her. Let go to drop her.
//   Throw her:   let go while your hand's moving fast. She tumbles the way you threw her, bounces off walls and the
//                floor, and a hard landing leaves her dizzy. Three throws in two minutes and she sulks until you say
//                sorry, pat her head, or leave her alone for five minutes. Like the desktop app (Capri 0.8.3).
//   Resize her:  while holding her with a controller, push its thumbstick up or down.
//   Pet her:     move your hand side to side over her head, with a controller or your bare hand.
//   Poke her:    jab her quickly with a controller or a fingertip. A slow touch isn't a poke.
//   Talk:        just talk. She listens on the headset's mic (needs a Fish Audio key, or OpenAI as her brain).
//   Mute mic:    X.
//   Bop:         A, while she's sitting on a ledge.
//   Come here:   B, or say "come here".
//   Say:         "sit by me" or "lay by me" and she finds the ledge nearest you and sits or lies down on it. "Lay down",
//                "sit up" and "stand up" change how she's resting where she is.
//   Settings:    Y opens her settings: BRAIN, VOICE, YOU and MOVES.
//   Let her see: Y → YOU. She can look through your Quest's camera when you ask ("what do you see?", "look at this"),
//                and with Watch me she glances now and then and says something about it, like the desktop app's
//                Watch my screen.
//   Put her down next to the edge of a table, couch or bed and she sits on it.
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace CapriRoom
{
    [RequireComponent(typeof(RoomSense), typeof(CapriHands))]
    public class CapriPet : MonoBehaviour
    {
        [Header("Size and movement")]
        [Tooltip("Her height in metres. You can also change it in the headset by holding her and pushing the thumbstick.")]
        [Range(0.2f, 1.7f)] public float size = 0.5f;
        public float walkSpeed = 0.35f;
        [Tooltip("She won't wander further than this from you.")]
        public float stayWithin = 2.5f;

        [Header("Habits (Wander, Sleep and Pop-ins are set in the headset: Y opens her settings)")]
        [Range(0, 1)] public float chanceToSitOnFurniture = 0.35f;
        public Vector2 secondsSittingOnFurniture = new Vector2(20, 60);

        private enum Mode { Waiting, GlitchingOut, GlitchingIn, Idle, Walking, Perched, Held, Falling, Napping, Waking, Dancing, Reacting }
        // How she rests in Perched mode: sit 1 (her original perch), sit 2 (one leg along the ledge), or lying down. She can
        // lie on the floor too; sitting needs a ledge.
        private enum Seat { Sit1, Sit2, Lay }

        private static readonly string[] PetLines =
        {
            "Heh. Okay, I'll allow it.", "Pets logged. Morale up twelve percent.", "Careful, choom. I bite viruses.",
            "Head pats? On my chrome? Bold.", "Okay, that's nice. Don't tell anyone.", "System status: very patted.",
            "You're gonna wear a groove in my firmware, choom.", "Easy on the hair. It took three patches to get it like this.",
            "Affection detected. Sending some back. Hi.", "Keep going. This beats defragging.", "Hehe. My cooling fans just kicked in.",
            "Ten out of ten pats. Would get patted again.", "Is this a bribe? Because it's working.", "Mmm. If I start glitch-purring, that's on you."
        };
        private static readonly string[] PokeLines = { "Hey! Personal space, choom.", "Ow. Rude.", "Poke detected. Filing a complaint.", "Do that again and I'm deleting your saves." };
        private static readonly string[] TossLines = { "Did you just throw me?!", "Landing confirmed. Mostly.", "Okay. Never again.", "Ten out of ten. Do it again." };
        private static readonly string[] HoldLines = { "Put me down, choom!", "Hey! Where are we going?", "Careful, I'm fragile code." };
        // Thrown, from the desktop app (Capri 0.8.3).
        private static readonly string[] ThrowLines = { "WHEEEE!", "Put me DOWN!", "Choom, I'm not a frisbee!", "AAAAAA!", "Not the hair!" };
        private static readonly string[] WarnLines = { "Okay, rude.", "Again?! One more and we're fighting.", "Do that again. I dare you." };
        private static readonly string[] DizzyLines = { "Ugh. Which way is up?", "Everything's spinning, choom.", "My gyro's cooked." };
        private static readonly string[] SulkLines = { "That's it. Don't touch me.", "Nope. We're done. Hands off." };
        private static readonly string[] DodgeLines = { "Nope.", "Hands off, choom.", "Say sorry first.", "Not happening." };
        private static readonly string[] SorryLines = { "...Fine. Apology accepted.", "Okay. We're cool. No more yeeting." };
        private static readonly string[] PatPeaceLines = { "...Okay, that's cheating. Fine, you're forgiven.", "Hmph. Head pats work. We're cool." };
        private static readonly string[] AloneLines = { "...Okay, I'm over it.", "Fine. I missed you. A little." };
        private const string FirstDodgeLine = "No. Don't touch me.";
        private static readonly Regex SorryWords = new Regex(@"\b(sorry|sry|soz|sowwy|apologi[sz]e|my bad|forgive me|lo siento|perd[oó]n|d[eé]sol[eé]e?|entschuldigung|scusa|desculpa)\b", RegexOptions.IgnoreCase);

        // Things you can tell her to do. Handled right away, without her brain.
        private static readonly Regex LayByMe = new Regex(@"\b(come\s+)?(lay|lie)(\s+down)?\s+(by|next\s+to|beside|with|near|over\s+by)\s+me\b", RegexOptions.IgnoreCase);
        private static readonly Regex SitByMe = new Regex(@"\b(come\s+)?sit(\s+down)?\s+(by|next\s+to|beside|with|near|over\s+by)\s+me\b", RegexOptions.IgnoreCase);
        private static readonly Regex LayDown = new Regex(@"\b(lay|lie)\s+(down|back)\b", RegexOptions.IgnoreCase);
        private static readonly Regex SitUp = new Regex(@"\bsit\s+(up|down)\b", RegexOptions.IgnoreCase);
        private static readonly Regex StandUp = new Regex(@"\b(stand|get)\s+up\b", RegexOptions.IgnoreCase);
        private static readonly Regex ComeOver = new Regex(@"\b(come\s+(here|over(\s+here)?|to\s+me|closer)|get\s+over\s+here)\b", RegexOptions.IgnoreCase);
        private static readonly string[] OnMyWayLines = { "On my way.", "Coming, choom.", "Scooting over." };
        private static readonly string[] NoRoomLines = { "Not enough room to lie down there. I'll sit.", "Too small to stretch out on. Sitting instead." };

        private static readonly string[] WakeLines = { "Mm? I'm up. I'm up.", "Was I asleep? Don't tell anyone.", "Rebooting... hi." };

        // Throwing, in metres a second. Slower than ThrowFrom she just drops, from ThrowHard she yells, ThrowMost caps a flick.
        private const float ThrowFrom = 1.2f, ThrowHard = 2.5f, ThrowMost = 7f;
        private const float ThrowDrag = 0.6f, WallBounce = 0.55f, FloorBounce = 0.42f, FloorGrip = 0.72f, FlightLongest = 8f;
        private const float Settle = 1.5f, Calm = 1.5f, DizzyFrom = 2.4f, DizzyFor = 1.8f, DizzySway = 7f;
        private const float SulkWindow = 120f, SulkAlone = 300f, DodgeSaid = 6f;
        private const int SulkThrows = 3;

        // How far her feet hang below your hand when you hold her (from the dangle art, in sizes).
        private const float DangleDrop = 0.907f;
        private const string SizeKey = "capri.size";

        private RoomSense room;
        private CapriHands hands;
        private Transform body;
        private SpriteRenderer sprite, mouth, shadow;
        private SpeechBubble bubble;
        private AudioSource voice;

        private Mode mode = Mode.Waiting;
        private CapriClip clip;
        private bool looping;
        private float clipStarted;
        private Action clipDone;

        private Surface standingOn;            // null = the floor
        private Seat seat;                     // how she's resting while Perched
        private Seat arriveAs;                 // how she'll rest where she's teleporting to
        private Vector3 walkTarget;
        private Surface teleportTarget;
        private float nextWander, perchUntil, nextBlink, lastAttention, gazeHeld, reactCooldown;
        private Vector3 velocity;
        private HandState holder;
        private float fallStarted, nextShadowCheck, shadowY, nextMouthFrame;
        private bool greeted, wasHearing;
        private SpeechBubble micTag;           // the mic's own little label, so muting never cuts off what she's saying
        private CapriSettings settings;
        private CapriBrain brain;
        private CapriVoice speaker;
        private CapriEars ears;
        private float nextPop;

        // Her flight when thrown: her tumble in degrees and degrees a second, how many times she's bounced off the floor, the
        // hardest she's hit anything, and whether she's already said something about it.
        private bool thrown, flightLine;
        private float spin, spinRate, flyHardest;
        private int flyFloors;
        private float dizzyFrom = -1, dizzyUntil = -1;
        // Her grudge: throws in the last two minutes, and when her sulk started and someone last bothered her.
        private readonly List<float> throwsAt = new List<float>();
        private float sulkSince = -1, sulkBothered, dodgeSaidAt = -10;
        private bool sulkLine, dodgedOnce;
        private bool Sulking { get { return sulkSince >= 0; } }

        // Her eyes (Let her see) and her glances (Watch me): when the next glance is due, whether one's in flight, and her
        // glance lines from the last hour so she doesn't repeat herself.
        private CapriEyes eyes;
        private bool eyesStarted, sawBefore, glancing;
        private float nextGlance = -1, lastGlance = -1;
        private readonly List<KeyValuePair<float, string>> glanced = new List<KeyValuePair<float, string>>();
        // Asking her to look at something sends her a picture with what you said.
        private string waitingLine;
        private static readonly Regex Looking = new Regex(@"\b(look(ing)? at|take a look|have a look|see (this|that|it|what|me|here)|can you see|do you see|what (do|can) you see|what am i wearing|what am i (holding|looking at|doing|watching|playing)|what('s| is) (this|that|on my|in front)|read (this|that|it)|check (this|that) out|how do i look)\b", RegexOptions.IgnoreCase);

        private bool Wandering { get { return CapriConfig.Wander != 0; } }
        private Vector2 secondsBetweenWanders { get { return CapriConfig.WanderGaps[Mathf.Clamp(CapriConfig.Wander, 0, 3)]; } }
        private float napAfterSeconds { get { float s = CapriConfig.SleepSeconds[Mathf.Clamp(CapriConfig.Sleep, 0, 4)]; return s <= 0 ? float.MaxValue : s; } }

        private void Start()
        {
            room = GetComponent<RoomSense>();
            hands = GetComponent<CapriHands>();
            size = PlayerPrefs.GetFloat(SizeKey, size);
            settings = CapriSettings.Make(this);
            brain = gameObject.AddComponent<CapriBrain>();
            speaker = gameObject.AddComponent<CapriVoice>();
            ears = gameObject.AddComponent<CapriEars>();
            ears.Paused = delegate { return voice.isPlaying || brain.Busy || speaker.Busy || mode == Mode.Waiting; };
            ears.Said = Heard;
            eyes = gameObject.AddComponent<CapriEyes>();
            sawBefore = CapriConfig.Sight;
            ears.Trouble = line => Say(line);
            nextPop = Time.time + Gap(CapriConfig.PopGaps[Mathf.Clamp(CapriConfig.PopIns, 0, 2)]);

            body = new GameObject("Body").transform;
            body.SetParent(transform, false);
            sprite = body.gameObject.AddComponent<SpriteRenderer>();
            GameObject mouthObject = new GameObject("Mouth");
            mouthObject.transform.SetParent(body, false);
            mouth = mouthObject.AddComponent<SpriteRenderer>();
            mouth.sortingOrder = 1;
            mouth.enabled = false;

            GameObject shadowObject = new GameObject("Shadow");
            shadowObject.transform.SetParent(transform, false);
            shadowObject.transform.localRotation = Quaternion.Euler(90, 0, 0);
            shadow = shadowObject.AddComponent<SpriteRenderer>();
            shadow.sprite = BlobSprite();
            shadow.sortingOrder = -1;

            bubble = SpeechBubble.Make(transform);
            micTag = SpeechBubble.Make(transform);
            micTag.transform.localScale = Vector3.one * 0.6f;
            voice = gameObject.AddComponent<AudioSource>();
            voice.spatialBlend = 1;           // her voice comes from where she is
            voice.minDistance = 0.5f;
            voice.maxDistance = 8;
            speaker.Attach(voice);

            SetVisible(false);
        }

        private void Update()
        {
            if (hands.Head == null) return;
            if (mode == Mode.Waiting)
            {
                if (!room.Ready) return;
                Vector3 front = hands.Head.position + RoomSense.Flat(hands.Head.forward).normalized * 1.1f;
                GlitchIn(new Surface { Point = room.FloorSpot(front, 0.4f) });
                return;
            }
            // Her eyes start after the room has loaded, so the camera's permission prompt doesn't clash with the room's.
            if (!eyesStarted) { eyesStarted = true; eyes.Want(CapriConfig.Sight); }

            Animate();
            if (Sulking && Time.time - sulkBothered > SulkAlone) MakeUp(AloneLines);
            Interact();
            if (settings.IsOpen) settings.Point(hands.Both);
            Listen();
            PopIn();
            Glance();
            if (waitingLine != null && !brain.Busy) { string said = waitingLine; waitingLine = null; Heard(said); }

            switch (mode)
            {
                case Mode.Idle: Idle(); break;
                case Mode.Walking: Walk(); break;
                case Mode.Perched: Perched(); break;
                case Mode.Held: Held(); break;
                case Mode.Falling: Fall(); break;
                case Mode.Napping: Nap(); break;
            }

            FaceYou();
            Pose();
            PlaceShadow();
            Mouth();
        }

        // ---------- Animation ----------

        private void Play(string name, bool loop, Action done = null)
        {
            clip = CapriClips.Get(name);
            looping = loop;
            clipStarted = Time.time;
            clipDone = done;
            sprite.sprite = clip.Frames[0];
        }

        private void Animate()
        {
            if (clip == null) return;
            int frame = (int)((Time.time - clipStarted) * clip.Fps);
            if (frame >= clip.Frames.Length)
            {
                if (looping) frame %= clip.Frames.Length;
                else
                {
                    sprite.sprite = clip.Frames[clip.Frames.Length - 1];
                    Action done = clipDone;
                    clipDone = null;
                    if (done != null) done();
                    return;
                }
            }
            sprite.sprite = clip.Frames[frame];
        }

        // Her art is drawn so one unit is her size: the body is scaled to it, lifted when she dangles, tumbles around her middle
        // when she's thrown, and sways around her hip when she's dizzy.
        private void Pose()
        {
            if (clip == null || sprite.sprite == null) return;
            body.localScale = Vector3.one * size;
            Vector3 lift = clip.Name == "dangle" ? Vector3.up * DangleDrop * size : Vector3.zero;
            float turn = mode == Mode.Falling && thrown && CapriConfig.Motion ? spin : 0;
            float sway = Sway();
            if (turn == 0 && sway == 0)
            {
                body.localRotation = Quaternion.identity;
                body.localPosition = lift;
                return;
            }
            // Turning on screen: clockwise for a throw to your right. She faces away from you, so that's minus around z.
            Quaternion roll = Quaternion.Euler(0, 0, -(turn + sway));
            Vector3 pivot = turn != 0 ? sprite.sprite.bounds.center * size : Vector3.up * size * 0.3f;
            body.localRotation = roll;
            body.localPosition = lift + pivot - roll * pivot;
        }

        // Dizzy after a hard landing, her head swims around her hip and settles. Not while she's held or falling.
        private float Sway()
        {
            if (!CapriConfig.Motion || Time.time < dizzyFrom || Time.time >= dizzyUntil || mode == Mode.Held || mode == Mode.Falling) return 0;
            float fade = Mathf.Min(1, (dizzyUntil - Time.time) / (DizzyFor * 0.6f));
            return DizzySway * Mathf.Sin((Time.time - dizzyFrom) * Mathf.PI * 2 * 1.3f) * fade;
        }

        private void Rest()
        {
            bool edge = standingOn != null && standingOn.Edge;
            if (!edge && seat != Seat.Lay) seat = Seat.Sit1;   // only lying works off a ledge
            if (edge || seat == Seat.Lay) { mode = Mode.Perched; Play(SeatClip, true); }
            else { mode = Mode.Idle; Play("idle", true); }
            nextBlink = Time.time + UnityEngine.Random.Range(2.5f, 6f);
        }

        private string SeatClip { get { return seat == Seat.Lay ? "lay" : seat == Seat.Sit2 ? "perch_sit2" : "perch"; } }
        private bool Resting { get { return mode == Mode.Perched; } }
        private bool Lying { get { return mode == Mode.Perched && seat == Seat.Lay; } }

        // ---------- Getting around ----------

        private void GlitchIn(Surface to)
        {
            SetVisible(true);
            standingOn = to.Raised ? to : null;
            seat = arriveAs;
            arriveAs = Seat.Sit1;
            if (seat == Seat.Lay && to.Edge) to.Point = RoomSense.LayPoint(to, size);
            transform.position = to.Point;
            mode = Mode.GlitchingIn;
            bool quietly = !greeted && !CapriConfig.GlitchIn;   // "Glitch in at startup" off: she's just there
            Play(quietly ? (to.Edge ? "perch" : "idle") : to.Edge ? "perch_arrive" : "arrive", false, delegate
            {
                Rest();
                if (to.Edge || seat == Seat.Lay) perchUntil = Time.time + UnityEngine.Random.Range(secondsSittingOnFurniture.x, secondsSittingOnFurniture.y) * (commanded ? 2 : 1);
                commanded = false;
                nextWander = Time.time + UnityEngine.Random.Range(secondsBetweenWanders.x, secondsBetweenWanders.y);
                if (!greeted) { greeted = true; Greet(); }
            });
        }

        private bool commanded;   // you told her where to go, so she stays a good while

        private void GlitchTo(Surface to, Seat rest = Seat.Sit1)
        {
            arriveAs = rest;
            teleportTarget = to;
            mode = Mode.GlitchingOut;
            bool perched = standingOn != null && standingOn.Edge;
            Play(perched ? "perch_leave" : "leave", false, delegate { GlitchIn(teleportTarget); });
        }

        private void ComeHere()
        {
            Vector3 front = hands.Head.position + RoomSense.Flat(hands.Head.forward).normalized * 0.9f;
            Voice("Coming, choom.");
            GlitchTo(new Surface { Point = room.FloorSpot(front, 0.3f) });
        }

        private void Wander()
        {
            nextWander = Time.time + UnityEngine.Random.Range(secondsBetweenWanders.x, secondsBetweenWanders.y);
            Vector3 you = hands.Head.position;
            if (UnityEngine.Random.value < chanceToSitOnFurniture)
            {
                Surface ledge = room.FurnitureSpot(you, stayWithin + 0.5f);
                if (ledge != null) { GlitchTo(ledge, RollSeat(ledge)); return; }
            }
            Vector3 spot = room.FloorSpot(you, stayWithin);
            if (standingOn != null) { GlitchTo(new Surface { Point = spot }); return; }
            walkTarget = spot;
            mode = Mode.Walking;
            Play("walk", true);
        }

        // Teleporting to a ledge she lies down, sits like before, or sits with one leg along it: a third each. A ledge too
        // small to lie on and she sits.
        private Seat RollSeat(Surface ledge)
        {
            int roll = UnityEngine.Random.Range(0, 3);
            if (roll == 0 && RoomSense.FitsLying(ledge, size)) return Seat.Lay;
            if (roll == 0) return UnityEngine.Random.value < 0.5f ? Seat.Sit1 : Seat.Sit2;
            return roll == 1 ? Seat.Sit1 : Seat.Sit2;
        }

        private void Idle()
        {
            if (Time.time > nextBlink)
            {
                nextBlink = Time.time + UnityEngine.Random.Range(2.5f, 6f);
                Play(UnityEngine.Random.value < 0.15f ? "blink_glitch" : "blink", false, delegate { if (mode == Mode.Idle) Play("idle", true); });
            }
            if (Time.time - lastAttention > napAfterSeconds && standingOn == null)
            {
                mode = Mode.Napping;
                Play("sleep", true);
                return;
            }
            // Follow you if you walk off.
            if (!Wandering) return;
            if (RoomSense.Flat(transform.position - hands.Head.position).magnitude > stayWithin + 1.5f) { nextWander = 0; }
            if (Time.time > nextWander && !voice.isPlaying) Wander();
        }

        private void Walk()
        {
            Vector3 to = walkTarget - transform.position;
            to.y = 0;
            float step = walkSpeed * size / 0.5f * Time.deltaTime;   // bigger Capri, longer steps
            if (to.magnitude <= step) { transform.position = new Vector3(walkTarget.x, transform.position.y, walkTarget.z); Rest(); return; }
            transform.position += to.normalized * step;
        }

        private void Perched()
        {
            if (clip != null && clip.Name == "perch_music") return;   // bopping along; A or X stops it
            if (Time.time > nextBlink && seat == Seat.Sit1)
            {
                nextBlink = Time.time + UnityEngine.Random.Range(2.5f, 6f);
                Play(UnityEngine.Random.value < 0.15f ? "perch_blink_glitch" : "perch_blink", false, delegate { if (mode == Mode.Perched) Play(SeatClip, true); });
            }
            if (Wandering && Time.time > perchUntil && Time.time - lastAttention > 5) GlitchTo(new Surface { Point = room.FloorSpot(hands.Head.position, stayWithin) });
        }

        private void Nap()
        {
            if (Time.time - lastAttention < 1)
            {
                mode = Mode.Waking;
                Play("wake", false, delegate { Rest(); Voice(Pick(WakeLines)); });
            }
        }

        // ---------- You and her ----------

        private Vector3 HeadPoint
        {
            get
            {
                if (Lying) return transform.position + transform.right * size * 0.42f + Vector3.up * size * 0.1f;   // her head's to your right
                return transform.position + Vector3.up * size * (Resting ? 0.42f : 0.82f);
            }
        }
        private Vector3 MiddlePoint { get { return transform.position + Vector3.up * size * (Lying ? 0.15f : 0.5f); } }

        // Pats: your hand has to go side to side over her head, turning back at least twice within a second or so.
        // Pokes: a quick jab into her, with a controller or a fingertip, moving toward her.
        private const float PatSwing = 0.1f, PatTurnsWithin = 1.2f, PokeSpeed = 1.2f;
        private sealed class Stroke { public int Way; public readonly List<float> Turns = new List<float>(); public bool TipInside; }
        private readonly Stroke leftStroke = new Stroke(), rightStroke = new Stroke();

        // True when this hand just turned back for the second time while moving side to side over her head.
        private bool Patting(HandState hand, Stroke stroke, bool overHead)
        {
            float now = Time.time;
            stroke.Turns.RemoveAll(t => now - t > PatTurnsWithin);
            if (!overHead || hand.Grabbing) { stroke.Way = 0; stroke.Turns.Clear(); return false; }
            float across = Vector3.Dot(hand.Velocity, transform.right);
            if (Mathf.Abs(across) < PatSwing || hand.Velocity.magnitude > 1.2f) return false;
            int way = across > 0 ? 1 : -1;
            if (stroke.Way != 0 && way != stroke.Way) stroke.Turns.Add(now);
            stroke.Way = way;
            if (stroke.Turns.Count < 2) return false;
            stroke.Turns.Clear();
            return true;
        }

        // True when this hand's fingertip (or controller) just went into her fast, heading toward her.
        private bool Poked(HandState hand, Stroke stroke, float reach)
        {
            Vector3 toHer = MiddlePoint - hand.Tip;
            bool inside = toHer.magnitude < reach;
            bool entered = inside && !stroke.TipInside;
            stroke.TipInside = inside;
            if (!entered || hand.Grabbing) return false;
            float towardHer = toHer.sqrMagnitude > 0.0001f ? Vector3.Dot(hand.TipVelocity, toHer.normalized) : 0;
            return towardHer >= PokeSpeed;
        }

        private void Interact()
        {
            bool busy = mode == Mode.GlitchingIn || mode == Mode.GlitchingOut;
            Vector3 eye = hands.Head.position;

            // Looking at her up close counts as attention (and wakes her after a couple of seconds).
            Vector3 toHer = MiddlePoint - eye;
            bool looking = toHer.magnitude < 2f && Vector3.Angle(hands.Head.forward, toHer) < 12f;
            gazeHeld = looking ? gazeHeld + Time.deltaTime : 0;
            if (mode != Mode.Napping && looking) lastAttention = Time.time;
            if (mode == Mode.Napping && gazeHeld > 2f) lastAttention = Time.time;

            foreach (HandState hand in hands.Both)
            {
                if (!hand.Tracked) continue;
                float reach = Mathf.Max(0.18f, size * 0.45f);
                float fromMiddle = Vector3.Distance(hand.Position, MiddlePoint);
                if (fromMiddle < 0.6f) lastAttention = Time.time;

                // Y (left controller) opens and closes her settings. X mutes and unmutes her mic.
                if (hand.ButtonTwoDown && hand.Left) { settings.Toggle(hands.Head); return; }
                if (hand.ButtonOneDown && hand.Left) { ToggleMic(); return; }

                if (busy) continue;

                if (hand.ButtonTwoDown && !hand.Left && mode != Mode.Held) { ComeHere(); return; }
                if (hand.ButtonOneDown && !hand.Left && mode == Mode.Perched && standingOn != null && standingOn.Edge) { ToggleDance(); return; }   // no standing dance, only bopping on a ledge

                // You can catch her mid-air. Sulking, she won't let you: she glitches a hop away instead.
                if (mode != Mode.Held && hand.GrabStarted && fromMiddle < reach + 0.1f)
                {
                    if (Sulking) { if (mode != Mode.Falling) Dodge(hand); return; }
                    PickUp(hand);
                    return;
                }

                Stroke stroke = hand.Left ? leftStroke : rightStroke;
                if (mode == Mode.Held || mode == Mode.Falling || mode == Mode.Napping) { stroke.TipInside = false; continue; }
                if (Poked(hand, stroke, reach) && Time.time > reactCooldown) { React("glitch", "perch_glitch", Pick(PokeLines)); return; }
                bool overHead = Vector3.Distance(hand.Position, HeadPoint) < Mathf.Max(0.15f, size * 0.3f);
                if (Patting(hand, stroke, overHead))
                {
                    if (Sulking) { MakeUp(PatPeaceLines); return; }
                    if (Time.time > reactCooldown) { React("grin", "perch", Pick(PetLines)); return; }
                }
            }
        }

        private void React(string standingClip, string perchedClip, string line)
        {
            reactCooldown = Time.time + 4f;
            Voice(line);
            if (Resting)
            {
                // Sit 1 has its own reactions; lying down or in sit 2 she just answers and stays put.
                if (seat == Seat.Sit1) Play(perchedClip, false, delegate { if (mode == Mode.Perched) Play(SeatClip, true); });
                return;
            }
            mode = Mode.Reacting;
            Play(standingClip, false, Rest);
        }

        private void ToggleDance()
        {
            lastAttention = Time.time;
            bool perched = standingOn != null && standingOn.Edge;
            if (!perched) return;
            mode = Mode.Perched;   // she bops along sitting up
            seat = Seat.Sit1;
            Play(clip != null && clip.Name == "perch_music" ? "perch" : "perch_music", true);
        }

        private void PickUp(HandState hand)
        {
            bool caught = mode == Mode.Falling;
            holder = hand;
            mode = Mode.Held;
            standingOn = null;
            seat = Seat.Sit1;
            thrown = false;
            spin = spinRate = 0;
            Play("dangle", true);
            if (!caught) Voice(Pick(HoldLines));
        }

        private void Held()
        {
            if (!holder.Tracked || holder.GrabEnded || !holder.Grabbing)
            {
                velocity = Vector3.ClampMagnitude(holder.Velocity, ThrowMost);
                float fast = velocity.magnitude;
                if (fast >= ThrowFrom) { Launch(fast); return; }
                // Put down gently beside the edge of a table, couch or bed: she sits on it.
                Surface ledge = room.LedgeNear(transform.position + Vector3.up * DangleDrop * size * 0.5f, 0.3f);
                if (ledge != null) { SitOn(ledge); return; }
                fallStarted = Time.time;
                thrown = false;
                mode = Mode.Falling;
                return;
            }
            transform.position = holder.Position - Vector3.up * DangleDrop * size;
            if (Mathf.Abs(holder.Thumbstick) > 0.3f)
            {
                size = Mathf.Clamp(size * (1 + holder.Thumbstick * Time.deltaTime), 0.2f, 1.7f);
                PlayerPrefs.SetFloat(SizeKey, size);
            }
            lastAttention = Time.time;
        }

        // Let go while your hand's still moving fast and she's thrown at its speed. She tumbles the way she's thrown, faster
        // the harder it is: a throw to your right spins her clockwise, like the desktop app.
        private void Launch(float fast)
        {
            fallStarted = Time.time;
            mode = Mode.Falling;
            thrown = true;
            flightLine = false;
            flyFloors = 0;
            flyHardest = fast;
            dizzyUntil = -1;
            spin = 0;
            Vector3 right = RoomSense.Flat(hands.Head.right).normalized;
            float across = Vector3.Dot(velocity, right), up = velocity.y;
            spinRate = CapriConfig.Motion ? Mathf.Clamp(across * 280f + (across < 0 ? -1 : 1) * Mathf.Abs(up) * 100f, -1080f, 1080f) : 0;
            Grudge(fast);
        }

        // One frame of a fall or a flight. Thrown, there's air drag, she bounces off walls, the ceiling and the sides of
        // furniture, and off the floor until she's slow enough to land.
        private void Fall()
        {
            float dt = Time.deltaTime;
            velocity += Physics.gravity * dt;
            if (thrown)
            {
                velocity *= Mathf.Exp(-ThrowDrag * dt);
                Spin(dt);
            }
            Vector3 next = transform.position + velocity * dt;
            Vector3 normal;
            if (thrown && room.Bounce(MiddlePoint, next + Vector3.up * size * 0.5f, out normal))
            {
                float into = Vector3.Dot(velocity, normal);
                if (into < 0)
                {
                    flyHardest = Mathf.Max(flyHardest, -into);
                    velocity -= (1 + WallBounce) * into * normal;
                    spinRate = -spinRate * 0.8f;
                }
                next = transform.position;
            }
            float ground = room.GroundBelow(next + Vector3.up * 0.3f);
            if (next.y <= ground && velocity.y <= 0)
            {
                float impact = -velocity.y;
                next.y = ground;
                transform.position = next;
                if (thrown && impact > Settle && flyFloors < 6)
                {
                    velocity = new Vector3(velocity.x * FloorGrip, impact * FloorBounce, velocity.z * FloorGrip);
                    spinRate *= 0.6f;
                    flyFloors++;
                    return;
                }
                bool flew = thrown || Time.time - fallStarted > 0.35f;
                bool wasThrown = thrown;
                Landed(wasThrown);
                if (!room.Inside(next)) { ComeHere(); return; }
                if (ground > room.FloorY + 0.15f)
                {
                    Surface ledge = room.LedgeNear(next, 0.6f);   // landed on furniture: sit on its nearest edge
                    if (ledge != null) { if (flew && !flightLine) Voice(Pick(TossLines)); SitOn(ledge); return; }
                }
                standingOn = ground > room.FloorY + 0.15f ? new Surface { Point = next, Raised = true } : null;
                mode = Mode.Reacting;
                if (flew && !flightLine) Voice(Pick(TossLines));
                Play("glitch", false, Rest);
                return;
            }
            transform.position = next;
            if (Time.time - fallStarted > (thrown ? FlightLongest : 4f)) ComeHere();   // lost somewhere: bring her back
        }

        // Her tumble slows in the air, and once she's slowed down or bounced off the floor she rights herself before she lands.
        private void Spin(float dt)
        {
            if (!CapriConfig.Motion) { spin = spinRate = 0; return; }
            spin += spinRate * dt;
            spinRate *= Mathf.Exp(-0.6f * dt);
            if (flyFloors == 0 && velocity.magnitude >= Calm) return;
            float off = spin - 360f * Mathf.Round(spin / 360f);
            spinRate += (-off * 60f - spinRate * 14f) * dt;
        }

        // Down after a flight: a hard one leaves her dizzy for a beat and gets a word out of her, and the third throw in two
        // minutes starts her sulk.
        private void Landed(bool wasThrown)
        {
            thrown = false;
            spin = spinRate = 0;
            if (!wasThrown) return;
            bool dizzy = flyHardest >= DizzyFrom;
            if (dizzy) { dizzyFrom = Time.time; dizzyUntil = Time.time + DizzyFor; }
            if (sulkLine) { sulkLine = false; Voice(Pick(SulkLines)); flightLine = true; }
            else if (dizzy && throwsAt.Count == 1) { Voice(Pick(DizzyLines)); flightLine = true; }
        }

        // Every throw counts toward her grudge: the second in two minutes gets a warning and the third starts her sulk. A hard
        // first one gets a yell as she flies.
        private void Grudge(float fast)
        {
            float now = Time.time;
            throwsAt.RemoveAll(at => now - at > SulkWindow);
            throwsAt.Add(now);
            if (throwsAt.Count >= SulkThrows) { throwsAt.Clear(); sulkSince = now; sulkBothered = now; dodgedOnce = false; sulkLine = true; return; }
            string line = throwsAt.Count == 2 ? Pick(WarnLines) : fast >= ThrowHard ? Pick(ThrowLines) : null;
            if (line != null) { Voice(line); flightLine = true; }
        }

        // Sulking, she won't be picked up: a grab makes her glitch a short hop away from your hand.
        private void Dodge(HandState hand)
        {
            sulkBothered = Time.time;
            if (!dodgedOnce) { dodgedOnce = true; dodgeSaidAt = Time.time; Voice(FirstDodgeLine); }
            else if (Time.time - dodgeSaidAt > DodgeSaid) { dodgeSaidAt = Time.time; Voice(Pick(DodgeLines)); }
            Vector3 away = RoomSense.Flat(transform.position - hand.Position);
            if (away.sqrMagnitude < 0.0001f) away = RoomSense.Flat(transform.position - hands.Head.position);
            Vector3 spot = room.FloorSpot(transform.position + away.normalized * 0.8f, 0.25f);
            GlitchTo(new Surface { Point = spot });
        }

        // A sorry, a head pat or five minutes alone ends her sulk.
        private void MakeUp(string[] lines)
        {
            sulkSince = -1;
            throwsAt.Clear();
            Voice(Pick(lines));
        }

        private void SitOn(Surface ledge)
        {
            standingOn = ledge;
            transform.position = ledge.Point;
            mode = Mode.Perched;
            seat = Seat.Sit1;
            // She stays a good while where you put her.
            perchUntil = Time.time + 2 * UnityEngine.Random.Range(secondsSittingOnFurniture.x, secondsSittingOnFurniture.y);
            nextBlink = Time.time + UnityEngine.Random.Range(2.5f, 6f);
            Play("perch_glitch", false, delegate { if (mode == Mode.Perched) Play(SeatClip, true); });
        }

        // ---------- Settings ----------

        public int SizeIndex
        {
            get
            {
                int best = 0;
                for (int i = 1; i < CapriConfig.SizeValues.Length; i++)
                    if (Mathf.Abs(CapriConfig.SizeValues[i] - size) < Mathf.Abs(CapriConfig.SizeValues[best] - size)) best = i;
                return best;
            }
        }

        public void SetSizeIndex(int index)
        {
            size = CapriConfig.SizeValues[Mathf.Clamp(index, 0, CapriConfig.SizeValues.Length - 1)];
            PlayerPrefs.SetFloat(SizeKey, size);
        }

        public void SettingsChanged()
        {
            speaker.Apply();
            nextWander = Mathf.Min(nextWander, Time.time + Gap(secondsBetweenWanders));
            nextPop = Time.time + Gap(CapriConfig.PopGaps[Mathf.Clamp(CapriConfig.PopIns, 0, 2)]);
            if (eyesStarted) eyes.Want(CapriConfig.Sight);
            nextGlance = -1;
            if (CapriConfig.Sight && !sawBefore)
                Say(!brain.Ready ? "Set up my brain under BRAIN, then I can look around." : !brain.CanSee ? "DeepSeek can't see pictures. Pick NVIDIA, OpenAI or Claude under BRAIN and I'll look." : "Eyes on. Ask me what I see, or turn on Watch me and I'll check in now and then.");
            sawBefore = CapriConfig.Sight;
        }

        public void ForgetChats()
        {
            brain.Forget();
            Say("Wiped. Who are you again? Kidding.");
        }

        public void TestVoice()
        {
            if (CapriConfig.VoiceService != 1 || string.IsNullOrEmpty(CapriConfig.FishKey)) { Say("Pick Fish Audio and add its key under VOICE first."); return; }
            if (string.IsNullOrEmpty(CapriConfig.FishVoiceId)) { Say("Pick my voice first: paste a fish.audio voice link under VOICE."); return; }
            Talk("Testing, testing. Do I sound preem from over here?");
        }

        private void ToggleMic()
        {
            CapriConfig.MicMuted = !CapriConfig.MicMuted;
            settings.Refresh();
            // Shown on the mic's own label: what she's saying keeps going, in her voice and her bubble.
            micTag.Say(CapriConfig.MicMuted ? "Mic off" : ears.Ready ? "Mic on" : "Mic on: add a Hearing key (Y > VOICE)", 2.5f);
        }

        // ---------- Conversation ----------

        // Shows a little "..." while you're talking, so you know she heard you start.
        private void Listen()
        {
            if (ears.Hearing && !wasHearing && !voice.isPlaying) bubble.Say("...", 1.5f);
            wasHearing = ears.Hearing;
        }

        private void Heard(string said)
        {
            lastAttention = Time.time;
            if (Sulking && SorryWords.IsMatch(said)) { MakeUp(SorryLines); return; }
            if (Command(said)) return;
            if (!brain.Ready) { Say("My brain's offline. Press Y and set one up under BRAIN."); return; }
            if (mode == Mode.Napping) { mode = Mode.Waking; Play("wake", false, Rest); }
            bubble.Say("...", 20);
            // Busy with a glance or a pop-in: what you said waits its turn instead of being dropped.
            if (brain.Busy) { waitingLine = said; return; }
            byte[] picture = null;
            if (Looking.IsMatch(said))
            {
                // Asked to look but she can't: she says why herself, instead of her brain guessing without a picture.
                if (!CapriConfig.Sight) { Say("I can't see yet. Press Y, then YOU, and turn on Let her see."); return; }
                if (!brain.CanSee) { Say("This brain can't see pictures. Pick NVIDIA, OpenAI or Claude under BRAIN and I'll look."); return; }
                picture = eyes.Snap(false);
                if (picture == null) { Say(eyes.WhyNot() ?? "Couldn't get a picture that time. Ask me again?"); return; }
                Debug.Log("[Capri] Looking: sent a " + picture.Length / 1024 + " KB picture");
            }
            brain.Reply(said, false, Talk, line => Say(line), picture);
        }

        // "Sit by me", "lay by me", "lay down", "sit up", "stand up", "come here": she does it right away. Sulking she
        // won't, and while you're holding or throwing her it's left for her brain to answer.
        private bool Command(string said)
        {
            bool layBy = LayByMe.IsMatch(said), sitBy = !layBy && SitByMe.IsMatch(said);
            bool layDown = !layBy && !sitBy && LayDown.IsMatch(said);
            bool sitUp = !layBy && !sitBy && !layDown && SitUp.IsMatch(said);
            bool standUp = !layBy && !sitBy && !layDown && !sitUp && StandUp.IsMatch(said);
            bool come = !layBy && !sitBy && !layDown && !sitUp && !standUp && ComeOver.IsMatch(said);
            if (!(layBy || sitBy || layDown || sitUp || standUp || come)) return false;
            if (mode == Mode.Held || mode == Mode.Falling || mode == Mode.GlitchingIn || mode == Mode.GlitchingOut || mode == Mode.Waiting) return false;
            if (Sulking) { sulkBothered = Time.time; Voice(Pick(DodgeLines)); return true; }
            if (mode == Mode.Napping || mode == Mode.Waking) { mode = Mode.Idle; }
            if (layBy) GoByYou(Seat.Lay);
            else if (sitBy) GoByYou(UnityEngine.Random.value < 0.5f ? Seat.Sit1 : Seat.Sit2);
            else if (layDown) LieHere();
            else if (sitUp) SitHere();
            else if (standUp) StandHere();
            else ComeToYou();
            return true;
        }

        // The ledge nearest you, then lie or sit on it. No ledge near you: she lies on the floor beside you, or for sitting
        // just comes and stands there.
        private void GoByYou(Seat want)
        {
            Vector3 you = hands.Head.position;
            Surface ledge = room.LedgeNearYou(you, stayWithin);
            commanded = true;
            if (ledge != null)
            {
                if (want == Seat.Lay && !RoomSense.FitsLying(ledge, size)) { want = UnityEngine.Random.value < 0.5f ? Seat.Sit1 : Seat.Sit2; Voice(Pick(NoRoomLines)); }
                else Voice(Pick(OnMyWayLines));
                GlitchTo(ledge, want);
                return;
            }
            Vector3 beside = you + RoomSense.Flat(hands.Head.forward).normalized * 0.7f + RoomSense.Flat(hands.Head.right).normalized * 0.4f;
            Voice(want == Seat.Lay ? "No furniture near you. Floor it is." : "Nowhere to sit near you, so I'll stand right here.");
            GlitchTo(new Surface { Point = room.FloorSpot(beside, 0.25f) }, want == Seat.Lay ? Seat.Lay : Seat.Sit1);
        }

        // "Lay down" where she is: on a ledge with room, or on the floor.
        private void LieHere()
        {
            bool edge = standingOn != null && standingOn.Edge;
            if (Lying) return;
            if (edge && !RoomSense.FitsLying(standingOn, size)) { Voice("Not enough room to lie down here."); return; }
            if (edge) transform.position = RoomSense.LayPoint(standingOn, size);
            if (standingOn != null && !standingOn.Edge) { Voice("Not here. Ask me to lay by you."); return; }
            seat = Seat.Lay;
            mode = Mode.Perched;
            perchUntil = Time.time + 2 * UnityEngine.Random.Range(secondsSittingOnFurniture.x, secondsSittingOnFurniture.y);
            Play("lay", true);
        }

        // "Sit up": from lying on a ledge back to sitting. On the floor there's nothing to sit on, so she stands.
        private void SitHere()
        {
            bool edge = standingOn != null && standingOn.Edge;
            if (edge) { seat = UnityEngine.Random.value < 0.5f ? Seat.Sit1 : Seat.Sit2; mode = Mode.Perched; Play(SeatClip, true); return; }
            if (Lying) { StandHere(); return; }
            Voice("Nothing to sit on here. Say sit by me.");
        }

        // "Stand up": off the floor she just gets up; on a ledge she hops down to the floor.
        private void StandHere()
        {
            bool edge = standingOn != null && standingOn.Edge;
            if (edge) { GlitchTo(new Surface { Point = room.FloorSpot(transform.position, 0.6f) }); return; }
            seat = Seat.Sit1;
            Rest();
        }

        // "Come here": close by on the floor she walks over, otherwise she glitches over to you.
        private void ComeToYou()
        {
            Vector3 front = hands.Head.position + RoomSense.Flat(hands.Head.forward).normalized * 0.8f;
            Vector3 spot = room.FloorSpot(front, 0.25f);
            bool onFloor = standingOn == null && !Lying;
            if (onFloor && RoomSense.Flat(spot - transform.position).magnitude < 3f)
            {
                Voice(Pick(OnMyWayLines));
                walkTarget = spot;
                mode = Mode.Walking;
                Play("walk", true);
                return;
            }
            ComeHere();
        }

        // Watch me: now and then she glances through your Quest's camera and says one short thing about what's in front of
        // you, like the desktop app's Watch my screen. Only when the view changed, she isn't busy, and you aren't talking
        // or holding her. A look she isn't sure of, or one that repeats an earlier line, stays unsaid.
        private void Glance()
        {
            int gap = CapriConfig.WatchSeconds[Mathf.Clamp(CapriConfig.Watch, 0, CapriConfig.WatchSeconds.Length - 1)];
            if (gap <= 0 || !CapriConfig.Sight || glancing) return;
            if (nextGlance < 0) nextGlance = Time.time + Mathf.Min(20, gap);
            if (Time.time < nextGlance) return;
            if (!eyes.Ready || !brain.Ready || !brain.CanSee || brain.Busy || speaker.Busy || voice.isPlaying || ears.Hearing || settings.IsOpen) return;
            if (mode == Mode.Held || mode == Mode.Falling || mode == Mode.Napping || mode == Mode.GlitchingIn || mode == Mode.GlitchingOut || mode == Mode.Waiting) return;
            nextGlance = Time.time + gap * UnityEngine.Random.Range(0.8f, 1.2f);
            byte[] shot = eyes.Snap(lastGlance >= 0 && Time.time - lastGlance < 300);
            if (shot == null) return;
            glancing = true;
            lastGlance = Time.time;
            glanced.RemoveAll(said => Time.time - said.Key > CapriGlances.Memory);
            List<string> recent = new List<string>();
            foreach (KeyValuePair<float, string> said in glanced) recent.Add(said.Value);
            brain.Look(shot, CapriGlances.Ask(recent), delegate (string raw)
            {
                glancing = false;
                string saw;
                string line = CapriGlances.Watched(raw, out saw);
                if (line.Length > 0 && CapriGlances.Repeats(line, recent)) line = "";
                if (saw != null) brain.Glanced(saw, line);
                if (line.Length == 0) return;
                glanced.Add(new KeyValuePair<float, string>(Time.time, line));
                Talk(line);
            }, delegate (string problem)
            {
                glancing = false;
                if (problem != null) Debug.Log("[Capri] Glance failed: " + problem);
            });
        }

        // She speaks up on her own now and then (Pop-ins).
        private void PopIn()
        {
            if (CapriConfig.PopIns == 0 || Time.time < nextPop) return;
            nextPop = Time.time + Gap(CapriConfig.PopGaps[Mathf.Clamp(CapriConfig.PopIns, 0, 2)]);
            if (!brain.Ready || brain.Busy || voice.isPlaying || ears.Hearing || mode == Mode.Napping || mode == Mode.Held || mode == Mode.Falling) return;
            brain.Reply(null, true, Talk, line => Debug.Log("[Capri] Pop-in failed: " + line));
        }

        // Says a line out loud with Fish Audio if it's set up, and always in her bubble.
        private void Talk(string line)
        {
            if (mode == Mode.Walking) Rest();   // she stops to talk
            if (!speaker.Ready) { Say(line); return; }
            bubble.Say("...", 20);
            speaker.Speak(line, delegate (AudioClip spoken) { PlayLine(line, spoken); }, delegate (string problem)
            {
                Say(line);
                if (problem != null) Debug.Log("[Capri] " + problem);
                if (problem != null) StartCoroutineLater(problem);
            });
        }

        private void StartCoroutineLater(string problem) { StartCoroutine(SayLater(problem, CapriConfig.BubbleSeconds[Mathf.Clamp(CapriConfig.Bubble, 0, 2)] + 0.5f)); }

        private System.Collections.IEnumerator SayLater(string line, float wait)
        {
            yield return new WaitForSeconds(wait);
            Say(line);
        }

        private static float Gap(Vector2 range) { return range == Vector2.zero ? float.MaxValue / 4 : UnityEngine.Random.Range(range.x, range.y); }

        // ---------- Talking ----------

        private void Greet()
        {
            int hour = DateTime.Now.Hour;
            string name = hour >= 5 && hour < 12 ? "morning" : hour >= 22 || hour < 5 ? "late" : "miss";
            string line = name == "morning" ? "Good morning, choom." : name == "late" ? "Isn't it late, choom?" : "Miss me, choom?";
            if (speaker.Ready) { Talk(line); return; }   // her greeting in your Fish Audio voice
            AudioClip hello = Resources.Load<AudioClip>("CapriVoice/hello-" + name);
            if (hello != null) PlayLine(line, hello);
            else Say(line);
        }

        // Her bubble, typed out at the desktop app's pace when there's no voice to keep time with.
        private const float LettersPerSecond = 28;

        public void Say(string line, float seconds = 0)
        {
            float setting = CapriConfig.BubbleSeconds[Mathf.Clamp(CapriConfig.Bubble, 0, 2)];
            float typing = line.Length / LettersPerSecond;
            bubble.Type(line, LettersPerSecond, Mathf.Max(seconds - typing, setting + line.Length * 0.03f - typing, 1f));
        }

        // Plays a spoken line and types it into her bubble at the pace she says it, like the desktop app.
        private void PlayLine(string line, AudioClip spoken)
        {
            voice.clip = spoken;
            voice.Play();
            if (mode == Mode.Idle && clip != null && clip.Name != "idle") Play("idle", true);
            float seconds = spoken.length / Mathf.Max(0.5f, voice.pitch);
            float letters = seconds < 0.2f ? LettersPerSecond : Mathf.Max(8, line.Length / seconds);
            bubble.Type(line, letters, 1.5f);
        }

        // Her own short lines (pats, pokes, throws and the rest) said out loud in her Fish Audio voice, like the desktop app.
        // Each one is only made once per voice and kept, so a pat sounds right away the next time. Without a voice set up,
        // or while she's already talking, they're typed in her bubble instead.
        private readonly Dictionary<string, AudioClip> spokenLines = new Dictionary<string, AudioClip>();
        private string spokenWith;

        private void Voice(string line)
        {
            if (!speaker.Ready || voice.isPlaying || speaker.Busy || brain.Busy) { Say(line); return; }
            string with = CapriConfig.FishVoiceId + "|" + CapriConfig.FishModel + "|" + CapriConfig.Speed;
            if (spokenWith != with) { spokenLines.Clear(); spokenWith = with; }
            AudioClip saved;
            if (spokenLines.TryGetValue(line, out saved) && saved != null) { PlayLine(line, saved); return; }
            bubble.Say("...", 4);
            speaker.Speak(line, delegate (AudioClip made)
            {
                spokenLines[line] = made;
                PlayLine(line, made);
            }, delegate (string problem)
            {
                Say(line);
                if (problem != null) Debug.Log("[Capri] " + problem);
            });
        }

        private void Mouth()
        {
            bool talking = voice.isPlaying && clip != null && clip.Name == "idle";
            mouth.enabled = talking;
            if (!talking || Time.time < nextMouthFrame) return;
            nextMouthFrame = Time.time + 1f / 12f;
            CapriClip shapes = CapriClips.Get("mouth");
            mouth.sprite = shapes.Frames[UnityEngine.Random.Range(0, shapes.Frames.Length)];
        }

        // ---------- Looks ----------

        private void FaceYou()
        {
            Vector3 away = RoomSense.Flat(transform.position - hands.Head.position);
            if (away.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(away, Vector3.up);
            bubble.transform.position = transform.position + Vector3.up * (size * (Lying ? 0.55f : Resting ? 0.75f : 1.08f) + 0.08f);
            bubble.Face(hands.Head.position);
            micTag.transform.position = bubble.transform.position + Vector3.up * 0.13f;
            micTag.Face(hands.Head.position);
        }

        private void PlaceShadow()
        {
            if (Time.time > nextShadowCheck)
            {
                nextShadowCheck = Time.time + 0.1f;
                shadowY = mode == Mode.Held || mode == Mode.Falling ? room.GroundBelow(transform.position + Vector3.up * 0.3f) : transform.position.y;
            }
            bool perched = (standingOn != null && standingOn.Edge) || Lying;
            shadow.enabled = sprite.enabled && !perched;
            shadow.transform.position = new Vector3(transform.position.x, shadowY + 0.004f, transform.position.z);
            float height = Mathf.Max(0, transform.position.y - shadowY);
            float spread = size * 0.55f * Mathf.Clamp(1 - height, 0.4f, 1f);
            shadow.transform.localScale = new Vector3(spread, spread * 0.5f, 1);
            shadow.color = new Color(0, 0, 0, 0.45f * Mathf.Clamp(1 - height, 0.2f, 1f));
        }

        private void SetVisible(bool on) { sprite.enabled = on; shadow.enabled = on; }

        private static Sprite BlobSprite()
        {
            const int n = 64;
            Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = new Vector2(x - n / 2f + 0.5f, y - n / 2f + 0.5f).magnitude / (n / 2f);
                    tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(1 - d * d)));
                }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
        }

        private static string Pick(string[] lines) { return lines[UnityEngine.Random.Range(0, lines.Length)]; }
    }
}
