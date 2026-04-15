---
name: board-unity
summary: Build, debug, and review Unity projects that target the Board platform and SDK.
---

# Board Unity

Use this skill when working on a Unity project that targets **Board** and uses the Board SDK.

Board is not a generic touch-tablet target. It has Board-specific project setup, piece recognition, simulator tooling, UI input handling, deployment, and platform APIs.

Docs: `https://docs.dev.board.fun`

## Use this skill for

- setting up a new Board Unity project
- debugging Board SDK install or package issues
- implementing piece or finger interactions
- fixing UI touch issues on Board hardware
- debugging piece recognition problems
- reviewing player/session/save integration
- preparing APK builds and Board deployment flow
- reviewing performance issues specific to Board hardware

## Immediate assumptions

Treat the project as Board unless the repo clearly proves otherwise.

Default constraints:

- Unity 2022.3 LTS+ or Unity 6
- Android target
- Android API 33
- IL2CPP
- ARM64
- Input System enabled
- Landscape Left
- Board SDK installed from `.tgz`
- deployment through `bdb`

## Repo-specific fast path

- Build helper: `Assets/Editor/BuildNewsroom.cs`
- APK output: `Builds/Android/Newsroom.apk`
- Current package id: `com.defaultcompany.newsroom`
- Current Board piece model: `arcade_v1.3.7.tflite`
- Local `bdb` fallback: `Tools/bdb`

## Critical Board rules

### 1. Use the setup wizard first

The preferred fix for project misconfiguration is:

- `Board > Configure Unity Project...`
- `Apply Selected Settings`

That wizard configures platform settings, API levels, scripting backend, and Input System.

### 2. Use explicit namespaces

```csharp
using Board.Core;
using Board.Input;
using Board.Session;
using Board.Save;
```

Do not use:

```csharp
using Board;
```

### 3. Track pieces by `contactId`

This is the most important Board input rule.

- `glyphId` = which piece type
- `contactId` = which live piece instance / contact

If you use `glyphId` to track an individual piece, the logic is probably wrong.

### 4. UI needs `BoardUIInputModule`

If UI should respond to finger touches on actual Board hardware, make sure the scene `EventSystem` uses `BoardUIInputModule`.

Fast fix:

- `Board > Input > Add BoardUIInputModule to EventSystems`

### 5. Piece recognition depends on the right model

Board piece detection requires the correct `.tflite` model.

Check:

- `Edit > Project Settings > Board > Input Settings`
- `Load Available Models`
- selected piece model matches the intended piece set

Models are stored in `Assets/StreamingAssets/`.

### 6. Prefer simulator before hardware deploy

Use:

- `Board > Input > Simulator`

The simulator reproduces Board input in editor and should be the default path for interaction iteration.

### 7. Force 60fps unless there is a reason not to

```csharp
void Awake()
{
    Application.targetFrameRate = 60;
}
```

Board hardware runs at 60Hz. A 30fps Android default will feel wrong.

## Recommended workflow

### New project setup

1. Confirm Unity version and Android Build Support
2. Install Board SDK from tarball
3. Run Board project setup wizard
4. Restart editor if Input System changes require it
5. Download piece model in Board Input Settings
6. Import the sample scene
7. Open simulator and verify contacts
8. Build APK
9. Deploy with `bdb`

### Debugging input bugs

1. Verify project setup
2. Verify orientation is Landscape Left
3. Verify Input System is enabled
4. Verify piece model is configured
5. Reproduce in simulator
6. Compare against Board sample scene
7. Reproduce on hardware
8. Inspect gameplay code last

### Debugging UI bugs

1. Check for `BoardUIInputModule`
2. Confirm issue occurs on hardware and not only editor
3. Check for mixed input module setup conflicts
4. Confirm UI is reacting to fingers, not piece contacts

### Debugging deploy issues

1. Confirm Android APK build exists
2. Confirm `bdb` is on PATH
3. Run `bdb help` or `bdb status`
4. Verify Board is connected over USB-C
5. Install with `bdb install`
6. Launch with correct package name
7. Stream logs if launch fails

### macOS build and deploy caveats

