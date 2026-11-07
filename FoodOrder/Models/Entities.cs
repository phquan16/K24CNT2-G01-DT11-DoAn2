using System.ComponentModel.DataAnnotations;
using static FoodOrder.Models.StatusConstants;

namespace FoodOrder.Models
{
    public class Entities
    {
        // ─────────────────────────── CATEGORY ───────────────────────────
        public class Category
        {
            public int CategoryId { get; set; }
            public string CategoryName { get; set; } = "";
            public string Slug { get; set; } = "";
            public int SortOrder { get; set; }
            public bool IsActive { get; set; } = true;

            // Navigation
            public ICollection<Product> Products { get; set; } = new List<Product>();
        }

        // ─────────────────────────── PRODUCT ───────────────────────────
        public class Product
        {
            public int ProductId { get; set; }
            public int CategoryId { get; set; }
            public string ProductName { get; set; } = "";
            public decimal Price { get; set; }
            public decimal? SalePrice { get; set; }
            public string? Description { get; set; }
            public string? Composition { get; set; }   // Thành phần
            public byte SpiceLevel { get; set; }       // Độ cay (0–5)
            public int? Calories { get; set; }
            public int? Stock { get; set; }            // null = không giới hạn suất
            public int LowStockThreshold { get; set; } = 5;
            public bool IsAvailable { get; set; } = true; // Còn / hết món
            public bool IsActive { get; set; } = true;    // Xóa mềm (BR-03)
            public DateTime CreatedAt { get; set; } = DateTime.Now;

            [Timestamp]
            public byte[] RowVersion { get; set; } = Array.Empty<byte>(); // Concurrency Token (BR-04)

            // Navigation
            public Category? Category { get; set; }
            public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
            public ICollection<ProductOption> Options { get; set; } = new List<ProductOption>();
            public ICollection<ProductIngredient> ProductIngredients { get; set; } = new List<ProductIngredient>();
            public ICollection<Review> Reviews { get; set; } = new List<Review>();
        }

        // ─────────────────────────── PRODUCT IMAGE ───────────────────────────
        public class ProductImage
        {
            public int ProductImageId { get; set; }
            public int ProductId { get; set; }
            public string ImageUrl { get; set; } = "";
            public bool IsMain { get; set; }

            // Navigation
            public Product? Product { get; set; }
        }

        // ─────────────────────────── PRODUCT OPTION ───────────────────────────
        public class ProductOption
        {
            public int ProductOptionId { get; set; }
            public int ProductId { get; set; }
            public string OptionName { get; set; } = "";
            public string OptionValue { get; set; } = "";
            public decimal ExtraPrice { get; set; }

            // Navigation
            public Product? Product { get; set; }
        }

        // ─────────────────────────── INGREDIENT ───────────────────────────
        public class Ingredient
        {
            public int IngredientId { get; set; }
            public string IngredientName { get; set; } = "";
            public string Unit { get; set; } = "";
            public decimal StockQty { get; set; }
            public decimal MinQty { get; set; }

            [Timestamp]
            public byte[] RowVersion { get; set; } = Array.Empty<byte>();

            // Navigation
            public ICollection<ProductIngredient> ProductIngredients { get; set; } = new List<ProductIngredient>();
        }

        // ─────────────────────────── PRODUCT INGREDIENT (N-N) ───────────────────────────
        /// <summary>Khóa kép (ProductId, IngredientId) — khai báo composite key ở AppDbContext.</summary>
        public class ProductIngredient
        {
            public int ProductId { get; set; }
            public int IngredientId { get; set; }
            public decimal QtyPerServing { get; set; }

            // Navigation
            public Product? Product { get; set; }
            public Ingredient? Ingredient { get; set; }
        }

        // ─────────────────────────── DINING TABLE ───────────────────────────
        public class DiningTable
        {
            [Key]
            public int TableId { get; set; }
            public string TableCode { get; set; } = "";
            public string TableName { get; set; } = "";
            public string? Area { get; set; }
            public string? QRCodePath { get; set; }
            public string Status { get; set; } = TableStatuses.Empty;
            public bool IsActive { get; set; } = true;

            // Navigation
            public ICollection<TableSession> TableSessions { get; set; } = new List<TableSession>();
            public ICollection<Order> Orders { get; set; } = new List<Order>();
        }

