using API.DI;
using API.Mapping;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddConfig(builder.Configuration);
builder.Services.ConfigureMapster();
builder.Services.AddHttpClient();
builder.Services.AddMyDependencyInjection();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

//app.UseResponseCompression();
app.UseRouting();
app.UseCors("AllowPolicy");
app.UseStaticFiles();
//app.MapHealthChecks("health");

app.Run();
