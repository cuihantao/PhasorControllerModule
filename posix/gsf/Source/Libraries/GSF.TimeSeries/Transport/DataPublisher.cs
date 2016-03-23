﻿//******************************************************************************************************
//  DataPublisher.cs - Gbtc
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
//  08/20/2010 - J. Ritchie Carroll
//       Generated original version of source code.
//  11/15/2010 - Mehulbhai P Thakker
//       Fixed issue when DataSubscriber tries to resubscribe by setting subscriber. Initialized
//       manually in ReceiveClientDataComplete event handler.
//  12/02/2010 - J. Ritchie Carroll
//       Fixed an issue for when DataSubcriber dynamically resubscribes with a different
//       synchronization method (e.g., going from unsynchronized to synchronized)
//  05/26/2011 - J. Ritchie Carroll
//       Implemented subscriber authentication model.
//  02/07/2012 - Mehulbhai Thakkar
//       Modified m_metadataTables to include filter expression and made the list ";" separated.
//  12/20/2012 - Starlynn Danyelle Gilliam
//       Modified Header.
//
//******************************************************************************************************

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Timers;
using System.Xml;
using GSF.Communication;
using GSF.Configuration;
using GSF.Data;
using GSF.IO;
using GSF.Net.Security;
using GSF.Security.Cryptography;
using GSF.TimeSeries.Adapters;
using GSF.TimeSeries.Statistics;
using GSF.Units;
using Timer = System.Timers.Timer;

namespace GSF.TimeSeries.Transport
{
    #region [ Enumerations ]

    /// <summary>
    /// Security modes used by the <see cref="DataPublisher"/> to secure data sent over the command channel.
    /// </summary>
    public enum SecurityMode
    {
        /// <summary>
        /// No security.
        /// </summary>
        None,

        /// <summary>
        /// Transport Layer Security.
        /// </summary>
        TLS,

        /// <summary>
        /// Pre-shared key.
        /// </summary>
        Gateway
    }

    /// <summary>
    /// Server commands received by <see cref="DataPublisher"/> and sent by <see cref="DataSubscriber"/>.
    /// </summary>
    /// <remarks>
    /// Solicited server commands will receive a <see cref="ServerResponse.Succeeded"/> or <see cref="ServerResponse.Failed"/>
    /// response code along with an associated success or failure message. Message type for successful responses will be based
    /// on server command - for example, server response for a successful MetaDataRefresh command will return a serialized
    /// <see cref="DataSet"/> of the available server metadata. Message type for failed responses will always be a string of
    /// text representing the error message.
    /// </remarks>
    public enum ServerCommand : byte
    {
        /// <summary>
        /// Authenticate command.
        /// </summary>
        /// <remarks>
        /// Requests that server authenticate client with the following encrypted authentication packet.
        /// Successful return message type will be string indicating client ID was validated.
        /// Client should not attempt further steps if authentication fails, user will need to take action.
        /// Client should send MetaDataRefresh and Subscribe commands upon successful authentication.
        /// If server is setup to not require authentication, authentication step can be skipped - however
        /// it will be up to the implementation to know if authentication is required - the server cannot be
        /// queried to determine if authentication is required.
        /// It is expected that use of this protocol between gateways over a shared network (i.e., the lower
        /// security zone) will require authentication.
        /// It is expected that use of this protocol between gateway and other systems (e.g., a PDC) in a
        /// private network (i.e., the higher security zone) can be setup to not require authentication.
        /// </remarks>
        Authenticate = 0x00,
        /// <summary>
        /// Meta data refresh command.
        /// </summary>
        /// <remarks>
        /// Requests that server send an updated set of metadata so client can refresh its point list.
        /// Successful return message type will be a <see cref="DataSet"/> containing server device and measurement metadata.
        /// Received device list should be defined as children of the "parent" server device connection similar to the way
        /// PMUs are defined as children of a parent PDC device connection.
        /// Devices and measurements contain unique Guids that should be used to key metadata updates in local repository.
        /// Optional string based message can follow command that should represent client requested meta-data filtering
        /// expressions, e.g.: "FILTER MeasurementDetail WHERE SignalType &lt;&gt; 'STAT'"
        /// </remarks>
        MetaDataRefresh = 0x01,
        /// <summary>
        /// Subscribe command.
        /// </summary>
        /// <remarks>
        /// Requests a subscription of streaming data from server based on connection string that follows.
        /// It will not be necessary to stop an existing subscription before requesting a new one.
        /// Successful return message type will be string indicating total number of allowed points.
        /// Client should wait for UpdateSignalIndexCache and UpdateBaseTime response codes before attempting
        /// to parse data when using the compact measurement format.
        /// </remarks>
        Subscribe = 0x02,
        /// <summary>
        /// Unsubscribe command.
        /// </summary>
        /// <remarks>
        /// Requests that server stop sending streaming data to the client and cancel the current subscription.
        /// </remarks>
        Unsubscribe = 0x03,
        /// <summary>
        /// Rotate cipher keys.
        /// </summary>
        /// <remarks>
        /// Manually requests that server send a new set of cipher keys for data packet encryption.
        /// </remarks>
        RotateCipherKeys = 0x04,
        /// <summary>
        /// Update processing interval.
        /// </summary>
        /// <remarks>
        /// Manually requests server to update the processing interval with the following specified value.
        /// </remarks>
        UpdateProcessingInterval = 0x05,
        /// <summary>
        /// Define operational modes for subscriber connection.
        /// </summary>
        /// <remarks>
        /// As soon as connection is established, requests that server set operational
        /// modes that affect how the subscriber and publisher will communicate.
        /// </remarks>
        DefineOperationalModes = 0x06,
        /// <summary>
        /// Confirm receipt of a notification.
        /// </summary>
        /// <remarks>
        /// This message is sent in response to <see cref="ServerResponse.Notify"/>.
        /// </remarks>
        ConfirmNotification = 0x07,
        /// <summary>
        /// Confirm receipt of a buffer block measurement.
        /// </summary>
        /// <remarks>
        /// This message is sent in response to <see cref="ServerResponse.BufferBlock"/>.
        /// </remarks>
        ConfirmBufferBlock = 0x08
    }

    /// <summary>
    /// Server responses sent by <see cref="DataPublisher"/> and received by <see cref="DataSubscriber"/>.
    /// </summary>
    public enum ServerResponse : byte
    {
        // Although the server commands and responses will be on two different paths, the response enumeration values
        // are defined as distinct from the command values to make it easier to identify codes from a wire analysis.

        /// <summary>
        /// Command succeeded response.
        /// </summary>
        /// <remarks>
        /// Informs client that its solicited server command succeeded, original command and success message follow.
        /// </remarks>
        Succeeded = 0x80,
        /// <summary>
        /// Command failed response.
        /// </summary>
        /// <remarks>
        /// Informs client that its solicited server command failed, original command and failure message follow.
        /// </remarks>
        Failed = 0x81,
        /// <summary>
        /// Data packet response.
        /// </summary>
        /// <remarks>
        /// Unsolicited response informs client that a data packet follows.
        /// </remarks>
        DataPacket = 0x82,
        /// <summary>
        /// Update signal index cache response.
        /// </summary>
        /// <remarks>
        /// Unsolicited response requests that client update its runtime signal index cache with the one that follows.
        /// </remarks>
        UpdateSignalIndexCache = 0x83,
        /// <summary>
        /// Update runtime base-timestamp offsets response.
        /// </summary>
        /// <remarks>
        /// Unsolicited response requests that client update its runtime base-timestamp offsets with those that follow.
        /// </remarks>
        UpdateBaseTimes = 0x84,
        /// <summary>
        /// Update runtime cipher keys response.
        /// </summary>
        /// <remarks>
        /// Response, solicited or unsolicited, requests that client update its runtime data cipher keys with those that follow.
        /// </remarks>
        UpdateCipherKeys = 0x85,
        /// <summary>
        /// Data start time response packet.
        /// </summary>
        /// <remarks>
        /// Unsolicited response provides the start time of data being processed from the first measurement.
        /// </remarks>
        DataStartTime = 0x86,
        /// <summary>
        /// Processing complete notification.
        /// </summary>
        /// <remarks>
        /// Unsolicited response provides notification that input processing has completed, typically via temporal constraint.
        /// </remarks>
        ProcessingComplete = 0x87,
        /// <summary>
        /// Buffer block response.
        /// </summary>
        /// <remarks>
        /// Unsolicited response informs client that a raw buffer block follows.
        /// </remarks>
        BufferBlock = 0x88,
        /// <summary>
        /// Notify response.
        /// </summary>
        /// <remarks>
        /// Unsolicited response provides a notification message to the client.
        /// </remarks>
        Notify = 0x89,
        /// <summary>
        /// Configuration changed response.
        /// </summary>
        /// <remarks>
        /// Unsolicited response provides a notification that the publisher's source configuration has changed and that
        /// client may want to request a meta-data refresh.
        /// </remarks>
        ConfigurationChanged = 0x8A,
        /// <summary>
        /// No operation keep-alive ping.
        /// </summary>
        /// <remarks>
        /// The command channel can remain quiet for some time, this command allows a period test of client connectivity.
        /// </remarks>
        NoOP = 0xFF
    }

    /// <summary>
    /// <see cref="DataPublisher"/> data packet flags.
    /// </summary>
    [Flags]
    public enum DataPacketFlags : byte
    {
        /// <summary>
        /// Determines if data packet is synchronized.
        /// </summary>
        /// <remarks>
        /// Bit set = synchronized, bit clear = unsynchronized.
        /// </remarks>
        Synchronized = (byte)Bits.Bit00,
        /// <summary>
        /// Determines if serialized measurement is compact.
        /// </summary>
        /// <remarks>
        /// Bit set = compact, bit clear = full fidelity.
        /// </remarks>
        Compact = (byte)Bits.Bit01,
        /// <summary>
        /// Determines which cipher index to use when encrypting data packet.
        /// </summary>
        /// <remarks>
        /// Bit set = use odd cipher index (i.e., 1), bit clear = use even cipher index (i.e., 0).
        /// </remarks>
        CipherIndex = (byte)Bits.Bit02,
        /// <summary>
        /// Determines if data packet payload is compressed.
        /// </summary>
        /// <remarks>
        /// Bit set = payload compressed, bit clear = payload normal.
        /// </remarks>
        Compressed = (byte)Bits.Bit03,
        /// <summary>
        /// Determines if the compressed data payload is in little-endian order.
        /// </summary>
        /// <remarks>
        /// Bit set = little-endian order compression, bit clear = big-endian order compression.
        /// </remarks>
        LittleEndianCompression = (byte)Bits.Bit04,
        /// <summary>
        /// No flags set.
        /// </summary>
        /// <remarks>
        /// This would represent unsynchronized, full fidelity measurement data packets.
        /// </remarks>
        NoFlags = (Byte)Bits.Nil
    }

    /// <summary>
    /// Operational modes that affect how <see cref="DataPublisher"/> and <see cref="DataSubscriber"/> communicate.
    /// </summary>
    /// <remarks>
    /// Operational modes are sent from a subscriber to a publisher to request operational behaviors for the
    /// connection, as a result the operation modes must be sent before any other command. The publisher may
    /// silently refuse some requests (e.g., compression) based on its configuration. Operational modes only
    /// apply to fundamental protocol control.
    /// </remarks>
    [Flags]
    public enum OperationalModes : uint
    {
        /// <summary>
        /// Mask to get version number of protocol.
        /// </summary>
        /// <remarks>
        /// Version number is currently set to 0.
        /// </remarks>
        VersionMask = (uint)(Bits.Bit04 | Bits.Bit03 | Bits.Bit02 | Bits.Bit01 | Bits.Bit00),
        /// <summary>
        /// Mask to get mode of compression.
        /// </summary>
        /// <remarks>
        /// GZip compression is only mode currently supported. Remaining bits are
        /// reserved for future compression modes.
        /// </remarks>
        CompressionModeMask = (uint)(Bits.Bit07 | Bits.Bit06 | Bits.Bit05),
        /// <summary>
        /// Mask to get character encoding used when exchanging messages between publisher and subscriber.
        /// </summary>
        /// <remarks>
        /// 00 = UTF-16, little endian<br/>
        /// 01 = UTF-16, big endian<br/>
        /// 10 = UTF-8<br/>
        /// 11 = ANSI
        /// </remarks>
        EncodingMask = (uint)(Bits.Bit09 | Bits.Bit08),
        /// <summary>
        /// Determines type of serialization to use when exchanging signal index cache and metadata.
        /// </summary>
        /// <remarks>
        /// Bit set = common serialization format, bit clear = .NET serialization format. The .NET
        /// serialization format is only for backwards compatibility with older GEP implementations
        /// and considered obsolete, it may be removed from future builds.
        /// </remarks>
        UseCommonSerializationFormat = (uint)Bits.Bit24,
        /// <summary>
        /// Determines whether external measurements are exchanged during metadata synchronization.
        /// </summary>
        /// <remarks>
        /// Bit set = external measurements are exchanged, bit clear = no external measurements are exchanged
        /// </remarks>
        ReceiveExternalMetadata = (uint)Bits.Bit25,
        /// <summary>
        /// Determines whether internal measurements are exchanged during metadata synchronization.
        /// </summary>
        /// <remarks>
        /// Bit set = internal measurements are exchanged, bit clear = no internal measurements are exchanged
        /// </remarks>
        ReceiveInternalMetadata = (uint)Bits.Bit26,
        /// <summary>
        /// Determines whether payload data is compressed when exchanging between publisher and subscriber.
        /// </summary>
        /// <remarks>
        /// Bit set = compress, bit clear = no compression
        /// </remarks>
        CompressPayloadData = (uint)Bits.Bit29,
        /// <summary>
        /// Determines whether the signal index cache is compressed when exchanging between publisher and subscriber.
        /// </summary>
        /// <remarks>
        /// Bit set = compress, bit clear = no compression
        /// </remarks>
        CompressSignalIndexCache = (uint)Bits.Bit30,
        /// <summary>
        /// Determines whether metadata is compressed when exchanging between publisher and subscriber.
        /// </summary>
        /// <remarks>
        /// Bit set = compress, bit clear = no compression
        /// </remarks>
        CompressMetadata = (uint)Bits.Bit31,
        /// <summary>
        /// No flags set.
        /// </summary>
        /// <remarks>
        /// This would represent protocol version 0,
        /// UTF-16 little endian character encoding,
        /// .NET serialization and no compression.
        /// </remarks>
        NoFlags = (uint)Bits.Nil
    }

    /// <summary>
    /// Enumeration for character encodings supported by the Gateway Exchange Protocol.
    /// </summary>
    public enum OperationalEncoding : uint
    {
        /// <summary>
        /// UTF-16, little endian
        /// </summary>
        Unicode = (uint)Bits.Nil,
        /// <summary>
        /// UTF-16, big endian
        /// </summary>
        BigEndianUnicode = (uint)Bits.Bit08,
        /// <summary>
        /// UTF-8
        /// </summary>
        UTF8 = (uint)Bits.Bit09,
        /// <summary>
        /// ANSI
        /// </summary>
        ANSI = (uint)(Bits.Bit09 | Bits.Bit08)
    }

    /// <summary>
    /// Enumeration for compression modes supported by the Gateway Exchange Protocol.
    /// </summary>
    public enum GatewayCompressionMode : uint
    {
        /// <summary>
        /// GZip compression
        /// </summary>
        GZip = (uint)Bits.Bit05,
        /// <summary>
        /// No compression
        /// </summary>
        None = (uint)Bits.Nil
    }

    #endregion

    /// <summary>
    /// Represents a data publishing server that allows multiple connections for data subscriptions.
    /// </summary>
    [Description("DataPublisher: server component that allows gateway-style subscription connections.")]
    [EditorBrowsable(EditorBrowsableState.Always)]
    public class DataPublisher : ActionAdapterCollection
    {
        #region [ Members ]

        // Nested Types

        private sealed class LatestMeasurementCache : FacileActionAdapterBase
        {
            public LatestMeasurementCache(string connectionString)
            {
                ConnectionString = connectionString;
            }

            public override DataSet DataSource
            {
                get
                {
                    return base.DataSource;
                }
                set
                {
                    base.DataSource = value;

                    if (Initialized)
                        UpdateInputMeasurementKeys();
                }
            }

            public override string Name
            {
                get
                {
                    return "LatestMeasurementCache";
                }
                set
                {
                    base.Name = value;
                }
            }

            public override bool SupportsTemporalProcessing
            {
                get
                {
                    return false;
                }
            }

            public override string GetShortStatus(int maxLength)
            {
                return "LatestMeasurementCache happily exists. :)";
            }

            private void UpdateInputMeasurementKeys()
            {
                string inputMeasurementKeys;

                if (Settings.TryGetValue("inputMeasurementKeys", out inputMeasurementKeys))
                    InputMeasurementKeys = ParseInputMeasurementKeys(DataSource, true, inputMeasurementKeys);
            }
        }

