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
// Hata Düzeltmesi: ChatAppDbContext sınıfını bulabilmek için root namespace'i ekliyoruz.
using api1;

var builder = WebApplication.CreateBuilder(args);

// --- RENDER'A ÖZEL DÜZELTME 1: DATABASE_URL'İ ÇEVİRME ---
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
            SslMode = SslMode.Require,
            // Npgsql uyarısı nedeniyle TrustServerCertificate kaldırıldı.
            // TrustServerCertificate = true 
        }.ToString();

        // Render ortam değişkenini kullanarak DefaultConnection'ı yapılandır
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
// --- DÜZELTME SONU ---


// --- RENDER'A ÖZEL DÜZELTME 2: PORT DİNLEME AYARI ---
var port = Environment.GetEnvironmentVariable("PORT");
if (port != null)
{
    builder.WebHost.ConfigureKestrel(serverOptions =>
    {
        serverOptions.ListenAnyIP(int.Parse(port));
    });
    Console.WriteLine($"--> Kestrel listening on port {port}");
}
// --- PORT DÜZELTMESİ SONU ---


// --- SERVİS EKLEMELERİ ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(); // Swagger servisi her zaman eklenir
builder.Services.AddSignalR();
// --- BAĞIMLILIK ENJEKSİYONU DÜZELTMESİ ---
builder.Services.AddScoped<api1.repository.IMessageRepository, api1.repository.MessageRepository>();

// CORS politikası
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy",
        policy =>
        {
            policy.WithOrigins(
                "https://chatapp-api-5smg.onrender.com",
                "http://127.0.0.1:5500"
            // Diğer frontend URL'lerinizi buraya ekleyin
            )
            .AllowAnyHeader()
            .WithMethods("GET", "POST")
            .AllowCredentials();
        });
});

// Veritabanı Bağlantısı
// Render ortamında DATABASE_URL kullanıldıysa o bağlantı dizesi kullanılır.
// Aksi takdirde appsettings.json'daki kullanılır.
builder.Services.AddDbContext<ChatAppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// --- OTOMATİK MİGRASYON BLOĞU ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<ChatAppDbContext>();
        // dbContext.Database.EnsureCreated(); // Migration kullanıldığı için bu genellikle gereksizdir.
        dbContext.Database.Migrate();
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Veritabanı migrasyonları başarıyla uygulandı.");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Veritabanı migrasyonu sırasında bir hata oluştu: {Message}", ex.Message);
        // Hata durumunda uygulama çıkışı yapılabilir, bu Render'a hatayı bildirir.
    }
}
// --- OTOMATİK MİGRASYON BLOĞU BİTİŞİ ---

app.UseCors("CorsPolicy");

// Tüm ortamlarda Swagger'ı aktif hale getirdik
// Güvenlik uyarısı: Bu, üretim ortamında API detaylarını ifşa eder.
app.UseSwagger();
app.UseSwaggerUI();


// if (app.Environment.IsDevelopment())
// {
//     app.UseSwagger();
//     app.UseSwaggerUI();
// }


// Render health check için genellikle kaldırılır, ama https zorunluysa kalmalı.
// SignalR için WebSockets'ta sorun çıkarabilir.
// app.UseHttpsRedirection(); 

app.UseAuthorization();

// --- RENDER HEALTH CHECK ENDPOINT'İ (Opsiyonel ama faydalı) ---
app.MapGet("/", () => "API is running successfully!");
// --- HEALTH CHECK SONU ---


// SignalR Hub Haritalaması
app.MapHub<ChatHub>("/chathub");

app.MapControllers();

app.Run();