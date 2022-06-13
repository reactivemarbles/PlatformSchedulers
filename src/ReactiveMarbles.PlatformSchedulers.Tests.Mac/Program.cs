// Copyright (c) 2022 Glenn Watson. All rights reserved.
// Glenn Watson licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Threading;
using AppKit;
using Xunit.Runners;

namespace VirtualDesktop.Mac.Streamer.Tests
{
    public static class Program
    {
        // We use consoleLock because messages can arrive in parallel, so we want to make sure we get
        // consistent console output.
        private static readonly object _consoleLock = new object();

        // Use an event to know when we're done
        private static readonly ManualResetEvent _finished = new ManualResetEvent(false);

        // Start out assuming success; we'll set this to 1 if we get a failed test
        private static int _result = 0;

        public static int Main()
        {
            NSApplication.Init();

            var testAssembly = typeof(Program).Assembly;

            using (var runner = AssemblyRunner.WithoutAppDomain(testAssembly.FullName))
            {
                runner.OnDiscoveryComplete = OnDiscoveryComplete;
                runner.OnExecutionComplete = OnExecutionComplete;
                runner.OnTestFailed = OnTestFailed;
                runner.OnTestSkipped = OnTestSkipped;

                Console.WriteLine("Discovering...");
                runner.Start(null);

                _finished.WaitOne();
                _finished.Dispose();

                return _result;
            }
        }

        private static void OnDiscoveryComplete(DiscoveryCompleteInfo info)
        {
            lock (_consoleLock)
            {
                Console.WriteLine($"Running {info.TestCasesToRun} of {info.TestCasesDiscovered} tests...");
            }
        }

        private static void OnExecutionComplete(ExecutionCompleteInfo info)
        {
            lock (_consoleLock)
            {
                Console.WriteLine($"Finished: {info.TotalTests} tests in {Math.Round(info.ExecutionTime, 3)}s ({info.TestsFailed} failed, {info.TestsSkipped} skipped)");
            }

            _finished.Set();
        }

        private static void OnTestFailed(TestFailedInfo info)
        {
            lock (_consoleLock)
            {
                Console.ForegroundColor = ConsoleColor.Red;

                Console.WriteLine("[FAIL] {0}: {1}", info.TestDisplayName, info.ExceptionMessage);
                if (info.ExceptionStackTrace != null)
                {
                    Console.WriteLine(info.ExceptionStackTrace);
                }

                Console.ResetColor();
            }

            _result = 1;
        }

        private static void OnTestSkipped(TestSkippedInfo info)
        {
            lock (_consoleLock)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[SKIP] {0}: {1}", info.TestDisplayName, info.SkipReason);
                Console.ResetColor();
            }
        }
    }
}