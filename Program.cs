using api1.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql; // Yeni: NpgsqlConnectionStringBuilder için
using System.Threading;

var builder = WebApplication.CreateBuilder(args);

// --- VÝTAL DÜZELTME: RENDER DATABASE_URL'ÝNÝ ÇEVÝRME ---
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
            SslMode = SslMode.Require, // Render için zorunlu
            TrustServerCertificate = true // Güvenlik sertifikasý doðrulamasýný atla
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


// --- DÝÐER SERVÝS EKLEMELERÝ ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

// Örnek CORS politikasý (Önceki düzeltmelerinizden gelen "AllowAll" kullanýlmýþtýr)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder =>
        {
            builder.AllowAnyMethod()
                   .AllowAnyHeader()
                   .AllowCredentials()
                   .WithOrigins("http://localhost:3000", "https://your-frontend-url.onrender.com"); // Frontend Render URL'nizi buraya ekleyin!
        });
});

// Artýk DefaultConnection, Render'ýn ortam deðiþkeni sayesinde doðru adrese iþaret ediyor.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// --- VÝTAL: OTOMATÝK MÝGRASYON BLOÐU (Thread.Sleep Kaldýrýldý) ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<AppDbContext>();

        // Baðlantý adresi doðru olduðu için bekleme süresi artýk gerekli deðil.
        dbContext.Database.Migrate(); // Migrasyonlarý uygula
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Veritabaný migrasyonlarý baþarýyla uygulandý.");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        // Hata mesajý artýk 127.0.0.1 yerine gerçek bir baðlantý hatasýysa görünür.
        logger.LogError(ex, "Veritabaný migrasyonu sýrasýnda bir hata oluþtu: {Message}", ex.Message);
    }
}
// --- OTOMATÝK MÝGRASYON BLOÐU BÝTÝÞÝ ---

app.UseCors("AllowAll");
// ... Uygulamanýn geri kalaný ...

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

// SignalR Hub'ý buraya ekliyoruz (Örneðin: app.MapHub<ChatHub>("/chathub");)

app.MapControllers();

app.Run();