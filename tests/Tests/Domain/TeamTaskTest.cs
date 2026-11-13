
using NUnit.Framework;
using Domain.Models;
using Domain.Enums;
using Newtonsoft.Json.Bson;
namespace Tests.Domain;

[TestFixture]
public sealed class TeamTaskTest
{

    private static readonly DateTime CreatedAt = new(2026, 11, 12, 22, 0, 0, DateTimeKind.Utc);

    private static TeamTask NewTask(string? name = "Testiranje podataka", Guid? id = null) =>
        new(id ?? Guid.NewGuid(), Guid.NewGuid(), name, "Opis testiranja", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), CreatedAt);

    [Test]

    public void Constructor_ValidData_IsValid_Planned()
    { 
        var task = NewTask("Testiranje podataka");
        Assert.Multiple(() =>
        {
            Assert.That(task.IsValid(), Is.True);
            Assert.That(task.Status, Is.EqualTo(TeamTaskStatus.Planned));
            Assert.That(task.Name, Is.EqualTo("Testiranje podataka"));
            Assert.That(task.Description, Is.EqualTo("Opis testiranja"));
        }

        );
    }


    [Test]

    public void Constructor_NullName_DoesNotThrowExc_IsValid()
    {
        TeamTask? newTask = null;
        Assert.That(() => newTask = NewTask(name: null), Throws.Nothing);
        Assert.That(newTask!.IsValid(), Is.False);
    }

    [TestCase("")]
    [TestCase("    ")]

    public void Constructor_BlankName_IsInValid(string name)
    {
        Assert.That(NewTask(name).IsValid(), Is.False);
    }

    [Test]

    public void Constructor_EmptyId_IsInValid()
    {
        Assert.That(NewTask(id:Guid.Empty).IsValid(),Is.False);
    }

    [Test]

    public void Status_IsNotPublicSetter()
    {
        var setter = typeof(TeamTask).GetProperty(nameof(TeamTask.Status))!.SetMethod ;
        Assert.That(setter is null || !setter.IsPublic, Is.True);
    }

    [Test]

    public void ChangedStatus_UpdateStatus_Author_Time()
    {
        var task = NewTask();
        var author = Guid.NewGuid();
        var time = CreatedAt.AddHours(3);
        task.ChangedStatus(TeamTaskStatus.Finished, author, time);

        Assert.Multiple(() =>
        {
            Assert.That(task.Status, Is.EqualTo(TeamTaskStatus.Finished));
            Assert.That(task.LastChangedBy, Is.EqualTo(author));
            Assert.That(task.LastChangedAt, Is.EqualTo(time));
        });
    }
         
    }