        // Events

        /// <summary>
        /// Indicates that a new client has connected to the publisher.
        /// </summary>
        /// <remarks>
        /// <see cref="EventArgs{T1, T2, T3}.Argument1"/> is the <see cref="Guid"/> based subscriber ID.<br/>
        /// <see cref="EventArgs{T1, T2, T3}.Argument2"/> is the connection identification (e.g., IP and DNS name, if available).<br/>
        /// <see cref="EventArgs{T1, T2, T3}.Argument3"/> is the subscriber information as reported by the client.
        /// </remarks>
        public event EventHandler<EventArgs<Guid, string, string>> ClientConnected;

        /// <summary>
        /// Indicates to the host that processing for an input adapter (via temporal session) has completed.
        /// </summary>
        /// <remarks>
        /// This event is expected to only be raised when an input adapter has been designed to process
        /// a finite amount of data, e.g., reading a historical range of data during temporal processing.
        /// </remarks>
        public event EventHandler ProcessingComplete;

        // Constants

        /// <summary>
        /// Default value for <see cref="RequireAuthentication"/>.
        /// </summary>
        public const bool DefaultRequireAuthentication = false;

        /// <summary>
        /// Default value for <see cref="SecurityMode"/>.
        /// </summary>
        public const SecurityMode DefaultSecurityMode = SecurityMode.None;

        /// <summary>
        /// Default value for <see cref="EncryptPayload"/>.
        /// </summary>
        public const bool DefaultEncryptPayload = false;

        /// <summary>
        /// Default value for <see cref="SharedDatabase"/>.
        /// </summary>
        public const bool DefaultSharedDatabase = false;

        /// <summary>
        /// Default value for <see cref="AllowPayloadCompression"/>.
        /// </summary>
        public const bool DefaultAllowPayloadCompression = true;

        /// <summary>
        /// Default value for <see cref="CompressionStrength"/>.
        /// </summary>
        public const int DefaultCompressionStrength = 31;

        /// <summary>
        /// Default value for <see cref="AllowSynchronizedSubscription"/>.
        /// </summary>
        public const bool DefaultAllowSynchronizedSubscription = true;

        /// <summary>
        /// Default value for <see cref="AllowMetadataRefresh"/>.
        /// </summary>
        public const bool DefaultAllowMetadataRefresh = true;

        /// <summary>
        /// Default value for <see cref="AllowNaNValueFilter"/>.
        /// </summary>
        public const bool DefaultAllowNaNValueFilter = true;

        /// <summary>
        /// Default value for <see cref="ForceNaNValueFilter"/>.
        /// </summary>
        public const bool DefaultForceNaNValueFilter = false;

        /// <summary>
        /// Default value for <see cref="UseBaseTimeOffsets"/>.
        /// </summary>
        public const bool DefaultUseBaseTimeOffsets = false;

        /// <summary>
        /// Default value for <see cref="CipherKeyRotationPeriod"/>.
        /// </summary>
        public const double DefaultCipherKeyRotationPeriod = 60000.0D;

        /// <summary>
        /// Default value for <see cref="MetadataTables"/>.
        /// </summary>
        public const string DefaultMetadataTables =
            "SELECT NodeID, UniqueID, OriginalSource, IsConcentrator, Acronym, Name, AccessID, ParentAcronym, ProtocolName, FramesPerSecond, CompanyAcronym, VendorAcronym, VendorDeviceName, Longitude, Latitude, InterconnectionName, ContactList, Enabled, UpdatedOn FROM DeviceDetail WHERE IsConcentrator = 0;" +
            "SELECT DeviceAcronym, ID, SignalID, PointTag, SignalReference, SignalAcronym, PhasorSourceIndex, Description, Internal, Enabled, UpdatedOn FROM MeasurementDetail;" +
            "SELECT DeviceAcronym, Label, Type, Phase, SourceIndex, UpdatedOn FROM PhasorDetail;" +
            "SELECT VersionNumber FROM SchemaVersion";

        /// <summary>
        /// Maximum packet size before software fragmentation of payload.
        /// </summary>
        public const int MaxPacketSize = ushort.MaxValue / 2;

        /// <summary>
        /// Size of client response header in bytes.
        /// </summary>
        /// <remarks>
        /// Header consists of response byte, in-response-to server command byte, 4-byte int representing payload length.
        /// </remarks>
        public const int ClientResponseHeaderSize = 6;

        // Length of random salt prefix
        internal const int CipherSaltLength = 8;

        // Fields
        private IServer m_commandChannel;
        private CertificatePolicyChecker m_certificateChecker;
        private Dictionary<X509Certificate, DataRow> m_subscriberIdentities;
        private ConcurrentDictionary<Guid, ClientConnection> m_clientConnections;
        private readonly ConcurrentDictionary<Guid, IServer> m_clientPublicationChannels;
        private readonly ConcurrentDictionary<MeasurementKey, Guid> m_signalIDCache;
        private readonly Dictionary<Guid, Dictionary<int, string>> m_clientNotifications;
        private readonly object m_clientNotificationsLock;
        private Timer m_commandChannelRestartTimer;
        private Timer m_cipherKeyRotationTimer;
        private RoutingTables m_routingTables;
        private readonly EventHandler<EventArgs<ICollection<IMeasurement>>> m_routedMeasurementsHandler;
        private string m_metadataTables;
        private string m_cacheMeasurementKeys;
        private SecurityMode m_securityMode;
        private bool m_encryptPayload;
        private bool m_sharedDatabase;
        private int m_compressionStrength;
        private bool m_allowPayloadCompression;
        private bool m_allowSynchronizedSubscription;
        private bool m_allowMetadataRefresh;
        private bool m_allowNaNValueFilter;
        private bool m_forceNaNValueFilter;
        private bool m_useBaseTimeOffsets;

        private long m_totalBytesSent;
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
        private long m_lifetimeLatencyMeasurements;
        private long m_bufferBlockRetransmissions;

        // For backwards compatibility.
        // If a client requests metadata, but fails to define the type of
        // metadata they would like to receive, we assume the client is
        // using an old version of the protocol. These flags will define
        // what type of metadata this type of client should actually receive.
        private OperationalModes m_forceReceiveMetadataFlags;

        private bool m_disposed;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Creates a new <see cref="DataPublisher"/>.
        /// </summary>
        public DataPublisher()
        {
            base.Name = "Data Publisher Collection";
            base.DataMember = "[internal]";

            m_clientConnections = new ConcurrentDictionary<Guid, ClientConnection>();
            m_clientPublicationChannels = new ConcurrentDictionary<Guid, IServer>();
            m_signalIDCache = new ConcurrentDictionary<MeasurementKey, Guid>();
            m_clientNotifications = new Dictionary<Guid, Dictionary<int, string>>();
            m_clientNotificationsLock = new object();
            m_securityMode = DefaultSecurityMode;
            m_encryptPayload = DefaultEncryptPayload;
            m_sharedDatabase = DefaultSharedDatabase;
            m_compressionStrength = DefaultCompressionStrength;
            m_allowPayloadCompression = DefaultAllowPayloadCompression;
            m_allowSynchronizedSubscription = DefaultAllowSynchronizedSubscription;
            m_allowMetadataRefresh = DefaultAllowMetadataRefresh;
            m_allowNaNValueFilter = DefaultAllowNaNValueFilter;
            m_forceNaNValueFilter = DefaultForceNaNValueFilter;
            m_useBaseTimeOffsets = DefaultUseBaseTimeOffsets;
            m_metadataTables = DefaultMetadataTables;
            m_forceReceiveMetadataFlags = OperationalModes.ReceiveInternalMetadata;

            m_routingTables = new RoutingTables();
            m_routingTables.ActionAdapters = this;
            m_routingTables.StatusMessage += m_routingTables_StatusMessage;
            m_routingTables.ProcessException += m_routingTables_ProcessException;
            m_routedMeasurementsHandler = m_routingTables.GetRoutedMeasurementsHandler();

            // Setup a timer for restarting the command channel if it fails
            m_commandChannelRestartTimer = new Timer(2000.0D);
            m_commandChannelRestartTimer.AutoReset = false;
            m_commandChannelRestartTimer.Enabled = false;
            m_commandChannelRestartTimer.Elapsed += m_commandChannelRestartTimer_Elapsed;

            // Setup a timer for rotating cipher keys
            m_cipherKeyRotationTimer = new Timer(DefaultCipherKeyRotationPeriod);
            m_cipherKeyRotationTimer.AutoReset = true;
            m_cipherKeyRotationTimer.Enabled = false;
            m_cipherKeyRotationTimer.Elapsed += m_cipherKeyRotationTimer_Elapsed;
        }

        /// <summary>
        /// Releases the unmanaged resources before the <see cref="DataPublisher"/> object is reclaimed by <see cref="GC"/>.
        /// </summary>
        ~DataPublisher()
        {
            Dispose(false);
        }

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets or sets flag that determines if <see cref="DataPublisher"/> should require subscribers to authenticate before making data requests.
        /// </summary>
        [ConnectionStringParameter,
        Description("Define the flag that determines if the publisher should require subscribers to authenticate before making data requests."),
        DefaultValue(DefaultRequireAuthentication)]
        public bool RequireAuthentication
        {
            get
            {
                return m_securityMode != SecurityMode.None;
            }
            set
            {
                m_securityMode = value ? SecurityMode.Gateway : SecurityMode.TLS;
            }
        }

        /// <summary>
        /// Gets or sets the security mode of the <see cref="DataPublisher"/>'s command channel.
        /// </summary>
        [ConnectionStringParameter,
        Description("Define the security mode used for communications over the command channel."),
        DefaultValue(DefaultSecurityMode)]
        public SecurityMode SecurityMode
        {
            get
            {
                return m_securityMode;
            }
            set
            {
                m_securityMode = value;
            }
        }

        /// <summary>
        /// Gets or sets flag that determines whether data sent over the data channel should be encrypted.
        /// </summary>
        /// <remarks>
        /// This value is only relevant if <see cref="RequireAuthentication"/> is true.
        /// </remarks>
        [ConnectionStringParameter,
        Description("Define the flag that determines whether data sent over the data channel should be encrypted. This value is only relevant when requireAuthentication is true."),
        DefaultValue(DefaultEncryptPayload)]
        public bool EncryptPayload
        {
            get
            {
                return m_encryptPayload;
            }
            set
            {
                m_encryptPayload = value;

                // Start cipher key rotation timer when encrypting payload
                if ((object)m_cipherKeyRotationTimer != null)
                    m_cipherKeyRotationTimer.Enabled = value;
            }
        }

        /// <summary>
        /// Gets or sets flag that indicates whether this publisher is publishing
        /// data that this node subscribed to from another node in a shared database.
        /// </summary>
        [ConnectionStringParameter,
        Description("Define the flag that indicates whether this publisher is publishing data that this node subscribed to from another node in a shared database."),
        DefaultValue(DefaultSharedDatabase)]
        public bool SharedDatabase
        {
            get
            {
                return m_sharedDatabase;
            }
            set
            {
                m_sharedDatabase = value;
            }
        }

        /// <summary>
        /// Gets or sets flag that indicates if this publisher will allow payload compression when requested by subscribers.
        /// </summary>
        [ConnectionStringParameter,
        Description("Define the flag that indicates if this publisher will allow payload compression when requested by subscribers."),
        DefaultValue(DefaultAllowPayloadCompression)]
        public bool AllowPayloadCompression
        {
            get
            {
                return m_allowPayloadCompression;
            }
            set
            {
                m_allowPayloadCompression = value;
            }
        }

        /// <summary>
        /// Gets or sets the compression strength value to use when compressing data for subscribers.
        /// </summary>
        [ConnectionStringParameter,
        Description("Define the compression strength value to use when compressing data for subscribers. Valid values are from 0 to 31: lower numbers provide faster compression times and higher numbers provide better compression ratios."),
        DefaultValue(DefaultCompressionStrength)]
        public int CompressionStrength
        {
            get
            {
                return m_compressionStrength;
            }
            set
            {
                if (value < 0)
                    value = 0;

                if (value > 31)
                    value = 31;

                m_compressionStrength = value;
            }
        }

        /// <summary>
        /// Gets or sets flag that indicates if this publisher will allow synchronized subscriptions when requested by subscribers.
        /// </summary>
        [ConnectionStringParameter,
        Description("Define the flag that indicates if this publisher will allow synchronized subscriptions when requested by subscribers."),
        DefaultValue(DefaultAllowSynchronizedSubscription)]
        public bool AllowSynchronizedSubscription
        {
            get
            {
                return m_allowSynchronizedSubscription;
            }
            set
            {
                m_allowSynchronizedSubscription = value;
            }
        }

        /// <summary>
        /// Gets or sets flag that indicates if this publisher will allow synchronized subscriptions when requested by subscribers.
        /// </summary>
        [ConnectionStringParameter,
        Description("Define the flag that indicates if this publisher will allow metadata refresh commands when requested by subscribers."),
        DefaultValue(DefaultAllowMetadataRefresh)]
        public bool AllowMetadataRefresh
        {
            get
            {
                return m_allowMetadataRefresh;
            }
            set
            {
                m_allowMetadataRefresh = value;
            }
        }

        /// <summary>
        /// Gets or sets flag that indicates if this publisher will allow filtering of data which is not a number.
        /// </summary>
        [ConnectionStringParameter,
        Description("Define the flag that indicates if this publisher will allow filtering of data which is not a number."),
        DefaultValue(DefaultAllowNaNValueFilter)]
        public bool AllowNaNValueFilter
        {
            get
            {
                return m_allowNaNValueFilter;
            }
            set
            {
                m_allowNaNValueFilter = value;
            }
        }

        /// <summary>
        /// Gets or sets flag that indicates if this publisher will force filtering of data which is not a number.
        /// </summary>
        [ConnectionStringParameter,
        Description("Define the flag that indicates if this publisher will force filtering of data which is not a number."),
        DefaultValue(DefaultForceNaNValueFilter)]
        public bool ForceNaNValueFilter
        {
            get
            {
                return m_forceNaNValueFilter;
            }
            set
            {
                m_forceNaNValueFilter = value;
            }
        }

        /// <summary>
        /// Gets or sets flag that determines whether to use base time offsets to decrease the size of compact measurements.
        /// </summary>
        [ConnectionStringParameter,
        Description("Define the flag that determines whether to use base time offsets to decrease the size of compact measurements."),
        DefaultValue(DefaultUseBaseTimeOffsets)]
        public bool UseBaseTimeOffsets
        {
            get
            {
                return m_useBaseTimeOffsets;
            }
            set
            {
                m_useBaseTimeOffsets = value;
            }
        }

        /// <summary>
        /// Gets or sets the cipher key rotation period.
        /// </summary>
        [ConnectionStringParameter,
        Description("Define the period, in milliseconds, over which new cipher keys will be provided to subscribers when EncryptPayload is true."),
        DefaultValue(DefaultCipherKeyRotationPeriod)]
        public double CipherKeyRotationPeriod
        {
            get
            {
                if ((object)m_cipherKeyRotationTimer != null)
                    return m_cipherKeyRotationTimer.Interval;

                return double.NaN;
            }
            set
            {
                if (value < 1000.0D)
                    throw new ArgumentOutOfRangeException("value", "Cipher key rotation period should not be set to less than 1000 milliseconds.");

                if ((object)m_cipherKeyRotationTimer != null)
                    m_cipherKeyRotationTimer.Interval = value;

                throw new ArgumentException("Cannot assign new cipher rotation period, timer is not defined.");
            }
        }

        /// <summary>
        /// Gets or sets the set of measurements which are cached by the data
        /// publisher to be published to subscribers immediately upon subscription.
        /// </summary>
        [ConnectionStringParameter,
        DefaultValue(""),
        Description("Defines the set of measurements to be cached and sent to subscribers immediately upon subscription."),
        CustomConfigurationEditor("GSF.TimeSeries.UI.WPF.dll", "GSF.TimeSeries.UI.Editors.MeasurementEditor")]
        public string CacheMeasurementKeys
        {
            get
            {
                return m_cacheMeasurementKeys;
            }
            set
            {
                m_cacheMeasurementKeys = value;
            }
        }

        /// <summary>
        /// Gets or sets <see cref="DataSet"/> based data source used to load each <see cref="IAdapter"/>.
        /// Updates to this property will cascade to all items in this <see cref="AdapterCollectionBase{T}"/>.
        /// </summary>
        public override DataSet DataSource
        {
            get
            {
                return base.DataSource;
            }
            set
            {
                if (DataSourceChanged(value))
                {
                    base.DataSource = value;

                    UpdateRights();
                    UpdateCertificateChecker();
                    UpdateClientNotifications();
                    UpdateLatestMeasurementCache();
                    NotifyClientsOfConfigurationChange();
                }
            }
        }

