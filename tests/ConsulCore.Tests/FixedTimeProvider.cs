namespace ConsulCore.Tests;

/// <summary>固定時間的 TimeProvider（.NET 內建的抽象類別，不需額外套件）。</summary>
public sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;
}
