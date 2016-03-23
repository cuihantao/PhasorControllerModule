﻿//******************************************************************************************************
//  LocalOutputAdapter.cs - Gbtc
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
//  09/10/2009 - Pinal C. Patel
//       Generated original version of source code.
//  09/11/2009 - Pinal C. Patel
//       Added support to refresh metadata from one or more external sources.
//  09/15/2009 - Stephen C. Wills
//       Added new header and license agreement.
//  09/17/2009 - Pinal C. Patel
//       Added option to refresh metadata during connection.
//       Modified RefreshMetadata() to perform synchronous refresh.
//       Corrected the implementation of Dispose().
//  09/18/2009 - Pinal C. Patel
//       Added override to Status property and added event handler to archive rollover notification.
//  10/28/2009 - Pinal C. Patel
//       Modified to allow for multiple instances of the adapter to be loaded and configured with 
//       different settings by persisting the settings in the config file under unique categories.
//  11/18/2009 - Pinal C. Patel
//       Added support for the replication of local historian archive.
//  12/01/2009 - Pinal C. Patel
//       Modified Initialize() to load all available metadata providers.
//  12/11/2009 - Pinal C. Patel
//       Fixed the implementation for allowing multiple adapter instances.
//       Expanded the adapter status to include dynamically loaded plug-ins.
//  04/28/2010 - Pinal C. Patel
//       Modified ProcessMeasurements() method to not throw an exception if the archive file is not 
//       open as this will be handled by ArchiveFile.WriteData() method if necessary.
//  06/13/2010 - J. Ritchie Carroll
//       Modified loaded plug-in's to use lower-cased instance name for configuration settings for
//       consistency and better looking configuration categories. Added static data operation to 
//       automatically optimize settings for defined local historians.
//  09/24/2010 - J. Ritchie Carroll
//       Added provider and service section to list of category sections to be removed when unused. 
//       Added automatic URL namespace reservation for built-in web services.
//  11/07/2010 - Pinal C. Patel
//       Modified namespace reservation logic to handle the changed URI format in SelfHostingService.
//  11/21/2011 - J. Ritchie Carroll
//       Modified historian optimization procedure to dynamically add reading adapters.
//  12/13/2012 - Starlynn Danyelle Gilliam
//       Modified Header.
//
//******************************************************************************************************

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using GSF;
using GSF.Collections;
using GSF.Configuration;
using GSF.Data;
using GSF.Historian;
using GSF.Historian.DataServices;
using GSF.Historian.Files;
using GSF.Historian.MetadataProviders;
using GSF.Historian.Replication;
using GSF.IO;
using GSF.TimeSeries;
using GSF.TimeSeries.Adapters;

namespace HistorianAdapters
{
    /// <summary>
    /// Represents an output adapter that archives measurements to a local archive.
    /// </summary>
    [Description("Local Historian: archives measurements to a local in-process openHistorian.")]
    public class LocalOutputAdapter : OutputAdapterBase
    {
        #region [ Members ]

        // Fields
        private readonly ArchiveFile m_archive;
        private DataServices m_dataServices;
        private MetadataProviders m_metadataProviders;
        private ReplicationProviders m_replicationProviders;
        private bool m_autoRefreshMetadata;
        private bool m_attemptingConnection;
        private string m_instanceName;
        private string m_archivePath;
        private long m_archivedMeasurements;
        private volatile int m_adapterLoadedCount;
        private readonly Dictionary<int, ulong> m_outOfSequenceCounts;
        private readonly ProcessQueue<IDataPoint> m_outOfSequenceQueue;
        private bool m_disposed;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalOutputAdapter"/> class.
        /// </summary>
        public LocalOutputAdapter()
        {
            m_autoRefreshMetadata = true;
            m_archive = new ArchiveFile();
            m_archive.MetadataFile = new MetadataFile();
            m_archive.StateFile = new StateFile();
            m_archive.IntercomFile = new IntercomFile();
            MetadataRefreshOperation.IsBackground = false;
            m_outOfSequenceCounts = new Dictionary<int, ulong>();
            m_outOfSequenceQueue = ProcessQueue<IDataPoint>.CreateRealTimeQueue(HandleOutOfSequenceData);
        }

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets or sets instance name defined for this <see cref="LocalOutputAdapter"/>.
        /// </summary>
        [ConnectionStringParameter,
        Description("Define the instance name for the archive. Leave this value blank to default to the adapter name (blank is typical setting)."),
        DefaultValue("")]
        public string InstanceName
        {
            get
            {
                return m_instanceName;
            }
            set
            {
                m_instanceName = value;
            }
        }

        /// <summary>
        /// Gets or sets a boolean indicating whether or not metadata is
        /// refreshed when the adapter attempts to connect to the archive.
        /// </summary>
        [ConnectionStringParameter,
        Description("Define a boolean indicating whether to refresh metadata from database on connect."),
        DefaultValue(true)]
        public bool AutoRefreshMetadata
        {
            get
            {
                return m_autoRefreshMetadata;
            }
            set
            {
                m_autoRefreshMetadata = value;
            }
        }

        /// <summary>
        /// Gets or sets primary keys of input measurements the <see cref="AdapterBase"/> expects, if any.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override MeasurementKey[] InputMeasurementKeys
        {
            get
            {
                return base.InputMeasurementKeys;
            }
            set
            {
                base.InputMeasurementKeys = value;
            }
        }

        /// <summary>
        /// Gets or sets the path to the archive.
        /// </summary>
        public string ArchivePath
        {
            get
            {
                return m_archivePath;
            }
            set
            {
                m_archivePath = value;
            }
        }

