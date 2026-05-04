using Microsoft.AspNetCore.SignalR;

namespace FinFlow.API.Hubs;

/// <summary>
/// SignalR hub that pushes real-time transaction events to all connected dashboard clients.
/// Clients connect via WebSocket and receive TransactionCreated and TransactionUpdated events.
/// </summary>
public class DashboardHub : Hub
{
    private readonly ILogger<DashboardHub> _logger;

    public DashboardHub(ILogger<DashboardHub> logger) => _logger = logger;

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Dashboard client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Dashboard client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
