﻿//******
//  DataGapRecoverer.cs - Gbtc
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
//  06/27/2014 - J. Ritchie Carroll
//       Generated original version of source code.
//
//******************************************************************************************************

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using GSF.Annotations;
using GSF.IO;
using GSF.Units;
using Timer = System.Timers.Timer;

namespace GSF.TimeSeries.Transport
{
    /// <summary>
    /// Represents a data gap recovery module. 
    /// </summary>
    /// <remarks>
    /// <para>
    /// Data gaps will be recovered using an unsynchronized temporal subscription.
    /// </para>
    /// <para>
    /// This class expects that source historian that feeds temporal subscription
    /// will recover data in time-sorted order.
    /// </para>
    /// </remarks>
    public class DataGapRecoverer : ISupportLifecycle, IProvideStatus
    {
        #region [ Members ]

        // Constants

        /// <summary>
        /// Default value for <see cref="RecoveryStartDelay"/>.
        /// </summary>
        public const double DefaultRecoveryStartDelay = 20.0D;

        /// <summary>
        /// Default value for <see cref="DataMonitoringInterval"/>.
        /// </summary>
        public const double DefaultDataMonitoringInterval = 10.0D;

        /// <summary>
        /// Default value for <see cref="MinimumRecoverySpan"/>.
        /// </summary>
        public const double DefaultMinimumRecoverySpan = 30.0D;

        /// <summary>
        /// Default value for <see cref="MaximumRecoverySpan"/>.
        /// </summary>
        public const double DefaultMaximumRecoverySpan = Time.SecondsPerDay * 10;

        /// <summary>
        /// Default value for <see cref="FilterExpression"/>.
        /// </summary>
        public const string DefaultFilterExpression = "FILTER ActiveMeasurements WHERE Internal <> 0";

        /// <summary>
        /// Default value for <see cref="RecoveryProcessingInterval"/>.
        /// </summary>
        public const int DefaultRecoveryProcessingInterval = 66;

        /// <summary>
        /// Default value for <see cref="UseMillisecondResolution"/>.
        /// </summary>
        public const bool DefaultUseMillisecondResolution = true;

        // Events

        /// <summary>
        /// Provides recovered measurements from temporal subscription.
        /// </summary>
        /// <remarks>
        /// <see cref="EventArgs{T}.Argument"/> is a collection of measurements for consumer to process.
        /// </remarks>
        public event EventHandler<EventArgs<ICollection<IMeasurement>>> RecoveredMeasurements;

        /// <summary>
        /// Provides status messages to consumer.
        /// </summary>
        /// <remarks>
        /// <see cref="EventArgs{T}.Argument"/> is new status message.
        /// </remarks>
        public event EventHandler<EventArgs<string>> StatusMessage;

        /// <summary>
        /// Event is raised when there is an exception encountered during <see cref="DataGapRecoverer"/> processing.
        /// </summary>
        /// <remarks>
        /// <see cref="EventArgs{T}.Argument"/> is the exception that was thrown.
        /// </remarks>
        public event EventHandler<EventArgs<Exception>> ProcessException;

        /// <summary>
        /// Raised after the <see cref="DataGapRecoverer"/> has been properly disposed.
        /// </summary>
        public event EventHandler Disposed;

        // Fields
        private readonly UnsynchronizedSubscriptionInfo m_subscriptionInfo;
        private ManualResetEventSlim m_dataGapRecoveryCompleted;
        private DataSubscriber m_temporalSubscription;
        private OutageLogProcessor m_dataGapLogProcessor;
        private OutageLog m_dataGapLog;
        private Timer m_dataStreamMonitor;
        private DataSet m_dataSource;
        private string m_sourceConnectionName;
        private string m_connectionString;
        private Time m_recoveryStartDelay;
        private Time m_minimumRecoverySpan;
        private Time m_maximumRecoverySpan;
        private Ticks m_mostRecentRecoveredTime;
        private long m_measurementsRecoveredForDataGap;
        private long m_measurementsRecoveredOverLastInterval;
        private volatile bool m_abnormalTermination;
        private volatile bool m_enabled;
        private volatile bool m_connected;
        private bool m_disposed;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Creates a new <see cref="DataGapRecoverer"/>.
        /// </summary>
        public DataGapRecoverer()
        {
            m_dataGapRecoveryCompleted = new ManualResetEventSlim(false);

            m_recoveryStartDelay = DefaultRecoveryStartDelay;
            m_minimumRecoverySpan = DefaultMinimumRecoverySpan;
            m_maximumRecoverySpan = DefaultMaximumRecoverySpan;

            m_subscriptionInfo = new UnsynchronizedSubscriptionInfo(false);
            m_subscriptionInfo.FilterExpression = DefaultFilterExpression;
            m_subscriptionInfo.ProcessingInterval = DefaultRecoveryProcessingInterval;
            m_subscriptionInfo.UseMillisecondResolution = DefaultUseMillisecondResolution;

            m_dataStreamMonitor = new Timer();
            m_dataStreamMonitor.Elapsed += DataStreamMonitor_Elapsed;
            m_dataStreamMonitor.Interval = DefaultDataMonitoringInterval * 1000.0D;
            m_dataStreamMonitor.AutoReset = true;
            m_dataStreamMonitor.Enabled = false;
        }

