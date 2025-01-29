var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
      .AddJsonOptions(options =>
      {
          options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
      });

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddScoped<ILogHistoryRepository, LogHistoryRepository>();
builder.Services.AddScoped<ILogTimeUnitOfWork, LogTimeUnitOfWork>();
builder.Services.AddScoped<IStatusHistoryRepository, StatusHistoryRepository>();
builder.Services.AddScoped<IActiveLogRepository, ActiveLogRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddDbContext<LogTimeDataContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString(nameof(ConnectionStringName.LogTime)));
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(o =>
    {
        o.WithTitle("LogTime API")
         .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });

}

app.UsePathBase("/logtime-3.0-api");
app.UseAuthorization();
app.MapControllers();

app.Run();
