#!/usr/bin/env python3
"""Migrate controller hardcoded messages to IStringLocalizer keys and update .resx files."""

import re
import xml.etree.ElementTree as ET
from pathlib import Path

STAYHUB = Path(__file__).resolve().parent.parent / "StayHub"
RESX_EN = STAYHUB / "StayHub.Common" / "Resources" / "Messages.resx"
RESX_VI = STAYHUB / "StayHub.Common" / "Resources" / "Messages.vi.resx"

# Vietnamese translations for known messages (English -> Vietnamese)
VI_MAP = {
    "Invalid input data.": "Dữ liệu đầu vào không hợp lệ.",
    "User not found.": "Không tìm thấy người dùng.",
    "User not found or account is inactive.": "Không tìm thấy người dùng hoặc tài khoản chưa được kích hoạt.",
    "Invalid token claims.": "Token không hợp lệ.",
    "Invalid token claims. User not identified.": "Token không hợp lệ. Không xác định được người dùng.",
    "Invalid user token.": "Token người dùng không hợp lệ.",
    "Cannot extract user ID from token": "Không thể lấy ID người dùng từ token",
    "User ID not found in token.": "Không tìm thấy ID người dùng trong token.",
    "Tour not found": "Không tìm thấy tour",
    "Tour not found.": "Không tìm thấy tour.",
    "Customer not found.": "Không tìm thấy khách hàng.",
    "Category not found.": "Không tìm thấy danh mục.",
    "Category not found or has been deleted.": "Không tìm thấy danh mục hoặc đã bị xóa.",
    "Ticket type not found.": "Không tìm thấy loại vé.",
    "Tourism information not found.": "Không tìm thấy thông tin du lịch.",
    "TourItinerary not found": "Không tìm thấy lịch trình tour",
    "TourScheduleItinerary not found": "Không tìm thấy lịch trình theo lịch",
    "TourScheduleTicket not found": "Không tìm thấy vé theo lịch",
    "Voucher not found": "Không tìm thấy voucher",
    "Invalid query parameters.": "Tham số truy vấn không hợp lệ.",
    "Invalid filter parameters.": "Tham số lọc không hợp lệ.",
    "Search keyword cannot be empty.": "Từ khóa tìm kiếm không được để trống.",
    "Search query cannot be empty.": "Truy vấn tìm kiếm không được để trống.",
    "Role name cannot be empty.": "Tên vai trò không được để trống.",
    "User IDs list cannot be empty.": "Danh sách ID người dùng không được để trống.",
    "Refresh token is required.": "Yêu cầu refresh token.",
    "Access token is required.": "Yêu cầu access token.",
    "Invalid service key": "Service key không hợp lệ",
    "Internal server error.": "Lỗi hệ thống.",
    "Lỗi hệ thống.": "Lỗi hệ thống.",
    "Lỗi hệ thống trong quá trình điểm danh.": "Lỗi hệ thống trong quá trình điểm danh.",
    "Không xác định được danh tính nhân viên.": "Không xác định được danh tính nhân viên.",
    "Điểm danh (Check-in) thành công!": "Điểm danh (Check-in) thành công!",
    "Không xác thực được user.": "Không xác thực được người dùng.",
    "ScheduleId và StaffId phải lớn hơn 0.": "ScheduleId và StaffId phải lớn hơn 0.",
    "ScheduleId không hợp lệ.": "ScheduleId không hợp lệ.",
    "Phân công nhân viên thành công.": "Phân công nhân viên thành công.",
    "Lấy danh sách nhân viên thành công.": "Lấy danh sách nhân viên thành công.",
    "Unable to determine current user.": "Không xác định được người dùng hiện tại.",
    "User identity could not be verified.": "Không thể xác minh danh tính người dùng.",
    "Admin/Staff identity could not be verified.": "Không thể xác minh danh tính Admin/Staff.",
    "Invalid token: Missing or invalid User ID.": "Token không hợp lệ: Thiếu hoặc sai User ID.",
    "Invalid token claims. Email not found.": "Token không hợp lệ. Không tìm thấy email.",
}


def to_key(text: str, existing: dict[str, str]) -> str:
    text = text.strip().rstrip(".")
    words = re.findall(r"[A-Za-z0-9]+", text)
    if not words:
        base = "Message"
    else:
        base = "".join(w[:1].upper() + w[1:] for w in words)
    if len(base) > 64:
        base = base[:58]
    key = base
    n = 2
    while key in existing and existing[key] != text:
        key = f"{base}{n}"
        n += 1
    return key


def load_resx_keys(path: Path) -> dict[str, str]:
    if not path.exists():
        return {}
    tree = ET.parse(path)
    root = tree.getroot()
    ns = ""
    result = {}
    for data in root.findall("data"):
        name = data.get("name")
        val_el = data.find("value")
        if name and val_el is not None and val_el.text:
            result[name] = val_el.text
    return result


