using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace server.Models
{
    public class Message
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Content { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [ForeignKey("User")]
        public int SenderId { get; set; }


        [ForeignKey("User")]
        public int? ReceiverId { get;set;} = null;

        public User Sender { get; set; }

        public User Receiver { get;set;}
    }
}