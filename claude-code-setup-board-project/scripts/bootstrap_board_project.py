#!/usr/bin/env python3
"""Write Claude Code Board project memory files into a repo."""

from __future__ import annotations

import argparse
from pathlib import Path


def render_template(template_path: Path, replacements: dict[str, str]) -> str:
    text = template_path.read_text(encoding="utf-8")
    for key, value in replacements.items():
        text = text.replace(key, value)
    return text


def write_file(path: Path, content: str, force: bool) -> None:
    if path.exists() and not force:
        raise FileExistsError(f"{path} already exists. Re-run with --force to overwrite.")
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Write CLAUDE.md and Board project docs into a repository."
    )
    parser.add_argument("--repo", required=True, help="Target repository root")
    parser.add_argument("--package-id", required=True, help="Android package id")
    parser.add_argument("--game-name", required=True, help="Human-readable game name")
    parser.add_argument(
        "--piece-model",
        default="[set your piece model here]",
        help="Board piece model filename",
    )
    parser.add_argument(
        "--build-script",
        default="[set your Unity build helper here]",
        help="Project-relative path to the Unity build helper",
    )
    parser.add_argument(
        "--apk-path",
        default="Builds/Android/[YourGame].apk",
        help="Project-relative APK output path",
    )
    parser.add_argument(
        "--bundle-id",
        default="",
        help="Optional bundle id override for launch examples; defaults to package id",
    )
    parser.add_argument(
        "--force",
        action="store_true",
        help="Overwrite existing generated files",
    )
    return parser.parse_args()


def main() -> int:
    args = parse_args()

    script_dir = Path(__file__).resolve().parent
    template_dir = script_dir.parent / "templates"
    repo_dir = Path(args.repo).expanduser().resolve()
    bundle_id = args.bundle_id or args.package_id

    replacements = {
        "{{GAME_NAME}}": args.game_name,
        "{{PACKAGE_ID}}": args.package_id,
        "{{BUNDLE_ID}}": bundle_id,
        "{{PIECE_MODEL}}": args.piece_model,
        "{{BUILD_SCRIPT}}": args.build_script,
        "{{APK_PATH}}": args.apk_path,
    }

    outputs = [
        (template_dir / "CLAUDE.md.template", repo_dir / "CLAUDE.md"),
        (
            template_dir / "board-project-guide.md.template",
            repo_dir / "docs" / "board-project-guide.md",
        ),
        (
            template_dir / "board-project-checklist.md.template",
            repo_dir / "docs" / "board-project-checklist.md",
        ),
    ]

    for template_path, output_path in outputs:
        content = render_template(template_path, replacements)
        write_file(output_path, content, force=args.force)
        print(f"[OK] Wrote {output_path}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
