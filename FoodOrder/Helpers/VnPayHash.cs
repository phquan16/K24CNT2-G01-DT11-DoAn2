using System.Security.Cryptography;
using System.Text;

namespace FoodOrder.Helpers;

public static class VnPayHash
{
    public static string HmacSha512(string secret, string data)
    {
        var key = Encoding.UTF8.GetBytes(secret);
        var bytes = Encoding.UTF8.GetBytes(data);
        var hash = HMACSHA512.HashData(key, bytes);
        return Convert.ToHexString(hash);
    }
}