        // ─────────────────────────── TABLE SESSION ───────────────────────────
        public class TableSession
        {
            public long TableSessionId { get; set; }
            public int TableId { get; set; }
            public Guid SessionToken { get; set; } = Guid.NewGuid();
            public DateTime StartedAt { get; set; } = DateTime.Now;
            public DateTime ExpiredAt { get; set; } = DateTime.Now.AddHours(4);
            public bool IsClosed { get; set; }

            // Navigation
            public DiningTable? DiningTable { get; set; }
            public ICollection<Order> Orders { get; set; } = new List<Order>();
            public ICollection<Review> Reviews { get; set; } = new List<Review>();
        }

        // ─────────────────────────── ORDER ───────────────────────────
        public class Order
        {
            public long OrderId { get; set; }
            public string OrderCode { get; set; } = "";
            public int TableId { get; set; }
            public long TableSessionId { get; set; }
            public string? UserId { get; set; }        // FK → AspNetUsers (nhân viên phục vụ, nullable)
            public decimal TotalAmount { get; set; }
            public string Status { get; set; } = OrderStatuses.New;
            public string PaymentMethod { get; set; } = PaymentMethods.Cash;
            public string PaymentStatus { get; set; } = PaymentStatuses.Unpaid;
            public string? CustomerEmail { get; set; }
            public string? InternalNote { get; set; }
            public DateTime CreatedAt { get; set; } = DateTime.Now;
            public DateTime UpdatedAt { get; set; } = DateTime.Now;
            public DateTime? ExpiredAt { get; set; }  // Hạn thanh toán VNPay (BR-07)

            // Navigation
            public DiningTable? DiningTable { get; set; }
            public TableSession? TableSession { get; set; }
            public ApplicationUser? User { get; set; }
            public ICollection<OrderDetail> Details { get; set; } = new List<OrderDetail>();
            public ICollection<Payment> Payments { get; set; } = new List<Payment>();
            public ICollection<OrderStatusHistory> StatusHistories { get; set; } = new List<OrderStatusHistory>();
        }

        // ─────────────────────────── ORDER DETAIL ───────────────────────────
        public class OrderDetail
        {
            public long OrderDetailId { get; set; }
            public long OrderId { get; set; }
            public int ProductId { get; set; }
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }    // Đơn giá chốt lúc đặt (BR-05)
            public string? SelectedOptions { get; set; }
            public string? Note { get; set; }
            public string ItemStatus { get; set; } = ItemStatuses.New;
            public string? InternalNote { get; set; }

            // Navigation
            public Order? Order { get; set; }
            public Product? Product { get; set; }
            public ICollection<OrderStatusHistory> StatusHistories { get; set; } = new List<OrderStatusHistory>();
        }

        // ─────────────────────────── ORDER STATUS HISTORY ───────────────────────────
        public class OrderStatusHistory
        {
            public long OrderStatusHistoryId { get; set; }
            public long OrderDetailId { get; set; }   // FK → OrderDetail
            public string? OldStatus { get; set; }
            public string NewStatus { get; set; } = "";
            public string? ChangedBy { get; set; }    // UserId nhân viên; null = khách hoặc hệ thống
            public DateTime ChangedAt { get; set; } = DateTime.Now;
            public string? Note { get; set; }

            // Navigation
            public OrderDetail? OrderDetail { get; set; }
        }

        // ─────────────────────────── PAYMENT ───────────────────────────
        public class Payment
        {
            public long PaymentId { get; set; }
            public long OrderId { get; set; }
            public decimal Amount { get; set; }
            public string PaymentMethod { get; set; } = PaymentMethods.Cash;
            public string? TransactionRef { get; set; } // vnp_TxnRef; không lưu thông tin thẻ
            public string Status { get; set; } = TransactionStatuses.Pending;
            public string? ResponseCode { get; set; }
            public DateTime CreatedAt { get; set; } = DateTime.Now;

            // Navigation
            public Order? Order { get; set; }
        }

        // ─────────────────────────── REVIEW ───────────────────────────
        public class Review
        {
            public int ReviewId { get; set; }
            public int ProductId { get; set; }
            public long TableSessionId { get; set; }
            public string CustomerName { get; set; } = "";
            public byte Rating { get; set; }           // 1–5
            public string? Comment { get; set; }
            public DateTime CreatedAt { get; set; } = DateTime.Now;

            // Navigation
            public Product? Product { get; set; }
            public TableSession? TableSession { get; set; }
        }
    }
}