        /// <summary>
        /// Gets the status of this <see cref="DataPublisher"/>.
        /// </summary>
        /// <remarks>
        /// Derived classes should provide current status information about the adapter for display purposes.
        /// </remarks>
        public override string Status
        {
            get
            {
                StringBuilder status = new StringBuilder();

                if ((object)m_commandChannel != null)
                    status.Append(m_commandChannel.Status);

                status.Append(base.Status);
                status.AppendFormat("  Buffer block retransmits: {0}", m_bufferBlockRetransmissions);
                status.AppendLine();

                return status.ToString();
            }
        }

        /// <summary>
        /// Gets or sets the name of this <see cref="DataPublisher"/>.
        /// </summary>
        /// <remarks>
        /// The assigned name is used as the settings category when persisting the TCP server settings.
        /// </remarks>
        public override string Name
        {
            get
            {
                return base.Name;
            }
            set
            {
                IPersistSettings commandChannel = m_commandChannel as IPersistSettings;
                base.Name = value.ToUpper();

                if ((object)commandChannel != null)
                    commandChannel.SettingsCategory = value.Replace("!", "").ToLower();
            }
        }

        /// <summary>
        /// Gets or sets semi-colon separated list of SQL select statements used to create data for meta-data exchange.
        /// </summary>
        [ConnectionStringParameter]
        [Description("Semi-colon separated list of SQL select statements used to create data for meta-data exchange.")]
        [DefaultValue(DefaultMetadataTables)]
        public string MetadataTables
        {
            get
            {
                return m_metadataTables;
            }
            set
            {
                m_metadataTables = value;
            }
        }

        /// <summary>
        /// Gets flag that determines if <see cref="DataPublisher"/> subscriptions
        /// are automatically initialized when they are added to the collection.
        /// </summary>
        protected override bool AutoInitialize
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets dictionary of connected clients.
        /// </summary>
        internal protected ConcurrentDictionary<Guid, ClientConnection> ClientConnections
        {
            get
            {
                return m_clientConnections;
            }
        }

        /// <summary>
        /// Gets or sets reference to <see cref="TcpServer"/> command channel, attaching and/or detaching to events as needed.
        /// </summary>
        protected IServer CommandChannel
        {
            get
            {
                return m_commandChannel;
            }
            set
            {
                if ((object)m_commandChannel != null)
                {
                    // Detach from events on existing command channel reference
                    m_commandChannel.ClientConnected -= m_commandChannel_ClientConnected;
                    m_commandChannel.ClientDisconnected -= m_commandChannel_ClientDisconnected;
                    m_commandChannel.ClientConnectingException -= m_commandChannel_ClientConnectingException;
                    m_commandChannel.ReceiveClientDataComplete -= m_commandChannel_ReceiveClientDataComplete;
                    m_commandChannel.ReceiveClientDataException -= m_commandChannel_ReceiveClientDataException;
                    m_commandChannel.SendClientDataException -= m_commandChannel_SendClientDataException;
                    m_commandChannel.ServerStarted -= m_commandChannel_ServerStarted;
                    m_commandChannel.ServerStopped -= m_commandChannel_ServerStopped;

                    if (m_commandChannel != value)
                        m_commandChannel.Dispose();
                }

                // Assign new command channel reference
                m_commandChannel = value;

                if ((object)m_commandChannel != null)
                {
                    // Attach to desired events on new command channel reference
                    m_commandChannel.ClientConnected += m_commandChannel_ClientConnected;
                    m_commandChannel.ClientDisconnected += m_commandChannel_ClientDisconnected;
                    m_commandChannel.ClientConnectingException += m_commandChannel_ClientConnectingException;
                    m_commandChannel.ReceiveClientDataComplete += m_commandChannel_ReceiveClientDataComplete;
                    m_commandChannel.ReceiveClientDataException += m_commandChannel_ReceiveClientDataException;
                    m_commandChannel.SendClientDataException += m_commandChannel_SendClientDataException;
                    m_commandChannel.ServerStarted += m_commandChannel_ServerStarted;
                    m_commandChannel.ServerStopped += m_commandChannel_ServerStopped;
                }
            }
        }

        /// <summary>
        /// Gets flag indicating if publisher is connected and listening.
        /// </summary>
        public bool IsConnected
        {
            get
            {
                return m_commandChannel.Enabled;
            }
        }

        /// <summary>
        /// Gets the total number of buffer block retransmissions on all subscriptions over the lifetime of the publisher.
        /// </summary>
        public long BufferBlockRetransmissions
        {
            get
            {
                return m_bufferBlockRetransmissions;
            }
        }

        /// <summary>
        /// Gets the total number of bytes sent to clients of this data publisher.
        /// </summary>
        public long TotalBytesSent
        {
            get
            {
                return m_totalBytesSent;
            }
        }

        /// <summary>
        /// Gets the total number of measurements processed through this data publisher over the lifetime of the publisher.
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
        /// Gets the minimum latency calculated over the full lifetime of the publisher.
        /// </summary>
        public int LifetimeMinimumLatency
        {
            get
            {
                return (int)Ticks.ToMilliseconds(m_lifetimeMinimumLatency);
            }
        }

        /// <summary>
        /// Gets the maximum latency calculated over the full lifetime of the publisher.
        /// </summary>
        public int LifetimeMaximumLatency
        {
            get
            {
                return (int)Ticks.ToMilliseconds(m_lifetimeMaximumLatency);
            }
        }

        /// <summary>
        /// Gets the average latency calculated over the full lifetime of the publisher.
        /// </summary>
        public int LifetimeAverageLatency
        {
            get
            {
                if (m_lifetimeLatencyMeasurements == 0)
                    return -1;

                return (int)Ticks.ToMilliseconds(m_lifetimeTotalLatency / m_lifetimeLatencyMeasurements);
            }
        }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="DataPublisher"/> object and optionally releases the managed resources.
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
                        CommandChannel = null;

                        if ((object)m_clientConnections != null)
                            m_clientConnections.Values.AsParallel().ForAll(cc => cc.Dispose());

                        m_clientConnections = null;

                        if ((object)m_routingTables != null)
                        {
                            m_routingTables.StatusMessage -= m_routingTables_StatusMessage;
                            m_routingTables.ProcessException -= m_routingTables_ProcessException;
                            m_routingTables.Dispose();
                        }
                        m_routingTables = null;

                        // Dispose command channel restart timer
                        if ((object)m_commandChannelRestartTimer != null)
                        {
                            m_commandChannelRestartTimer.Elapsed -= m_commandChannelRestartTimer_Elapsed;
                            m_commandChannelRestartTimer.Dispose();
                        }
                        m_commandChannelRestartTimer = null;

                        // Dispose the cipher key rotation timer
                        if ((object)m_cipherKeyRotationTimer != null)
                        {
                            m_cipherKeyRotationTimer.Elapsed -= m_cipherKeyRotationTimer_Elapsed;
                            m_cipherKeyRotationTimer.Dispose();
                        }
                        m_cipherKeyRotationTimer = null;
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
        /// Initializes <see cref="DataPublisher"/>.
        /// </summary>
        public override void Initialize()
        {
            // We don't call base class initialize since it tries to auto-load adapters from the defined
            // data member - instead, the data publisher dynamically creates adapters upon request
            Initialized = false;

            Clear();

            Dictionary<string, string> settings = Settings;
            string setting;
            double period;
            int strength;

            // Setup data publishing server with or without required authentication 
            if (settings.TryGetValue("requireAuthentication", out setting))
                RequireAuthentication = setting.ParseBoolean();

            // Check flag that will determine if subscriber payloads should be encrypted by default
            if (settings.TryGetValue("encryptPayload", out setting))
                m_encryptPayload = setting.ParseBoolean();

            // Check flag that indicates whether publisher is publishing data
            // that its node subscribed to from another node in a shared database
            if (settings.TryGetValue("sharedDatabase", out setting))
                m_sharedDatabase = setting.ParseBoolean();

            // Extract custom metadata table expressions if provided
            if (settings.TryGetValue("metadataTables", out setting) && !string.IsNullOrWhiteSpace(setting))
                m_metadataTables = setting;

            // See if a user defined compression strength has been provided
            if (settings.TryGetValue("compressionStrength", out setting) && int.TryParse(setting, out strength))
                CompressionStrength = strength;

            // Check flag to see if payload compression is allowed
            if (settings.TryGetValue("allowPayloadCompression", out setting))
                m_allowPayloadCompression = setting.ParseBoolean();

            // Check flag to see if synchronized subscriptions are allowed
            if (settings.TryGetValue("allowSynchronizedSubscription", out setting))
                m_allowSynchronizedSubscription = setting.ParseBoolean();

            // Check flag to see if metadata refresh commands are allowed
            if (settings.TryGetValue("allowMetadataRefresh", out setting))
                m_allowMetadataRefresh = setting.ParseBoolean();

            // Check flag to see if NaN value filtering is allowed
            if (settings.TryGetValue("allowNaNValueFilter", out setting))
                m_allowNaNValueFilter = setting.ParseBoolean();

            // Check flag to see if NaN value filtering is forced
            if (settings.TryGetValue("forceNaNValueFilter", out setting))
                m_forceNaNValueFilter = setting.ParseBoolean();

            if (settings.TryGetValue("useBaseTimeOffsets", out setting))
                m_useBaseTimeOffsets = setting.ParseBoolean();

            // Get user specified period for cipher key rotation
            if (settings.TryGetValue("cipherKeyRotationPeriod", out setting) && double.TryParse(setting, out period))
                CipherKeyRotationPeriod = period;

            // Get security mode used for the command channel
            if (settings.TryGetValue("securityMode", out setting))
                m_securityMode = (SecurityMode)Enum.Parse(typeof(SecurityMode), setting);

            // Determine the type of metadata to force upon clients who do not specify
            if (settings.TryGetValue("forceReceiveMetadataFlags", out setting) && Enum.TryParse(setting, true, out m_forceReceiveMetadataFlags))
                m_forceReceiveMetadataFlags &= (OperationalModes.ReceiveInternalMetadata | OperationalModes.ReceiveExternalMetadata);

            if (settings.TryGetValue("cacheMeasurementKeys", out m_cacheMeasurementKeys))
            {
                // Create adapter for caching measurements that have a slower refresh interval
                LatestMeasurementCache cache = new LatestMeasurementCache(string.Format("trackLatestMeasurements=true;lagTime=60;leadTime=60;inputMeasurementKeys={{{0}}}", m_cacheMeasurementKeys));

                // Set up its data source first
                cache.DataSource = DataSource;

                // Add the cache as a child to the data publisher
                Add(cache);

                // AutoInitialize is false for the DataPublisher,
                // so we must initialize manually
                cache.Initialize();
                cache.Initialized = true;

                // Trigger routing table calculation to route cached measurements to the cache
                m_routingTables.CalculateRoutingTables(null);

                // Start tracking cached measurements
                cache.Start();
            }

            if (m_securityMode != SecurityMode.TLS)
            {
                // Create a new TCP server
                TcpServer commandChannel = new TcpServer();

                // Initialize default settings
                commandChannel.SettingsCategory = Name.Replace("!", "").ToLower();
                commandChannel.ConfigurationString = "port=6165";
                commandChannel.PayloadAware = true;
                commandChannel.PersistSettings = true;

                // Assign command channel client reference and attach to needed events
                CommandChannel = commandChannel;
            }
            else
            {
                // Create a new TLS server
                TlsServer commandChannel = new TlsServer();

                // Create certificate checker
                m_certificateChecker = new CertificatePolicyChecker();
                m_subscriberIdentities = new Dictionary<X509Certificate, DataRow>();
                UpdateCertificateChecker();

                // Initialize default settings
                commandChannel.SettingsCategory = Name.Replace("!", "").ToLower();
                commandChannel.ConfigurationString = "port=6165";
                commandChannel.PayloadAware = true;
                commandChannel.PersistSettings = true;
                commandChannel.RequireClientCertificate = true;
                commandChannel.CertificateChecker = m_certificateChecker;

                // Assign command channel client reference and attach to needed events
                CommandChannel = commandChannel;
            }

            // Initialize TCP server (loads config file settings)
            m_commandChannel.Initialize();

            // Start cipher key rotation timer when encrypting payload
            if (m_encryptPayload && (object)m_cipherKeyRotationTimer != null)
                m_cipherKeyRotationTimer.Start();

            // Register publisher with the statistics engine
            StatisticsEngine.Register(this, "Publisher", "PUB");
            StatisticsEngine.Calculated += (sender, args) => ResetMeasurementsPerSecondCounters();

            Initialized = true;
        }

        /// <summary>
        /// Queues a collection of measurements for processing to each <see cref="IActionAdapter"/> connected to this <see cref="DataPublisher"/>.
        /// </summary>
        /// <param name="measurements">Measurements to queue for processing.</param>
        public override void QueueMeasurementsForProcessing(IEnumerable<IMeasurement> measurements)
        {
            int measurementCount;

            m_routedMeasurementsHandler(this, new EventArgs<ICollection<IMeasurement>>(measurements.ToList()));

            measurementCount = measurements.Count();
            m_lifetimeMeasurements += measurementCount;
            UpdateMeasurementsPerSecond(measurementCount);
        }

        /// <summary>
        /// Establish <see cref="DataPublisher"/> and start listening for client connections.
        /// </summary>
        public override void Start()
        {
            if (!Enabled)
            {
                base.Start();

                if ((object)m_commandChannel != null)
                    m_commandChannel.Start();
            }
        }

        /// <summary>
        /// Terminate <see cref="DataPublisher"/> and stop listening for client connections.
        /// </summary>
        public override void Stop()
        {
            base.Stop();

            if ((object)m_commandChannel != null)
                m_commandChannel.Stop();
        }

        /// <summary>
        /// Gets a short one-line status of this <see cref="DataPublisher"/>.
        /// </summary>
        /// <param name="maxLength">Maximum number of available characters for display.</param>
        /// <returns>A short one-line summary of the current status of the <see cref="DataPublisher"/>.</returns>
        public override string GetShortStatus(int maxLength)
        {
            if ((object)m_commandChannel != null)
                return string.Format("Publishing data to {0} clients.", m_commandChannel.ClientIDs.Length).CenterText(maxLength);

            return "Currently not connected".CenterText(maxLength);
        }

        /// <summary>
        /// Enumerates connected clients.
        /// </summary>
        [AdapterCommand("Enumerates connected clients.", "Administrator", "Editor", "Viewer")]
        public virtual void EnumerateClients()
        {
            OnStatusMessage(EnumerateClients(false));
        }

        /// <summary>
        /// Enumerates connected clients with active temporal sessions.
        /// </summary>
        [AdapterCommand("Enumerates connected clients with active temporal sessions.", "Administrator", "Editor", "Viewer")]
        public virtual void EnumerateTemporalClients()
        {
            OnStatusMessage(EnumerateClients(true));
        }

        private string EnumerateClients(bool filterToTemporalSessions)
        {
            StringBuilder clientEnumeration = new StringBuilder();
            Guid[] clientIDs = (Guid[])m_commandChannel.ClientIDs.Clone();
            ClientConnection connection;
            string timestampFormat;
            bool hasActiveTemporalSession;

            if (filterToTemporalSessions)
                clientEnumeration.AppendFormat("\r\nIndices for connected clients with active temporal sessions:\r\n\r\n");
            else
                clientEnumeration.AppendFormat("\r\nIndices for {0} connected clients:\r\n\r\n", clientIDs.Length);

            for (int i = 0; i < clientIDs.Length; i++)
            {
                if (m_clientConnections.TryGetValue(clientIDs[i], out connection) && (object)connection != null && (object)connection.Subscription != null)
                {
                    hasActiveTemporalSession = connection.Subscription.TemporalConstraintIsDefined();

                    if (!filterToTemporalSessions || hasActiveTemporalSession)
                    {
                        if (connection.Subscription is UnsynchronizedClientSubscription)
                            timestampFormat = string.Format("{0} Format, {1}-byte timestamps", connection.Subscription.UseCompactMeasurementFormat ? "Compact" : "Full", connection.Subscription.TimestampSize);
                        else
                            timestampFormat = string.Format("{0} Format, 8-byte frame-level timestamp", connection.Subscription.UseCompactMeasurementFormat ? "Compact" : "Full");

                        clientEnumeration.AppendFormat("  {0} - {1}\r\n          {2}\r\n          {3}\r\n          {4}\r\n          Active Temporal Session = {5}\r\n\r\n",
                                i.ToString().PadLeft(3),
                                connection.ConnectionID,
                                connection.SubscriberInfo,
                                timestampFormat,
                                connection.OperationalModes,
                                hasActiveTemporalSession ? "Yes" : "No");
                    }
                }
            }

            // Return enumeration
            return clientEnumeration.ToString();
        }


