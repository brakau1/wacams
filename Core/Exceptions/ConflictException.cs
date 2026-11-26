using System;
namespace WACAMS.Core.Exceptions
{
    public class ConflictException : Exception
    {
        public ConflictException(string message) : base(message) {}
        public ConflictException(string resource, string field, string value) : base($"{resource} with {field} '{value}' already exists.") {}
    }
}
