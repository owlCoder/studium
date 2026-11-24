using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Application.Activities;
using Application.Engagements;
using Application.Enrollments;
using Application.Home;
using Domain.Dashboard;
using Domain.Staff;
using Moq;
using NUnit.Framework;

namespace Tests.Application.Home;

[TestFixture]
public sealed class StaffRoleLabelTests
{
    [TestCase("ResponsibleTeacher", "Odgovorni nastavnik")]
    [TestCase("Teacher", "Nastavnik")]
    [TestCase("Assistant", "Asistent")]

    public void SvakaUlogaImaOznakuNaSrpskom(string roleName, string expected)
    {
        Assert.That(RoleLabels.For(roleName), Is.EqualTo(expected));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("Dean")]
    public void NepoznataIliPraznaUloga_NemaOznaku(string? roleName)
    {
        Assert.That(RoleLabels.For(roleName), Is.Null);
    }

    [Test] // Test iz zadatka US-7.8
    public async Task Asistent_VidiOznakuAsistent_USekcijiPredajem()
    {
        var userId = Guid.NewGuid();
        var bp = Guid.NewGuid();
        var ers = Guid.NewGuid();


        var staff = new Mock<IStaffOfferingsReader>();
        staff.Setup(x => x.ListActiveForUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new TeachingOfferingDto(bp, "Baze podataka", "2026/27", Responsibility.Assistant)
            });

        var student = new Mock<IStudentOfferingsReader>();
        student.Setup(x => x.ListActiveForStudentAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {

                new EnrolledOfferingDto(ers, "Elementi razvoja softvera", "2026/27")
            });

        var counter = new Mock<IOpenActivityCounter>();
        counter.Setup(x => x.CountOpenAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var handler = new GetHomeOverviewHandler(
            new ListMyOfferingsHandler(staff.Object, student.Object),
            counter.Object,
            new FeatureRegistry(Array.Empty<SupportedActivityType>()));

        var result = await handler.HandleAsync(userId);

        Assert.Multiple(() =>
        {
            Assert.That(result.Teaching.Single().RoleLabel, Is.EqualTo("Asistent"));
            Assert.That(result.Enrolled.Single().RoleLabel, Is.Null);

        });
    }
}


