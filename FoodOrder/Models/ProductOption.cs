using System;
using System.Collections.Generic;

namespace FoodOrder.Models;

public partial class ProductOption
{
    public int ProductOptionId { get; set; }

    public int ProductId { get; set; }

    public string OptionName { get; set; } = null!;

    public string OptionValue { get; set; } = null!;

    public decimal ExtraPrice { get; set; }

    public virtual Product Product { get; set; } = null!;
}
