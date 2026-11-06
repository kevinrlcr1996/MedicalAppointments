using MedicalAppointments.Application.Common;

namespace MedicalAppointments.Application.Scheduling.Blockouts;

public interface IBlockoutService
{
    Task<PagedResult<BlockoutDto>> GetPagedAsync(
        BlockoutQuery query,
        CancellationToken cancellationToken = default);

    Task<BlockoutDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<BlockoutDto> CreateAsync(
        CreateBlockoutCommand command,
        CancellationToken cancellationToken = default);

    Task<BlockoutDto> UpdateAsync(
        UpdateBlockoutCommand command,
        CancellationToken cancellationToken = default);

    Task<BlockoutDto> SetActiveAsync(
        SetBlockoutActiveCommand command,
        CancellationToken cancellationToken = default);
}