using FoodOrder.Data;
using Microsoft.EntityFrameworkCore;

namespace FoodOrder.Services;

public class ExpireOrdersWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ExpireOrdersWorker> _logger;

    public ExpireOrdersWorker(IServiceProvider services, ILogger<ExpireOrdersWorker> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<FoodOrderContext>();
                await Helpers.SqlProc.ExecuteAsync(db, "dbo.sp_ExpirePendingOrders");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Không chạy được sp_ExpirePendingOrders.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
