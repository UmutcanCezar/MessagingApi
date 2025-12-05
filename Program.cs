// ... diðer using'ler
using api1.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

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

        Thread.Sleep(5000); // 5 saniye bekle

        dbContext.Database.Migrate(); // Migrasyonlarý uygula
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Veritabaný migrasyonlarý baþarýyla uygulandý.");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Veritabaný migrasyonu sýrasýnda bir hata oluþtu: {Message}", ex.Message); // Hata mesajýný logla
    }
}
// --- OTOMATÝK MÝGRASYON BLOÐU BÝTÝÞÝ ---

app.UseCors("AllowAll");
// ... Uygulamanýn geri kalaný ...