// Copyright (c) 2022 Glenn Watson. All rights reserved.
// Glenn Watson licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Reactive.Concurrency;
using Microsoft.Reactive.Testing;
using ReactiveMarbles.PlatformSchedulers;
using Shouldly;
using VirtualDesktop.Mac.Streamer.Tests.Moqs;
using Xunit;

namespace PlatformSchedulers.Tests.Mac
{
    public class DispatchQueueSchedulerTests
    {
        private readonly TestScheduler _testScheduler;
        private readonly FakeDispatchQueue _fakeDispatchQueue;
        private readonly IScheduler _schedulerBeingTested;
        private readonly string _expectedState;
        private readonly TrackedDispose _scheduledCallbackDisposable;
        private int _scheduledCallbackCalledCount;

        public DispatchQueueSchedulerTests()
        {
            _testScheduler = new TestScheduler();

            _scheduledCallbackCalledCount = 0;
            _expectedState = "foo";
            _scheduledCallbackDisposable = new TrackedDispose();

            _fakeDispatchQueue = new FakeDispatchQueue();
            _schedulerBeingTested = new DispatchQueueScheduler(_fakeDispatchQueue.DispatchAsync, _testScheduler);
        }

        [Fact]
        public void WhenScheduleIsCancelledEarlyTheCallbackWillNotRun()
        {
            var timerDisposable = SetupSingleShotTimer();

            _testScheduler.AdvanceBy(TimeSpan.FromSeconds(0.999).Ticks);

            _fakeDispatchQueue.Queue.Count.ShouldBe(0);
            _scheduledCallbackCalledCount.ShouldBe(0);

            timerDisposable.Dispose();

            _fakeDispatchQueue.Queue.Count.ShouldBe(0);
            _scheduledCallbackCalledCount.ShouldBe(0);

            _testScheduler.AdvanceBy(TimeSpan.FromSeconds(100).Ticks);

            _fakeDispatchQueue.Queue.Count.ShouldBe(0);
            _scheduledCallbackCalledCount.ShouldBe(0);
        }

        [Fact]
        public void WhenScheduleIsCancelledAfterTimeoutButBeforeDispatchTheCallbackWillNotRun()
        {
            var timerDisposable = SetupSingleShotTimer();

            _testScheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);

            _fakeDispatchQueue.Queue.Count.ShouldBe(1);
            _scheduledCallbackCalledCount.ShouldBe(0);
            _scheduledCallbackDisposable.WasDisposed.ShouldBe(false);

            timerDisposable.Dispose();
            _fakeDispatchQueue.RunQueue();

            _fakeDispatchQueue.Queue.Count.ShouldBe(0);
            _scheduledCallbackCalledCount.ShouldBe(0);
            _scheduledCallbackDisposable.WasDisposed.ShouldBe(false);
        }

        [Fact]
        public void WhenScheduleIsCancelledAfterTimeoutAndDispatchTheCallbackRunsAndTheCallbackDisposableIsDisposed()
        {
            var timerDisposable = SetupSingleShotTimer();

            _testScheduler.AdvanceBy(TimeSpan.FromSeconds(1).Ticks);
            _fakeDispatchQueue.Queue.Count.ShouldBe(1);
            _fakeDispatchQueue.RunQueue();

            _scheduledCallbackCalledCount.ShouldBe(1);
            _scheduledCallbackDisposable.WasDisposed.ShouldBe(false);

            timerDisposable.Dispose();

            _scheduledCallbackCalledCount.ShouldBe(1);
            _scheduledCallbackDisposable.WasDisposed.ShouldBe(true);
        }

        private IDisposable SetupSingleShotTimer()
        {
            return _schedulerBeingTested.Schedule(_expectedState, TimeSpan.FromSeconds(1), (sch, st) =>
            {
                st.ShouldBe(_expectedState);
                sch.ShouldBe(_testScheduler);
                _scheduledCallbackCalledCount++;
                return _scheduledCallbackDisposable;
            });
        }
    }
}