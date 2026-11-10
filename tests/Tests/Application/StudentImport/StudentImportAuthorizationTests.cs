using Application.StudentImport;
using Infrastructure.StudentImport;
using NUnit.Framework;

namespace Tests.Application.StudentImport;

[TestFixture]
public sealed class StudentImportAuthorizationTests
{
    [TestCase(AccountRoles.Student)]
    [TestCase(AccountRoles.Asistent)]
    public void Nedozvoljena_Uloga_Se_Odbija(string role)
    {
        var authorizer = new StudentImportAuthorizer(
            new SimpleCurrentUser(Guid.NewGuid(), role), new InMemoryTeachingAssignmentDirectory());

        Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await authorizer.EnsureCanImportAsync(Guid.NewGuid()));
    }

    [Test]
    public async Task Administrator_I_Odgovorni_Nastavnik_Smeju_Da_Uvoze()
    {
        var offeringId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();
        var assignments = new InMemoryTeachingAssignmentDirectory();
        assignments.Seed(teacherId, offeringId, TeachingResponsibility.OdgovorniNastavnik);

        await new StudentImportAuthorizer(
            new SimpleCurrentUser(Guid.NewGuid(), AccountRoles.Administrator), assignments)
            .EnsureCanImportAsync(offeringId);
        await new StudentImportAuthorizer(
            new SimpleCurrentUser(teacherId, AccountRoles.Nastavnik), assignments)
            .EnsureCanImportAsync(offeringId);

        Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await new StudentImportAuthorizer(
                new SimpleCurrentUser(teacherId, AccountRoles.Nastavnik), assignments)
                .EnsureCanImportAsync(Guid.NewGuid()));
    }

    [TestCase(null)]
    [TestCase(AccountRoles.Administrator)]
    public void Neprijavljen_Korisnik_Se_Odbija(string? role)
    {
        var authorizer = new StudentImportAuthorizer(
            new SimpleCurrentUser(null, role), new InMemoryTeachingAssignmentDirectory());

        Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await authorizer.EnsureCanImportAsync(Guid.NewGuid()));
    }

    [Test]
    public void Nepostojece_Izvodjenje_Se_Odbija()
    {
        var guard = new StudentOfferingGuard(new InMemoryOfferingDirectory());

        Assert.ThrowsAsync<ArgumentException>(
            async () => await guard.EnsureExistsAsync(Guid.NewGuid()));
    }
}
