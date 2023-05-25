using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace mstg.Entities
{
    public class User
    {
        public class Enums
        {
            public enum Service
            {
                Mastodon = 0,
                Telegram = 1
            }
        }

        [Key]
        public string Id { get; set; }

        public DateTime LastAccessedAt { get; set; }
        public Enums.Service Service { get; set; }
        public string ServiceId { get; set; }
        public string? ServiceName { get; set; }
        public string? ServiceUsername { get; set; }
    }
}