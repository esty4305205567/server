namespace GameHub.Core.Exceptions;

public class DuplicateUsernameException : Exception
{
    public DuplicateUsernameException()
        : base("Username is already taken.")
    {
    }

    public DuplicateUsernameException(Exception innerException)
        : base("Username is already taken.", innerException)
    {
    }
}