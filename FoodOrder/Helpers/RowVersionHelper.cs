namespace FoodOrder.Helpers;

public static class RowVersionHelper
{
    public static bool TryParse(string? base64, out byte[] value)
    {
        value = Array.Empty<byte>();
        if (string.IsNullOrWhiteSpace(base64)) return false;
        try
        {
            value = Convert.FromBase64String(base64);
            return value.Length > 0;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
