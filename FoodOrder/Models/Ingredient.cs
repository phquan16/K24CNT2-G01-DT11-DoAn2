using System;
using System.Collections.Generic;

namespace FoodOrder.Models;

public partial class Ingredient
{
    public int IngredientId { get; set; }

    public string IngredientName { get; set; } = null!;

    public string Unit { get; set; } = null!;

    public decimal StockQty { get; set; }

    public decimal MinQty { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    public virtual ICollection<ProductIngredient> ProductIngredients { get; set; } = new List<ProductIngredient>();
}
