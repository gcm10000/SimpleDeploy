namespace SimpleDeploy.Application.Entities;

public class Deployment
{
    public Guid Id { get; private set; }
    public string Domain { get; set; } = string.Empty;
    public string GitRepo { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public bool AutoUpdate { get; set; }

    public Deployment()
    {
        Id = Guid.NewGuid();
    }
}