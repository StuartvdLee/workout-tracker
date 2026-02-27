using Api.Application.Sessions;
using Api.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/sessions/{sessionId:guid}/entries")]
public sealed class SessionEntriesController(SessionCommandService sessionCommandService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ExerciseEntryResponse>> AddEntry(Guid sessionId, [FromBody] CreateExerciseEntryRequest request, CancellationToken cancellationToken)
    {
        var entry = await sessionCommandService.AddEntryAsync(sessionId, request, cancellationToken);
        return Created($"/api/entries/{entry.Id}", entry);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ExerciseEntryResponse>>> ListEntries(Guid sessionId, CancellationToken cancellationToken)
    {
        var entries = await sessionCommandService.ListEntriesAsync(sessionId, cancellationToken);
        return Ok(entries);
    }
}
