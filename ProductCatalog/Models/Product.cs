namespace ProductCatalog.Models;

/// <summary>
/// Represents a product in the catalog
/// </summary>
public class Product
{
    public string Name { get; set; }
    public int Quantity { get; set; }

    public Product(string name, int quantity)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name cannot be null or empty.", nameof(name));
        
        if (quantity < 0)
            throw new ArgumentException("Product quantity cannot be negative.", nameof(quantity));

        Name = name.Trim();
        Quantity = quantity;
    }

    public override string ToString()
    {
        return $"{Name}: {Quantity} units";
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