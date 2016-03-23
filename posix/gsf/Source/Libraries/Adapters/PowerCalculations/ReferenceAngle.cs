﻿//******************************************************************************************************
//  ReferenceAngle.cs - Gbtc
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
//  05/19/2006 - J. Ritchie Carroll
//       Initial version of source generated.
//  01/16/2007 - Jian R. Zuo
//       Implement the unwrap offset of the angle.
//  01/17/2007 - J. Ritchie Carroll
//       Added code to detect data set changes (i.e., PMU's online/offline).
//  01/17/2007 - Jian R. Zuo
//       Added code to handle unwrap offset initialization and reset.
//  12/23/2009 - Jian R. Zuo
//       Converted code to C#.
//  12/28/2009 - Jian R. Zuo
//       Include System.Linq and use "Average" extension function.
//  04/12/2010 - J. Ritchie Carroll
//       Performed full code review, optimization and further abstracted code for average calculation.
//  12/13/2012 - Starlynn Danyelle Gilliam
//       Modified Header.
//
//******************************************************************************************************

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using GSF.Collections;
using GSF.TimeSeries;
using GSF.Units.EE;
using PhasorProtocolAdapters;

namespace PowerCalculations
{
    /// <summary>
    /// Calculates a composed reference angle.
    /// </summary>
    [Description("Reference Angle: Calculates a composed reference angle")]
    public class ReferenceAngle : CalculatedMeasurementBase
    {
        #region [ Members ]

        // Constants
        private const int BackupQueueSize = 10;

        // Fields
        private double m_phaseResetAngle;
        private Dictionary<MeasurementKey, Double> m_lastAngles;
        private Dictionary<MeasurementKey, Double> m_unwrapOffsets;
        private List<Double> m_latestCalculatedAngles;
        private IMeasurement[] m_measurements;

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Returns the detailed status of the <see cref="ReferenceAngle"/> calculator.
        /// </summary>
        public override string Status
        {
            get
            {
                const int ValuesToShow = 4;

                StringBuilder status = new StringBuilder();

                status.AppendFormat("  Last " + ValuesToShow + " calculated angles:");

                lock (m_latestCalculatedAngles)
                {
                    if (m_latestCalculatedAngles.Count > ValuesToShow)
                        status.Append(m_latestCalculatedAngles.GetRange(m_latestCalculatedAngles.Count - ValuesToShow, ValuesToShow).Select(v => v.ToString("0.00°")).ToDelimitedString(", "));
                    else
                        status.Append("Not enough values calculated yet ...");
                }

                status.AppendLine();
                status.Append(base.Status);

                return status.ToString();
            }
        }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Initializes the <see cref="ReferenceAngle"/> calculator.
        /// </summary>
        public override void Initialize()
        {
            base.Initialize();

            // Validate input measurements
            List<MeasurementKey> validInputMeasurementKeys = new List<MeasurementKey>();
            SignalType keyType;

            for (int i = 0; i < InputMeasurementKeys.Length; i++)
            {
                keyType = InputMeasurementKeyTypes[i];

                // Make sure measurement key type is a phase angle
                if (keyType == SignalType.VPHA || keyType == SignalType.IPHA)
                    validInputMeasurementKeys.Add(InputMeasurementKeys[i]);
            }

            if (validInputMeasurementKeys.Count == 0)
                throw new InvalidOperationException("No valid phase angles were specified as inputs to the reference angle calculator.");

            if (InputMeasurementKeyTypes.Count(s => s == SignalType.VPHA) > 0 && InputMeasurementKeyTypes.Count(s => s == SignalType.IPHA) > 0)
                throw new InvalidOperationException("A mixture of voltage and current phase angles were specified as inputs to the reference angle calculator - you must specify one or the other: only voltage phase angles or only current phase angles.");

            // Make sure only phase angles are used as input
            InputMeasurementKeys = validInputMeasurementKeys.ToArray();

            // Validate output measurements
            if (OutputMeasurements.Length < 1)
                throw new InvalidOperationException("An output measurement was not specified for the reference angle calculator - one measurement is expected to represent the \"Calculated Reference Angle\" value.");

            // Initialize member fields
            m_lastAngles = new Dictionary<MeasurementKey, double>();
            m_unwrapOffsets = new Dictionary<MeasurementKey, double>();
            m_latestCalculatedAngles = new List<double>();
            m_phaseResetAngle = MinimumMeasurementsToUse * 360.0D;
        }