        /// <summary>
        /// Returns a flag that determines if measurements sent to this <see cref="LocalOutputAdapter"/> are destined for archival.
        /// </summary>
        public override bool OutputIsForArchive
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets flag that determines if this <see cref="LocalOutputAdapter"/> uses an asynchronous connection.
        /// </summary>
        protected override bool UseAsyncConnect
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Returns the detailed status of the data output source.
        /// </summary>
        public override string Status
        {
            get
            {
                StringBuilder status = new StringBuilder();

                status.Append(base.Status);
                status.AppendLine();
                status.Append(m_archive.Status);
                status.AppendLine();
                status.Append(m_archive.MetadataFile.Status);
                status.AppendLine();
                status.Append(m_archive.StateFile.Status);
                status.AppendLine();
                status.Append(m_archive.IntercomFile.Status);
                status.AppendLine();
                status.Append(m_dataServices.Status);
                status.AppendLine();
                status.Append(m_metadataProviders.Status);
                status.AppendLine();
                status.Append(m_replicationProviders.Status);

                return status.ToString();
            }
        }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Refreshes metadata using all available and enabled providers.
        /// </summary>
        protected override void ExecuteMetadataRefresh()
        {
            try
            {
                if ((object)m_archive != null && m_archive.IsOpen && (object)m_archive.StateFile != null && m_archive.StateFile.IsOpen && (object)m_archive.MetadataFile != null && m_archive.MetadataFile.IsOpen)
                {
                    if (!m_attemptingConnection)
                    {
                        OnStatusMessage("Pausing measurement processing...");
                        InternalProcessQueue.Stop();
                    }

                    // Synchronously refresh the meta-base.
                    lock (m_metadataProviders.Adapters)
                    {
                        foreach (IMetadataProvider provider in m_metadataProviders.Adapters)
                        {
                            if (provider.Enabled)
                                provider.Refresh();
                        }
                    }

                    // Request a state file synchronization in case file watchers are disabled
                    m_archive.SynchronizeStateFile();

                    // Wait for the meta-base to synchronize, up to five seconds
                    int waitCounts = 0;

                    while (m_archive.StateFile.RecordsOnDisk != m_archive.MetadataFile.RecordsOnDisk && waitCounts < 50)
                    {
                        Thread.Sleep(100);
                        waitCounts++;
                    }
                }
            }
            finally
            {
                if (Enabled && !InternalProcessQueue.Enabled)
                {
                    if (m_attemptingConnection)
                    {
                        // OnConnected starts the internal process queue
                        // and sets m_attemptingConnection to false
                        OnConnected();
                    }
                    else
                    {
                        OnStatusMessage("Resuming measurement processing...");
                        InternalProcessQueue.Start();
                    }
                }
            }
        }

        /// <summary>
        /// Initializes this <see cref="LocalOutputAdapter"/>.
        /// </summary>
        /// <exception cref="ArgumentException"><b>InstanceName</b> is missing from the <see cref="AdapterBase.Settings"/>.</exception>
        public override void Initialize()
        {
            base.Initialize();

            Dictionary<string, string> settings = Settings;
            string refreshMetadata;

            // Validate settings.
            if (!settings.TryGetValue("instanceName", out m_instanceName) || string.IsNullOrWhiteSpace(m_instanceName))
                m_instanceName = Name.ToLower();

            if (!settings.TryGetValue("archivePath", out m_archivePath))
                m_archivePath = FilePath.GetAbsolutePath(FilePath.AddPathSuffix("Archive"));

            if (settings.TryGetValue("refreshMetadata", out refreshMetadata) || settings.TryGetValue("autoRefreshMetadata", out refreshMetadata))
                m_autoRefreshMetadata = refreshMetadata.ParseBoolean();

            //if (settings.TryGetValue("useNamespaceReservation", out setting))
            //    m_useNamespaceReservation = setting.ParseBoolean();
            //else
            //    m_useNamespaceReservation = false;

            // Initialize metadata file.
            m_instanceName = m_instanceName.ToLower();
            m_archive.MetadataFile.FileName = Path.Combine(m_archivePath, m_instanceName + "_dbase.dat");
            m_archive.MetadataFile.PersistSettings = true;
            m_archive.MetadataFile.SettingsCategory = m_instanceName + m_archive.MetadataFile.SettingsCategory;
            m_archive.MetadataFile.FileAccessMode = FileAccess.ReadWrite;
            m_archive.MetadataFile.Initialize();

            // Initialize state file.
            m_archive.StateFile.FileName = Path.Combine(m_archivePath, m_instanceName + "_startup.dat");
            m_archive.StateFile.PersistSettings = true;
            m_archive.StateFile.SettingsCategory = m_instanceName + m_archive.StateFile.SettingsCategory;
            m_archive.StateFile.FileAccessMode = FileAccess.ReadWrite;
            m_archive.StateFile.Initialize();

            // Initialize intercom file.
            m_archive.IntercomFile.FileName = Path.Combine(m_archivePath, "scratch.dat");
            m_archive.IntercomFile.PersistSettings = true;
            m_archive.IntercomFile.SettingsCategory = m_instanceName + m_archive.IntercomFile.SettingsCategory;
            m_archive.IntercomFile.FileAccessMode = FileAccess.ReadWrite;
            m_archive.IntercomFile.Initialize();

            // Initialize data archive file.           
            m_archive.FileName = Path.Combine(m_archivePath, m_instanceName + "_archive.d");
            m_archive.FileSize = 100;
            m_archive.CompressData = false;
            m_archive.PersistSettings = true;
            m_archive.SettingsCategory = m_instanceName + m_archive.SettingsCategory;

            m_archive.RolloverStart += m_archive_RolloverStart;
            m_archive.RolloverComplete += m_archive_RolloverComplete;
            m_archive.RolloverException += m_archive_RolloverException;

            m_archive.DataReadException += m_archive_DataReadException;
            m_archive.DataWriteException += m_archive_DataWriteException;

            m_archive.OffloadStart += m_archive_OffloadStart;
            m_archive.OffloadComplete += m_archive_OffloadComplete;
            m_archive.OffloadException += m_archive_OffloadException;

            m_archive.FutureDataReceived += m_archive_FutureDataReceived;
            m_archive.OutOfSequenceDataReceived += m_archive_OutOfSequenceDataReceived;

            m_archive.Initialize();

            // Provide web service support.
            m_dataServices = new DataServices();
            m_dataServices.AdapterCreated += DataServices_AdapterCreated;
            m_dataServices.AdapterLoaded += DataServices_AdapterLoaded;
            m_dataServices.AdapterUnloaded += DataServices_AdapterUnloaded;
            m_dataServices.AdapterLoadException += AdapterLoader_AdapterLoadException;

            // Provide metadata sync support.
            m_metadataProviders = new MetadataProviders();
            m_metadataProviders.AdapterCreated += MetadataProviders_AdapterCreated;
            m_metadataProviders.AdapterLoaded += MetadataProviders_AdapterLoaded;
            m_metadataProviders.AdapterUnloaded += MetadataProviders_AdapterUnloaded;
            m_metadataProviders.AdapterLoadException += AdapterLoader_AdapterLoadException;

            // Provide archive replication support.
            m_replicationProviders = new ReplicationProviders();
            m_replicationProviders.AdapterCreated += ReplicationProviders_AdapterCreated;
            m_replicationProviders.AdapterLoaded += ReplicationProviders_AdapterLoaded;
            m_replicationProviders.AdapterUnloaded += ReplicationProviders_AdapterUnloaded;
            m_replicationProviders.AdapterLoadException += AdapterLoader_AdapterLoadException;
        }

