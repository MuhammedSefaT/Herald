using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Herald;

/// <summary>
/// Extension methods that register Herald services on an <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    private static readonly Type[] HandlerInterfaces =
    [
        typeof(IRequestHandler<,>),
        typeof(IRequestHandler<>),
        typeof(INotificationHandler<>),
    ];

    /// <summary>
    /// Registers Herald: scans the configured assemblies for handlers, adds the configured behaviors and
    /// registers <see cref="IHerald"/>, <see cref="ISender"/> and <see cref="IPublisher"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every non-abstract, non-open-generic class that implements <see cref="IRequestHandler{TRequest, TResponse}"/>,
    /// <see cref="IRequestHandler{TRequest}"/> or <see cref="INotificationHandler{TNotification}"/> is registered as
    /// transient. A class that implements several handler interfaces is registered for each of them.
    /// </para>
    /// <para>
    /// The method can be called more than once; handler, behavior and <see cref="IHerald"/> registrations that
    /// already exist are not added again.
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The Herald configuration.</param>
    /// <returns>The same service collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="configuration"/> is null.</exception>
    /// <exception cref="ArgumentException">No assembly is configured for scanning.</exception>
    /// <exception cref="InvalidOperationException">More than one handler is found for the same request type.</exception>
    public static IServiceCollection AddHerald(this IServiceCollection services, Action<HeraldConfiguration> configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var heraldConfiguration = new HeraldConfiguration();
        configuration(heraldConfiguration);

        if (heraldConfiguration.Assemblies.Count == 0)
        {
            throw new ArgumentException(
                "At least one assembly must be registered for handler scanning. Call RegisterServicesFromAssembly, " +
                "RegisterServicesFromAssemblies or RegisterServicesFromAssemblyContaining<T> in the configuration.",
                nameof(configuration));
        }

        var handlers = FindHandlers(heraldConfiguration.Assemblies);
        var requestHandlers = GetNewRequestHandlers(
            services,
            handlers.Where(handler => handler.ServiceType.GetGenericTypeDefinition() != typeof(INotificationHandler<>)));

        foreach (var descriptor in requestHandlers)
        {
            services.Add(descriptor);
        }

        foreach (var handler in handlers.Where(handler => handler.ServiceType.GetGenericTypeDefinition() == typeof(INotificationHandler<>)))
        {
            services.TryAddEnumerable(ServiceDescriptor.Transient(handler.ServiceType, handler.ImplementationType));
        }

        foreach (var descriptor in heraldConfiguration.Behaviors)
        {
            services.TryAddEnumerable(descriptor);
        }

        services.TryAdd(new ServiceDescriptor(typeof(IHerald), typeof(HeraldDispatcher), heraldConfiguration.Lifetime));
        services.TryAdd(new ServiceDescriptor(typeof(ISender), static provider => provider.GetRequiredService<IHerald>(), heraldConfiguration.Lifetime));
        services.TryAdd(new ServiceDescriptor(typeof(IPublisher), static provider => provider.GetRequiredService<IHerald>(), heraldConfiguration.Lifetime));

        return services;
    }

    private static List<(Type ServiceType, Type ImplementationType)> FindHandlers(IEnumerable<Assembly> assemblies) =>
        (from assembly in assemblies
         from type in GetLoadableTypes(assembly)
         where type.IsClass && !type.IsAbstract && !type.ContainsGenericParameters
         from serviceType in type.GetInterfaces()
         where serviceType.IsGenericType && HandlerInterfaces.Contains(serviceType.GetGenericTypeDefinition())
         select (serviceType, type)).ToList();

    /// <summary>
    /// Returns the types in the assembly. When some types cannot be loaded, continues with the types that can.
    /// </summary>
    private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.OfType<Type>();
        }
    }

    /// <summary>
    /// Checks the request handlers against the existing registrations and returns the ones that are not registered yet.
    /// Throws without registering anything when a request type has more than one distinct handler.
    /// </summary>
    private static List<ServiceDescriptor> GetNewRequestHandlers(
        IServiceCollection services,
        IEnumerable<(Type ServiceType, Type ImplementationType)> requestHandlers)
    {
        var newHandlers = new List<ServiceDescriptor>();
        var conflicts = new List<string>();

        foreach (var group in requestHandlers.GroupBy(handler => handler.ServiceType))
        {
            var existingHandlers = services
                .Where(descriptor => !descriptor.IsKeyedService && descriptor.ServiceType == group.Key)
                .ToList();

            var handlerNames = existingHandlers
                .Select(DescribeImplementation)
                .Concat(group.Select(handler => handler.ImplementationType.FullName!))
                .Distinct()
                .ToList();

            if (handlerNames.Count > 1)
            {
                conflicts.Add(
                    $"Multiple handlers were found for request '{group.Key.GetGenericArguments()[0].FullName}': " +
                    $"{string.Join(", ", handlerNames)}.");
            }
            else if (existingHandlers.Count == 0)
            {
                newHandlers.Add(ServiceDescriptor.Transient(group.Key, group.First().ImplementationType));
            }
        }

        if (conflicts.Count > 0)
        {
            throw new InvalidOperationException(
                string.Join(Environment.NewLine, conflicts) + Environment.NewLine +
                "Each request type can have only one handler.");
        }

        return newHandlers;
    }

    private static string DescribeImplementation(ServiceDescriptor descriptor) =>
        descriptor.ImplementationType?.FullName
        ?? descriptor.ImplementationInstance?.GetType().FullName
        ?? "a handler registered with a factory";
}
