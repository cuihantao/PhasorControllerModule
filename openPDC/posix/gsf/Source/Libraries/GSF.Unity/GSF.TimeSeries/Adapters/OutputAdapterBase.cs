//******************************************************************************************************
//  OutputAdapterBase.cs - Gbtc
//
//  Copyright © 2010, Grid Protection Alliance.  All Rights Reserved.
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
//  09/02/2010 - J. Ritchie Carroll
//       Generated original version of source code.
//
//******************************************************************************************************

using GSF.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace GSF.TimeSeries.Adapters
{
    /// <summary>
    /// Represents the base class for any outgoing data stream.
    /// </summary>
    /// <remarks>
    /// This base class acts as a measurement queue so that output adapters can temporarily go
    /// offline without losing any measurements to be processed. Derived classes are expected to
    /// override <see cref="ProcessMeasurements"/> to handle queued measurements.
    /// </remarks>
    public abstract class OutputAdapterBase : AdapterBase, IOutputAdapter
    {
        #region [ Members ]

        // Events

        /// <summary>
        /// Event is raised every five seconds allowing host to track total number of unprocessed measurements.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Base class reports current queue size of unprocessed measurements so that if queue size reaches
        /// an unhealthy threshold, host can take evasive action.
        /// </para>
        /// <para>
        /// <see cref="EventArgs{T}.Argument"/> is total number of unprocessed measurements.
        /// </para>
        /// </remarks>
        public event EventHandler<EventArgs<int>> UnprocessedMeasurements;

        // Fields
        private ProcessQueue<IMeasurement> m_measurementQueue;
        private List<string> m_inputSourceIDs;
        private MeasurementKey[] m_requestedInputMeasurementKeys;
        private System.Timers.Timer m_connectionTimer;
        private System.Timers.Timer m_monitorTimer;
        private bool m_disposed;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Constructs a new instance of the <see cref="OutputAdapterBase"/>.
        /// </summary>
        protected OutputAdapterBase()
        {
            m_measurementQueue = ProcessQueue<IMeasurement>.CreateRealTimeQueue(StartMeasurementProcessing);
            m_measurementQueue.ProcessException += m_measurementQueue_ProcessException;

            m_connectionTimer = new System.Timers.Timer();
            m_connectionTimer.Elapsed += m_connectionTimer_Elapsed;

            m_connectionTimer.AutoReset = false;
            m_connectionTimer.Interval = 2000;
            m_connectionTimer.Enabled = false;

            m_monitorTimer = new System.Timers.Timer();
            m_monitorTimer.Elapsed += m_monitorTimer_Elapsed;

            // We monitor total number of unarchived measurements every 5 seconds - this is a useful statistic to monitor, if
            // total number of unarchived measurements gets very large, measurement archival could be falling behind
            m_monitorTimer.Interval = 5000;
            m_monitorTimer.AutoReset = true;
            m_monitorTimer.Enabled = false;
        }

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets or sets whether or not to automatically place measurements back into the processing
        /// queue if an exception occurs while processing.  Defaults to false.
        /// </summary>
        /// <remarks>
        /// Note that items being requeued will be added to the bottom of queue by default.
        /// </remarks>
        public virtual bool RequeueOnException
        {
            get
            {
                return m_measurementQueue.RequeueOnException;
            }
            set
            {
                m_measurementQueue.RequeueOnException = value;
            }
        }

        /// <summary>
        /// Gets or sets <see cref="MeasurementKey.Source"/> values used to filter input measurements.
        /// </summary>
        /// <remarks>
        /// This allows an adapter to associate itself with entire collections of measurements based on the source of the measurement keys.
        /// Set to <c>null</c> apply no filter.
        /// </remarks>
        public virtual string[] InputSourceIDs
        {
            get
            {
                if (m_inputSourceIDs == null)
                    return null;

                return m_inputSourceIDs.ToArray();
            }
            set
            {
                if (value == null)
                {
                    m_inputSourceIDs = null;
                }
                else
                {
                    m_inputSourceIDs = new List<string>(value);
                    m_inputSourceIDs.Sort();
                }

                // Filter measurements to list of specified source IDs
                AdapterBase.LoadInputSourceIDs(this);
            }
        }

        /// <summary>
        /// Gets or sets input measurement keys that are requested by other adapters based on what adapter says it can provide.
        /// </summary>
        public virtual MeasurementKey[] RequestedInputMeasurementKeys
        {
            get
            {
                return m_requestedInputMeasurementKeys;
            }
            set
            {
                m_requestedInputMeasurementKeys = value;
            }
        }

        /// <summary>
        /// Gets the flag that determines if measurements sent to this <see cref="OutputAdapterBase"/> are destined for archival.
        /// </summary>
        /// <remarks>
        /// This property allows the <see cref="OutputAdapterCollection"/> to calculate statistics on how many measurements have
        /// been archived per minute. Historians would normally set this property to <c>true</c>; other custom exports would set
        /// this property to <c>false</c>.
        /// </remarks>
        public abstract bool OutputIsForArchive
        {
            get;
        }

        /// <summary>
        /// Gets the flag indicating if this <see cref="OutputAdapterBase"/> implementation supports temporal processing.
        /// </summary>
        /// <remarks>
        /// For output adapters that archive data it is assumed that the desired behavior will be to not support temporal processing
        /// since the data being processed has already been archived (i.e., no need to attempt to rearchive old data). As a result
        /// the default behavior for an output adapter is to not support temporal processing when <see cref="OutputIsForArchive"/>
        /// is <c>true</c>. If you have an output adapter that you want to support temporal data processing independent of the
        /// <see cref="OutputIsForArchive"/> value, then override this property and force the base value to the desired state.
        /// </remarks>
        public override bool SupportsTemporalProcessing
        {
            get
            {
                return !OutputIsForArchive;
            }
        }

        /// <summary>
        /// Gets or sets the desired processing interval, in milliseconds, for the output adapter.
        /// </summary>
        /// <remarks>
        /// With the exception of the values of -1 and 0, this value specifies the desired processing interval for data, i.e.,
        /// basically a delay, or timer interval, overwhich to process data. A value of -1 means to use the default processing
        /// interval while a value of 0 means to process data as fast as possible.
        /// </remarks>
        public override int ProcessingInterval
        {
            get
            {
                return base.ProcessingInterval;
            }
            set
            {
                if (base.ProcessingInterval != value)
                {
                    base.ProcessingInterval = value;
                    bool enabled = false;
                    bool requeueOnException = ProcessQueue<IMeasurement>.DefaultRequeueOnException;
                    IMeasurement[] unprocessedMeasurements = null;

                    if (m_measurementQueue != null)
                    {
                        enabled = m_measurementQueue.Enabled;
                        requeueOnException = m_measurementQueue.RequeueOnException;

                        if (m_measurementQueue.Count > 0)
                        {
                            m_measurementQueue.Stop();
                            unprocessedMeasurements = m_measurementQueue.ToArray();
                        }

                        m_measurementQueue.ProcessException -= m_measurementQueue_ProcessException;
                        m_measurementQueue.Dispose();
                    }

                    if (value <= 0)
                    {
                        // The default processing interval is "as fast as possible"
                        m_measurementQueue = ProcessQueue<IMeasurement>.CreateRealTimeQueue(StartMeasurementProcessing);
                    }
                    else
                    {
                        // Set the desired processing interval
                        m_measurementQueue = ProcessQueue<IMeasurement>.CreateSynchronousQueue(StartMeasurementProcessing);
                        m_measurementQueue.ProcessInterval = value;
                    }

                    m_measurementQueue.ProcessException += m_measurementQueue_ProcessException;
                    m_measurementQueue.RequeueOnException = requeueOnException;

                    // Requeue any existing measurements
                    if (unprocessedMeasurements != null && unprocessedMeasurements.Length > 0)
                        m_measurementQueue.AddRange(unprocessedMeasurements);

                    m_measurementQueue.Enabled = enabled;
                }
            }
        }

        /// <summary>
        /// Gets flag that determines if the data output stream connects asynchronously.
        /// </summary>
        /// <remarks>
        /// Derived classes should return true when data output stream connects asynchronously, otherwise return false.
        /// </remarks>
        protected abstract bool UseAsyncConnect
        {
            get;
        }

        /// <summary>
        /// Gets or sets the connection attempt interval, in milliseconds, for the data output adapter.
        /// </summary>
        protected double ConnectionAttemptInterval
        {
            get
            {
                if (m_connectionTimer != null)
                    return m_connectionTimer.Interval;

                return 2000.0D;
            }
            set
            {
                if (m_connectionTimer != null)
                    m_connectionTimer.Interval = value;
            }
        }

        /// <summary>
        /// Allows derived class access to internal processing queue.
        /// </summary>
        protected ProcessQueue<IMeasurement> InternalProcessQueue
        {
            get
            {
                return m_measurementQueue;
            }
        }

        /// <summary>
        /// Returns the detailed status of the data input source.  Derived classes should extend status with implementation specific information.
        /// </summary>
        public override string Status
        {
            get
            {
                const int MaxMeasurementsToShow = 10;

                StringBuilder status = new StringBuilder();

                status.Append(base.Status);

                if (RequestedInputMeasurementKeys != null && RequestedInputMeasurementKeys.Length > 0)
                {
                    status.AppendFormat("      Requested input keys: {0} defined measurements", RequestedInputMeasurementKeys.Length);
                    status.AppendLine();
                    status.AppendLine();

                    for (int i = 0; i < Common.Min(RequestedInputMeasurementKeys.Length, MaxMeasurementsToShow); i++)
                    {
                        status.AppendLine(RequestedInputMeasurementKeys[i].ToString().TruncateRight(25).CenterText(50));
                    }

                    if (RequestedInputMeasurementKeys.Length > MaxMeasurementsToShow)
                        status.AppendLine("...".CenterText(50));

                    status.AppendLine();
                }

                status.AppendFormat("     Source ID filter list: {0}", (m_inputSourceIDs == null ? "[No filter applied]" : m_inputSourceIDs.ToDelimitedString(',')));
                status.AppendLine();
                status.AppendFormat("   Asynchronous connection: {0}", UseAsyncConnect);
                status.AppendLine();
                status.AppendFormat("     Output is for archive: {0}", OutputIsForArchive);
                status.AppendLine();
                status.AppendFormat("   Item reporting interval: {0}", MeasurementReportingInterval);
                status.AppendLine();
                status.Append(m_measurementQueue.Status);

                return status.ToString();
            }
        }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="OutputAdapterBase"/> object and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (!m_disposed)
            {
                try
                {
                    if (disposing)
                    {
                        if (m_connectionTimer != null)
                        {
                            m_connectionTimer.Elapsed -= m_connectionTimer_Elapsed;
                            m_connectionTimer.Dispose();
                        }
                        m_connectionTimer = null;

                        if (m_monitorTimer != null)
                        {
                            m_monitorTimer.Elapsed -= m_monitorTimer_Elapsed;
                            m_monitorTimer.Dispose();
                        }
                        m_monitorTimer = null;

                        if (m_measurementQueue != null)
                        {
                            m_measurementQueue.ProcessException -= m_measurementQueue_ProcessException;
                            m_measurementQueue.Dispose();
                        }
                        m_measurementQueue = null;
                    }
                }
                finally
                {
                    m_disposed = true;          // Prevent duplicate dispose.
                    base.Dispose(disposing);    // Call base class Dispose().
                }
            }
        }

        /// <summary>
        /// Initializes <see cref="OutputAdapterBase"/>.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();

            Dictionary<string, string> settings = Settings;
            string setting;

            // Load optional parameters
            if (settings.TryGetValue("inputSourceIDs", out setting) || settings.TryGetValue("sourceids", out setting))
                InputSourceIDs = setting.Split(',');
            else
                InputSourceIDs = null;

            // Start data monitor...
            if (m_monitorTimer != null)
                m_monitorTimer.Start();
        }

        /// <summary>
        /// Initiates request for metadata refresh for <see cref="OutputAdapterBase"/>, if implemented.
        /// </summary>
        [AdapterCommand("Requests metadata refresh of output adapter.")]
        public virtual void RefreshMetadata()
        {
            // Force a recalculation of input measurement keys so that system can appropriately update routing tables
            string setting;

            if (Settings.TryGetValue("inputMeasurementKeys", out setting))
                InputMeasurementKeys = AdapterBase.ParseInputMeasurementKeys(DataSource, setting);
            else
                InputMeasurementKeys = null;

            InputSourceIDs = InputSourceIDs;
        }

        /// <summary>
        /// Starts this <see cref="OutputAdapterBase"/> and initiates connection cycle to data output stream.
        /// </summary>		
        public override void Start()
        {
            base.Start();

            // Start the connection cycle
            if (m_connectionTimer != null)
                m_connectionTimer.Enabled = true;

            // Make sure data monitor is started...
            if (m_monitorTimer != null && !m_monitorTimer.Enabled)
                m_monitorTimer.Start();
        }

        /// <summary>
        /// Attempts to connect to data output stream.
        /// </summary>
        /// <remarks>
        /// Derived classes should attempt connection to data output stream here.  Any exceptions thrown
        /// by this implementation will result in restart of the connection cycle.
        /// </remarks>
        protected abstract void AttemptConnection();

        /// <summary>
        /// Called when data input source connection is established.
        /// </summary>
        /// <remarks>
        /// Derived classes should call this method manually if <see cref="UseAsyncConnect"/> is <c>true</c>.
        /// </remarks>
        protected virtual void OnConnected()
        {
            // Start data processing thread
            if (m_measurementQueue != null)
                m_measurementQueue.Start();

            OnStatusMessage("Connection established.");
        }

        /// <summary>
        /// Stops this <see cref="OutputAdapterBase"/> and disconnects from data output stream.
        /// </summary>
        public override void Stop()
        {
            try
            {
                bool performedDisconnect = Enabled;

                // Stop the connection cycle
                if (m_connectionTimer != null)
                    m_connectionTimer.Enabled = false;

                base.Stop();

                // Stop data processing thread
                m_measurementQueue.Stop();

                // Attempt disconnection from historian (e.g., consumer to call historian API disconnect function)
                AttemptDisconnection();

                if (performedDisconnect && !UseAsyncConnect)
                    OnDisconnected();
            }
            catch (ThreadAbortException)
            {
                // This exception can be safely ignored...
            }
            catch (Exception ex)
            {
                OnProcessException(new InvalidOperationException(string.Format("Exception occured during disconnect: {0}", ex.Message), ex));
            }
        }

        /// <summary>
        /// Attempts to disconnect from data output stream.
        /// </summary>
        /// <remarks>
        /// Derived classes should attempt disconnect from data output stream here.  Any exceptions thrown
        /// by this implementation will be reported to host via <see cref="AdapterBase.ProcessException"/> event.
        /// </remarks>
        protected abstract void AttemptDisconnection();

        /// <summary>
        /// Called when data input source is disconnected.
        /// </summary>
        /// <remarks>
        /// Derived classes should call this method manually if <see cref="UseAsyncConnect"/> is <c>true</c>.
        /// </remarks>
        protected virtual void OnDisconnected()
        {
            OnStatusMessage("Disconnected.");
        }

        /// <summary>
        /// Queues a single measurement for processing. Measurement is automatically filtered to the defined <see cref="IAdapter.InputMeasurementKeys"/>.
        /// </summary>
        /// <param name="measurement">Measurement to queue for processing.</param>
        public virtual void QueueMeasurementForProcessing(IMeasurement measurement)
        {
            QueueMeasurementsForProcessing(new IMeasurement[] { measurement });
        }

        /// <summary>
        /// Queues a collection of measurements for processing. Measurements are automatically filtered to the defined <see cref="IAdapter.InputMeasurementKeys"/>.
        /// </summary>
        /// <param name="measurements">Measurements to queue for processing.</param>
        public virtual void QueueMeasurementsForProcessing(IEnumerable<IMeasurement> measurements)
        {
            if (m_disposed)
                return;

            if (!ProcessMeasurementFilter || InputMeasurementKeys == null)
            {
                // No further filtering of incoming measurement required
                m_measurementQueue.AddRange(measurements);
                IncrementProcessedMeasurements(measurements.Count());
            }
            else
            {
                // Filter measurements down to specified input measurements
                List<IMeasurement> filteredMeasurements = new List<IMeasurement>();
                foreach (IMeasurement measurement in measurements)
                {
                    if (IsInputMeasurement(measurement.Key))
                        filteredMeasurements.Add(measurement);
                }

                if (filteredMeasurements.Count > 0)
                {
                    m_measurementQueue.AddRange(filteredMeasurements);
                    IncrementProcessedMeasurements(filteredMeasurements.Count());
                }
            }
        }

        // Proxy function to process measurements after waiting for any external events, if needed
        private void StartMeasurementProcessing(IMeasurement[] measurements)
        {
            // Wait for any defined external events
            WaitForExternalEvents();

            // Call user measurement processing function
            ProcessMeasurements(measurements);
        }

        /// <summary>
        /// Serializes measurements to data output stream.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Derived classes must implement this function to process queued measurements.
        /// For example, this function would "archive" measurements if output adapter is for a historian.
        /// </para>
        /// <para>
        /// It is important that consumers "resume" connection cycle if processing fails (e.g., connection
        /// to archive is lost). Here is an example:
        /// <example>
        /// <code>
        /// protected virtual void ProcessMeasurements(IMeasurement[] measurements)
        /// {
        ///     try
        ///     {
        ///         // Process measurements...
        ///         foreach (IMeasurement measurement in measurement)
        ///         {
        ///             ArchiveMeasurement(measurement);
        ///         }
        ///     }
        ///     catch (Exception)
        ///     {                
        ///         // So long as user hasn't requested to stop, restart connection cycle
        ///         if (Enabled)
        ///             Start();
        ///     }
        /// }
        /// </code>
        /// </example>
        /// </para>
        /// </remarks>
        protected abstract void ProcessMeasurements(IMeasurement[] measurements);

        /// <summary>
        /// This removes a range of measurements from the internal measurement queue.
        /// </summary>
        /// <remarks>
        /// This method is typically only used to curtail size of measurement queue if it's getting too large.  If more points are
        /// requested than there are points available - all points in the queue will be removed.
        /// </remarks>
        public virtual void RemoveMeasurements(int total)
        {
            if (m_disposed)
                return;

            if (total > m_measurementQueue.Count)
                total = m_measurementQueue.Count;

            IMeasurement measurement;

            for (int i = 0; i < total; i++)
            {
                m_measurementQueue.TryTake(out measurement);
            }
        }

        /// <summary>
        /// Blocks the current thread, if the <see cref="OutputAdapterBase"/> is connected, until all items
        /// in <see cref="OutputAdapterBase"/> queue are processed, and then stops processing.
        /// </summary>
        /// <remarks>
        /// <para>
        /// It is possible for items to be added to the queue while the flush is executing. The flush will continue to
        /// process items as quickly as possible until the queue is empty. Unless the user stops queueing items to be
        /// processed, the flush call may never return (not a happy situtation on shutdown).
        /// </para>
        /// <para>
        /// The <see cref="OutputAdapterBase"/> does not clear queue prior to destruction. If the user fails to call
        /// this method before the class is destructed, there may be items that remain unprocessed in the queue.
        /// </para>
        /// </remarks>
        public virtual void Flush()
        {
            if (m_measurementQueue != null)
                m_measurementQueue.Flush();
        }

        /// <summary>
        /// Raises the <see cref="UnprocessedMeasurements"/> event.
        /// </summary>
        /// <param name="unprocessedMeasurements">Total measurements in the queue that have not been processed.</param>
        protected virtual void OnUnprocessedMeasurements(int unprocessedMeasurements)
        {
            try
            {
                if (UnprocessedMeasurements != null)
                    UnprocessedMeasurements(this, new EventArgs<int>(unprocessedMeasurements));
            }
            catch (Exception ex)
            {
                // We protect our code from consumer thrown exceptions
                OnProcessException(new InvalidOperationException(string.Format("Exception in consumer handler for UnprocessedMeasurements event: {0}", ex.Message), ex));
            }
        }

        /// <summary>
        /// Raises <see cref="AdapterBase.ProcessException"/> event.
        /// </summary>
        /// <param name="ex">Processing <see cref="Exception"/>.</param>
        protected override void OnProcessException(Exception ex)
        {
            base.OnProcessException(ex);
        }

        private void m_connectionTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                // So long as user hasn't requested to stop, attempt connection
                if (Enabled)
                {
                    OnStatusMessage("Attempting connection...");

                    // Attempt connection to data output adapter (e.g., call historian API connect function).
                    AttemptConnection();

                    if (!UseAsyncConnect)
                        OnConnected();
                }
            }
            catch (ThreadAbortException)
            {
                // This exception can be safely ignored...
            }
            catch (Exception ex)
            {
                OnProcessException(new InvalidOperationException(string.Format("Connection attempt failed: {0}", ex.Message), ex));

                // So long as user hasn't requested to stop, keep trying connection
                if (Enabled)
                    Start();
            }
        }

        // All we do here is expose the total number of unarchived measurements in the queue
        private void m_monitorTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            OnUnprocessedMeasurements(m_measurementQueue.Count);
        }

        // Bubble any exceptions occuring in the process queue to the base class event
        private void m_measurementQueue_ProcessException(object sender, EventArgs<Exception> e)
        {
            OnProcessException(e.Argument);
        }

        #endregion
    }
}