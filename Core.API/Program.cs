using API.Config;
using API.DI;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddConfig(builder.Configuration);
builder.Services.ConfigureMapster();
builder.Services.AddApiServicesAndResilience(builder.Configuration, builder.Environment);
builder.Services.AddMyDependencyInjection();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseResponseCompression();
app.UseCors("AllowPolicy");
app.UseAuthorization();
app.MapControllers();
app.UseStaticFiles();

app.Run();
