using Microsoft.Extensions.DependencyInjection;

namespace Herald.Tests.Infrastructure;

/// <summary>
/// Test assembly'sini tarayan gerçek bir <see cref="ServiceCollection"/> kurar.
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
