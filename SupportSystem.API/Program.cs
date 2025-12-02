using Microsoft.EntityFrameworkCore;
using SupportSystem.API.Data;

var builder = WebApplication.CreateBuilder(args);

// Добавляем сервисы в контейнер
builder.Services.AddControllers();

// ДОБАВЛЯЕМ ПОДКЛЮЧЕНИЕ К MYSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString)  // Автоматически определяем версию MySQL
    )
);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Настраиваем конвейер HTTP запросов
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// ТЕСТ ПОДКЛЮЧЕНИЯ К БД
try
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        dbContext.Database.OpenConnection();
        Console.WriteLine("? Подключение к MySQL успешно!");
        dbContext.Database.CloseConnection();
    }
}
catch (Exception ex)
{
    Console.WriteLine($"? Ошибка подключения к MySQL: {ex.Message}");
}

app.Run();