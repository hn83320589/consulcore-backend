using consulcore_backend.Authorization;
using consulcore_backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ConsulCore.Tests.Integration;

// R7：seeder 原本「只要有任何權限就整個跳過」，之後新增的權限碼永遠進不了既有資料庫
public class DatabaseSeederTests(ConsulCoreApiFactory factory) : IClassFixture<ConsulCoreApiFactory>
{
    [Fact]
    public async Task Missing_permissions_are_added_without_touching_existing_ones()
    {
        factory.CreateClient(); // 啟動時已執行一次 seed
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var removed = await db.Permissions.SingleAsync(p => p.Code == Permissions.OrgSettings);
        db.Permissions.Remove(removed);
        await db.SaveChangesAsync();
        var idsBefore = await db.Permissions.ToDictionaryAsync(p => p.Code, p => p.Id);

        await DatabaseSeeder.SeedAsync(db);
        await DatabaseSeeder.SeedAsync(db); // 重複執行不產生重複資料

        var after = await db.Permissions.AsNoTracking().ToListAsync();
        Assert.Equal(Permissions.All.OrderBy(c => c), after.Select(p => p.Code).OrderBy(c => c));
        Assert.All(idsBefore, kv => Assert.Equal(kv.Value, after.Single(p => p.Code == kv.Key).Id));
    }
}
