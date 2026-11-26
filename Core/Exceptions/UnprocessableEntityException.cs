using System;
namespace WACAMS.Core.Exceptions
{
    public class UnprocessableEntityException : Exception
    {
        public UnprocessableEntityException(string message) : base(message) {}
    }
}
