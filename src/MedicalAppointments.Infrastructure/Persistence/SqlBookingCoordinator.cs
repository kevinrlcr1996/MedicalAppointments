using MedicalAppointments.Application.Common;
using MedicalAppointments.Application.Common.Exceptions;
using MedicalAppointments.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace MedicalAppointments.Infrastructure.Persistence;

public sealed class SqlBookingCoordinator : IBookingCoordinator
{
    private readonly AppDbContext _dbContext;

    public SqlBookingCoordinator(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<T> ExecuteInTransactionAsync<T>(
        IEnumerable<string> lockResources,
        Func<Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        var resources = lockResources
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        var executionStrategy = _dbContext.Database.CreateExecutionStrategy();

        return await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken);

            try
            {
                foreach (var resource in resources)
                {
                    await AcquireLockAsync(resource, transaction, cancellationToken);
                }

                var result = await action();

                await transaction.CommitAsync(cancellationToken);

                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    private async Task AcquireLockAsync(
        string resource,
        IDbContextTransaction transaction,
        CancellationToken cancellationToken)
    {
        var connection = _dbContext.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();

        command.Transaction = transaction.GetDbTransaction();
        command.CommandText = """
            EXEC sp_getapplock
                @Resource = @Resource,
                @LockMode = 'Exclusive',
                @LockOwner = 'Transaction',
                @LockTimeout = @LockTimeout;
            """;

        var resourceParameter = command.CreateParameter();
        resourceParameter.ParameterName = "@Resource";
        resourceParameter.Value = resource;
        command.Parameters.Add(resourceParameter);

        var timeoutParameter = command.CreateParameter();
        timeoutParameter.ParameterName = "@LockTimeout";
        timeoutParameter.Value = 5000;
        command.Parameters.Add(timeoutParameter);

        var result = await command.ExecuteScalarAsync(cancellationToken);

        var lockResult = result is int intValue
            ? intValue
            : Convert.ToInt32(result);

        if (lockResult < 0)
        {
            throw new ConflictException("Unable to acquire booking lock. Please retry.");
        }
    }
}