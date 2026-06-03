#!/usr/bin/env python3
"""Generate ValidationMessages.resx and ValidationMessages.vi.resx from message catalog."""
import xml.etree.ElementTree as ET
from pathlib import Path

# English key -> Vietnamese translation
VI: dict[str, str] = {
    "Action must be either 'Approve' or 'Reject'.": "Hành động phải là 'Approve' hoặc 'Reject'.",
    "AssignmentId must be greater than 0.": "AssignmentId phải lớn hơn 0.",
    "SessionId is required.": "SessionId là bắt buộc.",
    "SessionId must be between 8 and 64 characters.": "SessionId phải từ 8 đến 64 ký tự.",
    "PreferredList is required.": "PreferredList là bắt buộc.",
    "Rating must be between 1 and 7.": "Điểm phải từ 1 đến 7.",
    "AgeGroup cannot exceed 20 characters.": "AgeGroup không được vượt quá 20 ký tự.",
    "TravelExperience cannot exceed 30 characters.": "TravelExperience không được vượt quá 30 ký tự.",
    "Address cannot exceed 255 characters.": "Địa chỉ không được vượt quá 255 ký tự.",
    "Address is required": "Địa chỉ là bắt buộc",
    "Amount is required": "Số tiền là bắt buộc",
    "Amount must be greater than 0": "Số tiền phải lớn hơn 0",
    "AssignedRole cannot exceed 255 characters": "Vai trò phân công không được vượt quá 255 ký tự",
    "AvailableCount must be at least 1": "Số lượng khả dụng phải ít nhất là 1",
    "Avatar must be an image file (.jpg, .jpeg, .png, .gif).": "Ảnh đại diện phải là file ảnh (.jpg, .jpeg, .png, .gif).",
    "Banner image is required.": "Ảnh banner là bắt buộc.",
    "BillAmount must be greater than 0": "Số tiền hóa đơn phải lớn hơn 0",
    "Caption cannot exceed 500 characters.": "Chú thích không được vượt quá 500 ký tự.",
    "Category name is required.": "Tên danh mục là bắt buộc.",
    "CategoryId is required": "CategoryId là bắt buộc",
    "CategoryId must be greater than 0": "CategoryId phải lớn hơn 0",
    "ChildrenCount is required when HasChildren is true.": "ChildrenCount là bắt buộc khi HasChildren là true.",
    "City cannot exceed 100 characters.": "Thành phố không được vượt quá 100 ký tự.",
    "City is required": "Thành phố là bắt buộc",
    "Code is required": "Mã là bắt buộc",
    "Code must be between 3 and 50 characters": "Mã phải từ 3 đến 50 ký tự",
    "Code must contain only letters, numbers, hyphens, and underscores": "Mã chỉ được chứa chữ, số, gạch ngang và gạch dưới",
    "Comment cannot be empty.": "Bình luận không được để trống.",
    "Comment cannot contain only whitespace.": "Bình luận không được chỉ chứa khoảng trắng.",
    "Comment cannot exceed 1000 characters.": "Bình luận không được vượt quá 1000 ký tự.",
    "Comment cannot exceed 500 characters.": "Bình luận không được vượt quá 500 ký tự.",
    "Comment is required.": "Bình luận là bắt buộc.",
    "CompanionType is required.": "CompanionType là bắt buộc.",
    "CompanionType must be solo, family, couple, or group.": "CompanionType phải là solo, family, couple hoặc group.",
    "Country cannot exceed 100 characters.": "Quốc gia không được vượt quá 100 ký tự.",
    "Country is required": "Quốc gia là bắt buộc",
    "CustomerId is required.": "CustomerId là bắt buộc.",
    "CustomerId must be greater than 0": "CustomerId phải lớn hơn 0",
    "Date of Birth is required.": "Ngày sinh là bắt buộc.",
    "Date of birth cannot be in the future.": "Ngày sinh không được ở tương lai.",
    "DayNumber is required": "DayNumber là bắt buộc",
    "DayNumber must be greater than 0": "DayNumber phải lớn hơn 0",
    "Departure date is required": "Ngày khởi hành là bắt buộc",
    "Description cannot exceed 1000 characters": "Mô tả không được vượt quá 1000 ký tự",
    "Description cannot exceed 2000 characters": "Mô tả không được vượt quá 2000 ký tự",
    "Description is required": "Mô tả là bắt buộc",
    "DiscountType is required": "DiscountType là bắt buộc",
    "DiscountType must be 'Percent' or 'Amount'": "DiscountType phải là 'Percent' hoặc 'Amount'",
    "DiscountValue must be greater than 0": "DiscountValue phải lớn hơn 0",
    "DurationDays must be between 1 and 365.": "DurationDays phải từ 1 đến 365.",
    "ElderlyCount is required when HasElderly is true.": "ElderlyCount là bắt buộc khi HasElderly là true.",
    "Email is required.": "Email là bắt buộc.",
    "EndDate is required": "EndDate là bắt buộc",
    "EndDuration is required": "EndDuration là bắt buộc",
    "Facebook AccessToken is required.": "Facebook AccessToken là bắt buộc.",
    "Full Name is required.": "Họ tên là bắt buộc.",
    "Full name must be between 2 and 100 characters.": "Họ tên phải từ 2 đến 100 ký tự.",
    "Gender is required.": "Giới tính là bắt buộc.",
    "Google IdToken is required.": "Google IdToken là bắt buộc.",
    "Granularity must be day, week, or month.": "Granularity phải là day, week hoặc month.",
    "GroupSize must be between 1 and 500.": "GroupSize phải từ 1 đến 500.",
    "HTML tags are not allowed.": "Không cho phép thẻ HTML.",
    "Image is required.": "Ảnh là bắt buộc.",
    "Image size cannot exceed 5MB.": "Kích thước ảnh không được vượt quá 5MB.",
    "InteractionType must be view, click, wishlist, booking, or chat_recommend.": "InteractionType phải là view, click, wishlist, booking hoặc chat_recommend.",
    "Invalid companion type.": "Loại đồng hành không hợp lệ.",
    "Invalid email format.": "Định dạng email không hợp lệ.",
    "Invalid file format. Only image files are allowed.": "Định dạng file không hợp lệ. Chỉ cho phép file ảnh.",
    "Invalid interaction type.": "Loại tương tác không hợp lệ.",
    "Invalid nationality type.": "Loại quốc tịch không hợp lệ.",
    "Invalid phone number format.": "Định dạng số điện thoại không hợp lệ.",
    "IsLike cannot be null.": "IsLike không được null.",
    "ItineraryDate is required": "ItineraryDate là bắt buộc",
    "Latitude must be between -90 and 90.": "Vĩ độ phải từ -90 đến 90.",
    "LocationName cannot exceed 255 characters": "Tên địa điểm không được vượt quá 255 ký tự",
    "Longitude must be between -180 and 180.": "Kinh độ phải từ -180 đến 180.",
    "MaxDiscountAmount must be greater than 0": "MaxDiscountAmount phải lớn hơn 0",
    "MaxPrice must be non-negative.": "MaxPrice không được âm.",
    "Message cannot exceed 2000 characters.": "Tin nhắn không được vượt quá 2000 ký tự.",
    "Message is required.": "Tin nhắn là bắt buộc.",
    "Message must be at least 2 characters.": "Tin nhắn phải có ít nhất 2 ký tự.",
    "Message must be between 2 and 2000 characters.": "Tin nhắn phải từ 2 đến 2000 ký tự.",
    "MinPrice cannot be greater than MaxPrice.": "MinPrice không được lớn hơn MaxPrice.",
    "MinPrice must be non-negative.": "MinPrice không được âm.",
    "MomentId must be greater than 0.": "MomentId phải lớn hơn 0.",
    "Name cannot exceed 200 characters": "Tên không được vượt quá 200 ký tự",
    "Name cannot exceed 255 characters.": "Tên không được vượt quá 255 ký tự.",
    "Name is required": "Tên là bắt buộc",
    "Name is required.": "Tên là bắt buộc.",
    "NationalityType must be vietnamese or foreigner.": "NationalityType phải là vietnamese hoặc foreigner.",
    "New password is required.": "Mật khẩu mới là bắt buộc.",
    "New password must be at least 8 characters long, contain at least one uppercase letter, one lowercase letter, one number, and one special character.": "Mật khẩu mới phải có ít nhất 8 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt.",
    "Old password is required.": "Mật khẩu cũ là bắt buộc.",
    "Only .jpg, .jpeg, .png, .gif, and .webp extensions are allowed.": "Chỉ cho phép đuôi .jpg, .jpeg, .png, .gif và .webp.",
    "Only the following file extensions are allowed: {0}": "Chỉ cho phép các đuôi file sau: {0}",
    "OrderId is required": "OrderId là bắt buộc",
    "OrderId must be greater than 0": "OrderId phải lớn hơn 0",
    "Page must be greater than 0.": "Trang phải lớn hơn 0.",
    "PageSize must be between 1 and 100.": "PageSize phải từ 1 đến 100.",
    "Password is required.": "Mật khẩu là bắt buộc.",
    "Password must be at least 8 characters long, contain at least one uppercase letter, one lowercase letter, one number, and one special character.": "Mật khẩu phải có ít nhất 8 ký tự, gồm chữ hoa, chữ thường, số và ký tự đặc biệt.",
    "Phone Number is required.": "Số điện thoại là bắt buộc.",
    "PreferredEndDate cannot be before PreferredStartDate.": "PreferredEndDate không được trước PreferredStartDate.",
    "PreferredStartDate cannot be after PreferredEndDate.": "PreferredStartDate không được sau PreferredEndDate.",
    "PreferredStartDate cannot be too far in the past.": "PreferredStartDate không được quá xa trong quá khứ.",
    "PreferredStartDate is required.": "PreferredStartDate là bắt buộc.",
    "Price is required": "Giá là bắt buộc",
    "Price must be greater than or equal to 0": "Giá phải lớn hơn hoặc bằng 0",
    "Privacy is required.": "Quyền riêng tư là bắt buộc.",
    "Privacy must be 'Public', 'Private', or 'Friend'.": "Quyền riêng tư phải là 'Public', 'Private' hoặc 'Friend'.",
    "Provide at least one consultation criterion (city, country, budget, duration, category, or travel style).": "Cung cấp ít nhất một tiêu chí tư vấn (thành phố, quốc gia, ngân sách, thời lượng, danh mục hoặc phong cách du lịch).",
    "Quantity is required": "Số lượng là bắt buộc",
    "Quantity must be at least 1": "Số lượng phải ít nhất là 1",
    "Quantity must be greater than 0": "Số lượng phải lớn hơn 0",
    "Quantity must be greater than or equal to 0": "Số lượng phải lớn hơn hoặc bằng 0",
    "Query is required.": "Truy vấn là bắt buộc.",
    "Query must be between 2 and 1000 characters.": "Truy vấn phải từ 2 đến 1000 ký tự.",
    "Rating is required.": "Đánh giá là bắt buộc.",
    "Rating must be between 1 and 5 stars.": "Điểm đánh giá phải từ 1 đến 5 sao.",
    "Rating must be between 1 and 5.": "Điểm đánh giá phải từ 1 đến 5.",
    "Receiver ID is required.": "Receiver ID là bắt buộc.",
    "ReceiverId must be greater than 0.": "ReceiverId phải lớn hơn 0.",
    "Refresh token is required.": "Refresh token là bắt buộc.",
    "Request ID is required.": "Request ID là bắt buộc.",
    "RequestId must be greater than 0.": "RequestId phải lớn hơn 0.",
    "Return date is required": "Ngày về là bắt buộc",
    "ScheduleId is required": "ScheduleId là bắt buộc",
    "ScheduleId must be greater than 0": "ScheduleId phải lớn hơn 0",
    "ScheduleId must be greater than 0.": "ScheduleId phải lớn hơn 0.",
    "Select at least one travel interest.": "Chọn ít nhất một sở thích du lịch.",
    "SessionId cannot exceed 64 characters.": "SessionId không được vượt quá 64 ký tự.",
    "Slug is required.": "Slug là bắt buộc.",
    "SoldQuantity must be greater than or equal to 0": "SoldQuantity phải lớn hơn hoặc bằng 0",
    "SortBy must be one of: totalSpend, orderCount, reviewCount, wishlistCount, createdAt, lastOrderAt.": "SortBy phải là một trong: totalSpend, orderCount, reviewCount, wishlistCount, createdAt, lastOrderAt.",
    "SortOrder must be asc or desc.": "SortOrder phải là asc hoặc desc.",
    "Source name cannot exceed 255 characters.": "Tên nguồn không được vượt quá 255 ký tự.",
    "StaffId is required": "StaffId là bắt buộc",
    "StartDate cannot be after EndDate.": "StartDate không được sau EndDate.",
    "StartDate is required": "StartDate là bắt buộc",
    "StartDuration is required": "StartDuration là bắt buộc",
    "Status is required.": "Trạng thái là bắt buộc.",
    "Status must be either 'Active' or 'Blocked'.": "Trạng thái phải là 'Active' hoặc 'Blocked'.",
    "Status must be either 'Accepted' or 'Declined'.": "Trạng thái phải là 'Accepted' hoặc 'Declined'.",
    "Status cannot exceed 50 characters.": "Trạng thái không được vượt quá 50 ký tự.",
    "Ticket type name cannot exceed 100 characters.": "Tên loại vé không được vượt quá 100 ký tự.",
    "Ticket type name is required.": "Tên loại vé là bắt buộc.",
    "TicketTypeId is required": "TicketTypeId là bắt buộc",
    "TicketTypeId must be greater than 0": "TicketTypeId phải lớn hơn 0",
    "Title cannot exceed 255 characters": "Tiêu đề không được vượt quá 255 ký tự",
    "Title cannot exceed 255 characters.": "Tiêu đề không được vượt quá 255 ký tự.",
    "Title is required": "Tiêu đề là bắt buộc",
    "Title is required.": "Tiêu đề là bắt buộc.",
    "Top must be between 1 and 20.": "Top phải từ 1 đến 20.",
    "Top must be between 1 and 30.": "Top phải từ 1 đến 30.",
    "Top must be between 1 and 50.": "Top phải từ 1 đến 50.",
    "Top must be between 1 and 50.": "Top phải từ 1 đến 50.",
    "TourId is required": "TourId là bắt buộc",
    "TourId is required.": "TourId là bắt buộc.",
    "TourId must be greater than 0": "TourId phải lớn hơn 0",
    "TourId must be greater than 0 when provided": "TourId phải lớn hơn 0 khi được cung cấp",
    "TourScheduleTicketId must be greater than 0": "TourScheduleTicketId phải lớn hơn 0",
    "TravelInterests contains invalid value.": "TravelInterests chứa giá trị không hợp lệ.",
    "TravelStyle cannot exceed 500 characters.": "TravelStyle không được vượt quá 500 ký tự.",
    "Type cannot exceed 50 characters.": "Loại không được vượt quá 50 ký tự.",
    "Type is required.": "Loại là bắt buộc.",
    "UnitPrice must be greater than or equal to 0": "UnitPrice phải lớn hơn hoặc bằng 0",
    "UserId must be greater than 0": "UserId phải lớn hơn 0",
    "UserId must be greater than 0.": "UserId phải lớn hơn 0.",
    "Verification code is required.": "Mã xác minh là bắt buộc.",
    "VoucherCode must be between 3 and 50 characters": "VoucherCode phải từ 3 đến 50 ký tự",
}