        /// <summary>
        /// Releases the unmanaged resources before the <see cref="DataGapRecoverer"/> object is reclaimed by <see cref="GC"/>.
        /// </summary>
        ~DataGapRecoverer()
        {
            Dispose(false);
        }

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets or sets name of source connection device (e.g., a data subscriber).
        /// </summary>
        /// <remarks>
        /// This name will be used to create the <see cref="OutageLog.FileName"/>.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Value cannot be null or an empty string.</exception>
        public string SourceConnectionName
        {
            get
            {
                return m_sourceConnectionName;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentOutOfRangeException("value", "SourceConnectionName cannot be null or an empty string.");

                m_sourceConnectionName = value;
            }
        }

        /// <summary>
        /// Gets or sets <see cref="DataSet"/> based data source available to this <see cref="DataGapRecoverer"/>.
        /// </summary>
        public DataSet DataSource
        {
            get
            {
                return m_dataSource;
            }
            set
            {
                m_dataSource = value;
            }
        }

        /// <summary>
        /// Gets or sets connection string that will be used to make a temporal subscription when recovering data for an <see cref="Outage"/>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Value cannot be null or an empty string.</exception>
        public string ConnectionString
        {
            get
            {
                return m_connectionString;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentOutOfRangeException("value", "ConnectionString cannot be null or an empty string.");

                m_connectionString = value;
            }
        }

        /// <summary>
        /// Gets or sets the minimum time delay, in seconds, to wait before starting the data recovery for an <see cref="Outage"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// For some archiving systems it may take a few seconds for data to make it to disk and therefore be readily
        /// available for a temporal subscription query. The <see cref="RecoveryStartDelay"/> should be adjusted based
        /// on the nature of the system used to archive data. If the archival system makes data immediately available
        /// because of internal caching or other means, this value can be zero.
        /// </para>
        /// <para>
        /// Use of this value depends on the local clock, as such the value should be increased by the uncertainty of
        /// accuracy of the local clock. For example, if it is know that the local clock floats +/-5 seconds from
        /// real-time, then increase the desired value of the <see cref="RecoveryStartDelay"/> by 5 seconds.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Value cannot be a negative number.</exception>
        public Time RecoveryStartDelay
        {
            get
            {
                return m_recoveryStartDelay;
            }
            set
            {
                if (value < 0.0D)
                    throw new ArgumentOutOfRangeException("value", "RecoveryStartDelay cannot be a negative number.");

                m_recoveryStartDelay = value;
            }
        }

        /// <summary>
        /// Gets or sets the interval, in seconds, over which the data monitor will check for new data.
        /// </summary>
        /// <remarks>
        /// Once a connection is established a timer is enabled to monitor for new incoming data. The data monitoring timer
        /// exists to make sure data is being received so that the process of recovery does not wait endlessly for data that
        /// may never come because of a possible error in the recovery process. The <see cref="DataMonitoringInterval"/>
        /// allows the consumer to adjust the interval over which the timer will check for new incoming data.
        /// <para>
        /// It will take some time, perhaps a couple of seconds, to start the temporal subscription and begin the process
        /// of recovering data for an <see cref="Outage"/>. Make sure the value for <see cref="DataMonitoringInterval"/> is
        /// sufficiently large enough to handle any initial delays in data transmission.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">Value cannot be zero or a negative number.</exception>
        public Time DataMonitoringInterval
        {
            get
            {
                return m_dataStreamMonitor.Interval / 1000.0D;
            }
            set
            {
                if ((object)m_dataStreamMonitor == null)
                    throw new ArgumentNullException();

                if (value <= 0.0D)
                    throw new ArgumentOutOfRangeException("value", "DataMonitoringInterval cannot be zero or a negative number.");

                m_dataStreamMonitor.Interval = value * 1000.0D;
            }
        }

