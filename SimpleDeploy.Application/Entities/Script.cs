namespace SimpleDeploy.Application.Entities;

public class Script
{
    public Guid Id { get; private set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string Content { get; set; }

    public Script() 
    {
        Id = Guid.NewGuid();
    }
}
