using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using Studium.Api.Controllers;
using Studium.Application.ST016.DTOs;
using Studium.Application.ST016.Queries;

namespace Studium.Tests.Api.ST016;

[TestFixture]
public class CalendarControllerTests
{
    [Test]
    public async Task GetMonthlyCalendar_VracaOkRezultatSaPodacima()
    {
        // Arrange
        var mockMediator = new Mock<IMediator>();
        var ocekivaniPodaci = new List<CalendarEventDto> 
        { 
            new CalendarEventDto("Zadatak", DateTime.Now, "Tip", "Link") 
        };

        mockMediator.Setup(m => m.Send(It.IsAny<GetMonthlyCalendarQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(ocekivaniPodaci);

        var controller = new CalendarController(mockMediator.Object);

        // Act
        var rezultat = await controller.GetMonthlyCalendar(Guid.NewGuid(), 2026, 11) as OkObjectResult;

        // Assert
        Assert.That(rezultat, Is.Not.Null);
        Assert.That(rezultat.StatusCode, Is.EqualTo(200));
        Assert.That(rezultat.Value, Is.EqualTo(ocekivaniPodaci));
    }
}