namespace MedicalAppointments.Application.Common;

public interface IBookingCoordinator
{
    Task<T> ExecuteInTransactionAsync<T>(
        IEnumerable<string> lockResources,
        Func<Task<T>> action,
        CancellationToken cancellationToken = default);
}