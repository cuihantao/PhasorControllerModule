﻿//******************************************************************************************************
//  MeasurementKey.cs - Gbtc
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
//  09/02/2010 - J. Ritchie Carroll
//       Generated original version of source code.
//  12/20/2012 - Starlynn Danyelle Gilliam
//       Modified Header.
//
//******************************************************************************************************

using System;
using System.Collections.Concurrent;
using System.Data;
using GSF.Data;

namespace GSF.TimeSeries
{
    /// <summary>
    /// Represents a primary key for a measurement.
    /// </summary>
    /// <remarks>
    /// In the Static section of this class, all edits to <see cref="IDCache"/> as well as the
    /// ConcurrentDictionaries in <see cref="KeyCache"/> should occur within a lock on SyncEdits.
    /// </remarks>
    [Serializable]
    public class MeasurementKey
    {
        #region [ Members ]

        // Fields
        private readonly Guid m_signalID;
        private uint m_id;
        private string m_source;
        private readonly int m_hashCode;

        #endregion

        #region [ Constructors ]

        private MeasurementKey(Guid signalID, uint id, string source)
        {
            m_signalID = signalID;
            m_id = id;
            m_source = source;
            m_hashCode = base.GetHashCode();
        }

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets or sets <see cref="Guid"/> ID of signal associated with this <see cref="MeasurementKey"/>.
        /// </summary>
        public Guid SignalID
        {
            get
            {
                return m_signalID;
            }
        }

        /// <summary>
        /// Gets or sets the numeric ID of this <see cref="MeasurementKey"/>.
        /// </summary>
        public uint ID
        {
            get
            {
                return m_id;
            }
        }

        /// <summary>
        /// Gets or sets the source of this <see cref="MeasurementKey"/>.
        /// </summary>
        /// <remarks>
        /// This value is typically used to track the archive name in which the measurement, that this <see cref="MeasurementKey"/> represents, is stored.
        /// </remarks>
        public string Source
        {
            get
            {
                return m_source;
            }
        }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Returns a <see cref="String"/> that represents the current <see cref="MeasurementKey"/>.
        /// </summary>
        /// <returns>A <see cref="String"/> that represents the current <see cref="MeasurementKey"/>.</returns>
        public override string ToString()
        {
            return string.Format("{0}:{1}", m_source, m_id);
        }

        /// <summary>
        /// Serves as a hash function for the current <see cref="MeasurementKey"/>.
        /// </summary>
        /// <returns>A hash code for the current <see cref="MeasurementKey"/>.</returns>
        public override int GetHashCode()
        {
            return m_hashCode;
        }

        #endregion

        #region [ Static ]

        // Static Fields
        private static readonly ConcurrentDictionary<Guid, MeasurementKey> IDCache = new ConcurrentDictionary<Guid, MeasurementKey>();
        private static readonly ConcurrentDictionary<string, ConcurrentDictionary<uint, MeasurementKey>> KeyCache = new ConcurrentDictionary<string, ConcurrentDictionary<uint, MeasurementKey>>(StringComparer.OrdinalIgnoreCase);
        private static readonly object SyncEdits = new object();

        /// <summary>
        /// Represents an undefined measurement key.
        /// </summary>
        public static readonly MeasurementKey Undefined = CreateUndefinedMeasurementKey();

        // Static Methods

        /// <summary>
        /// Constructs a new <see cref="MeasurementKey"/> given the specified parameters.
        /// </summary>
        /// <param name="signalID"><see cref="Guid"/> ID of associated signal, if defined.</param>
        /// <param name="value">A string representation of the <see cref="MeasurementKey"/>.</param>
        /// <exception cref="ArgumentException"><paramref name="signalID"/> cannot be empty</exception>
        /// <exception cref="FormatException">The value is not in the correct format for a <see cref="MeasurementKey"/> value.</exception>
        public static MeasurementKey CreateOrUpdate(Guid signalID, string value)
        {
            string source;
            uint id;

            if (!TrySplit(value, out source, out id))
                throw new FormatException("The value is not in the correct format for a MeasurementKey value");

            return CreateOrUpdate(signalID, source, id);
        }

