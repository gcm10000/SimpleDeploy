using Microsoft.Data.Sqlite;
using Quartz;
using SimpleDeploy.Api.Jobs.Utils;

namespace SimpleDeploy.Application.Services;

public class DeployUpdateJob : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        using var db = new SqliteConnection("Data Source=deployments.db");
        db.Open();

        var cmd = db.CreateCommand();
        cmd.CommandText = "SELECT Domain, GitRepo FROM Deployments WHERE AutoUpdate = 1";

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var domain = reader.GetString(0);
            var siteDir = $"/home/deployer/sites/{domain}";

            var pullCmd = $"git -C {siteDir} pull";
            var (exitCode, output) = await ShellHelper.RunShell(pullCmd);
            Console.WriteLine($"[{DateTime.Now}] Atualização {domain}: {output}");

            if (exitCode == 0)
            {
                var buildCmd = $"cd {siteDir} && npm install && npm run build";
                var (_, buildOutput) = await ShellHelper.RunShell(buildCmd);
                Console.WriteLine($"[{DateTime.Now}] Build {domain}: {buildOutput}");
            }
        }
    }
}
