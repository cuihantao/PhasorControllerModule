﻿//******************************************************************************************************
//  PhasorMeasurementMapper.cs - Gbtc
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
//  05/18/2009 - J. Ritchie Carroll
//       Generated original version of source code.
//  03/21/2010 - J. Ritchie Carroll
//       Added new connection string settings to accommodate new MultiProtocolFrameParser properties.
//  12/04/2012 - J. Ritchie Carroll
//       Migrated to PhasorProtocolAdapters project.
//  12/13/2012 - Starlynn Danyelle Gilliam
//       Modified Header.
//
//******************************************************************************************************

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Timers;
using GSF;
using GSF.Communication;
using GSF.IO;
using GSF.PhasorProtocols;
using GSF.PhasorProtocols.Anonymous;
using GSF.TimeSeries;
using GSF.TimeSeries.Adapters;
using GSF.TimeSeries.Statistics;
using GSF.Units;
using GSF.Units.EE;

namespace PhasorProtocolAdapters
{
    /// <summary>
    /// Represents an <see cref="IInputAdapter"/> used to map measured values from a connection
    /// to a phasor measurement device to new <see cref="IMeasurement"/> values.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class PhasorMeasurementMapper : InputAdapterBase
    {
        #region [ Members ]

        // Nested Types

        // Represents a defined measurement for the mapper connection
        private class DefinedMeasurement
        {
            public string TagName;
            public MeasurementKey Key;
            public double Adder;
            public double Multiplier;
        }

        // Represents a missing data monitor needed to better calculate
        // total missing data when there are redundant frames in a
        // packet of data (e.g., when using IEC 61850-90-5)
        private class MissingDataMonitor : ConcentratorBase
        {
            #region [ Members ]

            // Fields
            private long m_lastFrameTimestamp;
            private long m_missingData;
            private int m_redundantFramesPerPacket;

            #endregion

            #region [ Properties ]

            /// <summary>
            /// Gets or sets the redundant frames per packet.
            /// </summary>
            public int RedundantFramesPerPacket
            {
                set
                {
                    m_redundantFramesPerPacket = value;
                }
            }

            /// <summary>
            /// Gets the missing data count since the monitor was started.
            /// </summary>
            public long TotalMissingData
            {
                get
                {
                    long cutOff;
                    int missingFrameCount;

                    cutOff = Ticks.AlignToSubsecondDistribution(RealTime - LagTicks, FramesPerSecond, TimeResolution);
                    missingFrameCount = (int)Math.Round((cutOff - m_lastFrameTimestamp) / TicksPerFrame);
                    m_missingData += (missingFrameCount > m_redundantFramesPerPacket) ? (missingFrameCount - m_redundantFramesPerPacket) : 0;
                    m_lastFrameTimestamp = cutOff;

                    return m_missingData;
                }
            }

            #endregion

            #region [ Methods ]

            /// <summary>
            /// Starts the <see cref="MissingDataMonitor"/>.
            /// </summary>
            public override void Start()
            {
                base.Start();
                m_lastFrameTimestamp = Ticks.AlignToSubsecondDistribution(RealTime, FramesPerSecond, TimeResolution);
            }

            /// <summary>
            /// Called when a frame is published by the underlying concentrator engine.
            /// </summary>
            /// <param name="frame">The frame that is published.</param>
            /// <param name="index">The index of the frame from 0 to <see cref="ConcentratorBase.FramesPerSecond"/> - 1.</param>
            protected override void PublishFrame(IFrame frame, int index)
            {
                int missingFrameCount;

                missingFrameCount = (int)Math.Round((frame.Timestamp - m_lastFrameTimestamp) / TicksPerFrame);
                m_missingData += (missingFrameCount > m_redundantFramesPerPacket) ? (missingFrameCount - m_redundantFramesPerPacket) : 0;
                m_lastFrameTimestamp = frame.Timestamp;
            }

            #endregion
        }

        // Fields
        private MultiProtocolFrameParser m_frameParser;
        private Dictionary<string, DefinedMeasurement> m_definedMeasurements;
        private ConcurrentDictionary<ushort, DeviceStatisticsHelper<ConfigurationCell>> m_definedDevices;
        private ConcurrentDictionary<string, DeviceStatisticsHelper<ConfigurationCell>> m_labelDefinedDevices;
        private readonly ConcurrentDictionary<string, long> m_undefinedDevices;
        private readonly ConcurrentDictionary<SignalKind, string[]> m_generatedSignalReferenceCache;
        private MissingDataMonitor m_missingDataMonitor;
        private Timer m_dataStreamMonitor;
        private Timer m_measurementCounter;
        private bool m_allowUseOfCachedConfiguration;
        private bool m_cachedConfigLoadAttempted;
        private readonly object m_configurationOperationLock;
        private TimeZoneInfo m_timezone;
        private Ticks m_timeAdjustmentTicks;
        private Ticks m_lastReportTime;
        private long m_outOfOrderFrames;
        private long m_totalLatency;
        private long m_minimumLatency;
        private long m_maximumLatency;
        private long m_latencyFrames;
        private long m_connectionAttempts;
        private long m_configurationChanges;
        private long m_totalDataFrames;
        private long m_totalConfigurationFrames;
        private long m_totalHeaderFrames;
        private string m_sharedMapping;
        private uint m_sharedMappingID;
        private bool m_forceLabelMapping;
        private ushort m_accessID;
        private bool m_isConcentrator;
        private bool m_receivedConfigFrame;
        private long m_bytesReceived;
        private double m_lagTime;
        private double m_leadTime;
        private long m_timeResolution;

        private long m_lifetimeMeasurements;
        private long m_minimumMeasurementsPerSecond;
        private long m_maximumMeasurementsPerSecond;
        private long m_totalMeasurementsPerSecond;
        private long m_measurementsPerSecondCount;
        private long m_measurementsInSecond;
        private long m_lastSecondsSinceEpoch;
        private long m_lifetimeTotalLatency;
        private long m_lifetimeMinimumLatency;
        private long m_lifetimeMaximumLatency;
        private long m_lifetimeLatencyFrames;
        private string m_qualityFlagsSignalReferenceName;
        private int m_lastMeasurementMappedCount = 4;
        private bool m_disposed;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Creates a new <see cref="PhasorMeasurementMapper"/>.
        /// </summary>
        public PhasorMeasurementMapper()
        {
            // Create a cached signal reference dictionary for generated signal references
            m_generatedSignalReferenceCache = new ConcurrentDictionary<SignalKind, string[]>();

            // Create data stream monitoring timer
            m_dataStreamMonitor = new Timer();
            m_dataStreamMonitor.Elapsed += m_dataStreamMonitor_Elapsed;
            m_dataStreamMonitor.AutoReset = true;
            m_dataStreamMonitor.Enabled = false;

            m_undefinedDevices = new ConcurrentDictionary<string, long>();
            m_configurationOperationLock = new object();
        }

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets flag that determines if device being mapped is a concentrator (i.e., data from multiple
        /// devices combined together from the connected device).
        /// </summary>
        public bool IsConcentrator
        {
            get
            {
                return m_isConcentrator;
            }
        }

        /// <summary>
        /// Gets or sets access ID (or ID code) for this device connection which is often necessary in order to make a connection to some phasor protocols.
        /// </summary>
        public ushort AccessID
        {
            get
            {
                return m_accessID;
            }
            set
            {
                m_accessID = value;
            }
        }

        private IEnumerable<DeviceStatisticsHelper<ConfigurationCell>> StatisticsHelpers
        {
            get
            {
                if ((object)m_labelDefinedDevices != null)
                    return m_definedDevices.Values.Concat(m_labelDefinedDevices.Values);

                return m_definedDevices.Values;
            }
        }

        /// <summary>
        /// Gets an enumeration of all defined system devices (regardless of ID or label based definition)
        /// </summary>
        public IEnumerable<ConfigurationCell> DefinedDevices
        {
            get
            {
                return StatisticsHelpers.Select(statisticsHelper => statisticsHelper.Device);
            }
        }

        /// <summary>
        /// Gets or sets flag that determines if use of cached configuration during initial connection is allowed when a configuration has not been received within the data loss interval.
        /// </summary>
        public bool AllowUseOfCachedConfiguration
        {
            get
            {
                return m_allowUseOfCachedConfiguration;
            }
            set
            {
                m_allowUseOfCachedConfiguration = value;
            }
        }

        /// <summary>
        /// Gets the configuration cache file name, with path.
        /// </summary>
        public string ConfigurationCacheFileName
        {
            get
            {
                return ConfigurationFrame.GetConfigurationCacheFileName(Name);
            }
        }

        /// <summary>
        /// Gets or sets time zone of this <see cref="PhasorMeasurementMapper"/>.
        /// </summary>
        /// <remarks>
        /// If time zone of clock of connected device is not set to UTC, assigning this property
        /// with proper time zone will allow proper adjustment.
        /// </remarks>
        public TimeZoneInfo TimeZone
        {
            get
            {
                return m_timezone;
            }
            set
            {
                m_timezone = value;
            }
        }

        /// <summary>
        /// Gets or sets ticks used to manually adjust time of this <see cref="PhasorMeasurementMapper"/>.
        /// </summary>
        /// <remarks>
        /// This property will allow for precise time adjustments of connected devices should
        /// this be needed.
        /// </remarks>
        public Ticks TimeAdjustmentTicks
        {
            get
            {
                return m_timeAdjustmentTicks;
            }
            set
            {
                m_timeAdjustmentTicks = value;
            }
        }

        /// <summary>
        /// Gets the number of missing frames of data, taking into account redundant frames.
        /// </summary>
        public long MissingData
        {
            get
            {
                if (m_frameParser.RedundantFramesPerPacket > 0 && (object)m_missingDataMonitor != null)
                    return m_missingDataMonitor.TotalMissingData;

                return MissingFrames;
            }
        }

        /// <summary>
        /// Gets the the total number of frames that have been received by the current mapper connection.
        /// </summary>
        public long TotalFrames
        {
            get
            {
                if ((object)m_frameParser != null)
                    return m_frameParser.TotalFramesReceived;

                return 0;
            }
        }

        /// <summary>
        /// Gets or set last report time for current mapper connection.
        /// </summary>
        public Ticks LastReportTime
        {
            get
            {
                return m_lastReportTime;
            }
            set
            {
                m_lastReportTime = value;
            }
        }

        /// <summary>
        /// Gets the total number of frames that have been missed by the current mapper connection.
        /// </summary>
        public long MissingFrames
        {
            get
            {
                if ((object)m_frameParser != null)
                    return m_frameParser.TotalMissingFrames;

                return 0;
            }
        }

        /// <summary>
        /// Gets the total number of CRC errors that have been encountered by the the current mapper connection.
        /// </summary>
        public long CRCErrors
        {
            get
            {
                if ((object)m_frameParser != null)
                    return m_frameParser.TotalCrcExceptions;

                return 0;
            }
        }

        /// <summary>
        /// Gets the total number frames that came in out of order from the current mapper connection.
        /// </summary>
        public long OutOfOrderFrames
        {
            get
            {
                return m_outOfOrderFrames;
            }
        }

        /// <summary>
        /// Gets the minimum latency in milliseconds over the last test interval.
        /// </summary>
        public int MinimumLatency
        {
            get
            {
                return (int)Ticks.ToMilliseconds(m_minimumLatency);
            }
        }

        /// <summary>
        /// Gets the maximum latency in milliseconds over the last test interval.
        /// </summary>
        public int MaximumLatency
        {
            get
            {
                return (int)Ticks.ToMilliseconds(m_maximumLatency);
            }
        }

        /// <summary>
        /// Gets the average latency in milliseconds over the last test interval.
        /// </summary>
        public int AverageLatency
        {
            get
            {
                if (m_latencyFrames == 0)
                    return -1;

                return (int)Ticks.ToMilliseconds(m_totalLatency / m_latencyFrames);
            }
        }

        /// <summary>
        /// Gets the total number of connection attempts.
        /// </summary>
        public long ConnectionAttempts
        {
            get
            {
                return m_connectionAttempts;
            }
        }

        /// <summary>
        /// Gets the total number of received configurations.
        /// </summary>
        public long ConfigurationChanges
        {
            get
            {
                return m_configurationChanges;
            }
        }

        /// <summary>
        /// Gets the total number of received data frames.
        /// </summary>
        public long TotalDataFrames
        {
            get
            {
                return m_totalDataFrames;
            }
        }

        /// <summary>
        /// Gets the total number of received configuration frames.
        /// </summary>
        public long TotalConfigurationFrames
        {
            get
            {
                return m_totalConfigurationFrames;
            }
        }

        /// <summary>
        /// Gets the total number of received header frames.
        /// </summary>
        public long TotalHeaderFrames
        {
            get
            {
                return m_totalHeaderFrames;
            }
        }

        /// <summary>
        /// Gets the defined frame rate.
        /// </summary>
        public int DefinedFrameRate
        {
            get
            {
                if ((object)m_frameParser != null)
                    return m_frameParser.ConfiguredFrameRate;

                return 0;
            }
        }

        /// <summary>
        /// Gets the actual frame rate.
        /// </summary>
        public double ActualFrameRate
        {
            get
            {
                if ((object)m_frameParser != null)
                    return m_frameParser.CalculatedFrameRate;

                return 0.0D;
            }
        }

        /// <summary>
        /// Gets the actual data rate.
        /// </summary>
        public double ActualDataRate
        {
            get
            {
                if ((object)m_frameParser != null)
                    return m_frameParser.ByteRate;

                return 0.0D;
            }
        }

        /// <summary>
        /// Gets or sets acronym of other device for which to assume a shared mapping.
        /// </summary>
        /// <remarks>
        /// Assigning acronym to this property automatically looks up ID of associated device.
        /// </remarks>
        public string SharedMapping
        {
            get
            {
                return m_sharedMapping;
            }
            set
            {
                m_sharedMapping = value;
                m_sharedMappingID = 0;

                if (!string.IsNullOrWhiteSpace(m_sharedMapping))
                {
                    try
                    {
                        DataRow[] filteredRows = DataSource.Tables["InputAdapters"].Select(string.Format("AdapterName = '{0}'", m_sharedMapping));

                        if (filteredRows.Length > 0)
                        {
                            m_sharedMappingID = uint.Parse(filteredRows[0]["ID"].ToString());
                        }
                        else
                        {
                            OnProcessException(new InvalidOperationException(string.Format("Failed to find input adapter ID for shared mapping \"{0}\", mapping was not assigned.", m_sharedMapping)));
                            m_sharedMapping = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        OnProcessException(new InvalidOperationException(string.Format("Failed to find input adapter ID for shared mapping \"{0}\" due to exception: {1} Mapping was not assigned.", m_sharedMapping, ex.Message), ex));
                        m_sharedMapping = null;
                    }
                }
            }
        }

        /// <summary>
        /// Returns ID of associated device with shared mapping or <see cref="AdapterBase.ID"/> of this <see cref="PhasorMeasurementMapper"/> if no shared mapping is defined.
        /// </summary>
        public uint SharedMappingID
        {
            get
            {
                if (m_sharedMappingID == 0)
                    return ID;

                return m_sharedMappingID;
            }
        }

        /// <summary>
        /// Gets flag that determines if the data input connects asynchronously.
        /// </summary>
        protected override bool UseAsyncConnect
        {
            get
            {
                // We use asynchronous connection on devices
                return true;
            }
        }

        /// <summary>
        /// Gets the flag indicating if this adapter supports temporal processing.
        /// </summary>
        /// <remarks>
        /// Since the phasor measurement mapper is designed to open sockets and connect to data streams,
        /// it is expected that this would not be desired in a temporal data streaming session.
        /// </remarks>
        public override bool SupportsTemporalProcessing
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Returns the detailed status of the data input source.
        /// </summary>
        public override string Status
        {
            get
            {
                StringBuilder status = new StringBuilder();

                status.Append(base.Status);
                status.AppendFormat("    Source is concentrator: {0}", m_isConcentrator);
                status.AppendLine();
                if (!string.IsNullOrWhiteSpace(SharedMapping))
                {
                    status.AppendFormat("     Shared mapping source: {0}", SharedMapping);
                    status.AppendLine();
                }
                status.AppendFormat(" Source connection ID code: {0}", m_accessID);
                status.AppendLine();
                status.AppendFormat("     Forcing label mapping: {0}", m_forceLabelMapping);
                status.AppendLine();
                status.AppendFormat("      Label mapped devices: {0}", (object)m_labelDefinedDevices == null ? 0 : m_labelDefinedDevices.Count);
                status.AppendLine();
                status.AppendFormat("          Target time zone: {0}", m_timezone.Id);
                status.AppendLine();
                status.AppendFormat("    Manual time adjustment: {0} seconds", m_timeAdjustmentTicks.ToSeconds().ToString("0.000"));
                status.AppendLine();
                status.AppendFormat("Allow use of cached config: {0}", m_allowUseOfCachedConfiguration);
                status.AppendLine();
                status.AppendFormat("No data reconnect interval: {0} seconds", Ticks.FromMilliseconds(m_dataStreamMonitor.Interval).ToSeconds().ToString("0.000"));
                status.AppendLine();

                if (m_allowUseOfCachedConfiguration)
                {
                    //                   123456789012345678901234567890
                    status.AppendFormat("   Cached config file name: {0}", FilePath.TrimFileName(ConfigurationCacheFileName, 51));
                    status.AppendLine();
                }

                status.AppendFormat("       Out of order frames: {0}", m_outOfOrderFrames);
                status.AppendLine();
                status.AppendFormat("           Minimum latency: {0}ms over {1} tests", MinimumLatency, m_latencyFrames);
                status.AppendLine();
                status.AppendFormat("           Maximum latency: {0}ms over {1} tests", MaximumLatency, m_latencyFrames);
                status.AppendLine();
                status.AppendFormat("           Average latency: {0}ms over {1} tests", AverageLatency, m_latencyFrames);
                status.AppendLine();

                if ((object)m_frameParser != null)
                    status.Append(m_frameParser.Status);

                status.AppendLine();
                status.Append("Parsed Frame Quality Statistics".CenterText(78));
                status.AppendLine();
                status.AppendLine();
                //                      1         2         3         4         5         6         7
                //             123456789012345678901234567890123456789012345678901234567890123456789012345678
                status.Append("Device                  Bad Data   Bad Time    Frame      Total    Last Report");
                status.AppendLine();
                status.Append(" Name                    Frames     Frames     Errors     Frames      Time");
                status.AppendLine();
                //                      1         2            1          1          1          1          1
                //             1234567890123456789012 1234567890 1234567890 1234567890 1234567890 123456789012
                status.Append("---------------------- ---------- ---------- ---------- ---------- ------------");
                status.AppendLine();

                IConfigurationCell parsedDevice;
                string stationName;

                foreach (ConfigurationCell definedDevice in DefinedDevices)
                {
                    stationName = null;

                    // Attempt to lookup station name in configuration frame of connected device
                    if ((object)m_frameParser != null && (object)m_frameParser.ConfigurationFrame != null)
                    {
                        // Attempt to lookup by label (if defined), then by ID code
                        if (((object)m_labelDefinedDevices != null && (object)definedDevice.StationName != null &&
                            m_frameParser.ConfigurationFrame.Cells.TryGetByStationName(definedDevice.StationName, out parsedDevice)) ||
                            m_frameParser.ConfigurationFrame.Cells.TryGetByIDCode(definedDevice.IDCode, out parsedDevice))
                            stationName = parsedDevice.StationName;
                    }

                    // We will default to defined name if parsed name is unavailable
                    if (string.IsNullOrWhiteSpace(stationName))
                        stationName = "[" + definedDevice.StationName.NotEmpty(definedDevice.IDLabel.NotEmpty("UNDEF") + ":" + definedDevice.IDCode) + "]";

                    status.Append(stationName.TruncateRight(22).PadRight(22));
                    status.Append(' ');
                    status.Append(definedDevice.DataQualityErrors.ToString().CenterText(10));
                    status.Append(' ');
                    status.Append(definedDevice.TimeQualityErrors.ToString().CenterText(10));
                    status.Append(' ');
                    status.Append(definedDevice.DeviceErrors.ToString().CenterText(10));
                    status.Append(' ');
                    status.Append(definedDevice.TotalFrames.ToString().CenterText(10));
                    status.Append(' ');
                    status.Append(((DateTime)definedDevice.LastReportTime).ToString("HH:mm:ss.fff"));
                    status.AppendLine();
                }

                status.AppendLine();
                status.AppendFormat("Undefined devices encountered: {0}", m_undefinedDevices.Count);
                status.AppendLine();

                foreach (KeyValuePair<string, long> item in m_undefinedDevices)
                {
                    status.Append("    Device \"");
                    status.Append(item.Key);
                    status.Append("\" encountered ");
                    status.Append(item.Value);
                    status.Append(" times");
                    status.AppendLine();
                }

                return status.ToString();
            }
        }

        /// <summary>
        /// Gets or sets reference to <see cref="MultiProtocolFrameParser"/>, attaching and/or detaching to events as needed.
        /// </summary>
        protected MultiProtocolFrameParser FrameParser
        {
            get
            {
                return m_frameParser;
            }
            set
            {
                if ((object)m_frameParser != null)
                {
                    // Detach from events on existing frame parser reference
                    m_frameParser.ConfigurationChanged -= m_frameParser_ConfigurationChanged;
                    m_frameParser.ConnectionAttempt -= m_frameParser_ConnectionAttempt;
                    m_frameParser.ConnectionEstablished -= m_frameParser_ConnectionEstablished;
                    m_frameParser.ConnectionException -= m_frameParser_ConnectionException;
                    m_frameParser.ConnectionTerminated -= m_frameParser_ConnectionTerminated;
                    m_frameParser.ExceededParsingExceptionThreshold -= m_frameParser_ExceededParsingExceptionThreshold;
                    m_frameParser.ParsingException -= m_frameParser_ParsingException;
                    m_frameParser.ReceivedConfigurationFrame -= m_frameParser_ReceivedConfigurationFrame;
                    m_frameParser.ReceivedDataFrame -= m_frameParser_ReceivedDataFrame;
                    m_frameParser.ReceivedHeaderFrame -= m_frameParser_ReceivedHeaderFrame;
                    m_frameParser.ReceivedFrameImage -= m_frameParser_ReceivedFrameImage;
                    m_frameParser.Dispose();
                }

                // Assign new frame parser reference
                m_frameParser = value;

                if ((object)m_frameParser != null)
                {
                    // Attach to events on new frame parser reference
                    m_frameParser.ConfigurationChanged += m_frameParser_ConfigurationChanged;
                    m_frameParser.ConnectionAttempt += m_frameParser_ConnectionAttempt;
                    m_frameParser.ConnectionEstablished += m_frameParser_ConnectionEstablished;
                    m_frameParser.ConnectionException += m_frameParser_ConnectionException;
                    m_frameParser.ConnectionTerminated += m_frameParser_ConnectionTerminated;
                    m_frameParser.ExceededParsingExceptionThreshold += m_frameParser_ExceededParsingExceptionThreshold;
                    m_frameParser.ParsingException += m_frameParser_ParsingException;
                    m_frameParser.ReceivedConfigurationFrame += m_frameParser_ReceivedConfigurationFrame;
                    m_frameParser.ReceivedDataFrame += m_frameParser_ReceivedDataFrame;
                    m_frameParser.ReceivedHeaderFrame += m_frameParser_ReceivedHeaderFrame;
                    m_frameParser.ReceivedFrameImage += m_frameParser_ReceivedFrameImage;
                }
            }
        }

        /// <summary>
        /// Gets total data packet bytes received during this session.
        /// </summary>
        public long TotalBytesReceived
        {
            get
            {
                if ((object)m_frameParser != null)
                    return m_frameParser.TotalBytesReceived;

                return 0;
            }
        }

        /// <summary>
        /// Gets the total number of measurements processed through this data publisher over the lifetime of the input stream.
        /// </summary>
        public long LifetimeMeasurements
        {
            get
            {
                return m_lifetimeMeasurements;
            }
        }

        /// <summary>
        /// Gets the minimum value of the measurements per second calculation.
        /// </summary>
        public long MinimumMeasurementsPerSecond
        {
            get
            {
                return m_minimumMeasurementsPerSecond;
            }
        }

        /// <summary>
        /// Gets the maximum value of the measurements per second calculation.
        /// </summary>
        public long MaximumMeasurementsPerSecond
        {
            get
            {
                return m_maximumMeasurementsPerSecond;
            }
        }

        /// <summary>
        /// Gets the average value of the measurements per second calculation.
        /// </summary>
        public long AverageMeasurementsPerSecond
        {
            get
            {
                if (m_measurementsPerSecondCount == 0L)
                    return 0L;

                return m_totalMeasurementsPerSecond / m_measurementsPerSecondCount;
            }
        }

        /// <summary>
        /// Gets the minimum latency calculated over the full lifetime of the input stream.
        /// </summary>
        public int LifetimeMinimumLatency
        {
            get
            {
                return (int)Ticks.ToMilliseconds(m_lifetimeMinimumLatency);
            }
        }

        /// <summary>
        /// Gets the maximum latency calculated over the full lifetime of the input stream.
        /// </summary>
        public int LifetimeMaximumLatency
        {
            get
            {
                return (int)Ticks.ToMilliseconds(m_lifetimeMaximumLatency);
            }
        }

        /// <summary>
        /// Gets the average latency calculated over the full lifetime of the input stream.
        /// </summary>
        public int LifetimeAverageLatency
        {
            get
            {
                if (m_lifetimeLatencyFrames == 0)
                    return -1;

                return (int)Ticks.ToMilliseconds(m_lifetimeTotalLatency / m_lifetimeLatencyFrames);
            }
        }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="PhasorMeasurementMapper"/> object and optionally releases the managed resources.
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
                        // Detach from frame parser events and set reference to null
                        FrameParser = null;

                        if ((object)m_dataStreamMonitor != null)
                        {
                            m_dataStreamMonitor.Elapsed -= m_dataStreamMonitor_Elapsed;
                            m_dataStreamMonitor.Dispose();
                            m_dataStreamMonitor = null;
                        }

                        if ((object)m_measurementCounter != null)
                        {
                            m_measurementCounter.Elapsed -= m_measurementCounter_Elapsed;
                            m_measurementCounter.Dispose();
                            m_measurementCounter = null;
                        }

                        if ((object)m_definedDevices != null)
                        {
                            // Unregister each existing device from the statistics engine
                            foreach (ConfigurationCell device in DefinedDevices)
                            {
                                StatisticsEngine.Unregister(device);
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
        /// Initializes <see cref="PhasorMeasurementMapper"/>.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();

            Dictionary<string, string> settings = Settings;
            string setting;

            // Cache signal reference name for connected device quality flags - in normal usage, name will not change for adapter lifetime
            m_qualityFlagsSignalReferenceName = SignalReference.ToString(Name, SignalKind.Quality);

            // Load optional mapper specific connection parameters
            if (settings.TryGetValue("isConcentrator", out setting))
                m_isConcentrator = setting.ParseBoolean();
            else
                m_isConcentrator = false;

            if (settings.TryGetValue("accessID", out setting))
                m_accessID = ushort.Parse(setting);
            else
                m_accessID = 1;

            if (settings.TryGetValue("forceLabelMapping", out setting))
                m_forceLabelMapping = setting.ParseBoolean();
            else
                m_forceLabelMapping = false;

            // Commented out for security purposes related to Parent collection --
            // Replace with secure notification mechanism

            //if (settings.TryGetValue("primaryDataSource", out setting))
            //{
            //    uint primaryDataSourceID = 0;

            //    // Lookup adapter runtime ID of specified primary data source
            //    if (!string.IsNullOrWhiteSpace(setting))
            //    {
            //        try
            //        {
            //            DataRow[] filteredRows = DataSource.Tables["InputAdapters"].Select(string.Format("AdapterName = '{0}'", setting));

            //            if (filteredRows.Length > 0)
            //                primaryDataSourceID = uint.Parse(filteredRows[0]["ID"].ToString());
            //            else
            //                OnProcessException(new InvalidOperationException(string.Format("Failed to find input adapter ID for primary data source \"{0}\", data source was not established.", setting)));
            //        }
            //        catch (Exception ex)
            //        {
            //            OnProcessException(new InvalidOperationException(string.Format("Failed to find input adapter ID for primary data source \"{0}\" due to exception: {1} Data source was not established.", setting, ex.Message), ex));
            //        }
            //    }

            //    if (primaryDataSourceID > 0)
            //    {
            //        // Get matching data subscriber
            //        m_primaryDataSource = Parent.FirstOrDefault(adapter => adapter.ID == primaryDataSourceID && adapter is DataSubscriber) as DataSubscriber;

            //        if ((object)m_primaryDataSource == null)
            //        {
            //            OnProcessException(new InvalidOperationException(string.Format("Input adapter \"{0}\" specified to be the primary data source was not a DataSubscriber adapter, data source was not established.", setting)));
            //        }
            //        else
            //        {
            //            // Attach to connection events of primary source adapter since these will be used to control when back connection is established...
            //            m_primaryDataSource.ConnectionEstablished += m_primaryDataSource_ConnectionEstablished;
            //            m_primaryDataSource.ConnectionTerminated += m_primaryDataSource_ConnectionTerminated;
            //        }
            //    }
            //}

            if (settings.TryGetValue("sharedMapping", out setting))
                SharedMapping = setting.Trim();
            else
                SharedMapping = null;

            if (settings.TryGetValue("timeZone", out setting) && !string.IsNullOrWhiteSpace(setting) && !setting.Trim().Equals("UTC", StringComparison.OrdinalIgnoreCase) && !setting.Trim().Equals("Coordinated Universal Time", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    m_timezone = TimeZoneInfo.FindSystemTimeZoneById(setting);
                }
                catch (Exception ex)
                {
                    OnProcessException(new InvalidOperationException(string.Format("Defaulting to UTC. Failed to find system time zone for ID \"{0}\": {1}", setting, ex.Message), ex));
                    m_timezone = TimeZoneInfo.Utc;
                }
            }
            else
            {
                m_timezone = TimeZoneInfo.Utc;
            }

            if (settings.TryGetValue("timeAdjustmentTicks", out setting))
                m_timeAdjustmentTicks = long.Parse(setting);
            else
                m_timeAdjustmentTicks = 0;

            if (settings.TryGetValue("dataLossInterval", out setting))
                m_dataStreamMonitor.Interval = double.Parse(setting) * 1000.0D;
            else
                m_dataStreamMonitor.Interval = 5000.0D;

            if (settings.TryGetValue("delayedConnectionInterval", out setting))
            {
                double interval = double.Parse(setting) * 1000.0D;

                // Minimum delay is one millisecond
                if (interval < 1.0D)
                    interval = 1.0D;

                ConnectionAttemptInterval = interval;
            }
            else
            {
                ConnectionAttemptInterval = 1500.0D;
            }

            if (settings.TryGetValue("allowUseOfCachedConfiguration", out setting))
                m_allowUseOfCachedConfiguration = setting.ParseBoolean();
            else
                m_allowUseOfCachedConfiguration = true;

            // Create a new phasor protocol frame parser for non-virtual connections
            MultiProtocolFrameParser frameParser = new MultiProtocolFrameParser();

            // Most of the parameters in the connection string will be for the data source in the frame parser
            // so we provide all of them, other parameters will simply be ignored
            frameParser.ConnectionString = ConnectionString;

            // Since input adapter will automatically reconnect on connection exceptions, we need only to specify
            // that the frame parser try to connect once per connection attempt
            frameParser.MaximumConnectionAttempts = 1;

            // For captured data simulations we will inject a simulated timestamp and auto-repeat file stream...
            if (frameParser.TransportProtocol == TransportProtocol.File)
            {
                if (settings.TryGetValue("definedFrameRate", out setting))
                    frameParser.DefinedFrameRate = int.Parse(setting);
                else
                    frameParser.DefinedFrameRate = 30;

                if (settings.TryGetValue("autoRepeatFile", out setting))
                    frameParser.AutoRepeatCapturedPlayback = setting.ParseBoolean();
                else
                    frameParser.AutoRepeatCapturedPlayback = true;

                if (settings.TryGetValue("useHighResolutionInputTimer", out setting))
                    frameParser.UseHighResolutionInputTimer = setting.ParseBoolean();
                else
                    frameParser.UseHighResolutionInputTimer = false;
            }

            // Apply other settings as needed
            if (settings.TryGetValue("simulateTimestamp", out setting))
                frameParser.InjectSimulatedTimestamp = setting.ParseBoolean();
            else
                frameParser.InjectSimulatedTimestamp = (frameParser.TransportProtocol == TransportProtocol.File);

            if (settings.TryGetValue("allowedParsingExceptions", out setting))
                frameParser.AllowedParsingExceptions = int.Parse(setting);

            if (settings.TryGetValue("parsingExceptionWindow", out setting))
                frameParser.ParsingExceptionWindow = Ticks.FromSeconds(double.Parse(setting));

            if (settings.TryGetValue("autoStartDataParsingSequence", out setting))
                frameParser.AutoStartDataParsingSequence = setting.ParseBoolean();

            if (settings.TryGetValue("skipDisableRealTimeData", out setting))
                frameParser.SkipDisableRealTimeData = setting.ParseBoolean();

            if (!(settings.TryGetValue("lagTime", out setting) && double.TryParse(setting, out m_lagTime)))
                m_lagTime = 10.0D;

            if (!(settings.TryGetValue("leadTime", out setting) && double.TryParse(setting, out m_leadTime)))
                m_leadTime = 3.0D;

            if (!(settings.TryGetValue("timeResolution", out setting) && long.TryParse(setting, out m_timeResolution)))
                m_timeResolution = 10000L;

            // Provide access ID to frame parser as this may be necessary to make a phasor connection
            frameParser.DeviceID = m_accessID;
            frameParser.SourceName = Name;

            // Assign reference to frame parser for this connection and attach to needed events
            FrameParser = frameParser;

            // Load specific configuration file if one was specified
            if (settings.TryGetValue("configurationFile", out setting))
                LoadConfiguration(setting);

            // Load input devices associated with this connection
            LoadInputDevices();

            // Load active device measurements associated with this connection
            LoadDeviceMeasurements();

            // Register with the statistics engine
            StatisticsEngine.Register(this, "InputStream", "IS");
            StatisticsEngine.Calculated += (sender, args) => ResetLatencyCounters();
            StatisticsEngine.Calculated += (sender, args) => ResetMeasurementsPerSecondCounters();
        }

        // Load device list for this mapper connection
        private void LoadInputDevices()
        {
            ConfigurationCell definedDevice;
            string deviceName;

            if ((object)m_definedDevices != null)
            {
                // Unregister each existing device from the statistics engine
                foreach (ConfigurationCell device in DefinedDevices)
                {
                    StatisticsEngine.Unregister(device);
                }
            }

            m_definedDevices = new ConcurrentDictionary<ushort, DeviceStatisticsHelper<ConfigurationCell>>();

            if (m_isConcentrator)
            {
                StringBuilder deviceStatus = new StringBuilder();
                bool devicedAdded;
                int index = 0;

                deviceStatus.AppendLine();
                deviceStatus.AppendLine();
                deviceStatus.Append("Loading expected concentrator device list...");
                deviceStatus.AppendLine();
                deviceStatus.AppendLine();

                // Making a connection to a concentrator that can support multiple devices
                foreach (DataRow row in DataSource.Tables["InputStreamDevices"].Select(string.Format("ParentID={0}", SharedMappingID)))
                {
                    // Create new configuration cell parsing needed ID code and label from input stream configuration
                    definedDevice = new ConfigurationCell(ushort.Parse(row["AccessID"].ToString()));
                    deviceName = row["Acronym"].ToNonNullString("[undefined]").Trim();

                    definedDevice.StationName = row["Name"].ToNonNullString(deviceName).Trim().TruncateRight(definedDevice.MaximumStationNameLength);
                    definedDevice.IDLabel = deviceName.TruncateRight(definedDevice.IDLabelLength);
                    definedDevice.Tag = uint.Parse(row["ID"].ToString());
                    definedDevice.Source = this;

                    devicedAdded = false;

                    if (m_forceLabelMapping)
                    {
                        // When forcing label mapping we always try to use label for unique lookup
                        if ((object)m_labelDefinedDevices == null)
                            m_labelDefinedDevices = new ConcurrentDictionary<string, DeviceStatisticsHelper<ConfigurationCell>>(StringComparer.OrdinalIgnoreCase);

                        // See if label already exists in this collection
                        if (m_labelDefinedDevices.ContainsKey(definedDevice.StationName))
                        {
                            // For devices that do not have unique labels when forcing label mapping, we fall back on its ID code for unique lookup
                            if (m_definedDevices.ContainsKey(definedDevice.IDCode))
                            {
                                OnProcessException(new InvalidOperationException(string.Format("ERROR: Device ID \"{0}\", labeled \"{1}\", was not unique in the {2} input stream. Data from devices that are not distinctly defined by ID code or label will not be correctly parsed until uniquely identified.", definedDevice.IDCode, definedDevice.StationName, Name)));
                            }
                            else
                            {
                                m_definedDevices.TryAdd(definedDevice.IDCode, new DeviceStatisticsHelper<ConfigurationCell>(definedDevice));
                                RegisterStatistics(definedDevice, definedDevice.IDLabel, "Device", "PMU");
                                devicedAdded = true;
                            }
                        }
                        else
                        {
                            m_labelDefinedDevices.TryAdd(definedDevice.StationName, new DeviceStatisticsHelper<ConfigurationCell>(definedDevice));
                            RegisterStatistics(definedDevice, definedDevice.IDLabel, "Device", "PMU");
                            devicedAdded = true;
                        }
                    }
                    else
                    {
                        // See if key already exists in this collection
                        if (m_definedDevices.ContainsKey(definedDevice.IDCode))
                        {
                            // For devices that do not have unique ID codes, we fall back on its label for unique lookup
                            if ((object)m_labelDefinedDevices == null)
                                m_labelDefinedDevices = new ConcurrentDictionary<string, DeviceStatisticsHelper<ConfigurationCell>>(StringComparer.OrdinalIgnoreCase);

                            if (m_labelDefinedDevices.ContainsKey(definedDevice.StationName))
                            {
                                OnProcessException(new InvalidOperationException(string.Format("ERROR: Device ID \"{0}\", labeled \"{1}\", was not unique in the {2} input stream. Data from devices that are not distinctly defined by ID code or label will not be correctly parsed until uniquely identified.", definedDevice.IDCode, definedDevice.StationName, Name)));
                            }
                            else
                            {
                                m_labelDefinedDevices.TryAdd(definedDevice.StationName, new DeviceStatisticsHelper<ConfigurationCell>(definedDevice));
                                RegisterStatistics(definedDevice, definedDevice.IDLabel, "Device", "PMU");
                                devicedAdded = true;
                            }
                        }
                        else
                        {
                            m_definedDevices.TryAdd(definedDevice.IDCode, new DeviceStatisticsHelper<ConfigurationCell>(definedDevice));
                            RegisterStatistics(definedDevice, definedDevice.IDLabel, "Device", "PMU");
                            devicedAdded = true;
                        }
                    }

                    if (devicedAdded)
                    {
                        // Create status display string for expected device
                        deviceStatus.Append("   Device ");
                        deviceStatus.Append((index++).ToString("00"));
                        deviceStatus.Append(": ");
                        deviceStatus.Append(definedDevice.StationName);
                        deviceStatus.Append(" (");
                        deviceStatus.Append(definedDevice.IDCode);
                        deviceStatus.Append(')');
                        deviceStatus.AppendLine();
                    }
                }

                OnStatusMessage(deviceStatus.ToString());

                if ((object)m_labelDefinedDevices != null)
                {
                    if (m_forceLabelMapping)
                        OnStatusMessage("{0} has {1} defined input devices that are using the device label for identification since connection has been forced to use label mapping. This is not the optimal configuration.", Name, m_labelDefinedDevices.Count);
                    else
                        OnStatusMessage("WARNING: {0} has {1} defined input devices that do not have unique ID codes (i.e., the AccessID), as a result system will use the device label for identification. This is not the optimal configuration.", Name, m_labelDefinedDevices.Count);
                }
            }
            else
            {
                // Making a connection to a single device
                definedDevice = new ConfigurationCell(m_accessID);

                // Used shared mapping name for single device connection if defined - this causes measurement mappings to be associated
                // with alternate device by caching signal references associated with shared mapping acronym
                if (string.IsNullOrWhiteSpace(SharedMapping))
                    deviceName = Name.ToNonNullString("[undefined]").Trim();
                else
                    deviceName = SharedMapping;

                definedDevice.StationName = deviceName.TruncateRight(definedDevice.MaximumStationNameLength);
                definedDevice.IDLabel = deviceName.TruncateRight(definedDevice.IDLabelLength);
                definedDevice.Tag = ID;
                definedDevice.Source = this;

                // When forcing label mapping we always try to use label for unique lookup instead of ID code
                if (m_forceLabelMapping)
                {
                    if ((object)m_labelDefinedDevices == null)
                        m_labelDefinedDevices = new ConcurrentDictionary<string, DeviceStatisticsHelper<ConfigurationCell>>(StringComparer.OrdinalIgnoreCase);

                    m_labelDefinedDevices.TryAdd(definedDevice.StationName, new DeviceStatisticsHelper<ConfigurationCell>(definedDevice));
                    RegisterStatistics(definedDevice, definedDevice.IDLabel, "Device", "PMU");

                    OnStatusMessage("Input device is using the device label for identification since connection has been forced to use label mapping. This is not the optimal configuration.");
                }
                else
                {
                    m_definedDevices.TryAdd(definedDevice.IDCode, new DeviceStatisticsHelper<ConfigurationCell>(definedDevice));
                    RegisterStatistics(definedDevice, definedDevice.IDLabel, "Device", "PMU");
                }
            }
        }

        // Load active device measurements for this mapper connection
        private void LoadDeviceMeasurements()
        {
            DefinedMeasurement definedMeasurement;
            Guid signalID;
            string signalReference;

            Dictionary<string, DefinedMeasurement> definedMeasurements = new Dictionary<string, DefinedMeasurement>();

            // Have to use a Convert expression for DeviceID column in Select function here since SQLite doesn't report data types for COALESCE based columns
            foreach (DataRow row in DataSource.Tables["ActiveMeasurements"].Select(string.Format("Convert(DeviceID, 'System.String')='{0}'", SharedMappingID)))
            {
                signalReference = row["SignalReference"].ToString();

                if (!string.IsNullOrWhiteSpace(signalReference))
                {
                    try
                    {
                        // Get measurement's signal ID
                        signalID = new Guid(row["SignalID"].ToNonNullString(Guid.NewGuid().ToString()));

                        // Create a measurement with a reference associated with this adapter
                        definedMeasurement = new DefinedMeasurement
                        {
                            Key = MeasurementKey.LookUpOrCreate(signalID, row["ID"].ToString()),
                            TagName = signalReference,
                            Adder = double.Parse(row["Adder"].ToNonNullString("0.0")),
                            Multiplier = double.Parse(row["Multiplier"].ToNonNullString("1.0"))
                        };

                        // Add measurement to definition list keyed by signal reference
                        if (!definedMeasurements.ContainsKey(signalReference))
                            definedMeasurements.Add(signalReference, definedMeasurement);
                    }
                    catch (Exception ex)
                    {
                        OnProcessException(new InvalidOperationException(string.Format("Failed to load signal reference \"{0}\" due to exception: {1}", signalReference, ex.Message), ex));
                    }
                }
            }

            m_definedMeasurements = definedMeasurements;

            // Update output measurements that input adapter can provide such that it can participate in connect on demand
            if (definedMeasurements.Count > 0)
            {
                Func<DefinedMeasurement, IMeasurement> converter = measurement => (IMeasurement)new Measurement()
                {
                    Key = measurement.Key,
                    TagName = measurement.TagName,
                    Adder = measurement.Adder,
                    Multiplier = measurement.Multiplier
                };

                OutputMeasurements = definedMeasurements.Values.Select(converter).ToArray();
            }
            else
            {
                OutputMeasurements = null;
            }

            OnStatusMessage("Loaded {0} active device measurements...", definedMeasurements.Count);
        }

        /// <summary>
        /// Sends the specified <see cref="DeviceCommand"/> to the current device connection.
        /// </summary>
        /// <param name="command"><see cref="DeviceCommand"/> to send to connected device.</param>
        [AdapterCommand("Sends the specified command to connected phasor device.", "Administrator", "Editor")]
        public void SendCommand(DeviceCommand command)
        {
            if ((object)m_frameParser != null)
            {
                if ((object)m_frameParser.SendDeviceCommand(command) != null)
                    OnStatusMessage("Sent device command \"{0}\"...", command);
            }
            else
            {
                OnStatusMessage("Failed to send device command \"{0}\", no frame parser is defined.", command);
            }
        }

        /// <summary>
        /// Resets the statistics of all devices associated with this connection.
        /// </summary>
        [AdapterCommand("Resets the statistics of all devices associated with this connection.", "Administrator", "Editor")]
        public void ResetStatistics()
        {
            if ((object)m_definedDevices != null)
            {
                foreach (ConfigurationCell definedDevice in DefinedDevices)
                {
                    definedDevice.DataQualityErrors = 0;
                    definedDevice.DeviceErrors = 0;
                    definedDevice.TotalFrames = 0;
                    definedDevice.TimeQualityErrors = 0;
                }

                m_outOfOrderFrames = 0;

                OnStatusMessage("Statistics reset for all devices associated with this connection.");
            }
            else
            {
                OnStatusMessage("Failed to reset statistics, no devices are defined.");
            }
        }

        /// <summary>
        /// Resets the statistics of the specified device associated with this connection.
        /// </summary>
        /// <param name="idCode">Integer ID code of device on which to reset statistics.</param>
        [AdapterCommand("Resets the statistics of the device with the specified ID code.", "Administrator", "Editor")]
        public void ResetDeviceStatistics(ushort idCode)
        {
            if ((object)m_definedDevices != null)
            {
                DeviceStatisticsHelper<ConfigurationCell> statisticsHelper;
                ConfigurationCell definedDevice;

                if (m_definedDevices.TryGetValue(idCode, out statisticsHelper))
                {
                    definedDevice = statisticsHelper.Device;
                    definedDevice.DataQualityErrors = 0;
                    definedDevice.DeviceErrors = 0;
                    definedDevice.TotalFrames = 0;
                    definedDevice.TimeQualityErrors = 0;

                    OnStatusMessage("Statistics reset for device with ID code \"{0}\" associated with this connection.", idCode);
                }
                else
                {
                    OnStatusMessage("WARNING: Failed to find device with ID code \"{0}\" associated with this connection.", idCode);
                }
            }
            else
            {
                OnStatusMessage("Failed to reset statistics, no devices are defined.");
            }
        }

        /// <summary>
        /// Resets the counters for the lifetime statistics without interrupting the adapter's operations.
        /// </summary>
        [AdapterCommand("Resets the counters for the lifetime statistics without interrupting the adapter's operations.", "Administrator", "Editor")]
        public virtual void ResetLifetimeCounters()
        {
            m_lifetimeMeasurements = 0L;
            m_lifetimeTotalLatency = 0L;
            m_lifetimeMinimumLatency = 0L;
            m_lifetimeMaximumLatency = 0L;
            m_lifetimeLatencyFrames = 0L;

            if ((object)m_frameParser != null)
                m_frameParser.ResetTotalBytesReceived();
        }

        /// <summary>
        /// Resets counters related to latency calculations.
        /// </summary>
        [AdapterCommand("Resets the latency counters for the device without interrupting the adapter's operations.", "Administrator", "Editor")]
        public void ResetLatencyCounters()
        {
            m_minimumLatency = 0;
            m_maximumLatency = 0;
            m_totalLatency = 0;
            m_latencyFrames = 0;
        }

        /// <summary>
        /// Attempts to delete the last known good (i.e., cached) configuration.
        /// </summary>
        [AdapterCommand("Attempts to delete the last known good configuration.", "Administrator", "Editor")]
        public void DeleteCachedConfiguration()
        {
            lock (m_configurationOperationLock)
            {
                try
                {
                    ConfigurationFrame.DeleteCachedConfiguration(Name);
                    m_frameParser.ConfigurationFrame = null;
                    m_receivedConfigFrame = false;
                    OnStatusMessage("Cached configuration file \"{0}\" was deleted.", ConfigurationCacheFileName);
                    SendCommand(DeviceCommand.SendConfigurationFrame2);
                }
                catch (Exception ex)
                {
                    OnProcessException(new InvalidOperationException(string.Format("Failed to delete cached configuration \"{0}\": {1}", ConfigurationCacheFileName, ex.Message), ex));
                }
            }
        }

        /// <summary>
        /// Returns the current configuration frame to the caller.
        /// </summary>
        /// <returns>A <see cref="IConfigurationFrame"/> if successful, -or- <c>null</c> if request failed.</returns>
        [AdapterCommand("Requests the current configuration frame and returns it to the caller.", "Administrator", "Editor")]
        public IConfigurationFrame RequestCurrentConfiguration()
        {
            return m_frameParser.ConfigurationFrame;
        }

        /// <summary>
        /// Attempts to load the last known good (i.e., cached) configuration.
        /// </summary>
        [AdapterCommand("Attempts to load the last known good configuration.", "Administrator", "Editor")]
        public void LoadCachedConfiguration()
        {
            lock (m_configurationOperationLock)
            {
                try
                {
                    IConfigurationFrame configFrame = ConfigurationFrame.GetCachedConfiguration(Name, true);

                    // As soon as a configuration frame is made available to the frame parser, regardless of source,
                    // full parsing of data frames can begin...
                    if ((object)configFrame != null)
                    {
                        m_frameParser.ConfigurationFrame = configFrame;
                        StartMeasurementCounter();
                        m_receivedConfigFrame = true;
                        m_configurationChanges++;
                    }
                    else
                    {
                        OnStatusMessage("NOTICE: Cannot load cached configuration, file \"{0}\" does not exist.", ConfigurationCacheFileName);
                    }
                }
                catch (Exception ex)
                {
                    OnProcessException(new InvalidOperationException(string.Format("Failed to load cached configuration \"{0}\": {1}", ConfigurationCacheFileName, ex.Message), ex));
                }
            }
        }

        /// <summary>
        /// Attempts to load the specified configuration.
        /// </summary>
        /// <param name="configurationFileName">Path and file name containing serialized configuration.</param>
        [AdapterCommand("Attempts to load the specified configuration.", "Administrator", "Editor")]
        public void LoadConfiguration(string configurationFileName)
        {
            lock (m_configurationOperationLock)
            {
                try
                {
                    IConfigurationFrame configFrame = ConfigurationFrame.GetCachedConfiguration(configurationFileName, false);

                    // As soon as a configuration frame is made available to the frame parser, regardless of source,
                    // full parsing of data frames can begin...
                    if ((object)configFrame != null)
                    {
                        m_frameParser.ConfigurationFrame = configFrame;

                        try
                        {
                            // Cache this configuration frame since its being loaded as the new last known good configuration
                            ConfigurationFrame.Cache(configFrame, OnProcessException, Name);
                        }
                        catch (Exception ex)
                        {
                            // Process exception for logging
                            OnProcessException(new InvalidOperationException("Failed to queue caching of config frame due to exception: " + ex.Message, ex));
                        }

                        StartMeasurementCounter();

                        m_receivedConfigFrame = true;
                        m_configurationChanges++;
                    }
                    else
                    {
                        OnStatusMessage("NOTICE: Cannot load configuration, file \"{0}\" does not exist.", configurationFileName);
                    }
                }
                catch (Exception ex)
                {
                    OnProcessException(new InvalidOperationException(string.Format("Failed to load configuration \"{0}\": {1}", configurationFileName, ex.Message), ex));
                }
            }
        }

        /// <summary>
        /// Gets a short one-line status of this <see cref="PhasorMeasurementMapper"/>.
        /// </summary>
        /// <param name="maxLength">Maximum number of available characters for display.</param>
        /// <returns>A short one-line summary of the current status of this <see cref="PhasorMeasurementMapper"/>.</returns>
        public override string GetShortStatus(int maxLength)
        {
            StringBuilder status = new StringBuilder();

            if ((object)m_frameParser != null && m_frameParser.IsConnected)
            {
                if (m_lastReportTime > 0)
                {
                    // Calculate total bad frames
                    long totalDataErrors = 0;

                    foreach (ConfigurationCell definedDevice in DefinedDevices)
                    {
                        totalDataErrors += definedDevice.DataQualityErrors;
                    }

                    // Generate a short connect time
                    Time connectionTime = m_frameParser.ConnectionTime;
                    string uptime;

                    if (connectionTime.ToDays() < 1.0D)
                    {
                        if (connectionTime.ToHours() < 1.0D)
                        {
                            if (connectionTime.ToMinutes() < 1.0D)
                                uptime = (int)connectionTime + " seconds";
                            else
                                uptime = connectionTime.ToMinutes().ToString("0.0") + " minutes";
                        }
                        else
                        {
                            uptime = connectionTime.ToHours().ToString("0.00") + " hours";
                        }
                    }
                    else
                    {
                        uptime = connectionTime.ToDays().ToString("0.00") + " days";
                    }

                    string uptimeStats = string.Format("Up for {0}, {1} errors", uptime, totalDataErrors);
                    string runtimeStats = string.Format(" {0} {1} fps", ((DateTime)m_lastReportTime).ToString("MM/dd/yyyy HH:mm:ss.fff"), m_frameParser.CalculatedFrameRate.ToString("0.00"));

                    uptimeStats = uptimeStats.TruncateRight(maxLength - runtimeStats.Length).PadLeft(maxLength - runtimeStats.Length, '\xA0');

                    status.Append(uptimeStats);
                    status.Append(runtimeStats);
                }
                else if ((object)m_frameParser.ConfigurationFrame == null)
                {
                    status.AppendFormat("  >> Awaiting configuration frame - {0} bytes received", m_frameParser.TotalBytesReceived);
                }
                else
                {
                    status.AppendFormat("  ** No data mapped, check configuration - {0} bytes received", m_frameParser.TotalBytesReceived);
                }
            }
            else
            {
                status.Append("  ** Not connected");
            }

            return status.ToString();
        }

        /// <summary>
        /// Attempts to connect to data input source.
        /// </summary>
        protected override void AttemptConnection()
        {
            m_lastReportTime = 0;
            m_bytesReceived = 0;
            m_outOfOrderFrames = 0;
            m_receivedConfigFrame = false;
            m_cachedConfigLoadAttempted = false;

            // Start frame parser
            if ((object)m_frameParser != null)
                m_frameParser.Start();
        }

        /// <summary>
        /// Attempts to disconnect from data input source.
        /// </summary>
        protected override void AttemptDisconnection()
        {
            // Stop data stream monitor
            m_dataStreamMonitor.Enabled = false;

            // Stop frame parser
            if ((object)m_frameParser != null)
                m_frameParser.Stop();

            if ((object)m_measurementCounter != null)
                m_measurementCounter.Stop();
        }

        /// <summary>
        /// Map parsed measurement value to defined measurement attributes (i.e., assign meta-data to parsed measured value).
        /// </summary>
        /// <param name="mappedMeasurements">Destination collection for the mapped measurement values.</param>
        /// <param name="signalReference">Derived <see cref="SignalReference"/> string for the parsed measurement value.</param>
        /// <param name="parsedMeasurement">The parsed <see cref="IMeasurement"/> value.</param>
        /// <remarks>
        /// This procedure is used to identify a parsed measurement value by its derived signal reference and apply the
        /// additional needed measurement meta-data attributes (i.e., ID, Source, Adder and Multiplier).
        /// </remarks>
        protected void MapMeasurementAttributes(List<IMeasurement> mappedMeasurements, string signalReference, IMeasurement parsedMeasurement)
        {
            // Coming into this function the parsed measurement value will only have a "value" and a "timestamp";
            // the measurement will not yet be associated with an actual historian measurement ID as the measurement
            // will have come directly out of the parsed phasor protocol data frame.  We take the generated signal
            // reference and use that to lookup the actual historian measurement ID, source, adder and multiplier.
            DefinedMeasurement definedMeasurement;

            // Lookup signal reference in defined measurement list
            if (m_definedMeasurements.TryGetValue(signalReference, out definedMeasurement))
            {
                // Assign ID and other relevant attributes to the parsed measurement value
                parsedMeasurement.Key = definedMeasurement.Key;
                parsedMeasurement.Adder = definedMeasurement.Adder;              // Allows for run-time additive measurement value adjustments
                parsedMeasurement.Multiplier = definedMeasurement.Multiplier;    // Allows for run-time multiplicative measurement value adjustments

                // Add the updated measurement value to the destination measurement collection
                mappedMeasurements.Add(parsedMeasurement);
            }
        }

        /// <summary>
        /// Extract frame measurements and add expose them via the <see cref="IInputAdapter.NewMeasurements"/> event.
        /// </summary>
        /// <param name="frame">Phasor data frame to extract measurements from.</param>
        protected void ExtractFrameMeasurements(IDataFrame frame)
        {
            const int AngleIndex = (int)CompositePhasorValue.Angle;
            const int MagnitudeIndex = (int)CompositePhasorValue.Magnitude;
            const int FrequencyIndex = (int)CompositeFrequencyValue.Frequency;
            const int DfDtIndex = (int)CompositeFrequencyValue.DfDt;

            List<IMeasurement> mappedMeasurements = new List<IMeasurement>(m_lastMeasurementMappedCount);
            DeviceStatisticsHelper<ConfigurationCell> statisticsHelper;
            ConfigurationCell definedDevice;
            PhasorValueCollection phasors;
            AnalogValueCollection analogs;
            DigitalValueCollection digitals;
            IMeasurement[] measurements;
            long timestamp;
            int x, count;

            // Adjust time to UTC based on source time zone
            if (!m_timezone.Equals(TimeZoneInfo.Utc))
                frame.Timestamp = TimeZoneInfo.ConvertTimeToUtc(frame.Timestamp, m_timezone);

            // We also allow "fine tuning" of time for fickle GPS clocks...
            if (m_timeAdjustmentTicks.Value != 0)
                frame.Timestamp += m_timeAdjustmentTicks;

            // Get adjusted timestamp of this frame
            timestamp = frame.Timestamp;

            // Track latest reporting time for mapper
            if (timestamp > m_lastReportTime.Value)
                m_lastReportTime = timestamp;
            else
                m_outOfOrderFrames++;

            // Track latency statistics against system time - in order for these statistics
            // to be useful, the local clock must be fairly accurate
            long latency = frame.ReceivedTimestamp.Value - timestamp;

            if (m_minimumLatency > latency || m_minimumLatency == 0)
                m_minimumLatency = latency;

            if (m_maximumLatency < latency || m_maximumLatency == 0)
                m_maximumLatency = latency;

            m_totalLatency += latency;
            m_latencyFrames++;

            if (m_lifetimeMinimumLatency > latency || m_lifetimeMinimumLatency == 0)
                m_lifetimeMinimumLatency = latency;

            if (m_lifetimeMaximumLatency < latency || m_lifetimeMaximumLatency == 0)
                m_lifetimeMaximumLatency = latency;

            m_lifetimeTotalLatency += latency;
            m_lifetimeLatencyFrames++;

            // Map quality flags (QF) from device frame, if any
            MapMeasurementAttributes(mappedMeasurements, m_qualityFlagsSignalReferenceName, frame.GetQualityFlagsMeasurement());

            // Loop through each parsed device in the data frame
            foreach (IDataCell parsedDevice in frame.Cells)
            {
                try
                {
                    // Lookup device by its label (if needed), then by its ID code
                    if (((object)m_labelDefinedDevices != null &&
                        m_labelDefinedDevices.TryGetValue(parsedDevice.StationName.ToNonNullString(), out statisticsHelper)) ||
                        m_definedDevices.TryGetValue(parsedDevice.IDCode, out statisticsHelper))
                    {
                        definedDevice = statisticsHelper.Device;

                        // Track latest reporting time for this device
                        if (timestamp > definedDevice.LastReportTime.Value)
                            definedDevice.LastReportTime = timestamp;

                        // Track quality statistics for this device
                        definedDevice.TotalFrames++;

                        if (!parsedDevice.DataIsValid)
                            definedDevice.DataQualityErrors++;

                        if (!parsedDevice.SynchronizationIsValid)
                            definedDevice.TimeQualityErrors++;

                        if (parsedDevice.DeviceError)
                            definedDevice.DeviceErrors++;

                        // Map status flags (SF) from device data cell itself
                        MapMeasurementAttributes(mappedMeasurements, definedDevice.GetSignalReference(SignalKind.Status), parsedDevice.GetStatusFlagsMeasurement());

                        // Map phase angles (PAn) and magnitudes (PMn)
                        phasors = parsedDevice.PhasorValues;
                        count = phasors.Count;

                        for (x = 0; x < count; x++)
                        {
                            // Get composite phasor measurements
                            measurements = phasors[x].Measurements;

                            // Map angle
                            MapMeasurementAttributes(mappedMeasurements, definedDevice.GetSignalReference(SignalKind.Angle, x, count), measurements[AngleIndex]);

                            // Map magnitude
                            MapMeasurementAttributes(mappedMeasurements, definedDevice.GetSignalReference(SignalKind.Magnitude, x, count), measurements[MagnitudeIndex]);
                        }

                        // Map frequency (FQ) and dF/dt (DF)
                        measurements = parsedDevice.FrequencyValue.Measurements;

                        // Map frequency
                        MapMeasurementAttributes(mappedMeasurements, definedDevice.GetSignalReference(SignalKind.Frequency), measurements[FrequencyIndex]);

                        // Map dF/dt
                        MapMeasurementAttributes(mappedMeasurements, definedDevice.GetSignalReference(SignalKind.DfDt), measurements[DfDtIndex]);

                        // Map analog values (AVn)
                        analogs = parsedDevice.AnalogValues;
                        count = analogs.Count;

                        for (x = 0; x < count; x++)
                        {
                            // Map analog value
                            MapMeasurementAttributes(mappedMeasurements, definedDevice.GetSignalReference(SignalKind.Analog, x, count), analogs[x].Measurements[0]);
                        }

                        // Map digital values (DVn)
                        digitals = parsedDevice.DigitalValues;
                        count = digitals.Count;

                        for (x = 0; x < count; x++)
                        {
                            // Map digital value
                            MapMeasurementAttributes(mappedMeasurements, definedDevice.GetSignalReference(SignalKind.Digital, x, count), digitals[x].Measurements[0]);
                        }

                        // Track measurement count statistics for this device
                        statisticsHelper.AddToMeasurementsReceived(CountParsedMeasurements(parsedDevice));
                    }
                    else
                    {
                        // Encountered an undefined device, track frame counts
                        long frameCount;

                        if (m_undefinedDevices.TryGetValue(parsedDevice.StationName, out frameCount))
                        {
                            frameCount++;
                            m_undefinedDevices[parsedDevice.StationName] = frameCount;
                        }
                        else
                        {
                            m_undefinedDevices.TryAdd(parsedDevice.StationName, 1);
                            OnStatusMessage("WARNING: Encountered an undefined device \"{0}\"...", parsedDevice.StationName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    OnProcessException(new InvalidOperationException(string.Format("Exception encountered while mapping \"{0}\" data frame cell \"{1}\" elements to measurements: {2}", Name, parsedDevice.StationName.ToNonNullString("[undefined]"), ex.Message), ex));
                }
            }

            m_lastMeasurementMappedCount = mappedMeasurements.Count;

            // Provide real-time measurements where needed
            OnNewMeasurements(mappedMeasurements);

            int measurementCount = mappedMeasurements.Count;
            m_lifetimeMeasurements += measurementCount;
            UpdateMeasurementsPerSecond(measurementCount);
        }

        // Could parsed measurements for this device
        private int CountParsedMeasurements(IDataCell parsedDevice)
        {
            int parsedMeasurementCount = 0;

            var phasorValues = parsedDevice.PhasorValues;
            var digitalValues = parsedDevice.DigitalValues;
            var analogValues = parsedDevice.AnalogValues;
            var frequencyValue = parsedDevice.FrequencyValue;
            int count;

            count = phasorValues.Count;
            for (int x = 0; x < count; x++)
            {
                foreach (IMeasurement measurement in phasorValues[x].Measurements)
                {
                    if (!double.IsNaN(measurement.Value))
                        parsedMeasurementCount++;
                }
            }

            count = digitalValues.Count;
            for (int x = 0; x < count; x++)
            {
                foreach (IMeasurement measurement in digitalValues[x].Measurements)
                {
                    if (!double.IsNaN(measurement.Value))
                        parsedMeasurementCount++;
                }
            }

            count = analogValues.Count;
            for (int x = 0; x < count; x++)
            {
                foreach (IMeasurement measurement in analogValues[x].Measurements)
                {
                    if (!double.IsNaN(measurement.Value))
                        parsedMeasurementCount++;
                }
            }

            // Ignore frequency measurements when frequency value is zero - some PDCs use
            // zero for missing frequency values
            if (frequencyValue.Frequency != 0.0D)
            {
                foreach (IMeasurement measurement in frequencyValue.Measurements)
                {
                    if (!double.IsNaN(measurement.Value))
                        parsedMeasurementCount++;
                }
            }

            return parsedMeasurementCount;
        }

        /// <summary>
        /// Get signal reference for specified <see cref="SignalKind"/>.
        /// </summary>
        /// <param name="type"><see cref="SignalKind"/> to request signal reference for.</param>
        /// <returns>Signal reference of given <see cref="SignalKind"/>.</returns>
        public string GetSignalReference(SignalKind type)
        {
            // We cache non-indexed signal reference strings so they don't need to be generated at each mapping call.
            string[] references;

            // Look up synonym in dictionary based on signal type, if found return single element
            if (m_generatedSignalReferenceCache.TryGetValue(type, out references))
                return references[0];

            // Create a new signal reference array (for single element)
            references = new string[1];

            // Create and cache new non-indexed signal reference
            references[0] = SignalReference.ToString(Name + "!IS", type);

            // Cache generated signal synonym
            m_generatedSignalReferenceCache.TryAdd(type, references);

            return references[0];
        }

        /// <summary>
        /// Get signal reference for specified <see cref="SignalKind"/> and <paramref name="index"/>.
        /// </summary>
        /// <param name="type"><see cref="SignalKind"/> to request signal reference for.</param>
        /// <param name="index">Index <see cref="SignalKind"/> to request signal reference for.</param>
        /// <param name="count">Number of signals defined for this <see cref="SignalKind"/>.</param>
        /// <returns>Signal reference of given <see cref="SignalKind"/> and <paramref name="index"/>.</returns>
        public string GetSignalReference(SignalKind type, int index, int count)
        {
            // We cache indexed signal reference strings so they don't need to be generated at each mapping call.
            // For speed purposes we intentionally do not validate that signalIndex falls within signalCount, be
            // sure calling procedures are very careful with parameters...
            string[] references;

            // Look up synonym in dictionary based on signal type
            if (m_generatedSignalReferenceCache.TryGetValue(type, out references))
            {
                // Verify signal count has not changed (we may have received new configuration from device)
                if (count == references.Length)
                {
                    // Create and cache new signal reference if it doesn't exist
                    if ((object)references[index] == null)
                        references[index] = SignalReference.ToString(Name + "!IS", type, index + 1);

                    return references[index];
                }
            }

            // Create a new indexed signal reference array
            references = new string[count];

            // Create and cache new signal reference
            references[index] = SignalReference.ToString(Name + "!IS", type, index + 1);

            // Cache generated signal synonym array
            m_generatedSignalReferenceCache.TryAdd(type, references);

            return references[index];
        }

        private void StartMeasurementCounter()
        {
            long now = DateTime.UtcNow.Ticks;

            DataSet dataSource;
            DataTable measurementTable;
            IConfigurationFrame configurationFrame;
            int measurementsPerFrame;

            if (!m_receivedConfigFrame)
            {
                // If this is the first time we've received the configuration frame,
                // we'll use it to calculate expected measurements per second for each device
                dataSource = DataSource;

                if ((object)dataSource != null && dataSource.Tables.Contains("ActiveMeasurements"))
                {
                    measurementTable = dataSource.Tables["ActiveMeasurements"];
                    configurationFrame = m_frameParser.ConfigurationFrame;

                    foreach (DeviceStatisticsHelper<ConfigurationCell> statisticsHelper in StatisticsHelpers)
                    {
                        measurementsPerFrame = measurementTable.Select(string.Format("SignalReference LIKE '{0}-%' AND SignalType <> 'FLAG' AND SignalType <> 'STAT'", statisticsHelper.Device.IDLabel)).Length;
                        statisticsHelper.ExpectedMeasurementsPerSecond = configurationFrame.FrameRate * measurementsPerFrame;
                        statisticsHelper.Reset(now);
                    }
                }
            }

            if ((object)m_measurementCounter == null)
            {
                // Create the timer if it doesn't already exist
                m_measurementCounter = new Timer(1000.0D);
                m_measurementCounter.Elapsed += m_measurementCounter_Elapsed;
            }

            // Start the measurement counter timer
            // to start gathering statistics
            m_measurementCounter.Start();
        }

        // Updates the measurements per second counters after receiving another set of measurements.
        private void UpdateMeasurementsPerSecond(int measurementCount)
        {
            long secondsSinceEpoch = DateTime.UtcNow.Ticks / Ticks.PerSecond;

            if (secondsSinceEpoch > m_lastSecondsSinceEpoch)
            {
                if (m_measurementsInSecond < m_minimumMeasurementsPerSecond || m_minimumMeasurementsPerSecond == 0L)
                    m_minimumMeasurementsPerSecond = m_measurementsInSecond;

                if (m_measurementsInSecond > m_maximumMeasurementsPerSecond || m_maximumMeasurementsPerSecond == 0L)
                    m_maximumMeasurementsPerSecond = m_measurementsInSecond;

                m_totalMeasurementsPerSecond += m_measurementsInSecond;
                m_measurementsPerSecondCount++;
                m_measurementsInSecond = 0L;

                m_lastSecondsSinceEpoch = secondsSinceEpoch;
            }

            m_measurementsInSecond += measurementCount;
        }

        // Resets the measurements per second counters after reading the values from the last calculation interval.
        private void ResetMeasurementsPerSecondCounters()
        {
            m_minimumMeasurementsPerSecond = 0L;
            m_maximumMeasurementsPerSecond = 0L;
            m_totalMeasurementsPerSecond = 0L;
            m_measurementsPerSecondCount = 0L;
        }

        // Replace with secure notification mechanism handlers for notification when subordinate phasor adapter needs to kick on when primary data feed goes offline (if feature is still deemed useful)

        //// Primary data source connection has terminated, engage backup connection
        //private void m_primaryDataSource_ConnectionTerminated(object sender, EventArgs e)
        //{
        //    OnStatusMessage("WARNING: Primary data source connection was terminated, attempting to engage backup connection...");
        //    Start();
        //}

        //// Primary data source connection has been reestablished, disengage backup connection
        //private void m_primaryDataSource_ConnectionEstablished(object sender, EventArgs e)
        //{
        //    if (Enabled)
        //        OnStatusMessage("Primary data source connection has been reestablished, disengaging backup connection...");

        //    Stop();
        //}

        private void m_frameParser_ReceivedDataFrame(object sender, EventArgs<IDataFrame> e)
        {
            ExtractFrameMeasurements(e.Argument);
            m_totalDataFrames++;

            if (m_frameParser.RedundantFramesPerPacket > 0)
            {
                if ((object)m_missingDataMonitor == null)
                {
                    m_missingDataMonitor = new MissingDataMonitor
                    {
                        LagTime = m_lagTime,
                        LeadTime = m_leadTime,
                        FramesPerSecond = m_frameParser.DefinedFrameRate,
                        TimeResolution = m_timeResolution,
                        UsePrecisionTimer = false
                    };
                }

                m_missingDataMonitor.RedundantFramesPerPacket = m_frameParser.RedundantFramesPerPacket;
                m_missingDataMonitor.SortMeasurements(e.Argument.Cells.Select(cell => cell.GetStatusFlagsMeasurement()));
            }
        }

        private void m_frameParser_ReceivedConfigurationFrame(object sender, EventArgs<IConfigurationFrame> e)
        {
            lock (m_configurationOperationLock)
            {
                if (!m_receivedConfigFrame)
                {
                    OnStatusMessage("Received configuration frame at {0}", DateTime.UtcNow);

                    try
                    {
                        // Cache configuration on an independent thread in case this takes some time
                        ConfigurationFrame.Cache(e.Argument, OnProcessException, Name);
                    }
                    catch (Exception ex)
                    {
                        // Process exception for logging
                        OnProcessException(new InvalidOperationException("Failed to queue caching of config frame due to exception: " + ex.Message, ex));
                    }

                    StartMeasurementCounter();

                    m_receivedConfigFrame = true;
                }
            }

            m_configurationChanges++;
            m_totalConfigurationFrames++;
        }

        private void m_frameParser_ReceivedHeaderFrame(object sender, EventArgs<IHeaderFrame> e)
        {
            m_totalHeaderFrames++;
        }

        private void m_frameParser_ReceivedFrameImage(object sender, EventArgs<FundamentalFrameType, int> e)
        {
            // We track bytes received so that connection can be restarted if data is not flowing
            m_bytesReceived += e.Argument2;
        }

        private void m_frameParser_ConnectionTerminated(object sender, EventArgs e)
        {
            OnDisconnected();

            if (m_frameParser.Enabled)
            {
                // Communications layer closed connection (close not initiated by system) - so we restart connection cycle...
                OnStatusMessage("WARNING: Connection closed by remote device, attempting reconnection...");
                Start();
            }
        }

        private void m_frameParser_ConnectionEstablished(object sender, EventArgs e)
        {
            OnConnected();

            ResetStatistics();

            // Enable data stream monitor for connections that support commands
            m_dataStreamMonitor.Enabled = m_frameParser.DeviceSupportsCommands || m_allowUseOfCachedConfiguration;
        }

        private void m_frameParser_ConnectionException(object sender, EventArgs<Exception, int> e)
        {
            Exception ex = e.Argument1;
            OnProcessException(new InvalidOperationException(string.Format("Connection attempt failed: {0}", ex.Message), ex));

            // So long as user hasn't requested to stop, keep trying connection
            if (Enabled)
                Start();
        }

        private void m_frameParser_ParsingException(object sender, EventArgs<Exception> e)
        {
            OnProcessException(e.Argument);
        }

        private void m_frameParser_ExceededParsingExceptionThreshold(object sender, EventArgs e)
        {
            OnStatusMessage("\r\nConnection is being reset due to an excessive number of exceptions...\r\n");

            // So long as user hasn't already requested to stop, we restart connection
            if (Enabled)
                Start();
        }

        private void m_frameParser_ConnectionAttempt(object sender, EventArgs e)
        {
            OnStatusMessage("Initiating {0} {1} based connection...", m_frameParser.PhasorProtocol.GetFormattedProtocolName(), m_frameParser.TransportProtocol.ToString().ToUpper());
            m_connectionAttempts++;
        }

        private void m_frameParser_ConfigurationChanged(object sender, EventArgs e)
        {
            OnStatusMessage("NOTICE: Configuration has changed, requesting new configuration frame...");

            // Reset data stream monitor to allow time for non-cached reception of new configuration frame...
            if (m_dataStreamMonitor.Enabled)
            {
                m_dataStreamMonitor.Stop();
                m_dataStreamMonitor.Start();
            }

            m_receivedConfigFrame = false;
            SendCommand(DeviceCommand.SendConfigurationFrame2);
        }

        private void m_dataStreamMonitor_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (m_bytesReceived == 0 && (m_frameParser.DeviceSupportsCommands || m_frameParser.ConnectionIsMulticast || m_frameParser.ConnectionIsListener))
            {
                // If we've received no data in the last time-span, we restart connect cycle...
                m_dataStreamMonitor.Enabled = false;
                OnStatusMessage("\r\nNo data received in {0} seconds, restarting connect cycle...\r\n", (m_dataStreamMonitor.Interval / 1000.0D).ToString("0.0"));
                Start();
            }
            else if (!m_receivedConfigFrame && m_allowUseOfCachedConfiguration)
            {
                // If data is being received but a configuration has yet to be loaded, attempt to load last known good configuration
                if (!m_cachedConfigLoadAttempted)
                {
                    OnStatusMessage("Configuration frame has yet to be received, attempting to load cached configuration...");
                    m_cachedConfigLoadAttempted = true;
                    LoadCachedConfiguration();
                }
                else if (m_frameParser.DeviceSupportsCommands)
                {
                    OnStatusMessage("\r\nConfiguration frame has yet to be received even after attempt to load from cache, restarting connect cycle...\r\n");
                    Start();
                }
            }

            m_bytesReceived = 0;
        }

        private void m_measurementCounter_Elapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            long now = DateTime.UtcNow.Ticks;
            IEnumerable<DeviceStatisticsHelper<ConfigurationCell>> statisticsHelpers = StatisticsHelpers;

            foreach (DeviceStatisticsHelper<ConfigurationCell> statisticsHelper in statisticsHelpers)
                statisticsHelper.Update(now);
        }

        private void RegisterStatistics(object source, string sourceName, string sourceCategory, string sourceAcronym)
        {
            if (string.IsNullOrWhiteSpace(SharedMapping))
                StatisticsEngine.Register(source, sourceName, sourceCategory, sourceAcronym);
        }

        #endregion
    }
}
