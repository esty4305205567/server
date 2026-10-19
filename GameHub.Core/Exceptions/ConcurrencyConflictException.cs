namespace GameHub.Core.Exceptions;

public class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException()
        : base("The entity was modified by another user.")
    {
    }

    public ConcurrencyConflictException(Exception innerException)
        : base("The entity was modified by another user.", innerException)
    {
    }
}