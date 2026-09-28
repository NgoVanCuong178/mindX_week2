using TicketManager.Cli;
using TicketManager.Cli.Models;
using TicketManager.Cli.Storage;

namespace TicketManager.IntegrationTests.Commands;

// Integration test cho các lệnh CLI: gọi CliApp.Run ngay trong tiến trình test, đi qua đủ
// các lớp thật (parse lệnh → TicketService → JsonTicketRepository → file JSON thật).
// Mỗi test dùng một file dữ liệu riêng trong thư mục tạm (truyền qua --data-file), tự xoá sau test.
// Kiểm tra những gì người dùng thấy được: exit code, stdout, stderr và nội dung file.
public sealed class CliCommandTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly string _dataFile;

    public CliCommandTests()
    {
        _dataFile = _temp.File("tickets.json");
    }

    public void Dispose() => _temp.Dispose();

    // C01 — Lệnh `create` với đủ các option (happy path).
    // Loại: Normal.
    // Truyền đủ 5 field theo đề bài: --title, --description, --status, --priority, --tag (2 lần).
    // Exit code 0, stdout có id "#1", stderr rỗng, và ticket được ghi vào file JSON
    // với đúng title, description, status, priority và tags.
    // [EP] Miền hợp lệ của lệnh create: truyền đủ mọi option (miền "chỉ có title" nằm ở E01).
    // [EG] --status dùng "in_progress" (có gạch dưới) — giá trị dễ bị parse sai nhất.
    [Fact]
    public void C01_Create_WithAllOptions_SavesTicket()
    {
        var result = Run("create", "--title", "Fix login bug", "--description", "Cannot login",
                         "--status", "in_progress", "--priority", "high",
                         "--tag", "api", "--tag", "auth");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("#1", result.Output);
        Assert.Equal("", result.Error);
        var saved = Assert.Single(LoadTickets());
        Assert.Equal("Fix login bug", saved.Title);
        Assert.Equal("Cannot login", saved.Description);
        Assert.Equal(TicketStatus.InProgress, saved.Status);
        Assert.Equal(TicketPriority.High, saved.Priority);
        Assert.Equal(["api", "auth"], saved.Tags);
    }

    // Dữ liệu cho C02: mỗi dòng đại diện cho một đường xử lý lỗi khác nhau.
    // Các biến thể giá trị của từng đường đã được unit test phủ, nên ở đây chỉ cần 1 dòng/đường.
    // [EP] Mỗi đường xử lý lỗi là một miền không hợp lệ → chọn 1 đại diện cho mỗi miền.
    public static TheoryData<string[]> InvalidInputs => new()
    {
        new[] { "create" },                               // thư viện parse báo lỗi: thiếu option bắt buộc --title
        new[] { "create", "--title", "" },                // TicketService báo lỗi validation (xem U04, U05)
        new[] { "update", "1", "--status", "wrong" },     // EnumText.Parse báo lỗi giá trị sai (xem U12)
    };

    // C02 — Input không hợp lệ.
    // Loại: Abnormal (thiếu dữ liệu, dữ liệu rỗng, sai giá trị).
    // (Boundary về độ dài đã được unit test U04–U06 phủ, không lặp lại qua CLI.)
    // Exit code 1, thông báo lỗi chỉ in ra stderr, stdout phải rỗng
    // (để script/CI đọc stdout không nhận nhầm thông báo lỗi thành kết quả).
    // [DT] Luật "input sai → exit 1" trong bảng quyết định chuyển lỗi thành exit code.
    // [EG] Lỗi in ra stdout thay vì stderr; lỗi in cả stack trace cho người dùng.
    [Theory]
    [MemberData(nameof(InvalidInputs))]
    public void C02_InvalidInput_ExitsWithOneAndWritesOnlyToStderr(string[] args)
    {
        Run("create", "--title", "Existing ticket");

        var result = Run(args);

        Assert.Equal(1, result.ExitCode);
        Assert.False(string.IsNullOrWhiteSpace(result.Error));
        AssertNoTechnicalDetails(result.Error);
        Assert.Equal("", result.Output);
    }

    // C03 — Lệnh `list` với bộ lọc, trong đó --tag được truyền 2 lần.
    // Loại: Normal.
    // Tạo 4 ticket, mỗi ticket lệch đúng một điều kiện so với "Login API":
    //   Payment API — sai priority (low)
    //   Profile API — thiếu tag "auth"
    //   Search API  — sai status (đã chuyển sang done)
    // Lọc --status open --priority high --tag api --tag auth → chỉ in "Login API".
    // Mục đích: chứng minh cả 3 option lọc (và --tag lặp lại nhiều lần) được truyền đúng
    // xuống service (logic lọc chi tiết đã có ở U07).
    // [DT] Dòng "đủ 3 bộ lọc" của bảng quyết định lọc, chạy qua CLI thật.
    [Fact]
    public void C03_List_WithFilters_PrintsOnlyMatchingTickets()
    {
        Run("create", "--title", "Login API", "--priority", "high", "--tag", "api", "--tag", "auth");
        Run("create", "--title", "Payment API", "--priority", "low", "--tag", "api", "--tag", "auth");
        Run("create", "--title", "Profile API", "--priority", "high", "--tag", "api");
        Run("create", "--title", "Search API", "--priority", "high", "--tag", "api", "--tag", "auth");
        Run("update", "4", "--status", "done");

        var result = Run("list", "--status", "open", "--priority", "high", "--tag", "api", "--tag", "auth");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Login API", result.Output);
        Assert.DoesNotContain("Payment API", result.Output);
        Assert.DoesNotContain("Profile API", result.Output);
        Assert.DoesNotContain("Search API", result.Output);
    }

    // C04 — Lệnh `list` khi file dữ liệu chưa tồn tại (lần đầu dùng app).
    // Loại: Boundary — 0 ticket, chưa có file.
    // Không được báo lỗi: exit code 0, in "No tickets found.", stderr rỗng.
    // [EP] Miền "file chưa tồn tại" của file dữ liệu — hợp lệ, không phải lỗi.
    // [EG] Lỗi hay gặp: crash FileNotFoundException ở lần chạy đầu tiên.
    [Fact]
    public void C04_List_WhenDataFileMissing_PrintsNoTickets()
    {
        var result = Run("list");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("No tickets found.", result.Output);
        Assert.Equal("", result.Error);
    }

    // C05 — Lệnh `show <id>` với ticket có tồn tại.
    // Loại: Normal — kèm Boundary: id 1 là id nhỏ nhất có thể tồn tại.
    // In đủ các field: title, description, status ("open"), priority ("high") và tags.
    // [EP] Miền hợp lệ "id tồn tại" của lệnh show.
    [Fact]
    public void C05_Show_ExistingTicket_PrintsAllFields()
    {
        Run("create", "--title", "Fix login bug", "--description", "Cannot login",
            "--priority", "high", "--tag", "api", "--tag", "auth");

        var result = Run("show", "1");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("Fix login bug", result.Output);
        Assert.Contains("Cannot login", result.Output);
        Assert.Contains("open", result.Output);
        Assert.Contains("high", result.Output);
        Assert.Contains("api", result.Output);
        Assert.Contains("auth", result.Output);
    }

    // C06 — Lệnh `show <id>` với id không tồn tại.
    // Loại: Abnormal (biên của id đã được U09 phủ ở tầng unit).
    // Exit code 1, stderr có "Ticket #99 not found", stdout rỗng.
    // Đây là test duy nhất ở tầng CLI chứng minh TicketNotFoundException → exit code 1.
    // [EP] Miền không hợp lệ "id không tồn tại".
    // [DT] Luật "không tìm thấy → exit 1" của bảng quyết định chuyển lỗi thành exit code.
    [Fact]
    public void C06_Show_UnknownId_ReportsNotFound()
    {
        var result = Run("show", "99");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("Ticket #99 not found", result.Error);
        AssertNoTechnicalDetails(result.Error);
        Assert.Equal("", result.Output);
    }

    // C07 — Lệnh `update <id> --status` (happy path).
    // Loại: Normal.
    // Dùng "in_progress" (có dấu gạch dưới) để chắc chắn chữ người dùng gõ được chuyển đúng
    // thành enum InProgress. Exit code 0, stdout có "#1", file lưu status mới và UpdatedAt.
    // [EG] "in_progress" là giá trị duy nhất có dấu gạch dưới — dễ bị parse sai nhất.
    [Fact]
    public void C07_Update_Status_SavesNewStatus()
    {
        Run("create", "--title", "Fix login bug");

        var result = Run("update", "1", "--status", "in_progress");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("#1", result.Output);
        var saved = Assert.Single(LoadTickets());
        Assert.Equal(TicketStatus.InProgress, saved.Status);
        Assert.NotNull(saved.UpdatedAt);
    }

    // Dữ liệu cho C08: lệnh nào cũng đọc file trước tiên, nên chỉ cần 2 đại diện:
    // một lệnh có ghi file (create) và một lệnh chỉ đọc (list).
    public static TheoryData<string[]> WriteAndReadCommands => new()
    {
        new[] { "create", "--title", "New ticket" },
        new[] { "list" },
    };

    // C08 — File JSON bị hỏng.
    // Loại: Abnormal.
    // Exit code 2, stderr có đường dẫn file (để người dùng biết file nào hỏng), stdout rỗng,
    // và QUAN TRỌNG NHẤT: nội dung file giữ nguyên từng byte. Lệnh create không được
    // coi file hỏng là danh sách rỗng rồi ghi đè, vì như vậy sẽ mất toàn bộ dữ liệu cũ.
    // [DT] Luật "file hỏng → exit 2" của bảng quyết định chuyển lỗi thành exit code.
    // [EG] Lỗi hay gặp: catch lỗi đọc file rồi coi như danh sách rỗng → ghi đè mất dữ liệu;
    //      in stack trace thay vì thông báo dễ hiểu.
    [Theory]
    [MemberData(nameof(WriteAndReadCommands))]
    public void C08_CorruptedDataFile_ExitsWithTwoAndKeepsFile(string[] args)
    {
        const string corrupted = "{ not valid json";
        File.WriteAllText(_dataFile, corrupted);

        var result = Run(args);

        Assert.Equal(2, result.ExitCode);
        Assert.Contains(_dataFile, result.Error);
        AssertNoTechnicalDetails(result.Error);
        Assert.Equal("", result.Output);
        Assert.Equal(corrupted, File.ReadAllText(_dataFile));
    }

    // Chạy CLI với các tham số cho sẵn, luôn gắn thêm --data-file trỏ vào file tạm của test.
    // Thu stdout/stderr vào StringWriter thay vì in ra console.
    private CliResult Run(params string[] args)
    {
        var output = new StringWriter();
        var error = new StringWriter();

        var exitCode = CliApp.Run([.. args, "--data-file", _dataFile], output, error);

        return new CliResult(exitCode, output.ToString(), error.ToString());
    }

    // Thông báo lỗi cho người dùng phải là câu dễ hiểu, không được lộ tên exception hay
    // stack trace (ví dụ in ex.ToString() thay vì ex.Message).
    private static void AssertNoTechnicalDetails(string error)
    {
        Assert.DoesNotContain("Exception", error);
        Assert.DoesNotContain("   at ", error);
    }

    // Đọc lại file dữ liệu để kiểm tra lệnh đã ghi đúng hay chưa.
    private IReadOnlyList<Ticket> LoadTickets() => new JsonTicketRepository(_dataFile).LoadAll();
}
