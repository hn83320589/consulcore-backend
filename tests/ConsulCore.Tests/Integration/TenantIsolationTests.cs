using System.Net;

namespace ConsulCore.Tests.Integration;

// 鎖住 BR-01 / 多租戶隔離：Global Query Filter 讓別的機構的個案「看不到」
public class TenantIsolationTests(ConsulCoreApiFactory factory) : IClassFixture<ConsulCoreApiFactory>
{
    private readonly ApiTestClient _api = new(factory.CreateClient());

    [Fact]
    public async Task Owner_cannot_read_or_list_another_organizations_client()
    {
        var ownerA = await _api.RegisterOrganizationAsync("A 診所", "iso_owner_a");
        var ownerB = await _api.RegisterOrganizationAsync("B 診所", "iso_owner_b");
        var client = await _api.PostOkAsync(ownerA, "owner", "/api/client-profiles", new
        {
            name = "A 的個案",
            idNumber = "A123456789",
            dateOfBirth = "1995-06-15",
            createAccount = false,
        });
        var clientId = client.GetProperty("id").GetInt32();

        var byId = await _api.SendAsync(HttpMethod.Get, $"/api/client-profiles/{clientId}", ownerB, "owner");
        var list = await ApiTestClient.ReadDataAsync(
            await _api.SendAsync(HttpMethod.Get, "/api/client-profiles", ownerB, "owner"));

        Assert.Equal(HttpStatusCode.NotFound, byId.StatusCode);
        Assert.DoesNotContain(list.EnumerateArray(), c => c.GetProperty("id").GetInt32() == clientId);
    }

    // 建立預約原本不檢查個案，外鍵又不分機構：拿別家機構的個案 id 也能預約成功
    [Fact]
    public async Task Cannot_book_an_appointment_for_another_organizations_client()
    {
        var clinic = await ClinicScenario.CreateAsync(_api, "iso_book");
        var otherOwner = await _api.RegisterOrganizationAsync("別家診所", "iso_book_other");
        var otherClient = await _api.PostOkAsync(otherOwner, "owner", "/api/client-profiles", new { name = "別家個案", createAccount = false });
        var slot = (await clinic.AvailableSlotsAsync(clinic.Monday)).First();

        var book = await _api.SendAsync(HttpMethod.Post, "/api/appointments", clinic.OwnerToken, "owner", new
        {
            clientProfileId = otherClient.GetProperty("id").GetInt32(), scheduleSlotId = slot.GetProperty("id").GetInt32(),
        });

        Assert.Equal(HttpStatusCode.NotFound, book.StatusCode);
        // 只看狀態碼不夠：沒檢查時預約其實已寫入，只是回傳時被多租戶 filter 擋住才變成 404
        Assert.Contains(await clinic.AvailableSlotsAsync(clinic.Monday),
            s => s.GetProperty("id").GetInt32() == slot.GetProperty("id").GetInt32());
    }
}