        /// <summary>
        /// Rotates cipher keys for specified client connection.
        /// </summary>
        /// <param name="clientIndex">Enumerated index for client connection.</param>
        [AdapterCommand("Rotates cipher keys for client connection using its enumerated index.", "Administrator")]
        public virtual void RotateCipherKeys(int clientIndex)
        {
            Guid clientID = Guid.Empty;
            bool success = true;

            try
            {
                clientID = m_commandChannel.ClientIDs[clientIndex];
            }
            catch
            {
                success = false;
                OnStatusMessage("ERROR: Failed to find connected client with enumerated index " + clientIndex);
            }

            if (success)
            {
                ClientConnection connection;

                if (m_clientConnections.TryGetValue(clientID, out connection))
                    connection.RotateCipherKeys();
                else
                    OnStatusMessage("ERROR: Failed to find connected client " + clientID);
            }
        }

        /// <summary>
        /// Gets subscriber information for specified client connection.
        /// </summary>
        /// <param name="clientIndex">Enumerated index for client connection.</param>
        [AdapterCommand("Gets subscriber information for client connection using its enumerated index.", "Administrator", "Editor", "Viewer")]
        public virtual string GetSubscriberInfo(int clientIndex)
        {
            Guid clientID = Guid.Empty;
            bool success = true;

            try
            {
                clientID = m_commandChannel.ClientIDs[clientIndex];
            }
            catch
            {
                success = false;
                OnStatusMessage("ERROR: Failed to find connected client with enumerated index " + clientIndex);
            }

            if (success)
            {
                ClientConnection connection;

                if (m_clientConnections.TryGetValue(clientID, out connection))
                    return connection.SubscriberInfo;

                OnStatusMessage("ERROR: Failed to find connected client " + clientID);
            }

            return "";
        }

        /// <summary>
        /// Gets temporal status for a specified client connection.
        /// </summary>
        /// <param name="clientIndex">Enumerated index for client connection.</param>
        [AdapterCommand("Gets temporal status for a subscriber, if any, using its enumerated index.", "Administrator", "Editor", "Viewer")]
        public virtual string GetTemporalStatus(int clientIndex)
        {
            Guid clientID = Guid.Empty;
            bool success = true;

            try
            {
                clientID = m_commandChannel.ClientIDs[clientIndex];
            }
            catch
            {
                success = false;
                OnStatusMessage("ERROR: Failed to find connected client with enumerated index " + clientIndex);
            }

            if (success)
            {
                ClientConnection connection;

                if (m_clientConnections.TryGetValue(clientID, out connection))
                {
                    string temporalStatus = null;

                    if ((object)connection.Subscription != null)
                    {
                        if (connection.Subscription.TemporalConstraintIsDefined())
                            temporalStatus = connection.Subscription.TemporalSessionStatus;
                        else
                            temporalStatus = "Subscription does not have an active temporal session.";
                    }

                    if (string.IsNullOrWhiteSpace(temporalStatus))
                        temporalStatus = "Temporal session status is unavailable.";

                    return temporalStatus;
                }

                OnStatusMessage("ERROR: Failed to find connected client " + clientID);
            }

            return "";
        }

        /// <summary>
        /// Gets the local certificate currently in use by the data publisher.
        /// </summary>
        /// <returns>The local certificate file read directly from the certificate file as an array of bytes.</returns>
        [AdapterCommand("Gets the local certificate currently in use by the data publisher.", "Administrator", "Editor")]
        public virtual byte[] GetLocalCertificate()
        {
            TlsServer commandChannel;

            commandChannel = m_commandChannel as TlsServer;

            if ((object)commandChannel == null)
                throw new InvalidOperationException("Certificates can only be imported in TLS security mode.");

            return File.ReadAllBytes(FilePath.GetAbsolutePath(commandChannel.CertificateFile));
        }

        /// <summary>
        /// Imports a certificate to the trusted certificates path.
        /// </summary>
        /// <param name="fileName">The file name to give to the certificate when imported.</param>
        /// <param name="certificateData">The data to be written to the certificate file.</param>
        /// <returns>The local path on the server where the file was written.</returns>
        [AdapterCommand("Imports a certificate to the trusted certificates path.", "Administrator", "Editor")]
        public virtual string ImportCertificate(string fileName, byte[] certificateData)
        {
            TlsServer commandChannel;
            string trustedCertificatesPath;
            string filePath;

            commandChannel = m_commandChannel as TlsServer;

            if ((object)commandChannel == null)
                throw new InvalidOperationException("Certificates can only be imported in TLS security mode.");

            trustedCertificatesPath = FilePath.GetAbsolutePath(commandChannel.TrustedCertificatesPath);
            filePath = Path.Combine(trustedCertificatesPath, fileName);
            filePath = FilePath.GetUniqueFilePathWithBinarySearch(filePath);

            if (!Directory.Exists(trustedCertificatesPath))
                Directory.CreateDirectory(trustedCertificatesPath);

            File.WriteAllBytes(filePath, certificateData);

            return filePath;
        }

        /// <summary>
        /// Gets subscriber status for specified subscriber ID.
        /// </summary>
        /// <param name="subscriberID">Guid based subscriber ID for client connection.</param>
        [AdapterCommand("Gets subscriber status for client connection using its subscriber ID.", "Administrator", "Editor", "Viewer")]
        public virtual Tuple<Guid, bool, string> GetSubscriberStatus(Guid subscriberID)
        {
            return new Tuple<Guid, bool, string>(subscriberID, GetConnectionProperty(subscriberID, cc => cc.IsConnected), GetConnectionProperty(subscriberID, cc => cc.SubscriberInfo));
        }

        /// <summary>
        /// Resets the counters for the lifetime statistics without interrupting the adapter's operations.
        /// </summary>
        [AdapterCommand("Resets the counters for the lifetime statistics without interrupting the adapter's operations.", "Administrator", "Editor")]
        public virtual void ResetLifetimeCounters()
        {
            m_lifetimeMeasurements = 0L;
            m_totalBytesSent = 0L;
            m_lifetimeTotalLatency = 0L;
            m_lifetimeMinimumLatency = 0L;
            m_lifetimeMaximumLatency = 0L;
            m_lifetimeLatencyMeasurements = 0L;
        }

