using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace SalesService.E2ETests;

public class OrderFlowE2ETests
{
    private readonly HttpClient _client = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };

    [Fact]
    public async Task CreateOrder_And_WaitForReservation_Result()
    {
        // Arrange - create a unique user
        var username = $"e2e_user_{DateTime.UtcNow.Ticks % 100000}";
        var password = "P@ssw0rd";

        var registerPayload = new { username = username, password = password, email = username + "@example.com" };
        var regResp = await _client.PostAsync("/api/auth/register", new StringContent(JsonSerializer.Serialize(registerPayload), Encoding.UTF8, "application/json"));
        regResp.EnsureSuccessStatusCode();

        // Login
        var loginPayload = new { username = username, password = password };
        var loginResp = await _client.PostAsync("/api/auth/login", new StringContent(JsonSerializer.Serialize(loginPayload), Encoding.UTF8, "application/json"));
        loginResp.EnsureSuccessStatusCode();
        var loginJson = JsonDocument.Parse(await loginResp.Content.ReadAsStringAsync());
        string? token = null;
        foreach (var prop in loginJson.RootElement.EnumerateObject())
        {
            if (string.Equals(prop.Name, "token", StringComparison.OrdinalIgnoreCase))
            {
                token = prop.Value.GetString();
                break;
            }
        }
        token.Should().NotBeNullOrEmpty();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Create order for productId=1 quantity=1
        var orderPayload = new { items = new[] { new { productId = 1, quantity = 1 } } };
        var orderResp = await _client.PostAsync("/api/sales/orders", new StringContent(JsonSerializer.Serialize(orderPayload), Encoding.UTF8, "application/json"));
        orderResp.EnsureSuccessStatusCode();
        var orderJson = JsonDocument.Parse(await orderResp.Content.ReadAsStringAsync());
        int orderId;
        // find order object case-insensitively
        JsonElement? orderElem = null;
        foreach (var prop in orderJson.RootElement.EnumerateObject())
        {
            if (string.Equals(prop.Name, "order", StringComparison.OrdinalIgnoreCase) || string.Equals(prop.Name, "Order", StringComparison.OrdinalIgnoreCase))
            {
                orderElem = prop.Value;
                break;
            }
        }
        if (orderElem == null)
        {
            // try root as the order object itself
            orderElem = orderJson.RootElement;
        }
        orderId = orderElem.Value.GetProperty("id").GetInt32();

        // Poll order until status changes from Pending (timeout 20s)
    var sw = System.Diagnostics.Stopwatch.StartNew();
    string? status = null;
        while (sw.Elapsed < TimeSpan.FromSeconds(20))
        {
            var getResp = await _client.GetAsync($"/api/sales/orders/{orderId}");
            getResp.EnsureSuccessStatusCode();
            var getJson = JsonDocument.Parse(await getResp.Content.ReadAsStringAsync());
            // find Order property case-insensitively
            JsonElement? gotOrder = null;
            foreach (var prop in getJson.RootElement.EnumerateObject())
            {
                if (string.Equals(prop.Name, "order", StringComparison.OrdinalIgnoreCase))
                {
                    gotOrder = prop.Value;
                    break;
                }
            }
            if (gotOrder == null)
            {
                gotOrder = getJson.RootElement;
            }
            status = gotOrder.Value.GetProperty("status").GetString();
            if (!string.Equals(status, "Pending", StringComparison.OrdinalIgnoreCase)) break;
            await Task.Delay(500);
        }

    status.Should().NotBeNull();
    // Accept either Reserved, Confirmed or Cancelled depending on flow
    status.Should().BeOneOf(new[] { "Reserved", "Confirmed", "Cancelled" });
    }
}
