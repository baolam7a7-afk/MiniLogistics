using MiniLogistics.DAL.Models;
using MiniLogistics.DAL.Repositories;

namespace MiniLogistics.DAL.UnitOfWork;

public interface IUnitOfWork
{
    // =====================================================
    // USER
    // =====================================================

    IRepository<User> Users { get; }

    IRepository<Role> Roles { get; }

    IRepository<UserRole> UserRoles { get; }

    IRepository<UserSession> UserSessions { get; }


    // =====================================================
    // ADDRESS
    // =====================================================

    IRepository<Address> Addresses { get; }


    // =====================================================
    // SHOP
    // =====================================================

    IRepository<Shop> Shops { get; }


    // =====================================================
    // CATEGORY
    // =====================================================

    IRepository<Category> Categories { get; }


    // =====================================================
    // PRODUCT
    // =====================================================

    IRepository<Product> Products { get; }

    IRepository<ProductVariant> ProductVariants { get; }

    IRepository<ProductImage> ProductImages { get; }


    // =====================================================
    // INVENTORY
    // =====================================================

    IRepository<Inventory> Inventories { get; }


    // =====================================================
    // CART
    // =====================================================

    IRepository<Cart> Carts { get; }

    IRepository<CartItem> CartItems { get; }


    // =====================================================
    // ORDER
    // =====================================================

    IRepository<Order> Orders { get; }

    IRepository<OrderItem> OrderItems { get; }

    IRepository<OrderStatusLog> OrderStatusLogs { get; }


    // =====================================================
    // REVIEW
    // =====================================================

    IRepository<Review> Reviews { get; }

    IRepository<ReviewReply> ReviewReplies { get; }


    // =====================================================
    // PAYMENT
    // =====================================================

    IRepository<PaymentTransaction> PaymentTransactions { get; }


    // =====================================================
    // SHIPMENT
    // =====================================================

    IRepository<Shipment> Shipments { get; }

    IRepository<ShipmentEvent> ShipmentEvents { get; }


    // =====================================================
    // VOUCHER
    // =====================================================

    IRepository<Voucher> Vouchers { get; }

    IRepository<OrderVoucher> OrderVouchers { get; }


    // =====================================================
    // SUPPORT TICKET
    // =====================================================

    IRepository<SupportTicket> SupportTickets { get; }

    IRepository<SupportMessage> SupportMessages { get; }


    // =====================================================
    // DISPUTE
    // =====================================================

    IRepository<Dispute> Disputes { get; }

    IRepository<DisputeMessage> DisputeMessages { get; }


    // =====================================================
    // RETURN
    // =====================================================

    IRepository<ReturnRequest> ReturnRequests { get; }


    // =====================================================
    // REFUND
    // =====================================================

    IRepository<RefundTransaction> RefundTransactions { get; }


    // =====================================================
    // SHOP WALLET
    // =====================================================

    IRepository<ShopWallet> ShopWallets { get; }

    IRepository<ShopWalletTransaction> ShopWalletTransactions { get; }


    // =====================================================
    // PAYOUT
    // =====================================================

    IRepository<PayoutRequest> PayoutRequests { get; }


    // =====================================================
    // REPORT
    // =====================================================

    IRepository<ReportSnapshot> ReportSnapshots { get; }


    // =====================================================
    // SAVE
    // =====================================================

    Task<int> SaveChangesAsync();


    // =====================================================
    // TRANSACTION
    // =====================================================

    Task<T> ExecuteInTransactionAsync<T>(
        Func<Task<T>> action);
}