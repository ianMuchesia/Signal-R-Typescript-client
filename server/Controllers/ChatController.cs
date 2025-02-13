

using Microsoft.AspNetCore.Mvc;
using server.AppDataContext;

namespace server.Controllers
{

    [Route("api/chat")]
    [ApiController]
    public class ChatController : ControllerBase
{
    private readonly ChatDBContext _context;

    public ChatController(ChatDBContext context)
    {
        _context = context;
    }

    [HttpGet("history")]
    public IActionResult GetChatHistory()
    {
        var messages = _context.Messages
            .OrderBy(m => m.Timestamp)
            .Select(m => new { m.Sender.Username, m.Content, m.Timestamp })
            .ToList();

        return Ok(messages);
    }
}
}