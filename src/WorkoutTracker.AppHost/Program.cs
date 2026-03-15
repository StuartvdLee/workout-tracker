var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithLifetime(ContainerLifetime.Persistent);

var workoutDb = postgres.AddDatabase("workout-tracker-db", databaseName: "workout_tracker");

var api = builder.AddProject<Projects.WorkoutTracker_Api>("api")
    .WithReference(workoutDb)
    .WaitFor(workoutDb);

builder.AddProject<Projects.WorkoutTracker_Web>("web")
    .WithExternalHttpEndpoints()
    .WithReference(api)
    .WaitFor(api);

builder.Build().Run();
