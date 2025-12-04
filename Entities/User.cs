using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace api1.Entities
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        [Required,MaxLength(50)]
        public string Username { get; set; }
        [Required,EmailAddress,MaxLength(100)]
        public string EmailAddress { get; set; }
        [Required]
        public string Password { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public DateTime CreadetAt { get; set; } = DateTime.UtcNow;
    }
}