        /// <summary>
        /// Gets to sets the minimum time span, in seconds, for which a data recovery will be attempted.
        /// Set to zero for no minimum.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Value cannot be a negative number.</exception>
        public Time MinimumRecoverySpan
        {
            get
            {
                return m_minimumRecoverySpan;
            }
            set
            {
                if (value < 0.0D)
                    throw new ArgumentOutOfRangeException("value", "MinimumRecoverySpan cannot be a negative number.");

                m_minimumRecoverySpan = value;
            }
        }

        /// <summary>
        /// Gets to sets the maximum time span, in seconds, for which a data recovery will be attempted.
        /// Set to <see cref="Double.MaxValue"/> for no maximum.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Value cannot be zero or a negative number.</exception>
        public Time MaximumRecoverySpan
        {
            get
            {
                return m_maximumRecoverySpan;
            }
            set
            {
                if (value <= 0.0D)
                    throw new ArgumentOutOfRangeException("value", "MaximumRecoverySpan cannot be zero or a negative number.");

                m_maximumRecoverySpan = value;
            }
        }

        /// <summary>
        /// Gets or sets the filter expression used to define which measurements are being requested for data recovery during an <see cref="Outage"/>.
        /// </summary>
        public string FilterExpression
        {
            get
            {
                return m_subscriptionInfo.FilterExpression;
            }
            set
            {
                m_subscriptionInfo.FilterExpression = value;
            }
        }

        /// <summary>
        /// Gets or sets the data recovery processing interval, in whole milliseconds, to use in the temporal data
        /// subscription when recovering data for an <see cref="Outage"/>.<br/>
        /// A value of <c>-1</c> indicates the default processing interval will be requested.<br/>
        /// A value of <c>0</c> indicates data will be processed as fast as possible.
        /// </summary>
        /// <remarks>
        /// With the exception of the values of -1 and 0, the <see cref="RecoveryProcessingInterval"/> value specifies
        /// the desired historical data playback processing interval in milliseconds. This is basically a delay, or timer
        /// interval, over which to process data. Setting this value to -1 means to use the default processing interval
        /// while setting the value to 0 means to process data as fast as possible, i.e., as fast as the historian can
        /// query the data. Depending on the available bandwidth, this parameter may need to be adjusted such that the
        /// data being recovered does not adversely interfere with the ongoing transmission of real-time data.
        /// </remarks>
        public int RecoveryProcessingInterval
        {
            get
            {
                return m_subscriptionInfo.ProcessingInterval;
            }
            set
            {
                m_subscriptionInfo.ProcessingInterval = value;
            }
        }

        /// <summary>
        /// Gets or sets the flag that determines whether measurement timestamps use millisecond resolution.
        /// If false, time will be of <see cref="Ticks"/> resolution.
        /// </summary>
        /// <remarks>
        /// If the source and destination historians can handle timestamps at a greater than millisecond resolution then
        /// the <see cref="UseMillisecondResolution"/> can be set to <c>false</c> to ensure that a full resolution timestamp
        /// is delivered through the data recovery process. Setting this property to <c>true</c> allows the temporal subscription
        /// used in the data recovery process to conserve data transmission bandwidth since not as much space will be needed for
        /// a timestamp with only millisecond resolution.
        /// </remarks>
        public bool UseMillisecondResolution
        {
            get
            {
                return m_subscriptionInfo.UseMillisecondResolution;
            }
            set
            {
                m_subscriptionInfo.UseMillisecondResolution = value;
            }
        }

        /// <summary>
        /// Gets or sets any additional constraint parameters that will be supplied to adapters in temporal
        /// subscription used when recovering data for an <see cref="Outage"/>.
        /// </summary>
        public string ConstraintParameters
        {
            get
            {
                return m_subscriptionInfo.ConstraintParameters;
            }
            set
            {
                m_subscriptionInfo.ConstraintParameters = value;
            }
        }

