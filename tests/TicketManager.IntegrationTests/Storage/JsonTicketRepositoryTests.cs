using TicketManager.Cli.Models;
using TicketManager.Cli.Storage;

namespace TicketManager.IntegrationTests.Storage;

// Integration test cho JsonTicketRepository: đọc/ghi file JSON thật trên ổ đĩa.
// Mỗi test dùng một thư mục tạm riêng (TempDirectory), tự xoá sau khi test chạy xong.
public sealed class JsonTicketRepositoryTests : IDisposable
{
    private readonly TempDirectory _temp = new();

    public void Dispose() => _temp.Dispose();

    // S01 — Lưu rồi đọc lại (round-trip) và ghi đè.
    // Loại: Normal.
    // Phần 1: lưu 2 ticket (một ticket có đủ mọi field, title tiếng Việt, enum, tags, thời gian
    //         có múi giờ) → mở repo mới đọc lại phải giống hệt từng field.
    // Phần 2: lưu lần 2 với danh sách khác → file bị ghi đè (chỉ còn ticket #3),
    //         không bị nối thêm vào nội dung cũ.
    // [EP] Miền hợp lệ "file có dữ liệu".
    // [EG] Lỗi hay gặp: mất dấu tiếng Việt, lệch múi giờ khi lưu thời gian, ghi nối thay vì ghi đè.
    [Fact]
    public void S01_SaveAll_ThenLoadAll_RoundTripsAndOverwrites()
    {
        var file = _temp.File("tickets.json");
        var first = new[]
        {
            new Ticket
            {
                Id = 1,
                Title = "Sửa lỗi đăng nhập",
                Description = "Không đăng nhập được bằng SSO",
                Status = TicketStatus.InProgress,
                Priority = TicketPriority.High,
                Tags = ["api", "auth"],
                CreatedAt = new DateTimeOffset(2026, 9, 28, 9, 0, 0, TimeSpan.FromHours(7)),
                UpdatedAt = new DateTimeOffset(2026, 9, 28, 10, 30, 0, TimeSpan.FromHours(7)),
            },
            new Ticket
            {
                Id = 2,
                Title = "Add dark mode",
                CreatedAt = new DateTimeOffset(2026, 9, 28, 11, 0, 0, TimeSpan.Zero),
            },
        };

        new JsonTicketRepository(file).SaveAll(first);
        var loaded = new JsonTicketRepository(file).LoadAll();

        Assert.Equivalent(first, loaded, strict: true);

        var second = new[] { new Ticket { Id = 3, Title = "Only ticket" } };
        new JsonTicketRepository(file).SaveAll(second);
        var reloaded = new JsonTicketRepository(file).LoadAll();

        Assert.Equal(3, Assert.Single(reloaded).Id);
    }

    // S02 — File và thư mục chưa tồn tại (lần đầu chạy app).
    // Loại: Boundary — trạng thái ban đầu, chưa có file nào (0 ticket).
    // Đọc → trả danh sách rỗng, không báo lỗi.
    // Lưu → tự tạo các thư mục còn thiếu ("nested/data") và file, đọc lại được ticket vừa lưu.
    // [EP] Miền "file chưa tồn tại". [EG] Lỗi hay gặp: DirectoryNotFoundException khi ghi.
    [Fact]
    public void S02_MissingFileAndDirectory_LoadsEmptyAndSaveCreatesThem()
    {
        var file = _temp.File("nested", "data", "tickets.json");
        var repository = new JsonTicketRepository(file);

        Assert.Empty(repository.LoadAll());

        repository.SaveAll([new Ticket { Id = 1, Title = "First" }]);

        Assert.True(File.Exists(file));
        Assert.Equal(1, Assert.Single(new JsonTicketRepository(file).LoadAll()).Id);
    }

    // S03 — File JSON bị hỏng. 3 kiểu hỏng:
    // Loại: Abnormal — kèm Boundary: file rỗng (0 byte).
    //   1) sai cú pháp JSON
    //   2) đúng cú pháp nhưng sai cấu trúc (là object, trong khi cần một mảng ticket)
    //   3) file rỗng (quyết định: coi là file hỏng, không coi là danh sách rỗng)
    // → ném StorageException có đường dẫn file trong thông báo, và nội dung file giữ nguyên.
    // [EP] Mỗi kiểu hỏng là một miền không hợp lệ khác nhau, với một đại diện.
    // [DT] Bảng quyết định khi đọc file: không tồn tại → rỗng (S02); tồn tại + hợp lệ → dữ liệu (S01);
    //      tồn tại + rỗng / sai cú pháp / sai cấu trúc → StorageException (S03).
    [Theory]
    [InlineData("{ not valid json")]
    [InlineData("{\"id\": 1, \"title\": \"not an array\"}")]
    [InlineData("")]
    public void S03_CorruptedFile_ThrowsStorageExceptionAndKeepsFile(string content)
    {
        var file = _temp.File("tickets.json");
        File.WriteAllText(file, content);

        var exception = Assert.Throws<StorageException>(() => new JsonTicketRepository(file).LoadAll());

        Assert.Contains(file, exception.Message);
        Assert.Equal(content, File.ReadAllText(file));
    }
}