HEADERS = """<?xml version="1.0" encoding="utf-8"?>
<root>
  <resheader name="resmimetype"><value>text/microsoft-resx</value></resheader>
  <resheader name="version"><value>2.0</value></resheader>
  <resheader name="reader"><value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value></resheader>
  <resheader name="writer"><value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value></resheader>
"""


def escape_xml(text: str) -> str:
    return (
        text.replace("&", "&amp;")
        .replace("<", "&lt;")
        .replace(">", "&gt;")
        .replace('"', "&quot;")
    )


def write_resx(path: Path, culture: str | None):
    lines = [HEADERS]
    for key in sorted(VI.keys()):
        value = VI[key] if culture == "vi" else key
        lines.append(f'  <data name="{escape_xml(key)}" xml:space="preserve">')
        lines.append(f"    <value>{escape_xml(value)}</value>")
        lines.append("  </data>")
    lines.append("</root>\n")
    path.write_text("\n".join(lines), encoding="utf-8")


def main():
    base = Path(__file__).resolve().parents[1] / "StayHub.Common" / "Resources"
    write_resx(base / "ValidationMessages.resx", None)
    write_resx(base / "ValidationMessages.vi.resx", "vi")
    print(f"Wrote {len(VI)} entries to {base}")


if __name__ == "__main__":
    main()
