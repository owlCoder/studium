using System;
using Domain.Users;
using Xunit;

namespace Tests.Domain;

public class UserTests
{
    [Fact]
    public void CreateUser_WithValidData_ShouldNormalizeUsernameAndSetDefaultStatus()
    {
        // Arrange
        var id = Guid.NewGuid();
        string firstName = "  Petar  ";
        string lastName = "  Petrović  ";
        string username = "  PETAR.PETROVIC  ";

        // Act
        var user = new User(id, firstName, lastName, username);

        // Assert
        Assert.Equal(id, user.Id);
        Assert.Equal("Petar", user.FirstName);
        Assert.Equal("Petrović", user.LastName);
        Assert.Equal("petar.petrovic", user.Username);
        Assert.Equal(UserStatus.Deaktiviran, user.Status);
        Assert.Null(user.Email); // Ispravljeno umjesto HasValue
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreateUser_WithInvalidUsername_ShouldThrowArgumentException(string invalidUsername)
    {
        // Arrange
        var id = Guid.NewGuid();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new User(id, "Ime", "Prezime", invalidUsername));
    }

    [Fact]
    public void CreateUser_WithEmptyGuid_ShouldThrowArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new User(Guid.Empty, "Ime", "Prezime", "korisnik"));
    }
}