        /// <summary>
        /// Gets a short one-line status of this <see cref="LocalOutputAdapter"/>.
        /// </summary>
        /// <param name="maxLength">Maximum length of the status message.</param>
        /// <returns>Text of the status message.</returns>
        public override string GetShortStatus(int maxLength)
        {
            return string.Format("Archived {0} measurements locally.", m_archivedMeasurements).CenterText(maxLength);
        }

        /// <summary>
        /// Releases the unmanaged resources used by this <see cref="LocalOutputAdapter"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (!m_disposed)
            {
                try
                {
                    // This will be done regardless of whether the object is finalized or disposed.
                    if (disposing)
                    {
                        // This will be done only when the object is disposed by calling Dispose().
                        if ((object)m_dataServices != null)
                        {
                            m_dataServices.AdapterCreated -= DataServices_AdapterCreated;
                            m_dataServices.AdapterLoaded -= DataServices_AdapterLoaded;
                            m_dataServices.AdapterUnloaded -= DataServices_AdapterUnloaded;
                            m_dataServices.AdapterLoadException -= AdapterLoader_AdapterLoadException;
                            m_dataServices.Dispose();
                        }

                        if ((object)m_metadataProviders != null)
                        {
                            m_metadataProviders.AdapterCreated -= MetadataProviders_AdapterCreated;
                            m_metadataProviders.AdapterLoaded -= MetadataProviders_AdapterLoaded;
                            m_metadataProviders.AdapterUnloaded -= MetadataProviders_AdapterUnloaded;
                            m_metadataProviders.AdapterLoadException -= AdapterLoader_AdapterLoadException;
                            m_metadataProviders.Dispose();
                        }

                        if ((object)m_replicationProviders != null)
                        {
                            m_replicationProviders.AdapterCreated -= ReplicationProviders_AdapterCreated;
                            m_replicationProviders.AdapterLoaded -= ReplicationProviders_AdapterLoaded;
                            m_replicationProviders.AdapterUnloaded -= ReplicationProviders_AdapterUnloaded;
                            m_replicationProviders.AdapterLoadException -= AdapterLoader_AdapterLoadException;
                            m_replicationProviders.Dispose();
                        }

                        if ((object)m_archive != null)
                        {
                            m_archive.RolloverStart -= m_archive_RolloverStart;
                            m_archive.RolloverComplete -= m_archive_RolloverComplete;
                            m_archive.RolloverException -= m_archive_RolloverException;

                            m_archive.DataReadException -= m_archive_DataReadException;
                            m_archive.DataWriteException -= m_archive_DataWriteException;

                            m_archive.OffloadStart -= m_archive_OffloadStart;
                            m_archive.OffloadComplete -= m_archive_OffloadComplete;
                            m_archive.OffloadException -= m_archive_OffloadException;

                            m_archive.FutureDataReceived -= m_archive_FutureDataReceived;
                            m_archive.OutOfSequenceDataReceived -= m_archive_OutOfSequenceDataReceived;

                            m_archive.Dispose();

                            if ((object)m_archive.MetadataFile != null)
                            {
                                m_archive.MetadataFile.Dispose();
                                m_archive.MetadataFile = null;
                            }

                            if ((object)m_archive.StateFile != null)
                            {
                                m_archive.StateFile.Dispose();
                                m_archive.StateFile = null;
                            }

                            if ((object)m_archive.IntercomFile != null)
                            {
                                m_archive.IntercomFile.Dispose();
                                m_archive.IntercomFile = null;
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
        /// Called when data output source connection is established.
        /// </summary>
        protected override void OnConnected()
        {
            m_attemptingConnection = false;
            base.OnConnected();
        }

        /// <summary>
        /// Attempts to connect to this <see cref="LocalOutputAdapter"/>.
        /// </summary>
        protected override void AttemptConnection()
        {
            m_attemptingConnection = true;

            // Open archive files
            m_archive.MetadataFile.Open();
            m_archive.StateFile.Open();
            m_archive.IntercomFile.Open();
            m_archive.Open();

            m_adapterLoadedCount = 0;

            // Initialization of services needs to occur after files are open
            m_dataServices.Initialize();
            m_metadataProviders.Initialize();
            m_replicationProviders.Initialize();

            int waitCount = 0;

            // Wait for adapter initialization to complete, up to 2 seconds
            while (waitCount < 20 && m_adapterLoadedCount != m_dataServices.Adapters.Count + m_metadataProviders.Adapters.Count + m_replicationProviders.Adapters.Count)
            {
                Thread.Sleep(100);
                waitCount++;
            }

            // Kick off a meta-data refresh...
            if (m_autoRefreshMetadata)
                RefreshMetadata();
            else
                OnConnected();
        }

        /// <summary>
        /// Attempts to disconnect from this <see cref="LocalOutputAdapter"/>.
        /// </summary>
        protected override void AttemptDisconnection()
        {
            m_attemptingConnection = false;

            if ((object)m_archive != null)
            {
                if (m_archive.IsOpen)
                {
                    m_archive.Save();
                    m_archive.Close();
                }

                if ((object)m_archive.MetadataFile != null && m_archive.MetadataFile.IsOpen)
                {
                    m_archive.MetadataFile.Save();
                    m_archive.MetadataFile.Close();
                }

                if ((object)m_archive.StateFile != null && m_archive.StateFile.IsOpen)
                {
                    m_archive.StateFile.Save();
                    m_archive.StateFile.Close();
                }

                if ((object)m_archive.IntercomFile != null && m_archive.IntercomFile.IsOpen)
                {
                    m_archive.IntercomFile.Save();
                    m_archive.IntercomFile.Close();
                }

                OnDisconnected();
                m_archivedMeasurements = 0;
            }
        }

        /// <summary>
        /// Archives <paramref name="measurements"/> locally.
        /// </summary>
        /// <param name="measurements">Measurements to be archived.</param>
        /// <exception cref="InvalidOperationException">Local archive is closed.</exception>
        protected override void ProcessMeasurements(IMeasurement[] measurements)
        {
            foreach (IMeasurement measurement in measurements)
            {
                if (WriteData(measurement))
                    m_archivedMeasurements++;
            }
        }

        private bool WriteData(IMeasurement measurement)
        {
            try
            {
                m_archive.WriteData(new ArchiveDataPoint(measurement));
                return true;
            }
            catch (Exception ex)
            {
                OnProcessException(ex);
            }

            return false;
        }

        private void m_archive_RolloverStart(object sender, EventArgs e)
        {
            OnStatusMessage("Archive is being rolled over...");
        }

        private void m_archive_RolloverComplete(object sender, EventArgs e)
        {
            OnStatusMessage("Archive rollover is complete.");
        }

        private void m_archive_RolloverException(object sender, EventArgs<Exception> e)
        {
            OnProcessException(new InvalidOperationException(string.Format("Archive rollover failed: {0}", e.Argument.Message), e.Argument));
        }

        private void m_archive_DataReadException(object sender, EventArgs<Exception> e)
        {
            OnProcessException(new InvalidOperationException(string.Format("Archive data read exception: {0}", e.Argument.Message), e.Argument));
        }

        private void m_archive_DataWriteException(object sender, EventArgs<Exception> e)
        {
            OnProcessException(new InvalidOperationException(string.Format("Archive write read exception: {0}", e.Argument.Message), e.Argument));
        }

        private void m_archive_OffloadStart(object sender, EventArgs e)
        {
            OnStatusMessage("Archive offload started...");
        }

        private void m_archive_OffloadComplete(object sender, EventArgs e)
        {
            OnStatusMessage("Archive offload complete.");
        }

        private void m_archive_OffloadException(object sender, EventArgs<Exception> e)
        {
            OnProcessException(new InvalidOperationException(string.Format("Archive offload exception: {0}", e.Argument.Message), e.Argument));
        }

        private void m_archive_FutureDataReceived(object sender, EventArgs<IDataPoint> e)
        {
            OnStatusMessage("Received data for point {0}:{1} with an unreasonable future timestamp. Data with a timestamp beyond {2:0.00} minutes of the local clock will not be archived. Check local system clock and data source clock for accuracy.", m_instanceName, e.Argument.HistorianID, m_archive.LeadTimeTolerance);
        }

        private void m_archive_OutOfSequenceDataReceived(object sender, EventArgs<IDataPoint> e)
        {
            // In case we are receiving many out-of-sequence points, we throttle user messages
            if ((object)m_outOfSequenceQueue != null)
                m_outOfSequenceQueue.Add(e.Argument);
        }

        private void HandleOutOfSequenceData(IDataPoint point)
        {
            if ((object)point == null)
                return;

            int id = point.HistorianID;
            ulong total = m_outOfSequenceCounts.GetOrAdd(id, 0UL);

            if (total % 100UL == 0UL)
            {
                if (total > 0UL)
                    OnStatusMessage("Received {0} points of out-of-sequence data for {1}:{2}. Data received out of order will not be archived, per configuration.", total, m_instanceName, id);
                else
                    OnStatusMessage("Received out-of-sequence data for {0}:{1}. Data received out of order will not be archived, per configuration.", m_instanceName, id);
            }

            m_outOfSequenceCounts[id] = total + 1UL;
        }

        private void DataServices_AdapterCreated(object sender, EventArgs<IDataService> e)
        {
            e.Argument.SettingsCategory = InstanceName + e.Argument.SettingsCategory;
        }

        private void DataServices_AdapterLoaded(object sender, EventArgs<IDataService> e)
        {
            e.Argument.Archive = m_archive;
            e.Argument.ServiceProcessException += DataServices_ServiceProcessException;
            OnStatusMessage("{0} has been loaded.", e.Argument.GetType().Name);

            m_adapterLoadedCount++;
        }

        private void DataServices_AdapterUnloaded(object sender, EventArgs<IDataService> e)
        {
            e.Argument.Archive = null;
            e.Argument.ServiceProcessException -= DataServices_ServiceProcessException;
            OnStatusMessage("{0} has been unloaded.", e.Argument.GetType().Name);
        }

        private void MetadataProviders_AdapterCreated(object sender, EventArgs<IMetadataProvider> e)
        {
            e.Argument.SettingsCategory = InstanceName + e.Argument.SettingsCategory;

            if (e.Argument.GetType() == typeof(AdoMetadataProvider))
            {
                // Populate the default configuration for AdoMetadataProvider.
                AdoMetadataProvider provider = e.Argument as AdoMetadataProvider;

                if ((object)provider != null)
                {
                    provider.Enabled = true;
                    provider.SelectString = string.Format("SELECT * FROM HistorianMetadata WHERE PlantCode='{0}'", Name);
                }

                // The following connection information is now provided via configuration Eval mappings
                //    provider.DataProviderString = config.Settings["SystemSettings"]["DataProviderString"].Value;

                //    ConfigurationFile config = ConfigurationFile.Current;
                //    string connectionString = config.Settings["SystemSettings"]["ConnectionString"].Value;
                //    Dictionary<string, string> settings = connectionString.ParseKeyValuePairs();
                //    string setting;

                //    if (settings.TryGetValue("Provider", out setting))
                //    {
                //        // Check if provider is for Access
                //        if (setting.StartsWith("Microsoft.Jet.OLEDB", StringComparison.OrdinalIgnoreCase))
                //        {
                //            // Make sure path to Access database is fully qualified
                //            if (settings.TryGetValue("Data Source", out setting))
                //            {
                //                settings["Data Source"] = FilePath.GetAbsolutePath(setting);
                //                connectionString = settings.JoinKeyValuePairs();
                //            }
                //        }
                //    }

                //    provider.ConnectionString = connectionString;
            }
        }

        private void MetadataProviders_AdapterLoaded(object sender, EventArgs<IMetadataProvider> e)
        {
            e.Argument.Metadata = m_archive.MetadataFile;
            e.Argument.MetadataRefreshStart += MetadataProviders_MetadataRefreshStart;
            e.Argument.MetadataRefreshComplete += MetadataProviders_MetadataRefreshComplete;
            e.Argument.MetadataRefreshTimeout += MetadataProviders_MetadataRefreshTimeout;
            e.Argument.MetadataRefreshException += MetadataProviders_MetadataRefreshException;
            OnStatusMessage("{0} has been loaded.", e.Argument.GetType().Name);

            m_adapterLoadedCount++;
        }

        private void MetadataProviders_AdapterUnloaded(object sender, EventArgs<IMetadataProvider> e)
        {
            e.Argument.Metadata = null;
            e.Argument.MetadataRefreshStart -= MetadataProviders_MetadataRefreshStart;
            e.Argument.MetadataRefreshComplete -= MetadataProviders_MetadataRefreshComplete;
            e.Argument.MetadataRefreshTimeout -= MetadataProviders_MetadataRefreshTimeout;
            e.Argument.MetadataRefreshException -= MetadataProviders_MetadataRefreshException;
            OnStatusMessage("{0} has been unloaded.", e.Argument.GetType().Name);
        }

        private void ReplicationProviders_AdapterCreated(object sender, EventArgs<IReplicationProvider> e)
        {
            e.Argument.SettingsCategory = InstanceName + e.Argument.SettingsCategory;
        }

        private void ReplicationProviders_AdapterLoaded(object sender, EventArgs<IReplicationProvider> e)
        {
            e.Argument.ReplicationStart += ReplicationProvider_ReplicationStart;
            e.Argument.ReplicationComplete += ReplicationProvider_ReplicationComplete;
            e.Argument.ReplicationProgress += ReplicationProvider_ReplicationProgress;
            e.Argument.ReplicationException += ReplicationProvider_ReplicationException;
            OnStatusMessage("{0} has been loaded.", e.Argument.GetType().Name);

            m_adapterLoadedCount++;
        }

        private void ReplicationProviders_AdapterUnloaded(object sender, EventArgs<IReplicationProvider> e)
        {
            e.Argument.ReplicationStart -= ReplicationProvider_ReplicationStart;
            e.Argument.ReplicationComplete -= ReplicationProvider_ReplicationComplete;
            e.Argument.ReplicationProgress -= ReplicationProvider_ReplicationProgress;
            e.Argument.ReplicationException -= ReplicationProvider_ReplicationException;
            OnStatusMessage("{0} has been unloaded.", e.Argument.GetType().Name);
        }

        private void AdapterLoader_AdapterLoadException(object sender, EventArgs<Exception> e)
        {
            OnProcessException(e.Argument);
        }

        private void DataServices_ServiceProcessException(object sender, EventArgs<Exception> e)
        {
            OnProcessException(e.Argument);
        }

        private void MetadataProviders_MetadataRefreshStart(object sender, EventArgs e)
        {
            OnStatusMessage("{0} has started metadata refresh...", sender.GetType().Name);
        }

        private void MetadataProviders_MetadataRefreshComplete(object sender, EventArgs e)
        {
            OnStatusMessage("{0} has finished metadata refresh.", sender.GetType().Name);
        }

        private void MetadataProviders_MetadataRefreshTimeout(object sender, EventArgs e)
        {
            OnStatusMessage("{0} has timed-out on metadata refresh.", sender.GetType().Name);
        }

        private void MetadataProviders_MetadataRefreshException(object sender, EventArgs<Exception> e)
        {
            OnProcessException(e.Argument);
        }

        private void ReplicationProvider_ReplicationStart(object sender, EventArgs e)
        {
            OnStatusMessage("{0} has started archive replication...", sender.GetType().Name);
        }

        private void ReplicationProvider_ReplicationComplete(object sender, EventArgs e)
        {
            OnStatusMessage("{0} has finished archive replication.", sender.GetType().Name);
        }

        private void ReplicationProvider_ReplicationProgress(object sender, EventArgs<ProcessProgress<int>> e)
        {
            OnStatusMessage("{0} has replicated archive file {1}.", sender.GetType().Name, e.Argument.ProgressMessage);
        }

        private void ReplicationProvider_ReplicationException(object sender, EventArgs<Exception> e)
        {
            OnProcessException(e.Argument);
        }

        #endregion

        #region [ Static ]

        // Static Methods

        // Apply historian configuration optimizations at start-up
        // ReSharper disable UnusedMember.Local
        // ReSharper disable UnusedParameter.Local
        private static void OptimizeLocalHistorianSettings(AdoDataConnection database, string nodeIDQueryString, ulong trackingVersion, string arguments, Action<string> statusMessage, Action<Exception> processException)
        {
            // Make sure setting exists to allow user to by-pass local historian optimizations at startup
            ConfigurationFile configFile = ConfigurationFile.Current;
            CategorizedSettingsElementCollection settings = configFile.Settings["systemSettings"];
            settings.Add("OptimizeLocalHistorianSettings", true, "Determines if the defined local historians will have their settings optimized at startup");

            // See if this node should optimize local historian settings
            if (settings["OptimizeLocalHistorianSettings"].ValueAsBoolean())
            {
                statusMessage("Optimizing settings for local historians...");

                // Load the defined local system historians
                IEnumerable<DataRow> historians = database.Connection.RetrieveData(database.AdapterType, string.Format("SELECT AdapterName FROM RuntimeHistorian WHERE NodeID = {0} AND TypeName = 'HistorianAdapters.LocalOutputAdapter'", nodeIDQueryString)).AsEnumerable();
                IEnumerable<DataRow> readers = database.Connection.RetrieveData(database.AdapterType, string.Format("SELECT * FROM CustomInputAdapter WHERE NodeID = {0} AND TypeName = 'HistorianAdapters.LocalInputAdapter'", nodeIDQueryString)).AsEnumerable();

                // Also check for local historian adapters loaded into CustomOutputAdapters
                historians = historians.Concat(database.Connection.RetrieveData(database.AdapterType, string.Format("SELECT AdapterName, ConnectionString FROM RuntimeCustomOutputAdapter WHERE NodeID = {0} AND TypeName = 'HistorianAdapters.LocalOutputAdapter'", nodeIDQueryString)).AsEnumerable());

                List<string> validHistorians = new List<string>();
                string name, acronym, currentPath, archivePath, fileName, defaultFileName, instanceName;

                // Get current execution path
                currentPath = FilePath.AddPathSuffix(FilePath.GetAbsolutePath(""));

                // Make sure archive path exists to hold historian files
                archivePath = FilePath.GetAbsolutePath(FilePath.AddPathSuffix("Archive"));

                if (!Directory.Exists(archivePath))
                    Directory.CreateDirectory(archivePath);

                // Apply settings optimizations to local historians
                foreach (DataRow row in historians)
                {
                    acronym = row.Field<string>("AdapterName").ToLower();
                    name = string.Format("local \'{0}\' historian", acronym);
                    validHistorians.Add(acronym);

                    // We handle the statistics historian as a special case
                    if (acronym == "stat")
                    {
                        // File size of the statistics historian is tuned based on the number of statistics being archived (two data blocks per statistic per archive file).
                        // This number is rounded up to the nearest MB to prevent rounding errors when serializing the settings to the configuration file.
                        // The data block size is set to its minimum of 1 KB which works better for archives with a low sample rate and a large number of signals.
                        const int DataBlocksPerSignal = 2;
                        int statisticsCount = Convert.ToInt32(database.Connection.ExecuteScalar("SELECT COUNT(*) FROM ActiveMeasurement WHERE SignalType = 'STAT'"));
                        int fileSize = (int)Math.Max(1.0D, Math.Ceiling(statisticsCount * DataBlocksPerSignal / 1024.0D));

                        // Set rollover preparation such that the historian will not be creating a standby file and rolling over simulataneously.
                        // Common.Mid is used to ensure that the setting is between 1 and 95, which are the upper and lower bounds of the rollover preparation threshold.
                        // The actual number of data blocks per signal is calculated because it may be different since the file size was rounded up to the nearest MB.
                        double actualDataBlocksPerSignal = fileSize * 1024.0D / statisticsCount;
                        int rolloverPreparationThreshold = Common.Mid(95, 100 - (int)(100 / actualDataBlocksPerSignal) - 1, 1);

                        settings = configFile.Settings["statArchiveFile"];
                        settings.Add("FileSize", fileSize, "Size (in MB) of the file.");
                        settings.Add("DataBlockSize", 1, "Size (in KB) of the data blocks in the file.");
                        settings.Add("RolloverPreparationThreshold", rolloverPreparationThreshold, "Percentage file full when the rollover preparation should begin.");
                        settings["FileSize"].Update(fileSize);
                        settings["DataBlockSize"].Update(1);
                        settings["RolloverPreparationThreshold"].Update(rolloverPreparationThreshold);
                    }
                    else
                    {
                        // Make sure needed historian configuration settings are properly defined
                        settings = configFile.Settings[string.Format("{0}MetadataFile", acronym)];
                        settings.Add("LoadOnOpen", true, string.Format("True if file records are to be loaded in memory when opened; otherwise False - this defaults to True for the {0} meta-data file.", name));
                        settings.Add("ReloadOnModify", false, string.Format("True if file records loaded in memory are to be re-loaded when file is modified on disk; otherwise False - this defaults to True for the {0} meta-data file.", name));
                        settings["LoadOnOpen"].Update(true);
                        settings["ReloadOnModify"].Update(false);

                        // Older versions placed the archive data files in the same folder as the executables, for both better organization
                        // and performance related to file monitoring, these files are now located in their own folder
                        defaultFileName = string.Format("{0}{1}{2}_dbase.dat", archivePath, Path.DirectorySeparatorChar, acronym);
                        settings.Add("FileName", defaultFileName, string.Format("Name of the {0} meta-data file including its path.", acronym));
                        fileName = settings["FileName"].Value;

                        if (string.Compare(FilePath.GetDirectoryName(fileName), currentPath, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            if (!File.Exists(defaultFileName))
                                File.Move(fileName, defaultFileName);
                            settings["FileName"].Update(defaultFileName);
                        }

                        settings = configFile.Settings[string.Format("{0}StateFile", acronym)];
                        settings.Add("AutoSaveInterval", 10000, string.Format("Interval in milliseconds at which the file records loaded in memory are to be saved automatically to disk. Use -1 to disable automatic saving - this defaults to 10,000 for the {0} state file.", name));
                        settings.Add("LoadOnOpen", true, string.Format("True if file records are to be loaded in memory when opened; otherwise False - this defaults to True for the {0} state file.", name));
                        settings.Add("SaveOnClose", true, string.Format("True if file records loaded in memory are to be saved to disk when file is closed; otherwise False - this defaults to True for the {0} state file.", name));
                        settings.Add("ReloadOnModify", false, string.Format("True if file records loaded in memory are to be re-loaded when file is modified on disk; otherwise False - this defaults to False for the {0} state file.", name));
                        settings["AutoSaveInterval"].Update(10000);
                        settings["LoadOnOpen"].Update(true);
                        settings["SaveOnClose"].Update(true);
                        settings["ReloadOnModify"].Update(false);

                        defaultFileName = string.Format("{0}{1}{2}_startup.dat", archivePath, Path.DirectorySeparatorChar, acronym);
                        settings.Add("FileName", defaultFileName, string.Format("Name of the {0} state file including its path.", acronym));
                        fileName = settings["FileName"].Value;

                        if (string.Compare(FilePath.GetDirectoryName(fileName), currentPath, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            if (!File.Exists(defaultFileName))
                                File.Move(fileName, defaultFileName);
                            settings["FileName"].Update(defaultFileName);
                        }

                        settings = configFile.Settings[string.Format("{0}IntercomFile", acronym)];
                        settings.Add("AutoSaveInterval", 1000, string.Format("Interval in milliseconds at which the file records loaded in memory are to be saved automatically to disk. Use -1 to disable automatic saving - this defaults to 1,000 for the {0} intercom file.", name));
                        settings.Add("LoadOnOpen", true, string.Format("True if file records are to be loaded in memory when opened; otherwise False - this defaults to True for the {0} intercom file.", name));
                        settings.Add("SaveOnClose", true, string.Format("True if file records loaded in memory are to be saved to disk when file is closed; otherwise False - this defaults to True for the {0} intercom file.", name));
                        settings.Add("ReloadOnModify", false, string.Format("True if file records loaded in memory are to be re-loaded when file is modified on disk; otherwise False - this defaults to False for the {0} intercom file.", name));
                        settings["AutoSaveInterval"].Update(1000);
                        settings["LoadOnOpen"].Update(true);
                        settings["SaveOnClose"].Update(true);
                        settings["ReloadOnModify"].Update(false);

                        defaultFileName = string.Format("{0}{1}scratch.dat", archivePath, Path.DirectorySeparatorChar);
                        settings.Add("FileName", defaultFileName, string.Format("Name of the {0} intercom file including its path.", acronym));
                        fileName = settings["FileName"].Value;

                        if (string.Compare(FilePath.GetDirectoryName(fileName), currentPath, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            if (!File.Exists(defaultFileName))
                                File.Move(fileName, defaultFileName);

                            settings["FileName"].Update(defaultFileName);
                        }

                        settings = configFile.Settings[string.Format("{0}ArchiveFile", acronym)];
                        settings.Add("CacheWrites", true, string.Format("True if writes are to be cached for performance; otherwise False - this defaults to True for the {0} working archive file.", name));
                        settings.Add("ConserveMemory", false, string.Format("True if attempts are to be made to conserve memory; otherwise False - this defaults to False for the {0} working archive file.", name));
                        settings["CacheWrites"].Update(true);
                        settings["ConserveMemory"].Update(false);

                        defaultFileName = string.Format("{0}{1}{2}_archive.d", archivePath, Path.DirectorySeparatorChar, acronym);
                        settings.Add("FileName", defaultFileName, string.Format("Name of the {0} working archive file including its path.", acronym));
                        fileName = settings["FileName"].Value;

                        if (string.Compare(FilePath.GetDirectoryName(fileName), currentPath, StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            if (!File.Exists(defaultFileName))
                                File.Move(fileName, defaultFileName);

                            settings["FileName"].Update(defaultFileName);
                        }

                        // Move any historical files in executable folder to the archive folder...
                        string[] archiveFileNames = FilePath.GetFileList(string.Format("{0}{1}{2}_archive*.d", FilePath.GetAbsolutePath(""), Path.DirectorySeparatorChar, acronym));

                        if ((object)archiveFileNames != null && archiveFileNames.Length > 0)
                        {
                            statusMessage("Relocating existing historical data to \"Archive\" folder...");

                            foreach (string archiveFileName in archiveFileNames)
                            {
                                defaultFileName = string.Format("{0}{1}{2}", archivePath, Path.DirectorySeparatorChar, FilePath.GetFileName(archiveFileName));

                                if (!File.Exists(defaultFileName))
                                    File.Move(archiveFileName, defaultFileName);
                            }
                        }

                        // Lookup matching reader record (i.e., LocalInputAdapter with instance name of the current historian)
                        DataRow match;

                        try
                        {
                            match = readers.FirstOrDefault(inputRow =>
                            {
                                if (inputRow["ConnectionString"].ToNonNullString().ParseKeyValuePairs().TryGetValue("instanceName", out instanceName))
                                    return string.Compare(instanceName, acronym, StringComparison.OrdinalIgnoreCase) == 0;

                                return false;
                            });
                        }
                        catch
                        {
                            match = null;
                        }

                        // If no match was found, add record for associated historical data reader
                        if ((object)match == null)
                        {
                            try
                            {
                                instanceName = acronym.ToUpper().Trim();
                                settings = configFile.Settings[string.Format("{0}ArchiveFile", acronym)];
                                string archiveLocation = FilePath.GetDirectoryName(settings["FileName"].Value);
                                string adapterName = string.Format("{0}READER", instanceName);
                                string connectionString = string.Format("archiveLocation={0}; instanceName={1}; sourceIDs={1}; publicationInterval=333333; connectOnDemand=true", archiveLocation, instanceName);
                                string query = string.Format("INSERT INTO CustomInputAdapter(NodeID, AdapterName, AssemblyName, TypeName, ConnectionString, LoadOrder, Enabled) " +
                                    "VALUES({0}, @adapterName, 'HistorianAdapters.dll', 'HistorianAdapters.LocalInputAdapter', @connectionString, 0, 1)", nodeIDQueryString);

                                if (database.IsOracle)
                                    query = query.Replace('@', ':');

                                database.Connection.ExecuteNonQuery(query, adapterName, connectionString);
                            }
                            catch (Exception ex)
                            {
                                processException(new InvalidOperationException("Failed to add associated historian reader input adapter for local historian: " + ex.Message, ex));
                            }
                        }
                    }
                }

                // Sort valid historians for binary search
                validHistorians.Sort();

                // Create a list to track categories to remove
                HashSet<string> categoriesToRemove = new HashSet<string>();

                // Search for unused settings categories
                foreach (PropertyInformation info in configFile.Settings.ElementInformation.Properties)
                {
                    name = info.Name;

                    if (name.EndsWith("MetadataFile") && validHistorians.BinarySearch(name.Substring(0, name.IndexOf("MetadataFile", StringComparison.OrdinalIgnoreCase))) < 0)
                        categoriesToRemove.Add(name);

                    if (name.EndsWith("StateFile") && validHistorians.BinarySearch(name.Substring(0, name.IndexOf("StateFile", StringComparison.OrdinalIgnoreCase))) < 0)
                        categoriesToRemove.Add(name);

                    if (name.EndsWith("IntercomFile") && validHistorians.BinarySearch(name.Substring(0, name.IndexOf("IntercomFile", StringComparison.OrdinalIgnoreCase))) < 0)
                        categoriesToRemove.Add(name);

                    if (name.EndsWith("ArchiveFile") && validHistorians.BinarySearch(name.Substring(0, name.IndexOf("ArchiveFile", StringComparison.OrdinalIgnoreCase))) < 0)
                        categoriesToRemove.Add(name);

                    if (name.EndsWith("AdoMetadataProvider") && validHistorians.BinarySearch(name.Substring(0, name.IndexOf("AdoMetadataProvider", StringComparison.OrdinalIgnoreCase))) < 0)
                        categoriesToRemove.Add(name);

                    if (name.EndsWith("OleDbMetadataProvider") && validHistorians.BinarySearch(name.Substring(0, name.IndexOf("OleDbMetadataProvider", StringComparison.OrdinalIgnoreCase))) < 0)
                        categoriesToRemove.Add(name);

                    if (name.EndsWith("RestWebServiceMetadataProvider") && validHistorians.BinarySearch(name.Substring(0, name.IndexOf("RestWebServiceMetadataProvider", StringComparison.OrdinalIgnoreCase))) < 0)
                        categoriesToRemove.Add(name);

                    if (name.EndsWith("MetadataService") && validHistorians.BinarySearch(name.Substring(0, name.IndexOf("MetadataService", StringComparison.OrdinalIgnoreCase))) < 0)
                        categoriesToRemove.Add(name);

                    if (name.EndsWith("TimeSeriesDataService") && validHistorians.BinarySearch(name.Substring(0, name.IndexOf("TimeSeriesDataService", StringComparison.OrdinalIgnoreCase))) < 0)
                        categoriesToRemove.Add(name);

                    if (name.EndsWith("HadoopReplicationProvider") && validHistorians.BinarySearch(name.Substring(0, name.IndexOf("HadoopReplicationProvider", StringComparison.OrdinalIgnoreCase))) < 0)
                        categoriesToRemove.Add(name);
                }

                if (categoriesToRemove.Count > 0)
                {
                    statusMessage("Removing unused local historian configuration settings...");

                    // Remove any unused settings categories
                    foreach (string category in categoriesToRemove)
                    {
                        configFile.Settings.Remove(category);
                    }
                }

                // Save any applied changes
                configFile.Save();

                // Associate temporal data readers with any added OSI-PI historian archive adapters
                AddPIHistorianReaders(database, nodeIDQueryString, processException);
            }
        }

        private static void AddPIHistorianReaders(AdoDataConnection database, string nodeIDQueryString, Action<Exception> processException)
        {
            // Load the defined local PI historians
            IEnumerable<DataRow> historians = database.Connection.RetrieveData(database.AdapterType, string.Format("SELECT AdapterName, ConnectionString FROM RuntimeHistorian WHERE NodeID = {0} AND TypeName = 'PIAdapters.PIOutputAdapter'", nodeIDQueryString)).AsEnumerable();
            IEnumerable<DataRow> readers = database.Connection.RetrieveData(database.AdapterType, string.Format("SELECT * FROM CustomInputAdapter WHERE NodeID = {0} AND TypeName = 'PIAdapters.PIPBInputAdapter'", nodeIDQueryString)).AsEnumerable();

            // Also check for PI adapters loaded into CustomOutputAdapters
            historians = historians.Concat(database.Connection.RetrieveData(database.AdapterType, string.Format("SELECT AdapterName, ConnectionString FROM RuntimeCustomOutputAdapter WHERE NodeID = {0} AND TypeName = 'PIAdapters.PIOutputAdapter'", nodeIDQueryString)).AsEnumerable());

            // Make sure a temporal reader is defined for each OSI-PI historian
            foreach (DataRow row in historians)
            {
                string acronym = row.Field<string>("AdapterName").ToLower();

                // Lookup matching reader record (i.e., PIPBInputAdapter with instance name of the current PI historian)
                DataRow match;

                try
                {
                    match = readers.FirstOrDefault(inputRow =>
                    {
                        string sourceIDs;

                        if (inputRow["ConnectionString"].ToNonNullString().ParseKeyValuePairs().TryGetValue("sourceIDs", out sourceIDs))
                            return string.Compare(sourceIDs, acronym, StringComparison.OrdinalIgnoreCase) == 0;

                        return false;
                    });
                }
                catch
                {
                    match = null;
                }

                // If no match was found, add record for associated historical data reader
                if ((object)match == null)
                {
                    try
                    {
                        Dictionary<string, string> settings = row.Field<string>("ConnectionString").ToNonNullString().ParseKeyValuePairs();
                        string instanceName = acronym.ToUpper().Trim();
                        string adapterName = string.Format("{0}READER", instanceName);
                        string serverName, userName, password, setting;
                        int connectTimeout;

                        if (!settings.TryGetValue("ServerName", out serverName))
                            throw new InvalidOperationException("Server name is a required setting for PI connections. Please add a server in the format servername=myservername to the connection string.");

                        if (settings.TryGetValue("UserName", out setting))
                            userName = setting;
                        else
                            userName = null;

                        if (settings.TryGetValue("Password", out setting))
                            password = setting;
                        else
                            password = null;

                        if (!settings.TryGetValue("ConnectTimeout", out setting) || !int.TryParse(setting, out connectTimeout))
                            connectTimeout = 30000;

                        string connectionString;

                        if (string.IsNullOrEmpty(userName))
                            connectionString = string.Format("ServerName={0}; ConnectTimeout={1}; sourceIDs={2}; connectOnDemand=true", serverName, connectTimeout, instanceName);
                        else
                            connectionString = string.Format("ServerName={0}; UserName={1}; Password={2}; ConnectTimeout={3}; sourceIDs={4}; connectOnDemand=true", serverName, userName, password.ToNonNullString(), connectTimeout, instanceName);

                        string query = string.Format("INSERT INTO CustomInputAdapter(NodeID, AdapterName, AssemblyName, TypeName, ConnectionString, LoadOrder, Enabled) " +
                            "VALUES({0}, @adapterName, 'PIAdapters.dll', 'PIAdapters.PIPBInputAdapter', @connectionString, 0, 1)", nodeIDQueryString);

                        if (database.IsOracle)
                            query = query.Replace('@', ':');

                        database.Connection.ExecuteNonQuery(query, adapterName, connectionString);
                    }
                    catch (Exception ex)
                    {
                        processException(new InvalidOperationException("Failed to add associated OSI-PI historian temporal data reader input adapter for local OSI-PI historian: " + ex.Message, ex));
                    }
                }
            }
        }

        //// Create an http namespace reservation
        //private static void AddNamespaceReservation(Uri serviceUri)
        //{
        //    OperatingSystem OS = Environment.OSVersion;
        //    ProcessStartInfo psi = null;
        //    string parameters = null;

        //    if (OS.Platform == PlatformID.Win32NT)
        //    {
        //        if (OS.Version.Major > 5)
        //        {
        //            // Vista, Windows 2008, Window 7, etc use "netsh" for reservations
        //            string everyoneUser = new SecurityIdentifier("S-1-1-0").Translate(typeof(NTAccount)).ToString();
        //            parameters = string.Format(@"http add urlacl url={0}://+:{1}{2} user=\{3}", serviceUri.Scheme, serviceUri.Port, serviceUri.AbsolutePath, everyoneUser);
        //            psi = new ProcessStartInfo("netsh", parameters);
        //        }
        //        else
        //        {
        //            // Attempt to use "httpcfg" for older Windows versions...
        //            parameters = string.Format(@"set urlacl /u {0}://*:{1}{2}/ /a D:(A;;GX;;;S-1-1-0)", serviceUri.Scheme, serviceUri.Port, serviceUri.AbsolutePath);
        //            psi = new ProcessStartInfo("httpcfg", parameters);
        //        }
        //    }

        //    if (psi != null)
        //    {
        //        psi.Verb = "runas";
        //        psi.CreateNoWindow = true;
        //        psi.WindowStyle = ProcessWindowStyle.Hidden;
        //        psi.UseShellExecute = false;
        //        psi.Arguments = parameters;

        //        using (Process shell = new Process())
        //        {
        //            shell.StartInfo = psi;
        //            shell.Start();
        //            if (!shell.WaitForExit(5000))
        //                shell.Kill();
        //        }
        //    }
        //}

        //private static void RemoveNamespaceReservation(Uri serviceUri)
        //{
        //    OperatingSystem OS = Environment.OSVersion;
        //    ProcessStartInfo psi = null;
        //    string parameters = null;

        //    if (OS.Platform == PlatformID.Win32NT)
        //    {
        //        if (OS.Version.Major > 5)
        //        {
        //            // Vista, Windows 2008, Window 7, etc use "netsh" for reservations
        //            parameters = string.Format(@"http delete urlacl url={0}://+:{1}{2}", serviceUri.Scheme, serviceUri.Port, serviceUri.AbsolutePath);
        //            psi = new ProcessStartInfo("netsh", parameters);
        //        }
        //        else
        //        {
        //            // Attempt to use "httpcfg" for older Windows versions...
        //            parameters = string.Format(@"delete urlacl /u {0}://*:{1}{2}/", serviceUri.Scheme, serviceUri.Port, serviceUri.AbsolutePath);
        //            psi = new ProcessStartInfo("httpcfg", parameters);
        //        }
        //    }

        //    if (psi != null)
        //    {
        //        psi.Verb = "runas";
        //        psi.CreateNoWindow = true;
        //        psi.WindowStyle = ProcessWindowStyle.Hidden;
        //        psi.UseShellExecute = false;
        //        psi.Arguments = parameters;

        //        using (Process shell = new Process())
        //        {
        //            shell.StartInfo = psi;
        //            shell.Start();
        //            if (!shell.WaitForExit(5000))
        //                shell.Kill();
        //        }
        //    }
        //}

        #endregion
    }
}
