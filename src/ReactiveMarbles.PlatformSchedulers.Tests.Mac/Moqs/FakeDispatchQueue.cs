// Copyright (c) 2022 Glenn Watson. All rights reserved.
// Glenn Watson licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace VirtualDesktop.Mac.Streamer.Tests.Moqs
{
    internal class FakeDispatchQueue
    {
        public List<Action> Queue { get; } = new List<Action>();

        public void DispatchAsync(Action callback)
        {
            Queue.Add(callback);
        }

        public void RunQueue()
        {
            foreach (var callback in Queue)
            {
                callback();
            }

            Queue.Clear();
        }
    }
}