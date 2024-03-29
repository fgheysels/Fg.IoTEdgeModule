﻿using Microsoft.Extensions.Logging;
using System;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Threading;

namespace Fg.IoTEdgeModule
{
    public class ShutdownHandler
    {
        /// <summary>
        /// Here are some references which were used for this code -
        /// https://stackoverflow.com/questions/40742192/how-to-do-gracefully-shutdown-on-dotnet-with-docker/43813871
        /// https://msdn.microsoft.com/en-us/library/system.gc.keepalive(v=vs.110).aspx
        /// </summary>
        public static ShutdownHandler Create(TimeSpan shutdownWaitPeriod, ILogger logger)
        {
            var cts = new CancellationTokenSource();
            var completed = new ManualResetEventSlim();
            object handler = null;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                WindowsShutdownHandler.HandlerRoutine hr = WindowsShutdownHandler.Init(cts, completed, shutdownWaitPeriod, logger);

                handler = hr;
            }
            else
            {
                LinuxShutdownHandler.Init(cts, completed, shutdownWaitPeriod, logger);
            }

            return new ShutdownHandler(cts, completed, handler);
        }

        private readonly ManualResetEventSlim _doneSignal;
        private readonly object _shutdownHandlerRoutine;
        public CancellationTokenSource CancellationTokenSource { get; }

        /// <summary>
        /// Signals that all resources have been cleaned up and that the shutdown sequence can continue.
        /// </summary>
        public void SignalCleanupComplete()
        {
            _doneSignal.Set();

            if (_shutdownHandlerRoutine != null)
            {
                GC.KeepAlive(_shutdownHandlerRoutine);
            }
        }

        private ShutdownHandler(CancellationTokenSource cts, ManualResetEventSlim doneSignal, object shutdownHandlerRoutine)
        {
            CancellationTokenSource = cts;
            _doneSignal = doneSignal;
            _shutdownHandlerRoutine = shutdownHandlerRoutine;
        }

        private static class LinuxShutdownHandler
        {
            public static void Init(CancellationTokenSource cts, ManualResetEventSlim completed, TimeSpan shutdownWaitPeriod, ILogger logger)
            {
                void OnUnload(AssemblyLoadContext ctx) => CancelProgram();

                void CancelProgram()
                {
                    logger?.LogInformation("Termination requested, initiating shutdown.");
                    cts.Cancel();
                    logger?.LogInformation("Waiting for cleanup to finish");
                    // Wait for shutdown operations to complete.
                    if (completed.Wait(shutdownWaitPeriod))
                    {
                        logger?.LogInformation("Done with cleanup. Shutting down.");
                    }
                    else
                    {
                        logger?.LogInformation("Timed out waiting for cleanup to finish. Shutting down.");
                    }
                }

                AssemblyLoadContext.Default.Unloading += OnUnload;
                Console.CancelKeyPress += (sender, cpe) => CancelProgram();
                logger?.LogDebug("Waiting on shutdown handler to trigger");
            }
        }

        /// <summary>
        /// This is the recommended way to handle shutdown of windows containers. References -
        /// https://github.com/moby/moby/issues/25982
        /// https://gist.github.com/darstahl/fbb80c265dcfd1b327aabcc0f3554e56
        /// </summary>
        private static class WindowsShutdownHandler
        {
            public delegate bool HandlerRoutine(CtrlTypes ctrlType);

            public enum CtrlTypes
            {
                CTRL_C_EVENT = 0,
                CTRL_BREAK_EVENT = 1,
                CTRL_CLOSE_EVENT = 2,
                CTRL_LOGOFF_EVENT = 5,
                CTRL_SHUTDOWN_EVENT = 6
            }

            public static HandlerRoutine Init(
                CancellationTokenSource cts,
                ManualResetEventSlim completed,
                TimeSpan waitPeriod,
                ILogger logger)
            {
                var hr = new HandlerRoutine(
                    type =>
                    {
                        logger?.LogInformation($"Received signal of type {type}");
                        if (type == CtrlTypes.CTRL_SHUTDOWN_EVENT)
                        {
                            logger?.LogInformation("Initiating shutdown");
                            cts.Cancel();
                            logger?.LogInformation("Waiting for cleanup to finish");
                            if (completed.Wait(waitPeriod))
                            {
                                logger?.LogInformation("Done with cleanup. Shutting down.");
                            }
                            else
                            {
                                logger?.LogInformation("Timed out waiting for cleanup to finish. Shutting down.");
                            }
                        }

                        return false;
                    });

                SetConsoleCtrlHandler(hr, true);
                logger?.LogDebug("Waiting on shutdown handler to trigger");
                return hr;
            }

            [DllImport("Kernel32")]
            static extern bool SetConsoleCtrlHandler(HandlerRoutine handler, bool add);
        }
    }
}
