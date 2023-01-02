using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace Proiect.Models
{
    public class Post
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Titlul este obligatoriu.")]
        public string Title { get; set; }

        [Required(ErrorMessage = "Continutul este obligatoriu.")]
        public string Content { get; set; }

        public DateTime Date { get; set; }

        public int? GroupId { get; set; }

        [Required(ErrorMessage = "Categoria este obligatorie.")]
        public int? CategoryId { get; set; }

        public string? UserId { get; set; }

        public virtual ApplicationUser? User { get; set; }

        public virtual Category? Category { get; set; }

        public virtual Group? Group { get; set; }

        public virtual ICollection<Comment>? Comments { get; set; }

        [NotMapped]
        public IEnumerable<SelectListItem>? Categ { get; set; }

    }
}
