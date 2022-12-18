using System.ComponentModel.DataAnnotations;

namespace Proiect.Models
{
    public class Friend
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string UserId { get; set; } 
        public virtual ApplicationUser User { get; set; }
        [Required]
        public string FriendId { get; set; }
    }
}
