using Application.Courses;
using NUnit.Framework;

namespace Tests.Api;

public class DeactivateFeatureTests
{
    [Test]
    public void Deactivation_is_not_exposed_until_implemented_and_secured()
    {
        var applicationAssembly = typeof(ListCoursesHandler).Assembly;

        var handler = applicationAssembly
            .GetTypes()
            .FirstOrDefault(t => t.Name == "DeactivateUserHandler");

        Assert.That(handler, Is.Null);
    }
}
