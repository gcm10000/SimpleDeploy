using Microsoft.EntityFrameworkCore;
using SimpleDeploy.Application.Entities;
using System.Collections.Generic;

namespace SimpleDeploy.Application.Contexts;

public class DeployDbContext : DbContext
{
    public DeployDbContext(DbContextOptions<DeployDbContext> options) : base(options) { }

    public DbSet<Script> Script { get; set; }
    public DbSet<Deployment> Deployments { get; set; }
}
