// Copyright (c) 2019 Glenn Watson. All rights reserved.
// Glenn Watson licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using Android.App;
using Android.OS;

namespace ReactiveMarbles.PlatformSchedulers
{
    /// <summary>
    /// HandlerScheduler is a scheduler that schedules items on a running
    /// Activity's main thread. This is the moral equivalent of
    /// DispatcherScheduler.
    /// </summary>
    public class HandlerScheduler : IScheduler
    {
        private readonly Handler _handler;
        private readonly long _looperId;

        /// <summary>
        /// Initializes a new instance of the <see cref="HandlerScheduler"/> class.
        /// </summary>
        /// <param name="handler">The handler.</param>
        /// <param name="threadIdAssociatedWithHandler">The thread identifier associated with handler.</param>
        public HandlerScheduler(Handler handler, long? threadIdAssociatedWithHandler)
        {
            _handler = handler;
            _looperId = threadIdAssociatedWithHandler ?? -1;
        }

        /// <summary>
        /// Gets a common instance to avoid allocations to the MainThread for the HandlerScheduler.
        /// </summary>
        public static IScheduler MainThreadScheduler { get; } = new HandlerScheduler(new Handler(Looper.MainLooper), Looper.MainLooper.Thread.Id);

        /// <inheritdoc/>
        public DateTimeOffset Now => DateTimeOffset.Now;

        /// <inheritdoc/>
        [SuppressMessage("Design", "CA2000", Justification = "Dispose disposable")]
        public IDisposable Schedule<TState>(TState state, Func<IScheduler, TState, IDisposable> action)
        {
            bool isCancelled = false;
            var innerDisp = new SerialDisposable() { Disposable = Disposable.Empty };

            _handler.Post(() =>
            {
                if (isCancelled)
                {
                    return;
                }

                innerDisp.Disposable = action(this, state);
            });

            return new CompositeDisposable(
                Disposable.Create(() => isCancelled = true),
                innerDisp);
        }

        /// <inheritdoc/>
        [SuppressMessage("Design", "CA2000", Justification = "Dispose disposable")]
        public IDisposable Schedule<TState>(TState state, TimeSpan dueTime, Func<IScheduler, TState, IDisposable> action)
        {
            bool isCancelled = false;
            var innerDisp = new SerialDisposable() { Disposable = Disposable.Empty };

            _handler.PostDelayed(
                () =>
            {
                if (isCancelled)
                {
                    return;
                }

                innerDisp.Disposable = action(this, state);
            }, dueTime.Ticks / 10 / 1000);

            return new CompositeDisposable(
                Disposable.Create(() => isCancelled = true),
                innerDisp);
        }

        /// <inheritdoc/>
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