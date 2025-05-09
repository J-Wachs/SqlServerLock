using Microsoft.EntityFrameworkCore;
using SqlServerLock.Interfaces;
using SqlServerLockDemo.Components;
using SqlServerLockDemo.Utils;
using SqlServerLockDemo.Utils.Interfaces;

namespace SqlServerLockDemo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();



            // Added for SqlServerLock:
            builder.Services.AddScoped<ISqlServerLock, SqlServerLock.SqlServerLock>();
            builder.Services.AddScoped<IDemoLock, DemoLock>();

            builder.Services.AddDbContext<DbContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("Default"));
            });
            // End Added for SqlServerLock.



            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();
            app.UseAntiforgery();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}
