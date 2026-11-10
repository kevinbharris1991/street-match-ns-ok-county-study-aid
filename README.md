# Street Match (N & S): OK County — Study Aid

A calm, stylish 2D Unity matching game for learning 40 work-related names and numbers.
Navy panels, mint accents, lavender number cards, soft sounds, and a no-timer approach
keep the focus on remembering rather than racing. No paid assets or external services.

## Open and play

1. Install **Unity 2022.3.62f3 LTS** through Unity Hub. The project targets this exact editor version.
   Include WebGL Build Support if you want a browser version.
2. In Unity Hub, choose **Add project from disk** and select this folder.
3. Let Unity import the project and resolve its packages.
4. Choose **Street Match (N & S): OK County — Study Aid > Open Game Scene**, then press **Play**.

The scene is intentionally lightweight: its UI, cards, camera, textures, and sounds
are constructed at runtime. Start this scene rather than a new, unnamed scene.
The first import configures the product title, window size, and legacy input module.
Use **Active Input Handling: Input Manager (Old)** if you change the input settings.
The launch screen shows the game title and this disclaimer: "Unofficial study aid,
not certified EMS training or an operational navigational tool." Press Enter or
tap/click it to start.

## How to play

- Study the pairs, then press **Ready? Start matching**.
- Drag a name onto its number **or** drag a number onto its name.
  Mouse and touch are supported. Alternatively, tap/click one card in each column.
- A correct pair disappears and earns **one point**. A mismatch leaves both cards
  available, with friendly feedback and no point penalty.
- Some supplied numbers appear more than once. Any visible card with the right
  number is valid; matching never depends on a hidden card identity.
- **Hint** (`H`) highlights a name and its correct number. **Escape** clears a
  selection; **Enter** activates the study/continue button.
- **Study again** returns to the preview. It starts a fresh round, while retaining
  points and learning progress already earned.
- Open **Menu** to return to the game or exit. In a browser build, choose **Exit game**
  and then close the browser tab; browsers do not let the game close its own tab.
- Set your name in **Menu** to personalize the round-complete message. Your name is
  saved locally on the device.

## Learning and levels

The eight levels introduce the list in groups of five, in the supplied order.
Levels 2, 3, and 4+ add respectively one, two, and three earlier pairs, chosen by
review priority. Boards contain no more than eight pairs. Complete a level to
unlock the next; previous levels remain replayable.

**Smart review** selects up to eight unlocked pairs. Due pairs come first, then
lower recall streaks and earlier due dates. Equal-priority ties are shuffled.
Review is always available, even when nothing is due yet.

**Mix all 40** unlocks when level 8 becomes available. It shuffles the complete
catalog into five boards of eight; each pair appears once per complete mix.

Three consecutive unaided recalls of a pair, in separate rounds, mark it
**mastered**. A miss or hint resets that pair's streak. Correcting a mistake
still earns the point, but is not counted as an unaided recall. A mastered pair
is a practice milestone, not a guarantee of long-term retention.

Review intervals after unaided recalls are **1 minute, 10 minutes, 1 day, then
3 days**. Hinted or previously missed pairs are due immediately. Studying before
each round is intentional; for a harder retrieval test, start matching without
reading the preview.

Progress is saved locally through Unity PlayerPrefs after matches, misses, and
hints. It includes lifetime points, unlocked levels, attempts, and recall history.
There is no account, analytics, server, or cross-device synchronization. Browser
storage must be enabled for WebGL persistence; private browsing or clearing site
data can remove saves. Save failures display a warning in the footer.

## Build

For a browser build, choose **Street Match (N & S): OK County — Study Aid > Build WebGL**. Output goes to
`Builds/WebGL`. Serve that directory using an HTTP server rather than opening
`index.html` directly, for example:

```sh
python3 -m http.server 8080 --directory Builds/WebGL
```

For desktop, use **File > Build Settings**, select your platform, and build.
The game scene is already included. Desktop and landscape tablet layouts are
recommended; portrait layouts also scale to the available screen and safe area.
The interface fits a minimum 1440 x 1000 logical layout within the usable screen,
including cutouts. Smaller windows scale the whole interface rather than clipping
actions. The splash artwork keeps its original aspect ratio.

An automated WebGL build can use:

```sh
Unity -batchmode -quit -projectPath "$PWD" \
  -executeMethod StreetMatch.Editor.GameProjectSetup.BuildWebGL \
  -logFile build.log
```

Unity and the requested platform module must be installed and licensed.

## Data and project structure

All 40 entries live in `Assets/Scripts/Core/GameRules.cs`. Original spelling and
numbers are preserved, including **Dobbs = 10000**, **Westministe**, **Midwest Blv**,
and **Cemetary/Garth Brooks**. Change display labels there if desired, but keep
pair IDs stable to preserve the meaning of existing saved progress.

- `Assets/Scripts/Core`: catalog, matching, progression, and review scheduling.
- `Assets/Scripts/Runtime`: UI, bidirectional drag/drop, animations, audio, saves.
- `Assets/Editor`: project setup and WebGL build command.
- `Assets/Tests/Editor`: shared rules regression suite and Unity test entry point.
- `Tests`: dependency-free .NET runner for the same core regression suite.
- `Assets/Art/StreetMatchIcon.png`: desktop app icon.
- `Assets/Resources/StreetMatchSplash.png`: in-game splash screen artwork.
- `Tools/generate_brand_art.py`: reproducibly generates the icon and splash artwork.

The scene, controller, and code assemblies use `StreetMatch` naming. Legacy
PlayerPrefs keys and the Unity company identifier remain unchanged only for
compatibility with existing local saves, sound preferences, and player names.
Rebuild and redistribute the game to apply source changes to an existing installation.

## Checks

In Unity, open **Window > General > Test Runner > EditMode** and run all tests.
Without Unity, the core suite can run with the .NET 10 SDK:

```sh
dotnet run --project Tests/GameRules.Tests.csproj
```

It checks the exact 40-entry catalog, randomized level/mix solvability, duplicate
values, single-point scoring, stale drops, zero, hints, mastery, review ordering,
and invalid progress.

Before distributing a build, check these Unity play-mode behaviors:

- Drag both directions, release outside a card, and try same-column drops.
- Swap same-number cards; remaining pairs must still be solvable.
- Try a wrong match, a hint, tap-to-match, sound toggle, and scene reload.
- Complete every level, inspect unlocks, then finish all five full-mix boards.
- Resize the window and test touch on your target device.
- Check Hint, Study again, and Continue after eight-pair rounds and long player names.
- Run the EditMode layout tests for flexible controls and eight-pair study screens.
- Reload a hosted WebGL build to confirm browser save persistence.

The rules suite can run without Unity, but it does not replace Unity compilation,
play-mode checks, or a target-platform build.
