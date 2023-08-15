#define USE_NETWORK_TIME

using KT.Sandbox.InternalClock;
using KT.Sandbox.InternalClock.Extensibility;

namespace ExampleApp
{
    /// <summary>
    /// This class demonstrates how to use a "clock" in order to bypass the
    /// system time.  
    /// 
    /// This is helpful when unit testing requires different times or in scenarios
    /// where the application may not rely on system time.
    /// 
    /// note: 
    ///     .net 8 prerelease has additional features around an depdending on
    ///     system time with an ITimeProvider implementation.  will be
    ///     worth looking into.
    /// </summary>
    internal class Program
    {
        //constants
        private const Int32 SECONDS_TO_WAIT = 3;

        /// <summary>
        /// Main entry-point of the application.
        /// 
        /// Comment out USE_NETWORK_TIME preprocessor directive (line 1) to
        /// set your own custom time
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static async Task Main(string[] args)
        {
#if USE_NETWORK_TIME
            //to demo use of network time:
            DateTimeOffset? myTime = null;                                                                  //clock will use ntp to get time if this is null
#else
            //to demo a custom time:
            DateTimeOffset? myTime = new DateTimeOffset(3099, 7, 4, 23, 59, 59, TimeSpan.FromHours(-7));    //--July 4th, 3099 at 11:59:59pm in Phoenix -07:00
#endif

            //set up a clock
            IClock appClock = KTClock.Instance;
            appClock.Initialize(myTime);

            //report the current time
            Console.WriteLine("at the tone, it is: {0:MM/dd/yyyy hh:mm:ss.fff tt}", appClock.Now);

            //wait a few seconds
            Console.WriteLine($"waiting {SECONDS_TO_WAIT} seconds...");

            await Task.Delay(SECONDS_TO_WAIT * 1_000);

            //report the current time again
            Console.WriteLine();
            Console.WriteLine("now it is:          {0:MM/dd/yyyy hh:mm:ss.fff tt}", appClock.Now);
        }
    }
}