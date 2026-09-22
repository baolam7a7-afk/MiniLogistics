using MiniLogistics.BLL.DTOs.Shipment;

namespace MiniLogistics.BLL.Services.Shipment;

public interface IShipmentService
{
    // =====================================================
    // CREATE SHIPMENT
    // Admin / Seller
    // =====================================================

    Task<ShipmentResponseDTO> CreateAsync(
        long actorUserId,
        string actorRole,
        CreateShipmentDTO request);


    // =====================================================
    // ASSIGN SHIPPER
    // Admin / Seller
    // =====================================================

    Task<ShipmentResponseDTO> AssignShipperAsync(
        long shipmentId,
        long actorUserId,
        string actorRole,
        AssignShipperDTO request);


    // =====================================================
    // GET ALL
    // Admin
    // =====================================================

    Task<List<ShipmentResponseDTO>> GetAllAsync();


    // =====================================================
    // GET MY SHIPMENTS
    // Shipper
    // =====================================================

    Task<List<ShipmentResponseDTO>> GetMyShipmentsAsync(
        long shipperUserId);


    // =====================================================
    // GET DETAIL
    // Admin / Seller / Shipper
    // =====================================================

    Task<ShipmentResponseDTO> GetByIdAsync(
        long shipmentId,
        long userId,
        string role);


    // =====================================================
    // UPDATE STATUS
    // Shipper
    // =====================================================

    Task<ShipmentResponseDTO> UpdateStatusAsync(
        long shipmentId,
        long shipperUserId,
        UpdateShipmentStatusDTO request);
}