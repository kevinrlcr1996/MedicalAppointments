using Asp.Versioning;
using MedicalAppointments.Application.Auditing;
using MedicalAppointments.Application.Authorization;
using MedicalAppointments.Contracts.Auditing;
using MedicalAppointments.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MedicalAppointments.Api.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/audit-logs")]
[ApiVersion("1.0")]
[Authorize(Policy = Permissions.AuditRead)]
public sealed class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _service;

    public AuditLogsController(IAuditLogService service) => _service = service;

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<AuditLogResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResponse<AuditLogResponse>>> GetPage(
        [FromQuery] AuditLogQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetPageAsync(query, cancellationToken);
        return Ok(new PaginatedResponse<AuditLogResponse>(
            result.Items.Select(x => new AuditLogResponse(x.Id, x.UserId, x.Action, x.EntityName, x.EntityId, x.Timestamp, x.CorrelationId)).ToArray(),
            result.Page, result.PageSize, result.TotalCount, result.TotalPages));
    }
}
