using System.ComponentModel.DataAnnotations;

namespace FoodOrder.ViewModels;

public class IngredientViewModel
{
    public int IngredientId { get; set; }

    [Required(ErrorMessage = "Nhập tên nguyên liệu"), StringLength(100)]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Nhập đơn vị (kg, l, cái...)"), StringLength(20)]
    public string Unit { get; set; } = "";

    [Range(0, 1000000)] public decimal StockQty { get; set; }
    [Range(0, 1000000)] public decimal MinQty { get; set; }
    public string? RowVersionBase64 { get; set; }
    public bool IsLow => StockQty < MinQty;
}

public class RecipeLineViewModel
{
    public int IngredientId { get; set; }
    public string IngredientName { get; set; } = "";
    public string Unit { get; set; } = "";
    public decimal QtyPerServing { get; set; }
}