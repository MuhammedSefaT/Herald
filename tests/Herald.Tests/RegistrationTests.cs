using System.Reflection;
using System.Reflection.Emit;
using Herald.Tests.Fixtures;
using Herald.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Herald.Tests;

public sealed class RegistrationTests
{
    [Fact]
    public void Resolve_HeraldSenderAndPublisher_AreAvailable()
    {
        using var provider = TestHost.Build();

        Assert.IsType<HeraldDispatcher>(provider.GetRequiredService<IHerald>());
        Assert.IsType<HeraldDispatcher>(provider.GetRequiredService<ISender>());
        Assert.IsType<HeraldDispatcher>(provider.GetRequiredService<IPublisher>());
    }

    [Fact]
    public void DefaultLifetime_IsTransient()
    {
        var services = TestHost.CreateServices();
        using var provider = TestHost.BuildProvider(services);

        Assert.Equal(ServiceLifetime.Transient, services.Single(descriptor => descriptor.ServiceType == typeof(IHerald)).Lifetime);
        Assert.NotSame(provider.GetRequiredService<IHerald>(), provider.GetRequiredService<IHerald>());
    }

    [Fact]
    public void ScopedLifetime_SenderAndPublisherResolveSameHeraldWithinScope()
    {
        var services = TestHost.CreateServices(configuration => configuration.Lifetime = ServiceLifetime.Scoped);
        using var provider = TestHost.BuildProvider(services);
        using var firstScope = provider.CreateScope();
        using var secondScope = provider.CreateScope();

        var herald = firstScope.ServiceProvider.GetRequiredService<IHerald>();

        Assert.Same(herald, firstScope.ServiceProvider.GetRequiredService<ISender>());
        Assert.Same(herald, firstScope.ServiceProvider.GetRequiredService<IPublisher>());
        Assert.NotSame(herald, secondScope.ServiceProvider.GetRequiredService<IHerald>());
        Assert.All(
            services.Where(descriptor => descriptor.ServiceType == typeof(IHerald) || descriptor.ServiceType == typeof(ISender) || descriptor.ServiceType == typeof(IPublisher)),
            descriptor => Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime));
    }

    [Fact]
    public void SingletonLifetime_SenderAndPublisherResolveSameHerald()
    {
        using var provider = TestHost.Build(configuration => configuration.Lifetime = ServiceLifetime.Singleton);

        var herald = provider.GetRequiredService<IHerald>();

        Assert.Same(herald, provider.GetRequiredService<IHerald>());
        Assert.Same(herald, provider.GetRequiredService<ISender>());
        Assert.Same(herald, provider.GetRequiredService<IPublisher>());
    }

    [Fact]
    public void AddHerald_WithoutAssemblies_ThrowsArgumentException()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<ArgumentException>(() => services.AddHerald(_ => { }));

        Assert.Equal("configuration", exception.ParamName);
        Assert.Empty(services);
    }

    [Fact]
    public void AddHerald_NullArguments_ThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddHerald(null!));
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddHerald(_ => { }));
    }

    [Fact]
    public void AddHerald_TwoHandlersForSameRequest_ThrowsAndRegistersNothing()
    {
        var services = new ServiceCollection();
        var assembly = CreateAssemblyWithTwoHandlersForConflictRequest();

        var exception = Assert.Throws<InvalidOperationException>(
            () => services.AddHerald(configuration => configuration.RegisterServicesFromAssembly(assembly)));

        Assert.Contains(typeof(ConflictRequest).FullName!, exception.Message);
        Assert.Contains("Conflicts.FirstConflictHandler", exception.Message);
        Assert.Contains("Conflicts.SecondConflictHandler", exception.Message);
        Assert.Empty(services);
    }

    [Fact]
    public void AddHerald_HandlerConflictsWithExistingRegistration_Throws()
    {
        var services = new ServiceCollection();
        services.AddSingleton<CallLog>();
        services.AddTransient<IRequestHandler<Ping, Pong>>(provider => new PingHandler(provider.GetRequiredService<CallLog>()));

        var exception = Assert.Throws<InvalidOperationException>(
            () => services.AddHerald(configuration => configuration.RegisterServicesFromAssemblyContaining<CallLog>()));

        Assert.Contains(typeof(Ping).FullName!, exception.Message);
        Assert.Contains(typeof(PingHandler).FullName!, exception.Message);
    }

    [Fact]
    public async Task AddHerald_CalledTwice_DoesNotDuplicateRegistrations()
    {
        var services = TestHost.CreateServices(configuration => configuration.AddOpenBehavior(typeof(RecordingOpenBehavior<,>)));
        var registrationCount = services.Count;
        services.AddHerald(configuration => configuration
            .RegisterServicesFromAssemblyContaining<CallLog>()
            .AddOpenBehavior(typeof(RecordingOpenBehavior<,>)));
        using var provider = TestHost.BuildProvider(services);
        var herald = provider.GetRequiredService<IHerald>();

        await herald.Send(new Ping("twice"));
        await herald.Send(new VoidCommand("twice"));
        await herald.Publish(new OrderPlaced());

        var entries = provider.GetRequiredService<CallLog>().Entries;
        Assert.Equal(registrationCount, services.Count);
        Assert.Single(entries, entry => entry == "handler:Ping");
        Assert.Single(entries, entry => entry == "behavior:Ping:Pong");
        Assert.Single(entries, entry => entry == "handler:VoidCommand:twice");
        Assert.Single(entries, entry => entry == "FirstOrderPlacedHandler:start");
        Assert.Single(entries, entry => entry == "SecondOrderPlacedHandler:start");
    }

    [Fact]
    public async Task ClassImplementingSeveralHandlerInterfaces_IsRegisteredForEach()
    {
        using var provider = TestHost.Build();
        var herald = provider.GetRequiredService<IHerald>();

        Assert.Equal("multi", await herald.Send(new MultiRequest()));
        await herald.Send(new MultiVoidRequest());
        await herald.Publish(new MultiNotification());

        Assert.Equal(
            new[] { "handler:MultiVoidRequest", "handler:MultiNotification" },
            provider.GetRequiredService<CallLog>().Entries);
    }

    [Fact]
    public async Task AbstractAndOpenGenericHandlers_AreNotRegistered()
    {
        var services = TestHost.CreateServices();
        using var provider = TestHost.BuildProvider(services);
        var herald = provider.GetRequiredService<IHerald>();

        Assert.IsType<InheritedRequestHandler>(Assert.Single(provider.GetServices<IRequestHandler<InheritedRequest, string>>()));
        Assert.Equal("derived", await herald.Send(new InheritedRequest()));
        Assert.DoesNotContain(services, descriptor => descriptor.ImplementationType == typeof(InheritedRequestHandlerBase));
        Assert.DoesNotContain(
            services,
            descriptor => descriptor.ImplementationType is { IsGenericType: true } type
                && type.GetGenericTypeDefinition() == typeof(GenericRequestHandler<>));
        await Assert.ThrowsAsync<InvalidOperationException>(() => herald.Send(new GenericRequest<int>(1)));
    }

    [Fact]
    public void Handlers_AreRegisteredAsTransient()
    {
        var services = TestHost.CreateServices();

        var handlerDescriptors = services
            .Where(descriptor => descriptor.ServiceType.IsGenericType)
            .Where(descriptor =>
                descriptor.ServiceType.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)
                || descriptor.ServiceType.GetGenericTypeDefinition() == typeof(IRequestHandler<>)
                || descriptor.ServiceType.GetGenericTypeDefinition() == typeof(INotificationHandler<>))
            .ToList();

        Assert.NotEmpty(handlerDescriptors);
        Assert.All(handlerDescriptors, descriptor => Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime));
    }

    [Fact]
    public void AddOpenBehavior_TypeThatIsNotOpenGeneric_ThrowsArgumentException()
    {
        var configuration = new HeraldConfiguration();

        var nonGeneric = Assert.Throws<ArgumentException>(() => configuration.AddOpenBehavior(typeof(OuterPingBehavior)));
        var closedGeneric = Assert.Throws<ArgumentException>(() => configuration.AddOpenBehavior(typeof(MiddleOpenBehavior<Ping, Pong>)));

        Assert.Equal("openBehaviorType", nonGeneric.ParamName);
        Assert.Equal("openBehaviorType", closedGeneric.ParamName);
    }

    [Fact]
    public void AddOpenBehavior_TypeThatIsNotPipelineBehavior_ThrowsArgumentException()
    {
        var configuration = new HeraldConfiguration();

        var exception = Assert.Throws<ArgumentException>(() => configuration.AddOpenBehavior(typeof(List<>)));

        Assert.Equal("openBehaviorType", exception.ParamName);
    }

    [Fact]
    public void ConfigurationMethods_ReturnSameInstance()
    {
        var configuration = new HeraldConfiguration();
        var assembly = typeof(CallLog).Assembly;

        Assert.Same(configuration, configuration.RegisterServicesFromAssembly(assembly));
        Assert.Same(configuration, configuration.RegisterServicesFromAssemblies(assembly));
        Assert.Same(configuration, configuration.RegisterServicesFromAssemblyContaining<CallLog>());
        Assert.Same(configuration, configuration.AddBehavior<IPipelineBehavior<Ping, Pong>, OuterPingBehavior>());
        Assert.Same(configuration, configuration.AddBehavior(typeof(IPipelineBehavior<Ping, Pong>), typeof(InnerPingBehavior)));
        Assert.Same(configuration, configuration.AddOpenBehavior(typeof(MiddleOpenBehavior<,>)));
    }

    /// <summary>
    /// Aynı istek için iki handler içeren bir assembly üretir. Test assembly'sine böyle iki handler konsaydı
    /// test assembly'sini tarayan tüm testler çakışma hatası alırdı.
    /// </summary>
    private static Assembly CreateAssemblyWithTwoHandlersForConflictRequest()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Herald.Tests.Conflicts"), AssemblyBuilderAccess.Run);
        var module = assembly.DefineDynamicModule("Herald.Tests.Conflicts");
        var handlerInterface = typeof(IRequestHandler<ConflictRequest, string>);
        var handleMethod = handlerInterface.GetMethod(nameof(IRequestHandler<ConflictRequest, string>.Handle))!;

        foreach (var typeName in new[] { "Conflicts.FirstConflictHandler", "Conflicts.SecondConflictHandler" })
        {
            var type = module.DefineType(typeName, TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class);
            type.AddInterfaceImplementation(handlerInterface);

            var method = type.DefineMethod(
                handleMethod.Name,
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot,
                handleMethod.ReturnType,
                handleMethod.GetParameters().Select(parameter => parameter.ParameterType).ToArray());
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ret);
            type.DefineMethodOverride(method, handleMethod);

            type.CreateType();
        }

        return assembly;
    }
}
