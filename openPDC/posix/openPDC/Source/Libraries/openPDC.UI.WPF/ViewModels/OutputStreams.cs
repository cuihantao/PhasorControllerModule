﻿//******************************************************************************************************
//  OutputStreams.cs - Gbtc
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
//  09/09/2011 - Mehulbhai Thakkar
//       Generated original version of source code.
//  09/14/2012 - Aniket Salver 
//          Added paging and sorting technique. 
//
//******************************************************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using openPDC.UI.DataModels;
using openPDC.UI.Modal;
using openPDC.UI.UserControls;
using openPDCManager.UI.DataModels;
using GSF.TimeSeries.UI;
using GSF.TimeSeries.UI.Commands;
using GSF.TimeSeries.UI.DataModels;

namespace openPDCManager.UI.ViewModels
{
    internal class OutputStreams : PagedViewModelBase<OutputStream, int>
    {
        #region [ Members ]

        private Dictionary<Guid, string> m_nodelookupList;
        private Dictionary<string, string> m_downSamplingMethodLookupList;
        private Dictionary<string, string> m_dataFormatLookupList;
        private Dictionary<string, string> m_coordinateFormatLookupList;
        private Dictionary<int, string> m_typeLookupList;
        private Dictionary<string, string> m_mirroringSourceLookupList;
        private RelayCommand m_initializeCommand;
        private RelayCommand m_copyCommand;
        private RelayCommand m_updateConfigurationCommand;
        private RelayCommand m_deviceCommand;
        private RelayCommand m_measurementCommand;
        private RelayCommand m_wizardCommand;
        private RelayCommand m_buildCommandChannelCommand;
        private RelayCommand m_buildDataChannelCommand;
        private string m_runtimeID;
        private bool m_mirrorMode;

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets <see cref="ICommand"/> object for update operation.
        /// </summary>
        public ICommand UpdateConfigurationCommand
        {
            get
            {
                if (m_updateConfigurationCommand == null)
                {
                    m_updateConfigurationCommand = new RelayCommand(UpdateConfiguration);
                }
                return m_updateConfigurationCommand;
            }
        }

        /// <summary>
        /// Gets <see cref="ICommand"/> object for copy operation.
        /// </summary>
        public ICommand CopyCommand
        {
            get
            {
                if (m_copyCommand == null)
                {
                    m_copyCommand = new RelayCommand(MakeCopy);
                }

                return m_copyCommand;
            }
        }

        /// <summary>
        /// Gets <see cref="ICommand"/> object for Initialize operation.
        /// </summary>
        public ICommand InitializeCommand
        {
            get
            {
                if (m_initializeCommand == null)
                {
                    m_initializeCommand = new RelayCommand(InitializeOutputStream);
                }
                return m_initializeCommand;
            }
        }

        /// <summary>
        /// Gets <see cref="ICommand"/> object to pop open connection string builder for command channel configuration.
        /// </summary>
        public ICommand BuildCommandChannelCommand
        {
            get
            {
                if (m_buildCommandChannelCommand == null)
                    m_buildCommandChannelCommand = new RelayCommand(BuildCommandChannel, () => CanSave);

                return m_buildCommandChannelCommand;
            }
        }

        /// <summary>
        /// Gets <see cref="ICommand"/> object to pop open connection string builder for data channel configuration.
        /// </summary>
        public ICommand BuildDataChannelCommand
        {
            get
            {
                if (m_buildDataChannelCommand == null)
                    m_buildDataChannelCommand = new RelayCommand(BuildDataChannel, () => CanSave);

                return m_buildDataChannelCommand;
            }
        }

        /// <summary>
        /// Gets <see cref="ICommand"/> to go to Devices configuration.
        /// </summary>
        public ICommand DeviceCommand
        {
            get
            {
                if (m_deviceCommand == null)
                    m_deviceCommand = new RelayCommand(GoToDevices);

                return m_deviceCommand;
            }
        }

