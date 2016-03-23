//******************************************************************************************************
//  InputAdapterBase.cs - Gbtc
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

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace GSF.TimeSeries.Adapters
{
    /// <summary>
    /// Represents the base class for any incoming input adapter.
    /// </summary>
    /// <remarks>
    /// Derived classes are expected to call <see cref="OnNewMeasurements"/> when new measurements are received.
    /// </remarks>
    public abstract class InputAdapterBase : AdapterBase, IInputAdapter
    {
        #region [ Members ]

        // Events

        /// <summary>
        /// Provides new measurements from input adapter.
        /// </summary>
        /// <remarks>
        /// <see cref="EventArgs{T}.Argument"/> is a collection of new measurements for host to process.
        /// </remarks>
        public event EventHandler<EventArgs<ICollection<IMeasurement>>> NewMeasurements;

        /// <summary>
        /// Indicates to the host that processing for the input adapter has completed.
        /// </summary>
        /// <remarks>
        /// This event is expected to only be raised when an input adapter has been designed to process
        /// a finite amount of data, e.g., reading a historical range of data during temporal procesing.
        /// </remarks>
        public event EventHandler ProcessingComplete;

        // Fields
        private List<string> m_outputSourceIDs;
        private MeasurementKey[] m_requestedOutputMeasurementKeys;
        private System.Timers.Timer m_connectionTimer;
        private bool m_isConnected;
        private bool m_disposed;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Constructs a new instance of the <see cref="InputAdapterBase"/>.
        /// </summary>
        protected InputAdapterBase()
        {
            m_connectionTimer = new System.Timers.Timer();
            m_connectionTimer.Elapsed += m_connectionTimer_Elapsed;

            m_connectionTimer.AutoReset = false;
            m_connectionTimer.Interval = 2000;
            m_connectionTimer.Enabled = false;
        }

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets or sets <see cref="MeasurementKey.Source"/> values used to filter output measurements.
        /// </summary>
        /// <remarks>
        /// This allows an adapter to associate itself with entire collections of measurements based on the source of the measurement keys.
        /// Set to <c>null</c> apply no filter.
        /// </remarks>
        public virtual string[] OutputSourceIDs
        {
            get
            {
                if (m_outputSourceIDs == null)
                    return null;

                return m_outputSourceIDs.ToArray();
            }
            set
            {
                if (value == null)
                {
                    m_outputSourceIDs = null;
                }
                else
                {
                    m_outputSourceIDs = new List<string>(value);
                    m_outputSourceIDs.Sort();
                }

                // Filter measurements to list of specified source IDs
                AdapterBase.LoadOutputSourceIDs(this);
            }
        }

        /// <summary>
        /// Gets or sets output measurement keys that are requested by other adapters based on what adapter says it can provide.
        /// </summary>
        public virtual MeasurementKey[] RequestedOutputMeasurementKeys
        {
            get
            {
                return m_requestedOutputMeasurementKeys;
            }
            set
            {
                m_requestedOutputMeasurementKeys = value;
            }
        }

        /// <summary>
        /// Gets flag that determines if <see cref="InputAdapterBase"/> is connected.
        /// </summary>
        public virtual bool IsConnected
        {
            get
            {
                return m_isConnected;
            }
            protected set
            {
                m_isConnected = value;
            }
        }

        /// <summary>
        /// Gets flag that determines if the data input connects asynchronously.
        /// </summary>
        /// <remarks>
        /// Derived classes should return true when data input source is connects asynchronously, otherwise return false.
        /// </remarks>
        protected abstract bool UseAsyncConnect
        {
            get;
        }

        /// <summary>
        /// Gets or sets the connection attempt interval, in milliseconds, for the data input source.
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
        /// Returns the detailed status of the data input source.
        /// </summary>
        /// <remarks>
        /// Derived classes should extend status with implementation specific information.
        /// </remarks>
        public override string Status
        {
            get
            {
                const int MaxMeasurementsToShow = 10;

                StringBuilder status = new StringBuilder();

                status.Append(base.Status);

                if (RequestedOutputMeasurementKeys != null && RequestedOutputMeasurementKeys.Length > 0)
                {
                    status.AppendFormat("     Requested output keys: {0} defined measurements", RequestedOutputMeasurementKeys.Length);
                    status.AppendLine();
                    status.AppendLine();

                    for (int i = 0; i < Common.Min(RequestedOutputMeasurementKeys.Length, MaxMeasurementsToShow); i++)
                    {
                        status.AppendLine(RequestedOutputMeasurementKeys[i].ToString().TruncateRight(25).CenterText(50));
                    }

                    if (RequestedOutputMeasurementKeys.Length > MaxMeasurementsToShow)
                        status.AppendLine("...".CenterText(50));

                    status.AppendLine();
                }

                status.AppendFormat("    Connection established: {0}", IsConnected);
                status.AppendLine();
                status.AppendFormat("   Asynchronous connection: {0}", UseAsyncConnect);
                status.AppendLine();
                status.AppendFormat("   Item reporting interval: {0}", MeasurementReportingInterval);
                status.AppendLine();

                return status.ToString();
            }
        }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="InputAdapterBase"/> object and optionally releases the managed resources.
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
        /// Initializes <see cref="InputAdapterBase"/>.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();

            Dictionary<string, string> settings = Settings;
            string setting;

            // Load optional parameters
            if (settings.TryGetValue("outputSourceIDs", out setting) || settings.TryGetValue("sourceids", out setting))
                OutputSourceIDs = setting.Split(',');
            else
                OutputSourceIDs = null;
        }

        /// <summary>
        /// Starts this <see cref="InputAdapterBase"/> and initiates connection cycle to data input source.
        /// </summary>
        public override void Start()
        {
            base.Start();

            // Start the connection cycle
            if (m_connectionTimer != null)
                m_connectionTimer.Enabled = true;
        }

        /// <summary>
        /// Attempts to connect to data input source.
        /// </summary>
        /// <remarks>
        /// Derived classes should attempt connection to data input source here.  Any exceptions thrown
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
            IsConnected = true;
            OnStatusMessage("Connection established.");
        }

        /// <summary>
        /// Stops this <see cref="InputAdapterBase"/> and disconnects from data input source.
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

                // Attempt disconnection from data source
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
        /// Attempts to disconnect from data input source.
        /// </summary>
        /// <remarks>
        /// Derived classes should attempt disconnect from data input source here.  Any exceptions thrown
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
            IsConnected = false;
            OnStatusMessage("Disconnected.");
        }

        /// <summary>
        /// Raises the <see cref="NewMeasurements"/> event.
        /// </summary>
        protected virtual void OnNewMeasurements(ICollection<IMeasurement> measurements)
        {
            try
            {
                if (NewMeasurements != null)
                    NewMeasurements(this, new EventArgs<ICollection<IMeasurement>>(measurements));

                IncrementProcessedMeasurements(measurements.Count);
            }
            catch (Exception ex)
            {
                // We protect our code from consumer thrown exceptions
                OnProcessException(new InvalidOperationException(string.Format("Exception in consumer handler for NewMeasurements event: {0}", ex.Message), ex));
            }
        }

        /// <summary>
        /// Raises the <see cref="ProcessingComplete"/> event.
        /// </summary>
        protected virtual void OnProcessingComplete()
        {
            try
            {
                if (ProcessingComplete != null)
                    ProcessingComplete(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                // We protect our code from consumer thrown exceptions
                OnProcessException(new InvalidOperationException(string.Format("Exception in consumer handler for ProcessingComplete event: {0}", ex.Message), ex));
            }
        }

        private void m_connectionTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                // So long as user hasn't requested to stop, attempt connection
                if (Enabled)
                {
                    OnStatusMessage("Attempting connection...");

                    // Attempt connection to data source
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

        #endregion
    }
}