using MiniLogistics.DAL.Data;
using MiniLogistics.DAL.Models;
using MiniLogistics.DAL.Repositories;

namespace MiniLogistics.DAL.UnitOfWork;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }


    // =====================================================
    // USER
    // =====================================================

    private IRepository<User>? _users;

    public IRepository<User> Users =>
        _users ??=
            new Repository<User>(_context);


    private IRepository<Role>? _roles;

    public IRepository<Role> Roles =>
        _roles ??=
            new Repository<Role>(_context);


    private IRepository<UserRole>? _userRoles;

    public IRepository<UserRole> UserRoles =>
        _userRoles ??=
            new Repository<UserRole>(_context);


    private IRepository<UserSession>? _userSessions;

    public IRepository<UserSession> UserSessions =>
        _userSessions ??=
            new Repository<UserSession>(_context);


    // =====================================================
    // ADDRESS
    // =====================================================

    private IRepository<Address>? _addresses;

    public IRepository<Address> Addresses =>
        _addresses ??=
            new Repository<Address>(_context);


    // =====================================================
    // SHOP
    // =====================================================

    private IRepository<Shop>? _shops;

    public IRepository<Shop> Shops =>
        _shops ??=
            new Repository<Shop>(_context);


    // =====================================================
    // CATEGORY
    // =====================================================

    private IRepository<Category>? _categories;

    public IRepository<Category> Categories =>
        _categories ??=
            new Repository<Category>(_context);


    // =====================================================
    // PRODUCT
    // =====================================================

    private IRepository<Product>? _products;

    public IRepository<Product> Products =>
        _products ??=
            new Repository<Product>(_context);


    private IRepository<ProductVariant>? _productVariants;

    public IRepository<ProductVariant> ProductVariants =>
        _productVariants ??=
            new Repository<ProductVariant>(_context);


    private IRepository<ProductImage>? _productImages;

    public IRepository<ProductImage> ProductImages =>
        _productImages ??=
            new Repository<ProductImage>(_context);


    // =====================================================
    // INVENTORY
    // =====================================================

    private IRepository<Inventory>? _inventories;

    public IRepository<Inventory> Inventories =>
        _inventories ??=
            new Repository<Inventory>(_context);


    // =====================================================
    // CART
    // =====================================================

    private IRepository<Cart>? _carts;

    public IRepository<Cart> Carts =>
        _carts ??=
            new Repository<Cart>(_context);


    private IRepository<CartItem>? _cartItems;

    public IRepository<CartItem> CartItems =>
        _cartItems ??=
            new Repository<CartItem>(_context);


    // =====================================================
    // ORDER
    // =====================================================

    private IRepository<Order>? _orders;

    public IRepository<Order> Orders =>
        _orders ??=
            new Repository<Order>(_context);


    private IRepository<OrderItem>? _orderItems;

    public IRepository<OrderItem> OrderItems =>
        _orderItems ??=
            new Repository<OrderItem>(_context);


    private IRepository<OrderStatusLog>? _orderStatusLogs;

    public IRepository<OrderStatusLog> OrderStatusLogs =>
        _orderStatusLogs ??=
            new Repository<OrderStatusLog>(_context);


    // =====================================================
    // REVIEW
    // =====================================================

    private IRepository<Review>? _reviews;

    public IRepository<Review> Reviews =>
        _reviews ??=
            new Repository<Review>(_context);


    private IRepository<ReviewReply>? _reviewReplies;

    public IRepository<ReviewReply> ReviewReplies =>
        _reviewReplies ??=
            new Repository<ReviewReply>(_context);


    // =====================================================
    // PAYMENT
    // =====================================================

    private IRepository<PaymentTransaction>? _paymentTransactions;

    public IRepository<PaymentTransaction> PaymentTransactions =>
        _paymentTransactions ??=
            new Repository<PaymentTransaction>(_context);


    // =====================================================
    // SHIPMENT
    // =====================================================

    private IRepository<Shipment>? _shipments;

    public IRepository<Shipment> Shipments =>
        _shipments ??=
            new Repository<Shipment>(_context);


    private IRepository<ShipmentEvent>? _shipmentEvents;

    public IRepository<ShipmentEvent> ShipmentEvents =>
        _shipmentEvents ??=
            new Repository<ShipmentEvent>(_context);


    // =====================================================
    // VOUCHER
    // =====================================================

    private IRepository<Voucher>? _vouchers;

    public IRepository<Voucher> Vouchers =>
        _vouchers ??=
            new Repository<Voucher>(_context);


    private IRepository<OrderVoucher>? _orderVouchers;

    public IRepository<OrderVoucher> OrderVouchers =>
        _orderVouchers ??=
            new Repository<OrderVoucher>(_context);


    // =====================================================
    // SUPPORT TICKET
    // =====================================================

    private IRepository<SupportTicket>? _supportTickets;

    public IRepository<SupportTicket> SupportTickets =>
        _supportTickets ??=
            new Repository<SupportTicket>(_context);


    private IRepository<SupportMessage>? _supportMessages;

    public IRepository<SupportMessage> SupportMessages =>
        _supportMessages ??=
            new Repository<SupportMessage>(_context);


    // =====================================================
    // DISPUTE
    // =====================================================

    private IRepository<Dispute>? _disputes;

    public IRepository<Dispute> Disputes =>
        _disputes ??=
            new Repository<Dispute>(_context);


    private IRepository<DisputeMessage>? _disputeMessages;

    public IRepository<DisputeMessage> DisputeMessages =>
        _disputeMessages ??=
            new Repository<DisputeMessage>(_context);


    // =====================================================
    // RETURN REQUEST
    // =====================================================

    private IRepository<ReturnRequest>? _returnRequests;

    public IRepository<ReturnRequest> ReturnRequests =>
        _returnRequests ??=
            new Repository<ReturnRequest>(_context);


    // =====================================================
    // REFUND
    // =====================================================

    private IRepository<RefundTransaction>? _refundTransactions;

    public IRepository<RefundTransaction> RefundTransactions =>
        _refundTransactions ??=
            new Repository<RefundTransaction>(_context);


    // =====================================================
    // SHOP WALLET
    // =====================================================

    private IRepository<ShopWallet>? _shopWallets;

    public IRepository<ShopWallet> ShopWallets =>
        _shopWallets ??=
            new Repository<ShopWallet>(_context);


    private IRepository<ShopWalletTransaction>? _shopWalletTransactions;

    public IRepository<ShopWalletTransaction> ShopWalletTransactions =>
        _shopWalletTransactions ??=
            new Repository<ShopWalletTransaction>(_context);


    // =====================================================
    // PAYOUT
    // =====================================================

    private IRepository<PayoutRequest>? _payoutRequests;

    public IRepository<PayoutRequest> PayoutRequests =>
        _payoutRequests ??=
            new Repository<PayoutRequest>(_context);


    // =====================================================
    // REPORT
    // =====================================================

    private IRepository<ReportSnapshot>? _reportSnapshots;

    public IRepository<ReportSnapshot> ReportSnapshots =>
        _reportSnapshots ??=
            new Repository<ReportSnapshot>(_context);


    // =====================================================
    // SAVE CHANGES
    // =====================================================

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }


    // =====================================================
    // TRANSACTION
    // =====================================================

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<Task<T>> action)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            var result = await action();

            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            return result;
        }
        catch
        {
            await transaction.RollbackAsync();

            throw;
        }
    }
}