using Microsoft.EntityFrameworkCore;
using Polly;

namespace Order.Domain.Contracts;

public interface IUnitOfWork
{
    public DbContext Context { get; }
    IOrderRepository Orders { get; }
    IOrderFlightRepository OrderFlights { get; }
    IOrderTrainRepository OrderTrains { get; }
    ISavedPassengerRepository SavedPassengers { get; }
    IOrderTrainCarTransportRepository TrainCarTransports { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}