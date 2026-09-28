using TicketManager.Cli.Storage;

namespace TicketManager.UnitTests.Storage;

// Unit test cho DataFilePath: quy tắc chọn đường dẫn file dữ liệu (hàm thuần, không I/O).
public class DataFilePathTests
{
    private static readonly string Home = Path.Combine(Path.GetTempPath(), "home");

    // U10 — Thứ tự ưu tiên khi chọn file dữ liệu:
    //   1) Normal   — có option --data-file → dùng option (dù TICKETS_FILE cũng có giá trị)
    //   2) Normal   — không có option, có biến môi trường TICKETS_FILE → dùng biến môi trường
    //   3) Boundary — không có cả hai → đường dẫn mặc định <home>/.tickets/tickets.json
    //   4) Abnormal — TICKETS_FILE được đặt nhưng rỗng ("") → coi như không có, dùng mặc định
    //      [EG] người dùng gõ `set TICKETS_FILE=` — nếu code chỉ kiểm tra != null sẽ dùng
    //      đường dẫn "" và crash.
    // (expectedPath = null nghĩa là mong đợi đường dẫn mặc định)
    // [DT] Bảng quyết định (option × biến môi trường): trường hợp "có option, không có biến
    //      môi trường" được gộp vào dòng 1 vì option luôn được ưu tiên.
    [Theory]
    [InlineData("from-option.json", "from-env.json", "from-option.json")]
    [InlineData(null, "from-env.json", "from-env.json")]
    [InlineData(null, null, null)]
    [InlineData(null, "", null)]
    public void U10_Resolve_PrefersOptionThenEnvironmentThenDefault(
        string? optionValue, string? environmentValue, string? expectedPath)
    {
        var defaultPath = Path.Combine(Home, ".tickets", "tickets.json");

        var path = DataFilePath.Resolve(optionValue, environmentValue, Home);

        Assert.Equal(expectedPath ?? defaultPath, path);
    }
}
