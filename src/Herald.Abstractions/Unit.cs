namespace Herald;

/// <summary>
/// A type that carries no value. Requests that do not return a value are represented by this type inside the pipeline.
/// All <see cref="Unit"/> values are equal.
/// </summary>
public readonly struct Unit : IEquatable<Unit>, IComparable<Unit>
{
    /// <summary>
    /// The single <see cref="Unit"/> value.
    /// </summary>
    public static readonly Unit Value = default;

    /// <summary>
    /// A completed task whose result is <see cref="Value"/>.
    /// </summary>
    public static Task<Unit> Task { get; } = System.Threading.Tasks.Task.FromResult(Value);

    /// <summary>
    /// Compares this value with another <see cref="Unit"/>. Because all values are equal, the result is always 0.
    /// </summary>
    /// <param name="other">The value to compare with.</param>
    /// <returns>Always 0.</returns>
    public int CompareTo(Unit other) => 0;

    /// <summary>
    /// Determines whether this value equals another <see cref="Unit"/>. The result is always <see langword="true"/>.
    /// </summary>
    /// <param name="other">The value to compare with.</param>
    /// <returns>Always <see langword="true"/>.</returns>
    public bool Equals(Unit other) => true;

    /// <summary>
    /// Determines whether the given object is a <see cref="Unit"/>.
    /// </summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns><see langword="true"/> if <paramref name="obj"/> is a <see cref="Unit"/>; otherwise, <see langword="false"/>.</returns>
    public override bool Equals(object? obj) => obj is Unit;

    /// <summary>
    /// Returns the hash code, which is the same for all values.
    /// </summary>
    /// <returns>Always 0.</returns>
    public override int GetHashCode() => 0;

    /// <summary>
    /// Returns the text representation of the value.
    /// </summary>
    /// <returns>Always <c>()</c>.</returns>
    public override string ToString() => "()";

    /// <summary>
    /// Determines whether two <see cref="Unit"/> values are equal. The result is always <see langword="true"/>.
    /// </summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns>Always <see langword="true"/>.</returns>
    public static bool operator ==(Unit left, Unit right) => true;

    /// <summary>
    /// Determines whether two <see cref="Unit"/> values differ. The result is always <see langword="false"/>.
    /// </summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns>Always <see langword="false"/>.</returns>
    public static bool operator !=(Unit left, Unit right) => false;
}
