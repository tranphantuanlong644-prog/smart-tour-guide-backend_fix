using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartTourGuide.Data.Context;
using SmartTourGuide.Data.Entities;
using SmartTourGuide.Shared.Constants;
using SmartTourGuide.Shared.Enums;

namespace SmartTourGuide.API.Controllers;

/// <summary>
/// Điều phối các nghiệp vụ quản trị tour, địa điểm tham quan
/// và mã QR của hệ thống SmartTourGuide.
/// Cung cấp các API phục vụ việc xem, tạo, cập nhật,
/// kích hoạt và vô hiệu hóa dữ liệu.
/// </summary>
/// <remarks>
/// Tất cả endpoint trong controller đều yêu cầu người dùng
/// có vai trò Administrator.
/// </remarks>
[ApiController]
[Authorize(Roles = nameof(UserRole.Administrator))]
[Route("api/admin")]

/// <summary>
/// Khởi tạo controller quản trị và tiếp nhận ngữ cảnh cơ sở dữ liệu.
/// </summary>
/// <param name="db">
/// Ngữ cảnh Entity Framework Core dùng để truy vấn và cập nhật dữ liệu.
/// </param>
public sealed class AdminController : ControllerBase
{
    private readonly SmartTourGuideDbContext _db;

    public AdminController(SmartTourGuideDbContext db) => _db = db;

    [HttpGet("tours")]

    /// <summary>
    /// Lấy danh sách các tour hiện có trong hệ thống.
    /// Gom nhóm các bản ghi mã QR theo TourId và chọn bản ghi
    /// đại diện để trả về thông tin tổng quan của từng tour.
    /// </summary>
    /// <param name="cancellationToken">
    /// Token hỗ trợ hủy thao tác truy vấn.
    /// </param>
    /// <returns>
    /// Danh sách tour bao gồm mã, tên, trạng thái và địa chỉ.
    /// </returns>
    public async Task<IActionResult> GetTours(CancellationToken cancellationToken)
    {
        var codes = await _db.TourQrCodes.AsNoTracking()
            .GroupBy(x => x.TourId)
            .Select(group => group.OrderBy(x => x.CreatedAt).First())
            .OrderBy(x => x.TourName)
            .Select(x => new { x.TourId, x.TourCode, x.TourName, x.IsActive, x.Address })
            .ToListAsync(cancellationToken);
        return Ok(codes.Select(x => new { Id = x.TourId, Code = x.TourCode, DefaultName = x.TourName, x.IsActive, x.Address }));
    }

    [HttpGet("tours/{id:guid}")]

    /// <summary>
    /// Lấy thông tin chi tiết của một tour theo mã định danh.
    /// Truy xuất thông tin tour từ bản ghi mã QR đại diện
    /// và danh sách địa điểm cùng nội dung bản dịch liên quan.
    /// </summary>
    /// <paramname="id">Mã định danh của tour.</param>
    /// <paramname="cancellationToken">Token hỗ trợ hủy truy vấn.</param>
    /// <returns>
    /// Thông tin chi tiết tour và danh sách địa điểm,
    /// hoặc NotFound nếu không tìm thấy tour.
    /// </returns>
    public async Task<IActionResult> GetTour(Guid id, CancellationToken cancellationToken)
    {
        var qr = await _db.TourQrCodes.AsNoTracking()
            .Where(x => x.TourId == id)
            .OrderBy(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (qr is null) return NotFound();

        var locations = await _db.TourLocations.AsNoTracking()
            .Where(x => x.TourId == id)
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new
            {
                // Kiểm tra mã địa điểm và thứ tự hiển thị đã được sử dụng
                // trong cùng tour hay chưa để tránh vi phạm chỉ mục duy nhất.
                x.Id, x.Code, x.DefaultName, x.Latitude, x.Longitude,
                x.TriggerRadiusMeters, x.DisplayOrder, x.ImageUrl, x.IsActive,
                Translations = x.Translations.Select(t => new
                {
                    t.LanguageCode, t.Title, t.Description, t.NarrationText,
                    t.AudioUrl, t.DurationSeconds, t.NarratorName
                })
            }).ToListAsync(cancellationToken);

        return Ok(new
        {
            Id = qr.TourId, Code = qr.TourCode, DefaultName = qr.TourName,
            qr.TourDescription, qr.Address, qr.CoverImageUrl, qr.IsActive,
            Translations = Array.Empty<object>(), Locations = locations
        });
    }

