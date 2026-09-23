using MiniLogistics.BLL.DTOs.SupportMessage;
using MiniLogistics.BLL.Exceptions;
using MiniLogistics.DAL.UnitOfWork;

using SupportMessageModel =
    MiniLogistics.DAL.Models.SupportMessage;

namespace MiniLogistics.BLL.Services.SupportMessage;

public class SupportMessageService : ISupportMessageService
{
    private readonly IUnitOfWork _unitOfWork;

    public SupportMessageService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }


    // =========================================================
    // CREATE
    // =========================================================

    public async Task<SupportMessageResponseDTO> CreateAsync(
        long userId,
        string role,
        CreateSupportMessageDTO request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        role = role.Trim().ToLowerInvariant();


        // -----------------------------------------------------
        // Validate Message
        // -----------------------------------------------------

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new BadRequestException(
                "Message không được để trống.");
        }

        var message = request.Message.Trim();

        if (message.Length > 5000)
        {
            throw new BadRequestException(
                "Message không được vượt quá 5000 ký tự.");
        }


        // -----------------------------------------------------
        // Kiểm tra Ticket
        // -----------------------------------------------------

        var ticket =
            await _unitOfWork.SupportTickets
                .GetByIdAsync(request.TicketId);

        if (ticket == null)
        {
            throw new NotFoundException(
                $"SupportTicket {request.TicketId} không tồn tại.");
        }


        // -----------------------------------------------------
        // CUSTOMER
        // Chỉ được gửi message vào Ticket của mình
        // -----------------------------------------------------

        if (role == "customer")
        {
            if (ticket.CreatedByUserId != userId)
            {
                throw new ForbiddenException(
                    "Bạn không có quyền gửi message vào Support Ticket này.");
            }
        }


        // -----------------------------------------------------
        // ADMIN
        // Được gửi vào mọi Ticket
        // -----------------------------------------------------

        else if (role == "admin")
        {
            // Cho phép
        }


        // -----------------------------------------------------
        // ROLE KHÁC
        // -----------------------------------------------------

        else
        {
            throw new ForbiddenException(
                "Bạn không có quyền gửi Support Message.");
        }


        // -----------------------------------------------------
        // Tạo Message
        // -----------------------------------------------------

        var supportMessage = new SupportMessageModel
        {
            TicketId = request.TicketId,
            SenderUserId = userId,
            Message = message,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.SupportMessages
            .AddAsync(supportMessage);

        await _unitOfWork.SaveChangesAsync();

        return MapToResponse(supportMessage);
    }


    // =========================================================
    // GET BY ID
    // =========================================================

    public async Task<SupportMessageResponseDTO?> GetByIdAsync(
        long userId,
        string role,
        long id)
    {
        role = role.Trim().ToLowerInvariant();

        var message =
            await _unitOfWork.SupportMessages
                .GetByIdAsync(id);

        if (message == null)
        {
            return null;
        }


        // -----------------------------------------------------
        // ADMIN
        // -----------------------------------------------------

        if (role == "admin")
        {
            return MapToResponse(message);
        }


        // -----------------------------------------------------
        // CUSTOMER
        // Customer chỉ được xem message
        // thuộc Ticket do mình tạo
        // -----------------------------------------------------

        if (role == "customer")
        {
            var ticket =
                await _unitOfWork.SupportTickets
                    .GetByIdAsync(message.TicketId);

            if (ticket == null)
            {
                throw new NotFoundException(
                    $"SupportTicket {message.TicketId} không tồn tại.");
            }

            if (ticket.CreatedByUserId != userId)
            {
                throw new ForbiddenException(
                    "Bạn không có quyền xem Support Message này.");
            }

            return MapToResponse(message);
        }


        // -----------------------------------------------------
        // ROLE KHÁC
        // -----------------------------------------------------

        throw new ForbiddenException(
            "Bạn không có quyền xem Support Message.");
    }


    // =========================================================
    // GET BY TICKET ID
    // =========================================================

    public async Task<IEnumerable<SupportMessageResponseDTO>>
        GetByTicketIdAsync(
            long userId,
            string role,
            long ticketId)
    {
        role = role.Trim().ToLowerInvariant();


        // -----------------------------------------------------
        // Kiểm tra Ticket
        // -----------------------------------------------------

        var ticket =
            await _unitOfWork.SupportTickets
                .GetByIdAsync(ticketId);

        if (ticket == null)
        {
            throw new NotFoundException(
                $"SupportTicket {ticketId} không tồn tại.");
        }


        // -----------------------------------------------------
        // ADMIN
        // -----------------------------------------------------

        if (role == "admin")
        {
            // Cho phép xem mọi message
        }


        // -----------------------------------------------------
        // CUSTOMER
        // Chỉ xem Ticket của mình
        // -----------------------------------------------------

        else if (role == "customer")
        {
            if (ticket.CreatedByUserId != userId)
            {
                throw new ForbiddenException(
                    "Bạn không có quyền xem Support Messages của Ticket này.");
            }
        }


        // -----------------------------------------------------
        // ROLE KHÁC
        // -----------------------------------------------------

        else
        {
            throw new ForbiddenException(
                "Bạn không có quyền xem Support Messages.");
        }


        // -----------------------------------------------------
        // Lấy Messages
        // -----------------------------------------------------

        var messages =
            await _unitOfWork.SupportMessages
                .FindAsync(x => x.TicketId == ticketId);

        return messages
            .OrderBy(x => x.CreatedAt)
            .Select(MapToResponse)
            .ToList();
    }


    // =========================================================
    // UPDATE
    // =========================================================

    public async Task<SupportMessageResponseDTO> UpdateAsync(
        long userId,
        string role,
        long id,
        UpdateSupportMessageDTO request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        role = role.Trim().ToLowerInvariant();


        // -----------------------------------------------------
        // Validate Message
        // -----------------------------------------------------

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new BadRequestException(
                "Message không được để trống.");
        }

        var messageText =
            request.Message.Trim();

        if (messageText.Length > 5000)
        {
            throw new BadRequestException(
                "Message không được vượt quá 5000 ký tự.");
        }


        // -----------------------------------------------------
        // Tìm Message
        // -----------------------------------------------------

        var message =
            await _unitOfWork.SupportMessages
                .GetByIdAsync(id);

        if (message == null)
        {
            throw new NotFoundException(
                $"SupportMessage {id} không tồn tại.");
        }


        // -----------------------------------------------------
        // CUSTOMER
        // Chỉ sửa message của chính mình
        // -----------------------------------------------------

        if (role == "customer")
        {
            if (message.SenderUserId != userId)
            {
                throw new ForbiddenException(
                    "Bạn không có quyền sửa Support Message này.");
            }
        }


        // -----------------------------------------------------
        // ADMIN
        // -----------------------------------------------------

        else if (role == "admin")
        {
            // Cho phép
        }


        // -----------------------------------------------------
        // ROLE KHÁC
        // -----------------------------------------------------

        else
        {
            throw new ForbiddenException(
                "Bạn không có quyền sửa Support Message.");
        }


        // -----------------------------------------------------
        // Update
        // -----------------------------------------------------

        message.Message = messageText;

        _unitOfWork.SupportMessages.Update(message);

        await _unitOfWork.SaveChangesAsync();

        return MapToResponse(message);
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
        // Tìm Message
        // -----------------------------------------------------

        var message =
            await _unitOfWork.SupportMessages
                .GetByIdAsync(id);

        if (message == null)
        {
            throw new NotFoundException(
                $"SupportMessage {id} không tồn tại.");
        }


        // -----------------------------------------------------
        // CUSTOMER
        // Chỉ xóa message của chính mình
        // -----------------------------------------------------

        if (role == "customer")
        {
            if (message.SenderUserId != userId)
            {
                throw new ForbiddenException(
                    "Bạn không có quyền xóa Support Message này.");
            }
        }


        // -----------------------------------------------------
        // ADMIN
        // -----------------------------------------------------

        else if (role == "admin")
        {
            // Cho phép
        }


        // -----------------------------------------------------
        // ROLE KHÁC
        // -----------------------------------------------------

        else
        {
            throw new ForbiddenException(
                "Bạn không có quyền xóa Support Message.");
        }


        // -----------------------------------------------------
        // Delete
        // -----------------------------------------------------

        _unitOfWork.SupportMessages.Delete(message);

        await _unitOfWork.SaveChangesAsync();
    }


    // =========================================================
    // MAPPING
    // =========================================================

    private SupportMessageResponseDTO MapToResponse(
        SupportMessageModel message)
    {
        return new SupportMessageResponseDTO
        {
            Id = message.Id,
            TicketId = message.TicketId,
            SenderUserId = message.SenderUserId,
            Message = message.Message,
            CreatedAt = message.CreatedAt
        };
    }
}