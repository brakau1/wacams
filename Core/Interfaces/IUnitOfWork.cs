namespace WACAMS.Core.Interfaces;

/// <summary>
/// Unit of Work pattern — coordinates transactions across multiple repositories.
/// Ensures all changes in a single business operation are committed atomically.
/// Implementations should wrap database operations in a transaction scope.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Persists all pending changes across all repositories within this unit of work.
    /// Should be called at the end of a business transaction after all entities have been added/updated/deleted.
    /// If any repository throws an exception, the entire transaction is rolled back.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins a new database transaction for this unit of work.
    /// All subsequent operations will participate in this transaction until CommitAsync or RollbackAsync is called.
    /// </summary>
    void BeginTransaction();

    /// <summary>
    /// Commits the current database transaction.
    /// Must be called after BeginTransaction and all repository operations.
    /// </summary>
    Task CommitAsync();

    /// <summary>
    /// Rolls back the current database transaction.
    /// All changes made since BeginTransaction are discarded.
    /// </summary>
    Task RollbackAsync();

    /// <summary>
    /// Disposes the underlying database context and releases resources.
    /// Called automatically when the using block exits.
    /// </summary>
    void Dispose();
}
