namespace HoppyRoute.Application.Exceptions
{
    /// <summary>
    /// The caller is authenticated but not allowed to do this particular thing
    /// (for example: creating an Admin). Controllers map it to 403.
    /// </summary>
    public class ForbiddenOperationException : Exception
    {
        public ForbiddenOperationException(string message) : base(message) { }
    }

    /// <summary>
    /// The current password given on change-password is wrong. This is a validation
    /// error (400), not an authentication failure: the caller's token is still valid.
    /// </summary>
    public class InvalidCurrentPasswordException : Exception
    {
        public InvalidCurrentPasswordException(string message) : base(message) { }
    }
}
