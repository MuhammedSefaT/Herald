using Microsoft.Extensions.DependencyInjection;

namespace Herald.Tests.Infrastructure;

/// <summary>
/// Builds a real <see cref="ServiceCollection"/> that scans the test assembly.
/// </summary>
public static class TestHost
{
    public static ServiceCollection CreateServices(Action<HeraldConfiguration>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<CallLog>();
        services.AddSingleton<TokenRecorder>();
        services.AddHerald(configuration =>
        {
            configuration.RegisterServicesFromAssemblyContaining<CallLog>();
            configure?.Invoke(configuration);
        });

        return services;
    }

    public static ServiceProvider Build(Action<HeraldConfiguration>? configure = null) =>
        BuildProvider(CreateServices(configure));

    public static ServiceProvider BuildProvider(IServiceCollection services) =>
        services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
}
