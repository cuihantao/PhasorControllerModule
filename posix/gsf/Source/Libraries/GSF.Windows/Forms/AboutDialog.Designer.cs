﻿//******************************************************************************************************
//  AboutDialog.Designer.cs - Gbtc
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
//  05/26/2006 - Pinal C. Patel
//       Original version of source code generated.
//  09/14/2009 - Stephen C. Wills
//       Added new header and license agreement.
//  12/20/2012 - Starlynn Danyelle Gilliam
//       Modified Header.
//
//******************************************************************************************************

namespace GSF.Windows.Forms
{
    partial class AboutDialog
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.TabControlInformation = new System.Windows.Forms.TabControl();
            this.TabPageDisclaimer = new System.Windows.Forms.TabPage();
            this.RichTextBoxDisclaimer = new System.Windows.Forms.RichTextBox();
            this.TabPageApplication = new System.Windows.Forms.TabPage();
            this.ListViewApplicationInfo = new System.Windows.Forms.ListView();
            this.ColumnHeaderApplicationKey = new System.Windows.Forms.ColumnHeader();
            this.ColumnHeaderApplicationValue = new System.Windows.Forms.ColumnHeader();
            this.TabPageAssemblies = new System.Windows.Forms.TabPage();
            this.ListViewAssemblyInfo = new System.Windows.Forms.ListView();
            this.ColumnHeaderAssemblyKey = new System.Windows.Forms.ColumnHeader();
            this.ColumnHeaderAssemblyValue = new System.Windows.Forms.ColumnHeader();
            this.ComboBoxAssemblies = new System.Windows.Forms.ComboBox();
            this.ButtonOK = new System.Windows.Forms.Button();
            this.PictureBoxLogo = new System.Windows.Forms.PictureBox();
            this.TabControlInformation.SuspendLayout();
            this.TabPageDisclaimer.SuspendLayout();
            this.TabPageApplication.SuspendLayout();
            this.TabPageAssemblies.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.PictureBoxLogo)).BeginInit();
            this.SuspendLayout();
            // 
            // TabControlInformation
            // 
            this.TabControlInformation.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.TabControlInformation.Controls.Add(this.TabPageDisclaimer);
            this.TabControlInformation.Controls.Add(this.TabPageApplication);
            this.TabControlInformation.Controls.Add(this.TabPageAssemblies);
            this.TabControlInformation.Location = new System.Drawing.Point(12, 68);
            this.TabControlInformation.Name = "TabControlInformation";
            this.TabControlInformation.SelectedIndex = 0;
            this.TabControlInformation.Size = new System.Drawing.Size(410, 253);
            this.TabControlInformation.TabIndex = 5;
            // 
            // TabPageDisclaimer
            // 
            this.TabPageDisclaimer.Controls.Add(this.RichTextBoxDisclaimer);
            this.TabPageDisclaimer.Location = new System.Drawing.Point(4, 22);
            this.TabPageDisclaimer.Name = "TabPageDisclaimer";
            this.TabPageDisclaimer.Padding = new System.Windows.Forms.Padding(3);
            this.TabPageDisclaimer.Size = new System.Drawing.Size(402, 227);
            this.TabPageDisclaimer.TabIndex = 0;
            this.TabPageDisclaimer.Text = "Disclaimer";
            this.TabPageDisclaimer.UseVisualStyleBackColor = true;
            // 
            // RichTextBoxDisclaimer
            // 
            this.RichTextBoxDisclaimer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.RichTextBoxDisclaimer.Font = new System.Drawing.Font("Trebuchet MS", 9.75F, System.Drawing.FontStyle.Bold);
            this.RichTextBoxDisclaimer.Location = new System.Drawing.Point(3, 3);
            this.RichTextBoxDisclaimer.Name = "RichTextBoxDisclaimer";
            this.RichTextBoxDisclaimer.ReadOnly = true;
            this.RichTextBoxDisclaimer.Size = new System.Drawing.Size(396, 221);
            this.RichTextBoxDisclaimer.TabIndex = 0;
            this.RichTextBoxDisclaimer.Text = "";
            this.RichTextBoxDisclaimer.LinkClicked += new System.Windows.Forms.LinkClickedEventHandler(this.RichTextBoxDisclaimer_LinkClicked);
            // 
            // TabPageApplication
            // 
            this.TabPageApplication.Controls.Add(this.ListViewApplicationInfo);
            this.TabPageApplication.Location = new System.Drawing.Point(4, 22);
            this.TabPageApplication.Name = "TabPageApplication";
            this.TabPageApplication.Padding = new System.Windows.Forms.Padding(3);
            this.TabPageApplication.Size = new System.Drawing.Size(402, 227);
            this.TabPageApplication.TabIndex = 1;
            this.TabPageApplication.Text = "Application";
            this.TabPageApplication.UseVisualStyleBackColor = true;
            // 
            // ListViewApplicationInfo
            // 
            this.ListViewApplicationInfo.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.ColumnHeaderApplicationKey,
            this.ColumnHeaderApplicationValue});
            this.ListViewApplicationInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ListViewApplicationInfo.FullRowSelect = true;
            this.ListViewApplicationInfo.Location = new System.Drawing.Point(3, 3);
            this.ListViewApplicationInfo.Name = "ListViewApplicationInfo";
            this.ListViewApplicationInfo.Size = new System.Drawing.Size(396, 221);
            this.ListViewApplicationInfo.TabIndex = 0;
            this.ListViewApplicationInfo.UseCompatibleStateImageBehavior = false;
            this.ListViewApplicationInfo.View = System.Windows.Forms.View.Details;
            // 
            // ColumnHeaderApplicationKey
            // 
            this.ColumnHeaderApplicationKey.Text = "Key";
            this.ColumnHeaderApplicationKey.Width = 110;
            // 
            // ColumnHeaderApplicationValue
            // 
            this.ColumnHeaderApplicationValue.Text = "Value";
            this.ColumnHeaderApplicationValue.Width = 260;
            // 
            // TabPageAssemblies
            // 
            this.TabPageAssemblies.Controls.Add(this.ListViewAssemblyInfo);
            this.TabPageAssemblies.Controls.Add(this.ComboBoxAssemblies);
            this.TabPageAssemblies.Location = new System.Drawing.Point(4, 22);
            this.TabPageAssemblies.Name = "TabPageAssemblies";
            this.TabPageAssemblies.Size = new System.Drawing.Size(402, 227);
            this.TabPageAssemblies.TabIndex = 2;
            this.TabPageAssemblies.Text = "Assemblies";
            this.TabPageAssemblies.UseVisualStyleBackColor = true;
            // 
            // ListViewAssemblyInfo
            // 
            this.ListViewAssemblyInfo.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.ColumnHeaderAssemblyKey,
            this.ColumnHeaderAssemblyValue});
            this.ListViewAssemblyInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ListViewAssemblyInfo.FullRowSelect = true;
            this.ListViewAssemblyInfo.Location = new System.Drawing.Point(0, 21);
            this.ListViewAssemblyInfo.Name = "ListViewAssemblyInfo";
            this.ListViewAssemblyInfo.Size = new System.Drawing.Size(402, 206);
            this.ListViewAssemblyInfo.TabIndex = 1;
            this.ListViewAssemblyInfo.UseCompatibleStateImageBehavior = false;
            this.ListViewAssemblyInfo.View = System.Windows.Forms.View.Details;
            // 
            // ColumnHeaderAssemblyKey
            // 
            this.ColumnHeaderAssemblyKey.Text = "Key";
            this.ColumnHeaderAssemblyKey.Width = 110;
            // 
            // ColumnHeaderAssemblyValue
            // 
            this.ColumnHeaderAssemblyValue.Text = "Value";
            this.ColumnHeaderAssemblyValue.Width = 260;
            // 
            // ComboBoxAssemblies
            // 
            this.ComboBoxAssemblies.Dock = System.Windows.Forms.DockStyle.Top;
            this.ComboBoxAssemblies.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ComboBoxAssemblies.FormattingEnabled = true;
            this.ComboBoxAssemblies.Location = new System.Drawing.Point(0, 0);
            this.ComboBoxAssemblies.Name = "ComboBoxAssemblies";
            this.ComboBoxAssemblies.Size = new System.Drawing.Size(402, 21);
            this.ComboBoxAssemblies.TabIndex = 0;
            this.ComboBoxAssemblies.SelectedIndexChanged += new System.EventHandler(this.ComboBoxAssemblies_SelectedIndexChanged);
            // 
            // ButtonOK
            // 
            this.ButtonOK.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.ButtonOK.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.ButtonOK.Location = new System.Drawing.Point(181, 333);
            this.ButtonOK.Name = "ButtonOK";
            this.ButtonOK.Size = new System.Drawing.Size(75, 23);
            this.ButtonOK.TabIndex = 6;
            this.ButtonOK.Text = "OK";
            this.ButtonOK.UseVisualStyleBackColor = true;
            this.ButtonOK.Click += new System.EventHandler(this.ButtonOK_Click);
            // 
            // PictureBoxLogo
            // 
            this.PictureBoxLogo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.PictureBoxLogo.BackColor = System.Drawing.Color.Transparent;
            this.PictureBoxLogo.Cursor = System.Windows.Forms.Cursors.Hand;
            this.PictureBoxLogo.Location = new System.Drawing.Point(12, 12);
            this.PictureBoxLogo.Name = "PictureBoxLogo";
            this.PictureBoxLogo.Size = new System.Drawing.Size(410, 50);
            this.PictureBoxLogo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.PictureBoxLogo.TabIndex = 7;
            this.PictureBoxLogo.TabStop = false;
            this.PictureBoxLogo.Click += new System.EventHandler(this.PictureBoxLogo_Click);
            // 
            // AboutDialog
            // 
            this.AcceptButton = this.ButtonOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.ButtonOK;
            this.ClientSize = new System.Drawing.Size(434, 368);
            this.Controls.Add(this.TabControlInformation);
            this.Controls.Add(this.ButtonOK);
            this.Controls.Add(this.PictureBoxLogo);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AboutDialog";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "About {0}";
            this.Load += new System.EventHandler(this._AboutDialog_Load);
            this.TabControlInformation.ResumeLayout(false);
            this.TabPageDisclaimer.ResumeLayout(false);
            this.TabPageApplication.ResumeLayout(false);
            this.TabPageAssemblies.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.PictureBoxLogo)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        internal System.Windows.Forms.TabControl TabControlInformation;
        internal System.Windows.Forms.TabPage TabPageDisclaimer;
        internal System.Windows.Forms.RichTextBox RichTextBoxDisclaimer;
        internal System.Windows.Forms.TabPage TabPageApplication;
        internal System.Windows.Forms.ListView ListViewApplicationInfo;
        internal System.Windows.Forms.ColumnHeader ColumnHeaderApplicationKey;
        internal System.Windows.Forms.ColumnHeader ColumnHeaderApplicationValue;
        internal System.Windows.Forms.TabPage TabPageAssemblies;
        internal System.Windows.Forms.ListView ListViewAssemblyInfo;
        internal System.Windows.Forms.ColumnHeader ColumnHeaderAssemblyKey;
        internal System.Windows.Forms.ColumnHeader ColumnHeaderAssemblyValue;
        internal System.Windows.Forms.ComboBox ComboBoxAssemblies;
        internal System.Windows.Forms.Button ButtonOK;
        internal System.Windows.Forms.PictureBox PictureBoxLogo;
    }
}