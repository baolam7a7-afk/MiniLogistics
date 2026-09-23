using System.Text.Json;

using MiniLogistics.BLL.DTOs.ReportSnapshot;
using MiniLogistics.BLL.Exceptions;
using MiniLogistics.DAL.UnitOfWork;

using ReportSnapshotModel =
    MiniLogistics.DAL.Models.ReportSnapshot;

namespace MiniLogistics.BLL.Services.ReportSnapshot;

public class ReportSnapshotService : IReportSnapshotService
{
    private readonly IUnitOfWork _unitOfWork;

    public ReportSnapshotService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }


    // =========================================================
    // CREATE
    // =========================================================

    public async Task<ReportSnapshotResponseDTO> CreateAsync(
        long userId,
        string role,
        CreateReportSnapshotDTO request)
    {
        // -----------------------------------------------------
        // Validate request
        // -----------------------------------------------------

        if (request == null)
        {
            throw new BadRequestException(
                "ReportSnapshot request không được null.");
        }

        role = role.Trim().ToLowerInvariant();

        ValidateRequest(
            request.Scope,
            request.ShopId,
            request.DateFrom,
            request.DateTo,
            request.MetricsJson);


        // -----------------------------------------------------
        // SELLER
        // Seller chỉ được tạo snapshot cho Shop của mình
        // -----------------------------------------------------

        if (role == "seller")
        {
            if (!request.ShopId.HasValue)
            {
                throw new BadRequestException(
                    "Seller phải cung cấp ShopId.");
            }

            var shop = await _unitOfWork.Shops
                .GetByIdAsync(request.ShopId.Value);

            if (shop == null)
            {
                throw new NotFoundException(
                    $"Shop {request.ShopId.Value} không tồn tại.");
            }

            if (shop.OwnerUserId != userId)
            {
                throw new ForbiddenException(
                    "Bạn không có quyền tạo ReportSnapshot cho Shop này.");
            }
        }


        // -----------------------------------------------------
        // ADMIN
        // -----------------------------------------------------

        else if (role == "admin")
        {
            // Admin được phép.
        }


        // -----------------------------------------------------
        // ROLE KHÁC
        // -----------------------------------------------------

        else
        {
            throw new ForbiddenException(
                "Bạn không có quyền tạo ReportSnapshot.");
        }


        // -----------------------------------------------------
        // Scope = shop phải có ShopId
        // -----------------------------------------------------

        if (request.Scope.Trim().ToLowerInvariant() == "shop"
            && !request.ShopId.HasValue)
        {
            throw new BadRequestException(
                "Scope 'shop' yêu cầu ShopId.");
        }


        // -----------------------------------------------------
        // Nếu có ShopId thì Shop phải tồn tại
        // -----------------------------------------------------

        if (request.ShopId.HasValue)
        {
            var shop = await _unitOfWork.Shops
                .GetByIdAsync(request.ShopId.Value);

            if (shop == null)
            {
                throw new NotFoundException(
                    $"Shop {request.ShopId.Value} không tồn tại.");
            }
        }


        // -----------------------------------------------------
        // Create
        // -----------------------------------------------------

        var snapshot = new ReportSnapshotModel
        {
            Scope = request.Scope.Trim().ToLowerInvariant(),
            ShopId = request.ShopId,
            DateFrom = request.DateFrom,
            DateTo = request.DateTo,
            MetricsJson = request.MetricsJson.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.ReportSnapshots
            .AddAsync(snapshot);

        await _unitOfWork.SaveChangesAsync();

        return MapToResponse(snapshot);
    }


    // =========================================================
    // GET BY ID
    // =========================================================

    public async Task<ReportSnapshotResponseDTO?> GetByIdAsync(
        long userId,
        string role,
        long id)
    {
        role = role.Trim().ToLowerInvariant();

        var snapshot =
            await _unitOfWork.ReportSnapshots
                .GetByIdAsync(id);

        if (snapshot == null)
        {
            return null;
        }


        // -----------------------------------------------------
        // ADMIN
        // -----------------------------------------------------

        if (role == "admin")
        {
            return MapToResponse(snapshot);
        }


        // -----------------------------------------------------
        // SELLER
        // Seller chỉ được xem snapshot của Shop mình
        // -----------------------------------------------------

        if (role == "seller")
        {
            if (!snapshot.ShopId.HasValue)
            {
                throw new ForbiddenException(
                    "Seller không có quyền xem ReportSnapshot này.");
            }

            var shop =
                await _unitOfWork.Shops
                    .GetByIdAsync(snapshot.ShopId.Value);

            if (shop == null)
            {
                throw new NotFoundException(
                    $"Shop {snapshot.ShopId.Value} không tồn tại.");
            }

            if (shop.OwnerUserId != userId)
            {
                throw new ForbiddenException(
                    "Bạn không có quyền xem ReportSnapshot này.");
            }

            return MapToResponse(snapshot);
        }


        // -----------------------------------------------------
        // ROLE KHÁC
        // -----------------------------------------------------

        throw new ForbiddenException(
            "Bạn không có quyền xem ReportSnapshot.");
    }


    // =========================================================
    // GET ALL
    // ADMIN ONLY
    // =========================================================

    public async Task<IEnumerable<ReportSnapshotResponseDTO>>
        GetAllAsync()
    {
        var snapshots =
            await _unitOfWork.ReportSnapshots
                .GetAllAsync();

        return snapshots
            .OrderByDescending(x => x.CreatedAt)
            .Select(MapToResponse)
            .ToList();
    }


    // =========================================================
    // GET BY SHOP
    // =========================================================

    public async Task<IEnumerable<ReportSnapshotResponseDTO>>
        GetByShopIdAsync(
            long userId,
            string role,
            long shopId)
    {
        role = role.Trim().ToLowerInvariant();

        if (shopId <= 0)
        {
            throw new BadRequestException(
                "ShopId không hợp lệ.");
        }


        // -----------------------------------------------------
        // Tìm Shop
        // -----------------------------------------------------

        var shop =
            await _unitOfWork.Shops
                .GetByIdAsync(shopId);

        if (shop == null)
        {
            throw new NotFoundException(
                $"Shop {shopId} không tồn tại.");
        }


        // -----------------------------------------------------
        // ADMIN
        // -----------------------------------------------------

        if (role == "admin")
        {
            // Admin được xem.
        }


        // -----------------------------------------------------
        // SELLER
        // -----------------------------------------------------

        else if (role == "seller")
        {
            if (shop.OwnerUserId != userId)
            {
                throw new ForbiddenException(
                    "Bạn không có quyền xem ReportSnapshot của Shop này.");
            }
        }


        // -----------------------------------------------------
        // ROLE KHÁC
        // -----------------------------------------------------

        else
        {
            throw new ForbiddenException(
                "Bạn không có quyền xem ReportSnapshot.");
        }


        // -----------------------------------------------------
        // Lấy snapshots
        // -----------------------------------------------------

        var snapshots =
            await _unitOfWork.ReportSnapshots
                .FindAsync(
                    x => x.ShopId == shopId);

        return snapshots
            .OrderByDescending(x => x.CreatedAt)
            .Select(MapToResponse)
            .ToList();
    }


    // =========================================================
    // UPDATE
    // =========================================================

    public async Task<ReportSnapshotResponseDTO> UpdateAsync(
        long userId,
        string role,
        long id,
        UpdateReportSnapshotDTO request)
    {
        // -----------------------------------------------------
        // Validate request
        // -----------------------------------------------------

        if (request == null)
        {
            throw new BadRequestException(
                "ReportSnapshot request không được null.");
        }

        role = role.Trim().ToLowerInvariant();

        ValidateRequest(
            request.Scope,
            request.ShopId,
            request.DateFrom,
            request.DateTo,
            request.MetricsJson);


        // -----------------------------------------------------
        // Tìm snapshot
        // -----------------------------------------------------

        var snapshot =
            await _unitOfWork.ReportSnapshots
                .GetByIdAsync(id);

        if (snapshot == null)
        {
            throw new NotFoundException(
                $"ReportSnapshot {id} không tồn tại.");
        }


        // -----------------------------------------------------
        // ADMIN
        // -----------------------------------------------------

        if (role == "admin")
        {
            // Admin được phép.
        }


        // -----------------------------------------------------
        // SELLER
        // -----------------------------------------------------

        else if (role == "seller")
        {
            if (!snapshot.ShopId.HasValue)
            {
                throw new ForbiddenException(
                    "Seller không có quyền sửa ReportSnapshot này.");
            }

            var shop =
                await _unitOfWork.Shops
                    .GetByIdAsync(snapshot.ShopId.Value);

            if (shop == null)
            {
                throw new NotFoundException(
                    $"Shop {snapshot.ShopId.Value} không tồn tại.");
            }

            if (shop.OwnerUserId != userId)
            {
                throw new ForbiddenException(
                    "Bạn không có quyền sửa ReportSnapshot này.");
            }


            // Seller không được chuyển snapshot sang Shop khác
            if (request.ShopId != snapshot.ShopId)
            {
                throw new ForbiddenException(
                    "Seller không được chuyển ReportSnapshot sang Shop khác.");
            }
        }


        // -----------------------------------------------------
        // ROLE KHÁC
        // -----------------------------------------------------

        else
        {
            throw new ForbiddenException(
                "Bạn không có quyền sửa ReportSnapshot.");
        }


        // -----------------------------------------------------
        // Scope = shop phải có ShopId
        // -----------------------------------------------------

        if (request.Scope.Trim().ToLowerInvariant() == "shop"
            && !request.ShopId.HasValue)
        {
            throw new BadRequestException(
                "Scope 'shop' yêu cầu ShopId.");
        }


        // -----------------------------------------------------
        // Shop phải tồn tại nếu có ShopId
        // -----------------------------------------------------

        if (request.ShopId.HasValue)
        {
            var shop =
                await _unitOfWork.Shops
                    .GetByIdAsync(request.ShopId.Value);

            if (shop == null)
            {
                throw new NotFoundException(
                    $"Shop {request.ShopId.Value} không tồn tại.");
            }
        }


        // -----------------------------------------------------
        // Update
        // -----------------------------------------------------

        snapshot.Scope =
            request.Scope.Trim().ToLowerInvariant();

        snapshot.ShopId =
            request.ShopId;

        snapshot.DateFrom =
            request.DateFrom;

        snapshot.DateTo =
            request.DateTo;

        snapshot.MetricsJson =
            request.MetricsJson.Trim();

        _unitOfWork.ReportSnapshots
            .Update(snapshot);

        await _unitOfWork.SaveChangesAsync();

        return MapToResponse(snapshot);
    }


    // =========================================================
    // DELETE
    // =========================================================

    public async Task DeleteAsync(
        long userId,
        string role,
        long id)
    {
        role = role.Trim().ToLowerInvariant();

        var snapshot =
            await _unitOfWork.ReportSnapshots
                .GetByIdAsync(id);

        if (snapshot == null)
        {
            throw new NotFoundException(
                $"ReportSnapshot {id} không tồn tại.");
        }


        // -----------------------------------------------------
        // ADMIN
        // -----------------------------------------------------

        if (role == "admin")
        {
            // Admin được xóa.
        }


        // -----------------------------------------------------
        // SELLER
        // -----------------------------------------------------

        else if (role == "seller")
        {
            if (!snapshot.ShopId.HasValue)
            {
                throw new ForbiddenException(
                    "Seller không có quyền xóa ReportSnapshot này.");
            }

            var shop =
                await _unitOfWork.Shops
                    .GetByIdAsync(snapshot.ShopId.Value);

            if (shop == null)
            {
                throw new NotFoundException(
                    $"Shop {snapshot.ShopId.Value} không tồn tại.");
            }

            if (shop.OwnerUserId != userId)
            {
                throw new ForbiddenException(
                    "Bạn không có quyền xóa ReportSnapshot này.");
            }
        }


        // -----------------------------------------------------
        // ROLE KHÁC
        // -----------------------------------------------------

        else
        {
            throw new ForbiddenException(
                "Bạn không có quyền xóa ReportSnapshot.");
        }


        // -----------------------------------------------------
        // Delete
        // -----------------------------------------------------

        _unitOfWork.ReportSnapshots
            .Delete(snapshot);

        await _unitOfWork.SaveChangesAsync();
    }


    // =========================================================
    // VALIDATE REQUEST
    // =========================================================

    private static void ValidateRequest(
        string scope,
        long? shopId,
        DateOnly dateFrom,
        DateOnly dateTo,
        string metricsJson)
    {
        // -----------------------------------------------------
        // Scope
        // -----------------------------------------------------

        if (string.IsNullOrWhiteSpace(scope))
        {
            throw new BadRequestException(
                "Scope không được để trống.");
        }

        scope = scope.Trim().ToLowerInvariant();

        var allowedScopes = new[]
        {
            "system",
            "shop"
        };

        if (!allowedScopes.Contains(scope))
        {
            throw new BadRequestException(
                "Scope không hợp lệ. " +
                "Chỉ chấp nhận: system, shop.");
        }


        // -----------------------------------------------------
        // Date
        // -----------------------------------------------------

        if (dateFrom > dateTo)
        {
            throw new BadRequestException(
                "DateFrom không được lớn hơn DateTo.");
        }


        // -----------------------------------------------------
        // MetricsJson
        // -----------------------------------------------------

        if (string.IsNullOrWhiteSpace(metricsJson))
        {
            throw new BadRequestException(
                "MetricsJson không được để trống.");
        }

        if (metricsJson.Length > 100000)
        {
            throw new BadRequestException(
                "MetricsJson quá lớn.");
        }

        try
        {
            JsonDocument.Parse(metricsJson);
        }
        catch (JsonException)
        {
            throw new BadRequestException(
                "MetricsJson phải là JSON hợp lệ.");
        }
    }


    // =========================================================
    // MAP
    // =========================================================

    private ReportSnapshotResponseDTO MapToResponse(
        ReportSnapshotModel snapshot)
    {
        return new ReportSnapshotResponseDTO
        {
            Id = snapshot.Id,
            Scope = snapshot.Scope,
            ShopId = snapshot.ShopId,
            DateFrom = snapshot.DateFrom,
            DateTo = snapshot.DateTo,
            MetricsJson = snapshot.MetricsJson,
            CreatedAt = snapshot.CreatedAt
        };
    }
}