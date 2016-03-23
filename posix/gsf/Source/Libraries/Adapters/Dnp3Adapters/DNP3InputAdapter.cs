﻿//******************************************************************************************************
//  Dnp3InputAdapter.cs - Gbtc
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
//  10/05/2012 - Adam Crain
//       Generated original version of source code.
//  03/27/2014 - Adam Crain
//       Updated to used latest opendnp3 code. 
//  03/28/2014 - J. Ritchie Carroll
//       Attached to an adapter instance to proxy IDNP3Manager singleton log messages.
//
//******************************************************************************************************

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using DNP3.Adapter;
using DNP3.Interface;
using GSF;
using GSF.IO;
using GSF.TimeSeries.Adapters;

namespace DNP3Adapters
{
    /// <summary>
    /// Input adapter that reads measurements from a remote dnp3 endpoint.
    /// </summary>
    [Description("DNP3: Reads measurements from a remote dnp3 endpoint")]
    public class DNP3InputAdapter : InputAdapterBase
    {
        #region [ Members ]

        // Nested Types

        // Class used to proxy dnp3 manager log entries to Iaon session
        private class IaonProxyLogHandler : ILogHandler
        {
            /// <summary>
            /// Handler for log entries.
            /// </summary>
            /// <param name="entry"><see cref="LogEntry"/> to handle.</param>
            public void Log(LogEntry entry)
            {
                // We avoid race conditions by always making sure access to status proxy is locked - this only
                // contends with adapter initialization and disposal so contention will not be normal
                lock (s_adapters)
                {
                    if ((object)s_statusProxy != null && !s_statusProxy.m_disposed)
                    {
                        if ((entry.filter.Flags & LogFilters.ERROR) > 0)
                        {
                            // Expose errors through exception processor
                            InvalidOperationException exception;

                            if (entry.keyValues.Count > 0)
                                exception = new InvalidOperationException(FormatLogEntry(entry), new Exception(entry.keyValues.JoinKeyValuePairs()));
                            else
                                exception = new InvalidOperationException(FormatLogEntry(entry));

                            s_statusProxy.OnProcessException(exception);
                        }
                        else
                        {
                            // For other messages, we just expose as a normal status
                            string message = FormatLogEntry(entry, true);

                            // The typical pattern in Iaon adapters is to prefix warning messages with "WARNING:"
                            if ((entry.filter.Flags & LogFilters.WARNING) > 0)
                                message = "WARNING: " + message;

                            s_statusProxy.OnStatusMessage(message);
                        }
                    }
                }
            }

            private static string FormatLogEntry(LogEntry entry, bool includeKeyValues = false)
            {
                StringBuilder entryText = new StringBuilder();

                if (!string.IsNullOrWhiteSpace(entry.loggerName))
                    entryText.AppendFormat("{0} - ", entry.loggerName);

                entryText.Append(entry.message);
                entryText.AppendFormat(" ({0})", LogFilters.GetFilterString(entry.filter.Flags));

                if (entry.errorCode != 0)
                    entryText.AppendFormat(" - error code {0}", entry.errorCode);

                if (!string.IsNullOrWhiteSpace(entry.location))
                    entryText.AppendFormat(" @ {0}", entry.location);

                if (includeKeyValues && entry.keyValues.Count > 0)
                {
                    entryText.AppendLine();
                    entryText.AppendFormat("Key Values = {0}", entry.keyValues.JoinKeyValuePairs());
                }

                return entryText.ToString();
            }
        }

        // Fields
        private string m_commsFilePath;             // Filename for the communication configuration file
        private string m_mappingFilePath;           // Filename for the measurement mapping configuration file
        private MasterConfiguration m_masterConfig; // Configuration for the master set during the Initialize call
        private MeasurementMap m_measurementMap;    // Configuration for the measurement map set during the Initialize call
        private TimeSeriesSOEHandler m_soeHandler;  // Time-series sequence of events handler
        private IChannel m_channel;                 // Communications channel set during the AttemptConnection call and used in AttemptDisconnect
        private bool m_active;                      // Flag that determines if the port/master has been added so that the resource can be cleaned up
        private bool m_disposed;

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets or sets the name of the xml file from which the communication parameters will be read.
        /// </summary>
        [ConnectionStringParameter,
        Description("Define the name of the XML file from which the communication configuration will be read. Include fully qualified path if file name is not in installation folder.")]
        public string CommsFilePath
        {
            get
            {
                return m_commsFilePath;
            }
            set
            {
                m_commsFilePath = value;
            }
        }

        /// <summary>
        /// Gets or sets the name of the xml file from which the measurement mapping is read.
        /// </summary>
        [ConnectionStringParameter,
        Description("Define the name of the XML file from which the communication configuration will be read. Include fully qualified path if file name is not in installation folder.")]
        public string MappingFilePath
        {
            get
            {
                return m_mappingFilePath;
            }
            set
            {
                m_mappingFilePath = value;
            }
        }

        /// <summary>
        /// Gets the flag indicating if this adapter supports temporal processing.
        /// </summary>
        public override bool SupportsTemporalProcessing
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets flag that determines if the data input connects asynchronously.
        /// </summary>
        /// <remarks>
        /// Derived classes should return true when data input source is connects asynchronously, otherwise return false.
        /// </remarks>
        protected override bool UseAsyncConnect
        {
            get
            {
                return false;
            }
        }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="DNP3InputAdapter"/> object and optionally releases the managed resources.
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
                        if (m_active)
                        {
                            m_active = false;

                            if ((object)m_channel != null)
                            {
                                // Shutdown the communications channel
                                m_channel.Shutdown();
                                m_channel = null;
                            }
                        }

