using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace api1.Entities
{
    public class FriendRequest
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int SenderId { get; set; }
        [Required]
        public int ReceiverId { get; set; }
        public byte Status { get; set; } = 0;
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        [ForeignKey(nameof(SenderId))]
        public User Sender { get; set; }
        [ForeignKey(nameof(ReceiverId))]
        public User Receiver { get; set; }

    }
}
