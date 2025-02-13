using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using server.AppDataContext;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using server.Models;

namespace server.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ChatDBContext _context;
        private readonly ILogger<ChatHub> _logger;

        private static readonly Dictionary<string, string> _userConnections = new Dictionary<string, string>();


        public ChatHub(ChatDBContext context, ILogger<ChatHub> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SendMessage(string message)
        {
            _logger.LogInformation("new message is received");
            // Get username from JWT token claims
            var username = Context.User?.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                _logger.LogError("User not found in token claims");
                throw new HubException("Unauthorized");
            }

            _logger.LogInformation($"Authenticated user {username} sending message");

            var sender = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (sender == null)
            {
                _logger.LogError($"User {username} not found in database");
                throw new HubException("User not found");
            }

            var newMessage = new Message
            {
                Content = message,
                SenderId = sender.Id,
                Timestamp = DateTime.Now
            };

            try
            {
                _context.Messages.Add(newMessage);
                await _context.SaveChangesAsync();
                await Clients.All.SendAsync("ReceiveMessage", username, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving message from user {username}");
                throw new HubException("Error saving message");
            }
        }

        public async Task GetChatHistory()
        {
            var messages = _context.Messages
                .OrderBy(m => m.Timestamp)
                .Select(m => new { m.Sender.Username, m.Content, m.Timestamp })
                .ToList();

            await Clients.Caller.SendAsync("ReceiveChatHistory", messages);
        }


        public override async Task OnConnectedAsync()
        {
            var username = Context.User?.Identity?.Name;
            if (!string.IsNullOrEmpty(username))
            {
                _userConnections[username] = Context.ConnectionId;
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var username = _userConnections.FirstOrDefault(x => x.Value == Context.ConnectionId).Key;
            if (username != null)
            {
                _userConnections.Remove(username);
            }
            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendPrivateMessage(string receiver, string message)
        {
            var sender = Context?.User?.Identity?.Name;

            if (_userConnections.TryGetValue(receiver, out var receiverConnectionId))
            {
                await Clients.Client(receiverConnectionId).SendAsync("ReceivePrivateMessage", sender, message);
            }

            var newMessage = new Message
            {
                Content = message,
                SenderId = _context.Users.First(u => u.Username == sender).Id,
                ReceiverId = _context.Users.First(u => u.Username == receiver).Id,
                Timestamp = DateTime.UtcNow
            };

            _context.Messages.Add(newMessage);
            _context.SaveChanges();
        }
    }



}