        /// <summary>
        /// Constructs a new <see cref="MeasurementKey"/> given the specified parameters.
        /// </summary>
        /// <param name="signalID"><see cref="Guid"/> ID of associated signal, if defined.</param>
        /// <param name="source">Source of the measurement that this <see cref="MeasurementKey"/> represents (e.g., name of archive).</param>
        /// <param name="id">Numeric ID of the measurement that this <see cref="MeasurementKey"/> represents.</param>
        /// <exception cref="ArgumentException"><paramref name="signalID"/> cannot be empty.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> cannot be null.</exception>
        public static MeasurementKey CreateOrUpdate(Guid signalID, string source, uint id)
        {
            Func<Guid, MeasurementKey> addValueFactory;
            Func<Guid, MeasurementKey, MeasurementKey> updateValueFactory;

            if (signalID == Guid.Empty)
                throw new ArgumentException("Unable to update undefined measurement key", "signalID");

            if (string.IsNullOrWhiteSpace(source))
                throw new ArgumentNullException("source", "MeasurementKey source cannot be null or empty");

            addValueFactory = guid =>
            {
                // Create a new measurement key and add it to the KeyCache
                ConcurrentDictionary<uint, MeasurementKey> idLookup = KeyCache.GetOrAdd(source, s => new ConcurrentDictionary<uint, MeasurementKey>());
                return idLookup[id] = new MeasurementKey(guid, id, source);
            };

            updateValueFactory = (guid, key) =>
            {
                ConcurrentDictionary<uint, MeasurementKey> idLookup;

                // If existing measurement key is exactly the same as the
                // one we are trying to create, simply return that key
                if (key.ID == id && key.Source == source)
                    return key;

                // Update source and ID and re-insert it into the KeyCache
                key.m_source = source;
                key.m_id = id;

                idLookup = KeyCache.GetOrAdd(source, s => new ConcurrentDictionary<uint, MeasurementKey>());
                idLookup[id] = key;

                return key;
            };

            // https://msdn.microsoft.com/en-us/library/ee378675(v=vs.110).aspx
            //
            //     If you call AddOrUpdate simultaneously on different threads,
            //     addValueFactory may be called multiple times, but its key/value
            //     pair might not be added to the dictionary for every call.
            //
            // This lock prevents race conditions that might occur in the addValueFactory that
            // could cause different MeasurementKey objects to be written to the KeyCache and IDCache
            lock (SyncEdits)
            {
                return IDCache.AddOrUpdate(signalID, addValueFactory, updateValueFactory);
            }
        }

        /// <summary>
        /// Constructs a new <see cref="MeasurementKey"/> given the specified parameters.
        /// </summary>
        /// <param name="signalID"><see cref="Guid"/> ID of associated signal, if defined.</param>
        /// <param name="value">A string representation of the <see cref="MeasurementKey"/>.</param>
        /// <param name="key">The measurement key that was created or updated or <see cref="Undefined"/>.</param>
        /// <returns>True if the measurement key was successfully created or updated, false otherwise.</returns>
        /// <exception cref="ArgumentException"><paramref name="signalID"/> cannot be empty.</exception>
        /// <exception cref="ArgumentNullException">Measurement key Source cannot be null.</exception>
        public static bool TryCreateOrUpdate(Guid signalID, string value, out MeasurementKey key)
        {
            try
            {
                key = CreateOrUpdate(signalID, value);
                return true;
            }
            catch
            {
                key = Undefined;
                return false;
            }
        }

        /// <summary>
        /// Constructs a new <see cref="MeasurementKey"/> given the specified parameters.
        /// </summary>
        /// <param name="signalID"><see cref="Guid"/> ID of associated signal, if defined.</param>
        /// <param name="source">Source of the measurement that this <see cref="MeasurementKey"/> represents (e.g., name of archive).</param>
        /// <param name="id">Numeric ID of the measurement that this <see cref="MeasurementKey"/> represents.</param>
        /// <param name="key">The measurement key that was created or updated or <see cref="Undefined"/>.</param>
        /// <returns>True if the measurement key was successfully created or updated, false otherwise.</returns>
        /// <exception cref="ArgumentException"><paramref name="signalID"/> cannot be empty.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> cannot be null.</exception>
        public static bool TryCreateOrUpdate(Guid signalID, string source, uint id, out MeasurementKey key)
        {
            try
            {
                key = CreateOrUpdate(signalID, source, id);
                return true;
            }
            catch
            {
                key = Undefined;
                return false;
            }
        }

