using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Herald;

/// <summary>
/// <see cref="ServiceCollectionExtensions.AddHerald"/> yapılandırması: taranacak assembly'ler,
/// pipeline behavior'ları ve <see cref="IHerald"/> kaydının ömrü.
/// </summary>
public sealed class HeraldConfiguration
{
    private readonly List<Assembly> _assemblies = [];
    private readonly List<ServiceDescriptor> _behaviors = [];

    /// <summary>
    /// <see cref="IHerald"/> kaydının ömrü. <see cref="ISender"/> ve <see cref="IPublisher"/> aynı ömürle
    /// <see cref="IHerald"/> kaydına yönlendirilir. Varsayılan değer <see cref="ServiceLifetime.Transient"/>.
    /// </summary>
    public ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Transient;

    internal IReadOnlyList<Assembly> Assemblies => _assemblies;

    internal IReadOnlyList<ServiceDescriptor> Behaviors => _behaviors;

    /// <summary>
    /// Handler'ları taranacak bir assembly ekler. Aynı assembly birden fazla kez eklenirse bir kez taranır.
    /// </summary>
    /// <param name="assembly">Taranacak assembly.</param>
    /// <returns>Zincirleme çağrı için aynı yapılandırma.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="assembly"/> null ise.</exception>
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
    /// Handler'ları taranacak birden fazla assembly ekler.
    /// </summary>
    /// <param name="assemblies">Taranacak assembly'ler.</param>
    /// <returns>Zincirleme çağrı için aynı yapılandırma.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="assemblies"/> veya elemanlarından biri null ise.</exception>
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
    /// <typeparamref name="T"/> tipinin bulunduğu assembly'yi taranacak assembly'lere ekler.
    /// </summary>
    /// <typeparam name="T">Assembly'si taranacak herhangi bir tip.</typeparam>
    /// <returns>Zincirleme çağrı için aynı yapılandırma.</returns>
    public HeraldConfiguration RegisterServicesFromAssemblyContaining<T>() =>
        RegisterServicesFromAssembly(typeof(T).Assembly);

    /// <summary>
    /// Kapalı bir pipeline behavior ekler. Behavior'lar eklenme sırasıyla çalışır: ilk eklenen en dışta.
    /// </summary>
    /// <typeparam name="TService">Servis tipi, ör. <c>IPipelineBehavior&lt;CreateOrder, Guid&gt;</c>.</typeparam>
    /// <typeparam name="TImplementation">Behavior sınıfı.</typeparam>
    /// <param name="lifetime">Behavior kaydının ömrü.</param>
    /// <returns>Zincirleme çağrı için aynı yapılandırma.</returns>
    /// <exception cref="ArgumentException"><typeparamref name="TService"/> bir <see cref="IPipelineBehavior{TRequest, TResponse}"/> değilse.</exception>
    public HeraldConfiguration AddBehavior<TService, TImplementation>(ServiceLifetime lifetime = ServiceLifetime.Transient)
        where TService : class
        where TImplementation : class, TService =>
        AddBehavior(typeof(TService), typeof(TImplementation), lifetime);

    /// <summary>
    /// Bir pipeline behavior ekler. Behavior'lar eklenme sırasıyla çalışır: ilk eklenen en dışta.
    /// </summary>
    /// <param name="serviceType">Servis tipi, ör. <c>typeof(IPipelineBehavior&lt;CreateOrder, Guid&gt;)</c>.</param>
    /// <param name="implementationType">Behavior sınıfı.</param>
    /// <param name="lifetime">Behavior kaydının ömrü.</param>
    /// <returns>Zincirleme çağrı için aynı yapılandırma.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="serviceType"/> veya <paramref name="implementationType"/> null ise.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="serviceType"/> bir <see cref="IPipelineBehavior{TRequest, TResponse}"/> değilse veya
    /// <paramref name="implementationType"/> <paramref name="serviceType"/> tipini uygulamıyorsa. Açık generic
    /// <c>IPipelineBehavior&lt;,&gt;</c> servis tipi için implementasyon da açık generic olmalıdır.
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
    /// Tüm istekler için çalışacak açık generic bir pipeline behavior ekler, ör. <c>typeof(LoggingBehavior&lt;,&gt;)</c>.
    /// Behavior'lar eklenme sırasıyla çalışır: ilk eklenen en dışta.
    /// </summary>
    /// <param name="openBehaviorType"><see cref="IPipelineBehavior{TRequest, TResponse}"/> uygulayan açık generic tip.</param>
    /// <param name="lifetime">Behavior kaydının ömrü.</param>
    /// <returns>Zincirleme çağrı için aynı yapılandırma.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="openBehaviorType"/> null ise.</exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="openBehaviorType"/> açık generic bir tip değilse veya
    /// <see cref="IPipelineBehavior{TRequest, TResponse}"/> uygulamıyorsa.
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
