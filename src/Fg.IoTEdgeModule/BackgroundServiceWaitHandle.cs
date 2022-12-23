using System.Threading.Tasks;
using System.Threading;

namespace Fg.IoTEdgeModule
{
    /// <summary>
    /// WaitHandle that can be used to make sure that BackgroundService implementations are only started
    /// when the host application signals that they can start.
    /// </summary>
    public static class BackgroundServiceWaitHandle
    {
        private static readonly ManualResetEvent WaitHandle = new ManualResetEvent(false);

        /// <summary>
        /// Wait for the BackgroundServiceWaitHandle to get signaled before execution can continue.
        /// </summary>
        public static async Task WaitForSignalAsync()
        {
            await Task.Yield();
            WaitHandle.WaitOne();
        }

        /// <summary>
        /// Signal that execution of the BackgroundService(s) can start.
        /// </summary>
        public static void SetSignal()
        {
            WaitHandle.Set();
        }
    }
}
