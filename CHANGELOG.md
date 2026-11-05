# Capri Room patch notes

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
