﻿//******************************************************************************************************
//  OutputStreamDevicePhasors.xaml.cs - Gbtc
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
//  08/23/2010 - Mehulbhai P Thakkar
//       Generated original version of source code.
//
//******************************************************************************************************

using System.Windows;
using TVA.Windows;
using System.Threading;

namespace openPDCManager.ModalDialogs
{
    /// <summary>
    /// Interaction logic for OutputStreamDevicePhasors.xaml
    /// </summary>
    public partial class OutputStreamDevicePhasors : SecureWindow
    {
        public OutputStreamDevicePhasors(int outputStreamDeviceID, string outputStreamDeviceAcronym)
        {
            Thread.CurrentPrincipal = ((App)Application.Current).Principal;
            InitializeComponent();
            this.Title = "Manage Phasors For Output Stream Device: " + outputStreamDeviceAcronym;            
            UserControlOutputStreamDevicePhasors.m_sourceOutputStreamDeviceID = outputStreamDeviceID;
            UserControlOutputStreamDevicePhasors.m_sourceOutputStreamDeviceAcronym = outputStreamDeviceAcronym;
        }
    }
}