                        // Detach from the time-series sequence of events new measurements event
                        if ((object)m_soeHandler != null)
                        {
                            m_soeHandler.NewMeasurements -= OnNewMeasurements;
                            m_soeHandler = null;
                        }

                        lock (s_adapters)
                        {
                            // Remove this adapter from the available list
                            s_adapters.Remove(this);

                            // See if we are disposing the status proxy instance
                            if (ReferenceEquals(s_statusProxy, this))
                            {
                                // Attempt to find a new status proxy
                                if (s_adapters.Count > 0)
                                    s_statusProxy = s_adapters[0];
                                else
                                    s_statusProxy = null;
                            }
                        }
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
        /// Initializes <see cref="DNP3InputAdapter"/>
        /// </summary>
        public override void Initialize()
        {
            Dictionary<string, string> settings = Settings;

            base.Initialize();

            settings.TryGetValue("CommsFilePath", out m_commsFilePath);

            if (string.IsNullOrWhiteSpace(m_commsFilePath))
                throw new ArgumentException("The required commsFile parameter was not specified");

            settings.TryGetValue("MappingFilePath", out m_mappingFilePath);

            if (string.IsNullOrWhiteSpace(m_mappingFilePath))
                throw new ArgumentException("The required mappingFile parameter was not specified");

            m_masterConfig = ReadConfig<MasterConfiguration>(m_commsFilePath);
            m_measurementMap = ReadConfig<MeasurementMap>(m_mappingFilePath);

            m_soeHandler = new TimeSeriesSOEHandler(new MeasurementLookup(m_measurementMap));
            m_soeHandler.NewMeasurements += OnNewMeasurements;

            lock (s_adapters)
            {
                // Add adapter to list of available adapters 
                s_adapters.Add(this);

                // If no adapter has been designated as the status proxy, might as well be this one
                if ((object)s_statusProxy == null)
                    s_statusProxy = this;
            }
        }

        private T ReadConfig<T>(string path)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));

            using (TextReader reader = new StreamReader(FilePath.GetAbsolutePath(path)))
            {
                return (T)serializer.Deserialize(reader);
            }
        }

        /// <summary>
        /// Attempts to connect to data input source.
        /// </summary>
        /// <remarks>
        /// Derived classes should attempt connection to data input source here.  Any exceptions thrown
        /// by this implementation will result in restart of the connection cycle.
        /// </remarks>
        protected override void AttemptConnection()
        {
            TcpClientConfig tcpConfig = m_masterConfig.client;
            string portName = tcpConfig.address + ":" + tcpConfig.port;
            TimeSpan minRetry = TimeSpan.FromMilliseconds(tcpConfig.minRetryMs);
            TimeSpan maxRetry = TimeSpan.FromMilliseconds(tcpConfig.maxRetryMs);

            IChannel channel = s_manager.AddTCPClient(portName, tcpConfig.level, minRetry, maxRetry, tcpConfig.address, tcpConfig.port);
            channel.AddStateListener(state => OnStatusMessage(portName + " - Channel state change: " + state));
            m_channel = channel;

            IMaster master = channel.AddMaster(portName, m_soeHandler, m_masterConfig.master);
            master.Enable();
            m_active = true;
        }

        /// <summary>
        /// Attempts to disconnect from data input source.
        /// </summary>
        /// <remarks>
        /// Derived classes should attempt disconnect from data input source here.  Any exceptions thrown
        /// by this implementation will be reported to host via <see cref="AdapterBase.ProcessException"/> event.
        /// </remarks>
        protected override void AttemptDisconnection()
        {
            if (m_active)
            {
                m_active = false;

                if ((object)m_channel != null)
                {
                    m_channel.Shutdown();
                    m_channel = null;
                }
            }
        }

        /// <summary>
        /// Gets a short one-line status of this <see cref="DNP3InputAdapter"/>.
        /// </summary>
        /// <param name="maxLength">Maximum number of available characters for display.</param>
        /// <returns>A short one-line summary of the current status of this <see cref="DNP3InputAdapter"/>.</returns>
        public override string GetShortStatus(int maxLength)
        {
            return string.Format("Received {0:N0} measurements so far...", ProcessedMeasurements).CenterText(maxLength);
        }

        #endregion

        #region [ Static ]

        // Static Fields

        // DNP3 manager shared across all of the DNP3 input adapters, concurrency level defaults to number of processors
        private static readonly IDNP3Manager s_manager;

        // We maintain a list of dnp3 adapters that can be used as status adapters for proxying messages from manager
        private static readonly List<DNP3InputAdapter> s_adapters;
        private static DNP3InputAdapter s_statusProxy;

        // Static Constructor
        static DNP3InputAdapter()
        {
            s_adapters = new List<DNP3InputAdapter>();
            s_manager = DNP3ManagerFactory.CreateManager(Environment.ProcessorCount);
            s_manager.AddLogHandler(new IaonProxyLogHandler());
        }

        #endregion
    }
}
