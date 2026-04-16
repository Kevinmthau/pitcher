#!/usr/bin/env python3
"""Install the Claude Code Board bootstrap package into ~/.claude."""

from __future__ import annotations

import argparse
import shutil
from pathlib import Path


def render_template(src: Path, replacements: dict[str, str]) -> str:
    text = src.read_text(encoding="utf-8")
    for key, value in replacements.items():
        text = text.replace(key, value)
    return text


def copy_file(src: Path, dst: Path) -> None:
    dst.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(src, dst)
    print(f"[OK] Installed {dst}")


def write_file(dst: Path, content: str) -> None:
    dst.parent.mkdir(parents=True, exist_ok=True)
    dst.write_text(content, encoding="utf-8")
    print(f"[OK] Installed {dst}")


def copy_tree(src: Path, dst: Path) -> None:
    if dst.exists():
        shutil.rmtree(dst)
    shutil.copytree(src, dst)
    print(f"[OK] Installed {dst}")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Install the Board bootstrap slash command and templates into Claude Code."
    )
    parser.add_argument(
        "--claude-home",
        default="~/.claude",
        help="Claude Code home directory, default: ~/.claude",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()

    package_root = Path(__file__).resolve().parent.parent
    claude_home = Path(args.claude_home).expanduser().resolve()
    commands_dir = claude_home / "commands"
    board_dir = claude_home / "board-project"

    command_content = render_template(
        package_root / "commands" / "setup-board-project.md",
        {"{{CLAUDE_HOME}}": str(claude_home)},
    )
    write_file(commands_dir / "setup-board-project.md", command_content)
    copy_file(
        package_root / "references" / "board-command-guide.md",
        board_dir / "board-command-guide.md",
    )
    copy_tree(package_root / "templates", board_dir / "templates")
    copy_tree(package_root / "scripts", board_dir / "scripts")

    print("")
    print("Installed Claude Code Board bootstrap.")
    print("Next steps:")
    print("1. Open a Board repo in Claude Code")
    print("2. Run /setup-board-project")
    print(f"3. Or pre-generate CLAUDE.md with {board_dir / 'scripts' / 'bootstrap_board_project.py'}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
