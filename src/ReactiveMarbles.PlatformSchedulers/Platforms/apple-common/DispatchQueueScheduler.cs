// Copyright (c) 2022 Glenn Watson. All rights reserved.
// Glenn Watson licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using CoreFoundation;

namespace ReactiveMarbles.PlatformSchedulers
{
    /// <summary>
    /// A dispatcher that will use a DispatchQueue to issue scheduled items.
    /// </summary>
    public class DispatchQueueScheduler : IScheduler
    {
        private readonly Action<Action> _dispatchAction;
        private readonly IScheduler _scheduler;

        /// <summary>
        /// Initializes a new instance of the <see cref="DispatchQueueScheduler"/> class.
        /// </summary>
        /// <param name="dispatchQueue">The dispatch queue to issue actions to. If empty will be the main queue.</param>
        /// <param name="delayedScheduler">A optional scheduler for dispatching long running items to.</param>
        public DispatchQueueScheduler(DispatchQueue dispatchQueue = null, IScheduler delayedScheduler = null)
        {
            if (dispatchQueue == null)
            {
                throw new ArgumentNullException(nameof(dispatchQueue));
            }

            _dispatchAction = dispatchQueue.DispatchAsync;
            _scheduler = delayedScheduler ?? new EventLoopScheduler();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DispatchQueueScheduler"/> class.
        /// </summary>
        /// <param name="dispatchAction">The dispatch action to issue actions to.</param>
        /// <param name="delayedScheduler">A optional scheduler for dispatching long running items to.</param>
        public DispatchQueueScheduler(Action<Action> dispatchAction, IScheduler delayedScheduler = null)
        {
            _dispatchAction = dispatchAction ?? throw new ArgumentNullException(nameof(dispatchAction));
            _scheduler = delayedScheduler ?? new EventLoopScheduler();
        }

        /// <inheritdoc/>
        public DateTimeOffset Now => _scheduler.Now;

        /// <inheritdoc />
        public IDisposable Schedule<TState>(TState state, Func<IScheduler, TState, IDisposable> action)
        {
            var innerDisp = new SingleAssignmentDisposable();

            _dispatchAction(() =>
            {
                if (!innerDisp.IsDisposed)
                {
                    innerDisp.Disposable = action(this, state);
                }
            });

            return innerDisp;
        }

        /// <inheritdoc />
        public IDisposable Schedule<TState>(
            TState state,
            TimeSpan dueTime,
            Func<IScheduler, TState, IDisposable> action)
        {
            return _scheduler.Schedule(state, dueTime, (scheduler, inputState) => RunInDispatcherAndWrapDispose(() => action(scheduler, inputState)));
        }

        /// <inheritdoc />
        public IDisposable Schedule<TState>(
            TState state,
            DateTimeOffset dueTime,
            Func<IScheduler, TState, IDisposable> action)
        {
            if (dueTime <= Now)
            {
                return Schedule(state, action);
            }

            return Schedule(state, dueTime - Now, action);
        }

        private IDisposable RunInDispatcherAndWrapDispose(Func<IDisposable> action)
        {
            // We're going to schedule the action to run in the CoreFoundation DispatchQueue.
            // We'll return a disposable that prevents the action from running if it hasn't yet,
            // or disposes the result of the action if it already has.
            var innerDisp = Disposable.Empty;
            bool isCancelled = false;

            _dispatchAction(() =>
            {
                if (!isCancelled)
                {
                    innerDisp = action();
                }
            });

            return Disposable.Create(() =>
            {
                isCancelled = true;
                innerDisp.Dispose();
            });
        }
    }
}