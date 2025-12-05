using api1.Data;
using api1.Hubs;
using api1.repository;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Npgsql; // URI dönüþümü için bu import gerekli
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// *** KRÝTÝK ADIM: RENDER DATABASE_URL URI DÖNÜÞÜMÜ ***
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (!string.IsNullOrEmpty(databaseUrl))
{
    // DATABASE_URL formatý (postgresql://user:pass@host/db) Npgsql'in beklediði Host=... formatýna dönüþtürülüyor.
    try
    {
        var uri = new Uri(databaseUrl);
        var userInfo = uri.UserInfo.Split(':');

        var connectionString = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port,
            Username = userInfo[0],
            Password = userInfo[1],
            Database = uri.AbsolutePath.TrimStart('/'),
            SslMode = SslMode.Require, // Render için SSL gereklidir
            TrustServerCertificate = true
        }.ToString();

        // Baðlantý dizesini, uygulamanýn kullandýðý DefaultConnection anahtarýna atýyoruz.
        builder.Configuration["ConnectionStrings:DefaultConnection"] = connectionString;
    }
    catch (Exception ex)
    {
        // Hata ayýklama için konsola yazdýrýyoruz.
        Console.WriteLine($"Error parsing DATABASE_URL: {ex.Message}");
    }
}
// *** URI DÖNÜÞÜMÜ SONU ***


// CORS ayarý
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins("http://127.0.0.1:5500", "http://localhost:5500", "https://*.onrender.com")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// IUserIdProvider'ý ekleyin (ChatHub için gereklidir)
// Not: CustomUserIdProvider sýnýfýnýzýn projede tanýmlý olduðunu varsayýyorum.
// Eðer tanýmsýzsa bu satýrý yorum satýrý yapmanýz gerekebilir, ancak SignalR için önemlidir.
// builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();

// VÝTAL DEÐÝÞÝKLÝK: PostgreSQL Baðlantý Dizesi Yapýlandýrmasý
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
        dbContext.Database.Migrate(); // Migrasyonlarý uygula
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Veritabaný migrasyonlarý baþarýyla uygulandý.");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Veritabaný migrasyonu sýrasýnda bir hata oluþtu.");
    }
}
// --- OTOMATÝK MÝGRASYON BLOÐU BÝTÝÞÝ ---

app.UseCors("AllowAll");

// Configure the HTTP request pipeline.

// SWAGGER DÜZELTMESÝ: Production'da da çalýþmasý için koþulsuz kullanýlýyor
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();
app.MapHub<ChatHub>("/chathub");

app.MapControllers();

app.Run();