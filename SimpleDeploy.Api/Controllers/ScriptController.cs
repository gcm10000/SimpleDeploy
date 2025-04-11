using Microsoft.AspNetCore.Mvc;
using SimpleDeploy.Application.Entities;
using SimpleDeploy.Application.Services;

namespace SimpleDeploy.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScriptController : ControllerBase
{
    private readonly ScriptService _scriptService;

    public ScriptController(ScriptService scriptService)
    {
        _scriptService = scriptService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var scripts = await _scriptService.GetAllAsync();
        return Ok(scripts);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var script = await _scriptService.GetByIdAsync(id);
        if (script == null) return NotFound();
        return Ok(script);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Script script)
    {
        var created = await _scriptService.CreateAsync(script);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Script script)
    {
        if (id != script.Id) return BadRequest("ID mismatch");

        var success = await _scriptService.UpdateAsync(script);
        return success ? NoContent() : NotFound();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var success = await _scriptService.DeleteAsync(id);
        return success ? NoContent() : NotFound();
    }

    [HttpPost("[action]")]
    public async Task<IActionResult> Execute(Guid id)
    {
        var success = await _scriptService.ExecuteAsync(id);
        return Ok(success);
    }
}
