using FoodOrder.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FoodOrder.Data;

public partial class FoodOrderContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
{
    public FoodOrderContext(DbContextOptions<FoodOrderContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Ingredient> Ingredients { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<OrderDetail> OrderDetails { get; set; }

    public virtual DbSet<OrderStatusHistory> OrderStatusHistories { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    public virtual DbSet<ProductImage> ProductImages { get; set; }

    public virtual DbSet<ProductIngredient> ProductIngredients { get; set; }

    public virtual DbSet<ProductOption> ProductOptions { get; set; }

    public virtual DbSet<Review> Reviews { get; set; }

    public virtual DbSet<Table> Tables { get; set; }

    public virtual DbSet<TableSession> TableSessions { get; set; }

    public virtual DbSet<KitchenQueueRow> KitchenQueue { get; set; }

    public virtual DbSet<ReadyToServeRow> ReadyToServe { get; set; }

    public virtual DbSet<RevenueByDayRow> RevenueByDay { get; set; }

    public virtual DbSet<PeakHourRow> PeakHours { get; set; }

    public virtual DbSet<TopProductRow> TopProducts { get; set; }

    public virtual DbSet<TableUsageRow> TableUsage { get; set; }

    public virtual DbSet<LowStockRow> LowStock { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>(e =>
        {
            e.Property(u => u.Id).HasColumnName("UserId");
            e.Property(u => u.FullName).HasMaxLength(100);
        });
        modelBuilder.Entity<IdentityRole>().Property(r => r.Id).HasColumnName("RoleId");
        modelBuilder.Entity<IdentityUserClaim<string>>().Property(c => c.Id).HasColumnName("UserClaimId");
        modelBuilder.Entity<IdentityRoleClaim<string>>().Property(c => c.Id).HasColumnName("RoleClaimId");

        modelBuilder.Entity<Category>(e =>
        {
            e.Property(x => x.CategoryName).HasMaxLength(100);
            e.Property(x => x.Slug).HasMaxLength(120);
        });

        modelBuilder.Entity<Product>(e =>
        {
            e.Property(x => x.ProductName).HasMaxLength(200);
            e.Property(x => x.Price).HasColumnType("decimal(18,0)");
            e.Property(x => x.SalePrice).HasColumnType("decimal(18,0)");
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.Composition).HasMaxLength(500);
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasOne(x => x.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProductImage>(e =>
        {
            e.Property(x => x.ImageUrl).HasMaxLength(500);
            e.HasOne(x => x.Product)
                .WithMany(p => p.ProductImages)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProductOption>(e =>
        {
            e.Property(x => x.OptionName).HasMaxLength(50);
            e.Property(x => x.OptionValue).HasMaxLength(50);
            e.Property(x => x.ExtraPrice).HasColumnType("decimal(18,0)");
            e.HasOne(x => x.Product)
                .WithMany(p => p.ProductOptions)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Ingredient>(e =>
        {
            e.Property(x => x.IngredientName).HasMaxLength(100);
            e.Property(x => x.Unit).HasMaxLength(20);
            e.Property(x => x.StockQty).HasColumnType("decimal(18,3)");
            e.Property(x => x.MinQty).HasColumnType("decimal(18,3)");
            e.Property(x => x.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<ProductIngredient>(e =>
        {
            e.HasKey(x => new { x.ProductId, x.IngredientId });
            e.Property(x => x.QtyPerServing).HasColumnType("decimal(18,3)");
            e.HasOne(x => x.Product)
                .WithMany(p => p.ProductIngredients)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.Ingredient)
                .WithMany(i => i.ProductIngredients)
                .HasForeignKey(x => x.IngredientId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Table>(e =>
        {
            e.ToTable("Tables");
            e.Property(x => x.TableCode).HasMaxLength(20);
            e.Property(x => x.TableName).HasMaxLength(50);
            e.Property(x => x.Area).HasMaxLength(50);
            e.Property(x => x.QrcodePath).HasColumnName("QRCodePath").HasMaxLength(255);
            e.Property(x => x.Status).HasMaxLength(20);
        });

        modelBuilder.Entity<TableSession>(e =>
        {
            e.HasOne(x => x.Table)
                .WithMany(t => t.TableSessions)
                .HasForeignKey(x => x.TableId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Order>(e =>
        {
            e.Property(x => x.OrderCode).HasMaxLength(20);
            e.Property(x => x.TotalAmount).HasColumnType("decimal(18,0)");
            e.Property(x => x.Status).HasMaxLength(20);
            e.Property(x => x.PaymentMethod).HasMaxLength(20);
            e.Property(x => x.PaymentStatus).HasMaxLength(20);
            e.Property(x => x.CustomerEmail).HasMaxLength(256);
            e.Property(x => x.InternalNote).HasMaxLength(500);
            e.HasOne(x => x.Table)
                .WithMany(t => t.Orders)
                .HasForeignKey(x => x.TableId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.TableSession)
                .WithMany(s => s.Orders)
                .HasForeignKey(x => x.TableSessionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderDetail>(e =>
        {
            e.ToTable("OrderDetails");
            e.Property(x => x.UnitPrice).HasColumnType("decimal(18,0)");
            e.Property(x => x.SelectedOptions).HasMaxLength(500);
            e.Property(x => x.Note).HasMaxLength(300);
            e.Property(x => x.ItemStatus).HasMaxLength(20);
            e.Property(x => x.InternalNote).HasMaxLength(500);
            e.HasOne(x => x.Order)
                .WithMany(o => o.OrderDetails)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Product)
                .WithMany(p => p.OrderDetails)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderStatusHistory>(e =>
        {
            e.ToTable("OrderStatusHistory");
            e.Property(x => x.OldStatus).HasMaxLength(20);
            e.Property(x => x.NewStatus).HasMaxLength(20);
            e.Property(x => x.ChangedBy).HasMaxLength(450);
            e.Property(x => x.Note).HasMaxLength(300);
            e.HasOne(x => x.OrderDetail)
                .WithMany(d => d.OrderStatusHistories)
                .HasForeignKey(x => x.OrderDetailId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Payment>(e =>
        {
            e.Property(x => x.Amount).HasColumnType("decimal(18,0)");
            e.Property(x => x.PaymentMethod).HasMaxLength(20);
            e.Property(x => x.TransactionRef).HasMaxLength(100);
            e.Property(x => x.Status).HasMaxLength(20);
            e.Property(x => x.ResponseCode).HasMaxLength(10);
            e.HasOne(x => x.Order)
                .WithMany(o => o.Payments)
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Review>(e =>
        {
            e.Property(x => x.CustomerName).HasMaxLength(100);
            e.Property(x => x.Comment).HasMaxLength(500);
            e.HasOne(x => x.Product)
                .WithMany(p => p.Reviews)
                .HasForeignKey(x => x.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.TableSession)
                .WithMany(s => s.Reviews)
                .HasForeignKey(x => x.TableSessionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<KitchenQueueRow>(e =>
        {
            e.HasNoKey();
            e.ToView("vw_KitchenQueue");
        });
        modelBuilder.Entity<ReadyToServeRow>(e =>
        {
            e.HasNoKey();
            e.ToView("vw_ReadyToServe");
        });
        modelBuilder.Entity<RevenueByDayRow>(e =>
        {
            e.HasNoKey();
            e.ToView("vw_RevenueByDay");
            e.Property(x => x.Revenue).HasColumnType("decimal(38,0)");
        });
        modelBuilder.Entity<PeakHourRow>(e =>
        {
            e.HasNoKey();
            e.ToView("vw_PeakHours");
            e.Property(x => x.Revenue).HasColumnType("decimal(38,0)");
        });
        modelBuilder.Entity<TopProductRow>(e =>
        {
            e.HasNoKey();
            e.ToView("vw_TopProducts");
            e.Property(x => x.TotalRevenue).HasColumnType("decimal(38,0)");
        });
        modelBuilder.Entity<TableUsageRow>(e =>
        {
            e.HasNoKey();
            e.ToView("vw_TableUsage");
            e.Property(x => x.Revenue).HasColumnType("decimal(38,0)");
        });
        modelBuilder.Entity<LowStockRow>(e =>
        {
            e.HasNoKey();
            e.ToView("vw_LowStock");
            e.Property(x => x.CurrentQty).HasColumnType("decimal(18,3)");
            e.Property(x => x.ThresholdQty).HasColumnType("decimal(18,3)");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
