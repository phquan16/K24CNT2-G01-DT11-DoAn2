using FoodOrder.ViewModels;

namespace FoodOrder.Services;

public interface IProductImportService
{
    Task<ImportPreviewViewModel> PreviewAsync(Stream csv);
    Task<int> CommitAsync(List<ImportRow> rows); // trả về số món đã thêm
}
