namespace Herald;

/// <summary>
/// Değer taşımayan dönüş tipi. Dönüş değeri olmayan istekler pipeline içinde bu tiple temsil edilir.
/// Tüm <see cref="Unit"/> değerleri birbirine eşittir.
/// </summary>
public readonly struct Unit : IEquatable<Unit>, IComparable<Unit>
{
    /// <summary>
    /// Tek <see cref="Unit"/> değeri.
    /// </summary>
    public static readonly Unit Value = default;

    /// <summary>
    /// <see cref="Value"/> sonucuyla tamamlanmış task.
    /// </summary>
    public static Task<Unit> Task { get; } = System.Threading.Tasks.Task.FromResult(Value);

    /// <summary>
    /// Bu değeri başka bir <see cref="Unit"/> ile karşılaştırır; tüm değerler eşit olduğu için sonuç her zaman 0'dır.
    /// </summary>
    /// <param name="other">Karşılaştırılacak değer.</param>
    /// <returns>Her zaman 0.</returns>
    public int CompareTo(Unit other) => 0;

    /// <summary>
    /// Bu değerin başka bir <see cref="Unit"/> ile eşitliğini kontrol eder; sonuç her zaman <see langword="true"/> olur.
    /// </summary>
    /// <param name="other">Karşılaştırılacak değer.</param>
    /// <returns>Her zaman <see langword="true"/>.</returns>
    public bool Equals(Unit other) => true;

    /// <summary>
    /// Verilen nesnenin bir <see cref="Unit"/> olup olmadığını kontrol eder.
    /// </summary>
    /// <param name="obj">Karşılaştırılacak nesne.</param>
    /// <returns><paramref name="obj"/> bir <see cref="Unit"/> ise <see langword="true"/>, aksi halde <see langword="false"/>.</returns>
    public override bool Equals(object? obj) => obj is Unit;

    /// <summary>
    /// Hash kodunu döndürür; tüm değerler için aynıdır.
    /// </summary>
    /// <returns>Her zaman 0.</returns>
    public override int GetHashCode() => 0;

    /// <summary>
    /// Değerin metin karşılığını döndürür.
    /// </summary>
    /// <returns>Her zaman <c>()</c>.</returns>
    public override string ToString() => "()";

    /// <summary>
    /// İki <see cref="Unit"/> değerinin eşit olup olmadığını döndürür; sonuç her zaman <see langword="true"/> olur.
    /// </summary>
    /// <param name="left">Sol değer.</param>
    /// <param name="right">Sağ değer.</param>
    /// <returns>Her zaman <see langword="true"/>.</returns>
    public static bool operator ==(Unit left, Unit right) => true;

    /// <summary>
    /// İki <see cref="Unit"/> değerinin farklı olup olmadığını döndürür; sonuç her zaman <see langword="false"/> olur.
    /// </summary>
    /// <param name="left">Sol değer.</param>
    /// <param name="right">Sağ değer.</param>
    /// <returns>Her zaman <see langword="false"/>.</returns>
    public static bool operator !=(Unit left, Unit right) => false;
}
