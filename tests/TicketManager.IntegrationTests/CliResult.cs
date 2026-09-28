namespace TicketManager.IntegrationTests;

// Kết quả một lần chạy CLI: exit code, nội dung stdout và nội dung stderr.
public record CliResult(int ExitCode, string Output, string Error);
