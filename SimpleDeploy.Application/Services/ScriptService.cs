using Microsoft.EntityFrameworkCore;
using SimpleDeploy.Application.Contexts;
using SimpleDeploy.Application.Entities;
using System.Diagnostics;
using CliWrap;
using CliWrap.Buffered;
using Microsoft.Extensions.Logging;


namespace SimpleDeploy.Application.Services;

public class ScriptService
{
    private readonly DeployDbContext _deployDbContext;
    private readonly ILogger<ScriptService> _logger;

    public ScriptService(DeployDbContext deployDbContext, ILogger<ScriptService> logger)
    {
        _deployDbContext = deployDbContext;
        _logger = logger;
    }


    public async Task<List<Script>> GetAllAsync()
    {
        return await _deployDbContext.Script.ToListAsync();
    }

    public async Task<Script?> GetByIdAsync(Guid id)
    {
        return await _deployDbContext.Script.FindAsync(id);
    }

    public async Task<Script> CreateAsync(Script script)
    {
        _deployDbContext.Script.Add(script);
        await _deployDbContext.SaveChangesAsync();
        return script;
    }

    public async Task<bool> UpdateAsync(Script updatedScript)
    {
        var existing = await _deployDbContext.Script.FindAsync(updatedScript.Id);
        if (existing == null) return false;

        existing.Name = updatedScript.Name;
        existing.Description = updatedScript.Description;
        existing.Content = updatedScript.Content;

        await _deployDbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var existing = await _deployDbContext.Script.FindAsync(id);
        if (existing == null) return false;

        _deployDbContext.Script.Remove(existing);
        await _deployDbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExecuteAsync(Guid id)
    {
        _logger.LogInformation("Iniciando execução do script com ID: {ScriptId}", id);

        var existing = await _deployDbContext.Script.FindAsync(id);
        if (existing == null)
        {
            _logger.LogWarning("Script com ID {ScriptId} não encontrado.", id);
            return false;
        }

        _logger.LogInformation("Script encontrado: {ScriptName}", existing.Name);

        var vpsIp = Environment.GetEnvironmentVariable("REMOTE_VPS_IP");
        if (string.IsNullOrWhiteSpace(vpsIp))
        {
            _logger.LogError("Variável de ambiente REMOTE_VPS_IP não está definida.");
            return false;
        }

        var tempFile = Path.Combine(Path.GetTempPath(), $"script-{Guid.NewGuid()}.sh");
        _logger.LogInformation("Criando arquivo temporário: {TempFile}", tempFile);

        await File.WriteAllTextAsync(tempFile, existing.Content);
        _logger.LogInformation("Script escrito no arquivo temporário.");

        var command = $"ssh -o StrictHostKeyChecking=no root@{vpsIp} 'bash -s' < \"{tempFile}\"";
        _logger.LogInformation("Executando comando SSH: {Command}", command);

        try
        {
            var result = await Cli.Wrap("/bin/bash")
                .WithArguments(["-c", command])
                .ExecuteBufferedAsync();

            _logger.LogInformation("Execução concluída. STDOUT: {Stdout}", result.StandardOutput);
            if (!string.IsNullOrWhiteSpace(result.StandardError))
                _logger.LogWarning("STDERR: {Stderr}", result.StandardError);

            if (result.ExitCode != 0)
            {
                _logger.LogError("Script retornou código de erro: {ExitCode}", result.ExitCode);
                return false;
            }

            _logger.LogInformation("Script executado com sucesso.");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao executar script.");
            return false;
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
                _logger.LogInformation("Arquivo temporário deletado: {TempFile}", tempFile);
            }
        }
    }


}
