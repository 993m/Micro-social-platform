using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Proiect.Models
{
    public class Post
    {
        [Key]
        public int Id { get; set; }

        public string Title { get; set; }

        [Required(ErrorMessage = "Continutul este obligatoriu.")]
        public string Content { get; set; }

        public DateTime Date { get; set; }

        public int? GroupId { get; set; }

        [Required(ErrorMessage = "Categoria este obligatorie")]
        public int CategoryId { get; set; }

        public virtual Category Category { get; set; }

        public virtual Group Group { get; set; }

        public virtual ICollection<Comment> Comments { get; set; }


    }
}