        /// <summary>
        /// Gets <see cref="ICommand"/> to go to Measurements configuration.
        /// </summary>
        public ICommand MeasurementCommand
        {
            get
            {
                if (m_measurementCommand == null)
                    m_measurementCommand = new RelayCommand(GoToMeasurements);

                return m_measurementCommand;
            }
        }

        /// <summary>
        /// Gets <see cref="ICommand"/> to launch device wizard.
        /// </summary>
        public ICommand WizardCommand
        {
            get
            {
                if (m_wizardCommand == null)
                    m_wizardCommand = new RelayCommand(LaunchDeviceWizard);

                return m_wizardCommand;
            }
        }

        /// <summary>
        /// Gets <see cref="Dictionary{T1,T2}"/> type collection of <see cref="Node"/> defined in the database.
        /// </summary>
        public Dictionary<Guid, string> NodeLookupList
        {
            get
            {
                return m_nodelookupList;
            }
        }

        /// <summary>
        /// Gets <see cref="Dictionary{T1,T2}"/> type collection of <see cref="OutputStream"/> types.
        /// </summary>
        public Dictionary<int, string> TypeLookupList
        {
            get
            {
                return m_typeLookupList;
            }
        }

        /// <summary>
        /// Gets <see cref="Dictionary{T1,T2}"/> type collection of <see cref="OutputStream"/> down sampling methods.
        /// </summary>
        public Dictionary<string, string> DownSamplingMethodLookupList
        {
            get
            {
                return m_downSamplingMethodLookupList;
            }
        }

        /// <summary>
        /// Gets <see cref="Dictionary{T1,T2}"/> type collection of <see cref="OutputStream"/> data formats.
        /// </summary>
        public Dictionary<string, string> DataFormatLookupList
        {
            get
            {
                return m_dataFormatLookupList;
            }
        }

        /// <summary>
        /// Gets <see cref="Dictionary{T1,T2}"/> type collection of <see cref="OutputStream"/> coordinate formats.
        /// </summary>
        public Dictionary<string, string> CoordinateFormatLookupList
        {
            get
            {
                return m_coordinateFormatLookupList;
            }
        }

        /// <summary>
        /// Gets flag that determines if <see cref="PagedViewModelBase{T1, T2}.CurrentItem"/> is a new record.
        /// </summary>
        public override bool IsNewRecord
        {
            get
            {
                return CurrentItem.ID == 0;
            }
        }

        /// <summary>
        /// Gets or sets <see cref="OutputStream"/> RuntimeID.
        /// </summary>
        public string RuntimeID
        {
            get
            {
                return m_runtimeID;
            }
            set
            {
                m_runtimeID = value;
                OnPropertyChanged("RuntimeID");
            }
        }

        /// <summary>
        /// Gets a boolean flag indicating if this screen is loaded for IEEE C37.118 mirroring.
        /// </summary>
        public bool MirrorMode
        {
            get
            {
                return m_mirrorMode;
            }
        }

        /// <summary>
        /// Gets or sets list of devices to use for mirroring.
        /// </summary>
        public Dictionary<string, string> MirroringSourceLookupList
        {
            get
            {
                return m_mirroringSourceLookupList;
            }
            set
            {
                m_mirroringSourceLookupList = value;
                OnPropertyChanged("MirroringSourceLookupList");
            }
        }

        #endregion

        #region [ Constructor ]

        public OutputStreams(int itemsPerPage, bool autoSave = true)
            : base(itemsPerPage, true)
        {
        }

        #endregion

        #region [ Methods ]

        public override int GetCurrentItemKey()
        {
            return CurrentItem.ID;
        }

        public override string GetCurrentItemName()
        {
            return CurrentItem.Name;
        }

        public override void Clear()
        {
            base.Clear();
            CurrentItem.NodeID = m_nodelookupList.First().Key;
        }

