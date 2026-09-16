
using MiniLogistics.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace MiniLogistics.DAL.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Shop> Shops => Set<Shop>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Voucher> Vouchers => Set<Voucher>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderStatusLog> OrderStatusLogs => Set<OrderStatusLog>();
    public DbSet<OrderVoucher> OrderVouchers => Set<OrderVoucher>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<ShipmentEvent> ShipmentEvents => Set<ShipmentEvent>();
    public DbSet<ReturnRequest> ReturnRequests => Set<ReturnRequest>();
    public DbSet<RefundTransaction> RefundTransactions => Set<RefundTransaction>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<ReviewReply> ReviewReplies => Set<ReviewReply>();
    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();
    public DbSet<SupportMessage> SupportMessages => Set<SupportMessage>();
    public DbSet<Dispute> Disputes => Set<Dispute>();
    public DbSet<DisputeMessage> DisputeMessages => Set<DisputeMessage>();
    public DbSet<ShopWallet> ShopWallets => Set<ShopWallet>();
    public DbSet<ShopWalletTransaction> ShopWalletTransactions => Set<ShopWalletTransaction>();
    public DbSet<PayoutRequest> PayoutRequests => Set<PayoutRequest>();
    public DbSet<ReportSnapshot> ReportSnapshots => Set<ReportSnapshot>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<User>(e => {
            e.ToTable("users"); e.HasKey(x => x.Id);
            e.Property(x => x.Email).HasMaxLength(255).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Phone).HasMaxLength(30);
            e.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
            e.Property(x => x.FullName).HasMaxLength(200);
            e.Property(x => x.AvatarUrl).HasMaxLength(1000);
            e.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("active").IsRequired();
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        });
        b.Entity<Role>(e => {
            e.ToTable("roles"); e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.Name).IsUnique();
        });
        b.Entity<UserRole>(e => {
            e.ToTable("user_roles"); e.HasKey(x => new { x.UserId, x.RoleId });
            e.Property(x => x.AssignedAt).HasDefaultValueSql("GETUTCDATE()");
            e.HasOne(x => x.User).WithMany(x => x.UserRoles).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Role).WithMany(x => x.UserRoles).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<UserSession>(e => {
            e.ToTable("user_sessions"); e.HasKey(x => x.Id);
            e.Property(x => x.RefreshTokenHash).HasMaxLength(500).IsRequired();
            e.Property(x => x.UserAgent).HasMaxLength(1000); e.Property(x => x.Ip).HasMaxLength(64);
            e.HasOne(x => x.User).WithMany(x => x.UserSessions).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<Address>(e => {
            e.ToTable("addresses"); e.HasKey(x => x.Id);
            e.Property(x => x.ReceiverName).HasMaxLength(200).IsRequired();
            e.Property(x => x.ReceiverPhone).HasMaxLength(30).IsRequired();
            e.Property(x => x.Line1).HasMaxLength(500).IsRequired();
            e.Property(x => x.Country).HasMaxLength(2).HasDefaultValue("VN").IsRequired();
            e.HasOne(x => x.User).WithMany(x => x.Addresses).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<Category>(e => {
            e.ToTable("categories"); e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired(); e.Property(x => x.Slug).HasMaxLength(255).IsRequired();
            e.HasIndex(x => x.Slug).IsUnique();
            e.HasOne(x => x.Parent).WithMany(x => x.Children).HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<Shop>(e => {
            e.ToTable("shops"); e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired(); e.Property(x => x.Slug).HasMaxLength(255).IsRequired();
            e.HasIndex(x => x.Slug).IsUnique(); e.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("pending");
            e.HasOne(x => x.OwnerUser).WithMany(x => x.Shops).HasForeignKey(x => x.OwnerUserId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<Product>(e => {
            e.ToTable("products"); e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(300).IsRequired(); e.Property(x => x.Slug).HasMaxLength(255).IsRequired();
            e.HasIndex(x => new { x.ShopId, x.Slug }).IsUnique();
            e.HasOne(x => x.Shop).WithMany(x => x.Products).HasForeignKey(x => x.ShopId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Category).WithMany(x => x.Products).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<ProductImage>(e => {
            e.ToTable("product_images"); e.HasKey(x => x.Id); e.Property(x => x.Url).HasMaxLength(1000).IsRequired();
            e.HasOne(x => x.Product).WithMany(x => x.ProductImages).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<ProductVariant>(e => {
            e.ToTable("product_variants"); e.HasKey(x => x.Id);
            e.Property(x => x.Sku).HasMaxLength(100); e.HasIndex(x => x.Sku).IsUnique().HasFilter("[sku] IS NOT NULL");
            e.Property(x => x.VariantName).HasMaxLength(300).IsRequired(); e.Property(x => x.Price).HasPrecision(18,2).IsRequired();
            e.Property(x => x.Stock).HasDefaultValue(0); e.HasOne(x => x.Product).WithMany(x => x.ProductVariants).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<Cart>(e => {
            e.ToTable("carts"); e.HasKey(x => x.Id); e.HasIndex(x => x.UserId).IsUnique();
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<CartItem>(e => {
            e.ToTable("cart_items"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.CartId, x.VariantId }).IsUnique();
            e.HasOne(x => x.Cart).WithMany(x => x.CartItems).HasForeignKey(x => x.CartId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Variant).WithMany(x => x.CartItems).HasForeignKey(x => x.VariantId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<Voucher>(e => {
            e.ToTable("vouchers"); e.HasKey(x => x.Id);
            e.Property(x => x.Scope).HasMaxLength(20).IsRequired(); e.Property(x => x.Code).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Code).IsUnique(); e.HasIndex(x => new { x.ShopId, x.Code }).IsUnique();
            e.Property(x => x.DiscountType).HasMaxLength(20).IsRequired(); e.Property(x => x.DiscountValue).HasPrecision(18,2).IsRequired();
            e.Property(x => x.MaxDiscount).HasPrecision(18,2); e.Property(x => x.MinOrderValue).HasPrecision(18,2);
            e.HasOne(x => x.Shop).WithMany(x => x.Vouchers).HasForeignKey(x => x.ShopId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<Order>(e => {
            e.ToTable("orders"); e.HasKey(x => x.Id); e.HasIndex(x => x.OrderCode).IsUnique();
            e.Property(x => x.OrderCode).HasMaxLength(50).IsRequired(); e.Property(x => x.Status).HasMaxLength(30).HasDefaultValue("pending");
            e.Property(x => x.Currency).HasMaxLength(3).HasDefaultValue("VND"); e.Property(x => x.PaymentMethod).HasMaxLength(20).IsRequired();
            e.Property(x => x.Subtotal).HasPrecision(18,2); e.Property(x => x.ShippingFee).HasPrecision(18,2);
            e.Property(x => x.DiscountTotal).HasPrecision(18,2); e.Property(x => x.Total).HasPrecision(18,2);
            e.HasOne(x => x.Customer).WithMany(x => x.CustomerOrders).HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Shop).WithMany(x => x.Orders).HasForeignKey(x => x.ShopId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ShippingAddress).WithMany().HasForeignKey(x => x.ShippingAddressId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<OrderItem>(e => {
            e.ToTable("order_items"); e.HasKey(x => x.Id);
            e.Property(x => x.ProductNameSnapshot).HasMaxLength(300).IsRequired(); e.Property(x => x.VariantNameSnapshot).HasMaxLength(300).IsRequired();
            e.Property(x => x.UnitPrice).HasPrecision(18,2); e.Property(x => x.LineTotal).HasPrecision(18,2);
            e.HasOne(x => x.Order).WithMany(x => x.OrderItems).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Product).WithMany(x => x.OrderItems).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Variant).WithMany(x => x.OrderItems).HasForeignKey(x => x.VariantId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<OrderStatusLog>(e => {
            e.ToTable("order_status_logs"); e.HasKey(x => x.Id); e.Property(x => x.ToStatus).HasMaxLength(30).IsRequired();
            e.HasOne(x => x.Order).WithMany(x => x.OrderStatusLogs).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.CreatedByUser).WithMany(x => x.OrderStatusLogs).HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<OrderVoucher>(e => {
            e.ToTable("order_vouchers"); e.HasKey(x => x.Id); e.Property(x => x.CodeSnapshot).HasMaxLength(100).IsRequired(); e.Property(x => x.DiscountAmount).HasPrecision(18,2);
            e.HasOne(x => x.Order).WithMany(x => x.OrderVouchers).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Voucher).WithMany(x => x.OrderVouchers).HasForeignKey(x => x.VoucherId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<PaymentTransaction>(e => {
            e.ToTable("payment_transactions"); e.HasKey(x => x.Id); e.Property(x => x.Provider).HasMaxLength(50);
            e.Property(x => x.Method).HasMaxLength(20).IsRequired(); e.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("pending");
            e.Property(x => x.Amount).HasPrecision(18,2); e.Property(x => x.ProviderTxnId).HasMaxLength(200);
            e.HasOne(x => x.Order).WithMany(x => x.PaymentTransactions).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<Shipment>(e => {
            e.ToTable("shipments"); e.HasKey(x => x.Id); e.HasIndex(x => x.OrderId).IsUnique(); e.HasIndex(x => x.TrackingCode)
    .IsUnique()
    .HasFilter("[TrackingCode] IS NOT NULL");
            e.Property(x => x.TrackingCode).HasMaxLength(100); e.Property(x => x.Status).HasMaxLength(30).HasDefaultValue("created"); e.Property(x => x.CodAmount).HasPrecision(18,2);
            e.HasOne(x => x.Order).WithOne(x => x.Shipment).HasForeignKey<Shipment>(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ShipperUser).WithMany(x => x.Shipments).HasForeignKey(x => x.ShipperUserId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<ShipmentEvent>(e => {
            e.ToTable("shipment_events"); e.HasKey(x => x.Id); e.Property(x => x.Status).HasMaxLength(30).IsRequired();
            e.HasOne(x => x.Shipment).WithMany(x => x.ShipmentEvents).HasForeignKey(x => x.ShipmentId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<ReturnRequest>(e => {
            e.ToTable("return_requests"); e.HasKey(x => x.Id); e.Property(x => x.Reason).HasMaxLength(500).IsRequired(); e.Property(x => x.Status).HasMaxLength(30).HasDefaultValue("requested");
            e.HasOne(x => x.Order).WithMany(x => x.ReturnRequests).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Customer).WithMany(x => x.ReturnRequests).HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.HandledByUser).WithMany().HasForeignKey(x => x.HandledByUserId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<RefundTransaction>(e => {
            e.ToTable("refund_transactions"); e.HasKey(x => x.Id); e.Property(x => x.Method).HasMaxLength(20).IsRequired(); e.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("pending"); e.Property(x => x.Amount).HasPrecision(18,2);
            e.HasOne(x => x.ReturnRequest).WithMany(x => x.RefundTransactions).HasForeignKey(x => x.ReturnRequestId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<Review>(e => {
            e.ToTable("reviews"); e.HasKey(x => x.Id); e.HasCheckConstraint("CK_reviews_rating", "[rating] BETWEEN 1 AND 5");
            e.HasOne(x => x.Order).WithMany(x => x.Reviews).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.OrderItem).WithMany(x => x.Reviews).HasForeignKey(x => x.OrderItemId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Product).WithMany(x => x.Reviews).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Customer).WithMany(x => x.Reviews).HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<ReviewReply>(e => {
            e.ToTable("review_replies"); e.HasKey(x => x.Id); e.HasIndex(x => x.ReviewId).IsUnique(); e.Property(x => x.Content).IsRequired();
            e.HasOne(x => x.Review).WithOne(x => x.ReviewReply).HasForeignKey<ReviewReply>(x => x.ReviewId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Shop).WithMany(x => x.ReviewReplies).HasForeignKey(x => x.ShopId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.RepliedByUser).WithMany(x => x.ReviewReplies).HasForeignKey(x => x.RepliedByUserId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<SupportTicket>(e => {
            e.ToTable("support_tickets"); e.HasKey(x => x.Id); e.Property(x => x.Subject).HasMaxLength(300).IsRequired(); e.Property(x => x.Status).HasMaxLength(30).HasDefaultValue("open");
            e.HasOne(x => x.CreatedByUser).WithMany(x => x.SupportTickets).HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Order).WithMany(x => x.SupportTickets).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<SupportMessage>(e => {
            e.ToTable("support_messages"); e.HasKey(x => x.Id); e.Property(x => x.Message).IsRequired();
            e.HasOne(x => x.Ticket).WithMany(x => x.SupportMessages).HasForeignKey(x => x.TicketId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.SenderUser).WithMany(x => x.SupportMessages).HasForeignKey(x => x.SenderUserId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<Dispute>(e => {
            e.ToTable("disputes"); e.HasKey(x => x.Id); e.Property(x => x.Reason).HasMaxLength(500).IsRequired(); e.Property(x => x.Status).HasMaxLength(30).HasDefaultValue("open");
            e.HasOne(x => x.Order).WithMany(x => x.Disputes).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.RaisedByUser).WithMany(x => x.RaisedDisputes).HasForeignKey(x => x.RaisedByUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.HandledByUser).WithMany(x => x.HandledDisputes).HasForeignKey(x => x.HandledByUserId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<DisputeMessage>(e => {
            e.ToTable("dispute_messages"); e.HasKey(x => x.Id); e.Property(x => x.Message).IsRequired();
            e.HasOne(x => x.Dispute).WithMany(x => x.DisputeMessages).HasForeignKey(x => x.DisputeId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.SenderUser).WithMany(x => x.DisputeMessages).HasForeignKey(x => x.SenderUserId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<ShopWallet>(e => {
            e.ToTable("shop_wallets"); e.HasKey(x => x.Id); e.HasIndex(x => x.ShopId).IsUnique(); e.Property(x => x.Balance).HasPrecision(18,2).HasDefaultValue(0);
            e.HasOne(x => x.Shop).WithOne(x => x.ShopWallet).HasForeignKey<ShopWallet>(x => x.ShopId).OnDelete(DeleteBehavior.Cascade);
        });
        b.Entity<ShopWalletTransaction>(e => {
            e.ToTable("shop_wallet_transactions"); e.HasKey(x => x.Id); e.Property(x => x.Type).HasMaxLength(20).IsRequired(); e.Property(x => x.Amount).HasPrecision(18,2);
            e.HasOne(x => x.Wallet).WithMany(x => x.Transactions).HasForeignKey(x => x.WalletId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Order).WithMany(x => x.ShopWalletTransactions).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<PayoutRequest>(e => {
            e.ToTable("payout_requests"); e.HasKey(x => x.Id); e.Property(x => x.Amount).HasPrecision(18,2);
            e.Property(x => x.BankAccountName).HasMaxLength(200).IsRequired(); e.Property(x => x.BankAccountNumber).HasMaxLength(100).IsRequired(); e.Property(x => x.BankName).HasMaxLength(200).IsRequired();
            e.Property(x => x.Status).HasMaxLength(20).HasDefaultValue("requested");
            e.HasOne(x => x.Shop).WithMany(x => x.PayoutRequests).HasForeignKey(x => x.ShopId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.ProcessedByUser).WithMany(x => x.ProcessedPayoutRequests).HasForeignKey(x => x.ProcessedByUserId).OnDelete(DeleteBehavior.Restrict);
        });
        b.Entity<ReportSnapshot>(e => {
            e.ToTable("report_snapshots"); e.HasKey(x => x.Id); e.Property(x => x.Scope).HasMaxLength(20).IsRequired(); e.Property(x => x.MetricsJson).IsRequired();
            e.HasOne(x => x.Shop).WithMany(x => x.ReportSnapshots).HasForeignKey(x => x.ShopId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
