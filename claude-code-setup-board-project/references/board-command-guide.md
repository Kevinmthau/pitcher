# Board Command Guide

Use this guidance whenever Claude Code runs `/setup-board-project`.

## Default Plan

1. Treat the project as a Unity Android Board SDK project
2. Read any existing `CLAUDE.md`, `AGENTS.md`, or repo notes before making assumptions
3. Verify required Board setup before debugging gameplay code
4. Create or update project memory files if they are missing or weak
5. Apply Board-specific coding rules and debugging heuristics

## Required Board Setup Checks

Verify these first:

- Unity `2022.3 LTS` or later
- Platform `Android`
- Minimum API `33`
- Target API `33`
- Scripting Backend `IL2CPP`
- Architecture `ARM64`
- Unity Input System `1.7.0+`
- Orientation `Landscape Left`
- Unity 6 entry point `Activity`, not `Game Activity`

Recommend this setup path before manual fixes:

1. Run `Board > Configure Unity Project...`
2. Click `Apply Selected Settings`
3. Restart the editor if prompted by Input System changes
4. Open `Edit > Project Settings > Board > Input Settings`
5. Click `Load Available Models`
6. Download and select the correct piece model

## Code Rules

Use explicit Board namespaces:

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

Input rules:

- Read contacts with `BoardInput.GetActiveContacts(...)`
- Track live pieces by `contactId`, not `glyphId`
- Treat `glyphId` as piece type, not unique piece identity
- Use `BoardContactPhase` explicitly
- Convert `orientation` from radians before applying Unity rotations

UI and runtime rules:

- Use `BoardUIInputModule` on the active `EventSystem`
- Set `Application.targetFrameRate = 60` unless there is a documented exception
- Avoid mouse-first or desktop-first assumptions

## Debug Order

1. Verify project setup
2. Verify the selected piece model
3. Verify `BoardUIInputModule` if UI is involved
4. Reproduce in the simulator
5. Reproduce on hardware
6. Compare behavior against the Board sample scene
7. Only then conclude the bug is in gameplay code

## Durable Project Memory

If project memory is missing, create:

- `CLAUDE.md`
- `docs/board-project-guide.md`
- `docs/board-project-checklist.md`

Prefer a `CLAUDE.md` that imports the two docs rather than putting everything inline.
