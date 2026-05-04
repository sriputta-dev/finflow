using FinFlow.NotificationService;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        services.AddHostedService<PaymentProcessedConsumer>();
    })
    .Build();

await host.RunAsync();
