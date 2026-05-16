using Hoarding.Application.Features.Inventory.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Hoarding.API.Controllers;

[ApiController]
[Route("api/v1/inventory")]
public class InventoryController : ControllerBase
{
    private readonly IMediator _mediator;
    public InventoryController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Channel-aware inventory search. Pass `channel=hoarding` or `channel=influencer`
    /// (omit for cross-channel results).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] SearchInventoryQuery query, CancellationToken ct)
    {
        var result = await _mediator.Send(query, ct);
        return Ok(result);
    }

    [HttpGet("trending")]
    public async Task<IActionResult> Trending(
        [FromQuery] string? channel,
        [FromQuery] int? cityId,
        [FromQuery] int limit = 10,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetTrendingInventoryQuery(channel, cityId, limit), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detail(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetInventoryDetailQuery(id), ct);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpGet("{id:guid}/quote")]
    public async Task<IActionResult> Quote(
        Guid id,
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate,
        [FromQuery] string? deliverableSpec,
        [FromQuery] bool includeSetupCost = true,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetInstantQuoteQuery(id, startDate, endDate, deliverableSpec, includeSetupCost), ct);
        return result == null ? NotFound() : Ok(result);
    }
}

/// <summary>
/// Convenience route - hoardings only. Forwards to /api/v1/inventory?channel=hoarding.
/// </summary>
[ApiController]
[Route("api/v1/hoardings")]
public class HoardingsController : ControllerBase
{
    private readonly IMediator _mediator;
    public HoardingsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] SearchInventoryQuery query, CancellationToken ct)
    {
        var q = query with { Channel = "Hoarding" };
        return Ok(await _mediator.Send(q, ct));
    }

    [HttpGet("trending")]
    public async Task<IActionResult> Trending([FromQuery] int? cityId, [FromQuery] int limit = 10, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetTrendingInventoryQuery("Hoarding", cityId, limit), ct));
}

/// <summary>
/// Convenience route - influencers only.
/// </summary>
[ApiController]
[Route("api/v1/influencers")]
public class InfluencersController : ControllerBase
{
    private readonly IMediator _mediator;
    public InfluencersController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] SearchInventoryQuery query, CancellationToken ct)
    {
        var q = query with { Channel = "Influencer" };
        return Ok(await _mediator.Send(q, ct));
    }

    [HttpGet("trending")]
    public async Task<IActionResult> Trending([FromQuery] int? cityId, [FromQuery] int limit = 10, CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetTrendingInventoryQuery("Influencer", cityId, limit), ct));
}
