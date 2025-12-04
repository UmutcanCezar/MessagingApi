using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace api1.Entities
{
    public class Message
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int SenderID { get; set; }
        [Required]
        public int ReceiverID { get; set; }
        [Required]
        public string Content { get; set; }
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; } = false;
        public bool IsDeleted { get; set; } = false;
        [ForeignKey(nameof(SenderID))]
        public User? Sender { get; set; }
        [ForeignKey(nameof(ReceiverID))]
        public User? Receiver { get; set; }
    }
}
