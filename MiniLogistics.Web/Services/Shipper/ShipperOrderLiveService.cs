using Microsoft.AspNetCore.SignalR.Client;
using MiniLogistics.Web.Services.Auth;

namespace MiniLogistics.Web.Services.Shipper;

public class OrderClaimedNotice
{
    public long OrderId { get; set; }
    public long ShipperUserId { get; set; }
}

public class ShipperOrderLiveService : IAsyncDisposable
{
    private readonly TokenStorageService _tokens;
    private readonly string _hubUrl;
    private HubConnection? _connection;
    private Task? _starting;
    private bool _disposed;

    public ShipperOrderLiveService(TokenStorageService tokens, HttpClient http)
    {
        _tokens = tokens;
        _hubUrl = new Uri(http.BaseAddress!, "hubs/chat").ToString();
    }

    public event Func<OrderClaimedNotice, Task>? OrderClaimed;

    public Task EnsureConnectedAsync()
    {
        if (_connection?.State == HubConnectionState.Connected)
        {
            return Task.CompletedTask;
        }

        if (_starting is { IsCompleted: false })
        {
            return _starting;
        }

        _starting = ConnectAsync();
        return _starting;
    }

    private async Task ConnectAsync()
    {
        var token = await _tokens.GetAccessTokenAsync();
        if (string.IsNullOrWhiteSpace(token) || _disposed)
        {
            return;
        }

        if (_connection != null)
        {
            await _connection.DisposeAsync();
        }

        _connection = new HubConnectionBuilder()
            .WithUrl(_hubUrl, options =>
            {
                options.AccessTokenProvider = async () => await _tokens.GetAccessTokenAsync();
            })
            .WithAutomaticReconnect()
            .Build();

        _connection.On<OrderClaimedNotice>("OrderClaimed", async notice =>
        {
            if (notice == null || OrderClaimed == null)
            {
                return;
            }

            var handlers = OrderClaimed.GetInvocationList().Cast<Func<OrderClaimedNotice, Task>>();
            foreach (var handler in handlers)
            {
                await handler(notice);
            }
        });

        try
        {
            await _connection.StartAsync();
        }
        catch
        {
            // Danh sách vẫn tải lại khi mở trang nếu hub chưa nối được.
        }
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        if (_connection != null)
        {
            await _connection.DisposeAsync();
        }
    }
}