        /// <summary>
        /// Looks up the measurement key associated with the given signal ID.
        /// </summary>
        /// <param name="signalID">The signal ID of the measurement key.</param>
        /// <returns>The measurement key associated with the given signal ID.</returns>
        /// <remarks>
        /// If no measurement key is found with the given signal ID,
        /// this method returns <see cref="Undefined"/>.
        /// </remarks>
        public static MeasurementKey LookUpBySignalID(Guid signalID)
        {
            MeasurementKey key;

            if (signalID == Guid.Empty)
                key = Undefined;
            else if (!IDCache.TryGetValue(signalID, out key))
                key = Undefined;

            return key;
        }

        /// <summary>
        /// Looks up the measurement key associated with the given source and ID.
        /// </summary>
        /// <param name="source">The source of the signal.</param>
        /// <param name="id">The source-specific unique integer identifier.</param>
        /// <returns>The measurement key associated with the given source and ID.</returns>
        /// <remarks>
        /// If no measurement key is found with the given source and ID,
        /// this method returns <see cref="Undefined"/>.
        /// </remarks>
        public static MeasurementKey LookUpBySource(string source, uint id)
        {
            ConcurrentDictionary<uint, MeasurementKey> idLookup;
            MeasurementKey key;

            if (!KeyCache.TryGetValue(source, out idLookup))
                return Undefined;

            if (!idLookup.TryGetValue(id, out key))
                return Undefined;

            return key;
        }

        /// <summary>
        /// Performs a lookup by signal ID and, failing that, attempts to create
        /// the key using the given signal ID and the parsed source, and ID.
        /// </summary>
        /// <param name="signalID">The signal ID of the key to be looked up.</param>
        /// <param name="value">A string representation of the <see cref="MeasurementKey"/>.</param>
        /// <returns>
        /// If the lookup succeeds, an existing measurement key with a matching signalID.
        /// If creation succeeds, a new measurement key with matching signal ID, source, and ID.
        /// Otherwise, <see cref="Undefined"/>.
        /// </returns>
        public static MeasurementKey LookUpOrCreate(Guid signalID, string value)
        {
            string source;
            uint id;

            if (!TrySplit(value, out source, out id))
                return LookUpOrCreate(signalID, Undefined.Source, Undefined.ID);

            return LookUpOrCreate(signalID, source, id);
        }

        /// <summary>
        /// Performs a lookup by signal ID and, failing that, attempts to
        /// create the key using the given signal ID, source, and ID.
        /// </summary>
        /// <param name="signalID">The signal ID of the key to be looked up.</param>
        /// <param name="source">The source to use for the key if the lookup fails.</param>
        /// <param name="id">The ID to use for the key if the lookup fails.</param>
        /// <returns>
        /// If the lookup succeeds, an existing measurement key with a matching signalID.
        /// If creation succeeds, a new measurement key with matching signal ID, source, and ID.
        /// Otherwise, <see cref="Undefined"/>.
        /// </returns>
        public static MeasurementKey LookUpOrCreate(Guid signalID, string source, uint id)
        {
            MeasurementKey key = LookUpBySignalID(signalID);

            if (key == Undefined && !TryCreateOrUpdate(signalID, source, id, out key))
                return Undefined;

            return key;
        }

        /// <summary>
        /// Performs a lookup by source and, failing that, attempts to create
        /// the key using a newly generated signal ID and the parsed source and ID.
        /// </summary>
        /// <param name="value">A string representation of the <see cref="MeasurementKey"/>.</param>
        /// <returns>
        /// If the lookup succeeds, an existing measurement key with a matching signalID.
        /// If creation succeeds, a new measurement key with matching signal ID, source, and ID.
        /// Otherwise, <see cref="Undefined"/>.
        /// </returns>
        public static MeasurementKey LookUpOrCreate(string value)
        {
            string source;
            uint id;

            if (!TrySplit(value, out source, out id))
                return Undefined;

            return LookUpOrCreate(source, id);
        }

        /// <summary>
        /// Performs a lookup by source and, failing that, attempts to create
        /// the key using a newly generated signal ID and the given source and ID.
        /// </summary>
        /// <param name="source">The source to use for the key if the lookup fails.</param>
        /// <param name="id">The ID to use for the key if the lookup fails.</param>
        /// <returns>
        /// If the lookup succeeds, an existing measurement key with a matching signalID.
        /// If creation succeeds, a new measurement key with matching signal ID, source, and ID.
        /// Otherwise, <see cref="Undefined"/>.
        /// </returns>
        public static MeasurementKey LookUpOrCreate(string source, uint id)
        {
            MeasurementKey key = LookUpBySource(source, id);

            if (key == Undefined && !TryCreateOrUpdate(Guid.NewGuid(), source, id, out key))
                return Undefined;

            return key;
        }

