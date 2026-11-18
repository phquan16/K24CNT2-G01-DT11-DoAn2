using FoodOrder.ViewModels;

namespace FoodOrder.Services;

public interface IIngredientService
{
    Task<List<IngredientViewModel>> GetAllAsync(bool onlyLow = false);
    Task<IngredientViewModel?> GetByIdAsync(int id);
    Task<(bool ok, string? error)> SaveAsync(IngredientViewModel vm);
    Task<(bool ok, string? error)> AddStockAsync(int id, decimal qty, string rowVersionBase64);
    Task<List<RecipeLineViewModel>> GetRecipeAsync(int productId);
    Task<(bool ok, string? error)> SetRecipeLineAsync(int productId, int ingredientId, decimal qty);
    Task RemoveRecipeLineAsync(int productId, int ingredientId);
}
