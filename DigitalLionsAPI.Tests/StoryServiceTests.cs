using DigitalLionsAPI.Models;
using DigitalLionsAPI.Services;
using Xunit;

namespace DigitalLionsAPI.Tests;

/// <summary>
/// Comprehensive test suite for StoryService with proper isolation and edge case coverage
/// </summary>
public class StoryServiceTests : IDisposable
{
    private readonly string _testFilePath;
    private readonly StoryService _service;

    public StoryServiceTests()
    {
        // Each test gets its own isolated temporary file
        _testFilePath = Path.Combine(Path.GetTempPath(), $"test_stories_{Guid.NewGuid()}.json");
        _service = new StoryService(_testFilePath);
    }

    public void Dispose()
    {
        // Clean up after each test
        if (File.Exists(_testFilePath))
        {
            File.Delete(_testFilePath);
        }
    }

    #region GetAllStoriesAsync Tests

    [Fact]
    public async Task GetAllStoriesAsync_EmptyFile_ReturnsEmptyList()
    {
        // Act
        var stories = await _service.GetAllStoriesAsync();

        // Assert
        Assert.NotNull(stories);
        Assert.Empty(stories);
    }

    [Fact]
    public async Task GetAllStoriesAsync_WithMultipleStories_ReturnsAllStories()
    {
        // Arrange
        var story1 = await CreateTestStory("Story 1", "Category A");
        var story2 = await CreateTestStory("Story 2", "Category B");
        var story3 = await CreateTestStory("Story 3", "Category C");

        // Act
        var stories = await _service.GetAllStoriesAsync();

        // Assert
        Assert.Equal(3, stories.Count);
        Assert.Contains(stories, s => s.Id == story1.Id);
        Assert.Contains(stories, s => s.Id == story2.Id);
        Assert.Contains(stories, s => s.Id == story3.Id);
    }

    #endregion

    #region GetStoryByIdAsync Tests

    [Fact]
    public async Task GetStoryByIdAsync_ExistingId_ReturnsStory()
    {
        // Arrange
        var created = await CreateTestStory("Find Me", "Test");

        // Act
        var found = await _service.GetStoryByIdAsync(created.Id);

        // Assert
        Assert.NotNull(found);
        Assert.Equal(created.Id, found.Id);
        Assert.Equal(created.Title, found.Title);
        Assert.Equal(created.Category, found.Category);
    }

    [Fact]
    public async Task GetStoryByIdAsync_NonExistingId_ReturnsNull()
    {
        // Act
        var result = await _service.GetStoryByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region IncrementLikesAsync Tests

    [Fact]
    public async Task IncrementLikesAsync_ValidId_IncrementsLikeCount()
    {
        // Arrange
        var created = await CreateTestStory("Likeable Story", "Test");
        var initialLikes = created.Likes;

        // Act
        var updated = await _service.IncrementLikesAsync(created.Id);

        // Assert
        Assert.NotNull(updated);
        Assert.Equal(initialLikes + 1, updated.Likes);

        // Verify persistence
        var fetched = await _service.GetStoryByIdAsync(created.Id);
        Assert.NotNull(fetched);
        Assert.Equal(initialLikes + 1, fetched.Likes);
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
    public async Task IncrementLikesAsync_MultipleCalls_IncrementsCorrectly()
    {
        // Arrange
        var created = await CreateTestStory("Popular Story", "Test");

        // Act - Like 5 times
        for (int i = 0; i < 5; i++)
        {
            await _service.IncrementLikesAsync(created.Id);
        }

        // Assert
        var final = await _service.GetStoryByIdAsync(created.Id);
        Assert.NotNull(final);
        Assert.Equal(5, final.Likes);
    }

    [Fact]
    public async Task IncrementLikesAsync_ConcurrentCalls_HandlesCorrectly()
    {
        // Arrange
        var created = await CreateTestStory("Concurrent Test", "Test");
        const int concurrentLikes = 10;

        // Act - Simulate concurrent likes
        var tasks = Enumerable.Range(0, concurrentLikes)
            .Select(_ => _service.IncrementLikesAsync(created.Id))
            .ToArray();

        await Task.WhenAll(tasks);

        // Assert - All tasks should succeed
        Assert.All(tasks, t => Assert.NotNull(t.Result));

        // Verify final count
        var final = await _service.GetStoryByIdAsync(created.Id);
        Assert.NotNull(final);
        Assert.Equal(concurrentLikes, final.Likes);
    }

    #endregion

    #region CreateStoryAsync Tests

    [Fact]
    public async Task CreateStoryAsync_ValidRequest_CreatesStoryWithId1()
    {
        // Arrange
        var request = new CreateStoryRequest
        {
            Title = "First Story",
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
        Assert.Equal(request.Summary, story.Summary);
        Assert.Equal(request.Description, story.Description);
        Assert.Equal(request.ImageUrl, story.ImageUrl);
        Assert.Equal(request.IsFeatured, story.IsFeatured);
        Assert.Equal(0, story.Likes);
    }

    [Fact]
    public async Task CreateStoryAsync_MultipleStories_AssignsSequentialIds()
    {
        // Arrange & Act
        var story1 = await CreateTestStory("Story 1", "Cat A");
        var story2 = await CreateTestStory("Story 2", "Cat B");
        var story3 = await CreateTestStory("Story 3", "Cat C");

        // Assert
        Assert.Equal(1, story1.Id);
        Assert.Equal(2, story2.Id);
        Assert.Equal(3, story3.Id);
    }

    [Fact]
    public async Task CreateStoryAsync_AfterMultipleCreates_PersistsAllStories()
    {
        // Arrange & Act
        await CreateTestStory("Story 1", "Cat A");
        await CreateTestStory("Story 2", "Cat B");
        await CreateTestStory("Story 3", "Cat C");

        // Assert
        var allStories = await _service.GetAllStoriesAsync();
        Assert.Equal(3, allStories.Count);
        Assert.All(allStories, story => Assert.True(story.Id > 0));
    }

    #endregion

    #region Edge Cases and Error Handling

    [Fact]
    public async Task Service_WithEmptyTitle_StillCreatesStory()
    {
        // Note: Business validation should happen in the API layer, not service layer
        // Arrange
        var request = new CreateStoryRequest
        {
            Title = "",
            Category = "Test",
            Summary = "Summary",
            Description = "Description"
        };

        // Act
        var story = await _service.CreateStoryAsync(request);

        // Assert
        Assert.NotNull(story);
        Assert.Equal("", story.Title);
    }

    [Fact]
    public void Constructor_InvalidPath_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new StoryService(null!));
    }

    #endregion

    #region Helper Methods

    private async Task<ImpactStory> CreateTestStory(string title, string category)
    {
        var request = new CreateStoryRequest
        {
            Title = title,
            Category = category,
            Summary = $"Summary for {title}",
            Description = $"Description for {title}",
            ImageUrl = "https://example.com/image.jpg",
            IsFeatured = false
        };

        return await _service.CreateStoryAsync(request);
    }

    #endregion
}