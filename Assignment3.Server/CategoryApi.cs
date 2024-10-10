namespace Assignment3.Server;

record Category(int Cid, string Name);

static class CategoryApi
{
    private static List<Category> _data = new() {
        new(1, "Beverages"),
        new(2, "Condiments"),
        new(3, "Confections")
    };

    public static Category? GetCategory(int cid)
        => _data.FirstOrDefault(c => c.Cid == cid);

    public static List<Category> GetCategories()
        => _data
            .OrderBy(c => c.Cid)
            .ToList();
    
    public static Category CreateCategory(Category category)
    {
        Category newC = category with { Cid = _data.Max(c => c.Cid) + 1 };
        _data.Add(newC);
        return newC;
    }

    public static bool UpdateCategory(int cid, Category category)
    {
        Category? c = GetCategory(cid);
        if (c == null)
            return false;

        Category newC = c with { Name = category.Name };
        _data.Remove(c);
        _data.Add(newC);
        return true;
    }

    public static bool DeleteCategory(int cid)
    {
        Category? c = GetCategory(cid);
        if (c == null)
            return false;
        _data.Remove(c);
        return true;
    }
}
