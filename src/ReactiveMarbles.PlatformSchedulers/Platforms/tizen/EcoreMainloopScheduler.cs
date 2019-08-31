﻿// Copyright (c) 2019 Glenn Watson. All rights reserved.
// Glenn Watson licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using ElmSharp;

namespace ReactiveMarbles.PlatformSchedulers
{
    internal class EcoreMainloopScheduler : IScheduler
    {
        public static IScheduler MainThreadScheduler { get; } = new EcoreMainloopScheduler();

        public DateTimeOffset Now => DateTimeOffset.Now;

        public IDisposable Schedule<TState>(TState state, Func<IScheduler, TState, IDisposable> action)
        {
            var innerDisp = new SingleAssignmentDisposable();
            EcoreMainloop.PostAndWakeUp(() =>
            {
                if (!innerDisp.IsDisposed)
                {
                    innerDisp.Disposable = action(this, state);
                }
            });
            return innerDisp;
        }

        public IDisposable Schedule<TState>(TState state, TimeSpan dueTime, Func<IScheduler, TState, IDisposable> action)
        {
            var innerDisp = Disposable.Empty;
            bool isCancelled = false;

            IntPtr timer = EcoreMainloop.AddTimer(dueTime.TotalSeconds, () =>
            {
                if (!isCancelled)
                {
                    innerDisp = action(this, state);
                }

                return false;
            });

            return Disposable.Create(() =>
            {
                isCancelled = true;
                EcoreMainloop.RemoveTimer(timer);
                innerDisp.Dispose();
            });
        }

        public IDisposable Schedule<TState>(TState state, DateTimeOffset dueTime, Func<IScheduler, TState, IDisposable> action)
        {
            if (dueTime <= Now)
            {
                return Schedule(state, action);
            }

            return Schedule(state, dueTime - Now, action);
        }
    }
}
