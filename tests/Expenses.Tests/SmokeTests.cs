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
        Maybe<int> answer = Maybe.From(42);

        Assert.True(answer.HasValue);
        Assert.Equal(42, answer.GetValueOrThrow());
    }

    [Fact]
    public void ResultIsAvailable()
    {
        Result<string> ok = Result.Success("wired up");
        
        Assert.True(ok.IsSuccess);
        Assert.Equal("wired up", ok.Value);
    }

    [Fact]
    public void FailedResultReportsItsError()
    {
        Result<string> failed = Result.Failure<string>("wired up");

        var outcome = failed.Match(
            onSuccess: value => $"Unexpected success {value}",
            onFailure: e => $"Failure: {e}");
        
        Assert.Equal("Failure: wired up", outcome);
    }
}
