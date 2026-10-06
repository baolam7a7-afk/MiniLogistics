using Microsoft.AspNetCore.SignalR.Client;
using MiniLogistics.Web.Services.Auth;

namespace MiniLogistics.Web.Services.Chat;

public class ChatInboxNotice
{
    public long MessageId { get; set; }
    public long ConversationId { get; set; }
    public long SenderId { get; set; }
    public string SenderName { get; set; } = "";
    public string? SenderAvatarUrl { get; set; }
    public long ReceiverId { get; set; }
    public string Content { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
    public bool FromCustomer { get; set; }
    public string? ShopName { get; set; }
    public string? ProductName { get; set; }
}

public class ChatLiveService : IAsyncDisposable
{
    private readonly TokenStorageService _tokens;
    private readonly string _hubUrl;
    private HubConnection? _connection;
    private Task? _starting;
    private bool _disposed;
    private readonly List<long> _rooms = new();

    public ChatLiveService(TokenStorageService tokens, HttpClient http)
    {
        _tokens = tokens;
        _hubUrl = new Uri(http.BaseAddress!, "hubs/chat").ToString();
    }

    public event Func<ChatInboxNotice, Task>? InboxChanged;
    public event Func<Task>? ConnectionRestored;
    public event Func<Task>? LinkChanged;

    public string LinkLabel { get; private set; } = "";

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
                options.AccessTokenProvider = async () =>
                    await _tokens.GetAccessTokenAsync();
            })
            .WithAutomaticReconnect()
            .Build();

        _connection.On<ChatInboxNotice>("InboxChanged", async notice =>
        {
            if (notice == null)
            {
                return;
            }

            await InvokeAsync(InboxChanged, handler => handler(notice));
        });

        _connection.Reconnecting += error =>
        {
            _ = error;
            return PublishLinkAsync("Đang kết nối lại...");
        };

        _connection.Reconnected += async connectionId =>
        {
            _ = connectionId;
            await RejoinAsync();
            await PublishLinkAsync("Đã kết nối");
            await InvokeAsync(ConnectionRestored, handler => handler());
        };

        _connection.Closed += async error =>
        {
            _ = error;
            if (_disposed)
            {
                return;
            }

            await PublishLinkAsync("Đang kết nối lại...");

            await Task.Delay(1500);
            if (_disposed || _connection.State != HubConnectionState.Disconnected)
            {
                return;
            }

            try
            {
                await _connection.StartAsync();
                await RejoinAsync();
                await PublishLinkAsync("Đã kết nối");
                await InvokeAsync(ConnectionRestored, handler => handler());
            }
            catch
            {
                _ = KeepTryingAsync();
            }
        };

        try
        {
            await _connection.StartAsync();
            await PublishLinkAsync("Đã kết nối");
        }
        catch
        {
            await PublishLinkAsync("Đang kết nối lại...");
            _ = KeepTryingAsync();
        }
    }

    public async Task JoinAsync(long conversationId)
    {
        if (conversationId <= 0)
        {
            return;
        }

        if (!_rooms.Contains(conversationId))
        {
            _rooms.Add(conversationId);
        }

        await InvokeJoinAsync(conversationId);
    }

    private async Task RejoinAsync()
    {
        foreach (var conversationId in _rooms.ToArray())
        {
            await InvokeJoinAsync(conversationId);
        }
    }

    private async Task InvokeJoinAsync(long conversationId)
    {
        if (_connection?.State != HubConnectionState.Connected)
        {
            return;
        }

        try
        {
            await _connection.InvokeAsync("JoinConversation", conversationId);
        }
        catch
        {
            // Hub từ chối nếu không phải thành viên cuộc trò chuyện.
        }
    }

    private async Task PublishLinkAsync(string label)
    {
        LinkLabel = label;
        await InvokeAsync(LinkChanged, handler => handler());
    }

    private async Task KeepTryingAsync()
    {
        while (!_disposed && _connection is { State: HubConnectionState.Disconnected })
        {
            await Task.Delay(2000);
            if (_disposed || _connection?.State != HubConnectionState.Disconnected)
            {
                return;
            }

            try
            {
                await _connection.StartAsync();
                await RejoinAsync();
                await PublishLinkAsync("Đã kết nối");
                await InvokeAsync(ConnectionRestored, handler => handler());
                return;
            }
            catch
            {
                // Thử lại ở vòng sau.
            }
        }
    }

    private static async Task InvokeAsync<THandler>(THandler? handlers, Func<THandler, Task> call)
        where THandler : Delegate
    {
        if (handlers == null)
        {
            return;
        }

        foreach (var handler in handlers.GetInvocationList().Cast<THandler>())
        {
            try
            {
                await call(handler);
            }
            catch
            {
                // Một trang lỗi không được làm rơi kết nối.
            }
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
