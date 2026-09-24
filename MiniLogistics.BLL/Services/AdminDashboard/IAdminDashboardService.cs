using MiniLogistics.BLL.DTOs.AdminDashboard;

namespace MiniLogistics.BLL.Services.AdminDashboard;

public interface IAdminDashboardService
{
    Task<AdminDashboardResponseDTO> GetDashboardAsync();
}