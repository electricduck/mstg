using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace mstg.Entities
{
    public class Queue
    {
        public class Enums
        {
            public enum Status
            {
                Queued = 0,
                Posted = 1,
                Removed = 2,
                Failed = 3
            }
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public DateTime CreatedAt { get; set; }
        public int FailureCount { get; set; }
        public string? FailureReason { get; set; }
        public Enums.Status Status { get; set; }
        public DateTime UpdatedAt { get; set; }

        public Post Post { get; set; }
    }
}