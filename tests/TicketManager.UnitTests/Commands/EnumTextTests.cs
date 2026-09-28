using TicketManager.Cli.Commands;
using TicketManager.Cli.Models;
using TicketManager.Cli.Services;

namespace TicketManager.UnitTests.Commands;

// Unit test cho EnumText: chuyển đổi giữa chữ người dùng gõ trên dòng lệnh
// (snake_case, ví dụ "in_progress") và enum trong code (TicketStatus.InProgress).
public class EnumTextTests
{
    // U11 — Parse chữ hợp lệ.
    // Loại: Normal.
    // Boundary: không áp dụng — enum là tập giá trị rời rạc, không có min/max; mỗi giá trị
    //    là một miền riêng (EP) và hàm Parse dùng chung cho mọi giá trị.
    //   "in_progress" → TicketStatus.InProgress (chữ có dấu gạch dưới ↔ tên enum PascalCase)
    //   "HIGH"        → TicketPriority.High     (không phân biệt hoa thường)
    // [EP] Miền hợp lệ: chữ snake_case nhiều từ, và chữ viết hoa. Hàm Parse là generic nên
    //      mỗi miền chỉ cần một đại diện, không cần lặp lại cho từng giá trị enum.
    [Theory]
    [InlineData("in_progress", TicketStatus.InProgress)]
    [InlineData("HIGH", TicketPriority.High)]
    public void U11_Parse_AcceptsSnakeCaseIgnoringCase<TEnum>(string text, TEnum expected)
        where TEnum : struct, Enum
    {
        Assert.Equal(expected, EnumText.Parse<TEnum>(text));
    }

    // Dữ liệu cho U12: (giá trị sai cần parse, danh sách giá trị hợp lệ phải có trong thông báo lỗi).
    public static TheoryData<Action, string> InvalidValues => new()
    {
        { () => EnumText.Parse<TicketPriority>("urgent"), "low, medium, high" },
        { () => EnumText.Parse<TicketStatus>("in progress"), "open, in_progress, done" },
    };

    // U12 — Parse chữ không hợp lệ.
    // Loại: Abnormal (sai định dạng / giá trị không tồn tại).
    // "urgent" (priority không tồn tại) và "in progress" (dùng dấu cách thay vì gạch dưới)
    // → ném ValidationException, thông báo lỗi liệt kê các giá trị hợp lệ để người dùng sửa.
    // [EP] Miền không hợp lệ: giá trị không tồn tại.
    // [EG] "in progress" — người dùng hay gõ dấu cách thay vì dấu gạch dưới.
    [Theory]
    [MemberData(nameof(InvalidValues))]
    public void U12_Parse_UnknownValue_ThrowsListingAllowedValues(Action parse, string allowedValues)
    {
        var exception = Assert.Throws<ValidationException>(parse);

        Assert.Contains(allowedValues, exception.Message);
    }

    // U13 — Format enum thành chữ để in ra màn hình (chiều ngược lại của Parse).
    // Loại: Normal.
    //   TicketStatus.InProgress → "in_progress"
    //   TicketPriority.High     → "high"
    // [EP] Tên enum nhiều từ (InProgress → có gạch dưới) và một từ (High → chỉ đổi chữ thường).
    [Theory]
    [InlineData(TicketStatus.InProgress, "in_progress")]
    [InlineData(TicketPriority.High, "high")]
    public void U13_Format_ReturnsSnakeCase<TEnum>(TEnum value, string expected)
        where TEnum : struct, Enum
    {
        Assert.Equal(expected, EnumText.Format(value));
    }
}
