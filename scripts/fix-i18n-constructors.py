#!/usr/bin/env python3
"""Fix constructor syntax broken by migrate-i18n-controllers.py"""

import re
from pathlib import Path

STAYHUB = Path(__file__).resolve().parent.parent / "StayHub"

pattern = re.compile(
    r"(public \w+Controller\([^\)]+\))\s*\{\s*: base\(localizer\)\s*",
    re.MULTILINE,
)

for path in STAYHUB.rglob("*Controller.cs"):
    text = path.read_text(encoding="utf-8")
    new_text = pattern.sub(r"\1\n            : base(localizer)\n        {", text)
    if new_text != text:
        path.write_text(new_text, encoding="utf-8")
        print(f"Fixed: {path.relative_to(STAYHUB)}")

print("Done.")
