namespace WACAMS.Core.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
    public NotFoundException(string entityType, object key) 
        : base($"Entity '{entityType}' ({key}) was not found.") { }
}
