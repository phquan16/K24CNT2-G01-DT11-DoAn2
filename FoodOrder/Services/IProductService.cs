using FoodOrder.ViewModels;

namespace FoodOrder.Services
{
    public interface IProductService
    {
        Task<ProductListViewModel> GetListAsync(int? categoryId, string? keyword, int page, int pageSize);
        Task<ProductViewModel?> GetByIdAsync(int id);
        Task<int> CreateAsync(ProductViewModel vm);
        // Trả về false nếu bị người khác sửa trước (xung đột RowVersion)
        Task<bool> UpdateAsync(ProductViewModel vm);
        Task SetActiveAsync(int id, bool active);
    }
}