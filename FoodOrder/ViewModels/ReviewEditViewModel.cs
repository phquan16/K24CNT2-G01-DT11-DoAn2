using System.ComponentModel.DataAnnotations;

namespace FoodOrder.ViewModels;

public class ReviewEditViewModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string TableCode { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;

    [Range(1, 5, ErrorMessage = "Chọn số sao từ 1 đến 5")]
    public int Rating { get; set; } = 5;

    [Required(ErrorMessage = "Vui lòng nhập tên")]
    [StringLength(100, ErrorMessage = "Tên tối đa 100 ký tự")]
    [Display(Name = "Tên của bạn")]
    public string CustomerName { get; set; } = "Khách";

    [StringLength(500, ErrorMessage = "Nhận xét tối đa 500 ký tự")]
    [Display(Name = "Nhận xét")]
    public string? Comment { get; set; }
}
