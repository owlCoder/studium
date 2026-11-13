namespace Application.Staff;

public sealed class AccessDeniedException : Exception
{
    public AccessDeniedException(string message) : base(message) { }
}

public sealed class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}
