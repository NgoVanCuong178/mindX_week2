namespace TicketManager.IntegrationTests;

// Tạo một thư mục tạm có tên ngẫu nhiên cho mỗi test và xoá sạch khi test kết thúc,
// để các test không dùng chung file và không để lại rác trên máy.
public sealed class TempDirectory : IDisposable
{
    public string Path { get; } =
        System.IO.Path.Combine(System.IO.Path.GetTempPath(), "tickets-tests-" + Guid.NewGuid().ToString("N"));

    public TempDirectory() => Directory.CreateDirectory(Path);

    public string File(params string[] parts) => System.IO.Path.Combine([Path, .. parts]);

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
