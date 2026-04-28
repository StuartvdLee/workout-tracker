# Build stage – requires both .NET SDK and Node.js to compile TypeScript via MSBuild
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

# Install Node.js LTS (required by the BuildTypeScript MSBuild target)
RUN apt-get update && apt-get install -y --no-install-recommends ca-certificates curl gnupg && \
    mkdir -p /etc/apt/keyrings && \
    curl -fsSL https://deb.nodesource.com/gpgkey/nodesource-repo.gpg.key \
      | gpg --dearmor -o /etc/apt/keyrings/nodesource.gpg && \
    echo "deb [signed-by=/etc/apt/keyrings/nodesource.gpg] https://deb.nodesource.com/node_22.x nodistro main" \
      | tee /etc/apt/sources.list.d/nodesource.list && \
    apt-get update && apt-get install -y --no-install-recommends nodejs && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /src

# Pre-install npm packages to warm the Docker layer cache.
# The MSBuild BuildTypeScript target skips npm ci when node_modules already exists,
# so this layer is effectively reused on rebuilds without package changes.
COPY src/WorkoutTracker.Web/package.json src/WorkoutTracker.Web/package-lock.json WorkoutTracker.Web/
RUN cd WorkoutTracker.Web && npm ci

# Copy project files and restore NuGet packages
COPY src/Directory.Build.props .
COPY src/WorkoutTracker.ServiceDefaults/WorkoutTracker.ServiceDefaults.csproj WorkoutTracker.ServiceDefaults/
COPY src/WorkoutTracker.Web/WorkoutTracker.Web.csproj WorkoutTracker.Web/
RUN dotnet restore WorkoutTracker.Web/WorkoutTracker.Web.csproj

# Copy source and publish (MSBuild BuildTypeScript target runs npm run build internally)
COPY src/WorkoutTracker.ServiceDefaults/ WorkoutTracker.ServiceDefaults/
COPY src/WorkoutTracker.Web/ WorkoutTracker.Web/
RUN dotnet publish WorkoutTracker.Web/WorkoutTracker.Web.csproj \
      --no-restore \
      -c Release \
      -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "WorkoutTracker.Web.dll"]
