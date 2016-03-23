﻿//******************************************************************************************************
//  CommonPhasorServices.cs - Gbtc
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
//  4/9/2010 - J. Ritchie Carroll
//       Generated original version of source code.
//  3/11/2011 - Mehulbhai P Thakkar
//       Fixed issue in PhasorDataSourceValidation when CompanyID is NULL in Device table.
//  12/04/2012 - J. Ritchie Carroll
//       Migrated to PhasorProtocolAdapters project.
//  12/13/2012 - Starlynn Danyelle Gilliam
//       Modified Header.
//
//******************************************************************************************************

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using GSF;
using GSF.Collections;
using GSF.Configuration;
using GSF.Data;
using GSF.IO;
using GSF.Parsing;
using GSF.PhasorProtocols;
using GSF.TimeSeries;
using GSF.TimeSeries.Adapters;
using GSF.TimeSeries.Statistics;
using GSF.Units;
using GSF.Units.EE;

namespace PhasorProtocolAdapters
{
    // ReSharper disable UnusedMember.Local
    // ReSharper disable UnusedParameter.Local
    // ReSharper disable ObjectCreationAsStatement

    /// <summary>
    /// Provides common phasor services.
    /// </summary>
    /// <remarks>
    /// Typically class should be implemented as a singleton since one instance will suffice.
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class CommonPhasorServices : FacileActionAdapterBase
    {
        #region [ Members ]

        // Fields
        private ManualResetEvent m_configurationWaitHandle;
        private MultiProtocolFrameParser m_frameParser;
        private IConfigurationFrame m_configurationFrame;
        private bool m_cancelConfigurationFrameRequest;
        private bool m_disposed;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Creates a new <see cref="CommonPhasorServices"/>.
        /// </summary>
        public CommonPhasorServices()
        {
            // Create wait handle to use to wait for configuration frame
            m_configurationWaitHandle = new ManualResetEvent(false);

            // Create a new phasor protocol frame parser used to dynamically request device configuration frames
            // and return them to remote clients so that the frame can be used in system setup and configuration
            m_frameParser = new MultiProtocolFrameParser();

            // Attach to events on new frame parser reference
            m_frameParser.ConnectionAttempt += m_frameParser_ConnectionAttempt;
            m_frameParser.ConnectionEstablished += m_frameParser_ConnectionEstablished;
            m_frameParser.ConnectionException += m_frameParser_ConnectionException;
            m_frameParser.ConnectionTerminated += m_frameParser_ConnectionTerminated;
            m_frameParser.ExceededParsingExceptionThreshold += m_frameParser_ExceededParsingExceptionThreshold;
            m_frameParser.ParsingException += m_frameParser_ParsingException;
            m_frameParser.ReceivedConfigurationFrame += m_frameParser_ReceivedConfigurationFrame;

            // We only want to try to connect to device and retrieve configuration as quickly as possible
            m_frameParser.MaximumConnectionAttempts = 1;
            m_frameParser.SourceName = Name;
            m_frameParser.AutoRepeatCapturedPlayback = false;
            m_frameParser.AutoStartDataParsingSequence = false;
            m_frameParser.SkipDisableRealTimeData = true;
        }

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets the flag indicating if this adapter supports temporal processing.
        /// </summary>
        /// <remarks>
        /// Since the common phasor services is designed to assist in various real-time operations,
        /// it is expected that this would not be desired in a temporal data streaming session.
        /// </remarks>
        public override bool SupportsTemporalProcessing
        {
            get
            {
                return false;
            }
        }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="CommonPhasorServices"/> object and optionally releases the managed resources.
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
                        // Detach from frame parser events and dispose
                        if ((object)m_frameParser != null)
                        {
                            m_frameParser.ConnectionAttempt -= m_frameParser_ConnectionAttempt;
                            m_frameParser.ConnectionEstablished -= m_frameParser_ConnectionEstablished;
                            m_frameParser.ConnectionException -= m_frameParser_ConnectionException;
                            m_frameParser.ConnectionTerminated -= m_frameParser_ConnectionTerminated;
                            m_frameParser.ExceededParsingExceptionThreshold -= m_frameParser_ExceededParsingExceptionThreshold;
                            m_frameParser.ParsingException -= m_frameParser_ParsingException;
                            m_frameParser.ReceivedConfigurationFrame -= m_frameParser_ReceivedConfigurationFrame;
                            m_frameParser.Dispose();
                        }
                        m_frameParser = null;

                        // Dispose configuration of wait handle
                        if (m_configurationWaitHandle != null)
                            m_configurationWaitHandle.Close();

                        m_configurationWaitHandle = null;
                        m_configurationFrame = null;
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
        /// Gets a short one-line status of this <see cref="CommonPhasorServices"/>.
        /// </summary>
        /// <param name="maxLength">Maximum number of available characters for display.</param>
        /// <returns>A short one-line summary of the current status of the <see cref="CommonPhasorServices"/>.</returns>
        public override string GetShortStatus(int maxLength)
        {
            return "Type \"LISTCOMMANDS 0\" to enumerate service commands.".CenterText(maxLength);
        }

        /// <summary>
        /// Requests a configuration frame from a phasor device.
        /// </summary>
        /// <param name="connectionString">Connection string used to connect to phasor device.</param>
        /// <returns>A <see cref="IConfigurationFrame"/> if successful, -or- <c>null</c> if request failed.</returns>
        [AdapterCommand("Connects to a phasor device and requests its configuration frame.", "Administrator", "Editor")]
        public IConfigurationFrame RequestDeviceConfiguration(string connectionString)
        {
            m_cancelConfigurationFrameRequest = false;

            if (string.IsNullOrEmpty(connectionString))
            {
                OnStatusMessage("ERROR: No connection string was specified, request for configuration canceled.");
                return new ConfigurationErrorFrame();
            }

            // Define a line of asterisks for emphasis
            string stars = new string('*', 79);

            // Only allow configuration request if another request is not already pending...
            if (Monitor.TryEnter(m_frameParser))
            {
                try
                {
                    Dictionary<string, string> settings = connectionString.ParseKeyValuePairs();
                    string setting;
                    ushort accessID;

                    // Get accessID from connection string
                    if (settings.TryGetValue("accessID", out setting))
                        accessID = ushort.Parse(setting);
                    else
                        accessID = 1;

                    // Most of the parameters in the connection string will be for the data source in the frame parser
                    // so we provide all of them, other parameters will simply be ignored
                    m_frameParser.ConnectionString = connectionString;

                    // Provide access ID to frame parser as this may be necessary to make a phasor connection
                    m_frameParser.DeviceID = accessID;

                    // Clear any existing configuration frame
                    m_configurationFrame = null;

                    // Inform user of temporary loss of command access
                    OnStatusMessage("\r\n{0}\r\n\r\nAttempting to request remote device configuration.\r\n\r\nThis request could take up to sixty seconds to complete.\r\n\r\nOther CPS config requests will not be accepted until request succeeds or fails.\r\n\r\n{0}", stars);

                    // Make sure the wait handle is not set
                    m_configurationWaitHandle.Reset();

                    // Start the frame parser - this will attempt connection
                    m_frameParser.Start();

                    // We wait a maximum of 60 seconds to receive the configuration frame - this delay should be the maximum time ever needed
                    // to receive a configuration frame. If the device connection is Active or Hybrid then the configuration frame should be
                    // returned immediately - for purely Passive connections the configuration frame is delivered once per minute.
                    if (!m_configurationWaitHandle.WaitOne(60000))
                        OnStatusMessage("WARNING: Timed-out waiting to retrieve remote device configuration.");

                    // Terminate connection to device
                    m_frameParser.Stop();

                    if (m_configurationFrame == null)
                    {
                        m_configurationFrame = new ConfigurationErrorFrame();

                        if (m_cancelConfigurationFrameRequest)
                            OnStatusMessage("Configuration frame request canceled by user.");
                        else
                            OnStatusMessage("Failed to retrieve remote device configuration.");
                    }

                    return m_configurationFrame;
                }
                catch (Exception ex)
                {
                    OnStatusMessage("ERROR: Failed to request configuration due to exception: {0}", ex.Message);
                }
                finally
                {
                    m_cancelConfigurationFrameRequest = false;

                    // Release the lock
                    Monitor.Exit(m_frameParser);

                    // Inform user of restoration of command access
                    OnStatusMessage("\r\n{0}\r\n\r\nRemote device configuration request completed.\r\n\r\nCPS config requests have been restored.\r\n\r\n{0}", stars);
                }
            }
            else
            {
                OnStatusMessage("ERROR: Cannot process simultaneous requests for device configurations, please try again in a few seconds..");
            }

            return new ConfigurationErrorFrame();
        }

        /// <summary>
        /// Cancels a configuration frame request
        /// </summary>
        [AdapterCommand("Cancels the currently executing configuration frame request.", "Administrator", "Editor")]
        public void CancelConfigurationFrameRequest()
        {
            m_cancelConfigurationFrameRequest = true;
            m_configurationWaitHandle.Set();
        }

        /// <summary>
        /// Sends the specified <see cref="DeviceCommand"/> to the current device connection.
        /// </summary>
        /// <param name="command"><see cref="DeviceCommand"/> to send to connected device.</param>
        public void SendCommand(DeviceCommand command)
        {
            if ((object)m_frameParser != null)
            {
                m_frameParser.SendDeviceCommand(command);
                OnStatusMessage("Sent device command \"{0}\"...", command);
            }
            else
            {
                OnStatusMessage("Failed to send device command \"{0}\", no frame parser is defined.", command);
            }
        }

        private void m_frameParser_ReceivedConfigurationFrame(object sender, EventArgs<IConfigurationFrame> e)
        {
            // Cache received configuration frame
            m_configurationFrame = e.Argument;

            OnStatusMessage("Successfully received configuration frame!");

            // Clear wait handle
            m_configurationWaitHandle.Set();
        }

        private void m_frameParser_ConnectionTerminated(object sender, EventArgs e)
        {
            // Communications layer closed connection (close not initiated by system) - so we cancel request..
            if (m_frameParser.Enabled)
                OnStatusMessage("ERROR: Connection closed by remote device, request for configuration canceled.");

            // Clear wait handle
            m_configurationWaitHandle.Set();
        }

        private void m_frameParser_ConnectionEstablished(object sender, EventArgs e)
        {
            OnStatusMessage("Connected to remote device, requesting configuration frame...");

            // Send manual request for configuration frame
            SendCommand(DeviceCommand.SendConfigurationFrame2);
        }

        private void m_frameParser_ConnectionException(object sender, EventArgs<Exception, int> e)
        {
            OnStatusMessage("ERROR: Connection attempt failed, request for configuration canceled: {0}", e.Argument1.Message);

            // Clear wait handle
            m_configurationWaitHandle.Set();
        }

        private void m_frameParser_ParsingException(object sender, EventArgs<Exception> e)
        {
            OnStatusMessage("ERROR: Parsing exception during request for configuration: {0}", e.Argument.Message);
        }

        private void m_frameParser_ExceededParsingExceptionThreshold(object sender, EventArgs e)
        {
            OnStatusMessage("\r\nRequest for configuration canceled due to an excessive number of exceptions...\r\n");

            // Clear wait handle
            m_configurationWaitHandle.Set();
        }

        private void m_frameParser_ConnectionAttempt(object sender, EventArgs e)
        {
            OnStatusMessage("Attempting {0} {1} based connection...", m_frameParser.PhasorProtocol.GetFormattedProtocolName(), m_frameParser.TransportProtocol.ToString().ToUpper());
        }

        #endregion

        #region [ Static ]

        // Static Fields
        private static readonly StatisticValueStateCache s_statisticValueCache = new StatisticValueStateCache();

        // Classic
        private const string DefaultPointTagNameExpression = "{CompanyAcronym}_{DeviceAcronym}[?{SignalType.Source}=Phasor[-{SignalType.Suffix}{SignalIndex}]]:{VendorAcronym}{SignalType.Abbreviation}[?{SignalType.Source}!=Phasor[?{SignalIndex}!=-1[{SignalIndex}]]]";

        // Classic with condensed phase information
        //private const string DefaultPointTagNameExpression = "{CompanyAcronym}_{DeviceAcronym}[?{SignalType.Source}=Phasor[-{SignalType.Suffix}{SignalIndex}]]:[?{VendorAcronym}!=[{VendorAcronym}_]][?{Phase}=A[APH]][?{Phase}=B[BPH]][?{Phase}=C[CPH]][?{Phase}=+[PSQ]][?{Phase}=-[NSQ]][?{Phase}=0[ZSQ]]{SignalType.Abbreviation}[?{SignalType.Source}!=Phasor[?{SignalIndex}!=-1[{SignalIndex}]]]";

        // Verbose
        //private const string DefaultPointTagNameExpression = "{CompanyAcronym}_{DeviceAcronym}[?{SignalType.Source}=Phasor[-{SignalType.Suffix}{SignalIndex}]]:[?{VendorAcronym}!=[{VendorAcronym}_]][?{Phase}=A[PhaseA_]][?{Phase}=B[PhaseB_]][?{Phase}=C[PhaseC_]][?{Phase}=+[PosSeq_]][?{Phase}=-[NegSeq_]][?{Phase}=0[ZeroSeq_]][?{SignalType.Acronym}!=STAT[{SignalType.LongAcronym}]][?{SignalType.Acronym}=STAT[{SignalType.Suffix}]][?{SignalType.Source}!=Phasor[?{SignalIndex}!=-1[{SignalIndex}]]]";

        // Verbose without vendor information
        //private const string DefaultPointTagNameExpression = "{CompanyAcronym}_{DeviceAcronym}[?{SignalType.Source}=Phasor[-{SignalType.Suffix}{SignalIndex}]]:[?{Phase}=A[PhaseA_]][?{Phase}=B[PhaseB_]][?{Phase}=C[PhaseC_]][?{Phase}=+[PosSeq_]][?{Phase}=-[NegSeq_]][?{Phase}=0[ZeroSeq_]][?{SignalType.Acronym}!=STAT[{SignalType.LongAcronym}]][?{SignalType.Acronym}=STAT[{SignalType.Suffix}]][?{SignalType.Source}!=Phasor[?{SignalIndex}!=-1[{SignalIndex}]]]";

        // Cached point tag name expression fields
        private static TemplatedExpressionParser s_pointTagExpressionParser;
        private static Dictionary<string, DataRow> s_signalTypes;

        // Static Methods

        /// <summary>
        /// Create a new point tag using the configured point tag name expression.
        /// </summary>
        /// <param name="companyAcronym">Company name acronym to use for the point tag.</param>
        /// <param name="deviceAcronym">Device name acronym to use for the point tag.</param>
        /// <param name="vendorAcronym">Vendor name acronym to use for the point tag. Can be null.</param>
        /// <param name="signalTypeAcronym">Acronym of signal type of the point.</param>
        /// <param name="signalIndex">Signal index of the point, if any.</param>
        /// <param name="phase">Signal phase of the point, if any.</param>
        /// <returns>A new point tag created using the configured point tag name expression.</returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static string CreatePointTag(string companyAcronym, string deviceAcronym, string vendorAcronym, string signalTypeAcronym, int signalIndex = -1, char phase = '_')
        {
            // Initialize point tag expression parser
            if ((object)s_pointTagExpressionParser == null)
                s_pointTagExpressionParser = InitializePointTagExpressionParser();

            // Initialize signal type dictionary
            if ((object)s_signalTypes == null)
                s_signalTypes = InitializeSignalTypes();

            Dictionary<string, string> substitutions;
            DataRow signalTypeValues;

            if (!s_signalTypes.TryGetValue(signalTypeAcronym, out signalTypeValues))
                throw new ArgumentOutOfRangeException("signalTypeAcronym", "No database definition was found for signal type \"" + signalTypeAcronym + "\"");

            // Validate key acronyms
            if ((object)companyAcronym == null)
                companyAcronym = "";

            if ((object)deviceAcronym == null)
                deviceAcronym = "";

            if ((object)vendorAcronym == null)
                vendorAcronym = "";

            companyAcronym = companyAcronym.Trim();
            deviceAcronym = deviceAcronym.Trim();
            vendorAcronym = vendorAcronym.Trim();

            // Define fixed parameter replacements
            substitutions = new Dictionary<string, string>
            {
                { "{CompanyAcronym}", companyAcronym },
                { "{DeviceAcronym}", deviceAcronym },
                { "{VendorAcronym}", vendorAcronym },
                { "{SignalIndex}", signalIndex.ToString() },
                { "{Phase}", phase.ToString() }
            };

            // Define signal type field value replacements
            DataColumnCollection columns = signalTypeValues.Table.Columns;

            for (int i = 0; i < columns.Count; i++)
            {
                substitutions.Add(string.Format("{{SignalType.{0}}}", columns[i].ColumnName), signalTypeValues[i].ToNonNullString());
            }

            return s_pointTagExpressionParser.Execute(substitutions);
        }

        private static TemplatedExpressionParser InitializePointTagExpressionParser()
        {
            TemplatedExpressionParser pointTagExpressionParser;

            // Get point tag name expression from configuration
            try
            {
                // Note that both manager and service application may need this expression and each will have their own setting, users
                // will need to synchronize these expressions in both config files for consistent custom point tag naming
                ConfigurationFile configFile = ConfigurationFile.Current;
                CategorizedSettingsElementCollection settings = configFile.Settings["systemSettings"];

                settings.Add("PointTagNameExpression", DefaultPointTagNameExpression, "Defines the expression used to create point tag names. NOTE: if updating this setting, synchronize value in both the manager and service config files.");

                pointTagExpressionParser = new TemplatedExpressionParser()
                {
                    TemplatedExpression = configFile.Settings["systemSettings"]["PointTagNameExpression"].Value
                };
            }
            catch
            {
                pointTagExpressionParser = new TemplatedExpressionParser()
                {
                    TemplatedExpression = DefaultPointTagNameExpression
                };
            }

            return pointTagExpressionParser;
        }

        private static Dictionary<string, DataRow> InitializeSignalTypes()
        {
            Dictionary<string, DataRow> signalTypes;

            // It is expected that when a point tag is needing to be created that the database will be available
            using (AdoDataConnection database = new AdoDataConnection("systemSettings"))
            {
                signalTypes = new Dictionary<string, DataRow>(StringComparer.OrdinalIgnoreCase);

                foreach (DataRow row in database.Connection.RetrieveData(database.AdapterType, "SELECT * FROM SignalType").AsEnumerable())
                {
                    signalTypes.AddOrUpdate(row["Acronym"].ToString(), row);
                }
            }

            return signalTypes;
        }

        /// <summary>
        /// Apply start-up phasor data source validations
        /// </summary>
        /// <param name="database">The database connection.</param>
        /// <param name="nodeIDQueryString">Current node ID in proper query format.</param>
        /// <param name="trackingVersion">Latest version of the configuration to which data operations were previously applied.</param>
        /// <param name="arguments">Arguments, if any, to be used but the data source validation.</param>
        /// <param name="statusMessage">The delegate which will display a status message to the user.</param>
        /// <param name="processException">The delegate which will handle exception logging.</param>
        [SuppressMessage("Microsoft.Maintainability", "CA1502"), SuppressMessage("Microsoft.Maintainability", "CA1505")]
        private static void PhasorDataSourceValidation(AdoDataConnection database, string nodeIDQueryString, ulong trackingVersion, string arguments, Action<string> statusMessage, Action<Exception> processException)
        {
            // Make sure setting exists to allow user to by-pass phasor data source validation at startup
            ConfigurationFile configFile = ConfigurationFile.Current;
            CategorizedSettingsElementCollection settings = configFile.Settings["systemSettings"];
            settings.Add("ProcessPhasorDataSourceValidation", true, "Determines if the phasor data source validation should be processed at startup");

            // See if this node should process phasor source validation
            if (!settings["ProcessPhasorDataSourceValidation"].ValueAsBoolean())
                return;

            Dictionary<string, string> args = new Dictionary<string, string>();
            bool skipOptimization = false, renameAllPointTags = false;
            string arg, acronym;

            if (!string.IsNullOrEmpty(arguments))
                args = arguments.ParseKeyValuePairs();

            if (args.TryGetValue("skipOptimization", out arg))
                skipOptimization = arg.ParseBoolean();

            if (args.TryGetValue("renameAllPointTags", out arg))
                renameAllPointTags = arg.ParseBoolean();

            CreateDefaultNode(database, nodeIDQueryString, statusMessage, processException);
            LoadDefaultConfigurationEntity(database, statusMessage, processException);
            LoadDefaultInterconnection(database, statusMessage, processException);
            LoadDefaultProtocol(database, statusMessage, processException);
            LoadDefaultSignalType(database, statusMessage, processException);
            LoadDefaultStatistic(database, statusMessage, processException);
            EstablishDefaultMeasurementKeyCache(database, statusMessage, processException);

            statusMessage("Validating signal types...");

            // Validate that the acronym for status flags is FLAG (it was STAT in prior versions)
            if (database.Connection.ExecuteScalar("SELECT Acronym FROM SignalType WHERE Suffix='SF'").ToNonNullString().ToUpper() == "STAT")
                database.Connection.ExecuteNonQuery("UPDATE SignalType SET Acronym='FLAG' WHERE Suffix='SF'");

            // Validate that the calculation and statistic signal types are defined (they did not in initial release)
            if (Convert.ToInt32(database.Connection.ExecuteScalar("SELECT COUNT(*) FROM SignalType WHERE Acronym='CALC'")) == 0)
                database.Connection.ExecuteNonQuery("INSERT INTO SignalType(Name, Acronym, Suffix, Abbreviation, LongAcronym, Source, EngineeringUnits) VALUES('Calculated Value', 'CALC', 'CV', 'CV', 'Calculated', 'PMU', '')");

            if (Convert.ToInt32(database.Connection.ExecuteScalar("SELECT COUNT(*) FROM SignalType WHERE Acronym='STAT'")) == 0)
                database.Connection.ExecuteNonQuery("INSERT INTO SignalType(Name, Acronym, Suffix, Abbreviation, LongAcronym, Source, EngineeringUnits) VALUES('Statistic', 'STAT', 'ST', 'ST', 'Statistic', 'Any', '')");

            if (Convert.ToInt32(database.Connection.ExecuteScalar("SELECT COUNT(*) FROM SignalType WHERE Acronym='QUAL'")) == 0)
                database.Connection.ExecuteNonQuery("INSERT INTO SignalType(Name, Acronym, Suffix, Abbreviation, LongAcronym, Source, EngineeringUnits) VALUES('Quality Flags', 'QUAL', 'QF', 'QF', 'QualityFlags', 'Frame', '')");

            // Make sure values are defined for long acronyms (did not exist in prior versions)
            if (Convert.ToInt32(database.Connection.ExecuteScalar("SELECT COUNT(*) FROM SignalType WHERE LongAcronym='Undefined'")) > 0)
            {
                // Update abbreviations to better values for consistent custom point tag naming convention
                if (database.Connection.ExecuteScalar("SELECT Abbreviation FROM SignalType WHERE Acronym='ALOG'").ToNonNullString().ToUpper() == "A")
                    database.Connection.ExecuteNonQuery("UPDATE SignalType SET Abbreviation='AV' WHERE Acronym='ALOG'");

                if (database.Connection.ExecuteScalar("SELECT Abbreviation FROM SignalType WHERE Acronym='DIGI'").ToNonNullString().ToUpper() == "D")
                    database.Connection.ExecuteNonQuery("UPDATE SignalType SET Abbreviation='DV' WHERE Acronym='DIGI'");

                if (database.Connection.ExecuteScalar("SELECT Abbreviation FROM SignalType WHERE Acronym='CALC'").ToNonNullString().ToUpper() == "C")
                    database.Connection.ExecuteNonQuery("UPDATE SignalType SET Abbreviation='CV' WHERE Acronym='CALC'");

                if (database.Connection.ExecuteScalar("SELECT Abbreviation FROM SignalType WHERE Acronym='STAT'").ToNonNullString().ToUpper() == "P")
                    database.Connection.ExecuteNonQuery("UPDATE SignalType SET Abbreviation='ST' WHERE Acronym='STAT'");

                if (database.Connection.ExecuteScalar("SELECT Abbreviation FROM SignalType WHERE Acronym='QUAL'").ToNonNullString().ToUpper() == "Q")
                    database.Connection.ExecuteNonQuery("UPDATE SignalType SET Abbreviation='QF' WHERE Acronym='QUAL'");

                IEnumerable<DataRow> signalTypes = database.Connection.RetrieveData(database.AdapterType, "SELECT Name, Acronym FROM SignalType WHERE LongAcronym='Undefined'").AsEnumerable();
                string longAcronym;

                foreach (DataRow row in signalTypes)
                {
                    acronym = row.Field<string>("Acronym").ToUpperInvariant().Trim();

                    switch (acronym)
                    {
                        case "IPHM":
                            longAcronym = "CurrentMagnitude";
                            break;
                        case "IPHA":
                            longAcronym = "CurrentAngle";
                            break;
                        case "VPHM":
                            longAcronym = "VoltageMagnitude";
                            break;
                        case "VPHA":
                            longAcronym = "VoltageAngle";
                            break;
                        case "FREQ":
                            longAcronym = "Frequency";
                            break;
                        case "DFDT":
                            longAcronym = "DfDt";
                            break;
                        case "ALOG":
                            longAcronym = "Analog";
                            break;
                        case "FLAG":
                            longAcronym = "StatusFlags";
                            break;
                        case "DIGI":
                            longAcronym = "Digital";
                            break;
                        case "CALC":
                            longAcronym = "Calculated";
                            break;
                        case "STAT":
                            longAcronym = "Statistic";
                            break;
                        case "ALRM":
                            longAcronym = "Alarm";
                            break;
                        case "QUAL":
                            longAcronym = "QualityFlags";
                            break;
                        default:
                            longAcronym = row.Field<string>("Name").Trim().RemoveWhiteSpace();

                            if (string.IsNullOrEmpty(longAcronym))
                                longAcronym = acronym.ToNonNullString("?");

                            break;
                    }

                    database.Connection.ExecuteNonQuery(string.Format("UPDATE SignalType SET LongAcronym='{0}' WHERE Acronym='{1}'", longAcronym, acronym));
                }
            }

            statusMessage("Validating output stream device ID codes...");

            // Validate all ID codes for output stream devices are not set their default value
            database.Connection.ExecuteNonQuery("UPDATE OutputStreamDevice SET IDCode = ID WHERE IDCode = 0");

            statusMessage("Verifying statistics archive exists...");

            // Validate that the statistics historian exists
            if (Convert.ToInt32(database.Connection.ExecuteScalar(string.Format("SELECT COUNT(*) FROM Historian WHERE Acronym='STAT' AND NodeID={0}", nodeIDQueryString))) == 0)
                database.Connection.ExecuteNonQuery(string.Format("INSERT INTO Historian(NodeID, Acronym, Name, AssemblyName, TypeName, ConnectionString, IsLocal, Description, LoadOrder, Enabled) VALUES({0}, 'STAT', 'Statistics Archive', 'HistorianAdapters.dll', 'HistorianAdapters.LocalOutputAdapter', '', 1, 'Local historian used to archive system statistics', 9999, 1)", nodeIDQueryString));

            // Make sure statistics path exists to hold historian files
            string statisticsPath = FilePath.GetAbsolutePath(FilePath.AddPathSuffix("Statistics"));

            if (!Directory.Exists(statisticsPath))
                Directory.CreateDirectory(statisticsPath);

            // Make sure needed statistic historian configuration settings are properly defined
            settings = configFile.Settings["statMetadataFile"];
            settings.Add("FileName", string.Format("Statistics{0}stat_dbase.dat", Path.DirectorySeparatorChar), "Name of the statistics meta-data file including its path.");
            settings.Add("LoadOnOpen", true, "True if file records are to be loaded in memory when opened; otherwise False - this defaults to True for the statistics meta-data file.");
            settings.Add("ReloadOnModify", false, "True if file records loaded in memory are to be re-loaded when file is modified on disk; otherwise False - this defaults to False for the statistics meta-data file.");
            settings["LoadOnOpen"].Update(true);
            settings["ReloadOnModify"].Update(false);

            settings = configFile.Settings["statStateFile"];
            settings.Add("FileName", string.Format("Statistics{0}stat_startup.dat", Path.DirectorySeparatorChar), "Name of the statistics state file including its path.");
            settings.Add("AutoSaveInterval", 10000, "Interval in milliseconds at which the file records loaded in memory are to be saved automatically to disk. Use -1 to disable automatic saving - this defaults to 10,000 for the statistics state file.");
            settings.Add("LoadOnOpen", true, "True if file records are to be loaded in memory when opened; otherwise False - this defaults to True for the statistics state file.");
            settings.Add("SaveOnClose", true, "True if file records loaded in memory are to be saved to disk when file is closed; otherwise False - this defaults to True for the statistics state file.");
            settings.Add("ReloadOnModify", false, "True if file records loaded in memory are to be re-loaded when file is modified on disk; otherwise False - this defaults to False for the statistics state file.");
            settings["AutoSaveInterval"].Update(10000);
            settings["LoadOnOpen"].Update(true);
            settings["SaveOnClose"].Update(true);
            settings["ReloadOnModify"].Update(false);

            settings = configFile.Settings["statIntercomFile"];
            settings.Add("FileName", string.Format("Statistics{0}scratch.dat", Path.DirectorySeparatorChar), "Name of the statistics intercom file including its path.");
            settings.Add("AutoSaveInterval", 10000, "Interval in milliseconds at which the file records loaded in memory are to be saved automatically to disk. Use -1 to disable automatic saving - this defaults to 10,000 for the statistics intercom file.");
            settings.Add("LoadOnOpen", true, "True if file records are to be loaded in memory when opened; otherwise False - this defaults to True for the statistics intercom file.");
            settings.Add("SaveOnClose", true, "True if file records loaded in memory are to be saved to disk when file is closed; otherwise False - this defaults to True for the statistics intercom file.");
            settings.Add("ReloadOnModify", false, "True if file records loaded in memory are to be re-loaded when file is modified on disk; otherwise False - this defaults to False for the statistics intercom file.");
            settings["AutoSaveInterval"].Update(1000);
            settings["LoadOnOpen"].Update(true);
            settings["SaveOnClose"].Update(true);
            settings["ReloadOnModify"].Update(false);

            settings = configFile.Settings["statArchiveFile"];
            settings.Add("FileName", string.Format("Statistics{0}stat_archive.d", Path.DirectorySeparatorChar), "Name of the statistics working archive file including its path.");
            settings.Add("CacheWrites", true, "True if writes are to be cached for performance; otherwise False - this defaults to True for the statistics working archive file.");
            settings.Add("ConserveMemory", false, "True if attempts are to be made to conserve memory; otherwise False - this defaults to False for the statistics working archive file.");
            settings["CacheWrites"].Update(true);
            settings["ConserveMemory"].Update(false);

            settings = configFile.Settings["statMetadataService"];
            settings.Add("Endpoints", "http.rest://localhost:6051/historian", "Semicolon delimited list of URIs where the web service can be accessed - this defaults to http.rest://localhost:6051/historian for the statistics meta-data service.");

            settings = configFile.Settings["statTimeSeriesDataService"];
            settings.Add("Endpoints", "http.rest://localhost:6052/historian", "Semicolon delimited list of URIs where the web service can be accessed - this defaults to http.rest://localhost:6052/historian for the statistics time-series data service.");

            configFile.Save();

            // Get the needed statistic related IDs
            int statHistorianID = Convert.ToInt32(database.Connection.ExecuteScalar(string.Format("SELECT ID FROM Historian WHERE Acronym='STAT' AND NodeID={0}", nodeIDQueryString)));

            // Load the defined system statistics
            IEnumerable<DataRow> statistics = database.Connection.RetrieveData(database.AdapterType, "SELECT * FROM Statistic ORDER BY Source, SignalIndex").AsEnumerable();

            // Filter statistics to device, input stream and output stream types            
            IEnumerable<DataRow> deviceStatistics = statistics.Where(row => string.Compare(row.Field<string>("Source"), "Device", true) == 0).ToList();
            IEnumerable<DataRow> inputStreamStatistics = statistics.Where(row => string.Compare(row.Field<string>("Source"), "InputStream", true) == 0).ToList();

            // Define kinds of output signal that will designate a location in an output stream protocol frame - other non-mappable measurements will be removed from output stream measurements
            SignalKind[] validOutputSignalKinds = { SignalKind.Angle, SignalKind.Magnitude, SignalKind.Frequency, SignalKind.DfDt, SignalKind.Status, SignalKind.Analog, SignalKind.Digital, SignalKind.Quality };

            HashSet<int> measurementIDsToDelete = new HashSet<int>();
            SignalReference deviceSignalReference;
            string query, signalReference, pointTag, company, description, protocolIDs;
            int adapterID, deviceID, signalIndex;
            bool firstStatisticExisted;
            int? historianID;

            string[] trackedTables;
            ulong changes;

            try
            {
                // Determine the tables for which changes are tracked
                if (trackingVersion != ulong.MinValue)
                {
                    trackedTables = database.Connection.RetrieveData(database.AdapterType, "SELECT Name FROM TrackedTable").Select()
                        .Select(row => row["Name"].ToNonNullString())
                        .ToArray();
                }
                else
                {
                    trackedTables = new string[0];
                }
            }
            catch
            {
                trackedTables = new string[0];
            }

            statusMessage("Validating device protocols...");

            // Extract IDs for phasor protocols
            StringBuilder protocolIDList = new StringBuilder();
            DataTable protocols = database.Connection.RetrieveData(database.AdapterType, "SELECT * FROM Protocol");

            if (protocols.Columns.Contains("Category"))
            {
                // Make sure new protocol types exist
                if (Convert.ToInt32(database.Connection.ExecuteScalar(string.Format("SELECT COUNT(*) FROM Protocol WHERE Acronym='GatewayTransport'"))) == 0)
                {
                    database.Connection.ExecuteNonQuery("INSERT INTO Protocol(Acronym, Name, Type, Category, AssemblyName, TypeName, LoadOrder) VALUES('GatewayTransport', 'Gateway Transport', 'Measurement', 'Gateway', 'GSF.TimeSeries.dll', 'GSF.TimeSeries.Transport.DataSubscriber', " + (protocols.Rows.Count + 1) + ")");

                    if (Convert.ToInt32(database.Connection.ExecuteScalar(string.Format("SELECT COUNT(*) FROM Protocol WHERE Acronym='WAV'"))) == 0)
                        database.Connection.ExecuteNonQuery("INSERT INTO Protocol(Acronym, Name, Type, Category, AssemblyName, TypeName, LoadOrder) VALUES('WAV', 'Wave Form Input Adapter', 'Frame', 'Audio', 'WavInputAdapter.dll', 'WavInputAdapter.WavInputAdapter', " + (protocols.Rows.Count + 2) + ")");

                    if (Convert.ToInt32(database.Connection.ExecuteScalar(string.Format("SELECT COUNT(*) FROM Protocol WHERE Acronym='IeeeC37_118V2'"))) == 0)
                        database.Connection.ExecuteNonQuery("INSERT INTO Protocol(Acronym, Name, Type, Category, AssemblyName, TypeName, LoadOrder) VALUES('IeeeC37_118V2', 'IEEE C37.118.2-2011', 'Frame', 'Phasor', 'PhasorProtocolAdapters.dll', 'PhasorProtocolAdapters.PhasorMeasurementMapper', 2)");

                    if (Convert.ToInt32(database.Connection.ExecuteScalar(string.Format("SELECT COUNT(*) FROM Protocol WHERE Acronym='VirtualInput'"))) == 0)
                        database.Connection.ExecuteNonQuery("INSERT INTO Protocol(Acronym, Name, Type, Category, AssemblyName, TypeName, LoadOrder) VALUES('VirtualInput', 'Virtual Device', 'Frame', 'Virtual', 'TestingAdapters.dll', 'TestingAdapters.VirtualInputAdapter', " + (protocols.Rows.Count + 4) + ")");
                }

                if (Convert.ToInt32(database.Connection.ExecuteScalar(string.Format("SELECT COUNT(*) FROM Protocol WHERE Acronym='Iec61850_90_5'"))) == 0)
                    database.Connection.ExecuteNonQuery("INSERT INTO Protocol(Acronym, Name, Type, Category, AssemblyName, TypeName, LoadOrder) VALUES('Iec61850_90_5', 'IEC 61850-90-5', 'Frame', 'Phasor', 'PhasorProtocolAdapters.dll', 'PhasorProtocolAdapters.PhasorMeasurementMapper', 12)");

                foreach (DataRow protocol in protocols.Rows)
                {
                    if (string.Compare(protocol.Field<string>("Category"), "Phasor", true) == 0)
                    {
                        if (protocolIDList.Length > 0)
                            protocolIDList.Append(", ");

                        protocolIDList.Append(protocol.ConvertField<int>("ID"));
                    }
                }
            }
            else
            {
                // Older schemas do not include protocol categories and assembly info
                foreach (DataRow protocol in protocols.Rows)
                {
                    if (protocolIDList.Length > 0)
                        protocolIDList.Append(", ");

                    protocolIDList.Append(protocol.ConvertField<int>("ID"));
                }
            }

            protocolIDs = protocolIDList.ToString();

            try
            {
                // Determine how many changes were made to devices and measurements -
                // if no changes were made, we can skip the next few steps
                if (trackedTables.Contains("Device") && trackedTables.Contains("Measurement"))
                    changes = Convert.ToUInt64(database.Connection.ExecuteScalar(string.Format("SELECT COUNT(*) FROM TrackedChange WHERE (TableName = 'Device' OR TableName = 'Measurement') AND ID > {0}", trackingVersion)));
                else
                    changes = ulong.MaxValue;
            }
            catch
            {
                changes = ulong.MaxValue;
            }

            if (skipOptimization || changes != 0L)
            {
                statusMessage("Validating device measurements...");

                // Get protocol ID list for those protocols that support time quality flags
                DataTable timeQualityProtocols = database.Connection.RetrieveData(database.AdapterType, "SELECT ID FROM Protocol WHERE Acronym = 'IeeeC37_118V1' OR Acronym = 'IeeeC37_118V2' OR Acronym = 'IeeeC37_118D6' OR Acronym = 'Iec61850_90_5'");
                StringBuilder timeQualityProtocolIDList = new StringBuilder();
                string timeQualityProtocolIDs;

                foreach (DataRow timeQualityProtocol in timeQualityProtocols.Rows)
                {
                    if (timeQualityProtocolIDList.Length > 0)
                        timeQualityProtocolIDList.Append(", ");

                    timeQualityProtocolIDList.Append(timeQualityProtocol.ConvertField<int>("ID"));
                }

                timeQualityProtocolIDs = timeQualityProtocolIDList.ToString();

                int qualityFlagsSignalTypeID = Convert.ToInt32(database.Connection.ExecuteScalar("SELECT ID FROM SignalType WHERE Acronym='QUAL'"));

                // Make sure one device quality flags measurement exists for each "connection" for devices that support time quality flags
                foreach (DataRow device in database.Connection.RetrieveData(database.AdapterType, string.Format("SELECT * FROM Device WHERE ((IsConcentrator = 0 AND ParentID IS NULL) OR IsConcentrator = 1) AND NodeID = {0} AND ProtocolID IN ({1})", nodeIDQueryString, timeQualityProtocolIDs)).Rows)
                {
                    deviceID = device.ConvertField<int>("ID");
                    acronym = device.Field<string>("Acronym");
                    signalReference = SignalReference.ToString(acronym, SignalKind.Quality);

                    // See if quality flags measurement exists for device
                    if (Convert.ToInt32(database.Connection.ExecuteScalar(string.Format("SELECT COUNT(*) FROM Measurement WHERE SignalReference = '{0}' AND DeviceID = {1}", signalReference, deviceID))) == 0)
                    {
                        historianID = device.ConvertNullableField<int>("HistorianID");

                        company = (string)database.Connection.ExecuteScalar(string.Format("SELECT MapAcronym FROM Company WHERE ID = {0}", device.ConvertNullableField<int>("CompanyID") ?? 0));

                        if (string.IsNullOrEmpty(company))
                            company = configFile.Settings["systemSettings"]["CompanyAcronym"].Value.TruncateRight(3);

                        pointTag = CreatePointTag(company, acronym, null, "QUAL");
                        description = string.Format("{0} Time Quality Flags", device.Field<string>("Name"));

                        query = database.ParameterizedQueryString("INSERT INTO Measurement(HistorianID, DeviceID, PointTag, SignalTypeID, PhasorSourceIndex, " +
                                                                  "SignalReference, Description, Enabled) VALUES({0}, {1}, {2}, {3}, NULL, {4}, {5}, 1)", "historianID", "deviceID", "pointTag",
                            "signalTypeID", "signalReference", "description");

                        database.Connection.ExecuteNonQuery(query, DataExtensions.DefaultTimeoutDuration, historianID.HasValue ? (object)historianID.Value : (object)DBNull.Value, deviceID, pointTag, qualityFlagsSignalTypeID, signalReference, description);
                    }
                }

                // Make sure needed device statistic measurements exist, currently statistics are only associated with phasor devices so we filter based on protocol
                foreach (DataRow device in database.Connection.RetrieveData(database.AdapterType, string.Format("SELECT * FROM Device WHERE IsConcentrator = 0 AND NodeID = {0} AND ProtocolID IN ({1})", nodeIDQueryString, protocolIDs)).Rows)
                {
                    foreach (DataRow statistic in deviceStatistics)
                    {
                        string oldAcronym;
                        string oldSignalReference;

                        signalIndex = statistic.ConvertField<int>("SignalIndex");
                        oldAcronym = device.Field<string>("Acronym");
                        acronym = oldAcronym + "!PMU";
                        oldSignalReference = SignalReference.ToString(oldAcronym, SignalKind.Statistic, signalIndex);
                        signalReference = SignalReference.ToString(acronym, SignalKind.Statistic, signalIndex);

                        // If the original format for device statistics is found in the database, update to new format
                        if (Convert.ToInt32(database.Connection.ExecuteScalar(string.Format("SELECT COUNT(*) FROM Measurement WHERE SignalReference='{0}' AND HistorianID={1}", oldSignalReference, statHistorianID))) > 0)
                            database.Connection.ExecuteNonQuery(string.Format("UPDATE Measurement SET SignalReference='{0}' WHERE SignalReference='{1}' AND HistorianID={2}", signalReference, oldSignalReference, statHistorianID));
                        else if (!skipOptimization)
                            break;
                    }
                }

                statusMessage("Validating input stream measurements...");

                // Make sure devices associated with a concentrator do not have any extraneous input stream statistic measurements - this can happen
                // when a device was once a direct connect device but now is part of a concentrator...
                foreach (DataRow inputStream in database.Connection.RetrieveData(database.AdapterType, string.Format("SELECT * FROM Device WHERE (IsConcentrator = 0 AND ParentID IS NOT NULL) AND NodeID = {0} AND ProtocolID IN ({1})", nodeIDQueryString, protocolIDs)).Rows)
                {
                    firstStatisticExisted = false;

                    foreach (DataRow statistic in inputStreamStatistics)
                    {
                        acronym = inputStream.Field<string>("Acronym") + "!IS";
                        signalIndex = statistic.ConvertField<int>("SignalIndex");
                        signalReference = SignalReference.ToString(acronym, SignalKind.Statistic, signalIndex);

                        // To reduce time required to execute these steps, only first statistic is verified to exist
                        if (!skipOptimization && !firstStatisticExisted)
                        {
                            firstStatisticExisted = (Convert.ToInt32(database.Connection.ExecuteScalar(string.Format("SELECT COUNT(*) FROM Measurement WHERE SignalReference='{0}'", signalReference))) > 0);

                            // If the first extraneous input statistic doesn't exist, we assume no others do as well
                            if (!firstStatisticExisted)
                                break;
                        }

                        // Remove extraneous input statistics
                        database.Connection.ExecuteNonQuery(string.Format("DELETE FROM Measurement WHERE SignalReference = '{0}'", signalReference));
                    }
                }
            }

            try
            {
                // Determine how many changes were made to output streams, devices, and measurements -
                // if no changes were made, we can skip the next few steps
                if (trackedTables.Contains("OutputStream") && trackedTables.Contains("OutputStreamDevice") && trackedTables.Contains("OutputStreamMeasurement") && trackedTables.Contains("Measurement"))
                    changes = Convert.ToUInt64(database.Connection.ExecuteScalar(string.Format("SELECT COUNT(*) FROM TrackedChange WHERE (TableName = 'OutputStream' OR TableName = 'OutputStreamDevice' OR TableName = 'OutputStreamMeasurement' OR TableName = 'Measurement') AND ID > {0}", trackingVersion)));
                else
                    changes = ulong.MaxValue;
            }
            catch
            {
                changes = ulong.MaxValue;
            }

            if (skipOptimization || changes != 0L)
            {
                statusMessage("Validating output stream measurements...");

                // Make sure needed output stream statistic measurements exist
                foreach (DataRow outputStream in database.Connection.RetrieveData(database.AdapterType, string.Format("SELECT * FROM OutputStream WHERE NodeID = {0}", nodeIDQueryString)).Rows)
                {
                    adapterID = outputStream.ConvertField<int>("ID");

                    // Load devices acronyms associated with this output stream
                    List<string> deviceAcronyms =
                        database.Connection.RetrieveData(database.AdapterType,
                            string.Format("SELECT Acronym FROM OutputStreamDevice WHERE AdapterID = {0} AND NodeID = {1}", adapterID, nodeIDQueryString))
                            .AsEnumerable()
                            .Select(row => row.Field<string>("Acronym"))
                            .ToList();

                    // Since measurements can be added to the output stream device itself (e.g., quality flags) - we add it as a valid mapping destination as well
                    deviceAcronyms.Add(outputStream.Field<string>("Acronym"));

                    // Sort list so binary search can be used to speed lookups
                    deviceAcronyms.Sort(StringComparer.OrdinalIgnoreCase);

                    // Validate measurements associated with this output stream
                    foreach (DataRow outputStreamMeasurement in database.Connection.RetrieveData(database.AdapterType, string.Format("SELECT * FROM OutputStreamMeasurement WHERE AdapterID = {0} AND NodeID = {1}", adapterID, nodeIDQueryString)).Rows)
                    {
                        // Parse output stream measurement signal reference
                        deviceSignalReference = new SignalReference(outputStreamMeasurement.Field<string>("SignalReference"));

                        // Validate that the signal reference is associated with one of the output stream's devices
                        if (deviceAcronyms.BinarySearch(deviceSignalReference.Acronym, StringComparer.OrdinalIgnoreCase) < 0)
                        {
                            // This measurement has a signal reference for a device that is not part of the associated output stream, so we mark it for deletion
                            measurementIDsToDelete.Add(outputStreamMeasurement.ConvertField<int>("ID"));
                        }

                        // Validate that the signal reference type is valid for an output stream
                        if (!validOutputSignalKinds.Any(validSignalKind => deviceSignalReference.Kind == validSignalKind))
                        {
                            // This measurement has a signal reference type that is not valid for an output stream, so we mark it for deletion
                            measurementIDsToDelete.Add(outputStreamMeasurement.ConvertField<int>("ID"));
                        }
                    }
                }
            }

            if (measurementIDsToDelete.Count > 0)
            {
                statusMessage(string.Format("Removing {0} unused output stream device measurements...", measurementIDsToDelete.Count));

                foreach (int measurementID in measurementIDsToDelete)
                {
                    database.Connection.ExecuteNonQuery(string.Format("DELETE FROM OutputStreamMeasurement WHERE ID = {0} AND NodeID = {1}", measurementID, nodeIDQueryString));
                }
            }

            if (renameAllPointTags)
            {
                statusMessage("Renaming all point tags...");

                string device, vendor, signalAcronym;
                char? phase;
                int? vendorDeviceID;
                SignalReference signal;

                foreach (DataRow measurement in database.Connection.RetrieveData(database.AdapterType, "SELECT SignalID, CompanyAcronym, DeviceAcronym, VendorDeviceID, SignalReference, SignalAcronym, Phase FROM MeasurementDetail WHERE SignalAcronym <> 'STAT' AND Internal <> 0 AND Subscribed = 0").Rows)
                {
                    company = measurement.ConvertField<string>("CompanyAcronym");

                    if (string.IsNullOrEmpty(company))
                        company = configFile.Settings["systemSettings"]["CompanyAcronym"].Value.TruncateRight(3);

                    device = measurement.ConvertField<string>("DeviceAcronym");

                    if ((object)device != null)
                    {
                        vendorDeviceID = measurement.ConvertNullableField<int>("VendorDeviceID");

                        if (vendorDeviceID.HasValue)
                            vendor = (string)database.Connection.ExecuteScalar("SELECT Acronym FROM Vendor WHERE ID = " + vendorDeviceID.Value);
                        else
                            vendor = null;

                        signalAcronym = measurement.ConvertField<string>("SignalAcronym");

                        try
                        {
                            signal = new SignalReference(measurement.ConvertField<string>("SignalReference"));
                            signalIndex = signal.Index;

                            if (signalIndex <= 0)
                                signalIndex = -1;
                        }
                        catch
                        {
                            signalIndex = -1;
                        }

                        phase = measurement.ConvertNullableField<char>("Phase");

                        database.Connection.ExecuteNonQuery(string.Format("UPDATE Measurement SET PointTag = '{0}' WHERE SignalID = '{1}'", CreatePointTag(company, device, vendor, signalAcronym, signalIndex, phase ?? '_'), database.Guid(measurement, "SignalID")));
                    }
                }
            }

            if (skipOptimization || renameAllPointTags)
            {
                // If skipOptimization is set to true, automatically set it back to false
                const string clearParametersQuery =
                    "UPDATE DataOperation SET Arguments = '' " +
                    "WHERE AssemblyName = 'PhasorProtocolAdapters.dll' " +
                    "AND TypeName = 'PhasorProtocolAdapters.CommonPhasorServices' " +
                    "AND MethodName = 'PhasorDataSourceValidation'";

                database.Connection.ExecuteNonQuery(clearParametersQuery);
            }
        }

        /// <summary>
        /// Creates the default node for the Node table.
        /// </summary>
        /// <param name="database">The database connection.</param>
        /// <param name="nodeIDQueryString">The ID of the node in the proper database format.</param>
        /// <param name="statusMessage">The delegate which will display a status message to the user.</param>
        /// <param name="processException">The delegate which will handle exception logging.</param>
        private static void CreateDefaultNode(AdoDataConnection database, string nodeIDQueryString, Action<string> statusMessage, Action<Exception> processException)
        {
            if (Convert.ToInt32(database.Connection.ExecuteScalar("SELECT COUNT(*) FROM Node")) == 0)
            {
                statusMessage("Creating default record for Node...");
                database.Connection.ExecuteNonQuery("INSERT INTO Node(Name, CompanyID, Description, Settings, MenuType, MenuData, Master, LoadOrder, Enabled) VALUES('Default', NULL, 'Default node', 'RemoteStatusServerConnectionString={server=localhost:8500;integratedSecurity=true};datapublisherport=6165;AlarmServiceUrl=http://localhost:5018/alarmservices', 'File', 'Menu.xml', 1, 0, 1)");
                database.Connection.ExecuteNonQuery("UPDATE Node SET ID=" + nodeIDQueryString);
            }
        }

        /// <summary>
        /// Loads the default configuration for the ConfigurationEntity table.
        /// </summary>
        /// <param name="database">The database connection.</param>
        /// <param name="statusMessage">The delegate which will display a status message to the user.</param>
        /// <param name="processException">The delegate which will handle exception logging.</param>
        private static void LoadDefaultConfigurationEntity(AdoDataConnection database, Action<string> statusMessage, Action<Exception> processException)
        {
            if (Convert.ToInt32(database.Connection.ExecuteScalar("SELECT COUNT(*) FROM ConfigurationEntity")) == 0)
            {
                statusMessage("Loading default records for ConfigurationEntity...");
                database.Connection.ExecuteNonQuery("INSERT INTO ConfigurationEntity(SourceName, RuntimeName, Description, LoadOrder, Enabled) VALUES('IaonInputAdapter', 'InputAdapters', 'Defines IInputAdapter definitions for a PDC node', 1, 1)");
                database.Connection.ExecuteNonQuery("INSERT INTO ConfigurationEntity(SourceName, RuntimeName, Description, LoadOrder, Enabled) VALUES('IaonActionAdapter', 'ActionAdapters', 'Defines IActionAdapter definitions for a PDC node', 2, 1)");
                database.Connection.ExecuteNonQuery("INSERT INTO ConfigurationEntity(SourceName, RuntimeName, Description, LoadOrder, Enabled) VALUES('IaonOutputAdapter', 'OutputAdapters', 'Defines IOutputAdapter definitions for a PDC node', 3, 1)");
                database.Connection.ExecuteNonQuery("INSERT INTO ConfigurationEntity(SourceName, RuntimeName, Description, LoadOrder, Enabled) VALUES('ActiveMeasurement', 'ActiveMeasurements', 'Defines active system measurements for a PDC node', 4, 1)");
                database.Connection.ExecuteNonQuery("INSERT INTO ConfigurationEntity(SourceName, RuntimeName, Description, LoadOrder, Enabled) VALUES('RuntimeInputStreamDevice', 'InputStreamDevices', 'Defines input stream devices associated with a concentrator', 5, 1)");
                database.Connection.ExecuteNonQuery("INSERT INTO ConfigurationEntity(SourceName, RuntimeName, Description, LoadOrder, Enabled) VALUES('RuntimeOutputStreamDevice', 'OutputStreamDevices', 'Defines output stream devices defined for a concentrator', 6, 1)");
                database.Connection.ExecuteNonQuery("INSERT INTO ConfigurationEntity(SourceName, RuntimeName, Description, LoadOrder, Enabled) VALUES('RuntimeOutputStreamMeasurement', 'OutputStreamMeasurements', 'Defines output stream measurements for an output stream', 7, 1)");
                database.Connection.ExecuteNonQuery("INSERT INTO ConfigurationEntity(SourceName, RuntimeName, Description, LoadOrder, Enabled) VALUES('OutputStreamDevicePhasor', 'OutputStreamDevicePhasors', 'Defines phasors for output stream devices', 8, 1)");
                database.Connection.ExecuteNonQuery("INSERT INTO ConfigurationEntity(SourceName, RuntimeName, Description, LoadOrder, Enabled) VALUES('OutputStreamDeviceAnalog', 'OutputStreamDeviceAnalogs', 'Defines analog values for output stream devices', 9, 1)");
                database.Connection.ExecuteNonQuery("INSERT INTO ConfigurationEntity(SourceName, RuntimeName, Description, LoadOrder, Enabled) VALUES('OutputStreamDeviceDigital', 'OutputStreamDeviceDigitals', 'Defines digital values for output stream devices', 10, 1)");
            }
        }

        /// <summary>
        /// Loads the default configuration for the Interconnection table.
        /// </summary>
        /// <param name="database">The database connection.</param>
        /// <param name="statusMessage">The delegate which will display a status message to the user.</param>
        /// <param name="processException">The delegate which will handle exception logging.</param>
        private static void LoadDefaultInterconnection(AdoDataConnection database, Action<string> statusMessage, Action<Exception> processException)
        {
            if (Convert.ToInt32(database.Connection.ExecuteScalar("SELECT COUNT(*) FROM Interconnection")) == 0)
            {
                statusMessage("Loading default records for Interconnection...");
                database.Connection.ExecuteNonQuery("INSERT INTO Interconnection(Acronym, Name, LoadOrder) VALUES('Eastern', 'Eastern Interconnection', 0)");
                database.Connection.ExecuteNonQuery("INSERT INTO Interconnection(Acronym, Name, LoadOrder) VALUES('Western', 'Western Interconnection', 1)");
                database.Connection.ExecuteNonQuery("INSERT INTO Interconnection(Acronym, Name, LoadOrder) VALUES('ERCOT', 'Texas Interconnection', 2)");
                database.Connection.ExecuteNonQuery("INSERT INTO Interconnection(Acronym, Name, LoadOrder) VALUES('Quebec', 'Quebec Interconnection', 3)");
                database.Connection.ExecuteNonQuery("INSERT INTO Interconnection(Acronym, Name, LoadOrder) VALUES('Alaskan', 'Alaskan Interconnection', 4)");
                database.Connection.ExecuteNonQuery("INSERT INTO Interconnection(Acronym, Name, LoadOrder) VALUES('Hawaii', 'Islands of Hawaii', 5)");
            }
        }

        /// <summary>
        /// Loads the default configuration for the Protocol table.
        /// </summary>
        /// <param name="database">The database connection.</param>
        /// <param name="statusMessage">The delegate which will display a status message to the user.</param>
        /// <param name="processException">The delegate which will handle exception logging.</param>
        private static void LoadDefaultProtocol(AdoDataConnection database, Action<string> statusMessage, Action<Exception> processException)
        {
            if (Convert.ToInt32(database.Connection.ExecuteScalar("SELECT COUNT(*) FROM Protocol")) == 0)
            {
                statusMessage("Loading default records for Protocol...");
                database.Connection.ExecuteNonQuery("INSERT INTO Protocol(Acronym, Name, Type, Category, AssemblyName, TypeName, LoadOrder) VALUES('IeeeC37_118V1', 'IEEE C37.118-2005', 'Frame', 'Phasor', 'PhasorProtocolAdapters.dll', 'PhasorProtocolAdapters.PhasorMeasurementMapper', 1)");
                database.Connection.ExecuteNonQuery("INSERT INTO Protocol(Acronym, Name, Type, Category, AssemblyName, TypeName, LoadOrder) VALUES('IeeeC37_118D6', 'IEEE C37.118 Draft 6', 'Frame', 'Phasor', 'PhasorProtocolAdapters.dll', 'PhasorProtocolAdapters.PhasorMeasurementMapper', 3)");
                database.Connection.ExecuteNonQuery("INSERT INTO Protocol(Acronym, Name, Type, Category, AssemblyName, TypeName, LoadOrder) VALUES('Ieee1344', 'IEEE 1344-1995', 'Frame', 'Phasor', 'PhasorProtocolAdapters.dll', 'PhasorProtocolAdapters.PhasorMeasurementMapper', 4)");
                database.Connection.ExecuteNonQuery("INSERT INTO Protocol(Acronym, Name, Type, Category, AssemblyName, TypeName, LoadOrder) VALUES('BpaPdcStream', 'BPA PDCstream', 'Frame', 'Phasor', 'PhasorProtocolAdapters.dll', 'PhasorProtocolAdapters.PhasorMeasurementMapper', 5)");
                database.Connection.ExecuteNonQuery("INSERT INTO Protocol(Acronym, Name, Type, Category, AssemblyName, TypeName, LoadOrder) VALUES('FNet', 'UTK FNET', 'Frame', 'Phasor', 'PhasorProtocolAdapters.dll', 'PhasorProtocolAdapters.PhasorMeasurementMapper', 6)");
                database.Connection.ExecuteNonQuery("INSERT INTO Protocol(Acronym, Name, Type, Category, AssemblyName, TypeName, LoadOrder) VALUES('SelFastMessage', 'SEL Fast Message', 'Frame', 'Phasor', 'PhasorProtocolAdapters.dll', 'PhasorProtocolAdapters.PhasorMeasurementMapper', 7)");
                database.Connection.ExecuteNonQuery("INSERT INTO Protocol(Acronym, Name, Type, Category, AssemblyName, TypeName, LoadOrder) VALUES('Macrodyne', 'Macrodyne', 'Frame', 'Phasor', 'PhasorProtocolAdapters.dll', 'PhasorProtocolAdapters.PhasorMeasurementMapper', 8)");
                database.Connection.ExecuteNonQuery("INSERT INTO Protocol(Acronym, Name, Type, Category, AssemblyName, TypeName, LoadOrder) VALUES('GatewayTransport', 'Gateway Transport', 'Measurement', 'Gateway', 'GSF.TimeSeries.dll', 'GSF.TimeSeries.Transport.DataSubscriber', 9)");
                database.Connection.ExecuteNonQuery("INSERT INTO Protocol(Acronym, Name, Type, Category, AssemblyName, TypeName, LoadOrder) VALUES('IeeeC37_118V2', 'IEEE C37.118.2-2011', 'Frame', 'Phasor', 'PhasorProtocolAdapters.dll', 'PhasorProtocolAdapters.PhasorMeasurementMapper', 2)");
                database.Connection.ExecuteNonQuery("INSERT INTO Protocol(Acronym, Name, Type, Category, AssemblyName, TypeName, LoadOrder) VALUES('WAV', 'Wave Form Input Adapter', 'Frame', 'Audio', 'WavInputAdapter.dll', 'WavInputAdapter.WavInputAdapter', 10)");
                database.Connection.ExecuteNonQuery("INSERT INTO Protocol(Acronym, Name, Type, Category, AssemblyName, TypeName, LoadOrder) VALUES('VirtualInput', 'Virtual Device', 'Frame', 'Virtual', 'TestingAdapters.dll', 'TestingAdapters.VirtualInputAdapter', 11)");
            }
        }

        /// <summary>
        /// Loads the default configuration for the SignalType table.
        /// </summary>
        /// <param name="database">The database connection.</param>
        /// <param name="statusMessage">The delegate which will display a status message to the user.</param>
        /// <param name="processException">The delegate which will handle exception logging.</param>
        private static void LoadDefaultSignalType(AdoDataConnection database, Action<string> statusMessage, Action<Exception> processException)
        {
            if (Convert.ToInt32(database.Connection.ExecuteScalar("SELECT COUNT(*) FROM SignalType WHERE Source = 'Phasor' OR Source = 'PMU'")) == 0)
            {
                statusMessage("Loading default records for SignalType...");
                database.Connection.ExecuteNonQuery("INSERT INTO SignalType(Name, Acronym, Suffix, Abbreviation, Source, EngineeringUnits) VALUES('Current Magnitude', 'IPHM', 'PM', 'I', 'Phasor', 'Amps')");
                database.Connection.ExecuteNonQuery("INSERT INTO SignalType(Name, Acronym, Suffix, Abbreviation, Source, EngineeringUnits) VALUES('Current Phase Angle', 'IPHA', 'PA', 'IH', 'Phasor', 'Degrees')");
                database.Connection.ExecuteNonQuery("INSERT INTO SignalType(Name, Acronym, Suffix, Abbreviation, Source, EngineeringUnits) VALUES('Voltage Magnitude', 'VPHM', 'PM', 'V', 'Phasor', 'Volts')");
                database.Connection.ExecuteNonQuery("INSERT INTO SignalType(Name, Acronym, Suffix, Abbreviation, Source, EngineeringUnits) VALUES('Voltage Phase Angle', 'VPHA', 'PA', 'VH', 'Phasor', 'Degrees')");
                database.Connection.ExecuteNonQuery("INSERT INTO SignalType(Name, Acronym, Suffix, Abbreviation, Source, EngineeringUnits) VALUES('Frequency', 'FREQ', 'FQ', 'F', 'PMU', 'Hz')");
                database.Connection.ExecuteNonQuery("INSERT INTO SignalType(Name, Acronym, Suffix, Abbreviation, Source, EngineeringUnits) VALUES('Frequency Delta (dF/dt)', 'DFDT', 'DF', 'DF', 'PMU', '')");
                database.Connection.ExecuteNonQuery("INSERT INTO SignalType(Name, Acronym, Suffix, Abbreviation, Source, EngineeringUnits) VALUES('Analog Value', 'ALOG', 'AV', 'A', 'PMU', '')");
                database.Connection.ExecuteNonQuery("INSERT INTO SignalType(Name, Acronym, Suffix, Abbreviation, Source, EngineeringUnits) VALUES('Status Flags', 'FLAG', 'SF', 'S', 'PMU', '')");
                database.Connection.ExecuteNonQuery("INSERT INTO SignalType(Name, Acronym, Suffix, Abbreviation, Source, EngineeringUnits) VALUES('Digital Value', 'DIGI', 'DV', 'D', 'PMU', '')");
                database.Connection.ExecuteNonQuery("INSERT INTO SignalType(Name, Acronym, Suffix, Abbreviation, Source, EngineeringUnits) VALUES('Calculated Value', 'CALC', 'CV', 'C', 'PMU', '')");
            }
        }

        /// <summary>
        /// Loads the default configuration for the Statistic table.
        /// </summary>
        /// <param name="database">The database connection.</param>
        /// <param name="statusMessage">The delegate which will display a status message to the user.</param>
        /// <param name="processException">The delegate which will handle exception logging.</param>
        private static void LoadDefaultStatistic(AdoDataConnection database, Action<string> statusMessage, Action<Exception> processException)
        {
            if (Convert.ToInt32(database.Connection.ExecuteScalar("SELECT COUNT(*) FROM Statistic")) == 0)
            {
                statusMessage("Loading default records for Statistic...");
                LoadStatistic(database, "INSERT INTO Statistic(Source, SignalIndex, Name, Description, AssemblyName, TypeName, MethodName, Arguments, Enabled, DataType, DisplayFormat, IsConnectedState, LoadOrder) VALUES('InputStream', 1, 'Total Frames', 'Total number of frames received from input stream during last reporting interval.', 'PhasorProtocolAdapters.dll', 'PhasorProtocolAdapters.CommonPhasorServices', 'GetInputStreamStatistic_TotalFrames', '', 1, 'System.Int32', @displayFormat, 0, 2)", "{0:N0}");
                LoadStatistic(database, "INSERT INTO Statistic(Source, SignalIndex, Name, Description, AssemblyName, TypeName, MethodName, Arguments, Enabled, DataType, DisplayFormat, IsConnectedState, LoadOrder) VALUES('InputStream', 2, 'Last Report Time', 'Timestamp of last received data frame from input stream.', 'PhasorProtocolAdapters.dll', 'PhasorProtocolAdapters.CommonPhasorServices', 'GetInputStreamStatistic_LastReportTime', '', 1, 'System.DateTime', @displayFormat, 0, 1)", "{0:mm':'ss'.'fff}");
                LoadStatistic(database, "INSERT INTO Statistic(Source, SignalIndex, Name, Description, AssemblyName, TypeName, MethodName, Arguments, Enabled, DataType, DisplayFormat, IsConnectedState, LoadOrder) VALUES('InputStream', 3, 'Missing Frames', 'Number of frames that were not received from input stream during last reporting interval.', 'PhasorProtocolAdapters.dll', 'PhasorProtocolAdapters.CommonPhasorServices', 'GetInputStreamStatistic_MissingFrames', '', 1, 'System.Int32', @displayFormat, 0, 3)", "{0:N0}");
                LoadStatistic(database, "INSERT INTO Statistic(Source, SignalIndex, Name, Description, AssemblyName, TypeName, MethodName, Arguments, Enabled, DataType, DisplayFormat, IsConnectedState, LoadOrder) VALUES('InputStream', 4, 'CRC Errors', 'Number of CRC errors reported from input stream during last reporting interval.', 'PhasorProtocolAdapters.dll', 'PhasorProtocolAdapters.CommonPhasorServices', 'GetInputStreamStatistic_CRCErrors', '', 1, 'System.Int32', @displayFormat, 0, 16)", "{0:N0}");
                LoadStatistic(database, "INSERT INTO Statistic(Source, SignalIndex, Name, Description, AssemblyName, TypeName, MethodName, Arguments, Enabled, DataType, DisplayFormat, IsConnectedState, LoadOrder) VALUES('InputStream', 5, 'Out of Order Frames', 'Number of out-of-order frames received from input stream during last reporting interval.', 'PhasorProtocolAdapters.dll', 'PhasorProtocolAdapters.CommonPhasorServices', 'GetInputStreamStatistic_OutOfOrderFrames', '', 1, 'System.Int32', @displayFormat, 0, 17)", "{0:N0}");
                LoadStatistic(database, "INSERT INTO Statistic(Source, SignalIndex, Name, Description, AssemblyName, TypeName, MethodName, Arguments, Enabled, DataType, DisplayFormat, IsConnectedState, LoadOrder) VALUES('InputStream', 6, 'Minimum Latency', 'Minimum latency from input stream, in milliseconds, during last reporting interval.', 'PhasorProtocolAdapters.dll', 'PhasorProtocolAdapters.CommonPhasorServices', 'GetInputStreamStatistic_MinimumLatency', '', 1, 'System.Double', @displayFormat, 0, 10)", "{0:N3} ms");
                LoadStatistic(database, "INSERT INTO Statistic(Source, SignalIndex, Name, Description, AssemblyName, TypeName, MethodName, Arguments, Enabled, DataType, DisplayFormat, IsConnectedState, LoadOrder) VALUES('InputStream', 7, 'Maximum Latency', 'Maximum latency from input stream, in milliseconds, during last reporting interval.', 'PhasorProtocolAdapters.dll', 'PhasorProtocolAdapters.CommonPhasorServices', 'GetInputStreamStatistic_MaximumLatency', '', 1, 'System.Double', @displayFormat, 0, 11)", "{0:N3} ms");
                LoadStatistic(database, "INSERT INTO Statistic(Source, SignalIndex, Name, Description, AssemblyName, TypeName, MethodName, Arguments, Enabled, DataType, DisplayFormat, IsConnectedState, LoadOrder) VALUES('InputStream', 8, 'Input Stream Connected', 'Boolean value representing if input stream was continually connected during last reporting interval.', 'PhasorProtocolAdapters.dll', 'PhasorProtocolAdapters.CommonPhasorServices', 'GetInputStreamStatistic_Connected', '', 1, 'System.Boolean', @displayFormat, 1, 18)", "{0}");
                LoadStatistic(database, "INSERT INTO Statistic(Source, SignalIndex, Name, Description, AssemblyName, TypeName, MethodName, Arguments, Enabled, DataType, DisplayFormat, IsConnectedState, LoadOrder) VALUES('InputStream', 9, 'Received Configuration', 'Boolean value representing if input stream has received (or has cached) a configuration frame during last reporting interval.', 'PhasorProtocolAdapters.dll', 'PhasorProtocolAdapters.CommonPhasorServices', 'GetInputStreamStatistic_ReceivedConfiguration', '', 1, 'System.Boolean', @displayFormat, 0, 8)", "{0}");
                LoadStatistic(database, "INSERT INTO Statistic(Source, SignalIndex, Name, Description, AssemblyName, TypeName, MethodName, Arguments, Enabled, DataType, DisplayFormat, IsConnectedState, LoadOrder) VALUES('InputStream', 10, 'Configuration Changes', 'Number of configuration changes reported by input stream during last reporting interval.', 'PhasorProtocolAdapters.dll', 'PhasorProtocolAdapters.CommonPhasorServices', 'GetInputStreamStatistic_ConfigurationChanges', '', 1, 'System.Int32', @displayFormat, 0, 9)", "{0:N0}");
                LoadStatistic(database, "INSERT INTO Statistic(Source, SignalIndex, Name, Description, AssemblyName, TypeName, MethodName, Arguments, Enabled, DataType, DisplayFormat, IsConnectedState, LoadOrder) VALUES('InputStream', 11, 'Total Data Frames', 'Number of data frames received from input stream during last reporting interval.', 'PhasorProtocolAdapters.dll', 'PhasorProtocolAdapters.CommonPhasorServices', 'GetInputStreamStatistic_TotalDataFrames', '', 1, 'System.Int32', @displayFormat, 0, 5)", "{0:N0}");
                LoadStatistic(database, "INSERT INTO Statistic(Source, SignalIndex, Name, Description, AssemblyName, TypeName, MethodName, Arguments, Enabled, DataType, DisplayFormat, IsConnectedState, LoadOrder) VALUES('InputStream', 12, 'Total Configuration Frames', 'Number of configuration frames received from input stream during last reporting interval.', 'PhasorProtocolAdapters.dll', 'PhasorProtocolAdapters.CommonPhasorServices', 'GetInputStreamStatistic_TotalConfigurationFrames', '', 1, 'System.Int32', @displayFormat, 0, 6)", "{0:N0}");
                LoadStatistic(database, "INSERT INTO Statistic(Source, SignalIndex, Name, Description, AssemblyName, TypeName, MethodName, Arguments, Enabled, DataType, DisplayFormat, IsConnectedState, LoadOrder) VALUES('InputStream', 13, 'Total Header Frames', 'Number of header frames received from input stream during last reporting interval.', 'PhasorProtocolAdapters.dll', 'PhasorProtocolAdapters.CommonPhasorServices', 'GetInputStreamStatistic_TotalHeaderFrames', '', 1, 'System.Int32', @displayFormat, 0, 7)", "{0:N0}");
                LoadStatistic(database, "INSERT INTO Statistic(Source, SignalIndex, Name, Description, AssemblyName, TypeName, MethodName, Arguments, Enabled, DataType, DisplayFormat, IsConnectedState, LoadOrder) VALUES('InputStream', 14, 'Average Latency', 'Average latency, in milliseconds, for data received from input stream during last reporting interval.', 'PhasorProtocolAdapters.dll', 'PhasorProtocolAdapters.CommonPhasorServices', 'GetInputStreamStatistic_AverageLatency', '', 1, 'System.Double', @displayFormat, 0, 12)", "{0:N3} ms");
                LoadStatistic(database, "INSERT INTO Statistic(Source, SignalIndex, Name, Description, AssemblyName, TypeName, MethodName, Arguments, Enabled, DataType, DisplayFormat, IsConnectedState, LoadOrder) VALUES('InputStream', 15, 'Defined Frame Rate', 'Frame rate as defined by input stream during last reporting interval.', 'PhasorProtocolAdapters.dll', 'PhasorProtocolAdapters.CommonPhasorServices', 'GetInputStreamStatistic_DefinedFrameRate', '', 1, 'System.Int32', @displayFormat, 0, 13)", "{0:N0} frames / second");
                LoadStatistic(database, "INSERT INTO Statistic(Source, SignalIndex, Name, Description, AssemblyName, TypeName, MethodName, Arguments, Enabled, DataType, DisplayFormat, IsConnectedState, LoadOrder) VALUES('InputStream', 16, 'Actual Frame Rate', 'Latest actual mean frame rate for data received from input stream during last reporting interval.', 'PhasorProtocolAdapters.dll', 'PhasorProtocolAdapters.CommonPhasorServices', 'GetInputStreamStatistic_ActualFrameRate', '', 1, 'System.Double', @displayFormat, 0, 14)", "{0:N3} frames / second");
                LoadStatistic(database, "INSERT INTO Statistic(Source, SignalIndex, Name, Description, AssemblyName, TypeName, MethodName, Arguments, Enabled, DataType, DisplayFormat, IsConnectedState, LoadOrder) VALUES('InputStream', 17, 'Actual Data Rate', 'Latest actual mean Mbps data rate for data received from input stream during last reporting interval.', 'PhasorProtocolAdapters.dll', 'PhasorProtocolAdapters.CommonPhasorServices', 'GetInputStreamStatistic_ActualDataRate', '', 1, 'System.Double', @displayFormat, 0, 15)", "{0:N3} Mbps");
                LoadStatistic(database, "INSERT INTO Statistic(Source, SignalIndex, Name, Description, AssemblyName, TypeName, MethodName, Arguments, Enabled, DataType, DisplayFormat, IsConnectedState, LoadOrder) VALUES('OutputStream', 1, 'Discarded Measurements', 'Number of discarded measurements reported by output stream during last reporting interval.', 'PhasorProtocolAdapters.dll', 'PhasorProtocolAdapters.CommonPhasorServices', 'GetOutputStreamStatistic_DiscardedMeasurements', '', 1, 'System.Int32', @displayFormat, 0, 4)", "{0:N0}");
                LoadStatistic(database, "INSERT INTO Statistic(Source, SignalIndex, Name, Description, AssemblyName, TypeName, MethodName, Arguments, Enabled, DataType, DisplayFormat, IsConnectedState, LoadOrder) VALUES('OutputStream', 2, 'Received Measurements', 'Number of received measurements reported by the output stream during last reporting interval.', 'PhasorProtocolAdapters.dll', 'PhasorProtocolAdapters.CommonPhasorServices', 'GetOutputStreamStatistic_ReceivedMeasurements', '', 1, 'System.Int32', @displayFormat, 0, 2)", "{0:N0}");
                LoadStatistic(database, "INSERT INTO Statistic(Source, SignalIndex, Name, Description, AssemblyName, TypeName, MethodName, Arguments, Enabled, DataType, DisplayFormat, IsConnectedState, LoadOrder) VALUES('OutputStream', 3, 'Expected Measurements', 'Number of expected measurements reported by the output stream during last reporting interval.', 'PhasorProtocolAdapters.dll', 'PhasorProtocolAdapters.CommonPhasorServices', 'GetOutputStreamStatistic_ExpectedMeasurements', '', 1, 'System.Int32', @displayFormat, 0, 1)", "{0:N0}");
                LoadStatistic(database, "INSERT INTO Statistic(Source, SignalIndex, Name, Description, AssemblyName, TypeName, MethodName, Arguments, Enabled, DataType, DisplayFormat, IsConnectedState, LoadOrder) VALUES('OutputStream', 4, 'Processed Measurements', 'Number of processed measurements reported by the output stream during last reporting interval.', 'PhasorProtocolAdapters.dll', 'PhasorProtocolAdapters.CommonPhasorServices', 'GetOutputStreamStatistic_ProcessedMeasurements', '', 1, 'System.Int32', @displayFormat, 0, 3)", "{0:N0}");
                LoadStatistic(database, "INSERT INTO Statistic(Source, SignalIndex, Name, Description, AssemblyName, TypeName, MethodName, Arguments, Enabled, DataType, DisplayFormat, IsConnectedState, LoadOrder) VALUES('OutputStream', 5, 'Measurements Sorted by Arrival', 'Number of measurements sorted by arrival reported by the output stream during last reporting interval.', 'PhasorProtocolAdapters.dll', 'PhasorProtocolAdapters.CommonPhasorServices', 'GetOutputStreamStatistic_MeasurementsSortedByArrival', '', 1, 'System.Int32', @displayFormat, 0, 7)", "{0:N0}");
                LoadStatistic(database, "INSERT INTO Statistic(Source, SignalIndex, Name, Description, AssemblyName, TypeName, MethodName, Arguments, Enabled, DataType, DisplayFormat, IsConnectedState, LoadOrder) VALUES('OutputStream', 6, 'Published Measurements', 'Number of published measurements reported by output stream during last reporting interval.', 'PhasorProtocolAdapters.dll', 'PhasorProtocolAdapters.CommonPhasorServices', 'GetOutputStreamStatistic_PublishedMeasurements', '', 1, 'System.Int32', @displayFormat, 0, 5)", "{0:N0}");
                LoadStatistic(database, "INSERT INTO Statistic(Source, SignalIndex, Name, Description, AssemblyName, TypeName, MethodName, Arguments, Enabled, DataType, DisplayFormat, IsConnectedState, LoadOrder) VALUES('OutputStream', 7, 'Down-sampled Measurements', 'Number of down-sampled measurements reported by the output stream during last reporting interval.', 'PhasorProtocolAdapters.dll', 'PhasorProtocolAdapters.CommonPhasorServices', 'GetOutputStreamStatistic_DownsampledMeasurements', '', 1, 'System.Int32', @displayFormat, 0, 6)", "{0:N0}");
                LoadStatistic(database, "INSERT INTO Statistic(Source, SignalIndex, Name, Description, AssemblyName, TypeName, MethodName, Arguments, Enabled, DataType, DisplayFormat, IsConnectedState, LoadOrder) VALUES('OutputStream', 8, 'Missed Sorts by Timeout', 'Number of missed sorts by timeout reported by the output stream during last reporting interval.', 'PhasorProtocolAdapters.dll', 'PhasorProtocolAdapters.CommonPhasorServices', 'GetOutputStreamStatistic_MissedSortsByTimeout', '', 1, 'System.Int32', @displayFormat, 0, 8)", "{0:N0}");
                LoadStatistic(database, "INSERT INTO Statistic(Source, SignalIndex, Name, Description, AssemblyName, TypeName, MethodName, Arguments, Enabled, DataType, DisplayFormat, IsConnectedState, LoadOrder) VALUES('OutputStream', 9, 'Frames Ahead of Schedule', 'Number of frames ahead of schedule reported by the output stream during last reporting interval.', 'PhasorProtocolAdapters.dll', 'PhasorProtocolAdapters.CommonPhasorServices', 'GetOutputStreamStatistic_FramesAheadOfSchedule', '', 1, 'System.Int32', @displayFormat, 0, 9)", "{0:N0}");
                LoadStatistic(database, "INSERT INTO Statistic(Source, SignalIndex, Name, Description, AssemblyName, TypeName, MethodName, Arguments, Enabled, DataType, DisplayFormat, IsConnectedState, LoadOrder) VALUES('OutputStream', 10, 'Published Frames', 'Number of published frames reported by the output stream during last reporting interval.', 'PhasorProtocolAdapters.dll', 'PhasorProtocolAdapters.CommonPhasorServices', 'GetOutputStreamStatistic_PublishedFrames', '', 1, 'System.Int32', @displayFormat, 0, 10)", "{0:N0}");
                LoadStatistic(database, "INSERT INTO Statistic(Source, SignalIndex, Name, Description, AssemblyName, TypeName, MethodName, Arguments, Enabled, DataType, DisplayFormat, IsConnectedState, LoadOrder) VALUES('OutputStream', 11, 'Output Stream Connected', 'Boolean value representing if the output stream was continually connected during last reporting interval.', 'PhasorProtocolAdapters.dll', 'PhasorProtocolAdapters.CommonPhasorServices', 'GetOutputStreamStatistic_Connected', '', 1, 'System.Boolean', @displayFormat, 1, 11)", "{0}");
            }

            // Make sure new input stream statistics are defined
            if (Convert.ToInt32(database.Connection.ExecuteScalar("SELECT COUNT(*) FROM Statistic WHERE MethodName = 'GetInputStreamStatistic_MissingData'")) == 0)
                LoadStatistic(database, "INSERT INTO Statistic(Source, SignalIndex, Name, Description, AssemblyName, TypeName, MethodName, Arguments, Enabled, DataType, DisplayFormat, IsConnectedState, LoadOrder) VALUES('InputStream', 18, 'Missing Data', 'Number of data units that were not received at least once from input stream during last reporting interval.', 'PhasorProtocolAdapters.dll', 'PhasorProtocolAdapters.CommonPhasorServices', 'GetInputStreamStatistic_MissingData', '', 1, 'System.Int32', @displayFormat, 0, 4)", "{0:N0}");

            // Make sure new output stream statistics are defined
            if (Convert.ToInt32(database.Connection.ExecuteScalar("SELECT COUNT(*) FROM Statistic WHERE MethodName = 'GetOutputStreamStatistic_MinimumLatency'")) == 0)
                LoadStatistic(database, "INSERT INTO Statistic(Source, SignalIndex, Name, Description, AssemblyName, TypeName, MethodName, Arguments, Enabled, DataType, DisplayFormat, IsConnectedState, LoadOrder) VALUES('OutputStream', 12, 'Minimum Latency', 'Minimum latency from output stream, in milliseconds, during last reporting interval.', 'PhasorProtocolAdapters.dll', 'PhasorProtocolAdapters.CommonPhasorServices', 'GetOutputStreamStatistic_MinimumLatency', '', 1, 'System.Double', @displayFormat, 0, 12)", "{0:N3} ms");

            if (Convert.ToInt32(database.Connection.ExecuteScalar("SELECT COUNT(*) FROM Statistic WHERE MethodName = 'GetOutputStreamStatistic_MaximumLatency'")) == 0)
                LoadStatistic(database, "INSERT INTO Statistic(Source, SignalIndex, Name, Description, AssemblyName, TypeName, MethodName, Arguments, Enabled, DataType, DisplayFormat, IsConnectedState, LoadOrder) VALUES('OutputStream', 13, 'Maximum Latency', 'Maximum latency from output stream, in milliseconds, during last reporting interval.', 'PhasorProtocolAdapters.dll', 'PhasorProtocolAdapters.CommonPhasorServices', 'GetOutputStreamStatistic_MaximumLatency', '', 1, 'System.Double', @displayFormat, 0, 13)", "{0:N3} ms");

            if (Convert.ToInt32(database.Connection.ExecuteScalar("SELECT COUNT(*) FROM Statistic WHERE MethodName = 'GetOutputStreamStatistic_AverageLatency'")) == 0)
                LoadStatistic(database, "INSERT INTO Statistic(Source, SignalIndex, Name, Description, AssemblyName, TypeName, MethodName, Arguments, Enabled, DataType, DisplayFormat, IsConnectedState, LoadOrder) VALUES('OutputStream', 14, 'Average Latency', 'Average latency, in milliseconds, for data published from output stream during last reporting interval.', 'PhasorProtocolAdapters.dll', 'PhasorProtocolAdapters.CommonPhasorServices', 'GetOutputStreamStatistic_AverageLatency', '', 1, 'System.Double', @displayFormat, 0, 14)", "{0:N3} ms");

            if (Convert.ToInt32(database.Connection.ExecuteScalar("SELECT COUNT(*) FROM Statistic WHERE MethodName = 'GetOutputStreamStatistic_ConnectedClientCount'")) == 0)
                LoadStatistic(database, "INSERT INTO Statistic(Source, SignalIndex, Name, Description, AssemblyName, TypeName, MethodName, Arguments, Enabled, DataType, DisplayFormat, IsConnectedState, LoadOrder) VALUES('OutputStream', 15, 'Connected Clients', 'Number of clients connected to the command channel of the output stream during last reporting interval.', 'PhasorProtocolAdapters.dll', 'PhasorProtocolAdapters.CommonPhasorServices', 'GetOutputStreamStatistic_ConnectedClientCount', '', 1, 'System.Int32', @displayFormat, 0, 15)", "{0:N0}");
        }

        [SuppressMessage("Microsoft.Security", "CA2100")]
        private static void LoadStatistic(AdoDataConnection database, string commandText, string displayFormat)
        {
            string query = database.IsOracle ? commandText.Replace("@", ":") : commandText;
            database.Connection.ExecuteNonQuery(query, displayFormat);
        }

        /// <summary>
        /// Establish default <see cref="MeasurementKey"/> cache.
        /// </summary>
        /// <param name="database">The database connection.</param>
        /// <param name="statusMessage">The delegate which will display a status message to the user.</param>
        /// <param name="processException">The delegate which will handle exception logging.</param>
        [SuppressMessage("Microsoft.Usage", "CA1806")]
        private static void EstablishDefaultMeasurementKeyCache(AdoDataConnection database, Action<string> statusMessage, Action<Exception> processException)
        {
            MeasurementKey key;
            string keyID;

            statusMessage("Establishing default measurement key cache...");

            // Establish default measurement key cache
            foreach (DataRow measurement in database.Connection.RetrieveData(database.AdapterType, "SELECT ID, SignalID FROM ActiveMeasurement").Rows)
            {
                keyID = measurement["ID"].ToNonNullString();

                if (string.IsNullOrWhiteSpace(keyID))
                    continue;

                // Cache new measurement key with associated Guid signal ID
                MeasurementKey.TryCreateOrUpdate(measurement["SignalID"].ToNonNullString(Guid.Empty.ToString()).ConvertToType<Guid>(), keyID, out key);
            }
        }

        #region [ InputStream Statistic Calculators ]

        /// <summary>
        /// Calculates total number of frames received from input stream during last reporting interval.
        /// </summary>
        /// <param name="source">Source InputStream.</param>
        /// <param name="arguments">Any needed arguments for statistic calculation.</param>
        /// <remarks>
        /// This statistic also calculates the other frame count statistics so its load order must occur first.
        /// </remarks>
        /// <returns>Total Frames Statistic.</returns>
        private static double GetInputStreamStatistic_TotalFrames(object source, string arguments)
        {
            double statistic = 0.0D;
            PhasorMeasurementMapper inputStream = source as PhasorMeasurementMapper;

            if ((object)inputStream != null)
                statistic = s_statisticValueCache.GetDifference(inputStream, inputStream.TotalFrames, "TotalFrames");

            return statistic;
        }

        /// <summary>
        /// Calculates timestamp of last received data frame from input stream.
        /// </summary>
        /// <param name="source">Source InputStream.</param>
        /// <param name="arguments">Any needed arguments for statistic calculation.</param>
        /// <returns>Last Report Time Statistic.</returns>
        private static double GetInputStreamStatistic_LastReportTime(object source, string arguments)
        {
            double statistic = 0.0D;
            PhasorMeasurementMapper inputStream = source as PhasorMeasurementMapper;

            // Local archival uses a 32-bit floating point number for statistical value storage so we
            // reduce the last reporting time resolution down to the hour to make sure the archived
            // timestamp is accurate at least to the milliseconds - remaining date/time high data bits
            // can be later deduced from the statistic's archival timestamp
            if ((object)inputStream != null)
            {
                Ticks lastReportTime = inputStream.LastReportTime;
                statistic = lastReportTime - lastReportTime.BaselinedTimestamp(BaselineTimeInterval.Hour);
            }

            return statistic;
        }

        /// <summary>
        /// Calculates number of frames that were not received from input stream during last reporting interval.
        /// </summary>
        /// <param name="source">Source InputStream.</param>
        /// <param name="arguments">Any needed arguments for statistic calculation.</param>
        /// <returns>Missing Frames Statistic.</returns>
        private static double GetInputStreamStatistic_MissingFrames(object source, string arguments)
        {
            double statistic = 0.0D;
            PhasorMeasurementMapper inputStream = source as PhasorMeasurementMapper;

            if ((object)inputStream != null)
                statistic = s_statisticValueCache.GetDifference(inputStream, inputStream.MissingFrames, "MissingFrames");

            return statistic;
        }

        /// <summary>
        /// Calculates number of data units that were not received at least once from input stream during last reporting interval.
        /// </summary>
        /// <param name="source">Source InputStream.</param>
        /// <param name="arguments">Any needed arguments for statistic calculation.</param>
        /// <returns>Missing Data Statistic.</returns>
        private static double GetInputStreamStatistic_MissingData(object source, string arguments)
        {
            double statistic = 0.0D;
            PhasorMeasurementMapper inputStream = source as PhasorMeasurementMapper;

            if ((object)inputStream != null)
                statistic = s_statisticValueCache.GetDifference(inputStream, inputStream.MissingData, "MissingData");

            return statistic;
        }

        /// <summary>
        /// Calculates number of CRC errors reported from input stream during last reporting interval.
        /// </summary>
        /// <param name="source">Source InputStream.</param>
        /// <param name="arguments">Any needed arguments for statistic calculation.</param>
        /// <returns>CRC Errors Statistic.</returns>
        private static double GetInputStreamStatistic_CRCErrors(object source, string arguments)
        {
            double statistic = 0.0D;
            PhasorMeasurementMapper inputStream = source as PhasorMeasurementMapper;

            if ((object)inputStream != null)
                statistic = s_statisticValueCache.GetDifference(inputStream, inputStream.CRCErrors, "CRCErrors");

            return statistic;
        }

        /// <summary>
        /// Calculates number of out-of-order frames received from input stream during last reporting interval.
        /// </summary>
        /// <param name="source">Source InputStream.</param>
        /// <param name="arguments">Any needed arguments for statistic calculation.</param>
        /// <returns>Out of Order Frames Statistic.</returns>
        private static double GetInputStreamStatistic_OutOfOrderFrames(object source, string arguments)
        {
            double statistic = 0.0D;
            PhasorMeasurementMapper inputStream = source as PhasorMeasurementMapper;

            if ((object)inputStream != null)
                statistic = s_statisticValueCache.GetDifference(inputStream, inputStream.OutOfOrderFrames, "OutOfOrderFrames");

            return statistic;
        }

        /// <summary>
        /// Calculates minimum latency from input stream, in milliseconds, during last reporting interval.
        /// </summary>
        /// <param name="source">Source InputStream.</param>
        /// <param name="arguments">Any needed arguments for statistic calculation.</param>
        /// <remarks>
        /// This statistic also calculates the maximum and average latency statistics so its load order must occur first.
        /// </remarks>
        /// <returns>Minimum Latency Statistic.</returns>
        private static double GetInputStreamStatistic_MinimumLatency(object source, string arguments)
        {
            double statistic = 0.0D;
            PhasorMeasurementMapper inputStream = source as PhasorMeasurementMapper;

            if ((object)inputStream != null)
                statistic = inputStream.MinimumLatency;

            return statistic;
        }

        /// <summary>
        /// Calculates maximum latency from input stream, in milliseconds, during last reporting interval.
        /// </summary>
        /// <param name="source">Source InputStream.</param>
        /// <param name="arguments">Any needed arguments for statistic calculation.</param>
        /// <returns>Maximum Latency Statistic.</returns>
        private static double GetInputStreamStatistic_MaximumLatency(object source, string arguments)
        {
            double statistic = 0.0D;
            PhasorMeasurementMapper inputStream = source as PhasorMeasurementMapper;

            if ((object)inputStream != null)
                statistic = inputStream.MaximumLatency;

            return statistic;
        }

        /// <summary>
        /// Calculates average latency, in milliseconds, for data received from input stream during last reporting interval.
        /// </summary>
        /// <param name="source">Source InputStream.</param>
        /// <param name="arguments">Any needed arguments for statistic calculation.</param>
        /// <returns>Average Latency Statistic.</returns>
        private static double GetInputStreamStatistic_AverageLatency(object source, string arguments)
        {
            double statistic = 0.0D;
            PhasorMeasurementMapper inputStream = source as PhasorMeasurementMapper;

            if ((object)inputStream != null)
                statistic = inputStream.AverageLatency;

            return statistic;
        }

        /// <summary>
        /// Calculates boolean value representing if input stream was continually connected during last reporting interval.
        /// </summary>
        /// <param name="source">Source InputStream.</param>
        /// <param name="arguments">Any needed arguments for statistic calculation.</param>
        /// <returns>Input Stream Connected Statistic.</returns>
        private static double GetInputStreamStatistic_Connected(object source, string arguments)
        {
            double statistic = 0.0D;
            PhasorMeasurementMapper inputStream = source as PhasorMeasurementMapper;

            if ((object)inputStream != null)
            {
                if (inputStream.IsConnected)
                    statistic = (s_statisticValueCache.GetDifference(inputStream, inputStream.ConnectionAttempts, "ConnectionAttempts") == 0.0D ? 1.0D : 0.0D);
            }

            return statistic;
        }

        /// <summary>
        /// Calculates boolean value representing if input stream has received (or has cached) a configuration frame during last reporting interval.
        /// </summary>
        /// <param name="source">Source InputStream.</param>
        /// <param name="arguments">Any needed arguments for statistic calculation.</param>
        /// <remarks>
        /// This statistic also calculates the total configuration changes so its load order must occur first.
        /// </remarks>
        /// <returns>Received Configuration Statistic.</returns>
        private static double GetInputStreamStatistic_ReceivedConfiguration(object source, string arguments)
        {
            double statistic = 0.0D;
            PhasorMeasurementMapper inputStream = source as PhasorMeasurementMapper;

            if ((object)inputStream != null)
            {
                double configChanges = s_statisticValueCache.GetDifference(inputStream, inputStream.ConfigurationChanges, "ReceivedConfiguration");
                statistic = (configChanges > 0 ? 1.0D : 0.0D);
            }

            return statistic;
        }

        /// <summary>
        /// Calculates number of configuration changes reported by input stream during last reporting interval.
        /// </summary>
        /// <param name="source">Source InputStream.</param>
        /// <param name="arguments">Any needed arguments for statistic calculation.</param>
        /// <returns>Configuration Changes Statistic.</returns>
        private static double GetInputStreamStatistic_ConfigurationChanges(object source, string arguments)
        {
            double statistic = 0.0D;
            PhasorMeasurementMapper inputStream = source as PhasorMeasurementMapper;

            if ((object)inputStream != null)
                statistic = (long)s_statisticValueCache.GetDifference(inputStream, inputStream.ConfigurationChanges, "ConfigurationChanges");

            return statistic;
        }

        /// <summary>
        /// Calculates number of data frames received from input stream during last reporting interval.
        /// </summary>
        /// <param name="source">Source InputStream.</param>
        /// <param name="arguments">Any needed arguments for statistic calculation.</param>
        /// <returns>Total Data Frames Statistic.</returns>
        private static double GetInputStreamStatistic_TotalDataFrames(object source, string arguments)
        {
            double statistic = 0.0D;
            PhasorMeasurementMapper inputStream = source as PhasorMeasurementMapper;

            if ((object)inputStream != null)
                statistic = s_statisticValueCache.GetDifference(inputStream, inputStream.TotalDataFrames, "TotalDataFrames");

            return statistic;
        }

        /// <summary>
        /// Calculates number of configuration frames received from input stream during last reporting interval.
        /// </summary>
        /// <param name="source">Source InputStream.</param>
        /// <param name="arguments">Any needed arguments for statistic calculation.</param>
        /// <returns>Total Configuration Frames Statistic.</returns>
        private static double GetInputStreamStatistic_TotalConfigurationFrames(object source, string arguments)
        {
            double statistic = 0.0D;
            PhasorMeasurementMapper inputStream = source as PhasorMeasurementMapper;

            if ((object)inputStream != null)
                statistic = s_statisticValueCache.GetDifference(inputStream, inputStream.TotalConfigurationFrames, "TotalConfigurationFrames");

            return statistic;
        }

        /// <summary>
        /// Calculates number of header frames received from input stream during last reporting interval.
        /// </summary>
        /// <param name="source">Source InputStream.</param>
        /// <param name="arguments">Any needed arguments for statistic calculation.</param>
        /// <returns>Total Header Frames Statistic.</returns>
        private static double GetInputStreamStatistic_TotalHeaderFrames(object source, string arguments)
        {
            double statistic = 0.0D;
            PhasorMeasurementMapper inputStream = source as PhasorMeasurementMapper;

            if ((object)inputStream != null)
                statistic = s_statisticValueCache.GetDifference(inputStream, inputStream.TotalHeaderFrames, "TotalHeaderFrames");

            return statistic;
        }

        /// <summary>
        /// Calculates frame rate as defined by input stream during last reporting interval.
        /// </summary>
        /// <param name="source">Source InputStream.</param>
        /// <param name="arguments">Any needed arguments for statistic calculation.</param>
        /// <returns>Defined Frame Rate Statistic.</returns>
        private static double GetInputStreamStatistic_DefinedFrameRate(object source, string arguments)
        {
            double statistic = 0.0D;
            PhasorMeasurementMapper inputStream = source as PhasorMeasurementMapper;

            if ((object)inputStream != null)
                statistic = inputStream.DefinedFrameRate;

            return statistic;
        }

        /// <summary>
        /// Calculates latest actual mean frame rate for data received from input stream during last reporting interval.
        /// </summary>
        /// <param name="source">Source InputStream.</param>
        /// <param name="arguments">Any needed arguments for statistic calculation.</param>
        /// <returns>Actual Frame Rate Statistic.</returns>
        private static double GetInputStreamStatistic_ActualFrameRate(object source, string arguments)
        {
            double statistic = 0.0D;
            PhasorMeasurementMapper inputStream = source as PhasorMeasurementMapper;

            if ((object)inputStream != null)
                statistic = inputStream.ActualFrameRate;

            return statistic;
        }

        /// <summary>
        /// Calculates latest actual mean Mbps data rate for data received from input stream during last reporting interval.
        /// </summary>
        /// <param name="source">Source InputStream.</param>
        /// <param name="arguments">Any needed arguments for statistic calculation.</param>
        /// <returns>Actual Data Rate Statistic.</returns>
        private static double GetInputStreamStatistic_ActualDataRate(object source, string arguments)
        {
            double statistic = 0.0D;
            PhasorMeasurementMapper inputStream = source as PhasorMeasurementMapper;

            if ((object)inputStream != null)
                statistic = inputStream.ActualDataRate * 8.0D / SI.Mega;

            return statistic;
        }

        private static double GetInputStreamStatistic_TotalBytesReceived(object source, string arguments)
        {
            double statistic = 0.0D;
            PhasorMeasurementMapper inputStream = source as PhasorMeasurementMapper;

            if ((object)inputStream != null)
            {
                statistic = s_statisticValueCache.GetDifference(source, inputStream.TotalBytesReceived, "TotalBytesReceived");

                if (statistic < 0.0D)
                    statistic = inputStream.TotalBytesReceived;
            }

            return statistic;
        }

        private static double GetInputStreamStatistic_LifetimeMeasurements(object source, string arguments)
        {
            double statistic = 0.0D;
            PhasorMeasurementMapper inputStream = source as PhasorMeasurementMapper;

            if ((object)inputStream != null)
                statistic = inputStream.LifetimeMeasurements;

            return statistic;
        }

        private static double GetInputStreamStatistic_MinimumMeasurementsPerSecond(object source, string arguments)
        {
            double statistic = 0.0D;
            PhasorMeasurementMapper inputStream = source as PhasorMeasurementMapper;

            if ((object)inputStream != null)
                statistic = inputStream.MinimumMeasurementsPerSecond;

            return statistic;
        }

        private static double GetInputStreamStatistic_MaximumMeasurementsPerSecond(object source, string arguments)
        {
            double statistic = 0.0D;
            PhasorMeasurementMapper inputStream = source as PhasorMeasurementMapper;

            if ((object)inputStream != null)
                statistic = inputStream.MaximumMeasurementsPerSecond;

            return statistic;
        }

        private static double GetInputStreamStatistic_AverageMeasurementsPerSecond(object source, string arguments)
        {
            double statistic = 0.0D;
            PhasorMeasurementMapper inputStream = source as PhasorMeasurementMapper;

            if ((object)inputStream != null)
                statistic = inputStream.AverageMeasurementsPerSecond;

            return statistic;
        }

        private static double GetInputStreamStatistic_LifetimeBytesReceived(object source, string arguments)
        {
            double statistic = 0.0D;
            PhasorMeasurementMapper inputStream = source as PhasorMeasurementMapper;

            if ((object)inputStream != null)
                statistic = inputStream.TotalBytesReceived;

            return statistic;
        }

        private static double GetInputStreamStatistic_LifetimeMinimumLatency(object source, string arguments)
        {
            double statistic = 0.0D;
            PhasorMeasurementMapper inputStream = source as PhasorMeasurementMapper;

            if ((object)inputStream != null)
                statistic = inputStream.LifetimeMinimumLatency;

            return statistic;
        }

        private static double GetInputStreamStatistic_LifetimeMaximumLatency(object source, string arguments)
        {
            double statistic = 0.0D;
            PhasorMeasurementMapper inputStream = source as PhasorMeasurementMapper;

            if ((object)inputStream != null)
                statistic = inputStream.LifetimeMaximumLatency;

            return statistic;
        }

        private static double GetInputStreamStatistic_LifetimeAverageLatency(object source, string arguments)
        {
            double statistic = 0.0D;
            PhasorMeasurementMapper inputStream = source as PhasorMeasurementMapper;

            if ((object)inputStream != null)
                statistic = inputStream.LifetimeAverageLatency;

            return statistic;
        }

        #endregion

        #region [ OutputStream Statistic Calculators ]

        /// <summary>
        /// Calculates number of discarded measurements reported by output stream during last reporting interval.
        /// </summary>
        /// <param name="source">Source OutputStream.</param>
        /// <param name="arguments">Any needed arguments for statistic calculation.</param>
        /// <returns>Discarded Measurements Statistic.</returns>
        private static double GetOutputStreamStatistic_DiscardedMeasurements(object source, string arguments)
        {
            double statistic = 0.0D;
            PhasorDataConcentratorBase outputStream = source as PhasorDataConcentratorBase;

            if ((object)outputStream != null)
                statistic = s_statisticValueCache.GetDifference(outputStream, outputStream.DiscardedMeasurements, "DiscardedMeasurements");

            return statistic;
        }

        /// <summary>
        /// Calculates number of received measurements reported by the output stream during last reporting interval.
        /// </summary>
        /// <param name="source">Source OutputStream.</param>
        /// <param name="arguments">Any needed arguments for statistic calculation.</param>
        /// <returns>Received Measurements Statistic.</returns>
        private static double GetOutputStreamStatistic_ReceivedMeasurements(object source, string arguments)
        {
            double statistic = 0.0D;
            PhasorDataConcentratorBase outputStream = source as PhasorDataConcentratorBase;

            if ((object)outputStream != null)
                statistic = s_statisticValueCache.GetDifference(outputStream, outputStream.ReceivedMeasurements, "ReceivedMeasurements");

            return statistic;
        }

        /// <summary>
        /// Calculates number of expected measurements reported by the output stream during last reporting interval.
        /// </summary>
        /// <param name="source">Source OutputStream.</param>
        /// <param name="arguments">Any needed arguments for statistic calculation.</param>
        /// <remarks>
        /// This statistic also calculates the total published frame count statistic so its load order must occur first.
        /// </remarks>
        /// <returns>Expected Measurements Statistic.</returns>
        private static double GetOutputStreamStatistic_ExpectedMeasurements(object source, string arguments)
        {
            double statistic = 0.0D;
            PhasorDataConcentratorBase outputStream = source as PhasorDataConcentratorBase;

            if ((object)outputStream != null)
            {
                double publishedFrames = s_statisticValueCache.GetDifference(outputStream, outputStream.PublishedFrames, "ExpectedMeasurements");
                statistic = outputStream.ExpectedMeasurements * publishedFrames;
            }

            return statistic;
        }

        /// <summary>
        /// Calculates number of processed measurements reported by the output stream during last reporting interval.
        /// </summary>
        /// <param name="source">Source OutputStream.</param>
        /// <param name="arguments">Any needed arguments for statistic calculation.</param>
        /// <returns>Processed Measurements Statistic.</returns>
        private static double GetOutputStreamStatistic_ProcessedMeasurements(object source, string arguments)
        {
            double statistic = 0.0D;
            PhasorDataConcentratorBase outputStream = source as PhasorDataConcentratorBase;

            if ((object)outputStream != null)
                statistic = s_statisticValueCache.GetDifference(outputStream, outputStream.ProcessedMeasurements, "ProcessedMeasurements");

            return statistic;
        }

        /// <summary>
        /// Calculates number of measurements sorted by arrival reported by the output stream during last reporting interval.
        /// </summary>
        /// <param name="source">Source OutputStream.</param>
        /// <param name="arguments">Any needed arguments for statistic calculation.</param>
        /// <returns>Measurements Sorted by Arrival Statistic.</returns>
        private static double GetOutputStreamStatistic_MeasurementsSortedByArrival(object source, string arguments)
        {
            double statistic = 0.0D;
            PhasorDataConcentratorBase outputStream = source as PhasorDataConcentratorBase;

            if ((object)outputStream != null)
                statistic = s_statisticValueCache.GetDifference(outputStream, outputStream.MeasurementsSortedByArrival, "MeasurementsSortedByArrival");

            return statistic;
        }

        /// <summary>
        /// Calculates number of published measurements reported by output stream during last reporting interval.
        /// </summary>
        /// <param name="source">Source OutputStream.</param>
        /// <param name="arguments">Any needed arguments for statistic calculation.</param>
        /// <returns>Published Measurements Statistic.</returns>
        private static double GetOutputStreamStatistic_PublishedMeasurements(object source, string arguments)
        {
            double statistic = 0.0D;
            PhasorDataConcentratorBase outputStream = source as PhasorDataConcentratorBase;

            if ((object)outputStream != null)
                statistic = s_statisticValueCache.GetDifference(outputStream, outputStream.PublishedMeasurements, "PublishedMeasurements");

            return statistic;
        }

        /// <summary>
        /// Calculates number of down-sampled measurements reported by the output stream during last reporting interval.
        /// </summary>
        /// <param name="source">Source OutputStream.</param>
        /// <param name="arguments">Any needed arguments for statistic calculation.</param>
        /// <returns>Down-sampled Measurements Statistic.</returns>
        private static double GetOutputStreamStatistic_DownsampledMeasurements(object source, string arguments)
        {
            double statistic = 0.0D;
            PhasorDataConcentratorBase outputStream = source as PhasorDataConcentratorBase;

            if ((object)outputStream != null)
                statistic = s_statisticValueCache.GetDifference(outputStream, outputStream.DownsampledMeasurements, "DownsampledMeasurements");

            return statistic;
        }

        /// <summary>
        /// Calculates number of missed sorts by timeout reported by the output stream during last reporting interval.
        /// </summary>
        /// <param name="source">Source OutputStream.</param>
        /// <param name="arguments">Any needed arguments for statistic calculation.</param>
        /// <returns>Missed Sorts by Timeout Statistic.</returns>
        private static double GetOutputStreamStatistic_MissedSortsByTimeout(object source, string arguments)
        {
            double statistic = 0.0D;
            PhasorDataConcentratorBase outputStream = source as PhasorDataConcentratorBase;

            if ((object)outputStream != null)
                statistic = s_statisticValueCache.GetDifference(outputStream, outputStream.MissedSortsByTimeout, "MissedSortsByTimeout");

            return statistic;
        }

        /// <summary>
        /// Calculates number of frames ahead of schedule reported by the output stream during last reporting interval.
        /// </summary>
        /// <param name="source">Source OutputStream.</param>
        /// <param name="arguments">Any needed arguments for statistic calculation.</param>
        /// <returns>Frames Ahead of Schedule Statistic.</returns>
        private static double GetOutputStreamStatistic_FramesAheadOfSchedule(object source, string arguments)
        {
            double statistic = 0.0D;
            PhasorDataConcentratorBase outputStream = source as PhasorDataConcentratorBase;

            if ((object)outputStream != null)
                statistic = s_statisticValueCache.GetDifference(outputStream, outputStream.FramesAheadOfSchedule, "FramesAheadOfSchedule");

            return statistic;
        }

        /// <summary>
        /// Calculates number of published frames reported by the output stream during last reporting interval.
        /// </summary>
        /// <param name="source">Source OutputStream.</param>
        /// <param name="arguments">Any needed arguments for statistic calculation.</param>
        /// <returns>Published Frames Statistic.</returns>
        private static double GetOutputStreamStatistic_PublishedFrames(object source, string arguments)
        {
            double statistic = 0.0D;
            PhasorDataConcentratorBase outputStream = source as PhasorDataConcentratorBase;

            if ((object)outputStream != null)
                statistic = s_statisticValueCache.GetDifference(outputStream, outputStream.PublishedFrames, "PublishedFrames");

            return statistic;
        }

        /// <summary>
        /// Calculates boolean value representing if the output stream was continually connected during last reporting interval.
        /// </summary>
        /// <param name="source">Source OutputStream.</param>
        /// <param name="arguments">Any needed arguments for statistic calculation.</param>
        /// <returns>Output Stream Connected Statistic.</returns>
        private static double GetOutputStreamStatistic_Connected(object source, string arguments)
        {
            double statistic = 0.0D;
            PhasorDataConcentratorBase outputStream = source as PhasorDataConcentratorBase;

            if ((object)outputStream != null)
            {
                if (outputStream.Enabled)
                    statistic = (s_statisticValueCache.GetDifference(outputStream, outputStream.ActiveConnections, "ActiveConnections") == 0.0D ? 1.0D : 0.0D);
            }

            return statistic;
        }

        /// <summary>
        /// Calculates minimum latency from output stream, in milliseconds, during last reporting interval.
        /// </summary>
        /// <param name="source">Source OutputStream.</param>
        /// <param name="arguments">Any needed arguments for statistic calculation.</param>
        /// <remarks>
        /// This statistic also calculates the maximum and average latency statistics so its load order must occur first.
        /// </remarks>
        /// <returns>Minimum Output Latency Statistic.</returns>
        private static double GetOutputStreamStatistic_MinimumLatency(object source, string arguments)
        {
            double statistic = 0.0D;
            PhasorDataConcentratorBase outputStream = source as PhasorDataConcentratorBase;

            if ((object)outputStream != null)
                statistic = outputStream.MinimumLatency;

            return statistic;
        }

        /// <summary>
        /// Calculates maximum latency from output stream, in milliseconds, during last reporting interval.
        /// </summary>
        /// <param name="source">Source OutputStream.</param>
        /// <param name="arguments">Any needed arguments for statistic calculation.</param>
        /// <returns>Maximum Output Latency Statistic.</returns>
        private static double GetOutputStreamStatistic_MaximumLatency(object source, string arguments)
        {
            double statistic = 0.0D;
            PhasorDataConcentratorBase outputStream = source as PhasorDataConcentratorBase;

            if ((object)outputStream != null)
                statistic = outputStream.MaximumLatency;

            return statistic;
        }

        /// <summary>
        /// Calculates average latency, in milliseconds, for data received from output stream during last reporting interval.
        /// </summary>
        /// <param name="source">Source OutputStream.</param>
        /// <param name="arguments">Any needed arguments for statistic calculation.</param>
        /// <returns>Average Output Latency Statistic.</returns>
        private static double GetOutputStreamStatistic_AverageLatency(object source, string arguments)
        {
            double statistic = 0.0D;
            PhasorDataConcentratorBase outputStream = source as PhasorDataConcentratorBase;

            if ((object)outputStream != null)
                statistic = outputStream.AverageLatency;

            return statistic;
        }

        /// <summary>
        /// Calculates number of clients connected to the command channel of the output stream during last reporting interval.
        /// </summary>
        /// <param name="source">Source OutputStream.</param>
        /// <param name="arguments">Any needed arguments for statistic calculation.</param>
        /// <returns>Output Stream Connected Statistic.</returns>
        private static double GetOutputStreamStatistic_ConnectedClientCount(object source, string arguments)
        {
            double statistic = 0.0D;
            PhasorDataConcentratorBase outputStream = source as PhasorDataConcentratorBase;

            if ((object)outputStream != null)
                statistic = outputStream.ConnectedClientCount;

            return statistic;
        }

        private static double GetOutputStreamStatistic_TotalBytesSent(object source, string arguments)
        {
            double statistic = 0.0D;
            PhasorDataConcentratorBase outputStream = source as PhasorDataConcentratorBase;

            if ((object)outputStream != null)
            {
                statistic = s_statisticValueCache.GetDifference(outputStream, outputStream.TotalBytesSent, "TotalBytesSent");

                if (statistic < 0.0D)
                    statistic = outputStream.TotalBytesSent;
            }

            return statistic;
        }

        private static double GetOutputStreamStatistic_LifetimeMeasurements(object source, string arguments)
        {
            double statistic = 0.0D;
            PhasorDataConcentratorBase outputStream = source as PhasorDataConcentratorBase;

            if ((object)outputStream != null)
                statistic = outputStream.LifetimeMeasurements;

            return statistic;
        }

        private static double GetOutputStreamStatistic_MinimumMeasurementsPerSecond(object source, string arguments)
        {
            double statistic = 0.0D;
            PhasorDataConcentratorBase outputStream = source as PhasorDataConcentratorBase;

            if ((object)outputStream != null)
                statistic = outputStream.MinimumMeasurementsPerSecond;

            return statistic;
        }

        private static double GetOutputStreamStatistic_MaximumMeasurementsPerSecond(object source, string arguments)
        {
            double statistic = 0.0D;
            PhasorDataConcentratorBase outputStream = source as PhasorDataConcentratorBase;

            if ((object)outputStream != null)
                statistic = outputStream.MaximumMeasurementsPerSecond;

            return statistic;
        }

        private static double GetOutputStreamStatistic_AverageMeasurementsPerSecond(object source, string arguments)
        {
            double statistic = 0.0D;
            PhasorDataConcentratorBase outputStream = source as PhasorDataConcentratorBase;

            if ((object)outputStream != null)
                statistic = outputStream.AverageMeasurementsPerSecond;

            return statistic;
        }

        private static double GetOutputStreamStatistic_LifetimeBytesSent(object source, string arguments)
        {
            double statistic = 0.0D;
            PhasorDataConcentratorBase outputStream = source as PhasorDataConcentratorBase;

            if ((object)outputStream != null)
                statistic = outputStream.TotalBytesSent;

            return statistic;
        }

        private static double GetOutputStreamStatistic_LifetimeMinimumLatency(object source, string arguments)
        {
            double statistic = 0.0D;
            PhasorDataConcentratorBase outputStream = source as PhasorDataConcentratorBase;

            if ((object)outputStream != null)
                statistic = outputStream.LifetimeMinimumLatency;

            return statistic;
        }

        private static double GetOutputStreamStatistic_LifetimeMaximumLatency(object source, string arguments)
        {
            double statistic = 0.0D;
            PhasorDataConcentratorBase outputStream = source as PhasorDataConcentratorBase;

            if ((object)outputStream != null)
                statistic = outputStream.LifetimeMaximumLatency;

            return statistic;
        }

        private static double GetOutputStreamStatistic_LifetimeAverageLatency(object source, string arguments)
        {
            double statistic = 0.0D;
            PhasorDataConcentratorBase outputStream = source as PhasorDataConcentratorBase;

            if ((object)outputStream != null)
                statistic = outputStream.LifetimeAverageLatency;

            return statistic;
        }

        private static double GetOutputStreamStatistic_LifetimeDiscardedMeasurements(object source, string arguments)
        {
            double statistic = 0.0D;
            PhasorDataConcentratorBase outputStream = source as PhasorDataConcentratorBase;

            if ((object)outputStream != null)
                statistic = outputStream.DiscardedMeasurements;

            return statistic;
        }

        private static double GetOutputStreamStatistic_LifetimeDownsampledMeasurements(object source, string arguments)
        {
            double statistic = 0.0D;
            PhasorDataConcentratorBase outputStream = source as PhasorDataConcentratorBase;

            if ((object)outputStream != null)
                statistic = outputStream.DownsampledMeasurements;

            return statistic;
        }

        #endregion

        #endregion
    }
}
