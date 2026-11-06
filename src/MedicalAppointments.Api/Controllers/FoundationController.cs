using Asp.Versioning;
using MedicalAppointments.Application.Authorization;
using MedicalAppointments.Application.Foundation;
using MedicalAppointments.Contracts.Foundation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MedicalAppointments.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/foundation")]
[ApiVersion("1.0")]
[Authorize(Policy = Policies.FoundationTestWrite)]
public sealed class FoundationController : ControllerBase
{
    private readonly IFoundationRealtimeTestService _service;

    public FoundationController(IFoundationRealtimeTestService service)
    {
        _service = service;
    }

    [HttpPost("realtime/test")]
    [EnableRateLimiting("foundation-test")]
    [ProducesResponseType(typeof(FoundationTestEventResponse), StatusCodes.Status202Accepted)]
    public async Task<ActionResult<FoundationTestEventResponse>> PublishTestEvent(
        [FromBody] PublishFoundationTestEventRequest request,
        CancellationToken cancellationToken)
    {
        var command = new PublishFoundationTestEventCommand(request.Message);

        var outboxMessageId = await _service.PublishTestEventAsync(command, cancellationToken);

        return Accepted(new FoundationTestEventResponse(
            outboxMessageId,
            "Outbox message queued"));
    }
}