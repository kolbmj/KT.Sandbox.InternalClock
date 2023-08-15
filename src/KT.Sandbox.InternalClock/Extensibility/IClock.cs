namespace KT.Sandbox.InternalClock.Extensibility
{
    /// <summary>
    /// This interface defines the contact for all Clocks
    /// </summary>
    public interface IClock
    {
        public DateTimeOffset Now { get; }
        public DateTimeOffset UtcNow { get; }
        public DateOnly NowDate { get; }
        public TimeOnly NowTime { get; }

        public void Initialize(DateTimeOffset? startingTime = null);
    }
}