        /// <summary>
        /// Gets or sets a boolean value that indicates whether the <see cref="DataGapRecoverer"/> is enabled.
        /// </summary>
        public bool Enabled
        {
            get
            {
                return m_enabled;
            }
            set
            {
                m_enabled = value;

                if ((object)m_dataGapLog != null)
                    m_dataGapLog.Enabled = m_enabled;

                if ((object)m_dataGapLogProcessor != null)
                    m_dataGapLogProcessor.Enabled = m_enabled;

                if ((object)m_temporalSubscription != null)
                    m_temporalSubscription.Enabled = m_enabled;
            }
        }

        /// <summary>
        /// Gets reference to the data gap <see cref="OutageLog"/> for this <see cref="DataGapRecoverer"/>.
        /// </summary>
        protected OutageLog DataGapLog
        {
            get
            {
                return m_dataGapLog;
            }
        }

        /// <summary>
        /// Gets reference to the data gap <see cref="OutageLogProcessor"/> for this <see cref="DataGapRecoverer"/>.
        /// </summary>
        protected OutageLogProcessor DataGapLogProcessor
        {
            get
            {
                return m_dataGapLogProcessor;
            }
        }

        // Gets the name of the data gap recoverer.
        string IProvideStatus.Name
        {
            get
            {
                if ((object)m_temporalSubscription != null)
                    return m_temporalSubscription.Name;

                return GetType().Name;
            }
        }

        /// <summary>
        /// Gets the status of the temporal <see cref="DataSubscriber"/> used to query historical data.
        /// </summary>
        public string TemporalSubscriptionStatus
        {
            get
            {
                return (object)m_temporalSubscription != null ? m_temporalSubscription.Status : "undefined";
            }
        }

