using api1.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Collections.Generic;
using System;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using api1.Hubs;
using Microsoft.AspNetCore.Hosting;
using api1.repository;
using Microsoft.AspNetCore.SignalR;
using api1;

var builder = WebApplication.CreateBuilder(args);

// --- RENDER DATABASE_URL AYARI ---
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (!string.IsNullOrEmpty(databaseUrl))
{
    Console.WriteLine("--> Using DATABASE_URL environment variable for connection string.");
    try
    {
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':');

        var connectionString = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port,
            Database = uri.AbsolutePath.Trim('/'),
            Username = userInfo[0],
            Password = userInfo[1],
            SslMode = SslMode.Require
        }.ToString();

        builder.Configuration.AddInMemoryCollection(new[]
        {
            new KeyValuePair<string, string>("ConnectionStrings:DefaultConnection", connectionString)
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"FATAL: Failed to parse DATABASE_URL: {ex.Message}");
    }
}

// --- RENDER PORT AYARI ---
var port = Environment.GetEnvironmentVariable("PORT");
if (port != null)
{
    builder.WebHost.ConfigureKestrel(serverOptions =>
    {
        serverOptions.ListenAnyIP(int.Parse(port));
    });
    Console.WriteLine($"--> Kestrel listening on port {port}");
}

// --- SERVİSLER ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

builder.Services.AddScoped<IMessageRepository, MessageRepository>();

// --- RENDER + SIGNALR UYUMLU CORS AYARI ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy
            .SetIsOriginAllowed(_ => true)      // Render için zorunlu
            .AllowAnyHeader()
            .AllowAnyMethod()                   // SignalR için şart!!
            .AllowCredentials();
    });
});

// --- DATABASE ---
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// --- OTOMATİK MIGRATION ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<AppDbContext>();
        dbContext.Database.Migrate();

        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Veritabanı migrasyonları başarıyla uygulandı.");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Veritabanı migrasyonu sırasında hata oluştu: {Message}", ex.Message);
    }
}

app.UseCors("CorsPolicy");

// --- Swagger ---
app.UseSwagger();
app.UseSwaggerUI();

// app.UseHttpsRedirection(); // Render + SignalR için kapalı olmalı

app.UseAuthorization();

// --- HEALTH CHECK ---
app.MapGet("/", () => "API is running successfully!");

// --- SIGNALR HUB ROUTE ---
app.MapHub<ChatHub>("/chathub");

// --- CONTROLLERS ---
app.MapControllers();

app.Run();
