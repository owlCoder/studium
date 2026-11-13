namespace Application.Auth;

public sealed class ActivateAccountCommand
{
    public required string Username { get; set; }
    public required string Token { get; set; }
    public required string Password { get; set; }
}
