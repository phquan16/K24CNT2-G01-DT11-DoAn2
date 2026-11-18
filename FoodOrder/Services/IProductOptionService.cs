namespace FoodOrder.Services;

public record OptionDto(int ProductOptionId, string OptionName, string OptionValue, decimal ExtraPrice);

public interface IProductOptionService
{
    Task<List<OptionDto>> GetAsync(int productId);
    Task<(bool ok, string? error, OptionDto? item)> AddAsync(int productId, string name, string value, decimal extra);
    Task<bool> DeleteAsync(int optionId);
}
