# AGENTS.md

This repository targets **Board**, a tabletop game platform built on **Unity + Android** with Board-specific input, session, save, and pause APIs.

## What you should assume

- This project uses the **Board Unity SDK**.
- Board projects should be treated as **Unity Android** projects, not generic desktop Unity projects.
- The correct docs source is: `https://docs.dev.board.fun`
- Prefer Board SDK APIs and patterns over generic Unity substitutes when Board functionality exists.

## Non-negotiable Board setup rules

When checking or modifying project setup, verify these first:

- **Unity**: 2022.3 LTS or later. Unity 6 is supported.
- **Platform**: Android.
- **Minimum API Level**: Android 13 / API 33.
- **Target API Level**: Android 13 / API 33.
- **Scripting Backend**: IL2CPP.
- **Architecture**: ARM64 required.
- **Input System**: Unity Input System 1.7.0+ enabled.
- **Orientation**: **Landscape Left** only.
- **Unity 6**: Application Entry Point must be **Activity**, not Game Activity.

If any of these are wrong, fix them before doing deeper debugging.

## First thing to do in a fresh Board project

Assume the developer should run:

1. `Board > Configure Unity Project...`
2. `Apply Selected Settings`
3. Restart the editor if prompted by Input System changes.
4. Open `Edit > Project Settings > Board > Input Settings`
5. Click `Load Available Models`
6. Select and download the correct **Piece Set Model**

The setup wizard should be the default recommendation before manual fixes.

## Required namespaces

Use Board namespaces explicitly:

```csharp
using Board.Core;
using Board.Input;
using Board.Session;
using Board.Save;
```

Do **not** use:

```csharp
using Board;
```

## Core Board systems

### Input
Use `BoardInput` for real Board contacts.

Common API shape:

```csharp
BoardContact[] contacts = BoardInput.GetActiveContacts();
BoardContact[] pieces = BoardInput.GetActiveContacts(BoardContactType.Glyph);
BoardContact[] fingers = BoardInput.GetActiveContacts(BoardContactType.Finger);
```

Important contact fields:

- `contactId`: unique per active contact
- `glyphId`: piece type id, not unique per individual piece
- `screenPosition`
- `orientation` in radians
- `phase`
- `isTouched`
- `type`

### Tracking rule
Track physical pieces by **`contactId`**, not `glyphId`.

- `glyphId` identifies the kind of piece
- `contactId` identifies the specific live contact

If you need to persist piece instances across frames, key your dictionaries and lookup tables by `contactId`.

### UI input
On Board hardware, Unity's standard `InputSystemUIInputModule` is not enough for touch UI.
Use **`BoardUIInputModule`** on your `EventSystem`.

Preferred fix:

- `Board > Input > Add BoardUIInputModule to EventSystems`

If UI buttons work in editor but not on Board hardware, check this first.

### Piece recognition
Board pieces use a Board-specific `.tflite` piece model.

- Models live in `Assets/StreamingAssets/`
- The active model is configured in `BoardInputSettings`
- If piece detection fails, verify the correct model is selected before assuming gameplay code is wrong

### Sessions and players
Use `BoardSession` for profile-aware multiplayer and player management. Do not invent separate local-player assumptions without a good reason.

### Save data
Use `BoardSaveGameManager` and Board save metadata for on-device Board save integration.

### Pause flow
Use Board pause-screen integration through `BoardApplication` instead of building only a custom pause path.

## Performance defaults

Set this early in startup unless there is a deliberate reason not to:

```csharp
Application.targetFrameRate = 60;
```

Board runs at 60Hz. Unity's Android default can be 30fps, which makes Board games feel laggy.

## Simulator-first workflow

For iteration, prefer testing Board input in the Unity editor with:

- `Board > Input > Simulator`

The simulator can generate piece and finger events without hardware. Use it before deploying, especially for logic bugs and iteration on interactions.

## Sample-driven development

The SDK includes a sample scene. When creating or debugging Board-specific interactions, check whether the sample already demonstrates the pattern.

Expected sample path after import:

`Assets/Samples/Board SDK/<version>/Input/Scenes/BoardInput.unity`

Use the sample to validate whether a problem is:

- project setup
- piece model configuration
- Board input handling
- game-specific logic

## Build and deploy expectations

Board projects build to an **APK** and deploy with **bdb**.

Typical loop:

