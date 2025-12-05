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
using api1.Hubs; // ChatHub'ýnýzýn bulunduðu namespace

var builder = WebApplication.CreateBuilder(args);

// --- VÝTAL DÜZELTME: RENDER DATABASE_URL'ÝNÝ ÇEVÝRME ---
// Bu blok, Render ortamýnda veritabaný baðlantý dizesini dinamik olarak ayarlar.
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
            SslMode = SslMode.Require, // Render için zorunlu (SSL kullanýlmalý)
            TrustServerCertificate = true // Sertifika doðrulamasý
        }.ToString();

        // Yeni oluþturulan baðlantý dizesini DefaultConnection olarak yapýlandýrmaya ekle
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


// --- SERVÝS EKLEMELERÝ ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

// CORS politikasý
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyMethod()
                   .AllowAnyHeader()
                   .AllowCredentials()
                   // TODO: Burayý KENDÝ CANLI RENDER URL'ÝNÝZ ÝLE DEÐÝÞTÝRMEYÝ UNUTMAYIN!
                   .WithOrigins("http://localhost:3000", "https://your-frontend-url.onrender.com");
        });
});

// Veritabaný Baðlantýsý
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// --- VÝTAL: OTOMATÝK MÝGRASYON BLOÐU ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<AppDbContext>();

        // Bu noktada baðlantý dizesi doðru ayarlanmýþ olmalý.
        dbContext.Database.Migrate(); // Migrasyonlarý uygula
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Veritabaný migrasyonlarý baþarýyla uygulandý.");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Veritabaný migrasyonu sýrasýnda bir hata oluþtu: {Message}", ex.Message);

        // Hata oluþursa uygulamanýn kapanmasý için
        Environment.Exit(1);
    }
}
// --- OTOMATÝK MÝGRASYON BLOÐU BÝTÝÞÝ ---

app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// VÝTAL KONTROL: SignalR Hub Haritalamasý
app.MapHub<ChatHub>("/chathub");

app.MapControllers();

app.Run();