using Infrastructure.Demo;
using Infrastructure.Identity;
using NUnit.Framework;

namespace Tests.Infrastructure.Demo;

[TestFixture]
public sealed class DemoSessionTests
{
    [Test]
    public async Task StartForDemoStudent_SesijaVodiNaDemoStudenta_INalogJeAktivan()
    {
        var sessions = new InMemorySessionReader();
        var accounts = new InMemoryAccountStatusReader();

        var token = DemoSession.StartForDemoStudent(sessions, accounts);
        var sessionUser = await sessions.FindUserIdAsync(token);
        var isActive = await accounts.IsActiveAsync(DemoData.DemoStudentId);

        Assert.Multiple(() =>
        {
            Assert.That(token, Has.Length.EqualTo(64));
            Assert.That(sessionUser, Is.EqualTo(DemoData.DemoStudentId));
            Assert.That(isActive, Is.True);
        });
    }

    [Test]
    public void StartForDemoStudent_SvakoPokretanjeDajeNoviToken()
    {
        var sessions = new InMemorySessionReader();
        var accounts = new InMemoryAccountStatusReader();

        var first = DemoSession.StartForDemoStudent(sessions, accounts);
        var second = DemoSession.StartForDemoStudent(sessions, accounts);

        Assert.That(first, Is.Not.EqualTo(second));
    }

    [Test]
    public void StartForDemoStudent_OdbijaNedostajuceAdaptere()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => DemoSession.StartForDemoStudent(null!, new InMemoryAccountStatusReader()),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => DemoSession.StartForDemoStudent(new InMemorySessionReader(), null!),
                Throws.TypeOf<ArgumentNullException>());
        });
    }
}
