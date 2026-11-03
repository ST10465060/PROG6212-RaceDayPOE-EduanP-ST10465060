using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Services;   // Part 3 - brings the blob service into scope

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<RaceDayDbContext>(options =>
options.UseSqlServer(builder.Configuration.GetConnectionString("RaceDayDb")));

// Blob storage for event banners and profile pictures. Registered as a
// singleton because BlobContainerClient is thread safe, so building a new
// one on every request would just waste connections. NEW
builder.Services.AddSingleton<IBlobStorageService, BlobStorageService>();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();

var app = builder.Build();

// Seed the development database on startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();
    DbSeeder.Seed(context);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();