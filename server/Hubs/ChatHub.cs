using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using server.AppDataContext;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using server.Models;
using System.Collections.Concurrent;

namespace server.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ChatDBContext _context;
        private readonly ILogger<ChatHub> _logger;

        private static readonly Dictionary<string, string> _userConnections = new Dictionary<string, string>();

        private static readonly ConcurrentDictionary<string, string> _userConnections2 = new ConcurrentDictionary<string, string>();



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
                _userConnections2.TryAdd(username, Context.ConnectionId);
                _logger.LogInformation($"User {username} connected with connection id {Context.ConnectionId}");
            }
            await base.OnConnectedAsync();
        }

        public async Task SendPrivateMessage(int conversationId, string message)
        {
            _logger.LogInformation($"Attempting to send message in conversation {conversationId}");

            var senderUsername = Context.User?.Identity?.Name;
            _logger.LogInformation($"Sender username from context: {senderUsername}");

            if (string.IsNullOrEmpty(senderUsername))
            {
                _logger.LogError("Authorization failed: username is null or empty");
                throw new HubException("Unauthorized");
            }

            try
            {
                _logger.LogInformation($"Fetching conversation with ID {conversationId}");
                var conversation = await _context.Conversations
                    .Include(c => c.User1)
                    .Include(c => c.User2)
                    .FirstOrDefaultAsync(c => c.Id == conversationId);

                if (conversation == null)
                {
                    _logger.LogError($"Conversation {conversationId} not found");
                    throw new HubException("Conversation not found");
                }

                _logger.LogInformation($"Found conversation between {conversation.User1.Username} and {conversation.User2.Username}");

                _logger.LogInformation($"Fetching sender details for {senderUsername}");
                var sender = await _context.Users.FirstAsync(u => u.Username == senderUsername);
                _logger.LogInformation($"Found sender with ID {sender.Id}");

                if (conversation.User1Id != sender.Id && conversation.User2Id != sender.Id)
                {
                    _logger.LogError($"User {senderUsername} not authorized for conversation {conversationId}");
                    throw new HubException("Not authorized to send message in this conversation");
                }

                var newMessage = new Message
                {
                    ConversationId = conversationId,
                    SenderId = sender.Id,
                    Content = message,
                    Timestamp = DateTime.UtcNow
                };

                _logger.LogInformation($"Saving new message from {senderUsername} in conversation {conversationId}");
                _context.Messages.Add(newMessage);
                await _context.SaveChangesAsync();

                // Broadcast to both users
                var user1 = conversation.User1.Username;
                var user2 = conversation.User2.Username;

                _logger.LogInformation($"Current active connections: {string.Join(", ", _userConnections2.Keys)}");

                if (_userConnections2.TryGetValue(user1, out var user1ConnectionId))
                {
                    _logger.LogInformation($"Sending message to {user1} with connection ID {user1ConnectionId}");
                    await Clients.Client(user1ConnectionId)
                        .SendAsync("ReceivePrivateMessage", conversationId, sender.Username, message);
                }
                else
                {
                    _logger.LogWarning($"User {user1} is not connected");
                }

                if (_userConnections2.TryGetValue(user2, out var user2ConnectionId))
                {
                    _logger.LogInformation($"Sending message to {user2} with connection ID {user2ConnectionId}");
                    await Clients.Client(user2ConnectionId)
                        .SendAsync("ReceivePrivateMessage", conversationId, sender.Username, message);
                }
                else
                {
                    _logger.LogWarning($"User {user2} is not connected");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing message in conversation {conversationId}");
                throw new HubException($"Failed to send message: {ex.Message}");
            }
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var username = Context.User?.Identity?.Name;
            if (!string.IsNullOrEmpty(username))
            {
                _userConnections2.TryRemove(username, out _);
                _logger.LogInformation($"User {username} disconnected");
            }
            await base.OnDisconnectedAsync(exception);
        }
    }



}