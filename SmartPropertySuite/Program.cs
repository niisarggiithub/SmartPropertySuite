using SmartPropertySuite.ApplicationDbContext;
using Microsoft.EntityFrameworkCore;
using SmartPropertySuite.Services;
using StackExchange.Redis;
using SmartPropertySuite.IServices;

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    // Add services to the container.
    builder.Services.AddControllers();

    builder.Services.AddScoped<IGoogleCalendarFactory, GoogleCalendarFactory>();
    builder.Services.AddScoped<ITokenService, TokenService>();
    builder.Services.AddScoped<IChatService, ChatService>();
    builder.Services.AddScoped<IGoogleCalendarService, GoogleCalendarService>();
    builder.Services.AddScoped<IChatDetails, ChatDetails>();
    builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")));
    builder.Services.AddScoped<IRedisService, RedisService>();

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.WithOrigins("http://localhost:54321", "https://hackathon-2025-baroda.azurewebsites.net")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
    });

    var app = builder.Build();

    // Enable serving static files from wwwroot
    app.UseDefaultFiles();  // Looks for index.html, default.html, etc.
    app.UseStaticFiles();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseCors("AllowAll");

    app.UseHttpsRedirection();

    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    throw new Exception(ex.Message, ex);
}