        /// <summary>
        /// Sends a notification to all subscribers.
        /// </summary>
        /// <param name="message">The message to be sent.</param>
        [AdapterCommand("Sends a notification to all subscribers.", "Administrator", "Editor")]
        public virtual void SendNotification(string message)
        {
            string notification = string.Format("[{0}] {1}", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff"), message);

            lock (m_clientNotificationsLock)
            {
                foreach (Dictionary<int, string> notifications in m_clientNotifications.Values)
                    notifications.Add(notification.GetHashCode(), notification);

                SerializeClientNotifications();
                SendAllNotifications();
            }

            OnStatusMessage("Sent notification: {0}", message);
        }

        /// <summary>
        /// Updates signal index cache based on input measurement keys.
        /// </summary>
        /// <param name="clientID">Client ID of connection over which to update signal index cache.</param>
        /// <param name="signalIndexCache">New signal index cache.</param>
        /// <param name="inputMeasurementKeys">Subscribed measurement keys.</param>
        public void UpdateSignalIndexCache(Guid clientID, SignalIndexCache signalIndexCache, MeasurementKey[] inputMeasurementKeys)
        {
            ConcurrentDictionary<ushort, Tuple<Guid, string, uint>> reference = new ConcurrentDictionary<ushort, Tuple<Guid, string, uint>>();
            List<Guid> unauthorizedKeys = new List<Guid>();
            ushort index = 0;
            Guid signalID;

            byte[] serializedSignalIndexCache;
            ClientConnection connection;

            if ((object)inputMeasurementKeys != null)
            {
                // We will now go through the client's requested keys and see which ones are authorized for subscription,
                // this information will be available through the returned signal index cache which will also define
                // a runtime index optimization for the allowed measurements.
                foreach (MeasurementKey key in inputMeasurementKeys)
                {
                    if (RequireAuthentication)
                    {
                        // Validate that subscriber has rights to this signal
                        if (SubscriberHasRights(signalIndexCache.SubscriberID, key, out signalID))
                            reference.TryAdd(index++, new Tuple<Guid, string, uint>(signalID, key.Source, key.ID));
                        else
                            unauthorizedKeys.Add(key.SignalID);
                    }
                    else
                    {
                        // When client authorization is not required, all points are assumed to be allowed
                        reference.TryAdd(index++, new Tuple<Guid, string, uint>(LookupSignalID(key), key.Source, key.ID));
                    }
                }
            }

            signalIndexCache.Reference = reference;
            signalIndexCache.UnauthorizedSignalIDs = unauthorizedKeys.ToArray();
            serializedSignalIndexCache = SerializeSignalIndexCache(clientID, signalIndexCache);

            // Send client updated signal index cache
            if (m_clientConnections.TryGetValue(clientID, out connection) && connection.IsSubscribed)
                SendClientResponse(clientID, ServerResponse.UpdateSignalIndexCache, ServerCommand.Subscribe, serializedSignalIndexCache);
        }

        /// <summary>
        /// Updates the latest measurement cache when the
        /// set of cached measurements may have changed.
        /// </summary>
        protected void UpdateLatestMeasurementCache()
        {
            try
            {
                string cacheMeasurementKeys;
                IActionAdapter cache;

                if (Settings.TryGetValue("cacheMeasurementKeys", out cacheMeasurementKeys))
                {
                    if (TryGetAdapterByName("LatestMeasurementCache", out cache))
                    {
                        cache.InputMeasurementKeys = AdapterBase.ParseInputMeasurementKeys(DataSource, true, cacheMeasurementKeys);
                        m_routingTables.CalculateRoutingTables(null);
                    }
                }
            }
            catch (Exception ex)
            {
                OnProcessException(new InvalidOperationException(string.Format("Failed to update latest measurement cache: {0}", ex.Message), ex));
            }
        }

        /// <summary>
        /// Updates each subscription's inputs based on
        /// possible updates to that subscriber's rights.
        /// </summary>
        protected void UpdateRights()
        {
            foreach (ClientConnection connection in m_clientConnections.Values)
                UpdateRights(connection);

            m_routingTables.CalculateRoutingTables(null);
        }

        private void UpdateClientNotifications()
        {
            try
            {
                Guid subscriberID;

                lock (m_clientNotificationsLock)
                {
                    m_clientNotifications.Clear();

                    if (DataSource.Tables.Contains("Subscribers"))
                    {
                        foreach (DataRow row in DataSource.Tables["Subscribers"].Rows)
                        {
                            if (Guid.TryParse(row["ID"].ToNonNullString(), out subscriberID))
                                m_clientNotifications.Add(subscriberID, new Dictionary<int, string>());
                        }
                    }

                    DeserializeClientNotifications();
                }
            }
            catch (Exception ex)
            {
                OnProcessException(new InvalidOperationException(string.Format("Failed to initialize client notification dictionary: {0}", ex.Message), ex));
            }
        }

        private void NotifyClientsOfConfigurationChange()
        {
            // Make sure publisher allows meta-data refresh, no need to notify clients of configuration change if they can't receive updates
            if (m_allowMetadataRefresh)
            {
                // This can be a lazy notification so we queue up work and return quickly
                ThreadPool.QueueUserWorkItem(state =>
                {
                    // Make a copy of client connection enumeration with ToArray() in case connections are added or dropped during notification
                    foreach (ClientConnection connection in m_clientConnections.Values)
                    {
                        SendClientResponse(connection.ClientID, ServerResponse.ConfigurationChanged, ServerCommand.Subscribe);
                    }
                });
            }
        }

        private void SerializeClientNotifications()
        {
            string notificationsFileName = FilePath.GetAbsolutePath(string.Format("{0}Notifications.txt", Name));

            // Delete existing file for re-serialization
            if (File.Exists(notificationsFileName))
                File.Delete(notificationsFileName);

            using (FileStream fileStream = File.OpenWrite(notificationsFileName))
            using (TextWriter writer = new StreamWriter(fileStream))
            {
                foreach (KeyValuePair<Guid, Dictionary<int, string>> pair in m_clientNotifications)
                {
                    foreach (string notification in pair.Value.Values)
                        writer.WriteLine("{0},{1}", pair.Key, notification);
                }
            }
        }

        private void DeserializeClientNotifications()
        {
            string notificationsFileName = FilePath.GetAbsolutePath(string.Format("{0}Notifications.txt", Name));
            Dictionary<int, string> notifications;
            string notification;
            Guid subscriberID;

            if (File.Exists(notificationsFileName))
            {
                using (FileStream fileStream = File.OpenRead(notificationsFileName))
                using (TextReader reader = new StreamReader(fileStream))
                {
                    string line = reader.ReadLine();

                    while ((object)line != null)
                    {
                        int separatorIndex = line.IndexOf(',');

                        if (Guid.TryParse(line.Substring(0, separatorIndex), out subscriberID))
                        {
                            if (m_clientNotifications.TryGetValue(subscriberID, out notifications))
                            {
                                notification = line.Substring(separatorIndex + 1);
                                notifications.Add(notification.GetHashCode(), notification);
                            }
                        }

                        line = reader.ReadLine();
                    }
                }
            }
        }

        private void SendAllNotifications()
        {
            foreach (ClientConnection connection in m_clientConnections.Values)
                SendNotifications(connection);
        }

        private void SendNotifications(ClientConnection connection)
        {
            Dictionary<int, string> notifications;
            byte[] hash;
            byte[] message;

            using (BlockAllocatedMemoryStream buffer = new BlockAllocatedMemoryStream())
            {
                if (m_clientNotifications.TryGetValue(connection.SubscriberID, out notifications))
                {
                    foreach (KeyValuePair<int, string> pair in notifications)
                    {
                        hash = BigEndian.GetBytes(pair.Key);
                        message = connection.Encoding.GetBytes(pair.Value);

                        buffer.Write(hash, 0, hash.Length);
                        buffer.Write(message, 0, message.Length);
                        SendClientResponse(connection.ClientID, ServerResponse.Notify, ServerCommand.Subscribe, buffer.ToArray());
                        buffer.Position = 0;
                    }
                }
            }
        }

        /// <summary>
        /// Determines if subscriber has rights to specified <paramref name="signalID"/>.
        /// </summary>
        /// <param name="subscriberID"><see cref="Guid"/> based subscriber ID.</param>
        /// <param name="signalID"><see cref="Guid"/> signal ID to lookup.</param>
        /// <returns><c>true</c> if subscriber has rights to specified <see cref="MeasurementKey"/>; otherwise <c>false</c>.</returns>
        protected bool SubscriberHasRights(Guid subscriberID, Guid signalID)
        {
            try
            {
                const string FilterRegex = @"(ALLOW|DENY)\s+WHERE\s+([^;]*)";

                DataRow subscriber;
                DataRow[] subscriberMeasurementGroups;

                IEnumerable<bool> explicitAuthorizationFlags;
                IEnumerable<bool> explicitGroupAuthorizationFlags;
                IEnumerable<bool> implicitFilterAuthorizationFlags;

                bool explicitlyAuthorized = false;
                bool explicitlyAuthorizedByGroup = false;
                bool implicitlyAuthorizedByFilter = false;

                // If authentication is not required,
                // subscriber has rights to everything
                if (!RequireAuthentication)
                    return true;

                subscriber = DataSource.Tables["Subscribers"].Select(string.Format("ID = '{0}' AND Enabled <> 0", subscriberID)).FirstOrDefault();

                // If subscriber has been disabled or removed
                // from the list of valid subscribers,
                // they no longer have rights to any signals
                if ((object)subscriber == null)
                    return false;

                // Look up explicitly defined individual measurements
                explicitAuthorizationFlags = DataSource.Tables["SubscriberMeasurements"].Select(string.Format("SubscriberID = '{0}' AND SignalID = '{1}'", subscriberID, signalID))
                    .Select(measurement => measurement["Allowed"].ToNonNullString("0").ParseBoolean());

                foreach (bool flag in explicitAuthorizationFlags)
                {
                    if (flag)
                        explicitlyAuthorized = true;
                    else
                        return false;
                }

                if (explicitlyAuthorized)
                    return true;

                // Look up explicitly defined group based measurements
                subscriberMeasurementGroups = DataSource.Tables["SubscriberMeasurementGroups"].Select(string.Format("SubscriberID = '{0}'", subscriberID));

                explicitGroupAuthorizationFlags = subscriberMeasurementGroups
                    .Where(subscriberMeasurementGroup => DataSource.Tables["MeasurementGroupMeasurements"].Select(string.Format("SignalID = '{0}' AND MeasurementGroupID = {1}", signalID, subscriberMeasurementGroup["MeasurementGroupID"])).Length > 0)
                    .Select(subscriberMeasurementGroup => subscriberMeasurementGroup["Allowed"].ToNonNullString("0").ParseBoolean());

                foreach (bool flag in explicitGroupAuthorizationFlags)
                {
                    if (flag)
                        explicitlyAuthorizedByGroup = true;
                    else
                        return false;
                }

                if (explicitlyAuthorizedByGroup)
                    return true;

                // Look up implicitly defined filter based measurements
                implicitFilterAuthorizationFlags = Regex.Matches(subscriber["AccessControlFilter"].ToNonNullString().ReplaceControlCharacters(), FilterRegex, RegexOptions.IgnoreCase)
                    .Cast<Match>()
                    .Where(match => DataSource.Tables["ActiveMeasurements"].Select(string.Format("SignalID = '{0}' AND ({1})", signalID, match.Groups[2].Value)).Length > 0)
                    .Select(match => (match.Groups[1].Value == "ALLOW"));

                foreach (bool flag in implicitFilterAuthorizationFlags)
                {
                    if (flag)
                        implicitlyAuthorizedByFilter = true;
                    else
                        return false;
                }

                if (implicitlyAuthorizedByFilter)
                    return true;

                // Look up implicitly defined group based measurements
                return subscriberMeasurementGroups
                    .Select(subscriberMeasurementGroup => Tuple.Create(subscriberMeasurementGroup, DataSource.Tables["MeasurementGroups"].Select(string.Format("ID = {0}", subscriberMeasurementGroup["MeasurementGroupID"]))))
                    .Where(tuple => tuple.Item2.Any(measurementGroup => AdapterBase.ParseInputMeasurementKeys(DataSource, false, measurementGroup["FilterExpression"].ToNonNullString()).Select(key => key.SignalID).Contains(signalID)))
                    .Select(tuple => tuple.Item1["Allowed"].ToNonNullString("0").ParseBoolean())
                    .DefaultIfEmpty(false)
                    .All(allowed => allowed);
            }
            catch (Exception ex)
            {
                OnProcessException(new InvalidOperationException(string.Format("Failed to determine subscriber rights for {0} due to exception: {1}", GetConnectionProperty(subscriberID, cc => cc.SubscriberAcronym), ex.Message), ex));
            }

            return false;
        }

        /// <summary>
        /// Determines if subscriber has rights to specified <see cref="MeasurementKey"/>.
        /// </summary>
        /// <param name="subscriberID"><see cref="Guid"/> based subscriber ID.</param>
        /// <param name="key"><see cref="MeasurementKey"/> to lookup.</param>
        /// <param name="signalID"><see cref="Guid"/> signal ID if found; otherwise an empty Guid.</param>
        /// <returns><c>true</c> if subscriber has rights to specified <see cref="MeasurementKey"/>; otherwise <c>false</c>.</returns>
        protected bool SubscriberHasRights(Guid subscriberID, MeasurementKey key, out Guid signalID)
        {
            signalID = LookupSignalID(key);

            if (signalID != Guid.Empty)
                return SubscriberHasRights(subscriberID, signalID);

            return false;
        }

        /// <summary>
        /// Looks up <see cref="Guid"/> signal ID for given <see cref="MeasurementKey"/>.
        /// </summary>
        /// <param name="key"><see cref="MeasurementKey"/> to lookup.</param>
        /// <returns><see cref="Guid"/> signal ID if found; otherwise an empty Guid.</returns>
        protected Guid LookupSignalID(MeasurementKey key)
        {
            // Attempt to lookup measurement key's signal ID that may have already been cached
            return m_signalIDCache.GetOrAdd(key, keyParam =>
            {
                Guid signalID = Guid.Empty;

                if ((object)DataSource != null && DataSource.Tables.Contains("ActiveMeasurements"))
                {
                    // Attempt to lookup input measurement keys for given source IDs from default measurement table, if defined
                    try
                    {
                        DataRow[] filteredRows = DataSource.Tables["ActiveMeasurements"].Select("ID='" + keyParam.ToString() + "'");

                        if (filteredRows.Length > 0)
                            Guid.TryParse(filteredRows[0]["SignalID"].ToString(), out signalID);
                    }
                    catch
                    {
                        // Errors here are not catastrophic, this simply limits the auto-assignment of input measurement keys based on specified source ID's
                    }
                }

                if (signalID == Guid.Empty)
                    signalID = key.SignalID;

                return signalID;
            });
        }

        /// <summary>
        /// Gets the text encoding associated with a particular client.
        /// </summary>
        /// <param name="clientID">ID of client.</param>
        /// <returns>Text encoding associated with a particular client.</returns>
        protected internal Encoding GetClientEncoding(Guid clientID)
        {
            ClientConnection connection;

            if (m_clientConnections.TryGetValue(clientID, out connection))
            {
                Encoding clientEncoding = connection.Encoding;

                if ((object)clientEncoding != null)
                    return clientEncoding;
            }

            // Default to Unicode
            return Encoding.Unicode;
        }

        /// <summary>
        /// Sends the start time of the first measurement in a connection transmission.
        /// </summary>
        /// <param name="clientID">ID of client to send response.</param>
        /// <param name="startTime">Start time, in <see cref="Ticks"/>, of first measurement transmitted.</param>
        internal protected virtual bool SendDataStartTime(Guid clientID, Ticks startTime)
        {
            bool result = SendClientResponse(clientID, ServerResponse.DataStartTime, ServerCommand.Subscribe, BigEndian.GetBytes((long)startTime));

            ClientConnection connection;

            if (m_clientConnections.TryGetValue(clientID, out connection))
                OnStatusMessage("Start time sent to {0}.", connection.ConnectionID);

            return result;
        }

        // Handle input processing complete notifications

        /// <summary>
        /// Sends response back to specified client.
        /// </summary>
        /// <param name="clientID">ID of client to send response.</param>
        /// <param name="response">Server response.</param>
        /// <param name="command">In response to command.</param>
        /// <returns><c>true</c> if send was successful; otherwise <c>false</c>.</returns>
        internal protected virtual bool SendClientResponse(Guid clientID, ServerResponse response, ServerCommand command)
        {
            return SendClientResponse(clientID, response, command, (byte[])null);
        }

        /// <summary>
        /// Sends response back to specified client with a message.
        /// </summary>
        /// <param name="clientID">ID of client to send response.</param>
        /// <param name="response">Server response.</param>
        /// <param name="command">In response to command.</param>
        /// <param name="status">Status message to return.</param>
        /// <returns><c>true</c> if send was successful; otherwise <c>false</c>.</returns>
        internal protected virtual bool SendClientResponse(Guid clientID, ServerResponse response, ServerCommand command, string status)
        {
            if ((object)status != null)
                return SendClientResponse(clientID, response, command, GetClientEncoding(clientID).GetBytes(status));

            return SendClientResponse(clientID, response, command);
        }

        /// <summary>
        /// Sends response back to specified client with a formatted message.
        /// </summary>
        /// <param name="clientID">ID of client to send response.</param>
        /// <param name="response">Server response.</param>
        /// <param name="command">In response to command.</param>
        /// <param name="formattedStatus">Formatted status message to return.</param>
        /// <param name="args">Arguments for <paramref name="formattedStatus"/>.</param>
        /// <returns><c>true</c> if send was successful; otherwise <c>false</c>.</returns>
        internal protected virtual bool SendClientResponse(Guid clientID, ServerResponse response, ServerCommand command, string formattedStatus, params object[] args)
        {
            if (!string.IsNullOrWhiteSpace(formattedStatus))
                return SendClientResponse(clientID, response, command, GetClientEncoding(clientID).GetBytes(string.Format(formattedStatus, args)));

            return SendClientResponse(clientID, response, command);
        }

        /// <summary>
        /// Sends response back to specified client with attached data.
        /// </summary>
        /// <param name="clientID">ID of client to send response.</param>
        /// <param name="response">Server response.</param>
        /// <param name="command">In response to command.</param>
        /// <param name="data">Data to return to client; null if none.</param>
        /// <returns><c>true</c> if send was successful; otherwise <c>false</c>.</returns>
        internal protected virtual bool SendClientResponse(Guid clientID, ServerResponse response, ServerCommand command, byte[] data)
        {
            return SendClientResponse(null, clientID, (byte)response, (byte)command, data);
        }

        /// <summary>
        /// Sends response back to specified client with attached data using specified working buffer.
        /// </summary>
        /// <param name="workingBuffer">Working buffer to use to assemble response, or <c>null</c> to have one created for you.</param>
        /// <param name="clientID">ID of client to send response.</param>
        /// <param name="response">Server response.</param>
        /// <param name="command">In response to command.</param>
        /// <param name="data">Data to return to client; null if none.</param>
        /// <returns><c>true</c> if send was successful; otherwise <c>false</c>.</returns>
        internal protected virtual bool SendClientResponse(BlockAllocatedMemoryStream workingBuffer, Guid clientID, ServerResponse response, ServerCommand command, byte[] data)
        {
            return SendClientResponse(workingBuffer, clientID, (byte)response, (byte)command, data);
        }

        /// <summary>
        /// Updates latency statistics based on the collection of latencies passed into the method.
        /// </summary>
        /// <param name="latencies">The latencies of the measurements sent by the publisher.</param>
        internal protected virtual void UpdateLatencyStatistics(IEnumerable<long> latencies)
        {
            foreach (long latency in latencies)
            {
                if (m_lifetimeMinimumLatency > latency || m_lifetimeMinimumLatency == 0)
                    m_lifetimeMinimumLatency = latency;

                if (m_lifetimeMaximumLatency < latency || m_lifetimeMaximumLatency == 0)
                    m_lifetimeMaximumLatency = latency;

                m_lifetimeTotalLatency += latency;
                m_lifetimeLatencyMeasurements++;
            }
        }

        // Attempts to get the subscriber for the given client based on that client's X.509 certificate.
        private void TryFindClientDetails(ClientConnection connection)
        {
            TlsServer commandChannel = m_commandChannel as TlsServer;
            TransportProvider<TlsServer.TlsSocket> client;
            X509Certificate remoteCertificate;
            X509Certificate trustedCertificate;
            DataRow subscriber;

            // If connection is not TLS, there is no X.509 certificate
            if ((object)commandChannel == null)
                return;

            // If connection is not found, cannot get X.509 certificate
            if (!commandChannel.TryGetClient(connection.ClientID, out client))
                return;

            // Get remote certificate and corresponding trusted certificate
            remoteCertificate = client.Provider.SslStream.RemoteCertificate;
            trustedCertificate = m_certificateChecker.GetTrustedCertificate(remoteCertificate);

            if ((object)trustedCertificate != null)
            {
                if (m_subscriberIdentities.TryGetValue(trustedCertificate, out subscriber))
                {
                    // Load client details from subscriber identity
                    connection.SubscriberID = Guid.Parse(subscriber["ID"].ToNonNullString(Guid.Empty.ToString()).Trim());
                    connection.SubscriberAcronym = subscriber["Acronym"].ToNonNullString().Trim();
                    connection.SubscriberName = subscriber["Name"].ToNonNullString().Trim();
                    connection.ValidIPAddresses = ParseAddressList(subscriber["ValidIPAddresses"].ToNonNullString());
                }
            }
        }

        // Parses a list of IP addresses.
        private List<IPAddress> ParseAddressList(string addressList)
        {
            string[] splitList = addressList.Split(';', ',');
            List<IPAddress> ipAddressList = new List<IPAddress>();
            IPAddress ipAddress;
            string dualStackAddress;

            foreach (string address in splitList)
            {
                // Attempt to parse the IP address
                if (!IPAddress.TryParse(address.Trim(), out ipAddress))
                    continue;

                // Add the parsed address to the list
                ipAddressList.Add(ipAddress);

                // IPv4 addresses may connect as an IPv6 dual-stack equivalent,
                // so attempt to add that equivalent address to the list as well
                if (ipAddress.AddressFamily == AddressFamily.InterNetwork)
                {
                    dualStackAddress = string.Format("::ffff:{0}", address.Trim());

                    if (IPAddress.TryParse(dualStackAddress, out ipAddress))
                        ipAddressList.Add(ipAddress);
                }
            }

            return ipAddressList;
        }

        // Update certificate validation routine.
        private void UpdateCertificateChecker()
        {
            try
            {
                CertificatePolicy policy;
                SslPolicyErrors validPolicyErrors;
                X509ChainStatusFlags validChainFlags;

                string remoteCertificateFile;
                X509Certificate certificate;

                if ((object)m_certificateChecker == null || m_securityMode != SecurityMode.TLS)
                    return;

                m_certificateChecker.DistrustAll();
                m_subscriberIdentities.Clear();

                foreach (DataRow subscriber in DataSource.Tables["Subscribers"].Select("Enabled <> 0"))
                {
                    try
                    {
                        policy = new CertificatePolicy();
                        remoteCertificateFile = subscriber["RemoteCertificateFile"].ToNonNullString();

                        if (Enum.TryParse(subscriber["ValidPolicyErrors"].ToNonNullString(), out validPolicyErrors))
                            policy.ValidPolicyErrors = validPolicyErrors;

                        if (Enum.TryParse(subscriber["ValidChainFlags"].ToNonNullString(), out validChainFlags))
                            policy.ValidChainFlags = validChainFlags;

                        if (File.Exists(remoteCertificateFile))
                        {
                            certificate = new X509Certificate2(remoteCertificateFile);
                            m_certificateChecker.Trust(certificate, policy);
                            m_subscriberIdentities.Add(certificate, subscriber);
                        }
                    }
                    catch (Exception ex)
                    {
                        OnProcessException(new InvalidOperationException(string.Format("Failed to add subscriber \"{0}\" certificate to trusted certificates: {1}", subscriber["Acronym"].ToNonNullNorEmptyString("[UNKNOWN]"), ex.Message), ex));
                    }
                }
            }
            catch (Exception ex)
            {
                OnProcessException(new InvalidOperationException(string.Format("Failed to update certificate checker: {0}", ex.Message), ex));
            }
        }

        // Update rights for the given subscription.
        private void UpdateRights(ClientConnection connection)
        {
            if ((object)connection == null)
                return;

            try
            {
                IClientSubscription subscription = connection.Subscription;
                MeasurementKey[] requestedInputs;
                HashSet<MeasurementKey> authorizedSignals;
                Guid subscriberID;
                string message;

                // Determine if the connection has been disabled or removed - make sure to set authenticated to false if necessary
                if ((object)DataSource != null && DataSource.Tables.Contains("Subscribers") &&
                    !DataSource.Tables["Subscribers"].Select(string.Format("ID = '{0}' AND Enabled <> 0", connection.SubscriberID)).Any())
                    connection.Authenticated = false;

                if ((object)subscription != null)
                {
                    // It is important here that "SELECT" not be allowed in parsing the input measurement keys expression since this key comes
                    // from the remote subscription - this will prevent possible SQL injection attacks.
                    requestedInputs = AdapterBase.ParseInputMeasurementKeys(DataSource, false, subscription.RequestedInputFilter);
                    authorizedSignals = new HashSet<MeasurementKey>();
                    subscriberID = subscription.SubscriberID;

                    foreach (MeasurementKey input in requestedInputs)
                    {
                        if (SubscriberHasRights(subscriberID, input.SignalID))
                            authorizedSignals.Add(input);
                    }

                    if (!authorizedSignals.SetEquals(subscription.InputMeasurementKeys))
                    {
                        // Update the subscription associated with this connection based on newly acquired or revoked rights
                        message = string.Format("Update to authorized signals caused subscription to change. Now subscribed to {0} signals.", authorizedSignals.Count);
                        subscription.InputMeasurementKeys = authorizedSignals.ToArray();
                        SendClientResponse(subscription.ClientID, ServerResponse.Succeeded, ServerCommand.Subscribe, message);
                    }
                }
            }
            catch (Exception ex)
            {
                OnProcessException(new InvalidOperationException(string.Format("Failed to update authorized signal rights for \"{0}\" - connection will be terminated: {1}", connection.ConnectionID, ex.Message), ex));

                // If we can't assign rights, terminate connection
                ThreadPool.QueueUserWorkItem(DisconnectClient, connection.ClientID);
            }
        }

        // Send binary response packet to client
        private bool SendClientResponse(BlockAllocatedMemoryStream workingBuffer, Guid clientID, byte responseCode, byte commandCode, byte[] data)
        {
            ClientConnection connection;
            bool success = false;
            bool createdBuffer = false;

            // Attempt to lookup associated client connection
            if (m_clientConnections.TryGetValue(clientID, out connection) && (object)connection != null)
            {
                try
                {
                    // Make sure a working buffer is available and ready to use
                    if ((object)workingBuffer == null)
                    {
                        // Create a new working buffer
                        workingBuffer = new BlockAllocatedMemoryStream();
                        createdBuffer = true;
                    }
                    else
                    {
                        // Reset existing working buffer
                        workingBuffer.SetLength(0);
                    }

                    bool dataPacketResponse = responseCode == (byte)ServerResponse.DataPacket;
                    bool useDataChannel = (dataPacketResponse || responseCode == (byte)ServerResponse.BufferBlock);

                    // Add response code
                    workingBuffer.WriteByte(responseCode);

                    // Add original in response to command code
                    workingBuffer.WriteByte(commandCode);

                    if ((object)data == null || data.Length == 0)
                    {
                        // Add zero sized data buffer to response packet
                        workingBuffer.Write(ZeroLengthBytes, 0, 4);
                    }
                    else
                    {
                        // If response is for a data packet and a connection key is defined, encrypt the data packet payload
                        if (dataPacketResponse && (object)connection.KeyIVs != null)
                        {
                            // Get a local copy of volatile keyIVs and cipher index since these can change at any time
                            byte[][][] keyIVs = connection.KeyIVs;
                            int cipherIndex = connection.CipherIndex;

                            // Reserve space for size of data buffer to go into response packet
                            workingBuffer.Write(ZeroLengthBytes, 0, 4);

                            // Get data packet flags
                            DataPacketFlags flags = (DataPacketFlags)data[0];

                            // Encode current cipher index into data packet flags
                            if (cipherIndex > 0)
                                flags |= DataPacketFlags.CipherIndex;

                            // Write data packet flags into response packet
                            workingBuffer.WriteByte((byte)flags);

                            // Copy source data payload into a memory stream
                            MemoryStream sourceData = new MemoryStream(data, 1, data.Length - 1);

                            // Encrypt payload portion of data packet and copy into the response packet
                            Common.SymmetricAlgorithm.Encrypt(sourceData, workingBuffer, keyIVs[cipherIndex][0], keyIVs[cipherIndex][1]);

                            // Calculate length of encrypted data payload
                            int payloadLength = (int)workingBuffer.Length - 6;

                            // Move the response packet position back to the packet size reservation
                            workingBuffer.Seek(2, SeekOrigin.Begin);

                            // Add the actual size of payload length to response packet
                            workingBuffer.Write(BigEndian.GetBytes(payloadLength), 0, 4);
                        }
                        else
                        {
                            // Add size of data buffer to response packet
                            workingBuffer.Write(BigEndian.GetBytes(data.Length), 0, 4);

                            // Add data buffer
                            workingBuffer.Write(data, 0, data.Length);
                        }
                    }

                    IServer publishChannel;

                    // Data packets and buffer blocks can be published on a UDP data channel, so check for this...
                    if (useDataChannel)
                        publishChannel = m_clientPublicationChannels.GetOrAdd(clientID, id => (object)connection != null ? connection.PublishChannel : m_commandChannel);
                    else
                        publishChannel = m_commandChannel;

                    // Send response packet
                    if ((object)publishChannel != null && publishChannel.CurrentState == ServerState.Running)
                    {
                        byte[] responseData = workingBuffer.ToArray();

                        if (publishChannel is UdpServer)
                            publishChannel.MulticastAsync(responseData, 0, responseData.Length);
                        else
                            publishChannel.SendToAsync(clientID, responseData, 0, responseData.Length);

                        m_totalBytesSent += responseData.Length;
                        success = true;
                    }
                }
                catch (ObjectDisposedException)
                {
                    // This happens when there is still data to be sent to a disconnected client - we can safely ignore this exception
                }
                catch (NullReferenceException)
                {
                    // This happens when there is still data to be sent to a disconnected client - we can safely ignore this exception
                }
                catch (SocketException ex)
                {
                    if (!HandleSocketException(clientID, ex))
                        OnProcessException(new InvalidOperationException("Failed to send response packet to client due to exception: " + ex.Message, ex));
                }
                catch (InvalidOperationException ex)
                {
                    // Could still be processing threads with client data after client has been disconnected, this can be safely ignored
                    if (!ex.Message.StartsWith("No client found"))
                        OnProcessException(new InvalidOperationException("Failed to send response packet to client due to exception: " + ex.Message, ex));
                }
                catch (Exception ex)
                {
                    OnProcessException(new InvalidOperationException("Failed to send response packet to client due to exception: " + ex.Message, ex));
                }
                finally
                {
                    if (createdBuffer)
                        workingBuffer.Dispose();
                }
            }

            return success;
        }

        // Socket exception handler
        private bool HandleSocketException(Guid clientID, Exception ex)
        {
            SocketException socketException = ex as SocketException;

            if ((object)socketException != null)
            {
                // WSAECONNABORTED and WSAECONNRESET are common errors after a client disconnect,
                // if they happen for other reasons, make sure disconnect procedure is handled
                if (socketException.ErrorCode == 10053 || socketException.ErrorCode == 10054)
                {
                    try
                    {
                        ThreadPool.QueueUserWorkItem(DisconnectClient, clientID);
                    }
                    catch (Exception queueException)
                    {
                        // Process exception for logging
                        OnProcessException(new InvalidOperationException("Failed to queue client disconnect due to exception: " + queueException.Message, queueException));
                    }

                    return true;
                }
            }

            if ((object)ex != null)
                HandleSocketException(clientID, ex.InnerException);

            return false;
        }

        // Disconnect client - this should be called from non-blocking thread (e.g., thread pool)
        private void DisconnectClient(object state)
        {
            try
            {
                Guid clientID = (Guid)state;
                ClientConnection connection;
                IServer publicationChannel;

                RemoveClientSubscription(clientID);

                if (m_clientConnections.TryRemove(clientID, out connection))
                {
                    connection.Dispose();
                    OnStatusMessage("Client disconnected from command channel.");
                }

                m_clientPublicationChannels.TryRemove(clientID, out publicationChannel);
            }
            catch (Exception ex)
            {
                OnProcessException(new InvalidOperationException(string.Format("Encountered an exception while processing client disconnect: {0}", ex.Message), ex));
            }
        }

        // Remove client subscription
        private void RemoveClientSubscription(Guid clientID)
        {
            lock (this)
            {
                IClientSubscription clientSubscription;

                if (TryGetClientSubscription(clientID, out clientSubscription))
                {
                    clientSubscription.Stop();
                    Remove(clientSubscription);

                    try
                    {
                        // Notify system that subscriber disconnected therefore demanded measurements may have changed
                        ThreadPool.QueueUserWorkItem(NotifyHostOfSubscriptionRemoval);
                    }
                    catch (Exception ex)
                    {
                        // Process exception for logging
                        OnProcessException(new InvalidOperationException("Failed to queue notification of subscription removal due to exception: " + ex.Message, ex));
                    }
                }
            }
        }

        // Handle notification on input measurement key change
        private void NotifyHostOfSubscriptionRemoval(object state)
        {
            OnInputMeasurementKeysUpdated();
        }

        // Attempt to find client subscription
        private bool TryGetClientSubscription(Guid clientID, out IClientSubscription subscription)
        {
            IActionAdapter adapter;

            // Lookup adapter by its client ID
            if (TryGetAdapter(clientID, GetClientSubscription, out adapter))
            {
                subscription = (IClientSubscription)adapter;
                return true;
            }

            subscription = null;
            return false;
        }

        private bool GetClientSubscription(IActionAdapter item, Guid value)
        {
            IClientSubscription subscription = item as IClientSubscription;

            if ((object)subscription != null)
                return subscription.ClientID == value;

            return false;
        }

        // Gets specified property from client connection based on subscriber ID
        private TResult GetConnectionProperty<TResult>(Guid subscriberID, Func<ClientConnection, TResult> predicate)
        {
            TResult result = default(TResult);

            // Lookup client connection by subscriber ID
            ClientConnection connection = m_clientConnections.Values.FirstOrDefault(cc => cc.SubscriberID == subscriberID);

            // Extract desired property from client connection using given predicate function
            if ((object)connection != null)
                result = predicate(connection);

            return result;
        }

        /// <summary>
        /// Raises the <see cref="ProcessingComplete"/> event.
        /// </summary>
        protected virtual void OnProcessingComplete()
        {
            try
            {
                if ((object)ProcessingComplete != null)
                    ProcessingComplete(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                // We protect our code from consumer thrown exceptions
                OnProcessException(new InvalidOperationException(string.Format("Exception in consumer handler for ProcessingComplete event: {0}", ex.Message), ex));
            }
        }

        /// <summary>
        /// Raises the <see cref="ClientConnected"/> event.
        /// </summary>
        /// <param name="subscriberID">Subscriber <see cref="Guid"/> (normally <see cref="ClientConnection.SubscriberID"/>).</param>
        /// <param name="connectionID">Connection identification (normally <see cref="ClientConnection.ConnectionID"/>).</param>
        /// <param name="subscriberInfo">Subscriber information (normally <see cref="ClientConnection.SubscriberInfo"/>).</param>
        protected virtual void OnClientConnected(Guid subscriberID, string connectionID, string subscriberInfo)
        {
            try
            {
                if ((object)ClientConnected != null)
                    ClientConnected(this, new EventArgs<Guid, string, string>(subscriberID, connectionID, subscriberInfo));
            }
            catch (Exception ex)
            {
                // We protect our code from consumer thrown exceptions
                OnProcessException(new InvalidOperationException(string.Format("Exception in consumer handler for ClientConnected event: {0}", ex.Message), ex));
            }
        }

        // Make sure to expose any routing table messages
        private void m_routingTables_StatusMessage(object sender, EventArgs<string> e)
        {
            OnStatusMessage(e.Argument);
        }

        // Make sure to expose any routing table exceptions
        private void m_routingTables_ProcessException(object sender, EventArgs<Exception> e)
        {
            OnProcessException(e.Argument);
        }

        // Cipher key rotation timer handler
        private void m_cipherKeyRotationTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if ((object)m_clientConnections != null)
            {
                foreach (ClientConnection connection in m_clientConnections.Values)
                {
                    if ((object)connection != null && connection.Authenticated)
                        connection.RotateCipherKeys();
                }
            }
        }

        // Command channel restart timer handler
        private void m_commandChannelRestartTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if ((object)m_commandChannel != null)
            {
                try
                {
                    // After a short delay, we try to restart the command channel
                    m_commandChannel.Start();
                }
                catch (Exception ex)
                {
                    OnProcessException(new InvalidOperationException("Failed to restart data publisher command channel: " + ex.Message, ex));
                }
            }
        }

