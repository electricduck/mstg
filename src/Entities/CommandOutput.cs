
namespace mstg.Entities
{
    public class CommandOutput
    {
        public string FailureReason { get; set; }
        public Entities.Media.Enums.Type MediaType { get; set; } = Entities.Media.Enums.Type.Unknown; // TODO: This is janky
        public string MediaUrl { get; set; }
        public bool Success { get; set; }
        public string Text { get; set; }
    }
}