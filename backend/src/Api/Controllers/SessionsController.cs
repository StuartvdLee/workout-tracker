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
        var session = await sessionCommandService.CreateSessionAsync(request, cancellationToken);
        return Created($"/api/sessions/{session.Id}", session);
    }
}
