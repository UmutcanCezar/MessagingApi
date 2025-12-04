using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace api1.Entities
{
    public class Friend
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int UserID { get; set; }
        [Required]
        public int FriendID { get; set; }
        public byte Status { get; set; } = 0; // 0 beklemede , 1 kabul edildi , 2 reddedildi
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [ForeignKey(nameof(UserID))]
        public User User { get; set; }
        [ForeignKey(nameof(FriendID))]
        public User FriendUser { get; set; }

    }
}