- A downloaded `bdb` binary may not execute until `chmod +x` is applied and the macOS quarantine flag is removed.
- If `bdb` fails from `Desktop`, copy it into the repo or another non-Desktop location and run it from there.
- Do not assume an escalated shell inherits the user's normal PATH; use the full `bdb` path if needed.
- On this machine, `/usr/local/bin` is root-owned, so repo-local `Tools/bdb` is a practical fallback.
- `bdb install` may succeed while the first `bdb launch` fails with `Serial port busy` or `BDB service not responding`; retry after `bdb status` reports `ready`.

### Unity CLI build caveats

- `-batchmode -executeMethod` can fail at licensing startup even when the machine already has a valid Unity entitlement license.
- If the Unity log shows `com.unity.editor.headless`, repeated licensing reconnects, or `Another instance of Unity.Licensing.Client is already running`, check for and kill stale `Unity.Licensing.Client` processes before retrying.
- For this repo, the reliable non-interactive path is to launch the editor normally with `-newsroomAutoBuild`; the startup hook in `BuildNewsroom.cs` builds the Android APK and exits.
- Successful invocation pattern:

```bash
/Applications/Unity/Hub/Editor/6000.4.2f1/Unity.app/Contents/MacOS/Unity \
  -projectPath /Users/kevinthau/Board-demo \
  -newsroomAutoBuild \
  -logFile /tmp/newsroom-autobuild.log
```

- Success markers in the log:
- `Build Finished, Result: Success.`
- `Newsroom APK built at /Users/kevinthau/Board-demo/Builds/Android/Newsroom.apk`

## Quick code patterns

### Read Board contacts

```csharp
using Board.Input;

BoardContact[] contacts = BoardInput.GetActiveContacts();
BoardContact[] pieces = BoardInput.GetActiveContacts(BoardContactType.Glyph);
BoardContact[] fingers = BoardInput.GetActiveContacts(BoardContactType.Finger);
```

### Track pieces across frames

```csharp
using System.Collections.Generic;
using System.Linq;
using Board.Input;
using UnityEngine;

public class PieceTracker : MonoBehaviour
{
    private readonly Dictionary<int, GameObject> trackedPieces = new();
    [SerializeField] private GameObject[] piecePrefabs;

    void Update()
    {
        var contacts = BoardInput.GetActiveContacts(BoardContactType.Glyph);
        var activeIds = new HashSet<int>();

        foreach (var contact in contacts)
        {
            activeIds.Add(contact.contactId);

            if (contact.phase == BoardContactPhase.Began)
            {
                var obj = Instantiate(piecePrefabs[contact.glyphId]);
                trackedPieces[contact.contactId] = obj;
            }

            if (trackedPieces.TryGetValue(contact.contactId, out var objRef))
            {
                objRef.transform.position = ScreenToWorld(contact.screenPosition);
                objRef.transform.rotation = Quaternion.Euler(0f, 0f, -contact.orientation * Mathf.Rad2Deg);
            }
        }

        foreach (var id in trackedPieces.Keys.ToList())
        {
            if (!activeIds.Contains(id))
            {
                Destroy(trackedPieces[id]);
                trackedPieces.Remove(id);
            }
        }
    }

    private Vector3 ScreenToWorld(Vector2 screenPosition)
    {
        var point = Camera.main.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 10f));
        return new Vector3(point.x, point.y, 0f);
    }
}
```

### Detect piece touch

```csharp
foreach (var contact in BoardInput.GetActiveContacts(BoardContactType.Glyph))
{
    if (contact.isTouched)
    {
        // a finger is touching this piece
    }
}
```

### Startup frame rate

```csharp
using UnityEngine;

public class StartupConfig : MonoBehaviour
{
    void Awake()
    {
        Application.targetFrameRate = 60;
    }
}
```

## Review checklist

When reviewing Board code, check for:

- wrong identity model using `glyphId`
- lack of `BoardContactPhase` handling
- missing conversion from radians to degrees
- standard Unity UI input module where Board UI input module is required
- missing Board model file or wrong model selection
- failure to use simulator for iteration
- Android orientation not locked to Landscape Left
- Android build target not configured correctly
- 30fps default left in place
