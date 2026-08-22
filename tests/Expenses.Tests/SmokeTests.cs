namespace Expenses.Tests;

using CSharpFunctionalExtensions;
public class SmokeTests
{
    [Fact]
    public void ArithmeticSmoke()
    {
        Assert.Equal(4, 2 + 2);
    }
}