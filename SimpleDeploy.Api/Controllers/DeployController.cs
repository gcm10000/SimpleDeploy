using Microsoft.AspNetCore.Mvc;
using SimpleDeploy.Application.Contexts;
using System.Diagnostics;

namespace SimpleDeploy.Api.Controllers;

public record DeployRequest(string Domain, string GitRepo, string Email);


[ApiController]
[Route("[controller]")]
public class DeployController : ControllerBase
{
    private readonly ILogger<DeployController> _logger;
    private readonly DeployDbContext _deployDbContext;

    public DeployController(
        ILogger<DeployController> logger, 
        DeployDbContext deployDbContext)
    {
        _logger = logger;
        _deployDbContext = deployDbContext;
    }

    [HttpPost("[action]")]
    public async Task<IActionResult> Test()
    {
        var entity = new Application.Entities.Deployment 
        {
            AutoUpdate = true,
            Domain = "www.contoso.com",
            Email = "gabrielc.machado@hotmail.com",
            GitRepo = "https://github.com/gcm10000/ferro-e-fogo-web",
            Timestamp = DateTime.Now
        };

        await _deployDbContext.Deployments.AddAsync(entity);
        await _deployDbContext.SaveChangesAsync();

        return Ok(entity);
    }


    //[HttpPost("NGINX")]
    //public async Task<IActionResult> DeployToDockerNginxAsync()
    //{
    //    var request = new DeployRequest(
    //        "seusite.com", // Substitua por domínio real que você controla
    //        "https://github.com/gcm10000/ferro-e-fogo-web",
    //        "gabrielc.machado@hotmail.com"
    //    );

    //}


    [HttpPost]
    public async Task<IActionResult> Deploy([FromBody] DeployRequest request)
    {
        _logger.LogInformation("Iniciando deploy para {Domain}", request.Domain);

        var siteDir = $"/home/deployer/sites/{request.Domain}";
        var cloneCmd = System.IO.Directory.Exists(siteDir) ?
            $"git -C {siteDir} pull" :
            $"git clone {request.GitRepo} {siteDir}";

        _logger.LogInformation("Executando: {Command}", cloneCmd);
        var cloneResult = await RunShell(cloneCmd);
        _logger.LogInformation("Resultado: {Output}", cloneResult.Output);

        if (cloneResult.ExitCode != 0)
        {
            _logger.LogError("Erro ao clonar o repositório.");
            return BadRequest($"Erro ao clonar: {cloneResult.Output}");
        }

        var buildCmd = $"cd {siteDir} && npm install && npm run build";
        _logger.LogInformation("Executando build: {Command}", buildCmd);
        var buildResult = await RunShell(buildCmd);
        _logger.LogInformation("Resultado do build: {Output}", buildResult.Output);

        if (buildResult.ExitCode != 0)
        {
            _logger.LogError("Erro no build do projeto.");
            return BadRequest($"Erro no build: {buildResult.Output}");
        }

        var nginxConfigPath = $"/etc/nginx/sites-available/{request.Domain}";
        var nginxConfig = $@"
server {{
    listen 80;
    server_name {request.Domain};

    location / {{
        proxy_pass http://localhost:3{new Random().Next(000, 999)};
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
    }}
}}
";
        _logger.LogInformation("Escrevendo configuração NGINX para {Path}", nginxConfigPath);
        await System.IO.File.WriteAllTextAsync(nginxConfigPath, nginxConfig);
        await RunShell($"ln -sf {nginxConfigPath} /etc/nginx/sites-enabled/");
        await RunShell("nginx -s reload");

        var sslCmd = $"certbot --nginx -d {request.Domain} --non-interactive --agree-tos -m {request.Email}";
        _logger.LogInformation("Executando SSL setup: {Command}", sslCmd);
        var sslResult = await RunShell(sslCmd);
        _logger.LogInformation("Resultado SSL: {Output}", sslResult.Output);

        if (sslResult.ExitCode != 0)
        {
            _logger.LogError("Erro ao configurar SSL.");
            return BadRequest($"Erro ao configurar SSL: {sslResult.Output}");
        }

        //_logger.LogInformation("Salvando registro no banco de dados...");
        //var db = new SqliteConnection("Data Source=deployments.db");
        //db.Open();
        //var cmd = db.CreateCommand();
        //cmd.CommandText = "CREATE TABLE IF NOT EXISTS Deployments (Domain TEXT, GitRepo TEXT, Email TEXT, Timestamp TEXT);";
        //await cmd.ExecuteNonQueryAsync();

        //cmd.CommandText = "INSERT INTO Deployments (Domain, GitRepo, Email, Timestamp) VALUES ($domain, $git, $email, $ts);";
        //cmd.Parameters.AddWithValue("$domain", request.Domain);
        //cmd.Parameters.AddWithValue("$git", request.GitRepo);
        //cmd.Parameters.AddWithValue("$email", request.Email);
        //cmd.Parameters.AddWithValue("$ts", DateTime.UtcNow.ToString("s"));
        //await cmd.ExecuteNonQueryAsync();

        _logger.LogInformation("Deploy finalizado para {Domain}", request.Domain);
        return Ok(new { status = "ok", message = "Site publicado com sucesso." });
    }

    private async Task<(int ExitCode, string Output)> RunShell(string cmd)
    {
        var psi = new ProcessStartInfo("/bin/bash", $"-c \"{cmd}\"")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = Process.Start(psi)!;
        var output = await process.StandardOutput.ReadToEndAsync();
        output += await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        Console.WriteLine($"[OUTPUT] {output}");
        return (process.ExitCode, output);
    }
}