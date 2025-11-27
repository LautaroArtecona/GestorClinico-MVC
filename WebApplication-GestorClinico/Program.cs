using Microsoft.EntityFrameworkCore;
using WebApplication_GestorClinico.Context;
using Microsoft.AspNetCore.Identity;

namespace WebApplication_GestorClinico
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var connectionString = builder.Configuration.GetConnectionString("ClinicaDBConnection") ?? throw new InvalidOperationException("Connection string 'ClinicaDBContextConnection' not found.");

            builder.Services.AddDbContext<ClinicaDBContext>(
                options => options.UseSqlServer(connectionString));

            // Configuraci�n de Identity con Roles
            builder.Services.AddDefaultIdentity<IdentityUser>(options => {
                options.SignIn.RequireConfirmedAccount = false;
                // Bajo la seguridad de la contraseña para facilitar las pruebas
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequiredLength = 4; 
            })
            .AddRoles<IdentityRole>() 
            .AddEntityFrameworkStores<ClinicaDBContext>(); 

            
            builder.Services.AddControllersWithViews();

            // Agrega los servicios para las p�ginas de Identity (Login, etc.)
            builder.Services.AddRazorPages();

            // Agregamos la sesion
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30); // Tiempo de vida de la cookie
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseSession();

            // Activa el middleware que lee la cookie de login.
            app.UseAuthentication();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            // Mapea las rutas de las paginas de Identity (ej. /Account/Login)
            app.MapRazorPages();

            app.Run();
        }
    }
}
