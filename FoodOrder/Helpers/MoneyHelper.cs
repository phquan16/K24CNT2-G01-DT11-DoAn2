using System.Globalization;

namespace FoodOrder.Helpers;

public static class MoneyHelper
{
    public static string ToVnd(this decimal value) =>
        value.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + " đ";
}