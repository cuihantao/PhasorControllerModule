//******************************************************************************************************
//  ManagedThread.cs - Gbtc
//
//  Copyright � 2012, Grid Protection Alliance.  All Rights Reserved.
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
//  01/25/2008 - J. Ritchie Carroll
//       Initial version of source generated.
//  09/11/2008 - J. Ritchie Carroll
//       Converted to C#.
//  08/10/2009 - Josh L. Patterson
//       Edited Comments.
//  09/14/2009 - Stephen C. Wills
//       Added new header and license agreement.
//  09/22/2011 - J. Ritchie Carroll
//       Added Mono implementation exception regions.
//  12/14/2012 - Starlynn Danyelle Gilliam
//       Modified Header.
//
//******************************************************************************************************

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using GSF.Units;

namespace GSF.Threading
{
    #region [ Enumerations ]

    /// <summary>
    /// Managed Thread Types
    /// </summary>
    public enum ThreadType
    {
        /// <summary>Standard thread created with public constructor</summary>
        StandardThread,
        /// <summary>Queued thread added into managed thread pool</summary>
        QueuedThread
    }

    /// <summary>
    /// Managed Thread States
    /// </summary>
    public enum ThreadStatus
    {
        /// <summary>Thread created, not started</summary>
        Unstarted,
        /// <summary>Thread queued for execution</summary>
        Queued,
        /// <summary>Thread start requested, execution pending</summary>
        Started,
        /// <summary>Thread executing</summary>
        Executing,
        /// <summary>Thread completed</summary>
        Completed,
        /// <summary>Thread aborted</summary>
        Aborted
    }

    #endregion

    /// <summary>
    /// Defines a managed thread
    /// </summary>
    /// <remarks>
    /// This class works like any normal thread but provides the benefit of automatic tracking
    /// through the ManagedThreads collection, total thread runtime and the ability to run
    /// the thread in an alternate execution context
    /// </remarks>
    public sealed class ManagedThread
    {
        #region [ Members ]

        // Fields
        private Thread m_thread;
        private readonly ThreadType m_type;
        private ThreadStatus m_status;
        private string m_name;
        private Ticks m_startTime;
        private Ticks m_stopTime;
        private readonly ContextCallback m_ctxCallback;
        private readonly ThreadStart m_tsCallback;
        private readonly ParameterizedThreadStart m_ptsCallback;
        private readonly ExecutionContext m_ctx;
        private object m_state;
        private object m_tag;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Initializes a new instance of the ManagedThread class.
        /// </summary>
        /// <param name="callback">A <see cref="ThreadStart"/> object.</param>
        public ManagedThread(ThreadStart callback)
            : this(ThreadType.StandardThread, callback, null, null)
        {
            m_thread = new Thread(HandleItem);
        }

        /// <summary>
        /// Initializes a new instance of the ManagedThread class, specifying a delegate that allows an object to be passed to the thread when the thread is started.
        /// </summary>
        /// <param name="callback">A <see cref="ParameterizedThreadStart"/> object.</param>
        public ManagedThread(ParameterizedThreadStart callback)
            : this(ThreadType.StandardThread, callback, null, null)
        {
            m_thread = new Thread(HandleItem);
        }

#if !MONO
        /// <summary>
        /// Initializes a new instance of the ManagedThread class, specifying a delegate that allows an object to be passed to the thread when the thread is started
        /// and allowing the user to specify an alternate execution context for the thread.
        /// </summary>
        /// <param name="callback">A <see cref="ContextCallback"/> delgate to execute.</param>
        /// <param name="ctx">An <see cref="ExecutionContext"/>.</param>
        public ManagedThread(ContextCallback callback, ExecutionContext ctx)
            : this(ThreadType.StandardThread, callback, null, ctx)
        {
            m_thread = new Thread(HandleItem);
        }
#endif

        internal ManagedThread(ThreadType type, ThreadStart callback, object state, ExecutionContext ctx)
        {
            m_type = type;
            m_status = (type == ThreadType.QueuedThread ? ThreadStatus.Queued : ThreadStatus.Unstarted);
            m_tsCallback = callback;
            m_state = state;
            m_ctx = ctx;
        }

