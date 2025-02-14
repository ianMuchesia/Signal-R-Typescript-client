using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace server.Models
{
  public class Message
{
    [Key]
    public int Id { get; set; }

    public int ConversationId { get; set; }
    public int SenderId { get; set; }

    [Required]
    public string Content { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [ForeignKey("ConversationId")]
    public Conversation Conversation { get; set; }
    
    [ForeignKey("SenderId")]
    public User Sender { get; set; }
}

}