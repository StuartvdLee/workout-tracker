using Api.Application.Progression;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/entries/{entryId:guid}/comparison")]
public sealed class EntryComparisonController(ProgressComparisonService progressComparisonService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetComparison(Guid entryId, CancellationToken cancellationToken)
    {
        var result = await progressComparisonService.BuildAsync(entryId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
