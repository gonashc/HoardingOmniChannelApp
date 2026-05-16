using Hoarding.Application.Features.Auth.Commands;
using Hoarding.Application.Features.Bookings.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hoarding.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    public AuthController(IMediator mediator) => _mediator = mediator;

    /// <summary>UC-33: Register a new user (advertiser, media_owner, etc.)</summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResult>> Register([FromBody] RegisterUserCommand cmd, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(cmd, ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
    }

    /// <summary>UC-33: Login with email or phone</summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResult>> Login([FromBody] LoginCommand cmd, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(cmd, ct);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { error = ex.Message }); }
    }
}

[ApiController]
[Route("api/v1/bookings")]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly IMediator _mediator;
    public BookingsController(IMediator mediator) => _mediator = mediator;

    /// <summary>UC-20, UC-22: Create a booking</summary>
    [HttpPost]
    public async Task<ActionResult<CreateBookingResult>> Create([FromBody] CreateBookingCommand cmd, CancellationToken ct)
    {
        try
        {
            var result = await _mediator.Send(cmd, ct);
            return CreatedAtAction(nameof(Create), new { id = result.BookingId }, result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
        catch (UnauthorizedAccessException ex) { return Unauthorized(new { error = ex.Message }); }
    }
}
