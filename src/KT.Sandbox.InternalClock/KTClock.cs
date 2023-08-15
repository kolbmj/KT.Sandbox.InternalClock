using KT.Sandbox.InternalClock.Exceptions;
using KT.Sandbox.InternalClock.Extensibility;

namespace KT.Sandbox.InternalClock
{
    /// <summary>
    /// This singleton represents an internal clock your application can use to set
    /// app-wide custom time that is independent of the system time
    /// </summary>
    public sealed class KTClock : IClock
    {
        //fields
        private Boolean _isInitialized = false;
        private static KTClock _instance = new();
        private DateTimeOffset _genesis = DateTimeOffset.MinValue;
        private readonly DateTime _systemStart = DateTime.Now;

        //properties
        public DateTimeOffset Now => GetNow();
        public DateTimeOffset UtcNow => GetNowUtc();
        public DateOnly NowDate => GetNowDate();
        public TimeOnly NowTime => GetNowTime();
        public static KTClock Instance { get { return _instance; } }

        /// <summary>
        /// Hide the constructor as this class should be a singleton in order
        /// to prevent changes in clock time after initially set.
        /// </summary>
        private KTClock()
        {
        }

        /// <summary>
        /// Initialize the clock with optional starting time.
        /// 
        /// If startingTime is null or missing, then clock will use network 
        /// time protocol to get the current time for a list of time servers.
        /// </summary>
        /// <param name="startingTime"></param>
        /// <exception cref="ClockException"></exception>
        public void Initialize(DateTimeOffset? startingTime = null)
        {
            //ensure the instance is init'd only once
            if (this._isInitialized)
                throw new ClockException("The clock has already been initialized.  Initialization process may only occur once.");

            _instance.SetGenesisTime(startingTime ?? NetworkTimeClient.GetNetworkTime());
            this._isInitialized = true;
        }

        /// <summary>
        /// Set the genesis time field
        /// </summary>
        /// <param name="startingTime"></param>
        private void SetGenesisTime(DateTimeOffset startingTime)
        {
            this._genesis = startingTime;
        }

        /// <summary>
        /// Return the clock's local time
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ClockException"></exception>
        private DateTimeOffset GetNow()
        {
            if (!this._isInitialized)
                throw new ClockException("The clock has not been initialized.  Please call Initialize first.");

            return _genesis.Add(DateTime.Now - _systemStart).ToLocalTime();
        }

        /// <summary>
        /// Return the clock's utc time
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ClockException"></exception>
        private DateTimeOffset GetNowUtc()
        {
            if (!this._isInitialized)
                throw new ClockException("The clock has not been initialized.  Please call Initialize first.");

            return _genesis.Add(DateTime.Now - _systemStart);
        }

        /// <summary>
        /// Return the clock's utc time as a DateOnly
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ClockException"></exception>
        private DateOnly GetNowDate()
        {
            if (!this._isInitialized)
                throw new ClockException("The clock has not been initialized.  Please call Initialize first.");

            return DateOnly.FromDateTime(_genesis.Add(DateTime.Now - _systemStart).ToLocalTime().Date);
        }

        // <summary>
        /// Return the clock's utc time as a TimeOnly
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ClockException"></exception>
        private TimeOnly GetNowTime()
        {
            if (!this._isInitialized)
                throw new ClockException("The clock has not been initialized.  Please call Initialize first.");

            return TimeOnly.FromDateTime(_genesis.Add(DateTime.Now - _systemStart).ToLocalTime().DateTime);
        }
    }
}
