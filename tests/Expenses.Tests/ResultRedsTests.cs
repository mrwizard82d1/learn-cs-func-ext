namespace Expenses.Tests;

using CSharpFunctionalExtensions;

public class ResultRedsTests
{
    private static Result<int> ParseQuantity(string input) =>
        int.TryParse(input, out var n)
            ? Result.Success(n)
            : Result.Failure<int>($"Not a number: '{input}'");
    
    // RED #1 - deliberately bad. Reads `.Value` as the primary assertion.
    [Fact]
    public void ValueAsAssertion_Throws()
    {
        var parsed = ParseQuantity("twelve");

        var ex = Assert.Throws<ResultFailureException>(() => parsed.Value);
        Assert.Contains("Not a number", ex.Message);
    }
    
    // RED #2 - better. Guard first, with the error in the message.
    [Fact]
    public void GuardFirst_GivesAnAssertionRed()
    {
        var parsed = ParseQuantity("twelve");
        
        Assert.True(parsed.IsSuccess, parsed.IsFailure ? parsed.Error : null);
        Assert.Equal(12, parsed.Value);
    }
    
    // RED #3 - best. Assert on the outcome itself; no unwrapping at all
    [Fact]
    public void MatchToAString_GivesADiff()
    {
        var parsed = ParseQuantity("twelve");

        var outcome = parsed.Match(
            onSuccess: n => $"Success: {n}",
            onFailure: e => $"failure: {e}");
        
        Assert.Equal("Success: 12", outcome);
    }
}