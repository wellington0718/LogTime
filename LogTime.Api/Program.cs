var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
      .AddJsonOptions(options =>
      {
          options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
      });

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddDbContext<LogTimeDataContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString(nameof(ConnectionStringName.LogTime)));
});

builder.Services.AddRepository();
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

app.UsePathBase("/LogTime");
app.UseAuthorization();

app.MapControllers();

app.Run();
