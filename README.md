# Ticket Manager CLI

Công cụ dòng lệnh quản lý ticket, lưu dữ liệu trong file JSON trên máy. Hỗ trợ tạo, liệt kê (có bộ lọc), xem chi tiết và cập nhật trạng thái ticket.

```
$ tickets create --title "Sửa lỗi đăng nhập" --priority high --tag api --tag auth
Created ticket #1

$ tickets list
ID   STATUS       PRIORITY  TITLE                                   TAGS
1    open         high      Sửa lỗi đăng nhập                       api, auth
```

Dự án được xây dựng theo Test-Driven Development (bài tập Tuần 2 — MindX Engineering Onboarding).

## Mục lục

- [Yêu cầu hệ thống](#yêu-cầu-hệ-thống)
- [Cài đặt](#cài-đặt)
- [Cấu hình](#cấu-hình)
- [Cách sử dụng](#cách-sử-dụng)
- [Exit code](#exit-code)
- [Chạy test](#chạy-test)
- [Cấu trúc dự án](#cấu-trúc-dự-án)

## Yêu cầu hệ thống

- [.NET SDK 9.0](https://dotnet.microsoft.com/download/dotnet/9.0) (bản 9.0.101 trở lên; phiên bản được cố định trong `global.json`)
- Git
- Hệ điều hành: Windows, macOS hoặc Linux

Kiểm tra đã cài .NET SDK:

```bash
dotnet --list-sdks
```

Kết quả phải có một dòng bắt đầu bằng `9.0.`.

## Cài đặt

### 1. Lấy mã nguồn và build

```bash
git clone https://github.com/NgoVanCuong178/mindX_week2.git
cd mindX_week2
dotnet build
```

Build thành công sẽ in `Build succeeded` với `0 Warning(s)` và `0 Error(s)`.

### 2. Chọn cách chạy

**Cách A — Cài thành lệnh `tickets` (khuyến nghị)**

Đóng gói thành [.NET tool](https://learn.microsoft.com/dotnet/core/tools/global-tools) và cài cho toàn máy:

```bash
dotnet pack src/TicketManager.Cli -o ./nupkg
dotnet tool install --global TicketManager.Cli --add-source ./nupkg
```

Sau đó gõ được `tickets` ở bất kỳ thư mục nào:

```bash
tickets --help
```

Nếu báo không tìm thấy lệnh `tickets`, thêm thư mục tool của .NET vào biến `PATH`:
- Windows: `%USERPROFILE%\.dotnet\tools`
- macOS / Linux: `~/.dotnet/tools`

Cập nhật lên bản code mới:

```bash
dotnet tool uninstall --global TicketManager.Cli
dotnet pack src/TicketManager.Cli -o ./nupkg
dotnet tool install --global TicketManager.Cli --add-source ./nupkg
```

Gỡ cài đặt:

```bash
dotnet tool uninstall --global TicketManager.Cli
```

**Cách B — Chạy trực tiếp từ mã nguồn (không cần cài)**

```bash
dotnet run --project src/TicketManager.Cli -- <lệnh> [tuỳ chọn]
```

Ví dụ:

```bash
dotnet run --project src/TicketManager.Cli -- list
```

Mọi ví dụ bên dưới viết theo cách A (`tickets ...`). Với cách B, thay `tickets` bằng `dotnet run --project src/TicketManager.Cli --`.

## Cấu hình

Cấu hình duy nhất là **vị trí file dữ liệu**. Chương trình chọn file theo thứ tự ưu tiên:

| Ưu tiên | Nguồn | Ví dụ |
|---|---|---|
| 1 | Tuỳ chọn `--data-file` trong câu lệnh | `tickets list --data-file ./work.json` |
| 2 | Biến môi trường `TICKETS_FILE` | `TICKETS_FILE=./work.json` |
| 3 | Mặc định | `~/.tickets/tickets.json` (Windows: `C:\Users\<tên>\.tickets\tickets.json`) |

Biến môi trường `TICKETS_FILE` được đặt nhưng để rỗng thì coi như không có.

**Đặt biến môi trường `TICKETS_FILE`:**

| Terminal | Chỉ cho phiên hiện tại |
|---|---|
| Git Bash / macOS / Linux | `export TICKETS_FILE=~/projects/tickets.json` |
| PowerShell | `$env:TICKETS_FILE = "C:\projects\tickets.json"` |
| Command Prompt (cmd) | `set TICKETS_FILE=C:\projects\tickets.json` |

**Về file dữ liệu:**

- Lần chạy đầu tiên chưa có file: coi như chưa có ticket nào. File và thư mục chứa nó được tạo tự động khi tạo ticket đầu tiên.
- File bị hỏng (sai cú pháp JSON, sai cấu trúc hoặc rỗng): chương trình báo lỗi, trả exit code `2` và **không ghi đè file** — dữ liệu cũ không bị mất.
- Không nên sửa file bằng tay; dùng các lệnh bên dưới.

## Cách sử dụng

Xem hướng dẫn nhanh ngay trong terminal:

```bash
tickets --help
tickets create --help
```

Tuỳ chọn `--data-file <đường-dẫn>` dùng được với mọi lệnh (xem [Cấu hình](#cấu-hình)).

### Giá trị hợp lệ

| Trường | Giá trị | Mặc định |
|---|---|---|
| `status` | `open`, `in_progress`, `done` | `open` |
| `priority` | `low`, `medium`, `high` | `medium` |

Không phân biệt chữ hoa, chữ thường (`HIGH` và `high` như nhau).

### `tickets create` — Tạo ticket

```bash
tickets create --title <tiêu-đề> [--description <mô-tả>] [--status <status>] [--priority <priority>] [--tag <tag>]...
```

| Tuỳ chọn | Bắt buộc | Mô tả |
|---|---|---|
| `--title` | Có | 1–200 ký tự; khoảng trắng hai đầu được bỏ đi |
| `--description` | Không | Tối đa 2000 ký tự |
| `--status` | Không | Mặc định `open` |
| `--priority` | Không | Mặc định `medium` |
| `--tag` | Không | Lặp lại để thêm nhiều tag. Tag được chuyển về chữ thường; tag trùng hoặc rỗng bị bỏ |

Id được cấp tự động, tăng dần từ 1.

```bash
$ tickets create --title "Sửa lỗi đăng nhập" --description "Không đăng nhập được bằng SSO" --priority high --tag api --tag auth
Created ticket #1

$ tickets create --title "Add dark mode" --tag ui
Created ticket #2
```

### `tickets list` — Liệt kê ticket

```bash
tickets list [--status <status>] [--priority <priority>] [--tag <tag>]...
```

- Không có bộ lọc: liệt kê tất cả, sắp theo id tăng dần.
- Nhiều bộ lọc: ticket phải thoả **tất cả** điều kiện.
- Nhiều `--tag`: ticket phải có **đủ** mọi tag được chỉ định.

```bash
$ tickets list
ID   STATUS       PRIORITY  TITLE                                   TAGS
1    open         high      Sửa lỗi đăng nhập                       api, auth
2    open         medium    Add dark mode                           ui

$ tickets list --priority high --tag api
ID   STATUS       PRIORITY  TITLE                                   TAGS
1    open         high      Sửa lỗi đăng nhập                       api, auth

$ tickets list --status done
No tickets found.
```

### `tickets show <id>` — Xem chi tiết

```bash
$ tickets show 1
ID:          1
Title:       Sửa lỗi đăng nhập
Description: Không đăng nhập được bằng SSO
Status:      open
Priority:    high
Tags:        api, auth
Created:     2026-09-28 23:07:07Z
Updated:
```

Thời gian hiển thị theo giờ UTC. `Updated` để trống nếu ticket chưa từng được cập nhật.

### `tickets update <id>` — Cập nhật trạng thái

```bash
tickets update <id> --status <status>
```

```bash
$ tickets update 1 --status in_progress
Updated ticket #1: status = in_progress
```

### Thông báo lỗi thường gặp

| Tình huống | Thông báo | Exit code |
|---|---|---|
| Thiếu `--title` | `Option '--title' is required.` | 1 |
| Title rỗng | `Title is required.` | 1 |
| Title dài hơn 200 ký tự | `Title must not exceed 200 characters.` | 1 |
| Giá trị status/priority sai | `Invalid value 'closed'. Allowed values: open, in_progress, done.` | 1 |
| Id không phải số | `Cannot parse argument 'abc' for command 'show' as expected type 'System.Int32'.` | 1 |
| Không tìm thấy ticket | `Ticket #99 not found` | 1 |
| File dữ liệu hỏng | `Data file '<đường-dẫn>' is corrupted or not valid JSON.` | 2 |

Thông báo lỗi được in ra **stderr**, kết quả in ra **stdout**.

## Exit code

| Exit code | Ý nghĩa |
|---|---|
| `0` | Thành công |
| `1` | Lỗi đầu vào: thiếu hoặc sai tuỳ chọn, dữ liệu không hợp lệ, không tìm thấy ticket |
| `2` | Lỗi file dữ liệu: file bị hỏng |

Dùng trong script:

```bash
if tickets show 5 > /dev/null 2>&1; then
  echo "Ticket #5 tồn tại"
fi
```

## Chạy test

```bash
dotnet test
```

Kết quả mong đợi:

```
Passed!  - Failed: 0, Passed: 33, ... TicketManager.UnitTests.dll
Passed!  - Failed: 0, Passed: 17, ... TicketManager.IntegrationTests.dll
```

| Nhóm | Project | Nội dung |
|---|---|---|
| Unit test | `tests/TicketManager.UnitTests` | Logic ticket và quy tắc validation; không đọc/ghi file |
| Integration test | `tests/TicketManager.IntegrationTests` | Lưu trữ file JSON thật, hành vi các lệnh CLI, và một test chạy chương trình thật qua tiến trình con |

Chạy riêng một nhóm hoặc một test (mỗi test có mã như `U04`, `C08` ở đầu tên):

```bash
dotnet test tests/TicketManager.UnitTests
dotnet test --filter "FullyQualifiedName~C08"
```

Đo code coverage (loại `Program.cs`, file chỉ chạy trong tiến trình con):

```bash
dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"tests/**/TestResults/**/coverage.cobertura.xml" -targetdir:TestResults/coverage -reporttypes:Html
```

Mở `TestResults/coverage/index.html` để xem báo cáo.

## Cấu trúc dự án

```
├── src/TicketManager.Cli/
│   ├── Program.cs                     Điểm vào chương trình
│   ├── CliApp.cs                      Khai báo lệnh, chuyển lỗi thành exit code
│   ├── Commands/EnumText.cs           Chuyển giữa chữ trên dòng lệnh và enum
│   ├── Services/TicketService.cs      Logic ticket và validation
│   ├── Models/Ticket.cs               Dữ liệu ticket
│   └── Storage/
│       ├── JsonTicketRepository.cs    Đọc/ghi file JSON
│       └── DataFilePath.cs            Chọn vị trí file dữ liệu
├── tests/
│   ├── TicketManager.UnitTests/
│   └── TicketManager.IntegrationTests/
├── coverlet.runsettings               Cấu hình đo coverage
└── global.json                        Cố định phiên bản .NET SDK
```

Các lớp được tách biệt: phần xử lý lệnh (`CliApp`, `Commands`) không đọc/ghi file trực tiếp; phần logic (`Services`) không biết về dòng lệnh; phần lưu trữ (`Storage`) không biết về quy tắc nghiệp vụ.
