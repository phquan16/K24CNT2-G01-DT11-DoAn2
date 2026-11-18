using System.ComponentModel.DataAnnotations;

namespace FoodOrder.ViewModels
{
    public class ProductViewModel
    {
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên món")]
        [StringLength(150)]
        [Display(Name = "Tên món")]
        public string Name { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng chọn danh mục")]
        [Display(Name = "Danh mục")]
        public int CategoryId { get; set; }

        [Range(1000, 10000000, ErrorMessage = "Giá từ 1.000đ đến 10.000.000đ")]
        [Display(Name = "Giá bán")]
        public decimal Price { get; set; }

        // null = không khuyến mãi
        [Range(1000, 10000000, ErrorMessage = "Giá khuyến mãi từ 1.000đ đến 10.000.000đ")]
        [Display(Name = "Giá khuyến mãi")]
        public decimal? SalePrice { get; set; }

        [StringLength(1000)]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [StringLength(500)]
        [Display(Name = "Thành phần")]
        public string? Composition { get; set; }

        [Range(0, 3, ErrorMessage = "Độ cay từ 0 đến 3")]
        [Display(Name = "Độ cay (0-3)")]
        public byte SpiceLevel { get; set; }

        [Range(0, 5000, ErrorMessage = "Calo từ 0 đến 5.000")]
        [Display(Name = "Calo")]
        public int? Calories { get; set; }

        // null = không theo dõi tồn kho
        [Range(0, 100000)]
        [Display(Name = "Tồn kho")]
        public int? Stock { get; set; }

        [Display(Name = "Còn bán")]
        public bool IsAvailable { get; set; } = true;

        [Display(Name = "Đang hiển thị")]
        public bool IsActive { get; set; } = true;

        // chống ghi đè
        public string? RowVersionBase64 { get; set; }
    }

    public class ProductListViewModel
    {
        public List<ProductRowViewModel> Items { get; set; } = new();
        public int? CategoryId { get; set; }
        public string? Keyword { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 15;
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);
        public List<CategoryViewModel> Categories { get; set; } = new();
    }

    public class ProductRowViewModel
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public decimal Price { get; set; }
        public decimal? SalePrice { get; set; }
        public int? Stock { get; set; }
        public bool IsAvailable { get; set; }
        public bool IsActive { get; set; }
        public string? MainImage { get; set; }
    }
}
