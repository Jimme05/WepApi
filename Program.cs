using Microsoft.EntityFrameworkCore;
using SimpleAuthBasicApi.Data;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
    serverOptions.ListenAnyIP(int.Parse(port));
});
// เชื่อมต่อ MySQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    )
);
// Program.cs
builder.Services.AddHttpClient("ImageOrigin", c =>
{
    c.BaseAddress = new Uri("http://202.28.34.203:30000/");
    c.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod()
    );
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.MapGet("/db-ping", async (AppDbContext db) =>
{
    try
    {
        var can = await db.Database.CanConnectAsync();
        return Results.Ok(new { canConnect = can });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});


app.UseCors("AllowAll");
app.UseStaticFiles();
app.UseSwagger();
app.UseSwaggerUI();
app.UseStaticFiles();
app.MapControllers();
app.MapGet("/", () => "Hello from SimpleAuthControllerApi!");

app.Run();