        internal ManagedThread(ThreadType type, ParameterizedThreadStart callback, object state, ExecutionContext ctx)
        {
            m_type = type;
            m_status = (type == ThreadType.QueuedThread ? ThreadStatus.Queued : ThreadStatus.Unstarted);
            m_ptsCallback = callback;
            m_state = state;
            m_ctx = ctx;
        }

        internal ManagedThread(ThreadType type, ContextCallback callback, object state, ExecutionContext ctx)
        {
            m_type = type;
            m_status = (type == ThreadType.QueuedThread ? ThreadStatus.Queued : ThreadStatus.Unstarted);
            m_ctxCallback = callback;
            m_state = state;
            m_ctx = ctx;
        }

        #endregion

        #region [ Properties ]

        /// <summary>
        /// An object containing data to be used by the thread's execution method.
        /// </summary>
        public object State
        {
            get
            {
                return m_state;
            }
            set
            {
                m_state = value;
            }
        }

        /// <summary>
        /// An object that allows additional user defined information to be tracked along with this thread.
        /// </summary>
        public object Tag
        {
            get
            {
                return m_tag;
            }
            set
            {
                m_tag = value;
            }
        }

        /// <summary>
        /// Returns the managed thread type (either StandardThread or QueuedThread)
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods")]
        public ThreadType Type
        {
            get
            {
                return m_type;
            }
        }

        /// <summary>
        /// Gets a value containing the curretn status of the current thread.
        /// </summary>
        public ThreadStatus Status
        {
            get
            {
                return m_status;
            }
            internal set
            {
                m_status = value;
            }
        }

        /// <summary>
        /// Gets a value indicating the execution status of the current thread.
        /// </summary>
        public bool IsAlive
        {
            get
            {
                return (m_status == ThreadStatus.Started || m_status == ThreadStatus.Executing);
            }
        }

        /// <summary>
        /// Gets or sets the name of the thread.
        /// </summary>
        public string Name
        {
            get
            {
                return m_name;
            }
            set
            {
                m_name = value;

                if (m_type == ThreadType.StandardThread)
                    m_thread.Name = value;
            }
        }

        /// <summary>
        /// Get the time, in ticks, that the thread started executing
        /// </summary>
        public Ticks StartTime
        {
            get
            {
                return m_startTime;
            }
        }

        /// <summary>
        /// Get the time, in ticks, that the thread finished executing
        /// </summary>
        public Ticks StopTime
        {
            get
            {
                return m_stopTime;
            }
        }

