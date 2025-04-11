namespace SimpleDeploy.Api.Jobs.Utils.Models;

public record DeployRequest(string Domain, string GitRepo, string Email, bool AutoUpdate);
