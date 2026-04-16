---
description: Bootstrap, audit, and debug a Board Unity Android project
argument-hint: [optional repo details or game idea]
---

Use the Board project bootstrap workflow for this repository.

Follow the Board-specific guidance in @{{CLAUDE_HOME}}/board-project/board-command-guide.md.

If this repo does not already have a Board-specific `CLAUDE.md`, create one using the installed templates or by running the installed bootstrap script:

- `{{CLAUDE_HOME}}/board-project/templates/CLAUDE.md.template`
- `{{CLAUDE_HOME}}/board-project/templates/board-project-guide.md.template`
- `{{CLAUDE_HOME}}/board-project/templates/board-project-checklist.md.template`
- `{{CLAUDE_HOME}}/board-project/scripts/bootstrap_board_project.py`

Priorities:

1. Verify Board project setup first
2. Prefer Board SDK APIs over generic Unity substitutes
3. Use `contactId` instead of `glyphId` for live piece identity
4. Check `BoardUIInputModule` whenever UI input is involved
5. Prefer simulator-first debugging before hardware-only conclusions
6. Ensure the repo ends up with durable project memory in `CLAUDE.md`

User request details:

$ARGUMENTS
