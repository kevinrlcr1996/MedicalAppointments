using Asp.Versioning;
using MedicalAppointments.Application.Authorization;
using MedicalAppointments.Application.Scheduling.Availability;
using MedicalAppointments.Contracts.Scheduling;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalAppointments.Api.Controllers.Scheduling;

[ApiController]
[Route("api/v{version:apiVersion}/availability")]
[ApiVersion("1.0")]
public sealed class AvailabilityController : ControllerBase
{
    private readonly IAvailabilityService _service;

    public AvailabilityController(IAvailabilityService service)
    {
        _service = service;
    }

    [HttpGet("doctors/{doctorId:guid}/clinics/{clinicId:guid}")]
    [Authorize(Policy = Permissions.AvailabilityRead)]
    public async Task<ActionResult<AvailabilityDayResponse>> GetDay(
        Guid doctorId,
        Guid clinicId,
        [FromQuery] DateOnly date,
        [FromQuery] int? slotDurationMinutes,
        [FromQuery] Guid? roomId,
        CancellationToken cancellationToken)
    {
        var query = new AvailabilityQuery(doctorId, clinicId, date, slotDurationMinutes, roomId);
        var result = await _service.GetDayAsync(query, cancellationToken);

        return Ok(new AvailabilityDayResponse(
            result.DoctorId,
            result.ClinicId,
            result.Date,
            result.TimeZoneId,
            result.Slots.Select(x => new AvailabilitySlotResponse(
                x.DoctorId,
                x.ClinicId,
                x.Date,
                x.StartAtUtc,
                x.EndAtUtc,
                x.StartLocal,
                x.EndLocal,
                x.DurationMinutes,
                x.TimeZoneId)).ToList()));
    }
}
