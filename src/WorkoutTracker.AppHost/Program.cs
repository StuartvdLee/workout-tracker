var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.WorkoutTracker_Api>("api");

builder.AddProject<Projects.WorkoutTracker_Web>("web")
    .WithExternalHttpEndpoints()
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
