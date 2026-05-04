using FinFlow.PaymentService;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((ctx, services) =>
    {
        services.AddHostedService<TransactionConsumer>();
    })
    .Build();

await host.RunAsync();
