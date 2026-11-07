# Capri Room patch notes

## v1.0.5 (2026-11-06)

**Added**
- Two new ways to rest, with placeholder art until Blank's sprites are in: lying down (her dark leg swings slowly up and down) animated at 60 frames a second, and a second sitting pose with one leg along the ledge and the other hanging over it. Her mouth moves when she talks in both, sitting right where the anime puts it. Each time she teleports to a ledge she picks lying, her original sit or the new sit, a third each. If the ledge is too short to lie on, she sits. Lying on a ledge, she rests a little way in from the edge, and always lies along it: she turns to face you, but never more than 20 degrees off the edge's line.
- Voice commands she does right away, without her brain:
  - "Lay by me" / "sit by me": she goes to the ledge nearest you and lies or sits on it. With no furniture near you, she lies on the floor beside you or stands there.
  - "Lay down", "sit up", "stand up": she changes how she's resting where she is.
  - "Come here": she walks over if she's close on the floor, or glitches over to you.
  - Sulking, she won't take orders until you say sorry.

**Changed**
- Pats now need your hand to go side to side over her head. Just touching her head isn't a pat any more.
- Pokes now need a quick jab into her. A slow touch isn't a poke.
- Muting or unmuting the mic (X) shows on its own small label above her bubble, and no longer cuts off what she's saying.

**Fixed**
- Hand tracking: pats and pokes use your palm and index fingertip instead of your wrist, so petting her with your bare hand works.
- If something goes wrong handling what you said, she tells you instead of going deaf, and keeps listening. If one of her poses fails to load, she uses a pose she has and says which one broke.
- "Lay down" left her sitting: Unity's incremental build shipped a stale copy of her old pose list, without lay or sit 2. Release builds are now always clean. Her lying sheet is also smaller (3976x3420), and if a pose ever fails to load she tells you instead of quietly sitting.

## v1.0.4 (2026-11-05)

**Added**
- Let her see (Y → YOU): Capri can look through your Quest 3's camera. Ask "what do you see?", "look at this" or "what am I holding?" and she answers about what's in front of you. It's off until you turn it on, and the headset asks you to allow its camera. She sees your real room, not herself.
- Watch me (Y → YOU, under Let her see): every 30 seconds to 5 minutes she glances at what you're doing and says one short thing about it, like the desktop app's Watch my screen. She only looks when the view has changed and you aren't talking to her. She keeps quiet when she isn't sure or has nothing new to say, and she remembers what she saw if you ask.
- Seeing needs a brain that takes pictures: NVIDIA (the default), OpenAI or Claude. DeepSeek can't.
- What you say while she's busy with a glance waits its turn instead of getting lost.

**Fixed**
- Fish Audio "out of credit" (HTTP 402):
  - Fish's hearing (speech to text) has no free tier, so it always answered 402 without prepaid API credit. A fish.audio plan doesn't include API credit. She now says so and points you to Groq (free). She no longer retries Fish a second time, which cost another 402.
  - Her voice is free only with Fish's s2.1-pro-free model. Any other model name, even a typo or a stray space, made Fish quietly use its paid model and answer 402. Fish model is now a choice (Free, or the paid ones), and anything unknown falls back to Free.
  - Clearer messages when Fish says no (402, 429 and others), with Fish's own reason.
- "Let her see": the headset camera prompt could get lost, so she never got the camera and her brain answered "I can't see" without a picture. She now asks again whenever you ask her to look. When she can't look, she tells you why herself: Let her see is off, the brain can't take pictures, the camera isn't allowed yet, or her eyes are still starting.
- She notices more ways of asking her to look: "can you see me?", "do you see this?", "what am I wearing?".
- Hearing: Fish Audio's hearing has no free tier. The Free model only covers her voice. The Hearing choices now read Groq (free), Fish Audio (paid) and OpenAI (paid). If Fish turns hearing down for credit (HTTP 402) and you've added a Groq key, she switches Hearing to Groq and hears you again.
- Capri → Debug → Allow camera on headset grants the camera over USB. The Headset report now includes the passthrough camera log.

## v1.0.3 (2026-11-04)

**Added**
- Her speech bubble types out her words as she says them, in time with her voice, like the desktop app. When she isn't speaking out loud, it types at 28 letters a second.
- She says her own lines out loud: pats, pokes, being picked up, thrown, landing, dizzy, sulking, making up and "Coming, choom." These use your Fish Audio voice, like the desktop app. Each line is made once and kept, so it plays straight away the next time. Without a Fish voice set up, they stay in her bubble.

## v1.0.2 (2026-11-04)

**Added**
- Throwing, like the desktop app (Capri 0.8.3). Let go while your hand's moving fast and she flies at your throw's speed:
  - She tumbles the way you threw her, faster the harder it is, then rights herself before she lands.
  - She bounces off walls, the ceiling, the sides of furniture and the floor.
  - A hard throw gets a yell ("WHEEEE!"), and a hard landing leaves her dizzy for a moment.
  - You can catch her mid-air.
  - Her grudge: a second throw within two minutes gets a warning, and a third makes her sulk. While she sulks she dodges your hand. Say sorry, pat her head, or leave her alone for five minutes to make up.
- Motion setting (Y → MOVES): turn it off and she doesn't spin or sway.

**Fixed (from Lachy's review of the port)**
- No default voice: she only talks out loud in a Fish Audio voice you picked. With no voice set, she shows her lines in her bubble and asks you to pick one.
- Clear then Done in the keyboard now really removes a saved key.
- Her voice reader stops at a broken WAV chunk instead of looping forever.

**Removed**
- The Unity menu that downloaded and installed the APK from GitHub releases. Capri → Release → 3. Install built APK on headset installs your own build instead.

## v1.0.1 (2026-11-03)

**Fixed**
- Capri now sees your furniture. Space Setup saves desks, tables, couches and beds as 3D boxes, and she only looked for flat surfaces, so she never found any ledges. She now uses the top of each box.
- Putting her down on a desk no longer drops her to the floor. She dangles below your hand, so her feet could already be inside the desk when you let go, and the ground check missed it.

**Changed**
- Every flat-topped object in your room scan counts as a ledge: tables, desks, couches, beds, shelves, screens, lamps, "other" and unlabeled objects. The room itself (floor, ceiling, walls, doors, windows, wall art) doesn't. Tops smaller than about a hand are skipped.
- Landing anywhere on furniture makes her sit on its nearest edge, within 60 cm (was 20 cm).

**Added (Unity editor tools)**
- Capri → Release → 3. Test sideload from GitHub: downloads the published APK and installs it on a USB-connected Quest.
- Capri → Release → Install built APK on headset: installs your local build before it's published.
- Capri → Debug → Headset report: reads Capri Room's permissions and log from the headset (read-only).

## v1.0.0 (2026-11-03)

First release: Capri in your room on Quest 3 and 3S, with talking, settings (Y), mic mute (X), come here (B) and ledge sitting.
