using Microsoft.Extensions.Hosting;

namespace Api;

internal static class DemoAdapterPolicy
{
    public const string TestingEnvironment = "Testing";

    public static bool IsAllowed(IHostEnvironment environment)
    {
        return environment.IsDevelopment() || environment.IsEnvironment(TestingEnvironment);
    }
}
