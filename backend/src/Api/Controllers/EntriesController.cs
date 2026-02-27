using Api.Application.Sessions;
using Api.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/entries/{entryId:guid}")]
public sealed class EntriesController(SessionCommandService sessionCommandService) : ControllerBase
{
    [HttpPatch]
    public async Task<ActionResult<ExerciseEntryResponse>> UpdateEntry(Guid entryId, [FromBody] UpdateExerciseEntryRequest request, CancellationToken cancellationToken)
    {
        var updated = await sessionCommandService.UpdateEntryAsync(entryId, request, cancellationToken);
        if (updated is null)
        {
            return NotFound();
        }

        return Ok(updated);
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteEntry(Guid entryId, CancellationToken cancellationToken)
    {
        var deleted = await sessionCommandService.DeleteEntryAsync(entryId, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