        /// <summary>
        /// Gets the status of this <see cref="DataGapRecoverer"/>.
        /// </summary>
        public string Status
        {
            get
            {
                StringBuilder status = new StringBuilder();

                status.AppendFormat(" Data recovery start delay: {0} seconds", RecoveryStartDelay.ToString(2));
                status.AppendLine();
                status.AppendFormat("  Data monitoring interval: {0} seconds", DataMonitoringInterval.ToString(2));
                status.AppendLine();
                status.AppendFormat("Minimum data recovery span: {0} seconds", MinimumRecoverySpan.ToString(2));
                status.AppendLine();
                status.AppendFormat("Maximum data recovery span: {0} seconds", MaximumRecoverySpan.ToString(2));
                status.AppendLine();
                status.AppendFormat("Recovery filter expression: {0}", FilterExpression.TruncateRight(51));
                status.AppendLine();
                status.AppendFormat(" Recovery processing speed: {0}", RecoveryProcessingInterval < 0 ? "Default" : (RecoveryProcessingInterval == 0 ? "As fast as possible" : RecoveryProcessingInterval.ToString("N0") + " milliseconds"));
                status.AppendLine();
                status.AppendFormat("Use millisecond resolution: {0}", UseMillisecondResolution);
                status.AppendLine();

                if ((object)m_temporalSubscription != null)
                {
                    status.AppendLine();
                    status.AppendLine("Data Gap Temporal Subscription Status".CenterText(50));
                    status.AppendLine("-------------------------------------".CenterText(50));
                    status.AppendFormat(m_temporalSubscription.Status);
                }

                if ((object)m_dataGapLog != null)
                {
                    status.AppendLine();
                    status.AppendLine("Data Gap Log Status".CenterText(50));
                    status.AppendLine("-------------------".CenterText(50));
                    status.AppendFormat(m_dataGapLog.Status);
                }

                return status.ToString();
            }
        }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Releases all the resources used by the <see cref="DataGapRecoverer"/> object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="DataGapRecoverer"/> object and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!m_disposed)
            {
                try
                {
                    if (disposing)
                    {
                        if ((object)m_dataGapRecoveryCompleted != null)
                        {
                            // Signal any waiting threads
                            m_abnormalTermination = true;
                            m_dataGapRecoveryCompleted.Set();
                            m_dataGapRecoveryCompleted.Dispose();
                            m_dataGapRecoveryCompleted = null;
                        }

                        if ((object)m_dataStreamMonitor != null)
                        {
                            m_dataStreamMonitor.Elapsed -= DataStreamMonitor_Elapsed;
                            m_dataStreamMonitor.Dispose();
                            m_dataStreamMonitor = null;
                        }

                        if ((object)m_dataGapLogProcessor != null)
                        {
                            m_dataGapLogProcessor.Dispose();
                            m_dataGapLogProcessor = null;
                        }

                        if ((object)m_dataGapLog != null)
                        {
                            m_dataGapLog.ProcessException -= Common_ProcessException;
                            m_dataGapLog.Dispose();
                            m_dataGapLog = null;
                        }

                        if ((object)m_temporalSubscription != null)
                        {
                            m_temporalSubscription.StatusMessage -= Common_StatusMessage;
                            m_temporalSubscription.ProcessException -= Common_ProcessException;
                            m_temporalSubscription.ConnectionEstablished -= TemporalSubscription_ConnectionEstablished;
                            m_temporalSubscription.ConnectionTerminated -= TemporalSubscription_ConnectionTerminated;
                            m_temporalSubscription.NewMeasurements -= TemporalSubscription_NewMeasurements;
                            m_temporalSubscription.ProcessingComplete -= TemporalSubscription_ProcessingComplete;
                            m_temporalSubscription.Dispose();
                            m_temporalSubscription = null;
                        }
                    }
                }
                finally
                {
                    m_disposed = true;  // Prevent duplicate dispose.

                    if ((object)Disposed != null)
                        Disposed(this, EventArgs.Empty);
                }
            }
        }

        /// <summary>
        /// Initializes the <see cref="DataGapRecoverer"/>.
        /// </summary>
        public void Initialize()
        {
            if (m_disposed)
                throw new InvalidOperationException("Data gap recoverer has been disposed. Cannot initialize.");

            Dictionary<string, string> settings = m_connectionString.ToNonNullString().ParseKeyValuePairs();
            string setting;
            double timeInterval;
            int processingInterval;

            if (settings.TryGetValue("sourceConnectionName", out setting) && !string.IsNullOrWhiteSpace(setting))
                m_sourceConnectionName = setting;

            if (settings.TryGetValue("recoveryStartDelay", out setting) && double.TryParse(setting, out timeInterval))
                RecoveryStartDelay = timeInterval;

            if (settings.TryGetValue("dataMonitoringInterval", out setting) && double.TryParse(setting, out timeInterval))
                DataMonitoringInterval = timeInterval;

            if (settings.TryGetValue("minimumRecoverySpan", out setting) && double.TryParse(setting, out timeInterval))
                MinimumRecoverySpan = timeInterval;

            if (settings.TryGetValue("maximumRecoverySpan", out setting) && double.TryParse(setting, out timeInterval))
                MaximumRecoverySpan = timeInterval;

            if (settings.TryGetValue("filterExpression", out setting) && !string.IsNullOrWhiteSpace(setting))
                FilterExpression = setting;

            if (settings.TryGetValue("recoveryProcessingInterval", out setting) && int.TryParse(setting, out processingInterval))
                RecoveryProcessingInterval = processingInterval;

            if (settings.TryGetValue("useMillisecondResolution", out setting))
                UseMillisecondResolution = setting.ParseBoolean();

            if (string.IsNullOrEmpty(m_sourceConnectionName))
                throw new NullReferenceException("Source connection name must defined - it is used to create outage log file name.");

            // Setup a new temporal data subscriber that will be used to query historical data
            m_temporalSubscription = new DataSubscriber();
            m_temporalSubscription.Name = m_sourceConnectionName + "!" + GetType().Name;
            m_temporalSubscription.DataSource = m_dataSource;
            m_temporalSubscription.ConnectionString = m_connectionString;
            m_temporalSubscription.StatusMessage += Common_StatusMessage;
            m_temporalSubscription.ProcessException += Common_ProcessException;
            m_temporalSubscription.ConnectionEstablished += TemporalSubscription_ConnectionEstablished;
            m_temporalSubscription.ConnectionTerminated += TemporalSubscription_ConnectionTerminated;
            m_temporalSubscription.ProcessingComplete += TemporalSubscription_ProcessingComplete;
            m_temporalSubscription.NewMeasurements += TemporalSubscription_NewMeasurements;
            m_temporalSubscription.Initialize();

            // Setup data gap outage log to persist unprocessed outages between class life-cycles
            m_dataGapLog = new OutageLog();
            m_dataGapLog.FileName = m_sourceConnectionName + "_OutageLog.txt";
            m_dataGapLog.ProcessException += Common_ProcessException;
            m_dataGapLog.Initialize();

            // Setup data gap processor to process items one at a time, a 5-second minimum period is established between each gap processing
            m_dataGapLogProcessor = new OutageLogProcessor(m_dataGapLog, ProcessDataGap, CanProcessDataGap, OnProcessException, GSF.Common.Max(5000, (int)(m_recoveryStartDelay * SI.Milli)));
        }

        /// <summary>
        /// Logs a new data gap for processing.
        /// </summary>
        /// <param name="startTime">Start time of data gap.</param>
        /// <param name="endTime">End time of data gap.</param>
        /// <returns><c>true</c> if data gap was logged for processing; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// Data gap will not be logged for processing if the <paramref name="startTime"/> and <paramref name="endTime"/> do not represent
        /// a valid time span for recovery according to <see cref="MinimumRecoverySpan"/> and <see cref="MaximumRecoverySpan"/>.
        /// </remarks>
        public bool LogDataGap(DateTime startTime, DateTime endTime)
        {
            if (m_disposed)
                throw new InvalidOperationException("Data gap recoverer has been disposed. Cannot log data gap for processing.");

            if ((object)m_dataGapLog == null)
                throw new InvalidOperationException("Data gap recoverer has not been initialized. Cannot log data gap for processing.");

            OnStatusMessage("Data gap recovery requested for period \"{0}\" - \"{1}\"...", startTime.ToString(OutageLog.DateTimeFormat, CultureInfo.InvariantCulture), endTime.ToString(OutageLog.DateTimeFormat, CultureInfo.InvariantCulture));

            Time dataGapSpan = (endTime - startTime).TotalSeconds;

            // Only log data gap for processing if it is in an acceptable time span for recovery
            if (dataGapSpan >= m_minimumRecoverySpan && dataGapSpan <= m_maximumRecoverySpan)
            {
                // Since local clock may float we add some buffer around recovery window
                m_dataGapLog.Add(new Outage(startTime.AddSeconds(-10.0D), endTime.AddSeconds(10.0D)));
                return true;
            }

            Time rangeLimit;
            string rangeLimitText;

            if (dataGapSpan < m_minimumRecoverySpan)
            {
                rangeLimit = m_minimumRecoverySpan;
                rangeLimitText = "minimum";
            }
            else
            {
                rangeLimit = m_maximumRecoverySpan;
                rangeLimitText = "maximum";
            }

            OnStatusMessage("Skipped data gap recovery for {0} of missed data - outside configured {1} range of {2}.", Time.ToElapsedTimeString(dataGapSpan, 2), rangeLimitText, Time.ToElapsedTimeString(rangeLimit, 2));

            return false;
        }

        /// <summary>
        /// Queues up a flush to happen asynchronously.
        /// </summary>
        public void FlushLogAsync()
        {
            ThreadPool.QueueUserWorkItem(start => FlushLog());
        }

        /// <summary>
        /// Blocks calling thread until data gap <see cref="OutageLog"/> has been flushed to disk.
        /// </summary>
        /// <param name="timeout">Optional time-out for waiting thread block. Defaults to waiting indefinitely.</param>
        /// <remarks>
        /// <para>
        /// Data gap log is automatically persisted to disk as <see cref="Outage"/> items are added or removed from the log.
        /// This function only exists to force a flush and block calling thread until flush has completed.
        /// </para>
        /// <para>
        /// Function first waits for any pending data gap operation to complete then waits for data gap log to
        /// be flushed. Both waits use the same <paramref name="timeout"/> value, as a result it is possible that
        /// total wait time could be longer than specified wait time.
        /// </para>
        /// </remarks>
        /// <returns><c>true</c> if log was flushed successfully; otherwise, <c>false</c> if flush timed out.</returns>
        public bool FlushLog(int timeout = Timeout.Infinite)
        {
            if (m_disposed)
                throw new InvalidOperationException("Data gap recoverer has been disposed. Cannot flush log.");

            if ((object)m_dataGapLog == null)
                throw new InvalidOperationException("Data gap recoverer has not been initialized. Cannot flush log.");

            // Wait for process completion if in progress
            if (m_dataGapRecoveryCompleted.Wait(timeout))
                return m_dataGapLog.Flush().Wait(timeout);

            return false;
        }

        // Can only start data gap processing when end time of recovery range is beyond recovery start delay
        private bool CanProcessDataGap(Outage dataGap)
        {
            return m_connected && Enabled && (DateTime.UtcNow - dataGap.EndTime).TotalSeconds > m_recoveryStartDelay;
        }

        // Any exceptions in this handler will be exposed through ProcessException event and cause OutageLogProcessor
        // to requeue the data gap outage so it will be processed again (could be that remote system is offline).
        private void ProcessDataGap(Outage dataGap)
        {
            // Establish start and stop time for temporal session
            m_subscriptionInfo.StartTime = dataGap.StartTime.ToString(OutageLog.DateTimeFormat, CultureInfo.InvariantCulture);
            m_subscriptionInfo.StopTime = dataGap.EndTime.ToString(OutageLog.DateTimeFormat, CultureInfo.InvariantCulture);

            OnStatusMessage("Starting data gap recovery for period \"{0}\" - \"{1}\"...", m_subscriptionInfo.StartTime, m_subscriptionInfo.StopTime);

            // Enable data monitor            
            m_dataStreamMonitor.Enabled = true;

            // Reset measurement counters
            m_measurementsRecoveredForDataGap = 0;
            m_measurementsRecoveredOverLastInterval = 0;

            // Reset processing fields
            m_mostRecentRecoveredTime = dataGap.StartTime.Ticks;
            m_abnormalTermination = false;

            // Reset process completion wait handle
            m_dataGapRecoveryCompleted.Reset();

            // Start temporal data recovery session
            m_temporalSubscription.Subscribe(m_subscriptionInfo);

            // Wait for process completion - success or fail
            m_dataGapRecoveryCompleted.Wait();

            // If temporal session failed to connect, retry data recovery for this outage
            if (m_abnormalTermination)
            {
                // Make sure any data recovered so far doesn't get unnecessarily re-recovered, this requires that source historian report data in time-sorted order
                dataGap.StartTime = new DateTime(GSF.Common.Max((Ticks)dataGap.StartTime.Ticks, m_mostRecentRecoveredTime - (m_subscriptionInfo.UseMillisecondResolution ? Ticks.PerMillisecond : 1L)), DateTimeKind.Utc);

                // Re-insert adjusted data gap at the top of the processing queue
                m_dataGapLog.Insert(0, dataGap);
                FlushLogAsync();

                if (m_measurementsRecoveredForDataGap == 0)
                    OnStatusMessage("WARNING: Failed to establish temporal session. Data recovery for period \"{0}\" - \"{1}\" will be re-attempted.", m_subscriptionInfo.StartTime, m_subscriptionInfo.StopTime);
                else
                    OnStatusMessage("WARNING: Temporal session was disconnected during recovery operation. Data recovery for adjusted period \"{0}\" - \"{1}\" will be re-attempted.", dataGap.StartTime.ToString(OutageLog.DateTimeFormat, CultureInfo.InvariantCulture), m_subscriptionInfo.StopTime);
            }

            // Disconnect temporal session
            m_temporalSubscription.Stop();

            // Disable data monitor            
            m_dataStreamMonitor.Enabled = false;

            OnStatusMessage("{0}Recovered {1} measurements for period \"{2}\" - \"{3}\".", m_measurementsRecoveredForDataGap == 0 ? "WARNING: " : "", m_measurementsRecoveredForDataGap, m_subscriptionInfo.StartTime, m_subscriptionInfo.StopTime);
        }

        /// <summary>
        /// Raises the <see cref="RecoveredMeasurements"/> event.
        /// </summary>
        protected virtual void OnRecoveredMeasurements(ICollection<IMeasurement> measurements)
        {
            try
            {
                if (RecoveredMeasurements != null)
                    RecoveredMeasurements(this, new EventArgs<ICollection<IMeasurement>>(measurements));
            }
            catch (Exception ex)
            {
                // We protect our code from consumer thrown exceptions
                OnProcessException(new InvalidOperationException(string.Format("Exception in consumer handler for RecoveredMeasurements event: {0}", ex.Message), ex));
            }
        }

        /// <summary>
        /// Raises the <see cref="StatusMessage"/> event.
        /// </summary>
        /// <param name="status">New status message.</param>
        protected virtual void OnStatusMessage(string status)
        {
            try
            {
                if ((object)StatusMessage != null)
                    StatusMessage(this, new EventArgs<string>(status));
            }
            catch (Exception ex)
            {
                // We protect our code from consumer thrown exceptions
                OnProcessException(new InvalidOperationException(string.Format("Exception in consumer handler for StatusMessage event: {0}", ex.Message), ex));
            }
        }

        /// <summary>
        /// Raises the <see cref="StatusMessage"/> event with a formatted status message.
        /// </summary>
        /// <param name="formattedStatus">Formatted status message.</param>
        /// <param name="args">Arguments for <paramref name="formattedStatus"/>.</param>
        /// <remarks>
        /// This overload combines string.Format and SendStatusMessage for convenience.
        /// </remarks>
        [StringFormatMethod("formattedStatus")]
        protected virtual void OnStatusMessage(string formattedStatus, params object[] args)
        {
            try
            {
                if ((object)StatusMessage != null)
                    StatusMessage(this, new EventArgs<string>(string.Format(formattedStatus, args)));
            }
            catch (Exception ex)
            {
                // We protect our code from consumer thrown exceptions
                OnProcessException(new InvalidOperationException(string.Format("Exception in consumer handler for StatusMessage event: {0}", ex.Message), ex));
            }
        }

        /// <summary>
        /// Raises <see cref="ProcessException"/> event.
        /// </summary>
        /// <param name="ex">Processing <see cref="Exception"/>.</param>
        protected virtual void OnProcessException(Exception ex)
        {
            if ((object)ProcessException != null)
                ProcessException(this, new EventArgs<Exception>(ex));
        }

        private void TemporalSubscription_ConnectionEstablished(object sender, EventArgs e)
        {
            m_connected = true;
        }

        private void TemporalSubscription_ConnectionTerminated(object sender, EventArgs e)
        {
            m_connected = false;

            try
            {
                // Disable data monitor            
                m_dataStreamMonitor.Enabled = false;

                // If temporal subscription is currently enabled - connection termination was not expected
                if ((object)m_temporalSubscription != null)
                    m_abnormalTermination = m_temporalSubscription.Enabled;
            }
            finally
            {
                if ((object)m_dataGapRecoveryCompleted != null)
                    m_dataGapRecoveryCompleted.Set();
            }
        }

        private void TemporalSubscription_ProcessingComplete(object sender, EventArgs<string> e)
        {
            OnStatusMessage("Temporal data recovery processing completed.");

            if ((object)m_dataGapRecoveryCompleted != null)
                m_dataGapRecoveryCompleted.Set();

            m_dataStreamMonitor.Enabled = false;
        }

        private void TemporalSubscription_NewMeasurements(object sender, EventArgs<ICollection<IMeasurement>> e)
        {
            ICollection<IMeasurement> measurements = e.Argument;
            int total = measurements.Count;

            if (total > 0)
            {
                m_measurementsRecoveredForDataGap += total;
                m_measurementsRecoveredOverLastInterval += total;

                // Publish recovered measurements back to consumer
                OnRecoveredMeasurements(measurements);

                // Track latest reporting time
                long mostRecentRecoveredTime = measurements.Select(m => (long)m.Timestamp).Max();

                if (mostRecentRecoveredTime > m_mostRecentRecoveredTime)
                    m_mostRecentRecoveredTime = mostRecentRecoveredTime;
            }

            // See if consumer has requested to stop recovery operations
            if (!m_enabled)
            {
                OnStatusMessage("Data gap recovery has been canceled.");

                m_abnormalTermination = true;

                if ((object)m_dataGapRecoveryCompleted != null)
                    m_dataGapRecoveryCompleted.Set();

                m_dataStreamMonitor.Enabled = false;
            }
        }

        private void Common_StatusMessage(object sender, EventArgs<string> e)
        {
            OnStatusMessage(e.Argument);
        }

        private void Common_ProcessException(object sender, EventArgs<Exception> e)
        {
            OnProcessException(e.Argument);
        }

        private void DataStreamMonitor_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (m_measurementsRecoveredOverLastInterval == 0)
            {
                // If we've received no measurements in the last time-span, we cancel current process...
                m_dataStreamMonitor.Enabled = false;
                OnStatusMessage("\r\nWARNING: No data received in {0} seconds, canceling current data recovery operation...\r\n", (m_dataStreamMonitor.Interval / 1000.0D).ToString("0.0"));

                if ((object)m_dataGapRecoveryCompleted != null)
                    m_dataGapRecoveryCompleted.Set();
            }

            // Reset measurements received count being monitored
            m_measurementsRecoveredOverLastInterval = 0L;
        }

        #endregion
    }
}
