using Aicode.Mcp.Core.Infrastructure;

namespace Aicode.Mcp.Tests;

public class CasingTests
{
    [Theory]
    [InlineData("order_table", "OrderTable")]
    [InlineData("orders-list", "OrdersList")]
    [InlineData("OrderTable", "OrderTable")]
    [InlineData("HTTPServer", "HttpServer")]
    public void Pascal_JoinsWords(string input, string expected) => Assert.Equal(expected, Casing.Pascal(input));

    [Fact]
    public void Camel_LowersFirstLetter() => Assert.Equal("createOrder", Casing.Camel("CreateOrder"));

    [Theory]
    [InlineData("OrderTable", "order_table")]
    [InlineData("orders_list", "orders_list")]
    [InlineData("OrdersList", "orders_list")]
    public void Snake_SplitsWords(string input, string expected) => Assert.Equal(expected, Casing.Snake(input));

    [Fact]
    public void Kebab_SplitsWords() => Assert.Equal("create-order", Casing.Kebab("CreateOrder"));
}