        public override void Initialize()
        {
            base.Initialize();

            m_nodelookupList = Node.GetLookupList(null);

            bool.TryParse(IsolatedStorageManager.ReadFromIsolatedStorage("MirrorMode").ToString(), out m_mirrorMode);

            if (m_mirrorMode)
                MirroringSourceLookupList = Device.GetDevicesForMirroringOutputStream(null);

            m_typeLookupList = new Dictionary<int, string>();
            m_typeLookupList.Add(1, "IEEE C37.118-2005");
            m_typeLookupList.Add(2, "BPA PDCstream");
            m_typeLookupList.Add(3, "IEC 61850-90-5");

            m_downSamplingMethodLookupList = new Dictionary<string, string>();
            m_downSamplingMethodLookupList.Add("LastReceived", "LastReceived");
            m_downSamplingMethodLookupList.Add("Closest", "Closest");
            m_downSamplingMethodLookupList.Add("Filtered", "Filtered");
            m_downSamplingMethodLookupList.Add("BestQuality", "BestQuality");

            m_dataFormatLookupList = new Dictionary<string, string>();
            m_dataFormatLookupList.Add("FloatingPoint", "FloatingPoint");
            m_dataFormatLookupList.Add("FixedInteger", "FixedInteger");

            m_coordinateFormatLookupList = new Dictionary<string, string>();
            m_coordinateFormatLookupList.Add("Polar", "Polar");
            m_coordinateFormatLookupList.Add("Rectangular", "Rectangular");
        }

