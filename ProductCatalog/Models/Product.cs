namespace ProductCatalog.Models;

/// <summary>
/// Represents a product in the catalog
/// </summary>
public class Product
{
    public string Name { get; set; }
    public int Quantity { get; set; }
    public string Category { get; set; }
    public ISet<string> Tags { get; set; }

    public Product(string name, int quantity, string category = "General", IEnumerable<string>? tags = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name cannot be null or empty.", nameof(name));
        
        if (quantity < 0)
            throw new ArgumentException("Product quantity cannot be negative.", nameof(quantity));

        Name = name.Trim();
        Quantity = quantity;
        Category = string.IsNullOrWhiteSpace(category) ? "General" : category.Trim();
        Tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        if (tags != null)
        {
            foreach (var tag in tags.Where(t => !string.IsNullOrWhiteSpace(t)))
            {
                Tags.Add(tag.Trim());
            }
        }
    }

    public override string ToString()
    {
        var tagsString = Tags.Any() ? $" [Tags: {string.Join(", ", Tags)}]" : "";
        return $"{Name}: {Quantity} units (Category: {Category}){tagsString}";
    }

    public override bool Equals(object? obj)
    {
        if (obj is Product other)
        {
            return string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    public override int GetHashCode()
    {
        return Name.ToLowerInvariant().GetHashCode();
    }
} 