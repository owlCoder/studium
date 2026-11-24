using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Infrastructure.Demo;
using Infrastructure.Identity;
using NUnit.Framework;

namespace Tests.Infrastructure.Demo;

[TestFixture]
public sealed class DemoStaffSessionTests
{
    [Test]
    public async Task StartForDemoTeacher_SesijaVodiNaDemoNastavnika_INalogJeAktivan()
    {
        var sessions = new InMemorySessionReader();
        var accounts = new InMemoryAccountStatusReader();

        var token = DemoSession.StartForDemoTeacher(sessions, accounts);
        var sessionUser = await sessions.FindUserIdAsync(token);
        var isActive = await accounts.IsActiveAsync(DemoData.DemoTeacherId);

        Assert.Multiple(() =>
        {
            Assert.That(token, Has.Length.EqualTo(64));
            Assert.That(sessionUser, Is.EqualTo(DemoData.DemoTeacherId));
            Assert.That(isActive, Is.True);
        });
    }

    [Test]
    public async Task StartForDemoAssistant_SesijaVodiNaDemoAsistentkinju_INalogJeAktivan()
    {
        var sessions = new InMemorySessionReader();
        var accounts = new InMemoryAccountStatusReader();

        var token = DemoSession.StartForDemoAssistant(sessions, accounts);
        var sessionUser = await sessions.FindUserIdAsync(token);
        var isActive = await accounts.IsActiveAsync(DemoData.DemoAssistantId);

        Assert.Multiple(() =>
        {
            Assert.That(token, Has.Length.EqualTo(64));
            Assert.That(sessionUser, Is.EqualTo(DemoData.DemoAssistantId));
            Assert.That(isActive, Is.True);
        });
    }

    [Test]
    public async Task SvakaDemoOsobaDobijaSvojToken()
    {
        var sessions = new InMemorySessionReader();
        var accounts = new InMemoryAccountStatusReader();

        var student = DemoSession.StartForDemoStudent(sessions, accounts);
        var teacher = DemoSession.StartForDemoTeacher(sessions, accounts);
        var assistant = DemoSession.StartForDemoAssistant(sessions, accounts);

        Assert.That(new[] { student, teacher, assistant }, Is.Unique);
        Assert.That(await sessions.FindUserIdAsync(student), Is.EqualTo(DemoData.DemoStudentId));
    }

    [Test]
    public void StaffSesije_OdbijajuNedostajuceAdaptere()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => DemoSession.StartForDemoTeacher(null!, new InMemoryAccountStatusReader()),
                Throws.TypeOf<ArgumentNullException>());
            Assert.That(() => DemoSession.StartForDemoAssistant(new InMemorySessionReader(), null!),
                Throws.TypeOf<ArgumentNullException>());
        });
    }
}
