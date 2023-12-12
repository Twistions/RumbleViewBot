using Microsoft.AspNetCore.Mvc;
using RumbleBotApi.Models;
using RumbleLib;
using RumbleViewBot;

namespace RumbleBotApi.Controllers;

[ApiController]
[Route("api/bot")]
public class BotController : ControllerBase
{
    private readonly BotManager _botManager;

    public BotController(BotManager botManager)
    {
        _botManager = botManager;
    }
    
    [HttpPost("task")]
    public async Task<IActionResult> CreateTask([FromBody] StartNewTaskSettings settings)
    {
        var rumbleTask = await _botManager.StartNewTask(settings);

        return CreatedAtAction(nameof(GetTaskStatus), new { id = rumbleTask.Id }, rumbleTask);
    }
    
    [HttpGet("task/{id}")]
    public Task<IActionResult> GetTaskStatus(string id)
    {
        var watchChannelTask = _botManager.GetTask(id);

        if (watchChannelTask == null)
        {
            return Task.FromResult<IActionResult>(NotFound($"RumbleTask with ID '{id}' not found."));
        }

        return Task.FromResult<IActionResult>(Ok(watchChannelTask));
    }
    
    [HttpDelete("task/{id}")]
    public IActionResult StopTask(string id)
    {
        if (_botManager.StopTask(id))
        {
            return Ok($"Task with ID '{id}' was stopped successfully.");
        }
        return NotFound($"Task with ID '{id}' not found.");
    }
    
    [HttpGet("tasks")]
    public IActionResult ListTasks()
    {
        var tasks = _botManager.ListTasks();
        return Ok(tasks);
    }
}
