namespace GameHub.Core.Exceptions;

public class DuplicateCodeException : Exception
{
    public DuplicateCodeException()
        : base("A game session with this code already exists.")
    {
    }

    public DuplicateCodeException(Exception innerException)
        : base("A game session with this code already exists.", innerException)
    {
    }
}