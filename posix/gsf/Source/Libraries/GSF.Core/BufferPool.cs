﻿//******************************************************************************************************
//  BufferPool.cs - Gbtc
//
//  Copyright © 2012, Grid Protection Alliance.  All Rights Reserved.
//
//  Licensed to the Grid Protection Alliance (GPA) under one or more contributor license agreements. See
//  the NOTICE file distributed with this work for additional information regarding copyright ownership.
//  The GPA licenses this file to you under the Eclipse Public License -v 1.0 (the "License"); you may
//  not use this file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://www.opensource.org/licenses/eclipse-1.0.php
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//  Code Modification History:
//  ----------------------------------------------------------------------------------------------------
//  10/27/2011 - J. Ritchie Carroll
//       Initial version of source generate
//  12/14/2012 - Starlynn Danyelle Gilliam
//       Modified Header..
//
//******************************************************************************************************

using System;
using System.ServiceModel.Channels;

namespace GSF
{
    /// <summary>
    /// Represents a common buffer pool that can be used by an application.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The buffer pool is statically created at application startup and is available for all components and classes
    /// within an application domain. Every time you need to use a buffer, you take one from the pool, use it, and
    /// return it to the pool when done.
    /// </para>
    /// <para>
    /// It is very important to return the buffer to the pool when you are finished using it. If you are using a buffer
    /// scoped within a method, make sure to use a try/finally so that you can take the buffer within the try and
    /// return the buffer within the finally. If you are using a buffer as a member scoped class field, make sure
    /// you use the standard dispose pattern and return the buffer in the <see cref="IDisposable.Dispose"/> method.
    /// </para>
    /// <para>
    /// Internally this class simply wraps a static instance of the WCF <see cref="BufferManager"/>.
    /// </para>
    /// </remarks>
    [Obsolete("It is not recommended to use this class because the need for pooling is rare and implementations of pooling can be dangerous.")]
    public static class BufferPool
    {
        // Note that the buffer manager will create an queue buffers as needed during run-time, the default maximum
        // pool size and default maximum buffer sizes are set to int max. Even if an app happened to max out the pool
        // size, the buffer manager will still successfully provide and manage buffers - they simply won't be cached.
        private static readonly BufferManager s_bufferManager = BufferManager.CreateBufferManager(int.MaxValue, int.MaxValue);

        /// <summary>
        /// Gets a buffer of at least the specified size from the pool.
        /// </summary>
        /// <param name="bufferSize">The size, in bytes, of the requested buffer.</param>
        /// <returns>A byte array that is the requested size of the buffer.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="bufferSize"/> cannot be less than zero.</exception>
        public static byte[] TakeBuffer(int bufferSize)
        {
            return s_bufferManager.TakeBuffer(bufferSize);
        }

        /// <summary>
        /// Returns a buffer to the pool.
        /// </summary>
        /// <param name="buffer">A reference to the buffer being returned.</param>
        /// <exception cref="ArgumentNullException"><paramref name="buffer"/> reference cannot be null.</exception>
        /// <exception cref="ArgumentException">Length of <paramref name="buffer"/> does not match the pool's buffer length property.</exception>
        public static void ReturnBuffer(byte[] buffer)
        {
            s_bufferManager.ReturnBuffer(buffer);
        }

        /// <summary>
        /// Releases all the buffers currently cached in the pool.
        /// </summary>
        public static void Clear()
        {
            s_bufferManager.Clear();
        }
    }
}