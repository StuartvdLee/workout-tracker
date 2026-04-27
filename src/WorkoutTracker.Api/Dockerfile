# Build stage – restore and publish the API project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first to leverage Docker layer caching for NuGet restore
COPY src/WorkoutTracker.ServiceDefaults/WorkoutTracker.ServiceDefaults.csproj WorkoutTracker.ServiceDefaults/
COPY src/WorkoutTracker.Infrastructure/WorkoutTracker.Infrastructure.csproj WorkoutTracker.Infrastructure/
COPY src/WorkoutTracker.Api/WorkoutTracker.Api.csproj WorkoutTracker.Api/
RUN dotnet restore WorkoutTracker.Api/WorkoutTracker.Api.csproj

# Copy source and publish
COPY src/WorkoutTracker.ServiceDefaults/ WorkoutTracker.ServiceDefaults/
COPY src/WorkoutTracker.Infrastructure/ WorkoutTracker.Infrastructure/
COPY src/WorkoutTracker.Api/ WorkoutTracker.Api/
RUN dotnet publish WorkoutTracker.Api/WorkoutTracker.Api.csproj \
      --no-restore \
      -c Release \
      -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "WorkoutTracker.Api.dll"]
