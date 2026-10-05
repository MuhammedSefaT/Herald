namespace Herald.Tests;

public sealed class UnitTypeTests
{
    [Fact]
    public void AllValues_AreEqual()
    {
        var left = Unit.Value;
        var right = default(Unit);

        var equalByOperator = left == right;
        var differentByOperator = left != right;

        Assert.True(equalByOperator);
        Assert.False(differentByOperator);
        Assert.True(left.Equals(right));
        Assert.True(left.Equals((object)right));
        Assert.False(left.Equals("()"));
        Assert.Equal(0, left.CompareTo(right));
        Assert.Equal(left.GetHashCode(), right.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsEmptyParentheses()
    {
        Assert.Equal("()", Unit.Value.ToString());
    }

    [Fact]
    public async Task Task_IsCompletedWithValue()
    {
        Assert.True(Unit.Task.IsCompletedSuccessfully);
        Assert.Same(Unit.Task, Unit.Task);
        Assert.Equal(Unit.Value, await Unit.Task);
    }
}
