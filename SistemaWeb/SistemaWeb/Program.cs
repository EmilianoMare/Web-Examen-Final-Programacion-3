using Microsoft.Data.SqlClient;
using SistemaWeb.Data;
using SistemaWeb.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// De esta manera ya podemos trabajar con esa clase dentro de todo el proyecto
builder.Services.AddScoped<SqlConnectionFactory>();
builder.Services.AddScoped<SaleRepository>();
builder.Services.AddScoped<SaleServices>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