    [HttpPost("tours")]

    /// <summary>
    /// Tạo tour mới từ thông tin do quản trị viên cung cấp.
    /// Kiểm tra dữ liệu đầu vào, tạo mã định danh và bản ghi mã QR
    /// chứa thông tin tour, sau đó lưu vào cơ sở dữ liệu.
    /// </summary>
    /// <paramname="request">Thông tin tour cần tạo.</param>
    /// <paramname="cancellationToken">Token hỗ trợ hủy thao tác.</param>
    /// <returns>
    /// HTTP 201 Created cùng mã định danh tour nếu tạo thành công;
    /// HTTP 400 BadRequest nếu dữ liệu không hợp lệ.
    /// </returns>
    public async Task<IActionResult> CreateTour(
        [FromBody] SaveTourRequest request, CancellationToken cancellationToken)
    {
        var validation = ValidateTourRequest(request);
        if (validation is not null) return BadRequest(new { message = validation });
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var qr = CreateQr(id, request, now, now.AddYears(10));
        _db.TourQrCodes.Add(qr);
        await _db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetTour), new { id }, new { Id = id });
    }

    [HttpPut("tours/{id:guid}")]

    /// <summary>
    /// Cập nhật thông tin chung của một tour.
    /// Đồng bộ mã tour, tên, mô tả, lời chào, địa chỉ
    /// và hình ảnh trên các bản ghi mã QR thuộc tour.
    /// </summary>
    /// <paramname="id">Mã định danh của tour cần cập nhật.</param>
    /// <paramname="request">Thông tin cập nhật của tour.</param>
    /// <paramname="cancellationToken">Token hỗ trợ hủy thao tác.</param>
    /// <returns>
    /// HTTP 204 NoContent nếu cập nhật thành công;
    /// HTTP 404 NotFound nếu không tìm thấy tour.
    /// </returns>
    public async Task<IActionResult> UpdateTour(
        Guid id, [FromBody] SaveTourRequest request, CancellationToken cancellationToken)
    {
        var validation = ValidateTourRequest(request);
        if (validation is not null) return BadRequest(new { message = validation });
        var codes = await _db.TourQrCodes.Where(x => x.TourId == id).ToListAsync(cancellationToken);
        if (codes.Count == 0) return NotFound();
        var normalizedCode = request.Code.Trim().ToUpperInvariant();
        foreach (var code in codes)
        {
            code.TourCode = normalizedCode;
            code.TourName = request.DefaultName.Trim();
            code.TourDescription = request.Description.Trim();
            code.WelcomeMessage = request.WelcomeMessage?.Trim();
            code.Address = request.Address?.Trim();
            code.CoverImageUrl = request.CoverImageUrl?.Trim();
        }
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("tours/{tourId:guid}/locations")]

    /// <summary>
    /// Tạo địa điểm tham quan mới thuộc một tour.
    /// Kiểm tra tour tồn tại, kiểm tra mã địa điểm và thứ tự hiển thị
    /// không bị trùng, sau đó tạo địa điểm cùng bản dịch ban đầu.
    /// </summary>
    /// <paramname="tourId">Mã định danh tour chứa địa điểm.</param>
    /// <paramname="request">Thông tin địa điểm cần tạo.</param>
    /// <paramname="cancellationToken">Token hỗ trợ hủy thao tác.</param>
    /// <returns>
    /// HTTP 200 OK cùng mã định danh địa điểm nếu tạo thành công;
    /// HTTP 400, 404 hoặc 409 nếu dữ liệu không hợp lệ,
    /// tour không tồn tại hoặc bị trùng thông tin.
    /// </returns>
    public async Task<IActionResult> CreateLocation(
        Guid tourId, [FromBody] SaveLocationRequest request, CancellationToken cancellationToken)
    {

        // Kiểm tra tọa độ địa lý nằm trong phạm vi hợp lệ của Trái Đất.
        var validation = ValidateLocationRequest(request);
        if (validation is not null) return BadRequest(new { message = validation });
        if (!await _db.TourQrCodes.AnyAsync(x => x.TourId == tourId, cancellationToken))
            return NotFound(new { message = "Không tìm thấy tour." });
        var code = request.Code.Trim().ToUpperInvariant();
        if (await _db.TourLocations.AnyAsync(x => x.TourId == tourId
                && (x.Code == code || x.DisplayOrder == request.DisplayOrder), cancellationToken))
            return Conflict(new { message = "Mã địa điểm hoặc thứ tự đã được sử dụng trong tour." });

        // Kiem tra dia diem moi co nam trong ban kinh cua dia diem da co hay khong.
        var duplicateLocation = await FindOverlappingLocationAsync(
            tourId,
            request.Latitude,
            request.Longitude,
            request.TriggerRadiusMeters,
            null,
            cancellationToken);

        if (duplicateLocation is not null)
        {
            return Conflict(new
            {
                message = "Dia diem bi trung hoac nam trong vung kich hoat cua dia diem da ton tai.",
                duplicateLocation = new
                {
                    duplicateLocation.Value.Location.Id,
                    duplicateLocation.Value.Location.DefaultName,
                    duplicateLocation.Value.Location.Code,
                    distanceMeters = Math.Round(duplicateLocation.Value.DistanceMeters, 2),
                    duplicateLocation.Value.Location.TriggerRadiusMeters
                }
            });
        }
        var now = DateTimeOffset.UtcNow;
        var location = new TourLocation
        {
            Id = Guid.NewGuid(), TourId = tourId, Code = code,
            DefaultName = request.DefaultName.Trim(), Latitude = request.Latitude,
            Longitude = request.Longitude, TriggerRadiusMeters = request.TriggerRadiusMeters,
            DisplayOrder = request.DisplayOrder, ImageUrl = request.ImageUrl?.Trim(),
            IsActive = request.IsActive, CreatedAt = now
        };
        _db.TourLocations.Add(location);
        _db.TourLocationTranslations.Add(CreateLocationTranslation(location.Id, request, now));
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { location.Id });
    }

    [HttpPut("locations/{id:guid}")]

    /// <summary>
    /// Cập nhật thông tin của một địa điểm tham quan.
    /// Đồng thời tìm bản dịch theo mã ngôn ngữ được cung cấp,
    /// tạo bản dịch mới nếu chưa tồn tại hoặc cập nhật bản dịch hiện có.
    /// </summary>
    /// <paramname="id">Mã định danh địa điểm cần cập nhật.</param>
    /// <paramname="request">Thông tin cập nhật của địa điểm và bản dịch.</param>
    /// <paramname="cancellationToken">Token hỗ trợ hủy thao tác.</param>
    /// <returns>
    /// HTTP 204 NoContent nếu cập nhật thành công;
    /// HTTP 400, 404 hoặc 409 tùy theo kết quả xử lý.
    /// </returns>
    public async Task<IActionResult> UpdateLocation(
        Guid id, [FromBody] SaveLocationRequest request, CancellationToken cancellationToken)
    {
        var validation = ValidateLocationRequest(request);

        // Nếu chưa có bản dịch cho ngôn ngữ được chọn,
        // tạo bản dịch mới; nếu đã tồn tại thì cập nhật nội dung hiện tại.
        if (validation is not null) return BadRequest(new { message = validation });
        var location = await _db.TourLocations.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (location is null) return NotFound();
        var code = request.Code.Trim().ToUpperInvariant();
        if (await _db.TourLocations.AnyAsync(x => x.Id != id && x.TourId == location.TourId
                && (x.Code == code || x.DisplayOrder == request.DisplayOrder), cancellationToken))
            return Conflict(new { message = "Mã địa điểm hoặc thứ tự đã được sử dụng trong tour." });

        // Loai tru chinh dia diem dang cap nhat khoi danh sach kiem tra.
        var duplicateLocation = await FindOverlappingLocationAsync(
            location.TourId,
            request.Latitude,
            request.Longitude,
            request.TriggerRadiusMeters,
            id,
            cancellationToken);

        if (duplicateLocation is not null)
        {
            return Conflict(new
            {
                message = "Dia diem bi trung hoac nam trong vung kich hoat cua dia diem khac.",
                duplicateLocation = new
                {
                    duplicateLocation.Value.Location.Id,
                    duplicateLocation.Value.Location.DefaultName,
                    duplicateLocation.Value.Location.Code,
                    distanceMeters = Math.Round(duplicateLocation.Value.DistanceMeters, 2),
                    duplicateLocation.Value.Location.TriggerRadiusMeters
                }
            });
        }
        location.Code = code;
        location.DefaultName = request.DefaultName.Trim();
        location.Latitude = request.Latitude;
        location.Longitude = request.Longitude;
        location.TriggerRadiusMeters = request.TriggerRadiusMeters;
        location.DisplayOrder = request.DisplayOrder;
        location.ImageUrl = request.ImageUrl?.Trim();
        location.IsActive = request.IsActive;
        location.UpdatedAt = DateTimeOffset.UtcNow;
        var language = SupportedLanguageCodes.Normalize(request.LanguageCode);
        var translation = await _db.TourLocationTranslations.FirstOrDefaultAsync(
            x => x.TourLocationId == id && x.LanguageCode == language, cancellationToken);
        if (translation is null)
        {
            translation = CreateLocationTranslation(id, request, DateTimeOffset.UtcNow);
            _db.TourLocationTranslations.Add(translation);
        }
        else
        {
            UpdateTranslation(translation, request);
        }
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPatch("locations/{id:guid}/active")]

    /// <summary>
    /// Thay đổi trạng thái hoạt động của một địa điểm tham quan.
    /// Cập nhật IsActive và thời điểm cập nhật gần nhất.
    /// </summary>
    /// <paramname="id">Mã định danh địa điểm.</param>
    /// <paramname="request">Trạng thái hoạt động cần thiết lập.</param>
    /// <paramname="cancellationToken">Token hỗ trợ hủy thao tác.</param>
    /// <returns>
    /// HTTP 204 NoContent nếu cập nhật thành công;
    /// HTTP 404 NotFound nếu không tìm thấy địa điểm.
    /// </returns>
    public async Task<IActionResult> SetLocationActive(
        Guid id, [FromBody] SetActiveRequest request, CancellationToken cancellationToken)
    {
        var location = await _db.TourLocations.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (location is null) return NotFound();
        location.IsActive = request.IsActive;
        location.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPatch("tours/{id:guid}/active")]

    /// <summary>
    /// Thay đổi trạng thái hoạt động của toàn bộ mã QR
    /// thuộc một tour theo trạng thái được quản trị viên cung cấp.
    /// </summary>
    /// <paramname="id">Mã định danh tour.</param>
    /// <paramname="request">Trạng thái hoạt động cần thiết lập.</param>
    /// <paramname="cancellationToken">Token hỗ trợ hủy thao tác.</param>
    /// <returns>
    /// HTTP 204 NoContent nếu cập nhật thành công;
    /// HTTP 404 NotFound nếu tour không có bản ghi mã QR.
    /// </returns>
    public async Task<IActionResult> SetTourActive(
        Guid id, [FromBody] SetActiveRequest request, CancellationToken cancellationToken)
    {
        var codes = await _db.TourQrCodes.Where(x => x.TourId == id).ToListAsync(cancellationToken);
        if (codes.Count == 0) return NotFound();
        foreach (var code in codes) code.IsActive = request.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("qrcodes")]

    /// <summary>
    /// Lấy danh sách mã QR trong hệ thống.
    /// Sắp xếp theo thời điểm tạo giảm dần và trả về
    /// thông tin truy cập, tour liên kết, trạng thái và lịch sử sử dụng.
    /// </summary>
    /// <paramname="cancellationToken">Token hỗ trợ hủy truy vấn.</param>
    /// <returns>Danh sách mã QR cùng các thông tin quản lý.</returns>
    public async Task<IActionResult> GetQrCodes(CancellationToken cancellationToken)
    {
        var codes = await _db.TourQrCodes.AsNoTracking().OrderByDescending(x => x.CreatedAt)
            .Select(x => new { x.Id, x.AccessCode, x.TourId, TourName = x.TourName,
                x.IsActive, x.UsageCount, x.MaximumUsageCount, x.CreatedAt, x.ExpiresAt, x.LastUsedAt })
            .ToListAsync(cancellationToken);
        return Ok(codes);
    }

    [HttpPost("qrcodes")]

    /// <summary>
    /// Tạo mã QR truy cập mới cho một tour đang hoạt động.
    /// Sao chép thông tin tour từ bản ghi mã QR nguồn,
    /// tạo mã truy cập ngẫu nhiên và thiết lập thời hạn
    /// cùng giới hạn số lượt sử dụng theo yêu cầu.
    /// </summary>
    /// <paramname="request">
    /// Thông tin tạo mã QR gồm tour, thời hạn và giới hạn lượt sử dụng.
    /// </param>
    /// <paramname="cancellationToken">Token hỗ trợ hủy thao tác.</param>
    /// <returns>
    /// HTTP 201 Created nếu tạo thành công;
    /// HTTP 400 BadRequest nếu thông tin không hợp lệ
    /// hoặc tour không tồn tại hay không hoạt động.
    /// </returns>
    public async Task<IActionResult> CreateQrCode(
        [FromBody] CreateQrCodeRequest request, CancellationToken cancellationToken)
    {
        if (request.ExpiresInDays is < 1 or > 3650)
            return BadRequest(new { message = "Thời hạn QR phải từ 1 đến 3650 ngày." });
        if (request.MaximumUsageCount is <= 0)
            return BadRequest(new { message = "Số lượt dùng tối đa phải lớn hơn 0." });
        var source = await _db.TourQrCodes.AsNoTracking()
            .FirstOrDefaultAsync(x => x.TourId == request.TourId && x.IsActive, cancellationToken);
        if (source is null) return BadRequest(new { message = "Tour không tồn tại hoặc đang bị tắt." });
        var now = DateTimeOffset.UtcNow;
        var qr = new TourQrCode
        {
            Id = Guid.NewGuid(), TourId = source.TourId, TourCode = source.TourCode,
            TourName = source.TourName, TourDescription = source.TourDescription,
            WelcomeMessage = source.WelcomeMessage, Address = source.Address,
            CoverImageUrl = source.CoverImageUrl,

            // Sinh mã truy cập ngẫu nhiên từ 8 byte dữ liệu mật mã,
            // sau đó chuyển thành chuỗi hexadecimal.
            AccessCode = Convert.ToHexString(RandomNumberGenerator.GetBytes(8)),
            Type = QrCodeType.Reusable, IsActive = true, UsageCount = 0,
            MaximumUsageCount = request.MaximumUsageCount, CreatedAt = now,
            ExpiresAt = now.AddDays(request.ExpiresInDays)
        };
        _db.TourQrCodes.Add(qr);
        await _db.SaveChangesAsync(cancellationToken);
        return Created($"/api/admin/qrcodes/{qr.Id}", new
        {
            qr.Id, qr.AccessCode, qr.TourId, TourName = qr.TourName,
            qr.ExpiresAt, qr.MaximumUsageCount
        });
    }

    [HttpPatch("qrcodes/{id:guid}/deactivate")]

    /// <summary>
    /// Vô hiệu hóa một mã QR được xác định bằng ID.
    /// Cập nhật trạng thái hoạt động và ghi nhận thời điểm vô hiệu hóa.
    /// </summary>
    /// <paramname="id">Mã định danh mã QR cần vô hiệu hóa.</param>
    /// <paramname="cancellationToken">Token hỗ trợ hủy thao tác.</param>
    /// <returns>
    /// HTTP 204 NoContent nếu cập nhật thành công;
    /// HTTP 404 NotFound nếu không tìm thấy mã QR.
    /// </returns>
    public async Task<IActionResult> DeactivateQrCode(Guid id, CancellationToken cancellationToken)
    {
        var qr = await _db.TourQrCodes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (qr is null) return NotFound();

        // Vô hiệu hóa mã QR và ghi nhận thời điểm thực hiện thao tác.
        qr.IsActive = false;
        qr.DeactivatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Tạo một bản ghi mã QR mới từ thông tin tour được cung cấp.
    /// Sinh mã truy cập ngẫu nhiên và thiết lập loại QR,
    /// trạng thái hoạt động cùng thời hạn hiệu lực.
    /// </summary>
    /// <paramname="tourId">Mã định danh tour.</param>
    /// <paramname="request">Thông tin tour dùng để tạo mã QR.</param>
    /// <paramname="now">Thời điểm tạo mã QR.</param>
    /// <paramname="expiresAt">Thời điểm mã QR hết hạn.</param>
    /// <returns>Bản ghi TourQrCode được khởi tạo.</returns>
    private static TourQrCode CreateQr(Guid tourId, SaveTourRequest request, DateTimeOffset now, DateTimeOffset expiresAt) => new()
    {
        Id = Guid.NewGuid(), TourId = tourId, TourCode = request.Code.Trim().ToUpperInvariant(),
        TourName = request.DefaultName.Trim(), TourDescription = request.Description.Trim(),
        WelcomeMessage = request.WelcomeMessage?.Trim(), Address = request.Address?.Trim(),
        CoverImageUrl = request.CoverImageUrl?.Trim(),
        AccessCode = Convert.ToHexString(RandomNumberGenerator.GetBytes(8)),
        Type = QrCodeType.Reusable, IsActive = request.IsActive, CreatedAt = now,
        ExpiresAt = expiresAt
    };

    /// <summary>
    /// Tạo bản dịch ban đầu cho một địa điểm tham quan
    /// dựa trên dữ liệu được gửi trong yêu cầu quản trị.
    /// Chuẩn hóa mã ngôn ngữ trước khi khởi tạo bản dịch.
    /// </summary>
    /// <paramname="id">Mã định danh địa điểm.</param>
    /// <paramname="request">Thông tin nội dung bản dịch.</param>
    /// <paramname="now">Thời điểm tạo bản dịch.</param>
    /// <returns>Đối tượng TourLocationTranslation được khởi tạo.</returns>
    private static TourLocationTranslation CreateLocationTranslation(Guid id, SaveLocationRequest request, DateTimeOffset now)
    {
        var translation = new TourLocationTranslation
        {
            Id = Guid.NewGuid(), TourLocationId = id,
            LanguageCode = SupportedLanguageCodes.Normalize(request.LanguageCode), CreatedAt = now
        };
        UpdateTranslation(translation, request);
        return translation;
    }

    /// <summary>
    /// Cập nhật nội dung bản dịch của một địa điểm.
    /// Bao gồm tiêu đề, mô tả, văn bản thuyết minh,
    /// đường dẫn âm thanh, thời lượng và tên người dẫn chuyện.
    /// </summary>
    /// <paramname="translation">Bản dịch cần cập nhật.</param>
    /// <paramname="request">Dữ liệu bản dịch mới.</param>
    private static void UpdateTranslation(TourLocationTranslation translation, SaveLocationRequest request)
    {
        translation.Title = request.Title.Trim();
        translation.Description = request.Description.Trim();
        translation.NarrationText = request.NarrationText.Trim();
        translation.AudioUrl = request.AudioUrl?.Trim() ?? string.Empty;
        translation.DurationSeconds = request.DurationSeconds;
        translation.NarratorName = request.NarratorName?.Trim();
        translation.UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Kiểm tra tính hợp lệ của dữ liệu yêu cầu tạo hoặc cập nhật tour.
    /// Kiểm tra mã tour, tên tour, tên bản dịch,
    /// độ dài nội dung và mã ngôn ngữ.
    /// </summary>
    /// <paramname="request">Dữ liệu tour cần kiểm tra.</param>
    /// <returns>
    /// null nếu dữ liệu hợp lệ;
    /// chuỗi thông báo lỗi nếu có điều kiện không đáp ứng.
    /// </returns>
    /// <summary>
    /// Tim dia diem gan nhat co tam nam trong ban kinh kich hoat
    /// cua dia diem moi hoac nguoc lai.
    /// Cho phep hai vong tron giao nhau neu tam cua chung khong nam
    /// trong ban kinh kich hoat cua nhau.
    /// </summary>
    private async Task<(TourLocation Location, double DistanceMeters)?>
        FindOverlappingLocationAsync(
            Guid tourId,
            double latitude,
            double longitude,
            double newRadiusMeters,
            Guid? excludeLocationId,
            CancellationToken cancellationToken)
    {
        // Chi so sanh cac dia diem thuoc cung mot tour.
        var existingLocations = await _db.TourLocations
            .AsNoTracking()
            .Where(x => x.TourId == tourId
                && (!excludeLocationId.HasValue || x.Id != excludeLocationId.Value))
            .ToListAsync(cancellationToken);

        (TourLocation Location, double DistanceMeters)? nearest = null;

        foreach (var existing in existingLocations)
        {
            // Tinh khoang cach giua hai tam dia diem theo met.
            var distance = CalculateDistanceMeters(
                latitude,
                longitude,
                existing.Latitude,
                existing.Longitude);

            // Neu tam moi nam trong ban kinh cu hoac tam cu nam
            // trong ban kinh moi thi xem la dia diem bi trung.
            var duplicateThreshold = Math.Max(
                newRadiusMeters,
                existing.TriggerRadiusMeters);

            if (distance <= duplicateThreshold
                && (nearest is null || distance < nearest.Value.DistanceMeters))
            {
                nearest = (existing, distance);
            }
        }

        return nearest;
    }

    /// <summary>
    /// Tinh khoang cach giua hai toa do GPS bang cong thuc Haversine.
    /// Ket qua tra ve theo met.
    /// </summary>
    private static double CalculateDistanceMeters(
        double latitude1,
        double longitude1,
        double latitude2,
        double longitude2)
    {
        const double earthRadiusMeters = 6371000;

        var lat1 = Math.PI * latitude1 / 180.0;
        var lat2 = Math.PI * latitude2 / 180.0;
        var deltaLat = Math.PI * (latitude2 - latitude1) / 180.0;
        var deltaLon = Math.PI * (longitude2 - longitude1) / 180.0;

        var a = Math.Pow(Math.Sin(deltaLat / 2), 2)
            + Math.Cos(lat1) * Math.Cos(lat2)
            * Math.Pow(Math.Sin(deltaLon / 2), 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return earthRadiusMeters * c;
    }
    private static string? ValidateTourRequest(SaveTourRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || request.Code.Trim().Length > 50) return "Mã tour bắt buộc và tối đa 50 ký tự.";
        if (string.IsNullOrWhiteSpace(request.DefaultName) || request.DefaultName.Trim().Length > 200) return "Tên tour bắt buộc và tối đa 200 ký tự.";
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 200) return "Tên bản dịch bắt buộc và tối đa 200 ký tự.";
        if (request.Description?.Length > 4000 || request.WelcomeMessage?.Length > 2000) return "Mô tả/lời chào vượt quá độ dài cho phép.";
        if (!SupportedLanguageCodes.IsSupported(request.LanguageCode)) return "Ngôn ngữ không được hỗ trợ.";
        return null;
    }

    /// <summary>
    /// Kiểm tra tính hợp lệ của dữ liệu địa điểm và nội dung bản dịch.
    /// Bao gồm mã địa điểm, tên, tọa độ GPS, bán kính kích hoạt,
    /// thứ tự hiển thị, thời lượng và giới hạn nội dung.
    /// </summary>
    /// <paramname="request">Dữ liệu địa điểm cần kiểm tra.</param>
    /// <returns>
    /// null nếu dữ liệu hợp lệ;
    /// chuỗi thông báo lỗi nếu có điều kiện không đáp ứng.
    /// </returns>
    private static string? ValidateLocationRequest(SaveLocationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || request.Code.Trim().Length > 50) return "Mã địa điểm bắt buộc và tối đa 50 ký tự.";
        if (string.IsNullOrWhiteSpace(request.DefaultName) || request.DefaultName.Trim().Length > 200) return "Tên địa điểm bắt buộc và tối đa 200 ký tự.";
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Trim().Length > 200) return "Tiêu đề bản dịch bắt buộc và tối đa 200 ký tự.";
        if (request.Latitude is < -90 or > 90 || request.Longitude is < -180 or > 180) return "Vĩ độ/kinh độ không hợp lệ.";
        if (request.TriggerRadiusMeters is <= 0 or > 500 || request.DisplayOrder <= 0) return "Bán kính phải từ 0 đến 500m và thứ tự phải lớn hơn 0.";
        if (request.DurationSeconds < 0 || request.Description?.Length > 4000 || request.NarrationText?.Length > 10000 || request.AudioUrl?.Length > 2000) return "Nội dung, audio URL hoặc thời lượng không hợp lệ.";
        if (!SupportedLanguageCodes.IsSupported(request.LanguageCode)) return "Ngôn ngữ không được hỗ trợ.";
        return null;
    }

    /// <summary>
    /// Đại diện cho yêu cầu thay đổi trạng thái hoạt động
    /// của một địa điểm hoặc tour.
    /// </summary>
    public sealed record SetActiveRequest(bool IsActive);

    /// <summary>
    /// Đại diện cho dữ liệu tạo hoặc cập nhật tour,
    /// bao gồm thông tin chung và nội dung theo ngôn ngữ được chọn.
    /// </summary>
    public sealed record SaveTourRequest(string Code, string DefaultName, string LanguageCode, string Name,
        string Description, string? WelcomeMessage, string? Address, string? CoverImageUrl, bool IsActive = true);
   
    /// <summary>
    /// Đại diện cho dữ liệu tạo hoặc cập nhật địa điểm tham quan
    /// cùng nội dung thuyết minh và tọa độ GPS.
    /// </summary>
    public sealed record SaveLocationRequest(string Code, string DefaultName, string LanguageCode, string Title,
        string Description, string NarrationText, string? AudioUrl, double DurationSeconds, string? NarratorName,
        double Latitude, double Longitude, double TriggerRadiusMeters, int DisplayOrder, string? ImageUrl, bool IsActive = true);

    /// <summary>
    /// Đại diện cho yêu cầu tạo mã QR mới cho một tour.
    /// Cho phép thiết lập thời hạn và giới hạn lượt sử dụng.
    /// </summary>
    public sealed record CreateQrCodeRequest(Guid TourId, int ExpiresInDays = 30, int? MaximumUsageCount = null);
}