        public override void Load()
        {
            Mouse.OverrideCursor = Cursors.Wait;
            List<int> pageKeys = null;

            try
            {
                if (OnBeforeLoadCanceled())
                    throw new OperationCanceledException("Load was canceled.");

                if ((object)ItemsKeys == null)
                    ItemsKeys = DataModels.OutputStream.LoadKeys(null, false, SortMember, SortDirection);

                pageKeys = ItemsKeys.Skip((CurrentPageNumber - 1) * ItemsPerPage).Take(ItemsPerPage).ToList();

                ItemsSource = OutputStream.Load(null, pageKeys);
                OnLoaded();
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                {
                    Popup(ex.Message + Environment.NewLine + "Inner Exception: " + ex.InnerException.Message, "Load " + DataModelName + " Exception:", MessageBoxImage.Error);
                    CommonFunctions.LogException(null, "Load " + DataModelName, ex.InnerException);
                }
                else
                {
                    Popup(ex.Message, "Load " + DataModelName + " Exception:", MessageBoxImage.Error);
                    CommonFunctions.LogException(null, "Load " + DataModelName, ex);
                }
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void InitializeOutputStream()
        {
            if (!CommonFunctions.CurrentPrincipal.IsInRole("Administrator, Editor"))
            {
                // Do nothing, If it is Accessed stays diabled.
            }
            else
            {
                if (Confirm("Do you want to send Initialize Command?", "Output Stream: " + CurrentItem.Acronym))
                {
                    try
                    {
                        var result = CommonFunctions.SendCommandToService("Initialize " + RuntimeID);
                        Popup(result, "", System.Windows.MessageBoxImage.Information);
                        CommonFunctions.SendCommandToService("Invoke 0 ReloadStatistics");
                    }
                    catch (Exception ex)
                    {
                        CommonFunctions.LogException(null, "WPF.SendInitialize", ex);
                        Popup("Failed to Send Initialize Command", ex.Message, System.Windows.MessageBoxImage.Error);
                    }
                }
            }
        }

        private void MakeCopy(object parameter)
        {
            try
            {
                if (Confirm("Do you want to make a copy of " + CurrentItem.Acronym + " output stream?", "This will only copy the output stream configuration, not associated devices."))
                {
                    OutputStream newOutputStream = new OutputStream()
                    {
                        ID = 0, // Set it to zero so it will be inserted instead of updated.
                        Enabled = false,
                        Acronym = CurrentItem.Acronym,
                        Name = "Copy of " + CurrentItem.Name,
                        AllowPreemptivePublishing = CurrentItem.AllowPreemptivePublishing,
                        AllowSortsByArrival = CurrentItem.AllowSortsByArrival,
                        AnalogScalingValue = CurrentItem.AnalogScalingValue,
                        AutoPublishConfigFrame = CurrentItem.AutoPublishConfigFrame,
                        AutoStartDataChannel = CurrentItem.AutoStartDataChannel,
                        CommandChannel = CurrentItem.CommandChannel,
                        ConnectionString = CurrentItem.ConnectionString,
                        CoordinateFormat = CurrentItem.CoordinateFormat,
                        CurrentScalingValue = CurrentItem.CurrentScalingValue,
                        DataChannel = CurrentItem.DataChannel,
                        DataFormat = CurrentItem.DataFormat,
                        DigitalMaskValue = CurrentItem.DigitalMaskValue,
                        DownSamplingMethod = CurrentItem.DownSamplingMethod,
                        FramesPerSecond = CurrentItem.FramesPerSecond,
                        IDCode = CurrentItem.IDCode,
                        IgnoreBadTimeStamps = CurrentItem.IgnoreBadTimeStamps,
                        LagTime = CurrentItem.LagTime,
                        LeadTime = CurrentItem.LeadTime,
                        TimeBase = CurrentItem.TimeBase,
                        AddPhaseLabelSuffix = CurrentItem.AddPhaseLabelSuffix,
                        ReplaceWithSpaceChar = CurrentItem.ReplaceWithSpaceChar,
                        LoadOrder = CurrentItem.LoadOrder,
                        NodeID = CurrentItem.NodeID,
                        NominalFrequency = CurrentItem.NominalFrequency,
                        PerformTimestampReasonabilityCheck = CurrentItem.PerformTimestampReasonabilityCheck,
                        TimeResolution = CurrentItem.TimeResolution,
                        Type = CurrentItem.Type,
                        UseLocalClockAsRealTime = CurrentItem.UseLocalClockAsRealTime,
                        VoltageScalingValue = CurrentItem.VoltageScalingValue
                    };

                    string originalAcronym = newOutputStream.Acronym;
                    int i = 1;
                    do
                    {
                        newOutputStream.Acronym = originalAcronym + i.ToString();
                        i++;
                    }
                    while (OutputStream.GetOutputStream(null, " WHERE Acronym = '" + newOutputStream.Acronym + "'") != null);

                    CurrentItem = newOutputStream;
                }
            }
            catch (Exception ex)
            {
                Popup("Failed to copy output stream.", ex.Message, System.Windows.MessageBoxImage.Error);
            }
        }

        private void UpdateConfiguration()
        {
            if (!CommonFunctions.CurrentPrincipal.IsInRole("Administrator, Editor"))
            {
                // Do nothing.It will avoid the Message Box to poping up,When the applucation is Viewed as a Viewer.
            }

            else
            {
                try
                {
                    if (Confirm("Do you want to update configuration?", ""))
                    {
                        string runtimeID = CommonFunctions.GetRuntimeID("OutputStream", CurrentItem.ID);
                        string result = CommonFunctions.SendCommandToService("reloadconfig");
                        result = CommonFunctions.SendCommandToService("Invoke " + runtimeID + " UpdateConfiguration");
                        Popup(result, "", MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    Popup("Failed to UpdateConfiguration", ex.Message, MessageBoxImage.Error);
                }
            }
        }

        private void BuildCommandChannel()
        {
            if (CurrentItem != null)
            {
                ConnectionStringBuilder csb = new ConnectionStringBuilder(ConnectionStringBuilder.ConnectionType.CommandChannel);
                if (!string.IsNullOrEmpty(CurrentItem.CommandChannel))
                    csb.ConnectionString = CurrentItem.CommandChannel;

                csb.Closed += new EventHandler(delegate(object popupWindow, EventArgs eargs)
                {
                    if ((bool)csb.DialogResult)
                        CurrentItem.CommandChannel = csb.ConnectionString;
                });
                csb.Owner = System.Windows.Application.Current.MainWindow;
                csb.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                csb.ShowDialog();
            }
        }

        private void BuildDataChannel()
        {
            if (CurrentItem != null)
            {
                ConnectionStringBuilder csb = new ConnectionStringBuilder(ConnectionStringBuilder.ConnectionType.DataChannel);
                if (!string.IsNullOrEmpty(CurrentItem.DataChannel))
                    csb.ConnectionString = CurrentItem.DataChannel;

                csb.Closed += new EventHandler(delegate(object popupWindow, EventArgs eargs)
                {
                    if ((bool)csb.DialogResult)
                        CurrentItem.DataChannel = csb.ConnectionString;
                });
                csb.Owner = System.Windows.Application.Current.MainWindow;
                csb.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                csb.ShowDialog();
            }
        }

        private void GoToDevices(object parameter)
        {
            OutputStreamDeviceUserControl outputStreamDeviceUserControl = new OutputStreamDeviceUserControl(CurrentItem.ID, MirrorMode);
            CommonFunctions.LoadUserControl(outputStreamDeviceUserControl, "Manage Devices for " + CurrentItem.Acronym);
        }

        private void LaunchDeviceWizard(object parameter)
        {
            if (!CommonFunctions.CurrentPrincipal.IsInRole("Administrator,Editor"))
            {
                // This will Disable the Device Wizard button, If we view it as a Viewer
            }
            else
            {
                if (!MirrorMode)
                {
                    OutputStreamCurrentDeviceUserControl outputStreamCurrentDeviceUserControl = new OutputStreamCurrentDeviceUserControl(CurrentItem.ID, CurrentItem.Acronym);
                    CommonFunctions.LoadUserControl(outputStreamCurrentDeviceUserControl, "Current Devices for " + CurrentItem.Acronym);
                }
            }
        }
        private void GoToMeasurements(object parameter)
        {
            OutputStreamMeasurementUserControl outputStreamMeasurementUserControl = new OutputStreamMeasurementUserControl(CurrentItem.ID);
            CommonFunctions.LoadUserControl(outputStreamMeasurementUserControl, "Manage Measurements for " + CurrentItem.Acronym);
        }

        protected override void OnPropertyChanged(string propertyName)
        {
            base.OnPropertyChanged(propertyName);

            if (propertyName == "CurrentItem")
            {
                if (CurrentItem == null)
                    RuntimeID = string.Empty;
                else
                    RuntimeID = GSF.TimeSeries.UI.CommonFunctions.GetRuntimeID("OutputStream", CurrentItem.ID);
            }
        }

        protected override void m_currentItem_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "MirroringSourceDevice")
            {

            }

            base.m_currentItem_PropertyChanged(sender, e);
        }

        public override void Save()
        {
            if (CanSave)
            {
                try
                {
                    Mouse.OverrideCursor = Cursors.Wait;

                    if (OnBeforeSaveCanceled())
                        throw new OperationCanceledException("Save was canceled.");

                    bool isNewRecord = IsNewRecord;
                    string result = OutputStream.Save(null, CurrentItem, m_mirrorMode);

                    OnSaved();

                    CurrentItemPropertyChanged = false;

                    if (!DisplayStatusMessage(result))
                        Popup(result, "Save " + DataModelName, MessageBoxImage.Information);

                    if (isNewRecord)
                    {
                        ItemsKeys = null;
                        Load();
                    }
                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null)
                    {
                        Popup(ex.Message + Environment.NewLine + "Inner Exception: " + ex.InnerException.Message, "Save " + DataModelName + " Exception:", MessageBoxImage.Error);
                        CommonFunctions.LogException(null, "Save " + DataModelName, ex.InnerException);
                    }
                    else
                    {
                        Popup(ex.Message, "Save " + DataModelName + " Exception:", MessageBoxImage.Error);
                        CommonFunctions.LogException(null, "Save " + DataModelName, ex);
                    }
                }
                finally
                {
                    Mouse.OverrideCursor = null;
                }
            }
        }

        #endregion
    }
}
