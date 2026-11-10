using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace FoodOrder.Helpers
{
    public static class SlugHelper
    {
        public static string ToSlug(string text)
        {
            var normalized = text.Trim().ToLowerInvariant().Replace('đ', 'd').Replace('Đ', 'd')
                .Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            return Regex.Replace(sb.ToString(), "[^a-z0-9]+", "-").Trim('-');
        }
    }
}
