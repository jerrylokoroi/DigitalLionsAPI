# Multi-stage Dockerfile for .NET 8 API
# Optimized for Render deployment

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY DigitalLionsAPI.sln .
COPY DigitalLionsAPI/DigitalLionsAPI.csproj DigitalLionsAPI/
COPY DigitalLionsAPI.Tests/DigitalLionsAPI.Tests.csproj DigitalLionsAPI.Tests/

# Restore dependencies
RUN dotnet restore

# Copy all source code
COPY . .

# Build and publish in Release mode
WORKDIR /src/DigitalLionsAPI
RUN dotnet publish -c Release -o /app/publish --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy published application from build stage
COPY --from=build /app/publish .

# Expose port 10000 (Render's default for web services)
EXPOSE 10000

# Set environment variables for production
ENV ASPNETCORE_URLS=http://+:10000
ENV ASPNETCORE_ENVIRONMENT=Production

# Run the application
ENTRYPOINT ["dotnet", "DigitalLionsAPI.dll"]