        // Determines whether the data in the data source has actually changed when receiving a new data source.
        private bool DataSourceChanged(DataSet newDataSource)
        {
            try
            {
                return !DataSetEqualityComparer.Default.Equals(DataSource, newDataSource);
            }
            catch
            {
                return true;
            }
        }

        #region [ Server Command Request Handlers ]

        // Handles authentication request
        private void HandleAuthenticationRequest(ClientConnection connection, byte[] buffer, int startIndex, int length)
        {
            Guid clientID = connection.ClientID;
            string message;

            // Handle authentication request
            try
            {
                DataRow subscriber = null;

                // Check security mode to determine whether authentication is supported
                if (m_securityMode == SecurityMode.TLS)
                {
                    message = "WARNING: Received authentication request from client while running in TLS mode.";
                    SendClientResponse(connection.ClientID, ServerResponse.Failed, ServerCommand.Authenticate, message);
                    OnProcessException(new InvalidOperationException(message));
                    return;
                }

                // Reset existing authentication state
                connection.Authenticated = false;

                // Subscriber connection is first referenced by its IP
                foreach (DataRow row in DataSource.Tables["Subscribers"].Select("Enabled <> 0"))
                {
                    // See if any of these IP addresses match the connection source of the subscriber
                    if (Enumerable.Contains(ParseAddressList(row["ValidIPAddresses"].ToNonNullString()), connection.IPAddress))
                        subscriber = row;

                    if ((object)subscriber != null)
                        break;
                }

                if ((object)subscriber == null)
                {
                    message = string.Format("No subscriber is registered for {0}, cannot authenticate connection - {1} request denied.", connection.ConnectionID, ServerCommand.Authenticate);
                    SendClientResponse(clientID, ServerResponse.Failed, ServerCommand.Authenticate, message);
                    OnStatusMessage("WARNING: Client {0} {1} command request denied - subscriber is disabled or not registered.", connection.ConnectionID, ServerCommand.Authenticate);
                }
                else
                {
                    // Found the subscriber record with a matching IP address, extract authentication information
                    string sharedSecret = subscriber["SharedSecret"].ToNonNullString().Trim();
                    string authenticationID = subscriber["AuthKey"].ToNonNullString().Trim();

                    // Update subscriber data in associated connection object
                    connection.SubscriberID = Guid.Parse(subscriber["ID"].ToNonNullString(Guid.Empty.ToString()).Trim());
                    connection.SubscriberAcronym = subscriber["Acronym"].ToNonNullString().Trim();
                    connection.SubscriberName = subscriber["Name"].ToNonNullString().Trim();
                    connection.SharedSecret = sharedSecret;

                    if (length >= 5)
                    {
                        // First 4 bytes beyond command byte represent an integer representing the length of the authentication string that follows
                        int byteLength = BigEndian.ToInt32(buffer, startIndex);
                        startIndex += 4;

                        // Byte length should be reasonable
                        if (byteLength >= 16 && byteLength <= 256)
                        {
                            if (length >= 5 + byteLength)
                            {
                                // Decrypt encoded portion of buffer
                                byte[] bytes = buffer.Decrypt(startIndex, byteLength, sharedSecret, CipherStrength.Aes256);
                                //startIndex += byteLength;

                                // Validate the authentication ID - if it matches, connection is authenticated
                                connection.Authenticated = (string.Compare(authenticationID, GetClientEncoding(clientID).GetString(bytes, CipherSaltLength, bytes.Length - CipherSaltLength), StringComparison.Ordinal) == 0);

                                if (connection.Authenticated)
                                {
                                    // Send success response
                                    message = string.Format("Registered subscriber \"{0}\" {1} was successfully authenticated.", connection.SubscriberName, connection.ConnectionID);
                                    SendClientResponse(clientID, ServerResponse.Succeeded, ServerCommand.Authenticate, message);
                                    OnStatusMessage(message);

                                    lock (m_clientNotificationsLock)
                                    {
                                        // Send any queued notifications to authenticated client
                                        SendNotifications(connection);
                                    }
                                }
                                else
                                {
                                    message = string.Format("Subscriber authentication failed - {0} request denied.", ServerCommand.Authenticate);
                                    SendClientResponse(clientID, ServerResponse.Failed, ServerCommand.Authenticate, message);
                                    OnStatusMessage("WARNING: Client {0} {1} command request denied - subscriber authentication failed.", connection.ConnectionID, ServerCommand.Authenticate);
                                }
                            }
                            else
                            {
                                message = "Not enough buffer was provided to parse client request.";
                                SendClientResponse(clientID, ServerResponse.Failed, ServerCommand.Authenticate, message);
                                OnProcessException(new InvalidOperationException(message));
                            }
                        }
                        else
                        {
                            message = string.Format("Received request packet with an unexpected size from {0} - {1} request denied.", connection.ConnectionID, ServerCommand.Authenticate);
                            SendClientResponse(clientID, ServerResponse.Failed, ServerCommand.Authenticate, message);
                            OnStatusMessage("WARNING: Registered subscriber \"{0}\" {1} {2} command request was denied due to oddly sized {3} byte authentication packet.", connection.SubscriberName, connection.ConnectionID, ServerCommand.Authenticate, byteLength);
                        }
                    }
                    else
                    {
                        message = "Not enough buffer was provided to parse client request.";
                        SendClientResponse(clientID, ServerResponse.Failed, ServerCommand.Authenticate, message);
                        OnProcessException(new InvalidOperationException(message));
                    }
                }
            }
            catch (Exception ex)
            {
                message = "Failed to process authentication request due to exception: " + ex.Message;
                SendClientResponse(clientID, ServerResponse.Failed, ServerCommand.Authenticate, message);
                OnProcessException(new InvalidOperationException(message, ex));
            }
        }

