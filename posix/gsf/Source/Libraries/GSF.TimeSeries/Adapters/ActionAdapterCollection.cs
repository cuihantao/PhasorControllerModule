﻿//******************************************************************************************************
//  ActionAdapterCollection.cs - Gbtc
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
using System.Collections.Generic;
using System.Text;

namespace GSF.TimeSeries.Adapters
{
    /// <summary>
    /// Represents a collection of <see cref="IActionAdapter"/> implementations.
    /// </summary>
    public class ActionAdapterCollection : AdapterCollectionBase<IActionAdapter>, IActionAdapter
    {
        #region [ Members ]

        // Events

        /// <summary>
        /// This event will be raised when there are new measurements available from the action adapter.
        /// </summary>
        /// <remarks>
        /// <see cref="EventArgs{T}.Argument"/> is collection of new measurements for host to process.
        /// </remarks>
        public event EventHandler<EventArgs<ICollection<IMeasurement>>> NewMeasurements;

        /// <summary>
        /// This event is raised every five seconds allowing consumer to track current number of unpublished seconds of data in the queue.
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

        /// <summary>
        /// Event is raised when temporal support is requested.
        /// </summary>
        public event EventHandler RequestTemporalSupport;

        // Fields
        private bool m_respectInputDemands;
        private bool m_respectOutputDemands;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Constructs a new instance of the <see cref="ActionAdapterCollection"/>.
        /// </summary>
        public ActionAdapterCollection()
        {
            base.Name = "Action Adapter Collection";
            base.DataMember = "ActionAdapters";
        }

        #endregion

        #region [ Properties ]

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
        /// Gets the descriptive status of this <see cref="ActionAdapterCollection"/>.
        /// </summary>
        public override string Status
        {
            get
            {
                StringBuilder status = new StringBuilder();

                status.Append(base.Status);
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
        /// Initializes the <see cref="ActionAdapterCollection"/>.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();

            Dictionary<string, string> settings = Settings;
            string setting;

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
        /// Queues a collection of measurements for processing to each <see cref="IActionAdapter"/> implementation in
        /// this <see cref="ActionAdapterCollection"/>.
        /// </summary>
        /// <param name="measurements">Measurements to queue for processing.</param>
        public virtual void QueueMeasurementsForProcessing(IEnumerable<IMeasurement> measurements)
        {
            try
            {
                lock (this)
                {
                    foreach (IActionAdapter item in this)
                    {
                        if (item.Enabled)
                            item.QueueMeasurementsForProcessing(measurements);
                    }
                }
            }
            catch (Exception ex)
            {
                OnProcessException(new InvalidOperationException("Failed to queue measurements to action adapters: " + ex.Message, ex));
            }
        }

        /// <summary>
        /// Raises the <see cref="NewMeasurements"/> event.
        /// </summary>
        /// <param name="measurements">New measurements.</param>
        protected virtual void OnNewMeasurements(ICollection<IMeasurement> measurements)
        {
            try
            {
                if (NewMeasurements != null)
                    NewMeasurements(this, new EventArgs<ICollection<IMeasurement>>(measurements));
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
        /// <param name="unpublishedSamples">Total number of unpublished seconds of data in the queue.</param>
        protected virtual void OnUnpublishedSamples(int unpublishedSamples)
        {
            try
            {
                if (UnpublishedSamples != null)
                    UnpublishedSamples(this, new EventArgs<int>(unpublishedSamples));
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

        /// <summary>
        /// Raises <see cref="RequestTemporalSupport"/> event.
        /// </summary>
        protected virtual void OnRequestTemporalSupport()
        {
            try
            {
                if ((object)RequestTemporalSupport != null)
                    RequestTemporalSupport(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                // We protect our code from consumer thrown exceptions
                OnProcessException(new InvalidOperationException(string.Format("Exception in consumer handler for RequestTemporalSupport event: {0}", ex.Message), ex));
            }
        }

        /// <summary>
        /// Wires events and initializes new <see cref="IActionAdapter"/> implementation.
        /// </summary>
        /// <param name="item">New <see cref="IActionAdapter"/> implementation.</param>
        protected override void InitializeItem(IActionAdapter item)
        {
            ActionAdapterCollection collection;

            if (item != null)
            {
                // Wire up events
                item.NewMeasurements += item_NewMeasurements;
                item.UnpublishedSamples += item_UnpublishedSamples;
                item.DiscardingMeasurements += item_DiscardingMeasurements;

                // Attach to collection-specific temporal support event
                collection = item as ActionAdapterCollection;

                if ((object)collection != null)
                    collection.RequestTemporalSupport += item_RequestTemporalSupport;

                base.InitializeItem(item);
            }
        }

        /// <summary>
        /// Unwires events and disposes of <see cref="IActionAdapter"/> implementation.
        /// </summary>
        /// <param name="item"><see cref="IActionAdapter"/> to dispose.</param>
        protected override void DisposeItem(IActionAdapter item)
        {
            ActionAdapterCollection collection;

            if (item != null)
            {
                // Un-wire events
                item.NewMeasurements -= item_NewMeasurements;
                item.UnpublishedSamples -= item_UnpublishedSamples;
                item.DiscardingMeasurements -= item_DiscardingMeasurements;

                // Detach from collection-specific temporal support event
                collection = item as ActionAdapterCollection;

                if ((object)collection != null)
                    collection.RequestTemporalSupport -= item_RequestTemporalSupport;

                base.DisposeItem(item);
            }
        }

        // Raise new measurements event on behalf of each item in collection
        private void item_NewMeasurements(object sender, EventArgs<ICollection<IMeasurement>> e)
        {
            if (NewMeasurements != null)
                NewMeasurements(sender, e);
        }

        // Raise unpublished samples event on behalf of each item in collection
        private void item_UnpublishedSamples(object sender, EventArgs<int> e)
        {
            if (UnpublishedSamples != null)
                UnpublishedSamples(sender, e);
        }

        // Raise discarding measurements event on behalf of each item in collection
        private void item_DiscardingMeasurements(object sender, EventArgs<IEnumerable<IMeasurement>> e)
        {
            if (DiscardingMeasurements != null)
                DiscardingMeasurements(sender, e);
        }

        // Raise request temporal support event on behalf of each item in collection
        private void item_RequestTemporalSupport(object sender, EventArgs e)
        {
            if ((object)RequestTemporalSupport != null)
                RequestTemporalSupport(sender, e);
        }

        #endregion
    }
}