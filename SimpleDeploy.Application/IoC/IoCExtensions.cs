using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SimpleDeploy.Application.Contexts;

namespace SimpleDeploy.Application.IoC;

public static class IoCExtensions
{
    public static IServiceCollection ConfigureApplication(this IServiceCollection services)
    {
        services.AddDbContext<DeployDbContext>(options =>
        options.UseSqlite("Data Source=deployments.db"));


        return services;
    }
}
