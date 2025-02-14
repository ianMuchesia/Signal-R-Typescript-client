

using Microsoft.AspNetCore.Mvc;
using server.AppDataContext;
using Microsoft.EntityFrameworkCore;
using server.Contracts;
using server.Models;
using Microsoft.AspNetCore.Authorization;

namespace server.Controllers
{
    [Authorize]
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

    // [HttpGet("history/{receiver}")]
    // public async Task<IActionResult> GetChatHistory(string receiver)
    // {
    //     var currentUser = User.Identity?.Name;
    //     if (string.IsNullOrEmpty(currentUser))
    //     {
    //         return Unauthorized();
    //     }

    //     var messages = await _context.Messages
    //         .Where(m => 
    //             (m.Sender.Username == currentUser && m.Receiver.Username == receiver) ||
    //             (m.Sender.Username == receiver && m.Receiver.Username == currentUser))
    //         .OrderByDescending(m => m.Timestamp)
    //         .Take(50) // Get last 50 messages
    //         .Select(m => new 
    //         {
    //             m.Id,
    //             m.Content,
    //             m.Timestamp,
    //             SenderUsername = m.Sender.Username,
    //             ReceiverUsername = m.Receiver.Username
    //         })
    //         .ToListAsync();

    //     return Ok(messages);
    // }

     [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations()
    {
        var username = User.Identity?.Name;
        var user = await _context.Users.FirstAsync(u => u.Username == username);

        var conversations = await _context.Conversations
            .Include(c => c.User1)
            .Include(c => c.User2)
            .Where(c => c.User1Id == user.Id || c.User2Id == user.Id)
            .Select(c => new
            {
                Id = c.Id,
                OtherUser = c.User1Id == user.Id ? 
                    new { c.User2.Username } : 
                    new { c.User1.Username }
            })
            .ToListAsync();

        return Ok(conversations);
    }

    [HttpGet("conversations/{id}/messages")]
    public async Task<IActionResult> GetMessages(int id)
    {
        var username = User.Identity?.Name;
        var user = await _context.Users.FirstAsync(u => u.Username == username);

        // Verify user is part of conversation
        var conversation = await _context.Conversations
            .FirstOrDefaultAsync(c => c.Id == id && 
                (c.User1Id == user.Id || c.User2Id == user.Id));

        if (conversation == null)
        {
            return NotFound();
        }

        var messages = await _context.Messages
            .Where(m => m.ConversationId == id)
            .OrderByDescending(m => m.Timestamp)
            .Take(50)
            .Select(m => new
            {
                m.Id,
                m.Content,
                m.Timestamp,
                SenderUsername = m.Sender.Username
            })
            .ToListAsync();

        return Ok(messages);
    }

  [HttpPost("conversations")]
public async Task<IActionResult> CreateConversation([FromBody] CreateConversationDto dto)
{
    if (string.IsNullOrEmpty(dto.OtherUsername))
    {
        return BadRequest("Username cannot be empty");
    }

    var username = User.Identity?.Name;
    if (string.IsNullOrEmpty(username))
    {
        return Unauthorized();
    }

    var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
    if (user == null)
    {
        return NotFound("Current user not found");
    }

    var otherUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == dto.OtherUsername);
    if (otherUser == null)
    {
        return NotFound($"User {dto.OtherUsername} not found");
    }

    // Check if conversation already exists
    var existingConversation = await _context.Conversations
        .FirstOrDefaultAsync(c => 
            (c.User1Id == user.Id && c.User2Id == otherUser.Id) ||
            (c.User1Id == otherUser.Id && c.User2Id == user.Id));

    if (existingConversation != null)
    {
        return Ok(new { id = existingConversation.Id });
    }

    // Create new conversation
    var conversation = new Conversation
    {
        User1Id = user.Id,
        User2Id = otherUser.Id
    };

    _context.Conversations.Add(conversation);
    await _context.SaveChangesAsync();

    return Ok(new { id = conversation.Id });
}
}
}