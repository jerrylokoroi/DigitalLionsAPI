using DigitalLionsAPI.Models;
using DigitalLionsAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure services
var dataFilePath = Path.Combine(builder.Environment.ContentRootPath, "Data", "stories.json");
builder.Services.AddSingleton<IStoryService>(new StoryService(dataFilePath));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");

// GET /stories - Returns all stories
app.MapGet("/stories", async (IStoryService storyService) =>
{
    var stories = await storyService.GetAllStoriesAsync();
    return Results.Ok(stories);
})
.WithName("GetAllStories")
.WithOpenApi();

// GET /stories/{id} - Returns a single story
app.MapGet("/stories/{id:int}", async (int id, IStoryService storyService) =>
{
    var story = await storyService.GetStoryByIdAsync(id);
    return story is not null ? Results.Ok(story) : Results.NotFound(new { message = "Story not found" });
})
.WithName("GetStoryById")
.WithOpenApi();

// POST /stories/{id}/like - Increments like count
app.MapPost("/stories/{id:int}/like", async (int id, IStoryService storyService) =>
{
    var story = await storyService.IncrementLikesAsync(id);
    return story is not null ? Results.Ok(story) : Results.NotFound(new { message = "Story not found" });
})
.WithName("LikeStory")
.WithOpenApi();

// POST /stories - Creates a new story (BONUS)
app.MapPost("/stories", async (CreateStoryRequest request, IStoryService storyService) =>
{
    // Basic validation
    if (string.IsNullOrWhiteSpace(request.Title))
        return Results.BadRequest(new { message = "Title is required" });

    if (string.IsNullOrWhiteSpace(request.Category))
        return Results.BadRequest(new { message = "Category is required" });

    if (string.IsNullOrWhiteSpace(request.Summary))
        return Results.BadRequest(new { message = "Summary is required" });

    if (string.IsNullOrWhiteSpace(request.Description))
        return Results.BadRequest(new { message = "Description is required" });

    var newStory = await storyService.CreateStoryAsync(request);
    return Results.Created($"/stories/{newStory.Id}", newStory);
})
.WithName("CreateStory")
.WithOpenApi();

app.Run();

// Make Program accessible for testing
public partial class Program { }