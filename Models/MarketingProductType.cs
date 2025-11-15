namespace PathfinderPhotography.Models;

public class MarketingProductType
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconClass { get; set; } = string.Empty;
}

public static class MarketingProducts
{
    public static readonly List<MarketingProductType> AvailableProducts =
    [
        new()
        {
            Name = "Framed Print",
            Description = "8x10 print with black frame on wall",
            IconClass = "bi-picture"
        },

        new()
        {
            Name = "Coffee Mug",
            Description = "11oz ceramic mug with photo wrap",
            IconClass = "bi-cup-hot"
        },

        new()
        {
            Name = "T-Shirt",
            Description = "Cotton t-shirt with photo print",
            IconClass = "bi-person"
        },

        new()
        {
            Name = "Greeting Card",
            Description = "5x7 folded greeting card",
            IconClass = "bi-envelope-heart"
        },

        new()
        {
            Name = "Phone Case",
            Description = "Hard case with photo print",
            IconClass = "bi-phone"
        },

        new()
        {
            Name = "Canvas Print",
            Description = "Gallery-wrapped canvas",
            IconClass = "bi-image"
        }
    ];
}
