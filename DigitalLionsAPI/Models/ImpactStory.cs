namespace DigitalLionsAPI.Models;

public class ImpactStory
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsFeatured { get; set; }
    public int Likes { get; set; }
}

public class CreateStoryRequest
{
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsFeatured { get; set; }
}

public class StoriesData
{
    public List<ImpactStory> ImpactStories { get; set; } = new();
}