        /// <summary>
        /// Calculates a virtual reference angle.
        /// </summary>
        /// <param name="frame">Single frame of measurement data within one second samples</param>
        /// <param name="index">Index of frame within the one second samples</param>
        protected override void PublishFrame(IFrame frame, int index)
        {
            Measurement calculatedMeasurement = Measurement.Clone(OutputMeasurements[0], frame.Timestamp);
            double angle, deltaAngle, angleTotal, angleAverage, lastAngle, unwrapOffset;
            IMeasurement currentAngle;
            MeasurementKey key;
            bool dataSetChanged;
            int i;

            dataSetChanged = false;
            angleTotal = 0.0D;

            // Attempt to get minimum needed reporting set of composite angles used to calculate reference angle
            if (TryGetMinimumNeededMeasurements(frame, ref m_measurements))
            {
                // See if data set has changed since last run
                if (m_lastAngles.Count > 0 && m_lastAngles.Count == m_measurements.Length)
                {
                    for (i = 0; i < m_measurements.Length; i++)
                    {
                        if (!m_lastAngles.ContainsKey(m_measurements[i].Key))
                        {
                            dataSetChanged = true;
                            break;
                        }
                    }
                }
                else
                    dataSetChanged = true;

                // Reinitialize all angle calculation data if data set has changed
                if (dataSetChanged)
                {
                    double angleRef, angleDelta0, angleDelta1, angleDelta2;

                    // Clear last angles and unwrap offsets
                    m_lastAngles.Clear();
                    m_unwrapOffsets.Clear();

                    // Calculate new unwrap offsets
                    angleRef = m_measurements[0].AdjustedValue;

                    for (i = 0; i < m_measurements.Length; i++)
                    {
                        angleDelta0 = Math.Abs(m_measurements[i].AdjustedValue - angleRef);
                        angleDelta1 = Math.Abs(m_measurements[i].AdjustedValue + 360.0D - angleRef);
                        angleDelta2 = Math.Abs(m_measurements[i].AdjustedValue - 360.0D - angleRef);

                        if (angleDelta0 < angleDelta1 && angleDelta0 < angleDelta2)
                            unwrapOffset = 0.0D;
                        else if (angleDelta1 < angleDelta2)
                            unwrapOffset = 360.0D;
                        else
                            unwrapOffset = -360.0D;

                        m_unwrapOffsets[m_measurements[i].Key] = unwrapOffset;
                    }
                }

                // Add up all the phase angles, unwrapping angles if necessary
                for (i = 0; i < m_measurements.Length; i++)
                {
                    // Get current angle value and key
                    angle = m_measurements[i].AdjustedValue;
                    key = m_measurements[i].Key;

                    // Get the unwrap offset for this angle
                    unwrapOffset = m_unwrapOffsets[key];

                    // Get angle value from last run,if there was a last run
                    if (m_lastAngles.TryGetValue(key, out lastAngle))
                    {
                        // Calculate the angle difference from last run
                        deltaAngle = angle - lastAngle;

                        // Adjust angle unwrap offset if needed
                        if (deltaAngle > 300)
                            unwrapOffset -= 360;
                        else if (deltaAngle < -300)
                            unwrapOffset += 360;

                        // Reset angle unwrap offset if needed
                        if (unwrapOffset > m_phaseResetAngle)
                            unwrapOffset -= m_phaseResetAngle;
                        else if (unwrapOffset < -m_phaseResetAngle)
                            unwrapOffset += m_phaseResetAngle;

                        // Record last angle unwrap offset
                        m_unwrapOffsets[key] = unwrapOffset;
                    }

                    // Add up all the angles
                    angleTotal += (angle + unwrapOffset);
                }
                // We use modulus function to make sure angle is in range of 0 to 360
                angleAverage = (angleTotal / m_measurements.Length) % 360.0D;

                // Record last angles for next run
                m_lastAngles.Clear();

                for (i = 0; i < m_measurements.Length; i++)
                {
                    currentAngle = m_measurements[i];
                    m_lastAngles.Add(currentAngle.Key, currentAngle.AdjustedValue);
                }
            }
            else
            {
                // Use stored average value when minimum set is not available
                if (m_latestCalculatedAngles.Count > 0)
                    angleAverage = m_latestCalculatedAngles.Average() % 360.0D;
                else
                    angleAverage = double.NaN;

                // Mark quality as "bad" when falling back to stored value
                calculatedMeasurement.StateFlags |= MeasurementStateFlags.BadData;
            }

            // Convert angle value to the range of -180 to 180
            if (angleAverage > 180)
                angleAverage -= 360;

            if (angleAverage <= -180)
                angleAverage += 360;

            calculatedMeasurement.Value = angleAverage;

            // Expose calculated value
            OnNewMeasurements(new IMeasurement[] { calculatedMeasurement });

            // Add calculated reference angle to latest angle queue as backup in case needed
            // minimum number of angles are not available
            lock (m_latestCalculatedAngles)
            {
                m_latestCalculatedAngles.Add(angleAverage);

                while (m_latestCalculatedAngles.Count > BackupQueueSize)
                    m_latestCalculatedAngles.RemoveAt(0);
            }
        }

        #endregion
    }
}