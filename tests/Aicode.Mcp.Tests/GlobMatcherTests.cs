using Aicode.Mcp.Core.Infrastructure;

namespace Aicode.Mcp.Tests;

public class GlobMatcherTests
{
    [Theory]
    [InlineData("Features/Orders/CreateOrder/CreateOrderHandler.cs", "Features/**/*Handler.cs", true)]
    [InlineData("Features/OrderHandler.cs", "Features/**/*Handler.cs", true)]
    [InlineData("Domain/Order.cs", "Features/**/*Handler.cs", false)]
    [InlineData("src/a/b/page.tsx", "src/**/page.tsx", true)]
    [InlineData("src/page.tsx", "src/**/page.tsx", true)]
    [InlineData("a/b.cs", "*.cs", false)]
    [InlineData("b.cs", "*.cs", true)]
    [InlineData("x.tsx", "*.{ts,tsx}", true)]
    [InlineData("x.js", "*.{ts,tsx}", false)]
    [InlineData("src\\a\\page.tsx", "src/**/page.tsx", true)]
    public void IsMatch_HandlesGlobSyntax(string path, string glob, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(path, glob));
    }

    [Fact]
    public void Matches_NeedsOnePositiveAndNoNegativeGlob()
    {
        var globs = new[] { "Features/**/*Handler.cs", "!Features/**/I*Handler.cs" };

        Assert.True(GlobMatcher.Matches("Features/Orders/CreateOrder/CreateOrderHandler.cs", globs));
        Assert.False(GlobMatcher.Matches("Features/Orders/CreateOrder/ICreateOrderHandler.cs", globs));
    }

    [Fact]
    public void Matches_ReturnsFalseForNullOrEmptyGlobs()
    {
        Assert.False(GlobMatcher.Matches("a.cs", null));
        Assert.False(GlobMatcher.Matches("a.cs", Array.Empty<string>()));
    }
}
