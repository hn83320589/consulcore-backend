using consulcore_backend.Authorization;

namespace ConsulCore.Tests.Unit;

// B3：正式環境若漏設金鑰而沿用 repo 內的 placeholder，任何人都能偽造 token
public class JwtSecretKeyTests
{
    private const string Placeholder = "DEV-ONLY-CHANGE-ME-IN-PRODUCTION-AT-LEAST-32-CHARS!!";
    private static readonly string StrongKey = new('k', 48);

    [Fact]
    public void Placeholder_is_allowed_in_development()
    {
        Assert.Equal(Placeholder, JwtSecretKey.Validate(Placeholder, "Development"));
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData("Testing")]
    public void Placeholder_is_rejected_outside_development(string environment)
    {
        var ex = Assert.Throws<InvalidOperationException>(() => JwtSecretKey.Validate(Placeholder, environment));

        Assert.Contains("Jwt__SecretKey", ex.Message);
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public void Key_shorter_than_256_bits_is_rejected_everywhere(string environment)
    {
        Assert.Throws<InvalidOperationException>(() => JwtSecretKey.Validate(new string('k', 31), environment));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Missing_key_is_rejected(string? key)
    {
        Assert.Throws<InvalidOperationException>(() => JwtSecretKey.Validate(key, "Development"));
    }

    [Fact]
    public void Strong_key_is_accepted_in_production()
    {
        Assert.Equal(StrongKey, JwtSecretKey.Validate(StrongKey, "Production"));
    }
}
