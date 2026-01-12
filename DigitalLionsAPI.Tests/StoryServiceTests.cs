using DigitalLionsAPI.Models;
using DigitalLionsAPI.Services;
using Xunit;

namespace DigitalLionsAPI.Tests;

public class StoryServiceTests : IDisposable
{
    private readonly string _testFilePath;
    private readonly StoryService _service;

    public StoryServiceTests()
    {
        _testFilePath = Path.Combine(Path.GetTempPath(), $"test_stories_{Guid.NewGuid()}.json");
        _service = new StoryService(_testFilePath);
    }

    public void Dispose()
    {
        if (File.Exists(_testFilePath))
        {
            File.Delete(_testFilePath);
        }
    }

    [Fact]
    public async Task IncrementLikesAsync_ValidId_IncrementsLikeCount()
    {
        // Arrange
        var createRequest = new CreateStoryRequest
        {
            Title = "Test Story",
            Category = "Test",
            Summary = "Test summary",
            Description = "Test description",
            ImageUrl = "https://example.com/image.jpg",
            IsFeatured = false
        };
        var createdStory = await _service.CreateStoryAsync(createRequest);
        var initialLikes = createdStory.Likes;

        // Act
        var updatedStory = await _service.IncrementLikesAsync(createdStory.Id);

        // Assert
        Assert.NotNull(updatedStory);
        Assert.Equal(initialLikes + 1, updatedStory.Likes);

        // Verify persistence
        var fetchedStory = await _service.GetStoryByIdAsync(createdStory.Id);
        Assert.NotNull(fetchedStory);
        Assert.Equal(initialLikes + 1, fetchedStory.Likes);
    }

    [Fact]
    public async Task IncrementLikesAsync_InvalidId_ReturnsNull()
    {
        // Act
        var result = await _service.IncrementLikesAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateStoryAsync_ValidRequest_CreatesStoryWithCorrectId()
    {
        // Arrange
        var request = new CreateStoryRequest
        {
            Title = "New Story",
            Category = "Technology",
            Summary = "A new story",
            Description = "Full description here",
            ImageUrl = "https://example.com/new.jpg",
            IsFeatured = true
        };

        // Act
        var story = await _service.CreateStoryAsync(request);

        // Assert
        Assert.NotNull(story);
        Assert.Equal(1, story.Id);
        Assert.Equal(request.Title, story.Title);
        Assert.Equal(request.Category, story.Category);
        Assert.Equal(0, story.Likes);
    }

    [Fact]
    public async Task GetStoryByIdAsync_ExistingId_ReturnsStory()
    {
        // Arrange
        var createRequest = new CreateStoryRequest
        {
            Title = "Find Me",
            Category = "Test",
            Summary = "Summary",
            Description = "Description",
            ImageUrl = "https://example.com/findme.jpg",
            IsFeatured = false
        };
        var created = await _service.CreateStoryAsync(createRequest);

        // Act
        var found = await _service.GetStoryByIdAsync(created.Id);

        // Assert
        Assert.NotNull(found);
        Assert.Equal(created.Id, found.Id);
        Assert.Equal(created.Title, found.Title);
    }

    [Fact]
    public async Task GetStoryByIdAsync_NonExistingId_ReturnsNull()
    {
        // Act
        var result = await _service.GetStoryByIdAsync(999);

        // Assert
        Assert.Null(result);
    }
}