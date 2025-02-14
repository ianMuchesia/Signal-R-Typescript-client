


using System.ComponentModel.DataAnnotations;

namespace server.Models
{
    public class ConversationParticipant
    {
        [Key]
        public int Id { get; set; }

        public int ConversationId { get; set; }
        public int UserId { get; set; }

        public DateTime LastRead { get; set; }
        public bool IsArchived { get; set; }

        public Conversation Conversation { get; set; }
        public User User { get; set; }
    }

}