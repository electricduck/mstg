using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace mstg.Entities
{
    public class Media
    {
        public class Enums
        {
            public enum Type
            {
                Unknown = 0,
                Photo = 1,
                Video = 2,
                Audio = 3
            }
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string MediaId { get; set; }
        public Enums.Type Type { get; set; }
        public string Url { get; set; }

        public Post Post { get; set; }
    }
}