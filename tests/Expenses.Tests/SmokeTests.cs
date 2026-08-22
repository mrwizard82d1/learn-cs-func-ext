namespace Expenses.Tests;

using CSharpFunctionalExtensions;
public class SmokeTests
{
    [Fact]
    public void ArithmeticSmoke()
    {
        Assert.Equal(4, 2 + 2);
    }

    [Fact]
    public void MaybeIsAvailable()
    {
        var answer = Maybe.From(42);

        Assert.True(answer.HasValue);
        Assert.Equal(42, answer.GetValueOrThrow());
    }

    [Fact]
    public void ResultIsAvailable()
    {
        var ok = Result.Success("wired up");
        
        Assert.True(ok.IsSuccess);
        Assert.Equal("wired up", ok.Value);
    }
}
