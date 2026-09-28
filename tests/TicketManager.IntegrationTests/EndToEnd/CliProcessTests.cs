using System.Diagnostics;
using System.Text;
using TicketManager.Cli.Storage;

namespace TicketManager.IntegrationTests.EndToEnd;

// E2E test: chạy chương trình thật (TicketManager.Cli.dll) trong một tiến trình con,
// y như người dùng gõ lệnh trong terminal. Không gọi code nội bộ, chỉ nhìn exit code và stdout.
// Đường dẫn file dữ liệu truyền qua biến môi trường TICKETS_FILE (không dùng --data-file).
public sealed class CliProcessTests : IDisposable
{
    private static readonly string CliDll = Path.Combine(AppContext.BaseDirectory, "TicketManager.Cli.dll");

    private readonly TempDirectory _temp = new();
    private readonly string _dataFile;

    public CliProcessTests()
    {
        _dataFile = _temp.File("tickets.json");
    }

    public void Dispose() => _temp.Dispose();

    // E01 — Luồng sử dụng đầy đủ: create → list → update → show.
    // Loại: Normal (luồng chuẩn của người dùng từ đầu đến cuối).
    // Kiểm tra cùng lúc:
    //   - Program.cs nối đúng vào CliApp (phần duy nhất các test khác không chạy qua)
    //   - biến môi trường TICKETS_FILE được dùng làm file dữ liệu
    //   - dữ liệu được lưu lại giữa các lần chạy chương trình riêng biệt
    //   - tiếng Việt in ra console không bị lỗi font/encoding
    // [EP] Miền hợp lệ "create chỉ với title" (miền "đủ option" nằm ở C01).
    // [EG] Lỗi hay gặp: console Windows không dùng UTF-8 → tiếng Việt thành "S?a l?i";
    //      quên đọc biến môi trường; dữ liệu chỉ nằm trong bộ nhớ, không được ghi ra file.
    [Fact]
    public void E01_FullFlow_CreateListUpdateShow()
    {
        var create = RunProcess("create", "--title", "Sửa lỗi đăng nhập");
        Assert.Equal(0, create.ExitCode);
        Assert.Contains("#1", create.Output);

        var list = RunProcess("list");
        Assert.Equal(0, list.ExitCode);
        Assert.Contains("Sửa lỗi đăng nhập", list.Output);

        var update = RunProcess("update", "1", "--status", "done");
        Assert.Equal(0, update.ExitCode);

        var show = RunProcess("show", "1");
        Assert.Equal(0, show.ExitCode);
        Assert.Contains("Sửa lỗi đăng nhập", show.Output);
        Assert.Contains("done", show.Output);

        Assert.True(File.Exists(_dataFile));
    }

    // Chạy `dotnet TicketManager.Cli.dll <args>` với TICKETS_FILE trỏ vào file tạm,
    // đọc stdout/stderr theo UTF-8, chờ tối đa 30 giây.
    private CliResult RunProcess(params string[] args)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add(CliDll);
        foreach (var arg in args)
        {
            startInfo.ArgumentList.Add(arg);
        }
        startInfo.Environment[DataFilePath.EnvironmentVariable] = _dataFile;

        using var process = Process.Start(startInfo)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        Assert.True(process.WaitForExit(TimeSpan.FromSeconds(30)), "CLI process timed out");

        return new CliResult(process.ExitCode, output.Result, error.Result);
    }
}
