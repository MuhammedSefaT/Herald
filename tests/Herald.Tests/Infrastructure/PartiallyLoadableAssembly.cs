using System.Reflection;

namespace Herald.Tests.Infrastructure;

/// <summary>
/// Bazı tipleri yüklenemeyen bir assembly'yi taklit eder: <see cref="GetTypes"/> çağrısı,
/// yüklenebilen tipleri ve yüklenemeyenlerin yerine null içeren bir <see cref="ReflectionTypeLoadException"/> fırlatır.
/// </summary>
public sealed class PartiallyLoadableAssembly(params Type[] loadableTypes) : Assembly
{
    public override Type[] GetTypes() =>
        throw new ReflectionTypeLoadException(
            [.. loadableTypes, null],
            [new TypeLoadException("Simulated type load failure.")]);
}
