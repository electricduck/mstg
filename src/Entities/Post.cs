using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace mstg.Entities
{
    public class Post
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string AccountId { get; set; }
        public string AccountName { get; set; }
        public string AccountProfileUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Instance { get; set; }
        public bool Sensitive { get; set; }
        public string StatusContent { get; set; }
        public string StatusId { get; set; }
        public string StatusUrl { get; set; }

        public List<Media> MediaItems { get; set; }
    }
}