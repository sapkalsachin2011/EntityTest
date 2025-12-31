# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files
COPY ["EntityTestApi.csproj", "./"]

# Restore dependencies
RUN dotnet restore "EntityTestApi.csproj"

# Copy source code
COPY . .

# Build the application
RUN dotnet build "EntityTestApi.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "EntityTestApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=publish /app/publish .

# Expose port
EXPOSE 5274

# Set environment
ENV ASPNETCORE_ENVIRONMENT=Development
ENV ASPNETCORE_URLS=http://+:5274

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:5274/api/products || exit 1

# Run application
ENTRYPOINT ["dotnet", "EntityTestApi.dll"]
