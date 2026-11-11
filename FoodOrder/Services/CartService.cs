using System.Text.Json;
using FoodOrder.ViewModels;

namespace FoodOrder.Services;

public interface ICartService
{
    IReadOnlyList<CartLine> GetLines();
    int GetQuantity();
    void Add(CartLine line);
    void Update(int index, int quantity);
    void Remove(int index);
    void Clear();
}

public class CartService : ICartService
{
    private const string Key = "fo_cart_json";
    private readonly IHttpContextAccessor _http;

    public CartService(IHttpContextAccessor http) => _http = http;

    public IReadOnlyList<CartLine> GetLines()
    {
        var raw = _http.HttpContext?.Session.GetString(Key);
        if (string.IsNullOrEmpty(raw)) return Array.Empty<CartLine>();
        try
        {
            return JsonSerializer.Deserialize<List<CartLine>>(raw) ?? new List<CartLine>();
        }
        catch (JsonException)
        {
            return Array.Empty<CartLine>();
        }
    }

    public int GetQuantity() => GetLines().Sum(l => l.Quantity);

    public void Add(CartLine line)
    {
        var lines = GetLines().ToList();
        var same = lines.FirstOrDefault(l =>
            l.ProductId == line.ProductId &&
            l.Note == line.Note &&
            l.OptionIds.OrderBy(x => x).SequenceEqual(line.OptionIds.OrderBy(x => x)));

        if (same != null) same.Quantity += line.Quantity;
        else lines.Add(line);

        Save(lines);
    }

    public void Update(int index, int quantity)
    {
        var lines = GetLines().ToList();
        if (index < 0 || index >= lines.Count) return;
        if (quantity <= 0) lines.RemoveAt(index);
        else lines[index].Quantity = quantity;
        Save(lines);
    }

    public void Remove(int index)
    {
        var lines = GetLines().ToList();
        if (index < 0 || index >= lines.Count) return;
        lines.RemoveAt(index);
        Save(lines);
    }

    public void Clear() => _http.HttpContext?.Session.Remove(Key);

    private void Save(List<CartLine> lines)
    {
        _http.HttpContext?.Session.SetString(Key, JsonSerializer.Serialize(lines));
    }
}
