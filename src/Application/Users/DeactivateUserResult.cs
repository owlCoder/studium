namespace Application.Users;

public enum DeactivateUserResult
{
    Deactivated,
    NotFound,
    AlreadyDeactivated,
    LastActiveAdministrator
}
