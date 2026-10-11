using System.Net;

namespace ConsulCore.Tests.Integration;

// B10：標記已讀原本不檢查收件人，任何人都能把別人的通知標成已讀（IDOR）
public class NotificationOwnershipTests(ConsulCoreApiFactory factory) : IClassFixture<ConsulCoreApiFactory>
{
    private readonly ApiTestClient _api = new(factory.CreateClient());

    private async Task<int> UnreadCountAsync(string token) =>
        (await ApiTestClient.ReadDataAsync(await _api.SendAsync(HttpMethod.Get, "/api/notifications/unread-count", token, "therapist")))
            .GetProperty("unreadCount").GetInt32();

    [Fact]
    public async Task User_cannot_mark_someone_elses_notification_as_read()
    {
        var clinic = await ClinicScenario.CreateAsync(_api, "noti1");
        await _api.CreateUserAsync(clinic.OwnerToken, "noti1_other", "therapist");
        var recipient = await _api.LoginTokenAsync("noti1_ther", "Pass123!");
        var other = await _api.LoginTokenAsync("noti1_other", "Pass123!");
        var slot = (await clinic.AvailableSlotsAsync(clinic.Monday)).First();
        await clinic.BookAsync(slot.GetProperty("id").GetInt32()); // 通知指派的心理師
        var notifications = await ApiTestClient.ReadDataAsync(
            await _api.SendAsync(HttpMethod.Get, "/api/notifications", recipient, "therapist"));
        var notificationId = notifications.EnumerateArray().First().GetProperty("id").GetInt32();
        var before = await UnreadCountAsync(recipient);

        var attempt = await _api.SendAsync(HttpMethod.Post, $"/api/notifications/{notificationId}/mark-read", other, "therapist");

        Assert.Equal(HttpStatusCode.NotFound, attempt.StatusCode);
        Assert.Equal(before, await UnreadCountAsync(recipient));
    }

    [Fact]
    public async Task Recipient_can_mark_their_own_notification_as_read()
    {
        var clinic = await ClinicScenario.CreateAsync(_api, "noti2");
        var recipient = await _api.LoginTokenAsync("noti2_ther", "Pass123!");
        var slot = (await clinic.AvailableSlotsAsync(clinic.Monday)).First();
        await clinic.BookAsync(slot.GetProperty("id").GetInt32());
        var notifications = await ApiTestClient.ReadDataAsync(
            await _api.SendAsync(HttpMethod.Get, "/api/notifications", recipient, "therapist"));
        var notificationId = notifications.EnumerateArray().First().GetProperty("id").GetInt32();
        var before = await UnreadCountAsync(recipient);

        var response = await _api.SendAsync(HttpMethod.Post, $"/api/notifications/{notificationId}/mark-read", recipient, "therapist");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(before - 1, await UnreadCountAsync(recipient));
    }
}
