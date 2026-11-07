using MedicalAppointments.Application.WaitingRoom.Stores;
using MedicalAppointments.Domain.WaitingRoom;
using MedicalAppointments.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MedicalAppointments.Infrastructure.WaitingRoom.Persistence;

public sealed class EfWaitingRoomCallStore : IWaitingRoomCallStore
{
    private readonly AppDbContext _dbContext;

    public EfWaitingRoomCallStore(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(WaitingRoomCall call, CancellationToken cancellationToken = default)
    {
        await _dbContext.WaitingRoomCalls.AddAsync(call, cancellationToken);
    }

    public Task<WaitingRoomCall?> GetTrackedByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.WaitingRoomCalls.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<bool> ExistsByAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        return _dbContext.WaitingRoomCalls.AnyAsync(x => x.AppointmentId == appointmentId, cancellationToken);
    }

    public async Task<int> GetNextSequenceAsync(
        Guid doctorId,
        Guid clinicId,
        DateOnly localDate,
        CancellationToken cancellationToken = default)
    {
        var max = await _dbContext.WaitingRoomCalls
            .Where(x => x.DoctorId == doctorId && x.ClinicId == clinicId && x.LocalDate == localDate)
            .Select(x => (int?)x.SequenceNumber)
            .MaxAsync(cancellationToken);

        return (max ?? 0) + 1;
    }

    public Task<WaitingRoomCall?> GetFirstQueuedAsync(
        Guid doctorId,
        Guid clinicId,
        DateOnly localDate,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.WaitingRoomCalls
            .Where(x => x.DoctorId == doctorId
                && x.ClinicId == clinicId
                && x.LocalDate == localDate
                && (x.Status == WaitingRoomCallStatus.Queued || x.Status == WaitingRoomCallStatus.Skipped))
            .OrderBy(x => x.SequenceNumber)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WaitingRoomCall>> GetQueuedCallsTrackedOrderedAsync(
        Guid doctorId,
        Guid clinicId,
        DateOnly localDate,
        CancellationToken cancellationToken = default)
    {
        // Tracked (sin AsNoTracking): CallNext reconcilia huérfanos y persiste en la misma transacción.
        return await _dbContext.WaitingRoomCalls
            .Where(x => x.DoctorId == doctorId
                && x.ClinicId == clinicId
                && x.LocalDate == localDate
                && (x.Status == WaitingRoomCallStatus.Queued || x.Status == WaitingRoomCallStatus.Skipped))
            .OrderBy(x => x.SequenceNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WaitingRoomCall>> GetBoardAsync(
        Guid doctorId,
        Guid clinicId,
        DateOnly localDate,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.WaitingRoomCalls
            .AsNoTracking()
            .Where(x => x.DoctorId == doctorId && x.ClinicId == clinicId && x.LocalDate == localDate)
            .OrderBy(x => x.SequenceNumber)
            .ToListAsync(cancellationToken);
    }
}