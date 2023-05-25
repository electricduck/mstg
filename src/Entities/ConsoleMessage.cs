
namespace mstg.Entities
{
    public class ConsoleMessage
    {
        public class Enums
        {
            public enum Type
            {
                Normal,
                Error,
                Info,
                Success
            }
        }

        public string Details { get; set; }
        public string Emoji { get; set; }
        public string Text { get; set; }
        public Enums.Type Type { get; set; } = Enums.Type.Normal;
    }
}