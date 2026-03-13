namespace HelpDesk.Domain.ValueObjects
{
    public record Priority(string Value)
    {
        public static Priority Low = new("Low");
        public static Priority Medium = new("Medium");
        public static Priority High = new("High");
        public static Priority Urgent = new("Urgent");

        public bool IsUrgent => Value == "Urgent";
    }
}
