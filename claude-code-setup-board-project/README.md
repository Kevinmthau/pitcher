# Claude Code Board Bootstrap

This package adapts the `setup-board-project` Codex skill into Claude Code's current customization model:

- a user-level slash command in `~/.claude/commands/`
- a reusable project `CLAUDE.md` template
- Board reference files for project memory
- helper scripts to install the package and bootstrap a new Board repo

## What Claude Code Uses

- Custom slash commands live in `~/.claude/commands/` or `.claude/commands/`
- Project memory lives in `./CLAUDE.md`
- `CLAUDE.md` can import additional files with `@path/to/file`

## Install

Run:

```bash
python3 scripts/install_claude_code_board_bootstrap.py
```

That installs:

- `~/.claude/commands/setup-board-project.md`
- `~/.claude/board-project/board-command-guide.md`
- `~/.claude/board-project/templates/CLAUDE.md.template`
- `~/.claude/board-project/templates/board-project-guide.md.template`
- `~/.claude/board-project/templates/board-project-checklist.md.template`
- `~/.claude/board-project/scripts/bootstrap_board_project.py`

## Use

For a new Board repo:

1. Create the Unity project
2. Import the Board SDK
3. Run `Board > Configure Unity Project...`
4. Load and select the piece model
5. Open the repo in Claude Code
6. Run `/setup-board-project`

You can also bootstrap the project files before opening Claude Code:

```bash
python3 ~/.claude/board-project/scripts/bootstrap_board_project.py \
  --repo /path/to/repo \
  --package-id com.yourcompany.yourgame \
  --game-name "Your Game" \
  --piece-model arcade_v1.3.7.tflite \
  --build-script Assets/Editor/BuildYourGame.cs \
  --apk-path Builds/Android/YourGame.apk
```
