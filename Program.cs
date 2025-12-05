using api1.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Collections.Generic; // KeyValuePair için eklendi
using System; // Environment ve Uri için eklendi
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using api1.Hubs; // ChatHub'ınızın bulunduğu namespace
using Microsoft.AspNetCore.Hosting; // ConfigureKestrel için eklendi
using api1.repository; // Bu using sizin repo'nuzda mevcutsa kalmalı
using Microsoft.AspNetCore.SignalR; // Bu using sizin hub'ınızda mevcutsa kalmalı

var builder = WebApplication.CreateBuilder(args);

// --- VİTAL DÜZELTME: RENDER DATABASE_URL'İNİ ÇEVİRME ---
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
            SslMode = SslMode.Require, // Render için zorunlu (SSL kullanılmalı)
            TrustServerCertificate = true // Sertifika doğrulaması
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
// --- DÜZELTME SONU ---


// --- VİTAL DÜZELTME: PORT DİNLEME AYARI (Render için) ---
var port = Environment.GetEnvironmentVariable("PORT");
if (port != null)
{
    // Render ortamında, uygulamanın belirtilen PORT'u dinlemesini sağlar.
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
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
// builder.Services.AddScoped<IMessageRepository, MessageRepository>(); // Eğer bu satırı kaldırdıysanız ekleyin

// CORS politikası
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyMethod()
                   .AllowAnyHeader()
                   .AllowCredentials()
                   // Frontend URL'nizi buraya ekleyin!
                   .WithOrigins("http://localhost:3000", "https://your-frontend-url.onrender.com", "http://127.0.0.1:5500", "http://localhost:5500");
        });
});

// Veritabanı Bağlantısı
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// --- VİTAL: OTOMATİK MİGRASYON BLOĞU ---
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
        logger.LogError(ex, "Veritabanı migrasyonu sırasında bir hata oluştu: {Message}", ex.Message);
        Environment.Exit(1);
    }
}
// --- OTOMATİK MİGRASYON BLOĞU BİTİŞİ ---

app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
// Swagger'ı Production'da kullanmak isterseniz üstteki if bloğunu kaldırabilirsiniz.

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// --- YENİ EKLENEN HEALTH CHECK ENDPOINT'İ ---
// Render'ın uygulamanın canlı olduğunu anlaması için basit bir yanıt döner.
app.MapGet("/", () => "API is running successfully!");
// --- HEALTH CHECK SONU ---

// SignalR Hub Haritalaması
app.MapHub<ChatHub>("/chathub");

app.MapControllers();

app.Run();