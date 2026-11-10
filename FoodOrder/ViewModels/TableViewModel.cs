using System.ComponentModel.DataAnnotations;

namespace FoodOrder.ViewModels
{
    public class TableViewModel
    {
        public int TableId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mã bàn")]
        [RegularExpression(@"^[A-Za-z0-9_-]{1,20}$", ErrorMessage = "Chỉ gồm chữ, số, dấu _ và - (tối đa 20 ký tự)")]
        [Display(Name = "Mã bàn (nằm trong QR)")]
        public string TableCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập tên bàn")]
        [StringLength(50)]
        [Display(Name = "Tên bàn")]
        public string TableName { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Khu vực")]
        public string? Area { get; set; }

        [Display(Name = "Đang sử dụng")]
        public bool IsActive { get; set; } = true;
    }
}
