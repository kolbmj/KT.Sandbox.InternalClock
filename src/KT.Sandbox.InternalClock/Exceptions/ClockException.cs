namespace KT.Sandbox.InternalClock.Exceptions
{
    /// <summary>
    /// This class is used for exceptions thrown within the Clock class
    /// </summary>
    public class ClockException : Exception
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message"></param>
        public ClockException(string message) : base(message) 
        { 
        }
    }
}
