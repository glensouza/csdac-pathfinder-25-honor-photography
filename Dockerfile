# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files for restore
COPY PathfinderPhotography.sln .
COPY PathfinderPhotography.csproj .
COPY PathfinderPhotography.ServiceDefaults/PathfinderPhotography.ServiceDefaults.csproj PathfinderPhotography.ServiceDefaults/
RUN dotnet restore PathfinderPhotography.csproj

# Copy everything else and build
COPY . .
RUN dotnet build PathfinderPhotography.csproj -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish PathfinderPhotography.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Create directories for uploads and data
RUN mkdir -p /app/wwwroot/uploads /app/Data

COPY --from=publish /app/publish .

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "PathfinderPhotography.dll"]
