using MiniLogistics.BLL.DTOs.SupportTicket;
using MiniLogistics.BLL.Exceptions;
using MiniLogistics.DAL.UnitOfWork;

using SupportTicketModel =
    MiniLogistics.DAL.Models.SupportTicket;

namespace MiniLogistics.BLL.Services.SupportTicket;

public class SupportTicketService : ISupportTicketService
{
    private readonly IUnitOfWork _unitOfWork;

    public SupportTicketService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // =========================================================
    // CREATE
    // =========================================================

    public async Task<SupportTicketResponseDTO> CreateAsync(
        long userId,
        CreateSupportTicketDTO request)
    {
        if (request == null)
        {
            throw new BadRequestException(
                "Support Ticket request không được null.");
        }

        if (string.IsNullOrWhiteSpace(request.Subject))
        {
            throw new BadRequestException(
                "Subject không được để trống.");
        }

        var subject = request.Subject.Trim();

        if (subject.Length > 300)
        {
            throw new BadRequestException(
                "Subject không được vượt quá 300 ký tự.");
        }

        // -----------------------------------------------------
        // Nếu có OrderId thì kiểm tra Order tồn tại
        // -----------------------------------------------------

        if (request.OrderId.HasValue)
        {
            var order =
                await _unitOfWork.Orders.GetByIdAsync(
                    request.OrderId.Value);

            if (order == null)
            {
                throw new NotFoundException(
                    $"Order {request.OrderId.Value} không tồn tại.");
            }

            // Order phải thuộc user đang tạo ticket
            if (order.CustomerId != userId)
            {
                throw new ForbiddenException(
                    "Bạn không có quyền tạo Support Ticket cho Order này.");
            }
        }

        // -----------------------------------------------------
        // Tạo Ticket
        // -----------------------------------------------------

        var ticket = new SupportTicketModel
        {
            Subject = subject,
            Status = "open",
            CreatedByUserId = userId,
            OrderId = request.OrderId,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.SupportTickets.AddAsync(ticket);

        await _unitOfWork.SaveChangesAsync();

        return MapToResponse(ticket);
    }

    // =========================================================
    // GET BY ID
    // =========================================================

    public async Task<SupportTicketResponseDTO?> GetByIdAsync(
        long userId,
        string role,
        long id)
    {
        role = role.Trim().ToLowerInvariant();

        var ticket =
            await _unitOfWork.SupportTickets.GetByIdAsync(id);

        if (ticket == null)
        {
            return null;
        }

        // -----------------------------------------------------
        // ADMIN
        // Admin được xem mọi Ticket
        // -----------------------------------------------------

        if (role == "admin")
        {
            return MapToResponse(ticket);
        }

        // -----------------------------------------------------
        // CUSTOMER
        // Customer chỉ được xem Ticket của chính mình
        // -----------------------------------------------------

        if (role == "customer")
        {
            if (ticket.CreatedByUserId != userId)
            {
                throw new ForbiddenException(
                    "Bạn không có quyền xem Support Ticket này.");
            }

            return MapToResponse(ticket);
        }

        // -----------------------------------------------------
        // CÁC ROLE KHÁC
        // -----------------------------------------------------

        throw new ForbiddenException(
            "Bạn không có quyền xem Support Ticket.");
    }

    // =========================================================
    // GET MY TICKETS
    // =========================================================

    public async Task<IEnumerable<SupportTicketResponseDTO>>
        GetMyTicketsAsync(long userId)
    {
        var tickets =
            await _unitOfWork.SupportTickets.FindAsync(
                x => x.CreatedByUserId == userId);

        return tickets
            .OrderByDescending(x => x.CreatedAt)
            .Select(MapToResponse)
            .ToList();
    }

    // =========================================================
    // GET ALL
    // =========================================================

    public async Task<IEnumerable<SupportTicketResponseDTO>>
        GetAllAsync()
    {
        var tickets =
            await _unitOfWork.SupportTickets.GetAllAsync();

        return tickets
            .OrderByDescending(x => x.CreatedAt)
            .Select(MapToResponse)
            .ToList();
    }

    // =========================================================
    // UPDATE
    // =========================================================

    public async Task<SupportTicketResponseDTO> UpdateAsync(
        long userId,
        string role,
        long id,
        UpdateSupportTicketDTO request)
    {
        if (request == null)
        {
            throw new BadRequestException(
                "Support Ticket request không được null.");
        }

        role = role.Trim().ToLowerInvariant();

        // -----------------------------------------------------
        // Tìm Ticket
        // -----------------------------------------------------

        var ticket =
            await _unitOfWork.SupportTickets.GetByIdAsync(id);

        if (ticket == null)
        {
            throw new NotFoundException(
                $"SupportTicket {id} không tồn tại.");
        }

        // -----------------------------------------------------
        // Kiểm tra Subject
        // -----------------------------------------------------

        if (string.IsNullOrWhiteSpace(request.Subject))
        {
            throw new BadRequestException(
                "Subject không được để trống.");
        }

        var subject = request.Subject.Trim();

        if (subject.Length > 300)
        {
            throw new BadRequestException(
                "Subject không được vượt quá 300 ký tự.");
        }

        // -----------------------------------------------------
        // CUSTOMER
        // Chỉ được sửa Ticket của chính mình
        // -----------------------------------------------------

        if (role == "customer")
        {
            if (ticket.CreatedByUserId != userId)
            {
                throw new ForbiddenException(
                    "Bạn không có quyền sửa Support Ticket này.");
            }
        }

        // -----------------------------------------------------
        // ADMIN
        // Được sửa mọi Ticket
        // -----------------------------------------------------

        else if (role == "admin")
        {
            // Admin được phép tiếp tục
        }

        // -----------------------------------------------------
        // CÁC ROLE KHÁC
        // -----------------------------------------------------

        else
        {
            throw new ForbiddenException(
                "Bạn không có quyền sửa Support Ticket.");
        }

        // -----------------------------------------------------
        // Kiểm tra Status
        // -----------------------------------------------------

        if (string.IsNullOrWhiteSpace(request.Status))
        {
            throw new BadRequestException(
                "Status không được để trống.");
        }

        var status =
            request.Status.Trim().ToLowerInvariant();

        var allowedStatuses = new[]
        {
            "open",
            "in_progress",
            "resolved",
            "closed"
        };

        if (!allowedStatuses.Contains(status))
        {
            throw new BadRequestException(
                "Status không hợp lệ. " +
                "Chỉ chấp nhận: open, in_progress, resolved, closed.");
        }

        // -----------------------------------------------------
        // Update
        // -----------------------------------------------------

        ticket.Subject = subject;
        ticket.Status = status;

        _unitOfWork.SupportTickets.Update(ticket);

        await _unitOfWork.SaveChangesAsync();

        return MapToResponse(ticket);
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
        // Tìm Ticket
        // -----------------------------------------------------

        var ticket =
            await _unitOfWork.SupportTickets.GetByIdAsync(id);

        if (ticket == null)
        {
            throw new NotFoundException(
                $"SupportTicket {id} không tồn tại.");
        }

        // -----------------------------------------------------
        // CUSTOMER
        // Chỉ được xóa Ticket của mình
        // -----------------------------------------------------

        if (role == "customer")
        {
            if (ticket.CreatedByUserId != userId)
            {
                throw new ForbiddenException(
                    "Bạn không có quyền xóa Support Ticket này.");
            }
        }

        // -----------------------------------------------------
        // ADMIN
        // Được xóa mọi Ticket
        // -----------------------------------------------------

        else if (role == "admin")
        {
            // Admin được phép tiếp tục
        }

        // -----------------------------------------------------
        // CÁC ROLE KHÁC
        // -----------------------------------------------------

        else
        {
            throw new ForbiddenException(
                "Bạn không có quyền xóa Support Ticket.");
        }

        // -----------------------------------------------------
        // Delete
        // -----------------------------------------------------

        _unitOfWork.SupportTickets.Delete(ticket);

        await _unitOfWork.SaveChangesAsync();
    }

    // =========================================================
    // MAPPING
    // =========================================================

    private SupportTicketResponseDTO MapToResponse(
        SupportTicketModel ticket)
    {
        return new SupportTicketResponseDTO
        {
            Id = ticket.Id,
            Subject = ticket.Subject,
            Status = ticket.Status,
            CreatedByUserId = ticket.CreatedByUserId,
            OrderId = ticket.OrderId,
            CreatedAt = ticket.CreatedAt
        };
    }
}