﻿//******************************************************************************************************
//  MeasurementMap.cs - Gbtc
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
//  12/13/2012 - Starlynn Danyelle Gilliam
//       Modified Header. 
//
//******************************************************************************************************

using System;
using System.Collections.Generic;

namespace DNP3Adapters
{
    /// <summary>
    /// Mapping
    /// </summary>
    public class Mapping
    {
        /// <summary>
        /// Creates a new <see cref="Mapping"/>.
        /// </summary>
        public Mapping()
        {
            this.tsfId = 0;
            this.dnpIndex = 0;
        }

        /// <summary>
        /// Creates a new <see cref="Mapping"/> with the specified parameters.
        /// </summary>
        /// <param name="id">ID</param>
        /// <param name="source">Source</param>
        /// <param name="index">Index</param>
        public Mapping(uint id, String source, UInt32 index)
        {
            this.tsfId = id;
            this.tsfSource = source;
            this.dnpIndex = index;
        }

        /// <summary>
        /// TSF ID
        /// </summary>
        public uint tsfId;

        /// <summary>
        /// TSF Source
        /// </summary>
        public String tsfSource;        
        
        /// <summary>
        /// DNP Index
        /// </summary>
        public UInt32 dnpIndex;
    }

    /// <summary>
    /// Measurement Map
    /// </summary>
    public class MeasurementMap
    {
        /// <summary>
        /// Binary Map
        /// </summary>
        public List<Mapping> binaryMap = new List<Mapping>();
        
        /// <summary>
        /// Analog Map
        /// </summary>
        public List<Mapping> analogMap = new List<Mapping>();
        
        /// <summary>
        /// Counter Map
        /// </summary>
        public List<Mapping> counterMap = new List<Mapping>();

        /// <summary>
        /// Fozen Counter Map
        /// </summary>
        public List<Mapping> frozenCounterMap = new List<Mapping>();

        /// <summary>
        /// double bit binary map
        /// </summary>
        public List<Mapping> doubleBitBinaryMap = new List<Mapping>();
        
        /// <summary>
        /// Control Status Map
        /// </summary>
        public List<Mapping> controlStatusMap = new List<Mapping>();
        
        /// <summary>
        /// Set Point Status Map
        /// </summary>
        public List<Mapping> setpointStatusMap = new List<Mapping>();
    }
}
