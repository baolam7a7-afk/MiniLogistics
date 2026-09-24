using MiniLogistics.BLL.DTOs.SellerDashboard;

namespace MiniLogistics.BLL.Services.SellerDashboard;

public interface ISellerDashboardService
{
    Task<SellerDashboardResponseDTO> GetDashboardAsync(long userId);
}