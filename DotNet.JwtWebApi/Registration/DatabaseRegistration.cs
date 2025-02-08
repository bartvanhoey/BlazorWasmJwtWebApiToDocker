using DotNet.JwtWebApi.Data;
using Microsoft.EntityFrameworkCore;

namespace DotNet.JwtWebApi.Registration;

public static class DatabaseRegistration
{
    public static void SetupDatabase(this WebApplicationBuilder webApplicationBuilder)
    {
        var connectionString = webApplicationBuilder.Configuration.GetConnectionString("DefaultConnection") ??
                               throw new InvalidOperationException("Connection string not found");
        webApplicationBuilder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));
    }

    

}