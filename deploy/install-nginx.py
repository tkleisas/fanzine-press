#!/usr/bin/env python3
"""Insert the Fanzine Press location block into bokontep.gr.conf.

Safe to re-run — detects the marker and skips if already installed.
"""
import shutil
import sys
from pathlib import Path

CONF = Path("/etc/nginx/sites-available/bokontep.gr.conf")
SNIPPET = Path("/tmp/nginx-fanzine-press.snippet")
BACKUP = CONF.with_suffix(".conf.bak-fanzine")
MARKER_START = "# --- Fanzine Press ---"
# Anchor: insert before the PHP location block (unique line in the conf)
ANCHOR = "\t# pass PHP scripts to FastCGI server"


def main() -> int:
    content = CONF.read_text()

    if MARKER_START in content:
        print("Already installed — skipping")
        return 0

    if ANCHOR not in content:
        print(f"ERROR: anchor not found in {CONF}: {ANCHOR!r}", file=sys.stderr)
        return 1

    snippet = SNIPPET.read_text()
    if not snippet.endswith("\n"):
        snippet += "\n"

    if not BACKUP.exists():
        shutil.copy2(CONF, BACKUP)
        print(f"Backed up to {BACKUP}")

    new_content = content.replace(ANCHOR, snippet + ANCHOR)
    CONF.write_text(new_content)
    print("Installed location block")
    return 0


if __name__ == "__main__":
    sys.exit(main())