        /// <summary>
        /// Gets the total amount of time, in seconds, that the managed thread has been active.
        /// </summary>
        public Time RunTime
        {
            get
            {
                Ticks processingTime = 0;

                if (m_startTime > 0)
                {
                    if (m_stopTime > 0)
                        processingTime = m_stopTime - m_startTime;
                    else
                        processingTime = DateTime.UtcNow.Ticks - m_startTime;
                }

                if (processingTime < 0)
                    processingTime = 0;

                return processingTime.ToSeconds();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating the scheduling priority of a thread.
        /// </summary>
        /// <returns>One of the ThreadPriority values. The default value is Normal.</returns>
        /// <remarks>
        /// Changing of this value is only available to standard threads - you can't change the priorty of queued threads since they are already
        /// allocated and owned by the .NET thread pool.
        /// </remarks>
        public ThreadPriority Priority
        {
            get
            {
                if (m_type == ThreadType.QueuedThread)
                {
                    return ThreadPriority.Normal;
                }
                else
                {
                    return m_thread.Priority;
                }
            }
            set
            {
                if (m_type == ThreadType.QueuedThread)
                    throw new InvalidOperationException("Cannot change priority of a thread that was queued into thread pool");

                m_thread.Priority = value;
            }
        }

        internal Thread Thread
        {
            get
            {
                return m_thread;
            }
            set
            {
                m_thread = value;
            }
        }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Raises a ThreadAbortException in the thread on which it is invoked, to begin the process of terminating the thread. Calling this method usually terminates the thread.
        /// </summary>
        public void Abort()
        {
            ManagedThreads.Cancel(this, true, null);
        }

        /// <summary>
        /// Raises a ThreadAbortException in the thread on which it is invoked, to begin the process of terminating the thread. Calling this method usually terminates the thread.
        /// </summary>
        /// <param name="stateInfo">An object that contains application-specific information, such as state, which can be used by the thread being aborted.</param>
        public void Abort(object stateInfo)
        {
            ManagedThreads.Cancel(this, true, stateInfo);
        }

        /// <summary>
        /// Causes a thread to be scheduled for execution.
        /// </summary>
        public void Start()
        {
            if (m_type == ThreadType.QueuedThread)
                throw new InvalidOperationException("Cannot manually start a thread that was queued into thread pool");

            ManagedThreads.Add(this);

            m_thread.Start();
        }

        /// <summary>
        /// Causes a thread to be scheduled for execution.
        /// </summary>
        /// <param name="parameter">An object that contains data to be used by the method the thread executes.</param>
        public void Start(object parameter)
        {
            if (m_type == ThreadType.QueuedThread)
                throw new InvalidOperationException("Cannot manually start a thread that was queued into thread pool");

            m_state = parameter;

            ManagedThreads.Add(this);

            m_thread.Start();
        }

        /// <summary>
        /// Blocks the calling thread until a thread terminates or the specified time elapses, while continuing to perform standard COM and SendMessage pumping.
        /// </summary>
        /// <remarks>
        /// This is only available for standard threads - queued threads don't have an associated thread until they are executing.
        /// </remarks>
        public void Join()
        {
            if (m_type == ThreadType.QueuedThread)
                throw new InvalidOperationException("Cannot join a thread that was queued into thread pool");

            if (!IsAlive)
                throw new InvalidOperationException("Cannot join a thread that has not been started");

            m_thread.Join();
        }

        /// <summary>
        /// Blocks the calling thread until a thread terminates or the specified time elapses, while continuing to perform standard COM and SendMessage pumping.
        /// </summary>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait for the thread to terminate. </param>
        /// <returns>true if the thread has terminated; false if the thread has not terminated after the amount of time specified by the millisecondsTimeout parameter has elapsed.</returns>
        /// <remarks>
        /// This is only available for standard threads - queued threads don't have an associated thread until they are executing.
        /// </remarks>
        public bool Join(int millisecondsTimeout)
        {
            if (m_type == ThreadType.QueuedThread)
                throw new InvalidOperationException("Cannot join a thread that was queued into thread pool.");

            if (!IsAlive)
                throw new InvalidOperationException("Cannot join a thread that has not been started.");

            return m_thread.Join(millisecondsTimeout);
        }

        /// <summary>
        /// Blocks the calling thread until a thread terminates or the specified time elapses, while continuing to perform standard COM and SendMessage pumping.
        /// </summary>
        /// <param name="timeout">A TimeSpan set to the amount of time to wait for the thread to terminate. </param>
        /// <returns>true if the thread terminated; false if the thread has not terminated after the amount of time specified by the timeout parameter has elapsed.</returns>
        /// <remarks>
        /// This is only available for standard threads - queued threads don't have an associated thread until they are executing.
        /// </remarks>
        public bool Join(TimeSpan timeout)
        {
            return Join((int)timeout.TotalMilliseconds);
        }

        internal void HandleItem()
        {
            // Set start state
            m_startTime = DateTime.UtcNow.Ticks;
            m_status = ThreadStatus.Executing;

            try
            {
                // Invoke the user's call back function
                if ((object)m_ctx == null)
                {
                    if ((object)m_tsCallback != null)
                    {
                        m_tsCallback.Invoke();
                    }
                    else if ((object)m_ptsCallback != null)
                    {
                        m_ptsCallback.Invoke(m_state);
                    }
                    else
                    {
                        m_ctxCallback.Invoke(m_state);
                    }
                }
#if !MONO
                else
                {
                    // If user specified an alternate execution context, we invoke
                    // their delegate under that context
                    ExecutionContext.Run(m_ctx, m_ctxCallback, m_state);
                }
#endif
            }
            finally
            {
                // Set finish state
                if (m_status == ThreadStatus.Executing)
                {
                    m_status = ThreadStatus.Completed;
                }
                m_stopTime = DateTime.UtcNow.Ticks;

                ManagedThreads.Remove(this);
            }
        }

        #endregion
    }
}