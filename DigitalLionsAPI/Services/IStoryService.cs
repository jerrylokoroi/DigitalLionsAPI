using DigitalLionsAPI.Models;

namespace DigitalLionsAPI.Services;

public interface IStoryService
{
    Task<List<ImpactStory>> GetAllStoriesAsync();
    Task<ImpactStory?> GetStoryByIdAsync(int id);
    Task<ImpactStory?> IncrementLikesAsync(int id);
    Task<ImpactStory> CreateStoryAsync(CreateStoryRequest request);
}