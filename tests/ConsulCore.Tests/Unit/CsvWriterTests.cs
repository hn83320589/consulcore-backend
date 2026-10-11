using consulcore_backend.Services;

namespace ConsulCore.Tests.Unit;

public class CsvWriterTests
{
    [Theory]
    [InlineData("=SUM(A1:A9)", "\"'=SUM(A1:A9)\"")]
    [InlineData("+886912", "\"'+886912\"")]
    [InlineData("-1", "\"'-1\"")]
    [InlineData("@cmd", "\"'@cmd\"")]
    [InlineData("\tx", "\"'\tx\"")]
    public void Values_starting_with_formula_characters_are_neutralised(string value, string expected)
    {
        Assert.Equal(expected, CsvWriter.Field(value));
    }

    [Theory]
    [InlineData("王小明", "\"王小明\"")]
    [InlineData("a,b", "\"a,b\"")]
    [InlineData("說\"好\"", "\"說\"\"好\"\"\"")]
    [InlineData("第一行\n第二行", "\"第一行\n第二行\"")]
    public void Plain_values_are_quoted_and_escaped(string value, string expected)
    {
        Assert.Equal(expected, CsvWriter.Field(value));
    }

    [Fact]
    public void Null_becomes_empty_field()
    {
        Assert.Equal("\"\"", CsvWriter.Field(null));
    }

    [Fact]
    public void Row_joins_escaped_fields_with_commas()
    {
        Assert.Equal("\"a\",\"1200\",\"'=1+1\"", CsvWriter.Row("a", 1200, "=1+1"));
    }
}
