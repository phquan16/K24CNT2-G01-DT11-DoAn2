using Microsoft.Extensions.Logging.Console;
using System.ComponentModel.DataAnnotations;

namespace FoodOrder.ViewModels
{
    public class ProductViewModel
    {
        public int ProductId { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập tên món")]
        [StringLength(150)]
        public string Name { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng chọn danh mục")]
        public int CategoryId { get; set; }

        [Range(1000, 10000000, ErrorMessage = "Giá từ 1.000đ đến 10.000.000đ")]
        public decimal Price { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        //null = khong theo doi ton kho
        [Range(0, 100000)]
        public int? Stock {  get; set; }

        public bool IsAvailable { get; set; } = true; // còn bán
        public bool IsActive { get; set; } = true; //false = đã xóa mềm

        // chống ghi đè
        public string? RowVersionBase64 { get; set; }
    }

    public class ProductListViewModel
    {
        public List<ProductListViewModel> Items { get; set; } = new();
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
        public int? Stock { get; set; }
        public bool IsAvailable { get; set; }
        public bool IsActive { get; set; }
        public string? MainImage { get; set; }
    }
}
