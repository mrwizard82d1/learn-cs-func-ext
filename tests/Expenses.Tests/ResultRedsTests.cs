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
        Assert.Equal($"Not a number: 'twelve'", ex.Error);
    }
    
    // RED #2 - better. Guard first, with the error in the message.
    [Fact]
    public void GuardFirst_GivesAnAssertionRed()
    {
        // var parsed = ParseQuantity("twelve");
        var parsed = ParseQuantity("12");
        
        Assert.True(parsed.IsSuccess, parsed.IsFailure ? parsed.Error : null);
        Assert.Equal(12, parsed.Value);
    }
    
    // RED #3 - best. Assert on the outcome itself; no unwrapping at all
    [Fact]
    public void MatchToAString_GivesADiff()
    {
        // var parsed = ParseQuantity("twelve");
        var parsed = ParseQuantity("12");

        var outcome = parsed.Match(
            onSuccess: n => $"Success: {n}",
            onFailure: e => $"failure: {e}");
        
        Assert.Equal("Success: 12", outcome);
    }
}