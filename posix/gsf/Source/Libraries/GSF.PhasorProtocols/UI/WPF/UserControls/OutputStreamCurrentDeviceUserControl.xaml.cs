﻿//******************************************************************************************************
//  OutputStreamCurrentDeviceUserControl.xaml.cs - Gbtc
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
//  09/22/2011 - Mehulbhai P Thakkar
//       Generated original version of source code.
//  09/14/2012 - Aniket Salver 
//          Added paging and sorting technique. 
//******************************************************************************************************

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using GSF.PhasorProtocols.UI.DataModels;

namespace GSF.PhasorProtocols.UI.UserControls
{
    /// <summary>
    /// Interaction logic for OutputStreamCurrentDeviceUserControl.xaml
    /// </summary>
    public partial class OutputStreamCurrentDeviceUserControl
    {
        #region [ Members ]

        private readonly int m_outputStreamID;
        //private string m_outputStreamAcronym;
        private ObservableCollection<OutputStreamDevice> m_currentDevices;
        private ObservableCollection<Device> m_newDevices;
        //private TsfPopup popupSettings;

        #endregion

        #region [ Constructor ]

        /// <summary>
        /// Creates an instance of <see cref="OutputStreamCurrentDeviceUserControl"/>.
        /// <param name="outputStreamID">ID of the output stream to filter data.</param>
        /// </summary>
        public OutputStreamCurrentDeviceUserControl(int outputStreamID)
        {
            InitializeComponent();
            m_outputStreamID = outputStreamID;
            //m_outputStreamAcronym = outputStreamAcronym;
            m_currentDevices = new ObservableCollection<OutputStreamDevice>();
            m_newDevices = new ObservableCollection<Device>();
            this.Loaded += OutputStreamCurrentDeviceUserControl_Loaded;
            this.KeyUp += OutputStreamCurrentDeviceUserControl_KeyUp;
        }

        #endregion

        #region [ Methods ]

        private void OutputStreamCurrentDeviceUserControl_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && PopupAddMore.IsOpen)
                PopupAddMore.IsOpen = false;
        }

        private void LoadCurrentDevices()
        {
            IList<int> keys = OutputStreamDevice.LoadKeys(null, m_outputStreamID);
            m_currentDevices = OutputStreamDevice.Load(null, keys);
            DataGridCurrentDevices.ItemsSource = m_currentDevices;
            if (m_currentDevices.Count == 0)
                PopupAddMore.IsOpen = true;
        }

        private void OutputStreamCurrentDeviceUserControl_Loaded(object sender, RoutedEventArgs e)
        {
            LoadCurrentDevices();
            LoadNewDevices(string.Empty);
        }

        private void CheckBoxAll_Checked(object sender, RoutedEventArgs e)
        {
            foreach (OutputStreamDevice outputStreamDevice in m_currentDevices)
                outputStreamDevice.Selected = true;
        }

        private void CheckBoxAll_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (OutputStreamDevice outputStreamDevice in m_currentDevices)
                outputStreamDevice.Selected = false;
        }

        private void ButtonDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (OutputStreamDevice outputStreamDevice in m_currentDevices)
                {
                    if (outputStreamDevice.Selected)
                        OutputStreamDevice.Delete(null, m_outputStreamID, outputStreamDevice.Acronym);
                }

                LoadCurrentDevices();
                LoadNewDevices(string.Empty);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Delete Output Stream Device");
            }
        }

        private void ButtonAdd_Click(object sender, RoutedEventArgs e)
        {
            LoadNewDevices(string.Empty);
            PopupAddMore.IsOpen = true;
        }

        #region [ Popup Code]

        private void LoadNewDevices(string filterText)
        {

            if (String.IsNullOrEmpty(filterText))
            {
                m_newDevices = Device.GetNewDevicesForOutputStream(null, m_outputStreamID);
            }
            else
            {
                filterText = filterText.ToLower();
                m_newDevices = new ObservableCollection<Device>(
                    Device.GetNewDevicesForOutputStream(null, m_outputStreamID).Where(d => d.Acronym.ToNonNullString().ToLower().Contains(filterText) ||
                                                                                            d.Name.ToNonNullString().ToLower().Contains(filterText) ||
                                                                                            d.CompanyAcronym.ToNonNullString().ToLower().Contains(filterText) ||
                                                                                            d.CompanyName.ToNonNullString().ToLower().Contains(filterText) ||
                                                                                            d.ParentAcronym.ToNonNullString().ToLower().Contains(filterText))
                    );
            }

            DataGridAddDevices.ItemsSource = m_newDevices;
        }

        private void CheckBoxAddMore_Checked(object sender, RoutedEventArgs e)
        {
            foreach (Device device in m_newDevices)
                device.Enabled = true;
        }

        private void CheckBoxAddMore_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (Device device in m_newDevices)
                device.Enabled = false;
        }

        private void ButtonAddMore_Click(object sender, RoutedEventArgs e)
        {
            OutputStreamDevice.AddDevices(null, m_outputStreamID, new ObservableCollection<Device>(m_newDevices.Where(d => d.Enabled)),
                (bool)CheckBoxAddDigitals.IsChecked, (bool)CheckBoxAddAnalogs.IsChecked);
            LoadCurrentDevices();
            PopupAddMore.IsOpen = false;
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            LoadCurrentDevices();
            PopupAddMore.IsOpen = false;
        }

        private void ButtonSearch_Click(object sender, RoutedEventArgs e)
        {
            LoadNewDevices(TextBoxSearch.Text.Replace("'", "").Replace("%", ""));
        }

        private void ButtonShowAll_Click(object sender, RoutedEventArgs e)
        {
            LoadNewDevices(string.Empty);
        }

        #endregion

        #endregion
    }
}
