namespace Application.Users;

public enum ChangePasswordResult
{
    Success,
    AccountNotFound,
    AccountInactive,
    InvalidCurrentPassword,
    InvalidNewPassword
}
