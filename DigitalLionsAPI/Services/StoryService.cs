using System.Text.Json;
using DigitalLionsAPI.Models;
using Microsoft.Extensions.Logging;

namespace DigitalLionsAPI.Services;

/// <summary>
/// Service for managing impact stories with JSON file persistence.
/// Thread-safe operations using SemaphoreSlim for file access coordination.
/// NOTE: This implementation is suitable for low-concurrency scenarios.
/// For high-traffic production use, consider a proper database solution.
/// </summary>
public class StoryService : IStoryService
{
    private readonly string _dataFilePath;
    private readonly SemaphoreSlim _fileLock = new(1, 1);
    private readonly ILogger<StoryService>? _logger;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public StoryService(string dataFilePath, ILogger<StoryService>? logger = null)
    {
        _dataFilePath = dataFilePath ?? throw new ArgumentNullException(nameof(dataFilePath));
        _logger = logger;
        EnsureDataFileExists();
    }

    private void EnsureDataFileExists()
    {
        try
        {
            var directory = Path.GetDirectoryName(_dataFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (!File.Exists(_dataFilePath))
            {
                var initialData = new StoriesData { ImpactStories = new List<ImpactStory>() };
                var json = JsonSerializer.Serialize(initialData, _jsonOptions);
                File.WriteAllText(_dataFilePath, json);
                _logger?.LogInformation("Created new data file at {FilePath}", _dataFilePath);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to ensure data file exists at {FilePath}", _dataFilePath);
            throw new IOException($"Failed to initialize data file: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Executes a read-modify-write operation atomically with file locking.
    /// Prevents race conditions during concurrent modifications.
    /// </summary>
    private async Task<T> ExecuteWithLockAsync<T>(Func<StoriesData, Task<(StoriesData data, T result)>> operation)
    {
        await _fileLock.WaitAsync();
        try
        {
            // Read
            var json = await File.ReadAllTextAsync(_dataFilePath);
            var data = JsonSerializer.Deserialize<StoriesData>(json);
            
            if (data == null || data.ImpactStories == null)
            {
                _logger?.LogWarning("Corrupted data file, initializing with empty data");
                data = new StoriesData { ImpactStories = new List<ImpactStory>() };
            }

            // Modify & get result
            var (updatedData, result) = await operation(data);

            // Write
            var updatedJson = JsonSerializer.Serialize(updatedData, _jsonOptions);
            await File.WriteAllTextAsync(_dataFilePath, updatedJson);

            return result;
        }
        catch (JsonException ex)
        {
            _logger?.LogError(ex, "JSON deserialization error in data file");
            throw new InvalidOperationException("Data file is corrupted", ex);
        }
        catch (IOException ex)
        {
            _logger?.LogError(ex, "File I/O error accessing {FilePath}", _dataFilePath);
            throw new InvalidOperationException("Failed to access data file", ex);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    /// <summary>
    /// Executes a read-only operation with file locking.
    /// </summary>
    private async Task<T> ExecuteReadOnlyAsync<T>(Func<StoriesData, T> operation)
    {
        await _fileLock.WaitAsync();
        try
        {
            var json = await File.ReadAllTextAsync(_dataFilePath);
            var data = JsonSerializer.Deserialize<StoriesData>(json);
            
            if (data == null || data.ImpactStories == null)
            {
                _logger?.LogWarning("Corrupted data file during read");
                data = new StoriesData { ImpactStories = new List<ImpactStory>() };
            }

            return operation(data);
        }
        catch (JsonException ex)
        {
            _logger?.LogError(ex, "JSON deserialization error in data file");
            throw new InvalidOperationException("Data file is corrupted", ex);
        }
        catch (IOException ex)
        {
            _logger?.LogError(ex, "File I/O error accessing {FilePath}", _dataFilePath);
            throw new InvalidOperationException("Failed to access data file", ex);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<List<ImpactStory>> GetAllStoriesAsync()
    {
        return await ExecuteReadOnlyAsync(data => data.ImpactStories.ToList());
    }

    public async Task<ImpactStory?> GetStoryByIdAsync(int id)
    {
        return await ExecuteReadOnlyAsync(data => 
            data.ImpactStories.FirstOrDefault(s => s.Id == id));
    }

    public async Task<ImpactStory?> IncrementLikesAsync(int id)
    {
        return await ExecuteWithLockAsync<ImpactStory?>(async data =>
        {
            var story = data.ImpactStories.FirstOrDefault(s => s.Id == id);
            
            if (story == null)
            {
                return (data, null);
            }

            story.Likes++;
            _logger?.LogInformation("Incremented likes for story {StoryId} to {Likes}", id, story.Likes);
            
            return await Task.FromResult((data, story));
        });
    }

    public async Task<ImpactStory> CreateStoryAsync(CreateStoryRequest request)
    {
        return await ExecuteWithLockAsync(async data =>
        {
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
            _logger?.LogInformation("Created new story with ID {StoryId}", newId);
            
            return await Task.FromResult((data, newStory));
        });
    }
}