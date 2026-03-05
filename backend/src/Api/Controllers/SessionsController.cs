using Api.Application.Sessions;
using Api.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/sessions")]
public sealed class SessionsController(SessionCommandService sessionCommandService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<WorkoutSessionResponse>> CreateSession([FromBody] CreateSessionRequest request, CancellationToken cancellationToken)
    {
        if (!SessionCommandService.IsValidWorkoutType(request.WorkoutType))
        {
            var details = new ValidationProblemDetails
            {
                Title = "One or more validation errors occurred.",
                Status = StatusCodes.Status400BadRequest
            };
            details.Errors.Add("workoutType", ["Please select a workout type."]);
            return BadRequest(details);
        }

        var session = await sessionCommandService.CreateSessionAsync(request, cancellationToken);
        return Created($"/api/sessions/{session.Id}", session);
    }
}
