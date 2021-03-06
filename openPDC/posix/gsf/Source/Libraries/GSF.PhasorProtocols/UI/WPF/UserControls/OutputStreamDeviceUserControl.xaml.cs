﻿//******************************************************************************************************
//  OutputStreamDeviceUserControl.xaml.cs - Gbtc
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
//  09/14/2011 - Aniket Salver
//       Generated original version of source code.
//  09/16/2011 - Mehulbhai Thakkar
//       Added code to attach this user control to parent Output Stream.
//       Organized code into proper regions.
//       Added delete key handling logic.
//
//******************************************************************************************************

using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GSF.PhasorProtocols.UI.ViewModels;

namespace GSF.PhasorProtocols.UI.UserControls
{
    /// <summary>
    /// Interaction logic for OutputStreamDeviceUserControl.xaml
    /// </summary>
    public partial class OutputStreamDeviceUserControl : UserControl
    {
        #region [ Members ]

        private readonly int m_outputStreamID;
        private OutputStreamDevices m_dataContext;
        private readonly bool m_mirrorMode;  // from output stream.
        private DataGridColumn m_sortColumn;
        private string m_sortMemberPath;
        private ListSortDirection m_sortDirection;

        #endregion

        #region [ Constructor ]

        /// <summary>
        /// Creates an instance of <see cref="OutputStreamDeviceUserControl"/> class.
        /// </summary>
        public OutputStreamDeviceUserControl(int outputStreamID, bool mirrorMode = false)
        {
            InitializeComponent();
            m_mirrorMode = mirrorMode;
            ButtonDeviceWizard.IsEnabled = !m_mirrorMode;
            UserControlDetailViewFooter.Visibility = m_mirrorMode ? Visibility.Collapsed : Visibility.Visible;
            m_outputStreamID = outputStreamID;
            this.Loaded += OutputStreamDeviceUserControl_Loaded;
            
        }

        #endregion

        #region [ Methods ]

        private void OutputStreamDeviceUserControl_Loaded(object sender, RoutedEventArgs e)
        {
            m_dataContext = new OutputStreamDevices(m_outputStreamID, 10);
            this.DataContext = m_dataContext;
            m_dataContext.PropertyChanged += ViewModel_PropertyChanged;   
        }

        private void DataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                DataGrid dataGrid = sender as DataGrid;
                if (dataGrid.SelectedItems.Count > 0)
                {
                    if (MessageBox.Show("Are you sure you want to delete " + dataGrid.SelectedItems.Count + " selected item(s)?", "Delete Selected Items", MessageBoxButton.YesNo) == MessageBoxResult.No)
                        e.Handled = true;
                }
            }
        }

        private void DataGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            if (e.Column.SortMemberPath != m_sortMemberPath)
                m_sortDirection = ListSortDirection.Ascending;
            else if (m_sortDirection == ListSortDirection.Ascending)
                m_sortDirection = ListSortDirection.Descending;
            else
                m_sortDirection = ListSortDirection.Ascending;

            m_sortColumn = e.Column;
            m_sortMemberPath = e.Column.SortMemberPath;
            m_dataContext.SortData(m_sortMemberPath, m_sortDirection);
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ItemsSource")
                Dispatcher.BeginInvoke(new Action(SortDataGrid));
        }

        private void SortDataGrid()
        {
            if ((object)m_sortColumn != null)
            {
                m_sortColumn.SortDirection = m_sortDirection;
                DataGridList.Items.SortDescriptions.Clear();
                DataGridList.Items.SortDescriptions.Add(new SortDescription(m_sortMemberPath, m_sortDirection));
                DataGridList.Items.Refresh();
            }
        }

        private void GridDetailView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (m_dataContext.IsNewRecord)
                DataGridList.SelectedIndex = -1;
        }

        #endregion
    }
}
