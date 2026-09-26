using MiniLogistics.BLL.DTOs.Common;
using MiniLogistics.BLL.DTOs.Dispute;
using MiniLogistics.BLL.Exceptions;
using MiniLogistics.DAL.UnitOfWork;

using DisputeModel =
    MiniLogistics.DAL.Models.Dispute;

namespace MiniLogistics.BLL.Services.Dispute;

public class DisputeService : IDisputeService
{
    private readonly IUnitOfWork _unitOfWork;

    public DisputeService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }


    // =========================================================
    // CREATE
    // =========================================================

    public async Task<DisputeResponseDTO> CreateAsync(
        long userId,
        CreateDisputeDTO request)
    {
        if (request == null)
        {
            throw new BadRequestException(
                "Dispute request không được null.");
        }

        if (request.OrderId <= 0)
        {
            throw new BadRequestException(
                "OrderId không hợp lệ.");
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new BadRequestException(
                "Reason không được để trống.");
        }

        var reason = request.Reason.Trim();

        if (reason.Length > 2000)
        {
            throw new BadRequestException(
                "Reason không được vượt quá 2000 ký tự.");
        }


        // -----------------------------------------------------
        // Kiểm tra Order
        // -----------------------------------------------------

        var order =
            await _unitOfWork.Orders
                .GetByIdAsync(request.OrderId);

        if (order == null)
        {
            throw new NotFoundException(
                $"Order {request.OrderId} không tồn tại.");
        }


        // -----------------------------------------------------
        // Order phải thuộc Customer đang tạo Dispute
        // -----------------------------------------------------

        if (order.CustomerId != userId)
        {
            throw new ForbiddenException(
                "Bạn không có quyền tạo Dispute cho Order này.");
        }


        // -----------------------------------------------------
        // Không cho tạo nhiều Dispute chưa đóng
        // cho cùng một Order
        // -----------------------------------------------------

        var existingDisputes =
            await _unitOfWork.Disputes.FindAsync(
                x =>
                    x.OrderId == request.OrderId &&
                    x.RaisedByUserId == userId &&
                    x.Status != "closed");

        if (existingDisputes.Any())
        {
            throw new BadRequestException(
                "Order này đang có một Dispute chưa đóng.");
        }


        // -----------------------------------------------------
        // Tạo Dispute
        // -----------------------------------------------------

        var dispute = new DisputeModel
        {
            OrderId = request.OrderId,

            RaisedByUserId = userId,

            Reason = reason,

            Status = "open",

            CreatedAt = DateTime.UtcNow,

            HandledByUserId = null,

            HandledAt = null
        };

        await _unitOfWork.Disputes
            .AddAsync(dispute);

        await _unitOfWork.SaveChangesAsync();

        return MapToResponse(dispute);
    }


    // =========================================================
    // GET BY ID
    // =========================================================

    public async Task<DisputeResponseDTO?> GetByIdAsync(
        long userId,
        string role,
        long id)
    {
        role = role.Trim().ToLowerInvariant();

        var dispute =
            await _unitOfWork.Disputes
                .GetByIdAsync(id);

        if (dispute == null)
        {
            return null;
        }


        // -----------------------------------------------------
        // ADMIN
        // Admin được xem mọi Dispute
        // -----------------------------------------------------

        if (role == "admin")
        {
            return MapToResponse(dispute);
        }


        // -----------------------------------------------------
        // CUSTOMER
        // Chỉ được xem Dispute của chính mình
        // -----------------------------------------------------

        if (role == "customer")
        {
            if (dispute.RaisedByUserId != userId)
            {
                throw new ForbiddenException(
                    "Bạn không có quyền xem Dispute này.");
            }

            return MapToResponse(dispute);
        }


        // -----------------------------------------------------
        // ROLE KHÁC
        // -----------------------------------------------------

        throw new ForbiddenException(
            "Bạn không có quyền xem Dispute.");
    }


    // =========================================================
    // GET MY DISPUTES
    // PAGINATION
    // =========================================================

    public async Task<PagedResponseDTO<DisputeResponseDTO>>
        GetMyDisputesAsync(
            long userId,
            DisputePaginationRequestDTO request)
    {
        if (request == null)
        {
            request = new DisputePaginationRequestDTO();
        }


        // -----------------------------------------------------
        // Validate Pagination
        // -----------------------------------------------------

        if (request.Page < 1)
        {
            request.Page = 1;
        }

        if (request.PageSize < 1)
        {
            request.PageSize = 10;
        }

        if (request.PageSize > 100)
        {
            request.PageSize = 100;
        }


        var status =
            request.Status?
                .Trim()
                .ToLowerInvariant();

        var search =
            request.Search?
                .Trim()
                .ToLowerInvariant();


        // -----------------------------------------------------
        // Get dữ liệu
        // -----------------------------------------------------

        var disputes =
            await _unitOfWork.Disputes
                .FindAsync(
                    x => x.RaisedByUserId == userId);


        // -----------------------------------------------------
        // Filter
        // -----------------------------------------------------

        if (!string.IsNullOrWhiteSpace(status))
        {
            disputes = disputes
                .Where(x =>
                    x.Status
                        .Trim()
                        .ToLowerInvariant()
                        == status)
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            disputes = disputes
                .Where(x =>
                    x.Reason
                        .ToLowerInvariant()
                        .Contains(search)
                    ||
                    x.OrderId
                        .ToString()
                        .Contains(search))
                .ToList();
        }


        // -----------------------------------------------------
        // Sort
        // -----------------------------------------------------

        var ordered =
            disputes
                .OrderByDescending(x => x.CreatedAt)
                .ToList();


        // -----------------------------------------------------
        // Total
        // -----------------------------------------------------

        var totalItems = ordered.Count;

        var totalPages =
            totalItems == 0
                ? 0
                : (int)Math.Ceiling(
                    totalItems /
                    (double)request.PageSize);


        // -----------------------------------------------------
        // Pagination
        // -----------------------------------------------------

        var items =
            ordered
                .Skip(
                    (request.Page - 1)
                    * request.PageSize)
                .Take(request.PageSize)
                .Select(MapToResponse)
                .ToList();


        // -----------------------------------------------------
        // Response
        // -----------------------------------------------------

        return new PagedResponseDTO<DisputeResponseDTO>
        {
            Items = items,

            Page = request.Page,

            PageSize = request.PageSize,

            TotalItems = totalItems,

            TotalPages = totalPages
        };
    }


    // =========================================================
    // GET ALL
    // ADMIN
    // PAGINATION
    // =========================================================

    public async Task<PagedResponseDTO<DisputeResponseDTO>>
        GetAllAsync(
            DisputePaginationRequestDTO request)
    {
        if (request == null)
        {
            request = new DisputePaginationRequestDTO();
        }


        // -----------------------------------------------------
        // Validate Pagination
        // -----------------------------------------------------

        if (request.Page < 1)
        {
            request.Page = 1;
        }

        if (request.PageSize < 1)
        {
            request.PageSize = 10;
        }

        if (request.PageSize > 100)
        {
            request.PageSize = 100;
        }


        var status =
            request.Status?
                .Trim()
                .ToLowerInvariant();

        var search =
            request.Search?
                .Trim()
                .ToLowerInvariant();


        // -----------------------------------------------------
        // Get tất cả
        // -----------------------------------------------------

        var disputes =
            await _unitOfWork.Disputes
                .GetAllAsync();


        // -----------------------------------------------------
        // Filter Status
        // -----------------------------------------------------

        if (!string.IsNullOrWhiteSpace(status))
        {
            disputes = disputes
                .Where(x =>
                    x.Status
                        .Trim()
                        .ToLowerInvariant()
                        == status)
                .ToList();
        }


        // -----------------------------------------------------
        // Search
        // -----------------------------------------------------

        if (!string.IsNullOrWhiteSpace(search))
        {
            disputes = disputes
                .Where(x =>
                    x.Reason
                        .ToLowerInvariant()
                        .Contains(search)
                    ||
                    x.OrderId
                        .ToString()
                        .Contains(search)
                    ||
                    x.RaisedByUserId
                        .ToString()
                        .Contains(search))
                .ToList();
        }


        // -----------------------------------------------------
        // Sort
        // -----------------------------------------------------

        var ordered =
            disputes
                .OrderByDescending(x => x.CreatedAt)
                .ToList();


        // -----------------------------------------------------
        // Total
        // -----------------------------------------------------

        var totalItems = ordered.Count;

        var totalPages =
            totalItems == 0
                ? 0
                : (int)Math.Ceiling(
                    totalItems /
                    (double)request.PageSize);


        // -----------------------------------------------------
        // Pagination
        // -----------------------------------------------------

        var items =
            ordered
                .Skip(
                    (request.Page - 1)
                    * request.PageSize)
                .Take(request.PageSize)
                .Select(MapToResponse)
                .ToList();


        // -----------------------------------------------------
        // Response
        // -----------------------------------------------------

        return new PagedResponseDTO<DisputeResponseDTO>
        {
            Items = items,

            Page = request.Page,

            PageSize = request.PageSize,

            TotalItems = totalItems,

            TotalPages = totalPages
        };
    }


    // =========================================================
    // UPDATE
    // =========================================================

    public async Task<DisputeResponseDTO> UpdateAsync(
        long userId,
        string role,
        long id,
        UpdateDisputeDTO request)
    {
        if (request == null)
        {
            throw new BadRequestException(
                "Dispute request không được null.");
        }

        role = role.Trim().ToLowerInvariant();


        // -----------------------------------------------------
        // Tìm Dispute
        // -----------------------------------------------------

        var dispute =
            await _unitOfWork.Disputes
                .GetByIdAsync(id);

        if (dispute == null)
        {
            throw new NotFoundException(
                $"Dispute {id} không tồn tại.");
        }


        // -----------------------------------------------------
        // Validate Status
        // -----------------------------------------------------

        if (string.IsNullOrWhiteSpace(request.Status))
        {
            throw new BadRequestException(
                "Status không được để trống.");
        }

        var status =
            request.Status
                .Trim()
                .ToLowerInvariant();

        var allowedStatuses = new[]
        {
            "open",
            "in_progress",
            "resolved",
            "rejected",
            "closed"
        };

        if (!allowedStatuses.Contains(status))
        {
            throw new BadRequestException(
                "Status không hợp lệ. " +
                "Chỉ chấp nhận: open, in_progress, resolved, rejected, closed.");
        }


        // -----------------------------------------------------
        // CUSTOMER
        // -----------------------------------------------------

        if (role == "customer")
        {
            if (dispute.RaisedByUserId != userId)
            {
                throw new ForbiddenException(
                    "Bạn không có quyền sửa Dispute này.");
            }


            // Customer chỉ được đóng Dispute
            if (status != "closed")
            {
                throw new ForbiddenException(
                    "Customer chỉ có thể đóng Dispute của mình.");
            }

            dispute.Status = status;
        }


        // -----------------------------------------------------
        // ADMIN
        // -----------------------------------------------------

        else if (role == "admin")
        {
            dispute.Status = status;

            dispute.HandledByUserId = userId;

            dispute.HandledAt = DateTime.UtcNow;
        }


        // -----------------------------------------------------
        // ROLE KHÁC
        // -----------------------------------------------------

        else
        {
            throw new ForbiddenException(
                "Bạn không có quyền cập nhật Dispute.");
        }


        // -----------------------------------------------------
        // Save
        // -----------------------------------------------------

        _unitOfWork.Disputes.Update(dispute);

        await _unitOfWork.SaveChangesAsync();

        return MapToResponse(dispute);
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


        // -----------------------------------------------------
        // Tìm Dispute
        // -----------------------------------------------------

        var dispute =
            await _unitOfWork.Disputes
                .GetByIdAsync(id);

        if (dispute == null)
        {
            throw new NotFoundException(
                $"Dispute {id} không tồn tại.");
        }


        // -----------------------------------------------------
        // CUSTOMER
        // -----------------------------------------------------

        if (role == "customer")
        {
            if (dispute.RaisedByUserId != userId)
            {
                throw new ForbiddenException(
                    "Bạn không có quyền xóa Dispute này.");
            }


            // Chỉ xóa Dispute đang open
            if (dispute.Status != "open")
            {
                throw new BadRequestException(
                    "Chỉ có thể xóa Dispute đang ở trạng thái open.");
            }
        }


        // -----------------------------------------------------
        // ADMIN
        // -----------------------------------------------------

        else if (role == "admin")
        {
            // Admin được xóa
        }


        // -----------------------------------------------------
        // ROLE KHÁC
        // -----------------------------------------------------

        else
        {
            throw new ForbiddenException(
                "Bạn không có quyền xóa Dispute.");
        }


        // -----------------------------------------------------
        // Delete
        // -----------------------------------------------------

        _unitOfWork.Disputes.Delete(dispute);

        await _unitOfWork.SaveChangesAsync();
    }


    // =========================================================
    // MAPPING
    // =========================================================

    private DisputeResponseDTO MapToResponse(
        DisputeModel dispute)
    {
        return new DisputeResponseDTO
        {
            Id = dispute.Id,

            OrderId = dispute.OrderId,

            RaisedByUserId =
                dispute.RaisedByUserId,

            Reason = dispute.Reason,

            Status = dispute.Status,

            CreatedAt = dispute.CreatedAt,

            HandledByUserId =
                dispute.HandledByUserId,

            HandledAt =
                dispute.HandledAt
        };
    }
}