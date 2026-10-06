using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Herald;

/// <summary>
/// Configuration for <see cref="ServiceCollectionExtensions.AddHerald"/>: the assemblies to scan,
/// the pipeline behaviors and the lifetime of the <see cref="IHerald"/> registration.
/// </summary>
public sealed class HeraldConfiguration
{
    private readonly List<Assembly> _assemblies = [];
    private readonly List<ServiceDescriptor> _behaviors = [];

    /// <summary>
    /// The lifetime of the <see cref="IHerald"/> registration. <see cref="ISender"/> and <see cref="IPublisher"/>
    /// are forwarded to the <see cref="IHerald"/> registration with the same lifetime. The default is
    /// <see cref="ServiceLifetime.Transient"/>.
    /// </summary>
    public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Transient;

    internal IReadOnlyList<Assembly> Assemblies => _assemblies;

    internal IReadOnlyList<ServiceDescriptor> Behaviors => _behaviors;

    /// <summary>
    /// Adds an assembly to scan for handlers. An assembly that is added more than once is scanned once.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <returns>The same configuration, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="assembly"/> is null.</exception>
    public HeraldConfiguration RegisterServicesFromAssembly(Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        if (!_assemblies.Contains(assembly))
        {
            _assemblies.Add(assembly);
        }

        return this;
    }

    /// <summary>
    /// Adds several assemblies to scan for handlers.
    /// </summary>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <returns>The same configuration, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="assemblies"/> or one of its elements is null.</exception>
    public HeraldConfiguration RegisterServicesFromAssemblies(params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        foreach (var assembly in assemblies)
        {
            RegisterServicesFromAssembly(assembly);
        }

        return this;
    }

    /// <summary>
    /// Adds the assembly that contains <typeparamref name="T"/> to the assemblies to scan.
    /// </summary>
    /// <typeparam name="T">Any type in the assembly to scan.</typeparam>
    /// <returns>The same configuration, for chaining.</returns>
    public HeraldConfiguration RegisterServicesFromAssemblyContaining<T>() =>
        RegisterServicesFromAssembly(typeof(T).Assembly);

    /// <summary>
    /// Adds a closed pipeline behavior. Behaviors run in the order they are added: the first one added is outermost.
    /// </summary>
    /// <typeparam name="TService">The service type, for example <c>IPipelineBehavior&lt;CreateOrder, Guid&gt;</c>.</typeparam>
    /// <typeparam name="TImplementation">The behavior class.</typeparam>
    /// <param name="lifetime">The lifetime of the behavior registration.</param>
    /// <returns>The same configuration, for chaining.</returns>
    /// <exception cref="ArgumentException"><typeparamref name="TService"/> is not an <see cref="IPipelineBehavior{TRequest, TResponse}"/>.</exception>
    public HeraldConfiguration AddBehavior<TService, TImplementation>(ServiceLifetime lifetime = ServiceLifetime.Transient)
        where TService : class
        where TImplementation : class, TService =>
        AddBehavior(typeof(TService), typeof(TImplementation), lifetime);

    /// <summary>
    /// Adds a pipeline behavior. Behaviors run in the order they are added: the first one added is outermost.
    /// </summary>
    /// <param name="serviceType">The service type, for example <c>typeof(IPipelineBehavior&lt;CreateOrder, Guid&gt;)</c>.</param>
    /// <param name="implementationType">The behavior class.</param>
    /// <param name="lifetime">The lifetime of the behavior registration.</param>
    /// <returns>The same configuration, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="serviceType"/> or <paramref name="implementationType"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="serviceType"/> is not an <see cref="IPipelineBehavior{TRequest, TResponse}"/>, or
    /// <paramref name="implementationType"/> does not implement <paramref name="serviceType"/>. For the open generic
    /// <c>IPipelineBehavior&lt;,&gt;</c> service type, the implementation must also be an open generic type.
    /// </exception>
    public HeraldConfiguration AddBehavior(Type serviceType, Type implementationType, ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        ArgumentNullException.ThrowIfNull(implementationType);

        if (!serviceType.IsGenericType || serviceType.GetGenericTypeDefinition() != typeof(IPipelineBehavior<,>))
        {
            throw new ArgumentException(
                $"Type '{serviceType}' is not IPipelineBehavior<TRequest, TResponse>.",
                nameof(serviceType));
        }

        var implementsService = serviceType.IsGenericTypeDefinition
            ? implementationType.IsGenericTypeDefinition && ImplementsPipelineBehavior(implementationType)
            : serviceType.IsAssignableFrom(implementationType);

        if (!implementsService)
        {
            throw new ArgumentException(
                $"Type '{implementationType}' does not implement '{serviceType}'.",
                nameof(implementationType));
        }

        _behaviors.Add(new ServiceDescriptor(serviceType, implementationType, lifetime));
        return this;
    }

    /// <summary>
    /// Adds an open generic pipeline behavior that runs for all requests, for example <c>typeof(LoggingBehavior&lt;,&gt;)</c>.
    /// Behaviors run in the order they are added: the first one added is outermost.
    /// </summary>
    /// <param name="openBehaviorType">An open generic type that implements <see cref="IPipelineBehavior{TRequest, TResponse}"/>.</param>
    /// <param name="lifetime">The lifetime of the behavior registration.</param>
    /// <returns>The same configuration, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="openBehaviorType"/> is null.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="openBehaviorType"/> is not an open generic type or does not implement
    /// <see cref="IPipelineBehavior{TRequest, TResponse}"/>.
    /// </exception>
    public HeraldConfiguration AddOpenBehavior(Type openBehaviorType, ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        ArgumentNullException.ThrowIfNull(openBehaviorType);

        if (!openBehaviorType.IsGenericTypeDefinition)
        {
            throw new ArgumentException(
                $"Type '{openBehaviorType.FullName}' is not an open generic type. Pass the behavior as typeof(LoggingBehavior<,>).",
                nameof(openBehaviorType));
        }

        if (!ImplementsPipelineBehavior(openBehaviorType))
        {
            throw new ArgumentException(
                $"Type '{openBehaviorType.FullName}' does not implement IPipelineBehavior<TRequest, TResponse>.",
                nameof(openBehaviorType));
        }

        _behaviors.Add(new ServiceDescriptor(typeof(IPipelineBehavior<,>), openBehaviorType, lifetime));
        return this;
    }

    private static bool ImplementsPipelineBehavior(Type type) =>
        type.GetInterfaces().Any(implemented =>
            implemented.IsGenericType && implemented.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>));
}
