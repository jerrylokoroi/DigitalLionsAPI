using System.Text.Json;
using DigitalLionsAPI.Models;

namespace DigitalLionsAPI.Services;

public class StoryService : IStoryService
{
    private readonly string _dataFilePath;
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    public StoryService(string dataFilePath)
    {
        _dataFilePath = dataFilePath;
        EnsureDataFileExists();
    }

    private void EnsureDataFileExists()
    {
        if (!File.Exists(_dataFilePath))
        {
            var initialData = new StoriesData { ImpactStories = new List<ImpactStory>() };
            var json = JsonSerializer.Serialize(initialData, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_dataFilePath, json);
        }
    }

    private async Task<StoriesData> ReadDataAsync()
    {
        await _fileLock.WaitAsync();
        try
        {
            var json = await File.ReadAllTextAsync(_dataFilePath);
            return JsonSerializer.Deserialize<StoriesData>(json) ?? new StoriesData();
        }
        finally
        {
            _fileLock.Release();
        }
    }

    private async Task WriteDataAsync(StoriesData data)
    {
        await _fileLock.WaitAsync();
        try
        {
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_dataFilePath, json);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<List<ImpactStory>> GetAllStoriesAsync()
    {
        var data = await ReadDataAsync();
        return data.ImpactStories;
    }

    public async Task<ImpactStory?> GetStoryByIdAsync(int id)
    {
        var data = await ReadDataAsync();
        return data.ImpactStories.FirstOrDefault(s => s.Id == id);
    }

    public async Task<ImpactStory?> IncrementLikesAsync(int id)
    {
        var data = await ReadDataAsync();
        var story = data.ImpactStories.FirstOrDefault(s => s.Id == id);

        if (story == null)
            return null;

        story.Likes++;
        await WriteDataAsync(data);
        return story;
    }

    public async Task<ImpactStory> CreateStoryAsync(CreateStoryRequest request)
    {
        var data = await ReadDataAsync();

        var newId = data.ImpactStories.Any()
            ? data.ImpactStories.Max(s => s.Id) + 1
            : 1;

        var newStory = new ImpactStory
        {
            Id = newId,
            Title = request.Title,
            Category = request.Category,
            Summary = request.Summary,
            Description = request.Description,
            ImageUrl = request.ImageUrl,
            IsFeatured = request.IsFeatured,
            Likes = 0
        };

        data.ImpactStories.Add(newStory);
        await WriteDataAsync(data);
        return newStory;
    }
}