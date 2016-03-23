//******************************************************************************************************
//  FacileActionAdapterBase.cs - Gbtc
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
//  12/02/2010 - J. Ritchie Carroll
//       Added an immediate measurement tracking option for incoming data.
//
//******************************************************************************************************

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace GSF.TimeSeries.Adapters
{
    /// <summary>
    /// Represents the base class for simple, non-time-aligned, action adapters.
    /// </summary>
    /// <remarks>
    /// This base class acts on incoming measurements, in a non-time-aligned fashion, for general processing. If derived
    /// class needs time-aligned data for processing, the <see cref="ActionAdapterBase"/> class should be used instead.
    /// Derived classes are expected call <see cref="OnNewMeasurements"/> for any new measurements that may get created.
    /// </remarks>
    public abstract class FacileActionAdapterBase : AdapterBase, IActionAdapter
    {
        #region [ Members ]

        // Events

        /// <summary>
        /// Provides new measurements from action adapter.
        /// </summary>
        /// <remarks>
        /// <see cref="EventArgs{T}.Argument"/> is a collection of new measurements for host to process.
        /// </remarks>
        public event EventHandler<EventArgs<ICollection<IMeasurement>>> NewMeasurements;

        /// <summary>
        /// This event is raised by derived class, if needed, to track current number of unpublished seconds of data in the queue.
        /// </summary>
        /// <remarks>
        /// <see cref="EventArgs{T}.Argument"/> is the total number of unpublished seconds of data.
        /// </remarks>
        public event EventHandler<EventArgs<int>> UnpublishedSamples;

        /// <summary>
        /// This event is raised if there are any measurements being discarded during the sorting process.
        /// </summary>
        /// <remarks>
        /// <see cref="EventArgs{T}.Argument"/> is the enumeration of <see cref="IMeasurement"/> values that are being discarded during the sorting process.
        /// </remarks>
        public event EventHandler<EventArgs<IEnumerable<IMeasurement>>> DiscardingMeasurements;

        // Fields
        private List<string> m_inputSourceIDs;
        private List<string> m_outputSourceIDs;
        private MeasurementKey[] m_requestedInputMeasurementKeys;
        private MeasurementKey[] m_requestedOutputMeasurementKeys;
        private bool m_respectInputDemands;
        private bool m_respectOutputDemands;
        private int m_framesPerSecond;                              // Defined frames per second, if defined
        private bool m_trackLatestMeasurements;                     // Determines whether or not to track latest measurements
        private ImmediateMeasurements m_latestMeasurements;         // Absolute latest received measurement values
        private bool m_useLocalClockAsRealTime;                     // Determines whether or not to use local system clock as "real-time"
        private long m_realTimeTicks;                               // Timstamp of real-time or the most recently received measurement

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Creates a new instance of the <see cref="FacileActionAdapterBase"/> class.
        /// </summary>
        protected FacileActionAdapterBase()
        {
            m_latestMeasurements = new ImmediateMeasurements();
            m_latestMeasurements.RealTimeFunction = () => RealTime;
            m_useLocalClockAsRealTime = true;
        }

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets or sets primary keys of input measurements the <see cref="FacileActionAdapterBase"/> expects, if any.
        /// </summary>
        public override MeasurementKey[] InputMeasurementKeys
        {
            get
            {
                return base.InputMeasurementKeys;
            }
            set
            {
                base.InputMeasurementKeys = value;

                // Clear measurement cache when updating input measurement keys
                if (TrackLatestMeasurements)
                    LatestMeasurements.ClearMeasurementCache();
            }
        }

        /// <summary>
        /// Gets or sets <see cref="MeasurementKey.Source"/> values used to filter input measurement keys.
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
        /// Gets or sets flag indicating if action adapter should respect auto-start requests based on input demands.
        /// </summary>
        /// <remarks>
        /// Action adapters are in the curious position of being able to both consume and produce points, as such the user needs to be able to control how their
        /// adapter will behave concerning routing demands when the adapter is setup to connect on demand. In the case of respecting auto-start input demands,
        /// as an example, this would be <c>false</c> for an action adapter that calculated measurement, but <c>true</c> for an action adapter used to archive inputs.
        /// </remarks>
        public virtual bool RespectInputDemands
        {
            get
            {
                return m_respectInputDemands;
            }
            set
            {
                m_respectInputDemands = value;
            }
        }

        /// <summary>
        /// Gets or sets flag indicating if action adapter should respect auto-start requests based on output demands.
        /// </summary>
        /// <remarks>
        /// Action adapters are in the curious position of being able to both consume and produce points, as such the user needs to be able to control how their
        /// adapter will behave concerning routing demands when the adapter is setup to connect on demand. In the case of respecting auto-start output demands,
        /// as an example, this would be <c>true</c> for an action adapter that calculated measurement, but <c>false</c> for an action adapter used to archive inputs.
        /// </remarks>
        public virtual bool RespectOutputDemands
        {
            get
            {
                return m_respectOutputDemands;
            }
            set
            {
                m_respectOutputDemands = value;
            }
        }

        /// <summary>
        /// Gets or sets the frames per second to be used by the <see cref="FacileActionAdapterBase"/>.
        /// </summary>
        /// <remarks>
        /// This value is only tracked in the <see cref="FacileActionAdapterBase"/>, derived class will determine its use.
        /// </remarks>
        [ConnectionStringParameter,
        DefaultValue(0),
        Description("Defines the number of frames per second expected by the adapter.")]
        public virtual int FramesPerSecond
        {
            get
            {
                return m_framesPerSecond;
            }
            set
            {
                m_framesPerSecond = value;
            }
        }

        /// <summary>
        /// Gets or sets the allowed past time deviation tolerance, in seconds (can be subsecond).
        /// </summary>
        /// <remarks>
        /// <para>Defines the time sensitivity to past measurement timestamps.</para>
        /// <para>The number of seconds allowed before assuming a measurement timestamp is too old.</para>
        /// <para>This becomes the amount of delay introduced by the concentrator to allow time for data to flow into the system.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">LagTime must be greater than zero, but it can be less than one.</exception>
        [ConnectionStringParameter,
        DefaultValue(10.0D),
        Description("Defines the allowed past time deviation tolerance, in seconds (can be subsecond).")]
        public double LagTime
        {
            get
            {
                return LatestMeasurements.LagTime;
            }
            set
            {
                LatestMeasurements.LagTime = value;
            }
        }

        /// <summary>
        /// Gets or sets the allowed future time deviation tolerance, in seconds (can be subsecond).
        /// </summary>
        /// <remarks>
        /// <para>Defines the time sensitivity to future measurement timestamps.</para>
        /// <para>The number of seconds allowed before assuming a measurement timestamp is too advanced.</para>
        /// <para>This becomes the tolerated +/- accuracy of the local clock to real-time.</para>
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">LeadTime must be greater than zero, but it can be less than one.</exception>
        [ConnectionStringParameter,
        DefaultValue(5.0D),
        Description("Defines the allowed future time deviation tolerance, in seconds (can be subsecond).")]
        public double LeadTime
        {
            get
            {
                return LatestMeasurements.LeadTime;
            }
            set
            {
                LatestMeasurements.LeadTime = value;
            }
        }

        /// <summary>
        /// Gets or sets flag to start tracking the absolute latest received measurement values.
        /// </summary>
        /// <remarks>
        /// Lastest received measurement value will be available via the <see cref="LatestMeasurements"/> property.
        /// </remarks>
        public virtual bool TrackLatestMeasurements
        {
            get
            {
                return m_trackLatestMeasurements;
            }
            set
            {
                m_trackLatestMeasurements = value;
            }
        }

        /// <summary>
        /// Gets reference to the collection of absolute latest received measurement values.
        /// </summary>
        public virtual ImmediateMeasurements LatestMeasurements
        {
            get
            {
                return m_latestMeasurements;
            }
        }

        /// <summary>
        /// Gets or sets flag that determines whether or not to use the local clock time as real time.
        /// </summary>
        /// <remarks>
        /// Use your local system clock as real time only if the time is locally GPS-synchronized,
        /// or if the measurement values being sorted were not measured relative to a GPS-synchronized clock.
        /// Turn this off if the class is intended to process historical data.
        /// </remarks>
        public virtual bool UseLocalClockAsRealTime
        {
            get
            {
                return m_useLocalClockAsRealTime;
            }
            set
            {
                m_useLocalClockAsRealTime = value;
            }
        }

        /// <summary>
        /// Gets the the most accurate time value that is available. If <see cref="UseLocalClockAsRealTime"/> = <c>true</c>, then
        /// this function will return <see cref="DateTime.UtcNow"/>. Otherwise, this function will return the timestamp of the
        /// most recent measurement.
        /// </summary>
        public Ticks RealTime
        {
            get
            {
                // When using local clock as real-time, assume this is the best value we have for real time.
                if (UseLocalClockAsRealTime || !TrackLatestMeasurements)
                    return PrecisionTimer.UtcNow.Ticks;

                // Assume lastest measurement timestamp is the best value we have for real-time.
                return m_realTimeTicks;
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
                StringBuilder status = new StringBuilder();

                status.Append(base.Status);
                status.AppendFormat("        Defined frame rate: {0} frames/sec", FramesPerSecond);
                status.AppendLine();
                status.AppendFormat("      Measurement tracking: {0}", m_trackLatestMeasurements ? "Enabled" : "Disabled");
                status.AppendLine();
                status.AppendFormat("  Respecting input demands: {0}", RespectInputDemands);
                status.AppendLine();
                status.AppendFormat(" Respecting output demands: {0}", RespectOutputDemands);
                status.AppendLine();

                return status.ToString();
            }
        }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Initializes <see cref="FacileActionAdapterBase"/>.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();

            Dictionary<string, string> settings = Settings;
            string setting;

            if (settings.TryGetValue("framesPerSecond", out setting))
                FramesPerSecond = int.Parse(setting);

            if (settings.TryGetValue("useLocalClockAsRealTime", out setting))
                UseLocalClockAsRealTime = setting.ParseBoolean();

            if (settings.TryGetValue("trackLatestMeasurements", out setting))
                TrackLatestMeasurements = setting.ParseBoolean();

            if (TrackLatestMeasurements)
            {
                if (settings.TryGetValue("lagTime", out setting))
                    LatestMeasurements.LagTime = double.Parse(setting);
                else
                    LatestMeasurements.LagTime = 10.0;

                if (settings.TryGetValue("leadTime", out setting))
                    LatestMeasurements.LeadTime = double.Parse(setting);
                else
                    LatestMeasurements.LeadTime = 5.0;
            }

            if (settings.TryGetValue("respectInputDemands", out setting))
                RespectInputDemands = setting.ParseBoolean();
            else
                RespectInputDemands = false;

            if (settings.TryGetValue("respectOutputDemands", out setting))
                RespectOutputDemands = setting.ParseBoolean();
            else
                RespectOutputDemands = true;
        }

        /// <summary>
        /// Queues a single measurement for processing.
        /// </summary>
        /// <param name="measurement">Measurement to queue for processing.</param>
        public virtual void QueueMeasurementForProcessing(IMeasurement measurement)
        {
            QueueMeasurementsForProcessing(new IMeasurement[] { measurement });
        }

        /// <summary>
        /// Queues a collection of measurements for processing.
        /// </summary>
        /// <param name="measurements">Measurements to queue for processing.</param>
        public virtual void QueueMeasurementsForProcessing(IEnumerable<IMeasurement> measurements)
        {
            // If enabled, facile adapter will track the absolute latest measurement values.
            if (m_trackLatestMeasurements)
            {
                bool useLocalClockAsRealTime = UseLocalClockAsRealTime;

                foreach (IMeasurement measurement in measurements)
                {
                    m_latestMeasurements.UpdateMeasurementValue(measurement);

                    // Track latest timestamp as real-time, if requested.
                    // This class is not currently going through hassle of determining if
                    // the latest timestamp is reasonable...
                    if (!useLocalClockAsRealTime && measurement.Timestamp > m_realTimeTicks)
                        m_realTimeTicks = measurement.Timestamp;
                }
            }
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
        /// Raises the <see cref="UnpublishedSamples"/> event.
        /// </summary>
        /// <param name="seconds">Total number of unpublished seconds of data.</param>
        protected virtual void OnUnpublishedSamples(int seconds)
        {
            try
            {
                if (UnpublishedSamples != null)
                    UnpublishedSamples(this, new EventArgs<int>(seconds));
            }
            catch (Exception ex)
            {
                // We protect our code from consumer thrown exceptions
                OnProcessException(new InvalidOperationException(string.Format("Exception in consumer handler for UnpublishedSamples event: {0}", ex.Message), ex));
            }
        }

        /// <summary>
        /// Raises the <see cref="DiscardingMeasurements"/> event.
        /// </summary>
        /// <param name="measurements">Enumeration of <see cref="IMeasurement"/> values being discarded.</param>
        protected virtual void OnDiscardingMeasurements(IEnumerable<IMeasurement> measurements)
        {
            try
            {
                if (DiscardingMeasurements != null)
                    DiscardingMeasurements(this, new EventArgs<IEnumerable<IMeasurement>>(measurements));
            }
            catch (Exception ex)
            {
                // We protect our code from consumer thrown exceptions
                OnProcessException(new InvalidOperationException(string.Format("Exception in consumer handler for DiscardingMeasurements event: {0}", ex.Message), ex));
            }
        }

        #endregion
    }
}