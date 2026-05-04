using FinFlow.API.Data;
using FinFlow.API.Hubs;
using FinFlow.API.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<FinFlowDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

// SignalR
builder.Services.AddSignalR();

// CORS for React frontend
builder.Services.AddCors(opts =>
    opts.AddPolicy("Frontend", p =>
        p.WithOrigins(builder.Configuration["SignalR:CorsOrigin"] ?? "http://localhost:3000")
         .AllowAnyHeader()
         .AllowAnyMethod()
         .AllowCredentials()));

// App services
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddHostedService<OutboxPublisher>();

var app = builder.Build();

// Apply migrations on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FinFlowDbContext>();
    db.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("Frontend");
app.UseAuthorization();
app.MapControllers();
app.MapHub<DashboardHub>("/hubs/dashboard");

app.Run();
