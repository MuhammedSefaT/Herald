using System.Reflection;

namespace Herald.Tests.Infrastructure;

/// <summary>
/// Simulates an assembly in which some types cannot be loaded: <see cref="GetTypes"/> throws a
/// <see cref="ReflectionTypeLoadException"/> that contains the loadable types and null for the types that failed to load.
/// </summary>
public sealed class PartiallyLoadableAssembly(params Type[] loadableTypes) : Assembly
{
    public override Type[] GetTypes() =>
        throw new ReflectionTypeLoadException(
            [.. loadableTypes, null],
            [new TypeLoadException("Simulated type load failure.")]);
}
