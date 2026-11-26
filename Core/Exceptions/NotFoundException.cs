using System;
namespace WACAMS.Core.Exceptions
{
    public class NotFoundException : Exception
    {
        public NotFoundException(string message) : base(message) {}
        public NotFoundException(string entity, int id) : base($"{entity} with id {id} not found.") {}
        public NotFoundException(string entity, Guid id) : base($"{entity} with id {id} not found.") {}
    }
}
