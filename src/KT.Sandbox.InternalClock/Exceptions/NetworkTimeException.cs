namespace KT.Sandbox.InternalClock.Exceptions
{
    /// <summary>
    /// This class is used for exceptions thrown when there is a network
    /// issue trying to reach ntp servers
    /// </summary>
    public class NetworkTimeException : Exception
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message"></param>
        public NetworkTimeException(string message) : base(message)
        {
        }
    }
}
