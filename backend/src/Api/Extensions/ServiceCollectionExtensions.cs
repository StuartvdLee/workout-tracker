using Api.Application.Exercises;
using Api.Application.Progression;
using Api.Application.Sessions;
using Api.Infrastructure.Repositories;

namespace Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWorkoutTrackerServices(this IServiceCollection services)
    {
        services.AddScoped<IWorkoutSessionRepository, WorkoutSessionRepository>();
        services.AddScoped<IExerciseEntryRepository, ExerciseEntryRepository>();
        services.AddScoped<SessionCommandService>();
        services.AddScoped<ExerciseHistoryQueryService>();
        services.AddScoped<ProgressComparisonService>();
        return services;
    }
}