```bash
bdb status
bdb install path/to/game.apk
bdb launch com.yourcompany.yourgame
bdb logs com.yourcompany.yourgame
bdb stop com.yourcompany.yourgame
bdb list
```

If deployment fails, check:

- APK actually built for Android
- package name is correct
- Board is connected over USB-C
- `bdb` is installed and on PATH
- Board OS is new enough for `bdb`

## Repo-specific build and deploy notes

- The current app package id is `com.defaultcompany.newsroom`.
- The current Newsroom Android build helper is `Assets/Editor/BuildNewsroom.cs`.
- That script outputs the APK to `Builds/Android/Newsroom.apk`.
- The current prototype uses the Arcade piece model `arcade_v1.3.7.tflite`.
- A local `bdb` binary under `Tools/bdb` is an acceptable fallback if `/usr/local/bin` is root-owned or unavailable.

### macOS `bdb` caveats

- A browser-downloaded `bdb` binary may keep the macOS quarantine flag and fail with `permission denied` or similar execution errors.
- If that happens, run `chmod +x` and clear quarantine with `xattr -d com.apple.quarantine`.
- On this machine, executing `bdb` from `Desktop` was unreliable; copying it into the repo (for example `Tools/bdb`) worked.
- Do not assume an escalated shell will inherit the user's normal PATH. Use an explicit binary path when needed.

### Unity build automation caveats

- On this machine, `Unity -batchmode -executeMethod ...` can stall or fail during licensing startup even when the machine has a valid entitlement license.
- If the Unity log shows `com.unity.editor.headless`, `Another instance of Unity.Licensing.Client is already running`, or repeated licensing reconnects, check for a stale `Unity.Licensing.Client` process first.
- A stale licensing client can hold the `Unity-LicenseClient-<user>` mutex and break subsequent command-line builds until it is killed.
- If `-executeMethod` is unreliable, the repo supports a regular-editor auto-build path with the `-newsroomAutoBuild` argument, which triggers `BuildNewsroom` on editor startup and exits on completion.
- The successful automated build path for this repo was:

```bash
/Applications/Unity/Hub/Editor/6000.4.2f1/Unity.app/Contents/MacOS/Unity \
  -projectPath /Users/kevinthau/Board-demo \
  -newsroomAutoBuild \
  -logFile /tmp/newsroom-autobuild.log
```

- Wait for `Build Finished, Result: Success.` and `Newsroom APK built at .../Builds/Android/Newsroom.apk`.

### Post-install launch caveats

- `bdb install` can succeed while the first `bdb launch` fails with `Serial port busy` or `BDB service not responding`.
- If that happens, wait a few seconds, run `bdb status` until it reports `ready`, then retry `bdb launch`.

## How to debug Board issues

When investigating a bug, work in this order:

1. Verify Board project setup
2. Verify correct piece model is installed
3. Verify `BoardUIInputModule` if UI is involved
4. Reproduce in the simulator
5. Reproduce on hardware
6. Compare behavior to SDK sample scene
7. Only then conclude the issue is in gameplay code

## Code generation guidelines

When writing Board gameplay code:

- Prefer small MonoBehaviours with clear frame-by-frame contact handling
- Use `BoardContactPhase` explicitly
- Convert `orientation` from radians when applying Unity rotations
- Keep editor-only simulation helpers separate from runtime Board logic where possible
- Avoid desktop-only assumptions like mouse-first interaction models
- Avoid adding third-party abstractions unless there is clear project value

## When asked to add a new Board mechanic

Default implementation plan:

1. Identify whether it is based on fingers, pieces, or both
2. Read from `BoardInput.GetActiveContacts(...)`
3. Track state by `contactId`
4. Map `screenPosition` into world or board-space coordinates
5. Use simulator to verify behavior
6. Deploy to hardware for final validation

## What to mention in reviews

In code reviews and refactor suggestions, pay special attention to:

- incorrect use of `glyphId` for identity
- missing `BoardUIInputModule`
- missing or wrong piece model
- 30fps cap on Android
- wrong orientation
- old Unity input usage that bypasses Board systems
- hardcoded assumptions that ignore multiple simultaneous contacts

## Sources of truth

When in doubt, use these pages first:

- Quick Start
- Setup Reference
- Build & Deploy
- Touch
- Pieces
- Simulator
- AI Assistant Setup

Docs root: `https://docs.dev.board.fun`