        /// <summary>
        /// Converts the string representation of a <see cref="MeasurementKey"/> into its value equivalent.
        /// </summary>
        /// <param name="value">A string representing the <see cref="MeasurementKey"/> to convert.</param>
        /// <returns>A <see cref="MeasurementKey"/> value equivalent the representation contained in <paramref name="value"/>.</returns>
        /// <exception cref="FormatException">The value is not in the correct format for a <see cref="MeasurementKey"/> value.</exception>
        public static MeasurementKey Parse(string value)
        {
            MeasurementKey key;

            if (TryParse(value, out key))
                return key;

            throw new FormatException("The value is not in the correct format for a MeasurementKey value");
        }

        /// <summary>
        /// Attempts to convert the string representation of a <see cref="MeasurementKey"/> into its value equivalent.
        /// </summary>
        /// <param name="value">A string representing the <see cref="MeasurementKey"/> to convert.</param>
        /// <param name="key">Output <see cref="MeasurementKey"/> in which to stored parsed value.</param>
        /// <returns>A <c>true</c> if <see cref="MeasurementKey"/>representation contained in <paramref name="value"/> could be parsed; otherwise <c>false</c>.</returns>
        public static bool TryParse(string value, out MeasurementKey key)
        {
            string source;
            uint id;

            // Split the input into source and ID
            if (TrySplit(value, out source, out id))
            {
                // First attempt to look up an existing key
                key = LookUpBySource(source, id);

                if (key == Undefined)
                {
                    try
                    {
                        // Lookup failed - attempt to create it with a newly generated signal ID
                        key = CreateOrUpdate(Guid.NewGuid(), source, id);
                    }
                    catch
                    {
                        // source is null or empty
                        key = Undefined;
                    }
                }
            }
            else
            {
                // Incorrect format for a measurement key
                key = Undefined;
            }

            return (key != Undefined);
        }

        /// <summary>
        /// Establish default <see cref="MeasurementKey"/> cache.
        /// </summary>
        /// <param name="connection">The database connection.</param>
        /// <param name="adapterType">The database adapter type.</param>
        /// <param name="measurementTable">Measurement table name used to load measurement key cache.</param>
        /// <remarks>
        /// Source tables are expected to have at least the following fields:
        /// <code>
        ///      ID          NVARCHAR    Measurement key formatted as: ArchiveSource:PointID
        ///      SignalID    GUID        Unique identification for measurement
        /// </code>
        /// </remarks>
        public static void EstablishDefaultCache(IDbConnection connection, Type adapterType, string measurementTable = "ActiveMeasurement")
        {
            string source;
            uint id;

            // Establish default measurement key cache
            foreach (DataRow measurement in connection.RetrieveData(adapterType, string.Format("SELECT ID, SignalID FROM {0}", measurementTable)).Rows)
            {
                if (TrySplit(measurement["ID"].ToString(), out source, out id))
                    CreateOrUpdate(measurement["SignalID"].ToNonNullString(Guid.Empty.ToString()).ConvertToType<Guid>(), source, id);
            }
        }

        /// <summary>
        /// Creates the undefined measurement key. Used to initialize <see cref="Undefined"/>.
        /// </summary>
        private static MeasurementKey CreateUndefinedMeasurementKey()
        {
            MeasurementKey key = new MeasurementKey(Guid.Empty, uint.MaxValue, "__");
            //Note. No lock on SyncEdit is required since this method is only called by the static constructor.
            KeyCache.GetOrAdd(key.Source, kcf => new ConcurrentDictionary<uint, MeasurementKey>())[uint.MaxValue] = key;
            return key;
        }

        /// <summary>
        /// Attempts to split the given string representation
        /// of a measurement key into a source and ID pair.
        /// </summary>
        private static bool TrySplit(string value, out string source, out uint id)
        {
            string[] elem;

            if (!string.IsNullOrEmpty(value))
            {
                elem = value.Split(':');

                if (elem.Length == 2 && uint.TryParse(elem[1].Trim(), out id))
                {
                    source = elem[0].Trim();
                    return true;
                }
            }

            source = Undefined.Source;
            id = Undefined.ID;
            return false;
        }

        #endregion
    }
}