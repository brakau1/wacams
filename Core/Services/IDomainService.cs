namespace WACAMS.Core.Services;

/// <summary>
/// Base interface for all domain services in the WACAMS system.
/// Domain services encapsulate business logic that does not belong to a single entity.
/// Implementations should be stateless and thread-safe for concurrent request handling.
/// </summary>
public interface IDomainService
{
    /// <summary>
    /// Marker method — serves as the base contract for all domain services.
    /// Concrete implementations define specific business operations.
    /// </summary>
    void Execute();
}
