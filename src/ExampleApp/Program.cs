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
    ///     .net 8 prerelease has additional features around an depending on
    ///     system time with an ITimeProvider implementation.  will be
    ///     worth looking into.
    /// </summary>
    internal class Program
    {
        //constants
        private const Int32 SECONDS_TO_WAIT = 3;

        /// <summary>
        /// Main entry-point of the application.
        /// </summary>
        /// <returns></returns>
        private static async Task Main()
        {
            //set up a clock
            IClock appClock = KTClock.Instance;
            appClock.Initialize();
            //appClock.Initialize(new DateTimeOffset(3099, 7, 4, 23, 59, 59, TimeSpan.FromHours(-7))); //--July 4th, 3099 at 11:59:59pm in Phoenix -07:00
            
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