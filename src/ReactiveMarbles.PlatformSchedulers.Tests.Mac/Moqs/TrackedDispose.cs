// Copyright (c) 2019 Glenn Watson. All rights reserved.
// Glenn Watson licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

using System;

namespace VirtualDesktop.Mac.Streamer.Tests.Moqs
{
    internal class TrackedDispose : IDisposable
    {
        private int _disposeCount;

        public bool WasDisposed
        {
            get
            {
                if (_disposeCount == 0)
                {
                    return false;
                }

                if (_disposeCount == 1)
                {
                    return true;
                }

                throw new Exception("Invalid dispose count: " + _disposeCount);
            }
        }

        public void Dispose()
        {
            _disposeCount++;
        }
    }
}