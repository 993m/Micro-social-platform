using System.ComponentModel.DataAnnotations;

namespace Proiect.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }
        public string? UserId { get; set; }
        public virtual ApplicationUser? User { get; set; }
        public string? FromUserId { get; set; }
        public string? FromUserName { get; set; }
        public int? GroupId { get; set; }
        public virtual Group? Group { get; set; }
        
        [Required(ErrorMessage = "Mesajul este obligatoriu.")]
        public string Message { get; set; }
        public bool Seen { get; set; }
        public DateTime Date { get; set; }
    }
}
