using System.ComponentModel.DataAnnotations;

namespace FoodOrder.ViewModels;

public class CategoryViewModel
{
    public int CategoryId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên danh mục")]
    [StringLength(100, ErrorMessage = "Tối đa 100 ký tự")]
    [Display(Name = "Tên danh mục")]
    public string CategoryName { get; set; } = string.Empty;

    [Display(Name = "Thứ tự hiển thị")]
    public int SortOrder { get; set; }

    [Display(Name = "Đang hiển thị")]
    public bool IsActive { get; set; } = true;
}