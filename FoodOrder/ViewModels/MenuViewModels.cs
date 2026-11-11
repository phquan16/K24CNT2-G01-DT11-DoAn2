namespace FoodOrder.ViewModels;

public class CategoryChipViewModel
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
}

public class MenuItemViewModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? SalePrice { get; set; }
    public byte SpiceLevel { get; set; }
    public bool IsSoldOut { get; set; }
    public string? ImageUrl { get; set; }
}

public class MenuPageViewModel
{
    public string TableCode { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public string? Keyword { get; set; }
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; }
    public List<CategoryChipViewModel> Categories { get; set; } = new();
    public List<MenuItemViewModel> Items { get; set; } = new();
}

public class OptionChoiceViewModel
{
    public int ProductOptionId { get; set; }
    public string Value { get; set; } = string.Empty;
    public decimal ExtraPrice { get; set; }
}

public class OptionGroupViewModel
{
    public string Name { get; set; } = string.Empty;
    public List<OptionChoiceViewModel> Choices { get; set; } = new();
}

public class ProductDetailViewModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Composition { get; set; }
    public decimal Price { get; set; }
    public decimal? SalePrice { get; set; }
    public byte SpiceLevel { get; set; }
    public int? Calories { get; set; }
    public bool IsSoldOut { get; set; }
    public string? ImageUrl { get; set; }
    public int ReviewCount { get; set; }
    public double? AverageRating { get; set; }
    public List<OptionGroupViewModel> Groups { get; set; } = new();
    public string TableCode { get; set; } = string.Empty;
}