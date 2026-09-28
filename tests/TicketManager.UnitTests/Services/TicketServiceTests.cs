using Microsoft.Extensions.Time.Testing;
using TicketManager.Cli.Models;
using TicketManager.Cli.Services;
using TicketManager.UnitTests.Fakes;

namespace TicketManager.UnitTests.Services;

// Unit test cho TicketService: logic nghiệp vụ và validation.
// Không chạm file thật: dùng InMemoryTicketRepository (repo giả trong bộ nhớ)
// và FakeTimeProvider (đồng hồ giả, cố định lúc Now) để kết quả luôn ổn định.
public class TicketServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 28, 9, 0, 0, TimeSpan.Zero);

    private readonly InMemoryTicketRepository _repository = new();
    private readonly FakeTimeProvider _time = new(Now);
    private readonly TicketService _service;

    public TicketServiceTests()
    {
        _service = new TicketService(_repository, _time);
    }

    // U01 — Tạo ticket chỉ với title (happy path).
    // Loại: Normal — kèm Boundary: repo rỗng → id nhỏ nhất (1); description 0 ký tự.
    // Kiểm tra mọi giá trị mặc định: id = 1 (repo rỗng), description rỗng, status Open
    // (không truyền --status thì mặc định là open),
    // priority Medium, không có tag, CreatedAt lấy từ đồng hồ, UpdatedAt = null,
    // và ticket trả về giống hệt ticket đã được lưu vào repo.
    [Fact]
    public void U01_Create_WithOnlyTitle_AppliesDefaultsAndSaves()
    {
        var ticket = _service.Create("Fix login bug");

        Assert.Equal(1, ticket.Id);
        Assert.Equal("Fix login bug", ticket.Title);
        Assert.Equal("", ticket.Description);
        Assert.Equal(TicketStatus.Open, ticket.Status);
        Assert.Equal(TicketPriority.Medium, ticket.Priority);
        Assert.Empty(ticket.Tags);
        Assert.Equal(Now, ticket.CreatedAt);
        Assert.Null(ticket.UpdatedAt);
        Assert.Equivalent(ticket, Assert.Single(_repository.Tickets), strict: true);
    }

    // U02 — Sinh id tự tăng theo quy tắc max(id) + 1.
    // Loại: Boundary — id mới = max + 1 khi dãy id có lỗ hổng.
    // Repo có id 1 và 5 (có lỗ hổng) → ticket mới phải là 6, không phải 3 (đếm số lượng).
    // [BVA] U01 là biên repo rỗng (id = 1); U02 là trường hợp có lỗ hổng trong dãy id.
    // [EG] Lỗi hay gặp: sinh id bằng count + 1 → trùng id với ticket #5 hiện có.
    [Fact]
    public void U02_Create_AssignsMaxIdPlusOne()
    {
        _repository.Seed(NewTicket(1), NewTicket(5));

        var ticket = _service.Create("New ticket");

        Assert.Equal(6, ticket.Id);
        Assert.Equal(3, _repository.Tickets.Count);
    }

    // U03 — Tạo ticket với đủ 5 field theo đề bài (title, description, status, priority, tags):
    //       giữ nguyên dữ liệu người dùng truyền vào và chuẩn hoá input.
    // Loại: Normal (đủ field) — kèm Abnormal: tag trùng / chỉ có khoảng trắng được
    //       xử lý êm (bị loại bỏ) thay vì báo lỗi.
    // title bị trim khoảng trắng hai đầu; description, status (InProgress — khác mặc định Open)
    // và priority giữ nguyên;
    // tags: trim → chữ thường → bỏ tag rỗng → bỏ trùng ([" API ", "api", "   "] → ["api"]).
    // [EP] Mỗi tag đại diện một miền: có khoảng trắng + chữ hoa / trùng / chỉ có khoảng trắng.
    // [EG] Dùng "   " thay vì "": nếu code bỏ tag rỗng TRƯỚC khi trim thì "   " sẽ lọt qua
    //      thành tag "" — lỗi này chỉ bị bắt khi tag chỉ chứa khoảng trắng.
    [Fact]
    public void U03_Create_KeepsProvidedFieldsAndNormalizesInput()
    {
        var ticket = _service.Create("  Fix login bug  ", "Cannot login with SSO",
                                     TicketStatus.InProgress, TicketPriority.High,
                                     [" API ", "api", "   "]);

        Assert.Equal("Fix login bug", ticket.Title);
        Assert.Equal("Cannot login with SSO", ticket.Description);
        Assert.Equal(TicketStatus.InProgress, ticket.Status);
        Assert.Equal(TicketPriority.High, ticket.Priority);
        Assert.Equal(["api"], ticket.Tags);
    }

    // U04 — Validation: title là bắt buộc.
    // Loại: Abnormal (thiếu dữ liệu) — kèm Boundary: "" là độ dài min - 1.
    // 3 cách để title "không có nội dung": null, chuỗi rỗng, chỉ có khoảng trắng
    // → ném ValidationException và KHÔNG có ticket nào được lưu.
    // [EP] Cả 3 thuộc miền không hợp lệ "title không có nội dung".
    // [EG] Vẫn giữ 3 đại diện vì mỗi giá trị bắt một lỗi thực tế khác nhau:
    //      null → NullReferenceException; "" → quên kiểm tra rỗng;
    //      "   " → dùng IsNullOrEmpty thay vì IsNullOrWhiteSpace.
    // [BVA] "" (0 ký tự) cũng là giá trị min - 1 của độ dài title.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void U04_Create_TitleMissingOrBlank_Throws(string? title)
    {
        Assert.Throws<ValidationException>(() => _service.Create(title));
        Assert.Empty(_repository.Tickets);
    }

    // U05 — Validation: vượt giới hạn độ dài (giá trị biên bên ngoài).
    // Loại: Abnormal (vượt quá ký tự) + Boundary (max + 1).
    // title 201 ký tự (giới hạn 200) hoặc description 2001 ký tự (giới hạn 2000)
    // → ném ValidationException và không lưu gì.
    // [BVA] Giá trị max + 1 của từng field; giá trị max hợp lệ nằm ở U06.
    [Theory]
    [InlineData(201, 0)]
    [InlineData(10, 2001)]
    public void U05_Create_FieldOverMaxLength_Throws(int titleLength, int descriptionLength)
    {
        Assert.Throws<ValidationException>(() =>
            _service.Create(new string('t', titleLength), new string('d', descriptionLength)));
        Assert.Empty(_repository.Tickets);
    }

    // U06 — Validation: các giá trị biên HỢP LỆ (cùng với U04/U05 là giá trị biên không hợp lệ).
    // Loại: Boundary — cả 3 dòng.
    // Mỗi dòng: (số ký tự của title, số ký tự của description, số khoảng trắng thêm vào hai đầu title)
    //   1) title 1 ký tự                    — [BVA] biên dưới (min); U04 "" là min - 1
    //   2) title 200 + description 2000     — [BVA] biên trên (max); U05 là max + 1.
    //                                         Cặp U05/U06 bắt lỗi viết nhầm `<` thành `<=`.
    //   3) title 200 ký tự + 2 khoảng trắng mỗi đầu (dài 204 trước trim, 200 sau trim)
    //                                       — [BVA + EG] giới hạn tính SAU khi trim; nếu code
    //                                         kiểm tra độ dài trước khi trim sẽ báo lỗi sai.
    [Theory]
    [InlineData(1, 0, 0)]
    [InlineData(200, 2000, 0)]
    [InlineData(200, 0, 2)]
    public void U06_Create_ValidBoundaryLengths_Succeeds(int titleLength, int descriptionLength, int padding)
    {
        var spaces = new string(' ', padding);
        var title = spaces + new string('t', titleLength) + spaces;

        var ticket = _service.Create(title, new string('d', descriptionLength));

        Assert.Equal(titleLength, ticket.Title.Length);
        Assert.Equal(descriptionLength, ticket.Description.Length);
    }

    // U07 — Logic lọc của List trên cùng một bộ 4 ticket mẫu:
    //   #1 Open,       High,   tags [api, backend]
    //   #2 InProgress, High,   tags [api]
    //   #3 Open,       Low,    tags [ui]
    //   #4 Done,       Medium, không có tag
    // Mỗi dòng là một trường hợp:
    //   1) Normal   — không lọc → trả tất cả, theo thứ tự id
    //   2) Normal   — lọc theo status
    //   3) Normal   — lọc theo priority
    //   4) Normal   — lọc theo 1 tag, không phân biệt hoa thường ("API" khớp tag "api")
    //   5) Normal   — lọc theo nhiều tag: ticket phải có ĐỦ mọi tag (AND) → chỉ #1
    //   6) Normal   — kết hợp status + priority + tag (điều kiện AND)
    //   7) Boundary — danh sách tag rỗng (0 tag) → coi như không lọc theo tag
    //   8) Boundary — không ticket nào khớp → danh sách rỗng (0 kết quả)
    // (Abnormal của list — giá trị lọc sai — nằm ở U12 và C08.)
    // [DT] Bảng quyết định 3 điều kiện (status × priority × tags, mỗi điều kiện có/không) = 8 tổ hợp.
    //      Tổ hợp (status + tags) và (priority + tags) được gộp: các bộ lọc độc lập, kết hợp theo AND;
    //      mỗi bộ lọc đã được chứng minh riêng (dòng 2–5) và dòng 6 (đủ 3 bộ lọc) sẽ fail nếu code
    //      bỏ sót bất kỳ bộ lọc nào khi kết hợp (ví dụ viết if/else-if).
    // [EG] Dòng 4: "API" viết hoa bắt lỗi lọc tag phân biệt hoa thường.
    //      Dòng 5: bắt lỗi dùng OR (có một trong các tag) thay vì AND — OR sẽ trả thêm #2.
    //      Dòng 7: bắt lỗi coi danh sách rỗng là "phải khớp tag rỗng" → trả về 0 ticket.
    [Theory]
    [InlineData(null, null, null, new[] { 1, 2, 3, 4 })]
    [InlineData(TicketStatus.Open, null, null, new[] { 1, 3 })]
    [InlineData(null, TicketPriority.High, null, new[] { 1, 2 })]
    [InlineData(null, null, new[] { "API" }, new[] { 1, 2 })]
    [InlineData(null, null, new[] { "api", "backend" }, new[] { 1 })]
    [InlineData(TicketStatus.Open, TicketPriority.High, new[] { "api" }, new[] { 1 })]
    [InlineData(null, null, new string[0], new[] { 1, 2, 3, 4 })]
    [InlineData(TicketStatus.Done, TicketPriority.High, null, new int[0])]
    public void U07_List_AppliesFilters(TicketStatus? status, TicketPriority? priority,
                                        string[]? tags, int[] expectedIds)
    {
        _repository.Seed(
            NewTicket(1, TicketStatus.Open, TicketPriority.High, "api", "backend"),
            NewTicket(2, TicketStatus.InProgress, TicketPriority.High, "api"),
            NewTicket(3, TicketStatus.Open, TicketPriority.Low, "ui"),
            NewTicket(4, TicketStatus.Done, TicketPriority.Medium));

        var tickets = _service.List(status, priority, tags);

        Assert.Equal(expectedIds, tickets.Select(t => t.Id));
    }

    // U08 — Cập nhật status.
    // Loại: Normal.
    // Status đổi sang Done, UpdatedAt = thời điểm hiện tại (đồng hồ đã tiến 1 giờ),
    // thay đổi được lưu vào repo, và các field khác (title, description, priority,
    // tags, CreatedAt) giữ nguyên.
    // [EG] Lỗi hay gặp: sửa ticket nhưng quên gọi SaveAll. Repo giả sao chép ticket mỗi lần
    //      đọc/ghi nên lỗi này làm test fail thay vì pass nhầm.
    [Fact]
    public void U08_UpdateStatus_ChangesStatusAndUpdatedAtOnly()
    {
        var original = NewTicket(1, TicketStatus.Open, TicketPriority.High, "api");
        _repository.Seed(original);
        _time.Advance(TimeSpan.FromHours(1));

        var updated = _service.UpdateStatus(1, TicketStatus.Done);

        Assert.Equal(TicketStatus.Done, updated.Status);
        Assert.Equal(Now.AddHours(1), updated.UpdatedAt);
        var saved = Assert.Single(_repository.Tickets);
        Assert.Equivalent(updated, saved, strict: true);
        Assert.Equal(original.Title, saved.Title);
        Assert.Equal(original.Description, saved.Description);
        Assert.Equal(original.Priority, saved.Priority);
        Assert.Equal(original.Tags, saved.Tags);
        Assert.Equal(original.CreatedAt, saved.CreatedAt);
    }

    // Dữ liệu cho U09: (thao tác cần tìm ticket theo id, id không tồn tại). Repo chỉ có ticket #1.
    public static TheoryData<Action<TicketService>, int> UnknownIdOperations => new()
    {
        { service => service.Get(0), 0 },                                // biên min - 1
        { service => service.Get(2), 2 },                                // biên max + 1
        { service => service.UpdateStatus(2, TicketStatus.Done), 2 },    // biên max + 1, qua UpdateStatus
    };

    // U09 — Không tìm thấy ticket.
    // Loại: Abnormal (id không tồn tại) + Boundary (id sát biên của dãy id hiện có).
    // Repo chỉ có ticket #1:
    //   Get(0)               → id 0 là min - 1 (không có ticket nào id <= 0)
    //   Get(2)               → id 2 là max + 1 (ngay sau ticket lớn nhất)
    //   UpdateStatus(2, ...) → cùng biên max + 1, qua thao tác cập nhật
    // → ném TicketNotFoundException mang đúng id, và ticket #1 không bị thay đổi.
    // [EP] Miền không hợp lệ "id không tồn tại" (miền hợp lệ "id tồn tại" nằm ở U08, C05).
    // [BVA] Chọn id sát biên thay vì một số bất kỳ như 99.
    // [EG] Lỗi hay gặp: dùng id làm chỉ số mảng (tickets[id - 1]) → id 0 gây
    //      ArgumentOutOfRangeException thay vì thông báo "not found".
    [Theory]
    [MemberData(nameof(UnknownIdOperations))]
    public void U09_UnknownId_ThrowsTicketNotFound(Action<TicketService> act, int unknownId)
    {
        _repository.Seed(NewTicket(1));

        var exception = Assert.Throws<TicketNotFoundException>(() => act(_service));

        Assert.Equal(unknownId, exception.TicketId);
        Assert.Equal(TicketStatus.Open, Assert.Single(_repository.Tickets).Status);
    }

    // Tạo nhanh một ticket mẫu để đưa sẵn vào repo.
    private static Ticket NewTicket(int id,
                                    TicketStatus status = TicketStatus.Open,
                                    TicketPriority priority = TicketPriority.Medium,
                                    params string[] tags) => new()
    {
        Id = id,
        Title = $"Ticket {id}",
        Description = $"Description {id}",
        Status = status,
        Priority = priority,
        Tags = tags.ToList(),
        CreatedAt = Now.AddDays(-1),
    };
}
