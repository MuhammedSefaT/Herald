using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Herald;

/// <summary>
/// Herald servislerini <see cref="IServiceCollection"/> üzerine kaydeden extension metotları.
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
    /// Herald'ı kaydeder: verilen assembly'lerdeki handler'ları tarar, yapılandırmadaki behavior'ları ekler ve
    /// <see cref="IMediator"/>, <see cref="ISender"/>, <see cref="IPublisher"/> servislerini kaydeder.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Abstract ve açık generic olmayan, <see cref="IRequestHandler{TRequest, TResponse}"/>,
    /// <see cref="IRequestHandler{TRequest}"/> veya <see cref="INotificationHandler{TNotification}"/> uygulayan
    /// tüm sınıflar Transient olarak kaydedilir. Bir sınıf birden fazla handler arayüzü uyguluyorsa hepsi kaydedilir.
    /// </para>
    /// <para>
    /// Metot birden fazla kez çağrılabilir; daha önce kaydedilmiş handler, behavior ve mediator kayıtları tekrar eklenmez.
    /// </para>
    /// </remarks>
    /// <param name="services">Servis koleksiyonu.</param>
    /// <param name="configuration">Herald yapılandırması.</param>
    /// <returns>Zincirleme çağrı için aynı servis koleksiyonu.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> veya <paramref name="configuration"/> null ise.</exception>
    /// <exception cref="ArgumentException">Yapılandırmada hiç assembly kaydedilmemişse.</exception>
    /// <exception cref="InvalidOperationException">Aynı istek tipi için birden fazla handler bulunursa.</exception>
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

        services.TryAdd(new ServiceDescriptor(typeof(IMediator), typeof(Mediator), heraldConfiguration.Lifetime));
        services.TryAdd(new ServiceDescriptor(typeof(ISender), static provider => provider.GetRequiredService<IMediator>(), heraldConfiguration.Lifetime));
        services.TryAdd(new ServiceDescriptor(typeof(IPublisher), static provider => provider.GetRequiredService<IMediator>(), heraldConfiguration.Lifetime));

        return services;
    }

    private static List<(Type ServiceType, Type ImplementationType)> FindHandlers(IEnumerable<Assembly> assemblies) =>
        (from assembly in assemblies
         from type in assembly.GetTypes()
         where type.IsClass && !type.IsAbstract && !type.ContainsGenericParameters
         from serviceType in type.GetInterfaces()
         where serviceType.IsGenericType && HandlerInterfaces.Contains(serviceType.GetGenericTypeDefinition())
         select (serviceType, type)).ToList();

    /// <summary>
    /// Request handler'larını mevcut kayıtlarla birlikte kontrol eder ve henüz kaydedilmemiş olanları döndürür.
    /// Aynı istek tipi için birden fazla farklı handler varsa hiçbir kayıt yapmadan exception fırlatır.
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
