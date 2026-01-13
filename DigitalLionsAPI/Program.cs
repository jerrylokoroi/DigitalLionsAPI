using DigitalLionsAPI.Models;
using DigitalLionsAPI.Services;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Configure services
var dataFilePath = builder.Configuration["DataFilePath"];
if (string.IsNullOrEmpty(dataFilePath))
{
    dataFilePath = "Data/stories.json";
}
// Use AppContext.BaseDirectory to point to bin/Debug/net8.0 where the app runs
// This ensures the data file is writable (not in the source directory)
var fullDataPath = Path.Combine(AppContext.BaseDirectory, dataFilePath);

builder.Services.AddSingleton<IStoryService>(sp =>
{
    var logger = sp.GetService<ILogger<StoryService>>();
    return new StoryService(fullDataPath, logger);
});

// Configure CORS from appsettings
var corsOrigins = builder.Configuration.GetSection("CorsOrigins").Get<string[]>() 
                  ?? new[] { "http://localhost:5173", "http://localhost:3000" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(corsOrigins)
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

// Add HTTPS redirection for production
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowFrontend");

// Global exception handler
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        
        var error = new ErrorResponse
        {
            Message = "An unexpected error occurred",
            StatusCode = 500,
            Details = app.Environment.IsDevelopment() 
                ? context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error.Message 
                : null
        };
        
        await context.Response.WriteAsJsonAsync(error);
    });
});

// GET /stories - Returns all stories
app.MapGet("/stories", async (IStoryService storyService, ILogger<Program> logger) =>
{
    try
    {
        var stories = await storyService.GetAllStoriesAsync();
        var response = stories.Select(StoryResponse.FromDomain).ToList();
        return Results.Ok(response);
    }
    catch (InvalidOperationException ex)
    {
        logger.LogError(ex, "Failed to retrieve stories");
        return Results.Problem(
            detail: ex.Message,
            statusCode: StatusCodes.Status500InternalServerError,
            title: "Failed to retrieve stories"
        );
    }
})
.WithName("GetAllStories")
.WithOpenApi()
.Produces<List<StoryResponse>>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

// GET /stories/{id} - Returns a single story
app.MapGet("/stories/{id:int}", async (int id, IStoryService storyService, ILogger<Program> logger) =>
{
    try
    {
        if (id <= 0)
        {
            return Results.BadRequest(new ErrorResponse 
            { 
                Message = "Invalid story ID", 
                StatusCode = 400 
            });
        }

        var story = await storyService.GetStoryByIdAsync(id);
        
        if (story is null)
        {
            return Results.NotFound(new ErrorResponse 
            { 
                Message = $"Story with ID {id} not found", 
                StatusCode = 404 
            });
        }

        return Results.Ok(StoryResponse.FromDomain(story));
    }
    catch (InvalidOperationException ex)
    {
        logger.LogError(ex, "Failed to retrieve story {StoryId}", id);
        return Results.Problem(
            detail: ex.Message,
            statusCode: StatusCodes.Status500InternalServerError,
            title: "Failed to retrieve story"
        );
    }
})
.WithName("GetStoryById")
.WithOpenApi()
.Produces<StoryResponse>(StatusCodes.Status200OK)
.Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
.Produces<ErrorResponse>(StatusCodes.Status404NotFound)
.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

// POST /stories/{id}/like - Increments like count
app.MapPost("/stories/{id:int}/like", async (int id, IStoryService storyService, ILogger<Program> logger) =>
{
    try
    {
        if (id <= 0)
        {
            return Results.BadRequest(new ErrorResponse 
            { 
                Message = "Invalid story ID", 
                StatusCode = 400 
            });
        }

        var story = await storyService.IncrementLikesAsync(id);
        
        if (story is null)
        {
            return Results.NotFound(new ErrorResponse 
            { 
                Message = $"Story with ID {id} not found", 
                StatusCode = 404 
            });
        }

        return Results.Ok(StoryResponse.FromDomain(story));
    }
    catch (InvalidOperationException ex)
    {
        logger.LogError(ex, "Failed to like story {StoryId}", id);
        return Results.Problem(
            detail: ex.Message,
            statusCode: StatusCodes.Status500InternalServerError,
            title: "Failed to like story"
        );
    }
})
.WithName("LikeStory")
.WithOpenApi()
.Produces<StoryResponse>(StatusCodes.Status200OK)
.Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
.Produces<ErrorResponse>(StatusCodes.Status404NotFound)
.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

// POST /stories - Creates a new story (BONUS)
app.MapPost("/stories", async (CreateStoryRequest request, IStoryService storyService, ILogger<Program> logger) =>
{
    try
    {
        // Validate using Data Annotations
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(request);
        
        if (!Validator.TryValidateObject(request, validationContext, validationResults, true))
        {
            var errors = validationResults.Select(v => v.ErrorMessage).ToList();
            return Results.BadRequest(new ErrorResponse
            {
                Message = "Validation failed",
                StatusCode = 400,
                Details = string.Join("; ", errors)
            });
        }

        var newStory = await storyService.CreateStoryAsync(request);
        var response = StoryResponse.FromDomain(newStory);
        
        return Results.Created($"/stories/{newStory.Id}", response);
    }
    catch (InvalidOperationException ex)
    {
        logger.LogError(ex, "Failed to create story");
        return Results.Problem(
            detail: ex.Message,
            statusCode: StatusCodes.Status500InternalServerError,
            title: "Failed to create story"
        );
    }
})
.WithName("CreateStory")
.WithOpenApi()
.Produces<StoryResponse>(StatusCodes.Status201Created)
.Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
.Produces<ProblemDetails>(StatusCodes.Status500InternalServerError);

app.Run();

// Make Program accessible for testing
public partial class Program { }