        // Handles subscribe request
        private void HandleSubscribeRequest(ClientConnection connection, byte[] buffer, int startIndex, int length)
        {
            Guid clientID = connection.ClientID;
            IClientSubscription subscription;
            string message, setting;

            // Handle subscribe
            try
            {
                // Make sure there is enough buffer for flags and connection string length
                if (length >= 6)
                {
                    // Next byte is the data packet flags
                    DataPacketFlags flags = (DataPacketFlags)buffer[startIndex];
                    startIndex++;

                    bool useSynchronizedSubscription = ((byte)(flags & DataPacketFlags.Synchronized) > 0);

                    if (useSynchronizedSubscription && !m_allowSynchronizedSubscription)
                    {
                        // Remotely synchronized subscriptions are currently disallowed by data publisher
                        message = "Client request for remotely synchronized data subscription was denied. Data publisher is currently configured to disallow synchronized subscriptions.";
                        SendClientResponse(clientID, ServerResponse.Failed, ServerCommand.Subscribe, message);
                        OnProcessException(new InvalidOperationException(message));
                    }
                    else
                    {
                        bool usePayloadCompression = m_allowPayloadCompression && ((connection.OperationalModes & OperationalModes.CompressPayloadData) > 0);
                        bool useCompactMeasurementFormat = ((byte)(flags & DataPacketFlags.Compact) > 0);
                        bool addSubscription = false;

                        // Next 4 bytes are an integer representing the length of the connection string that follows
                        int byteLength = BigEndian.ToInt32(buffer, startIndex);
                        startIndex += 4;

                        if (byteLength > 0 && length >= 6 + byteLength)
                        {
                            string connectionString = GetClientEncoding(clientID).GetString(buffer, startIndex, byteLength);
                            //startIndex += byteLength;

                            // Get client subscription
                            if ((object)connection.Subscription == null)
                                TryGetClientSubscription(clientID, out subscription);
                            else
                                subscription = connection.Subscription;

                            if ((object)subscription == null)
                            {
                                // Client subscription not established yet, so we create a new one
                                if (useSynchronizedSubscription)
                                    subscription = new SynchronizedClientSubscription(this, clientID, connection.SubscriberID);
                                else
                                    subscription = new UnsynchronizedClientSubscription(this, clientID, connection.SubscriberID);

                                addSubscription = true;
                            }
                            else
                            {
                                // Check to see if consumer is requesting to change synchronization method
                                if (useSynchronizedSubscription)
                                {
                                    if (subscription is UnsynchronizedClientSubscription)
                                    {
                                        // Subscription is for unsynchronized measurements and consumer is requesting synchronized
                                        subscription.Stop();

                                        lock (this)
                                        {
                                            Remove(subscription);
                                        }

                                        // Create a new synchronized subscription
                                        subscription = new SynchronizedClientSubscription(this, clientID, connection.SubscriberID);
                                        addSubscription = true;
                                    }
                                }
                                else
                                {
                                    if (subscription is SynchronizedClientSubscription)
                                    {
                                        // Subscription is for synchronized measurements and consumer is requesting unsynchronized
                                        subscription.Stop();

                                        lock (this)
                                        {
                                            Remove(subscription);
                                        }

                                        // Create a new unsynchronized subscription
                                        subscription = new UnsynchronizedClientSubscription(this, clientID, connection.SubscriberID);
                                        addSubscription = true;
                                    }
                                }
                            }

                            // Update client subscription properties
                            subscription.ConnectionString = connectionString;
                            subscription.DataSource = DataSource;

                            // Pass subscriber assembly information to connection, if defined
                            if (subscription.Settings.TryGetValue("assemblyInfo", out setting))
                                connection.SubscriberInfo = setting;

                            // Set up UDP data channel if client has requested this
                            connection.DataChannel = null;

                            if (subscription.Settings.TryGetValue("dataChannel", out setting))
                            {
                                Socket clientSocket = connection.GetCommandChannelSocket();
                                Dictionary<string, string> settings = setting.ParseKeyValuePairs();
                                IPEndPoint localEndPoint = null;
                                string networkInterface = "::0";

                                // Make sure return interface matches incoming client connection
                                if ((object)clientSocket != null)
                                    localEndPoint = clientSocket.LocalEndPoint as IPEndPoint;

                                if ((object)localEndPoint != null)
                                {
                                    networkInterface = localEndPoint.Address.ToString();

                                    // Remove dual-stack prefix
                                    if (networkInterface.StartsWith("::ffff:", true, CultureInfo.InvariantCulture))
                                        networkInterface = networkInterface.Substring(7);
                                }

                                if (settings.TryGetValue("port", out setting) || settings.TryGetValue("localport", out setting))
                                {
                                    connection.DataChannel = new UdpServer(string.Format("Port=-1; Clients={0}:{1}; interface={2}", connection.IPAddress, int.Parse(setting), networkInterface));
                                    connection.DataChannel.Start();
                                }
                            }

                            // Remove any existing cached publication channel since connection is changing
                            IServer publicationChannel;
                            m_clientPublicationChannels.TryRemove(clientID, out publicationChannel);

                            // Update payload compression state and strength
                            subscription.UsePayloadCompression = usePayloadCompression;
                            subscription.CompressionStrength = m_compressionStrength;

                            // Update measurement serialization format type
                            subscription.UseCompactMeasurementFormat = useCompactMeasurementFormat;

                            // Track subscription in connection information
                            connection.Subscription = subscription;

                            // Subscribed signals (i.e., input measurement keys) will be parsed from connection string during
                            // initialization of adapter. This should also gracefully handle "resubscribing" which can add and
                            // remove subscribed points since assignment and use of input measurement keys is synchronized
                            // within the client subscription class
                            if (addSubscription)
                            {
                                // Adding client subscription to collection will automatically initialize it
                                lock (this)
                                {
                                    Add(subscription);
                                }

                                // Attach to processing completed notification
                                subscription.BufferBlockRetransmission += subscription_BufferBlockRetransmission;
                                subscription.ProcessingComplete += subscription_ProcessingComplete;
                            }

                            // Make sure temporal support is initialized
                            OnRequestTemporalSupport();

                            // Manually initialize client subscription
                            subscription.Initialize();
                            subscription.Initialized = true;

                            // Send updated signal index cache to client with validated rights of the selected input measurement keys
                            byte[] serializedSignalIndexCache = SerializeSignalIndexCache(clientID, subscription.SignalIndexCache);
                            SendClientResponse(clientID, ServerResponse.UpdateSignalIndexCache, ServerCommand.Subscribe, serializedSignalIndexCache);

                            // Spawn routing table recalculation
                            m_routingTables.CalculateRoutingTables(null);

                            // Make sure adapter is started
                            subscription.Start();

                            // Send new or updated cipher keys
                            if (connection.Authenticated && m_encryptPayload)
                                connection.RotateCipherKeys();

                            // If client has subscribed to any cached measurements, queue them up for the client
                            IActionAdapter adapter;
                            LatestMeasurementCache cache;

                            if (TryGetAdapterByName("LatestMeasurementCache", out adapter))
                            {
                                cache = adapter as LatestMeasurementCache;

                                if ((object)cache != null)
                                {
                                    IEnumerable<IMeasurement> cachedMeasurements = cache.LatestMeasurements.Where(measurement => subscription.InputMeasurementKeys.Any(key => key.SignalID == measurement.ID));
                                    subscription.QueueMeasurementsForProcessing(cachedMeasurements);
                                }
                            }

                            // Notify any direct publisher consumers about the new client connection
                            try
                            {
                                OnClientConnected(connection.SubscriberID, connection.ConnectionID, connection.SubscriberInfo);
                            }
                            catch (Exception ex)
                            {
                                OnProcessException(new InvalidOperationException(string.Format("ClientConnected event handler exception: {0}", ex.Message), ex));
                            }

                            // Send success response
                            if (subscription.TemporalConstraintIsDefined())
                            {
                                if ((object)subscription.InputMeasurementKeys != null)
                                    message = string.Format("Client subscribed as {0}compact {1}synchronized with a temporal constraint and {2} signals.", useCompactMeasurementFormat ? "" : "non-", useSynchronizedSubscription ? "" : "un", subscription.InputMeasurementKeys.Length);
                                else
                                    message = string.Format("Client subscribed as {0}compact {1}synchronized with a temporal constraint, but no signals were specified. Make sure \"inputMeasurementKeys\" setting is properly defined.", useCompactMeasurementFormat ? "" : "non-", useSynchronizedSubscription ? "" : "un");
                            }
                            else
                            {
                                if ((object)subscription.InputMeasurementKeys != null)
                                    message = string.Format("Client subscribed as {0}compact {1}synchronized with {2} signals.", useCompactMeasurementFormat ? "" : "non-", useSynchronizedSubscription ? "" : "un", subscription.InputMeasurementKeys.Length);
                                else
                                    message = string.Format("Client subscribed as {0}compact {1}synchronized, but no signals were specified. Make sure \"inputMeasurementKeys\" setting is properly defined.", useCompactMeasurementFormat ? "" : "non-", useSynchronizedSubscription ? "" : "un");
                            }

                            connection.IsSubscribed = true;
                            SendClientResponse(clientID, ServerResponse.Succeeded, ServerCommand.Subscribe, message);
                            OnStatusMessage(message);
                        }
                        else
                        {
                            if (byteLength > 0)
                                message = "Not enough buffer was provided to parse client data subscription.";
                            else
                                message = "Cannot initialize client data subscription without a connection string.";

                            SendClientResponse(clientID, ServerResponse.Failed, ServerCommand.Subscribe, message);
                            OnProcessException(new InvalidOperationException(message));
                        }
                    }
                }
                else
                {
                    message = "Not enough buffer was provided to parse client data subscription.";
                    SendClientResponse(clientID, ServerResponse.Failed, ServerCommand.Subscribe, message);
                    OnProcessException(new InvalidOperationException(message));
                }
            }
            catch (Exception ex)
            {
                message = "Failed to process client data subscription due to exception: " + ex.Message;
                SendClientResponse(clientID, ServerResponse.Failed, ServerCommand.Subscribe, message);
                OnProcessException(new InvalidOperationException(message, ex));
            }
        }

        // Handles unsubscribe request
        private void HandleUnsubscribeRequest(ClientConnection connection)
        {
            Guid clientID = connection.ClientID;

            RemoveClientSubscription(clientID); // This does not disconnect client command channel - nor should it...

            // Detach from processing completed notification
            if ((object)connection.Subscription != null)
            {
                connection.Subscription.BufferBlockRetransmission -= subscription_BufferBlockRetransmission;
                connection.Subscription.ProcessingComplete -= subscription_ProcessingComplete;
            }

            connection.Subscription = null;
            connection.IsSubscribed = false;

            SendClientResponse(clientID, ServerResponse.Succeeded, ServerCommand.Unsubscribe, "Client unsubscribed.");
            OnStatusMessage(connection.ConnectionID + " unsubscribed.");
        }

