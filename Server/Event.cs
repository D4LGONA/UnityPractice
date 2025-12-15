namespace Events
{
    public enum EventType
    {
        EVT_TREE_RESPAWN = 0,
        EVT_DB_SAVE = 1
    }

    public interface IEventTarget
    {
        void OnEvent(EventType type);
    }

    public class Event
    {
        public uint DueSeconds { get; }
        public EventType Type { get; }
        public IEventTarget Target { get; }

        public Event(uint dueSeconds, IEventTarget target, EventType type)
        {
            DueSeconds = dueSeconds;
            Target = target;
            Type = type;
        }

        public static uint NowSeconds()
            => (uint)System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        public static Event After(uint delaySeconds, IEventTarget target, EventType type)
        {
            uint due = NowSeconds() + delaySeconds;
            return new Event(due, target, type);
        }
    }

    public static class TimeUtil
    {
        public static uint NowSeconds()
            => (uint)System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}
