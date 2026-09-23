using MiniLogistics.BLL.DTOs.DisputeMessage;
using MiniLogistics.BLL.Exceptions;
using MiniLogistics.DAL.UnitOfWork;

using DisputeMessageModel =
    MiniLogistics.DAL.Models.DisputeMessage;

namespace MiniLogistics.BLL.Services.DisputeMessage;

public class DisputeMessageService : IDisputeMessageService
{
    private readonly IUnitOfWork _unitOfWork;

    public DisputeMessageService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }


    // =========================================================
    // CREATE
    // =========================================================

    public async Task<DisputeMessageResponseDTO> CreateAsync(
        long userId,
        string role,
        CreateDisputeMessageDTO request)
    {
        // -----------------------------------------------------
        // Validate request
        // -----------------------------------------------------

        if (request == null)
        {
            throw new BadRequestException(
                "DisputeMessage request không được null.");
        }

        role = role.Trim().ToLowerInvariant();


        // -----------------------------------------------------
        // Validate DisputeId
        // -----------------------------------------------------

        if (request.DisputeId <= 0)
        {
            throw new BadRequestException(
                "DisputeId không hợp lệ.");
        }


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
        // Tìm Dispute
        // -----------------------------------------------------

        var dispute =
            await _unitOfWork.Disputes
                .GetByIdAsync(request.DisputeId);

        if (dispute == null)
        {
            throw new NotFoundException(
                $"Dispute {request.DisputeId} không tồn tại.");
        }


        // -----------------------------------------------------
        // CUSTOMER
        // Chỉ được gửi vào Dispute của mình
        // -----------------------------------------------------

        if (role == "customer")
        {
            if (dispute.RaisedByUserId != userId)
            {
                throw new ForbiddenException(
                    "Bạn không có quyền gửi message vào Dispute này.");
            }
        }


        // -----------------------------------------------------
        // ADMIN
        // Được gửi vào mọi Dispute
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
                "Bạn không có quyền gửi Dispute Message.");
        }


        // -----------------------------------------------------
        // Không cho message vào Dispute đã đóng
        // -----------------------------------------------------

        if (dispute.Status.Trim().ToLowerInvariant() == "closed")
        {
            throw new BadRequestException(
                "Dispute đã đóng, không thể gửi thêm message.");
        }


        // -----------------------------------------------------
        // Tạo Message
        // -----------------------------------------------------

        var disputeMessage = new DisputeMessageModel
        {
            DisputeId = request.DisputeId,
            SenderUserId = userId,
            Message = messageText,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.DisputeMessages
            .AddAsync(disputeMessage);

        await _unitOfWork.SaveChangesAsync();

        return MapToResponse(disputeMessage);
    }


    // =========================================================
    // GET BY ID
    // =========================================================

    public async Task<DisputeMessageResponseDTO?> GetByIdAsync(
        long userId,
        string role,
        long id)
    {
        role = role.Trim().ToLowerInvariant();

        var message =
            await _unitOfWork.DisputeMessages
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
        // Chỉ được xem message thuộc Dispute của mình
        // -----------------------------------------------------

        if (role == "customer")
        {
            var dispute =
                await _unitOfWork.Disputes
                    .GetByIdAsync(message.DisputeId);

            if (dispute == null)
            {
                throw new NotFoundException(
                    $"Dispute {message.DisputeId} không tồn tại.");
            }

            if (dispute.RaisedByUserId != userId)
            {
                throw new ForbiddenException(
                    "Bạn không có quyền xem Dispute Message này.");
            }

            return MapToResponse(message);
        }


        // -----------------------------------------------------
        // ROLE KHÁC
        // -----------------------------------------------------

        throw new ForbiddenException(
            "Bạn không có quyền xem Dispute Message.");
    }


    // =========================================================
    // GET BY DISPUTE ID
    // =========================================================

    public async Task<IEnumerable<DisputeMessageResponseDTO>>
        GetByDisputeIdAsync(
            long userId,
            string role,
            long disputeId)
    {
        role = role.Trim().ToLowerInvariant();


        // -----------------------------------------------------
        // Tìm Dispute
        // -----------------------------------------------------

        var dispute =
            await _unitOfWork.Disputes
                .GetByIdAsync(disputeId);

        if (dispute == null)
        {
            throw new NotFoundException(
                $"Dispute {disputeId} không tồn tại.");
        }


        // -----------------------------------------------------
        // ADMIN
        // -----------------------------------------------------

        if (role == "admin")
        {
            // Cho phép xem mọi Dispute
        }


        // -----------------------------------------------------
        // CUSTOMER
        // Chỉ xem Dispute của mình
        // -----------------------------------------------------

        else if (role == "customer")
        {
            if (dispute.RaisedByUserId != userId)
            {
                throw new ForbiddenException(
                    "Bạn không có quyền xem Dispute Messages của Dispute này.");
            }
        }


        // -----------------------------------------------------
        // ROLE KHÁC
        // -----------------------------------------------------

        else
        {
            throw new ForbiddenException(
                "Bạn không có quyền xem Dispute Messages.");
        }


        // -----------------------------------------------------
        // Lấy Messages
        // -----------------------------------------------------

        var messages =
            await _unitOfWork.DisputeMessages
                .FindAsync(x => x.DisputeId == disputeId);

        return messages
            .OrderBy(x => x.CreatedAt)
            .Select(MapToResponse)
            .ToList();
    }


    // =========================================================
    // UPDATE
    // =========================================================

    public async Task<DisputeMessageResponseDTO> UpdateAsync(
        long userId,
        string role,
        long id,
        UpdateDisputeMessageDTO request)
    {
        // -----------------------------------------------------
        // Validate request
        // -----------------------------------------------------

        if (request == null)
        {
            throw new BadRequestException(
                "DisputeMessage request không được null.");
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
            await _unitOfWork.DisputeMessages
                .GetByIdAsync(id);

        if (message == null)
        {
            throw new NotFoundException(
                $"DisputeMessage {id} không tồn tại.");
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
                    "Bạn không có quyền sửa Dispute Message này.");
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
                "Bạn không có quyền sửa Dispute Message.");
        }


        // -----------------------------------------------------
        // Kiểm tra Dispute
        // -----------------------------------------------------

        var dispute =
            await _unitOfWork.Disputes
                .GetByIdAsync(message.DisputeId);

        if (dispute == null)
        {
            throw new NotFoundException(
                $"Dispute {message.DisputeId} không tồn tại.");
        }


        // -----------------------------------------------------
        // Không sửa khi Dispute đã đóng
        // -----------------------------------------------------

        if (dispute.Status.Trim().ToLowerInvariant() == "closed")
        {
            throw new BadRequestException(
                "Dispute đã đóng, không thể sửa message.");
        }


        // -----------------------------------------------------
        // Update
        // -----------------------------------------------------

        message.Message = messageText;

        _unitOfWork.DisputeMessages.Update(message);

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
            await _unitOfWork.DisputeMessages
                .GetByIdAsync(id);

        if (message == null)
        {
            throw new NotFoundException(
                $"DisputeMessage {id} không tồn tại.");
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
                    "Bạn không có quyền xóa Dispute Message này.");
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
                "Bạn không có quyền xóa Dispute Message.");
        }


        // -----------------------------------------------------
        // Kiểm tra Dispute
        // -----------------------------------------------------

        var dispute =
            await _unitOfWork.Disputes
                .GetByIdAsync(message.DisputeId);

        if (dispute == null)
        {
            throw new NotFoundException(
                $"Dispute {message.DisputeId} không tồn tại.");
        }


        // -----------------------------------------------------
        // Không xóa khi Dispute đã đóng
        // -----------------------------------------------------

        if (dispute.Status.Trim().ToLowerInvariant() == "closed")
        {
            throw new BadRequestException(
                "Dispute đã đóng, không thể xóa message.");
        }


        // -----------------------------------------------------
        // Delete
        // -----------------------------------------------------

        _unitOfWork.DisputeMessages.Delete(message);

        await _unitOfWork.SaveChangesAsync();
    }


    // =========================================================
    // MAPPING
    // =========================================================

    private DisputeMessageResponseDTO MapToResponse(
        DisputeMessageModel message)
    {
        return new DisputeMessageResponseDTO
        {
            Id = message.Id,
            DisputeId = message.DisputeId,
            SenderUserId = message.SenderUserId,
            Message = message.Message,
            CreatedAt = message.CreatedAt
        };
    }
}