        // Handles meta-data refresh request
        private void HandleMetadataRefresh(ClientConnection connection, byte[] buffer, int startIndex, int length)
        {
            // Ensure that the subscriber is allowed to request meta-data
            if (!m_allowMetadataRefresh)
                throw new InvalidOperationException("Meta-data refresh has been disallowed by the DataPublisher.");

            OnStatusMessage("Received meta-data refresh request from {0}, preparing response...", connection.ConnectionID);

            Guid clientID = connection.ClientID;
            Dictionary<string, Tuple<string, string, int>> filterExpressions = new Dictionary<string, Tuple<string, string, int>>(StringComparer.OrdinalIgnoreCase);
            string message, tableName, filterExpression, sortField;
            int takeCount;
            Ticks startTime = DateTime.UtcNow.Ticks;

            // Attempt to parse out any subscriber provided meta-data filter expressions
            try
            {
                // Note that these client provided meta-data filter expressions are applied post SQL data retrieval to the limited-capability 
                // DataTable.Select() function against an in-memory DataSet and therefore are not subject to SQL injection attacks
                if (length > 4)
                {
                    int responseLength = BigEndian.ToInt32(buffer, startIndex);
                    startIndex += 4;

                    if (length >= responseLength + 4)
                    {
                        string metadataFilters = GetClientEncoding(clientID).GetString(buffer, startIndex, responseLength);
                        string[] expressions = metadataFilters.Split(';');

                        // Go through each subscriber specified filter expressions
                        foreach (string expression in expressions)
                        {
                            // Attempt to parse filter expression and add it dictionary if successful
                            if (AdapterBase.ParseFilterExpression(expression, out tableName, out filterExpression, out sortField, out takeCount))
                                filterExpressions.Add(tableName, Tuple.Create(filterExpression, sortField, takeCount));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OnProcessException(new InvalidOperationException("Failed to parse subscriber provided meta-data filter expressions: " + ex.Message, ex));
            }

            try
            {
                using (AdoDataConnection adoDatabase = new AdoDataConnection("systemSettings"))
                {
                    IDbConnection dbConnection = adoDatabase.Connection;
                    DataSet metadata = new DataSet();
                    DataTable table;
                    byte[] serializedMetadata;
                    Tuple<string, string, int> filterParameters;

                    // Initialize active node ID
                    Guid nodeID = Guid.Parse(dbConnection.ExecuteScalar(string.Format("SELECT NodeID FROM IaonActionAdapter WHERE ID = {0}", ID)).ToString());

                    // Determine whether we're sending internal and external meta-data
                    bool sendExternalMetadata = connection.OperationalModes.HasFlag(OperationalModes.ReceiveExternalMetadata);
                    bool sendInternalMetadata = connection.OperationalModes.HasFlag(OperationalModes.ReceiveInternalMetadata);

                    if (!sendExternalMetadata && !sendInternalMetadata)
                    {
                        // Force the client to receive metadata if they have specified that they don't want any
                        sendExternalMetadata = m_forceReceiveMetadataFlags.HasFlag(OperationalModes.ReceiveExternalMetadata);
                        sendInternalMetadata = m_forceReceiveMetadataFlags.HasFlag(OperationalModes.ReceiveInternalMetadata);
                    }

                    // Copy key meta-data tables
                    foreach (string tableExpression in m_metadataTables.Split(';'))
                    {
                        if (string.IsNullOrWhiteSpace(tableExpression))
                            continue;

                        // Query the table or view information from the database
                        table = dbConnection.RetrieveData(adoDatabase.AdapterType, tableExpression);

                        // Remove any expression from table name
                        Match regexMatch = Regex.Match(tableExpression, @"FROM \w+");
                        table.TableName = regexMatch.Value.Split(' ')[1];

                        sortField = "";
                        takeCount = int.MaxValue;

                        // Build filter list
                        List<string> filters = new List<string>();

                        if (table.Columns.Contains("NodeID"))
                            filters.Add(string.Format("NodeID = '{0}'", nodeID));

                        if (table.Columns.Contains("Internal") && !(sendInternalMetadata && sendExternalMetadata))
                            filters.Add(string.Format("Internal {0} 0", sendExternalMetadata ? "=" : "<>"));

                        if (table.Columns.Contains("OriginalSource") && !(sendInternalMetadata && sendExternalMetadata))
                            filters.Add(string.Format("OriginalSource IS {0} NULL", sendExternalMetadata ? "NOT" : ""));

                        if (filterExpressions.TryGetValue(table.TableName, out filterParameters))
                        {
                            filters.Add("(" + filterParameters.Item1 + ")");
                            sortField = filterParameters.Item2;
                            takeCount = filterParameters.Item3;
                        }

                        // Determine whether we need to check subscriber for rights to the data
                        bool checkSubscriberRights = RequireAuthentication && table.Columns.Contains("SignalID");

                        if (m_sharedDatabase || (filters.Count == 0 && !checkSubscriberRights))
                        {
                            // Add a copy of the results to the dataset for meta-data exchange
                            metadata.Tables.Add(table.Copy());
                        }
                        else
                        {
                            IEnumerable<DataRow> filteredRows;
                            List<DataRow> filteredRowList;

                            // Make a copy of the table structure
                            metadata.Tables.Add(table.Clone());

                            // Filter in-memory data table down to desired rows
                            filteredRows = table.Select(string.Join(" AND ", filters), sortField);

                            // Reduce data to only what the subscriber has rights to
                            if (checkSubscriberRights)
                                filteredRows = filteredRows.Where(row => SubscriberHasRights(connection.SubscriberID, adoDatabase.Guid(row, "SignalID")));

                            // Apply any maximum row count that user may have specified
                            filteredRowList = filteredRows.Take(takeCount).ToList();

                            if (filteredRowList.Count > 0)
                            {
                                DataTable metadataTable = metadata.Tables[table.TableName];

                                // Manually copy-in each row into table
                                foreach (DataRow row in filteredRowList)
                                {
                                    DataRow newRow = metadataTable.NewRow();

                                    // Copy each column of data in the current row
                                    for (int x = 0; x < table.Columns.Count; x++)
                                    {
                                        newRow[x] = row[x];
                                    }

                                    metadataTable.Rows.Add(newRow);
                                }
                            }
                        }
                    }

                    // TODO: Although protected against unprovided tables and columns, this post-analysis operation is schema specific. This may need to be moved to an external function and executed via delegate to allow this kind of work for other schemas.

                    // Do some post analysis on the meta-data to be delivered to the client, e.g., if a device exists with no associated measurements - don't send the device.
                    if (metadata.Tables.Contains("MeasurementDetail") && metadata.Tables["MeasurementDetail"].Columns.Contains("DeviceAcronym") && metadata.Tables.Contains("DeviceDetail") && metadata.Tables["DeviceDetail"].Columns.Contains("Acronym"))
                    {
                        List<DataRow> rowsToRemove = new List<DataRow>();
                        string deviceAcronym;

                        // Remove device records where no associated measurements records exist
                        foreach (DataRow row in metadata.Tables["DeviceDetail"].Rows)
                        {
                            deviceAcronym = row["Acronym"].ToNonNullString();

                            if (!string.IsNullOrEmpty(deviceAcronym) && (int)metadata.Tables["MeasurementDetail"].Compute("Count(DeviceAcronym)", string.Format("DeviceAcronym = '{0}'", deviceAcronym)) == 0)
                                rowsToRemove.Add(row);
                        }

                        if (metadata.Tables.Contains("PhasorDetail") && metadata.Tables["PhasorDetail"].Columns.Contains("DeviceAcronym"))
                        {
                            // Remove phasor records where no associated device records exist
                            foreach (DataRow row in metadata.Tables["PhasorDetail"].Rows)
                            {
                                deviceAcronym = row["DeviceAcronym"].ToNonNullString();

                                if (!string.IsNullOrEmpty(deviceAcronym) && (int)metadata.Tables["DeviceDetail"].Compute("Count(Acronym)", string.Format("Acronym = '{0}'", deviceAcronym)) == 0)
                                    rowsToRemove.Add(row);
                            }
                        }

                        // Remove any unnecessary rows
                        foreach (DataRow row in rowsToRemove)
                            row.Delete();
                    }

                    serializedMetadata = SerializeMetadata(clientID, metadata);

                    long rowCount = metadata.Tables.Cast<DataTable>().Select(dataTable => (long)dataTable.Rows.Count).Sum();

                    if (rowCount > 0)
                    {
                        Time elapsedTime = (DateTime.UtcNow.Ticks - startTime).ToSeconds();
                        OnStatusMessage("{0:N0} records spanning {1:N0} tables of meta-data prepared in {2}, sending response to {3}...", rowCount, metadata.Tables.Count, elapsedTime.ToString(2), connection.ConnectionID);
                    }
                    else
                    {
                        OnStatusMessage("No meta-data is available, sending an empty response to {0}...", connection.ConnectionID);
                    }

                    SendClientResponse(clientID, ServerResponse.Succeeded, ServerCommand.MetaDataRefresh, serializedMetadata);
                }
            }
            catch (Exception ex)
            {
                message = "Failed to transfer meta-data due to exception: " + ex.Message;
                SendClientResponse(clientID, ServerResponse.Failed, ServerCommand.MetaDataRefresh, message);
                OnProcessException(new InvalidOperationException(message, ex));
            }
        }

        // Handles request to update processing interval on client session
        private void HandleUpdateProcessingInterval(ClientConnection connection, byte[] buffer, int startIndex, int length)
        {
            Guid clientID = connection.ClientID;
            string message;

            // Make sure there is enough buffer for new processing interval value
            if (length >= 4)
            {
                // Next 4 bytes are an integer representing the new processing interval
                int processingInterval = BigEndian.ToInt32(buffer, startIndex);

                IClientSubscription subscription = connection.Subscription;

                if ((object)subscription != null)
                {
                    subscription.ProcessingInterval = processingInterval;
                    SendClientResponse(clientID, ServerResponse.Succeeded, ServerCommand.UpdateProcessingInterval, "New processing interval of {0} assigned.", processingInterval);
                    OnStatusMessage("{0} was assigned a new processing interval of {1}.", connection.ConnectionID, processingInterval);
                }
                else
                {
                    message = "Client subscription was not available, could not update processing interval.";
                    SendClientResponse(clientID, ServerResponse.Failed, ServerCommand.UpdateProcessingInterval, message);
                    OnProcessException(new InvalidOperationException(message));
                }
            }
            else
            {
                message = "Not enough buffer was provided to update client processing interval.";
                SendClientResponse(clientID, ServerResponse.Failed, ServerCommand.UpdateProcessingInterval, message);
                OnProcessException(new InvalidOperationException(message));
            }
        }

        // Handle request to define operational modes for client connection
        private void HandleDefineOperationalModes(ClientConnection connection, byte[] buffer, int startIndex, int length)
        {
            uint operationalModes;

            if (length >= 4)
            {
                operationalModes = BigEndian.ToUInt32(buffer, startIndex);

                if ((operationalModes & (uint)OperationalModes.VersionMask) != 0u)
                    OnStatusMessage("WARNING: Protocol version not supported. Operational modes may not be set correctly for client {0}.", connection.ClientID);

                connection.OperationalModes = (OperationalModes)operationalModes;
            }
        }

        // Handle confirmation of receipt of notification
        private void HandleConfirmNotification(ClientConnection connection, byte[] buffer, int startIndex, int length)
        {
            int hash = BigEndian.ToInt32(buffer, startIndex);
            Dictionary<int, string> notifications;
            string notification;

            if (length >= 4)
            {
                lock (m_clientNotificationsLock)
                {
                    if (m_clientNotifications.TryGetValue(connection.SubscriberID, out notifications))
                    {
                        if (notifications.TryGetValue(hash, out notification))
                        {
                            notifications.Remove(hash);
                            OnStatusMessage("Subscriber {0} confirmed receipt of notification: {1}.", connection.ConnectionID, notification);
                            SerializeClientNotifications();
                        }
                        else
                        {
                            OnStatusMessage("Confirmation for unknown notification received from client {0}.", connection.ConnectionID);
                        }
                    }
                    else
                    {
                        OnStatusMessage("Unsolicited confirmation of notification received.");
                    }
                }
            }
            else
            {
                OnStatusMessage("Malformed notification confirmation received.");
            }
        }

        private void HandleConfirmBufferBlock(ClientConnection connection, byte[] buffer, int startIndex, int length)
        {
            uint sequenceNumber;

            if (length >= 4)
            {
                sequenceNumber = BigEndian.ToUInt32(buffer, startIndex);
                connection.Subscription.ConfirmBufferBlock(sequenceNumber);
            }
        }

        private byte[] SerializeSignalIndexCache(Guid clientID, SignalIndexCache signalIndexCache)
        {
            byte[] serializedSignalIndexCache = null;
            ClientConnection connection;

            if (m_clientConnections.TryGetValue(clientID, out connection))
            {
                OperationalModes operationalModes = connection.OperationalModes;
                GatewayCompressionMode gatewayCompressionMode = (GatewayCompressionMode)(operationalModes & OperationalModes.CompressionModeMask);
                bool useCommonSerializationFormat = (operationalModes & OperationalModes.UseCommonSerializationFormat) > 0;
                bool compressSignalIndexCache = (operationalModes & OperationalModes.CompressSignalIndexCache) > 0;

                GZipStream deflater = null;

                if (!useCommonSerializationFormat)
                {
                    // Use standard .NET BinaryFormatter
                    serializedSignalIndexCache = Serialization.Serialize(signalIndexCache, SerializationFormat.Binary);
                }
                else
                {
                    // Use ISupportBinaryImage implementation
                    signalIndexCache.Encoding = GetClientEncoding(clientID);
                    serializedSignalIndexCache = new byte[signalIndexCache.BinaryLength];
                    signalIndexCache.GenerateBinaryImage(serializedSignalIndexCache, 0);
                }

                if (compressSignalIndexCache && gatewayCompressionMode == GatewayCompressionMode.GZip)
                {
                    try
                    {
                        // Compress serialized signal index cache into compressed data buffer
                        using (BlockAllocatedMemoryStream compressedData = new BlockAllocatedMemoryStream())
                        {
                            deflater = new GZipStream(compressedData, CompressionMode.Compress, true);
                            deflater.Write(serializedSignalIndexCache, 0, serializedSignalIndexCache.Length);
                            deflater.Close();
                            deflater = null;

                            serializedSignalIndexCache = compressedData.ToArray();
                        }
                    }
                    finally
                    {
                        if ((object)deflater != null)
                            deflater.Close();
                    }
                }
            }

            return serializedSignalIndexCache;
        }

        private byte[] SerializeMetadata(Guid clientID, DataSet metadata)
        {
            byte[] serializedMetadata = null;
            ClientConnection connection;

            if (m_clientConnections.TryGetValue(clientID, out connection))
            {
                OperationalModes operationalModes = connection.OperationalModes;
                GatewayCompressionMode gatewayCompressionMode = (GatewayCompressionMode)(operationalModes & OperationalModes.CompressionModeMask);
                bool useCommonSerializationFormat = (operationalModes & OperationalModes.UseCommonSerializationFormat) > 0;
                bool compressMetadata = (operationalModes & OperationalModes.CompressMetadata) > 0;

                GZipStream deflater = null;

                if (!useCommonSerializationFormat)
                {
                    serializedMetadata = Serialization.Serialize(metadata, SerializationFormat.Binary);
                }
                else
                {
                    // Encode XML into encoded data buffer
                    using (BlockAllocatedMemoryStream encodedData = new BlockAllocatedMemoryStream())
                    using (XmlTextWriter xmlWriter = new XmlTextWriter(encodedData, GetClientEncoding(clientID)))
                    {
                        metadata.WriteXml(xmlWriter, XmlWriteMode.WriteSchema);
                        xmlWriter.Flush();

                        // Return result of encoding
                        serializedMetadata = encodedData.ToArray();
                    }
                }

                if (compressMetadata && gatewayCompressionMode == GatewayCompressionMode.GZip)
                {
                    try
                    {
                        // Compress serialized metadata into compressed data buffer
                        using (BlockAllocatedMemoryStream compressedData = new BlockAllocatedMemoryStream())
                        {
                            deflater = new GZipStream(compressedData, CompressionMode.Compress, true);
                            deflater.Write(serializedMetadata, 0, serializedMetadata.Length);
                            deflater.Close();
                            deflater = null;

                            // Return result of compression
                            serializedMetadata = compressedData.ToArray();
                        }
                    }
                    finally
                    {
                        if ((object)deflater != null)
                            deflater.Close();
                    }
                }

            }

            return serializedMetadata;
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

        private void subscription_BufferBlockRetransmission(object sender, EventArgs eventArgs)
        {
            m_bufferBlockRetransmissions++;
        }

        // Bubble up processing complete notifications from subscriptions
        private void subscription_ProcessingComplete(object sender, EventArgs<IClientSubscription, EventArgs> e)
        {
            // Expose notification via data publisher event subscribers
            if ((object)ProcessingComplete != null)
                ProcessingComplete(sender, e.Argument2);

            IClientSubscription subscription = e.Argument1;
            string senderType = (object)sender == null ? "N/A" : sender.GetType().Name;

            // Send direct notification to associated client
            if ((object)subscription != null)
                SendClientResponse(subscription.ClientID, ServerResponse.ProcessingComplete, ServerCommand.Subscribe, senderType);
        }

        #endregion

        #region [ Command Channel Handlers ]

        private void m_commandChannel_ReceiveClientDataComplete(object sender, EventArgs<Guid, byte[], int> e)
        {
            try
            {
                Guid clientID = e.Argument1;
                byte[] buffer = e.Argument2;
                int length = e.Argument3;
                int index = 0;

                if (length > 0 && (object)buffer != null)
                {
                    ClientConnection connection;
                    ServerCommand command;
                    string message;
                    byte commandByte = buffer[index];
                    index++;

                    // Attempt to parse solicited server command
                    bool validServerCommand = Enum.TryParse(commandByte.ToString(), out command);

                    // Look up this client connection
                    if (!m_clientConnections.TryGetValue(clientID, out connection))
                    {
                        // Received a request from an unknown client, this request is denied
                        OnStatusMessage("WARNING: Ignored {0} byte {1} command request received from an unrecognized client: {2}", length, validServerCommand ? command.ToString() : "unidentified", clientID);
                    }
                    else if (validServerCommand)
                    {
                        if (command != ServerCommand.DefineOperationalModes)
                        {
                            if (command == ServerCommand.Authenticate)
                            {
                                // Handle authenticate
                                HandleAuthenticationRequest(connection, buffer, index, length);
                                return;
                            }

                            if (RequireAuthentication && !connection.Authenticated)
                            {
                                message = string.Format("Subscriber not authenticated - {0} request denied.", command);
                                SendClientResponse(clientID, ServerResponse.Failed, command, message);
                                OnStatusMessage("WARNING: Client {0} {1} command request denied - subscriber not authenticated.", connection.ConnectionID, command);
                                return;
                            }
                        }

                        switch (command)
                        {
                            case ServerCommand.Subscribe:
                                // Handle subscribe
                                HandleSubscribeRequest(connection, buffer, index, length);
                                break;
                            case ServerCommand.Unsubscribe:
                                // Handle unsubscribe
                                HandleUnsubscribeRequest(connection);
                                break;
                            case ServerCommand.MetaDataRefresh:
                                // Handle meta data refresh (per subscriber request)
                                HandleMetadataRefresh(connection, buffer, index, length);
                                break;
                            case ServerCommand.RotateCipherKeys:
                                // Handle rotation of cipher keys (per subscriber request)
                                connection.RotateCipherKeys();
                                break;
                            case ServerCommand.UpdateProcessingInterval:
                                // Handle request to update processing interval
                                HandleUpdateProcessingInterval(connection, buffer, index, length);
                                break;
                            case ServerCommand.DefineOperationalModes:
                                // Handle request to define operational modes
                                HandleDefineOperationalModes(connection, buffer, index, length);
                                break;
                            case ServerCommand.ConfirmNotification:
                                // Handle confirmation of receipt of notification
                                HandleConfirmNotification(connection, buffer, index, length);
                                break;
                            case ServerCommand.ConfirmBufferBlock:
                                // Handle confirmation of receipt of a buffer block
                                HandleConfirmBufferBlock(connection, buffer, index, length);
                                break;
                        }
                    }
                    else
                    {
                        // Handle unrecognized commands
                        message = " sent an unrecognized server command: 0x" + commandByte.ToString("X").PadLeft(2, '0');
                        SendClientResponse(null, clientID, (byte)ServerResponse.Failed, commandByte, GetClientEncoding(clientID).GetBytes("Client" + message));
                        OnProcessException(new InvalidOperationException("WARNING: " + connection.ConnectionID + message));
                    }
                }
            }
            catch (Exception ex)
            {
                OnProcessException(new InvalidOperationException(string.Format("Encountered an exception while processing received client data: {0}", ex.Message), ex));
            }
        }

        private void m_commandChannel_ClientConnected(object sender, EventArgs<Guid> e)
        {
            Guid clientID = e.Argument;
            ClientConnection connection = new ClientConnection(this, clientID, m_commandChannel);

            if (m_securityMode == SecurityMode.TLS)
            {
                TryFindClientDetails(connection);
                connection.Authenticated = connection.ValidIPAddresses.Contains(connection.IPAddress);
            }

            m_clientConnections[clientID] = connection;

            OnStatusMessage("Client connected to command channel.");

            if (connection.Authenticated)
            {
                lock (m_clientNotificationsLock)
                {
                    // Send any queued notifications to authenticated client
                    SendNotifications(connection);
                }
            }
            else if (m_securityMode == SecurityMode.TLS)
            {
                const string ErrorFormat = "Unable to authenticate client. Client connected using" +
                    " certificate of subscriber \"{0}\", however the IP address used ({1}) was" +
                    " not found among the list of valid IP addresses.";

                string errorMessage = string.Format(ErrorFormat, connection.SubscriberName, connection.IPAddress);

                OnProcessException(new InvalidOperationException(errorMessage));
            }
        }

        private void m_commandChannel_ClientDisconnected(object sender, EventArgs<Guid> e)
        {
            try
            {
                ThreadPool.QueueUserWorkItem(DisconnectClient, e.Argument);
            }
            catch (Exception ex)
            {
                // Process exception for logging
                OnProcessException(new InvalidOperationException("Failed to queue client disconnect due to exception: " + ex.Message, ex));
            }
        }

        private void m_commandChannel_ClientConnectingException(object sender, EventArgs<Exception> e)
        {
            Exception ex = e.Argument;
            OnProcessException(new InvalidOperationException("Data publisher encountered an exception while connecting client to the command channel: " + ex.Message, ex));
        }

        private void m_commandChannel_ServerStarted(object sender, EventArgs e)
        {
            OnStatusMessage("Data publisher command channel started.");
        }

        private void m_commandChannel_ServerStopped(object sender, EventArgs e)
        {
            if (Enabled)
            {
                OnStatusMessage("Data publisher command channel was unexpectedly terminated, restarting...");

                // We must wait for command channel to completely shutdown before trying to restart...
                if ((object)m_commandChannelRestartTimer != null)
                    m_commandChannelRestartTimer.Start();
            }
            else
            {
                OnStatusMessage("Data publisher command channel stopped.");
            }
        }

        private void m_commandChannel_SendClientDataException(object sender, EventArgs<Guid, Exception> e)
        {
            Exception ex = e.Argument2;

            if (!HandleSocketException(e.Argument1, ex) && !(ex is NullReferenceException) && !(ex is ObjectDisposedException))
                OnProcessException(new InvalidOperationException("Data publisher encountered an exception while sending command channel data to client connection: " + ex.Message, ex));
        }

        private void m_commandChannel_ReceiveClientDataException(object sender, EventArgs<Guid, Exception> e)
        {
            Exception ex = e.Argument2;

            if (!HandleSocketException(e.Argument1, ex) && !(ex is NullReferenceException) && !(ex is ObjectDisposedException))
                OnProcessException(new InvalidOperationException("Data publisher encountered an exception while receiving command channel data from client connection: " + ex.Message, ex));
        }

        #endregion

        #endregion

        #region [ Static ]

        // Static Fields

        // Constant zero length integer byte array
        private readonly static byte[] ZeroLengthBytes = { 0, 0, 0, 0 };

        #endregion
    }
}
