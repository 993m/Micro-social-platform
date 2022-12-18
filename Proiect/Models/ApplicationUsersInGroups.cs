using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Proiect.Models
{
    public class ApplicationUsersInGroups
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]

        [Key]
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Userul este obligatoriu.")]
        public string UserId { get; set; }

        [Required(ErrorMessage = "Grupul este obligatoriu.")]
        public int GroupId { get; set; }
        public virtual ApplicationUser? User { get; set; }
        public virtual Group? Group { get; set; }
        public bool Confirmed { get; set; }
    }
}
