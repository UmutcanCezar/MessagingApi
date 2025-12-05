using api1.Data;
using api1.Hubs;
using api1.repository;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using Microsoft.Extensions.DependencyInjection; // IServiceScopeFactory için gerekli

var builder = WebApplication.CreateBuilder(args);

// CORS ayarý
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        // Render'a daðýtým yaptýðýnýzda, buraya Render URL'nizi de eklemek isteyebilirsiniz.
        policy.WithOrigins("http://127.0.0.1:5500", "http://localhost:5500", "https://*.onrender.com")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();

// VÝTAL DEÐÝÞÝKLÝK: PostgreSQL Baðlantý Dizesi Yapýlandýrmasý
builder.Services.AddDbContext<AppDbContext>(options =>
    // SQL Server yerine UseNpgsql kullanýyoruz.
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// --- VÝTAL: OTOMATÝK MÝGRASYON BLOÐU (Render için zorunlu) ---
// Uygulama baþladýktan hemen sonra, veritabanýna baðlanýp migrasyonlarý uygular.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var dbContext = services.GetRequiredService<AppDbContext>();
        // Eðer veritabaný varsa, tüm bekleyen migrasyonlarý uygula
        dbContext.Database.Migrate();
    }
    catch (Exception ex)
    {
        // Hata durumunda loglama yap (Render loglarýnda görünür)
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Veritabaný migrasyonu sýrasýnda bir hata oluþtu.");
    }
}
// --- OTOMATÝK MÝGRASYON BLOÐU BÝTÝÞÝ ---

app.UseCors("AllowAll");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();
app.MapHub<ChatHub>("/chathub");

app.MapControllers();

app.Run();