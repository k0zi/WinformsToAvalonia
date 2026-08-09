namespace WarehouseApp.Data.Models;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? ParentCategoryId { get; set; }

    public Category? ParentCategory { get; set; }
    public List<Category> Children { get; set; } = [];
    public List<Product> Products { get; set; } = [];
}
