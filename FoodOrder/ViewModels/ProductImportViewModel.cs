namespace FoodOrder.ViewModels;

public class ImportRow
{
    public int Line { get; set; }                 // số dòng trong file (tính cả tiêu đề)
    public string ProductName { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public decimal Price { get; set; }
    public string? Description { get; set; }
    public int? Stock { get; set; }
    public int CategoryId { get; set; }
    public string Status { get; set; } = "OK";    // OK | Trùng | Lỗi
    public string? Message { get; set; }
}

public class ImportPreviewViewModel
{
    public List<ImportRow> Rows { get; set; } = new();
    public int OkCount => Rows.Count(r => r.Status == "OK");
    public int DuplicateCount => Rows.Count(r => r.Status == "Trùng");
    public int ErrorCount => Rows.Count(r => r.Status == "Lỗi");
    public string? FileError { get; set; }
}