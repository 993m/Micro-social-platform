using System.ComponentModel.DataAnnotations;

namespace Proiect.Models
{
    public class Group
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Numele grupului este obligatoriu.")]
        public string GroupName { get; set; }

        public DateTime Date { get; set; }

        public string? Description { get; set; }

        public virtual ICollection<Post> Posts { get; set; }
    }

}
