namespace Application.Users;

public enum ChangePasswordResult
{
    Success,
    AccountNotFound,
    InvalidCurrentPassword,
    InvalidNewPassword
}