using Api.Application.Exercises;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/exercises/{exerciseName}/history")]
public sealed class ExerciseHistoryController(ExerciseHistoryQueryService exerciseHistoryQueryService) : ControllerBase
{
    private static readonly Guid DefaultUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [HttpGet]
    public async Task<IActionResult> GetHistory(string exerciseName, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var history = await exerciseHistoryQueryService.GetHistoryAsync(DefaultUserId, exerciseName, page, pageSize, cancellationToken);

        return Ok(new
        {
            history.ExerciseName,
            history.Page,
            history.PageSize,
            history.Total,
            Entries = history.Entries
        });
    }
}