def save_resx(path: Path, entries: dict[str, str], vi: bool = False):
    lines = [
        '<?xml version="1.0" encoding="utf-8"?>',
        '<root>',
        '  <resheader name="resmimetype"><value>text/microsoft-resx</value></resheader>',
        '  <resheader name="version"><value>2.0</value></resheader>',
        '  <resheader name="reader"><value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value></resheader>',
        '  <resheader name="writer"><value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value></resheader>',
    ]
    for key in sorted(entries.keys()):
        val = entries[key]
        val = val.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")
        lines.append(f'  <data name="{key}" xml:space="preserve">')
        lines.append(f"    <value>{val}</value>")
        lines.append("  </data>")
    lines.append("</root>")
    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def ensure_localizer_in_controller(content: str, class_name: str) -> str:
    if "IStringLocalizer<Messages>" in content:
        return content

    if ": LocalizedControllerBase" not in content and ": ControllerBase" in content:
        content = content.replace(": ControllerBase", ": LocalizedControllerBase")
        if "using StayHub.Common.Controllers;" not in content:
            content = content.replace(
                "using Microsoft.AspNetCore.Mvc;",
                "using Microsoft.AspNetCore.Mvc;\nusing Microsoft.Extensions.Localization;\nusing StayHub.Common.Controllers;\nusing StayHub.Common.Resources;",
            )

    # Add constructor param if missing
    ctor_pattern = re.compile(
        rf"(public {class_name}\([^)]*)\)",
        re.DOTALL,
    )
    match = ctor_pattern.search(content)
    if match and "IStringLocalizer" not in match.group(1):
        old = match.group(0)
        if old.count("(") == 1 and old.endswith(")"):
            inner = old[old.index("(") + 1 : -1].strip()
            if inner:
                new = old[:-1] + ", IStringLocalizer<Messages> localizer)"
            else:
                new = f"public {class_name}(IStringLocalizer<Messages> localizer)"
            content = content.replace(old, new, 1)
            # Add base call
            content = re.sub(
                rf"(public {class_name}\([^{{]+\)\s*\{{)",
                r"\1\n            : base(localizer)",
                content,
                count=1,
            )
    return content


def migrate_controller(path: Path, text_to_key: dict[str, str]) -> bool:
    content = path.read_text(encoding="utf-8")
    if "AuthController" in path.name and "IStringLocalizer" in content:
        return False

    original = content
    class_match = re.search(r"public class (\w+Controller)", content)
    if not class_match:
        return False
    class_name = class_match.group(1)

    def repl(m):
        msg = m.group(1)
        if msg.startswith("_localizer[") or msg.startswith("M("):
            return m.group(0)
        key = text_to_key.get(msg)
        if not key:
            return m.group(0)
        return f'message = M("{key}")'

    content = re.sub(r'message = "([^"]+)"', repl, content)

    if content != original:
        content = ensure_localizer_in_controller(content, class_name)
        path.write_text(content, encoding="utf-8")
        return True
    return False


def main():
    en_entries = load_resx_keys(RESX_EN)
    vi_entries = load_resx_keys(RESX_VI)

    # Build reverse map en value -> key from existing
    value_to_key = {v: k for k, v in en_entries.items()}

    # Collect all messages from controllers
    all_messages: set[str] = set()
    for ctrl in STAYHUB.rglob("*Controller.cs"):
        if "StayHub.Common" in str(ctrl):
            continue
        text = ctrl.read_text(encoding="utf-8")
        for m in re.finditer(r'message = "([^"]+)"', text):
            all_messages.add(m.group(1))

    for msg in sorted(all_messages):
        if msg not in value_to_key:
            key = to_key(msg, en_entries)
            en_entries[key] = msg if msg.endswith(".") or msg.endswith("!") else msg
            if not msg.endswith(".") and not msg.endswith("!") and " " in msg:
                pass  # keep as-is
            vi_entries[key] = VI_MAP.get(msg, msg)  # fallback: same text

    save_resx(RESX_EN, en_entries)
    save_resx(RESX_VI, vi_entries)

    text_to_key = {v: k for k, v in en_entries.items()}
    migrated = 0
    for ctrl in sorted(STAYHUB.rglob("*Controller.cs")):
        if "StayHub.Common" in str(ctrl) or ctrl.name == "AuthController.cs":
            continue
        if migrate_controller(ctrl, text_to_key):
            migrated += 1
            print(f"Migrated: {ctrl.relative_to(STAYHUB)}")

    print(f"Done. {len(en_entries)} keys, {migrated} controllers migrated.")


if __name__ == "__main__":
    main()
