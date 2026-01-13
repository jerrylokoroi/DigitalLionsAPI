using System.ComponentModel.DataAnnotations;

namespace DigitalLionsAPI.Models;

/// <summary>
/// Domain model representing an impact story
/// </summary>
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

/// <summary>
/// DTO for creating a new story
/// </summary>
public class CreateStoryRequest
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Category is required")]
    [StringLength(50, ErrorMessage = "Category cannot exceed 50 characters")]
    public string Category { get; set; } = string.Empty;

    [Required(ErrorMessage = "Summary is required")]
    [StringLength(500, ErrorMessage = "Summary cannot exceed 500 characters")]
    public string Summary { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required")]
    [StringLength(5000, ErrorMessage = "Description cannot exceed 5000 characters")]
    public string Description { get; set; } = string.Empty;

    [Url(ErrorMessage = "ImageUrl must be a valid URL")]
    public string ImageUrl { get; set; } = string.Empty;

    public bool IsFeatured { get; set; }
}

/// <summary>
/// DTO for API responses
/// </summary>
public class StoryResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsFeatured { get; set; }
    public int Likes { get; set; }

    public static StoryResponse FromDomain(ImpactStory story) => new()
    {
        Id = story.Id,
        Title = story.Title,
        Category = story.Category,
        Summary = story.Summary,
        Description = story.Description,
        ImageUrl = story.ImageUrl,
        IsFeatured = story.IsFeatured,
        Likes = story.Likes
    };
}

/// <summary>
/// Standard error response DTO
/// </summary>
public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string? Details { get; set; }
}

/// <summary>
/// Root data structure for JSON file persistence
/// </summary>
public class StoriesData
{
    public List<ImpactStory> ImpactStories { get; set; } = new();
}