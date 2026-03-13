
namespace HelpDesk.Domain.ValueObjects
{
    public record SlaLimit
    {
        public DateTime Deadline { get; init; }

        private SlaLimit(DateTime deadline) => Deadline = deadline;

        public static SlaLimit Create(Priority priority, DateTime startTime)
        {
            var duration = priority.Value switch
            {
                "Urgent" => TimeSpan.FromHours(4),
                "High" => TimeSpan.FromHours(24),
                "Medium" => TimeSpan.FromDays(3),
                "Low" => TimeSpan.FromDays(7),
                _ => throw new ArgumentException("Invalid Priority")
            };
            return new SlaLimit(startTime.Add(duration));
        }

        public SlaLimit ApplyEscalation()
        {
            var remainingTime = Deadline - DateTime.UtcNow;

            if (remainingTime <= TimeSpan.Zero) return this;
            // Rule: Escalation reduces the remaining response time limit by 33%
            var reducedRemaining = TimeSpan.FromTicks((long)(remainingTime.Ticks * 0.67));
            return new SlaLimit(DateTime.UtcNow.Add(reducedRemaining));
        }

        public bool IsBreached() => Deadline <DateTime.UtcNow;
    }
}
