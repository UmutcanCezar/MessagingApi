using api1.Data;
using api1.Hubs;
using api1.repository;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Npgsql; // URI d�n���m� i�in bu import gerekli
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// *** KR�T�K ADIM: RENDER DATABASE_URL URI D�N���M� ***
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (!string.IsNullOrEmpty(databaseUrl))
{
    // DATABASE_URL format� (postgresql://user:pass@host/db) Npgsql'in bekledi�i Host=... format�na d�n��t�r�l�yor.
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
            SslMode = SslMode.Require, // Render i�in SSL gereklidir
            TrustServerCertificate = true
        }.ToString();

        // Ba�lant� dizesini, uygulaman�n kulland��� DefaultConnection anahtar�na at�yoruz.
        builder.Configuration["ConnectionStrings:DefaultConnection"] = connectionString;
    }
    catch (Exception ex)
    {
        // Hata ay�klama i�in konsola yazd�r�yoruz.
        Console.WriteLine($"Error parsing DATABASE_URL: {ex.Message}");
    }
}
// *** URI D�N���M� SONU ***


// CORS ayar�
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

// IUserIdProvider'� ekleyin (ChatHub i�in gereklidir)
// Not: CustomUserIdProvider s�n�f�n�z�n projede tan�ml� oldu�unu varsay�yorum.
// E�er tan�ms�zsa bu sat�r� yorum sat�r� yapman�z gerekebilir, ancak SignalR i�in �nemlidir.
// builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();

// V�TAL DE����KL�K: PostgreSQL Ba�lant� Dizesi Yap�land�rmas�
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// --- V�TAL: OTOMAT�K M�GRASYON BLO�U ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<AppDbContext>();
        dbContext.Database.Migrate(); // Migrasyonlar� uygula
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Veritaban� migrasyonlar� ba�ar�yla uyguland�.");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Veritaban� migrasyonu s�ras�nda bir hata olu�tu.");
    }
}
// --- OTOMAT�K M�GRASYON BLO�U B�T��� ---

app.UseCors("AllowAll");

// Configure the HTTP request pipeline.

// SWAGGER D�ZELTMES�: Production'da da �al��mas� i�in ko�ulsuz kullan�l�yor
app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();
app.MapHub<ChatHub>("/chathub");

app.MapControllers();

app.Run();