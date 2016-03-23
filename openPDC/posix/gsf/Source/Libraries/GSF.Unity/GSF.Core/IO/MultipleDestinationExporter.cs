//*******************************************************************************************************
//  MultipleDestinationExporter.cs - Gbtc
//
//  Tennessee Valley Authority, 2009
//  No copyright is claimed pursuant to 17 USC § 105.  All Other Rights Reserved.
//
//  This software is made freely available under the TVA Open Source Agreement (see below).
//  Code in this file licensed to GSF under one or more contributor license agreements listed below.
//
//  Code Modification History:
//  -----------------------------------------------------------------------------------------------------
//  02/13/2008 - J. Ritchie Carroll
//       Initial version of source generated.
//  07/29/2008 - J. Ritchie Carroll
//       Added "Initialize" method to enable user to reconnect to network shares.
//       Added more descriptive status messages to provide more detailed user feedback.
//  09/19/2008 - J. Ritchie Carroll
//       Converted to C#.
//  10/22/2008 - Pinal C. Patel
//       Edited code comments.
//  09/14/2009 - Stephen C. Wills
//       Added new header and license agreement.
//  01/27/2011 - J. Ritchie Carroll
//       Modified internal operation to minimize risk of file dead lock and/or memory overload.
//  09/22/2011 - J. Ritchie Carroll
//       Added Mono implementation exception regions.
//
//*******************************************************************************************************

#region [ TVA Open Source Agreement ]
/*

 THIS OPEN SOURCE AGREEMENT ("AGREEMENT") DEFINES THE RIGHTS OF USE,REPRODUCTION, DISTRIBUTION,
 MODIFICATION AND REDISTRIBUTION OF CERTAIN COMPUTER SOFTWARE ORIGINALLY RELEASED BY THE
 TENNESSEE VALLEY AUTHORITY, A CORPORATE AGENCY AND INSTRUMENTALITY OF THE UNITED STATES GOVERNMENT
 ("GOVERNMENT AGENCY"). GOVERNMENT AGENCY IS AN INTENDED THIRD-PARTY BENEFICIARY OF ALL SUBSEQUENT
 DISTRIBUTIONS OR REDISTRIBUTIONS OF THE SUBJECT SOFTWARE. ANYONE WHO USES, REPRODUCES, DISTRIBUTES,
 MODIFIES OR REDISTRIBUTES THE SUBJECT SOFTWARE, AS DEFINED HEREIN, OR ANY PART THEREOF, IS, BY THAT
 ACTION, ACCEPTING IN FULL THE RESPONSIBILITIES AND OBLIGATIONS CONTAINED IN THIS AGREEMENT.

 Original Software Designation: openPDC
 Original Software Title: The GSF Open Source Phasor Data Concentrator
 User Registration Requested. Please Visit https://naspi.tva.com/Registration/
 Point of Contact for Original Software: J. Ritchie Carroll <mailto:jrcarrol@tva.gov>

 1. DEFINITIONS

 A. "Contributor" means Government Agency, as the developer of the Original Software, and any entity
 that makes a Modification.

 B. "Covered Patents" mean patent claims licensable by a Contributor that are necessarily infringed by
 the use or sale of its Modification alone or when combined with the Subject Software.

 C. "Display" means the showing of a copy of the Subject Software, either directly or by means of an
 image, or any other device.

 D. "Distribution" means conveyance or transfer of the Subject Software, regardless of means, to
 another.

 E. "Larger Work" means computer software that combines Subject Software, or portions thereof, with
 software separate from the Subject Software that is not governed by the terms of this Agreement.

 F. "Modification" means any alteration of, including addition to or deletion from, the substance or
 structure of either the Original Software or Subject Software, and includes derivative works, as that
 term is defined in the Copyright Statute, 17 USC § 101. However, the act of including Subject Software
 as part of a Larger Work does not in and of itself constitute a Modification.

 G. "Original Software" means the computer software first released under this Agreement by Government
 Agency entitled openPDC, including source code, object code and accompanying documentation, if any.

 H. "Recipient" means anyone who acquires the Subject Software under this Agreement, including all
 Contributors.

 I. "Redistribution" means Distribution of the Subject Software after a Modification has been made.

 J. "Reproduction" means the making of a counterpart, image or copy of the Subject Software.

 K. "Sale" means the exchange of the Subject Software for money or equivalent value.

 L. "Subject Software" means the Original Software, Modifications, or any respective parts thereof.

 M. "Use" means the application or employment of the Subject Software for any purpose.

 2. GRANT OF RIGHTS

 A. Under Non-Patent Rights: Subject to the terms and conditions of this Agreement, each Contributor,
 with respect to its own contribution to the Subject Software, hereby grants to each Recipient a
 non-exclusive, world-wide, royalty-free license to engage in the following activities pertaining to
 the Subject Software:

 1. Use

 2. Distribution

 3. Reproduction

 4. Modification

 5. Redistribution

 6. Display

 B. Under Patent Rights: Subject to the terms and conditions of this Agreement, each Contributor, with
 respect to its own contribution to the Subject Software, hereby grants to each Recipient under Covered
 Patents a non-exclusive, world-wide, royalty-free license to engage in the following activities
 pertaining to the Subject Software:

 1. Use

 2. Distribution

 3. Reproduction

 4. Sale

 5. Offer for Sale

 C. The rights granted under Paragraph B. also apply to the combination of a Contributor's Modification
 and the Subject Software if, at the time the Modification is added by the Contributor, the addition of
 such Modification causes the combination to be covered by the Covered Patents. It does not apply to
 any other combinations that include a Modification. 

 D. The rights granted in Paragraphs A. and B. allow the Recipient to sublicense those same rights.
 Such sublicense must be under the same terms and conditions of this Agreement.

 3. OBLIGATIONS OF RECIPIENT

 A. Distribution or Redistribution of the Subject Software must be made under this Agreement except for
 additions covered under paragraph 3H. 

 1. Whenever a Recipient distributes or redistributes the Subject Software, a copy of this Agreement
 must be included with each copy of the Subject Software; and

 2. If Recipient distributes or redistributes the Subject Software in any form other than source code,
 Recipient must also make the source code freely available, and must provide with each copy of the
 Subject Software information on how to obtain the source code in a reasonable manner on or through a
 medium customarily used for software exchange.

 B. Each Recipient must ensure that the following copyright notice appears prominently in the Subject
 Software:

          No copyright is claimed pursuant to 17 USC § 105.  All Other Rights Reserved.

 C. Each Contributor must characterize its alteration of the Subject Software as a Modification and
 must identify itself as the originator of its Modification in a manner that reasonably allows
 subsequent Recipients to identify the originator of the Modification. In fulfillment of these
 requirements, Contributor must include a file (e.g., a change log file) that describes the alterations
 made and the date of the alterations, identifies Contributor as originator of the alterations, and
 consents to characterization of the alterations as a Modification, for example, by including a
 statement that the Modification is derived, directly or indirectly, from Original Software provided by
 Government Agency. Once consent is granted, it may not thereafter be revoked.

 D. A Contributor may add its own copyright notice to the Subject Software. Once a copyright notice has
 been added to the Subject Software, a Recipient may not remove it without the express permission of
 the Contributor who added the notice.

 E. A Recipient may not make any representation in the Subject Software or in any promotional,
 advertising or other material that may be construed as an endorsement by Government Agency or by any
 prior Recipient of any product or service provided by Recipient, or that may seek to obtain commercial
 advantage by the fact of Government Agency's or a prior Recipient's participation in this Agreement.

 F. In an effort to track usage and maintain accurate records of the Subject Software, each Recipient,
 upon receipt of the Subject Software, is requested to register with Government Agency by visiting the
 following website: https://naspi.tva.com/Registration/. Recipient's name and personal information
 shall be used for statistical purposes only. Once a Recipient makes a Modification available, it is
 requested that the Recipient inform Government Agency at the web site provided above how to access the
 Modification.

 G. Each Contributor represents that that its Modification does not violate any existing agreements,
 regulations, statutes or rules, and further that Contributor has sufficient rights to grant the rights
 conveyed by this Agreement.

 H. A Recipient may choose to offer, and to charge a fee for, warranty, support, indemnity and/or
 liability obligations to one or more other Recipients of the Subject Software. A Recipient may do so,
 however, only on its own behalf and not on behalf of Government Agency or any other Recipient. Such a
 Recipient must make it absolutely clear that any such warranty, support, indemnity and/or liability
 obligation is offered by that Recipient alone. Further, such Recipient agrees to indemnify Government
 Agency and every other Recipient for any liability incurred by them as a result of warranty, support,
 indemnity and/or liability offered by such Recipient.

 I. A Recipient may create a Larger Work by combining Subject Software with separate software not
 governed by the terms of this agreement and distribute the Larger Work as a single product. In such
 case, the Recipient must make sure Subject Software, or portions thereof, included in the Larger Work
 is subject to this Agreement.

 J. Notwithstanding any provisions contained herein, Recipient is hereby put on notice that export of
 any goods or technical data from the United States may require some form of export license from the
 U.S. Government. Failure to obtain necessary export licenses may result in criminal liability under
 U.S. laws. Government Agency neither represents that a license shall not be required nor that, if
 required, it shall be issued. Nothing granted herein provides any such export license.

 4. DISCLAIMER OF WARRANTIES AND LIABILITIES; WAIVER AND INDEMNIFICATION

 A. No Warranty: THE SUBJECT SOFTWARE IS PROVIDED "AS IS" WITHOUT ANY WARRANTY OF ANY KIND, EITHER
 EXPRESSED, IMPLIED, OR STATUTORY, INCLUDING, BUT NOT LIMITED TO, ANY WARRANTY THAT THE SUBJECT
 SOFTWARE WILL CONFORM TO SPECIFICATIONS, ANY IMPLIED WARRANTIES OF MERCHANTABILITY, FITNESS FOR A
 PARTICULAR PURPOSE, OR FREEDOM FROM INFRINGEMENT, ANY WARRANTY THAT THE SUBJECT SOFTWARE WILL BE ERROR
 FREE, OR ANY WARRANTY THAT DOCUMENTATION, IF PROVIDED, WILL CONFORM TO THE SUBJECT SOFTWARE. THIS
 AGREEMENT DOES NOT, IN ANY MANNER, CONSTITUTE AN ENDORSEMENT BY GOVERNMENT AGENCY OR ANY PRIOR
 RECIPIENT OF ANY RESULTS, RESULTING DESIGNS, HARDWARE, SOFTWARE PRODUCTS OR ANY OTHER APPLICATIONS
 RESULTING FROM USE OF THE SUBJECT SOFTWARE. FURTHER, GOVERNMENT AGENCY DISCLAIMS ALL WARRANTIES AND
 LIABILITIES REGARDING THIRD-PARTY SOFTWARE, IF PRESENT IN THE ORIGINAL SOFTWARE, AND DISTRIBUTES IT
 "AS IS."

 B. Waiver and Indemnity: RECIPIENT AGREES TO WAIVE ANY AND ALL CLAIMS AGAINST GOVERNMENT AGENCY, ITS
 AGENTS, EMPLOYEES, CONTRACTORS AND SUBCONTRACTORS, AS WELL AS ANY PRIOR RECIPIENT. IF RECIPIENT'S USE
 OF THE SUBJECT SOFTWARE RESULTS IN ANY LIABILITIES, DEMANDS, DAMAGES, EXPENSES OR LOSSES ARISING FROM
 SUCH USE, INCLUDING ANY DAMAGES FROM PRODUCTS BASED ON, OR RESULTING FROM, RECIPIENT'S USE OF THE
 SUBJECT SOFTWARE, RECIPIENT SHALL INDEMNIFY AND HOLD HARMLESS  GOVERNMENT AGENCY, ITS AGENTS,
 EMPLOYEES, CONTRACTORS AND SUBCONTRACTORS, AS WELL AS ANY PRIOR RECIPIENT, TO THE EXTENT PERMITTED BY
 LAW.  THE FOREGOING RELEASE AND INDEMNIFICATION SHALL APPLY EVEN IF THE LIABILITIES, DEMANDS, DAMAGES,
 EXPENSES OR LOSSES ARE CAUSED, OCCASIONED, OR CONTRIBUTED TO BY THE NEGLIGENCE, SOLE OR CONCURRENT, OF
 GOVERNMENT AGENCY OR ANY PRIOR RECIPIENT.  RECIPIENT'S SOLE REMEDY FOR ANY SUCH MATTER SHALL BE THE
 IMMEDIATE, UNILATERAL TERMINATION OF THIS AGREEMENT.

 5. GENERAL TERMS

 A. Termination: This Agreement and the rights granted hereunder will terminate automatically if a
 Recipient fails to comply with these terms and conditions, and fails to cure such noncompliance within
 thirty (30) days of becoming aware of such noncompliance. Upon termination, a Recipient agrees to
 immediately cease use and distribution of the Subject Software. All sublicenses to the Subject
 Software properly granted by the breaching Recipient shall survive any such termination of this
 Agreement.

 B. Severability: If any provision of this Agreement is invalid or unenforceable under applicable law,
 it shall not affect the validity or enforceability of the remainder of the terms of this Agreement.

 C. Applicable Law: This Agreement shall be subject to United States federal law only for all purposes,
 including, but not limited to, determining the validity of this Agreement, the meaning of its
 provisions and the rights, obligations and remedies of the parties.

 D. Entire Understanding: This Agreement constitutes the entire understanding and agreement of the
 parties relating to release of the Subject Software and may not be superseded, modified or amended
 except by further written agreement duly executed by the parties.

 E. Binding Authority: By accepting and using the Subject Software under this Agreement, a Recipient
 affirms its authority to bind the Recipient to all terms and conditions of this Agreement and that
 Recipient hereby agrees to all terms and conditions herein.

 F. Point of Contact: Any Recipient contact with Government Agency is to be directed to the designated
 representative as follows: J. Ritchie Carroll <mailto:jrcarrol@tva.gov>.

*/
#endregion

#region [ Contributor License Agreements ]

//******************************************************************************************************
//
//  Copyright © 2011, Grid Protection Alliance.  All Rights Reserved.
//
//  The GPA licenses this file to you under the Eclipse Public License -v 1.0 (the "License"); you may
//  not use this file except in compliance with the License. You may obtain a copy of the License at:
//
//      http://www.opensource.org/licenses/eclipse-1.0.php
//
//  Unless agreed to in writing, the subject software distributed under the License is distributed on an
//  "AS-IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. Refer to the
//  License for the specific language governing permissions and limitations.
//
//******************************************************************************************************

#endregion

using GSF.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
//using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace GSF.IO
{
    /// <summary>
    /// Handles the exporting of a file to multiple destinations that are defined in the config file.
    /// </summary>
    /// <remarks>
    /// This class is useful for updating the same file on multiple servers (e.g., load balanced web server).
    /// </remarks>
    /// <example>
    /// This example shows the use <see cref="MultipleDestinationExporter"/> for exporting data to multiple locations:
    /// <code>
    /// using System;
    /// using GSF.IO;
    ///
    /// class Program
    /// {
    ///     static MultipleDestinationExporter s_exporter;
    ///
    ///     static void Main(string[] args)
    ///     {
    ///         s_exporter = new MultipleDestinationExporter();
    ///         s_exporter.Initialized += s_exporter_Initialized;
    ///         ExportDestination[] defaultDestinations = new ExportDestination[] 
    ///         {
    ///             new ExportDestination(@"\\server1\share\exportFile.txt", false, "domain", "user1", "password1"), 
    ///             new ExportDestination(@"\\server2\share\exportFile.txt", false, "domain", "user2", "password2")
    ///         };
    ///         // Initialize with the destinations where data is to be exported.
    ///         s_exporter.Initialize(defaultDestinations);
    ///
    ///         Console.ReadLine();
    ///     }
    ///
    ///     static void s_exporter_Initialized(object sender, EventArgs e)
    ///     {
    ///         // Export data to all defined locations after initialization.
    ///         s_exporter.ExportData("TEST DATA");
    ///     }
    /// }
    /// </code>
    /// This example shows the config file entry that can be used to specify the <see cref="ExportDestination"/> 
    /// used by the <see cref="MultipleDestinationExporter"/> when exporting data:
    /// <code>
    /// <![CDATA[
    /// <exportDestinations>
    ///   <add name="ExportTimeout" value="-1" description="Total allowed time for all exports to execute in milliseconds."
    ///     encrypted="false" />
    ///   <add name="ExportCount" value="2" description="Total number of export files to produce."
    ///     encrypted="false" />
    ///   <add name="ExportDestination1" value="\\server1\share\" description="Root path for export destination. Use UNC path (\\server\share) with no trailing slash for network shares."
    ///     encrypted="false" />
    ///   <add name="ExportDestination1.ConnectToShare" value="True" description="Set to True to attempt authentication to network share."
    ///     encrypted="false" />
    ///   <add name="ExportDestination1.Domain" value="domain" description="Domain used for authentication to network share (computer name for local accounts)."
    ///     encrypted="false" />
    ///   <add name="ExportDestination1.UserName" value="user1" description="User name used for authentication to network share."
    ///     encrypted="false" />
    ///   <add name="ExportDestination1.Password" value="l2qlAwAPihJjoThH+G53BUxzYsIkTE2yNBHLtd1WA3hysDhwDB82ouJb9n35QtG8"
    ///     description="Encrypted password used for authentication to network share."
    ///     encrypted="true" />
    ///   <add name="ExportDestination1.FileName" value="exportFile.txt" description="Path and file name of data export (do not include drive letter or UNC share). Prefix with slash when using UNC paths (\path\filename.txt)."
    ///     encrypted="false" />
    ///   <add name="ExportDestination2" value="\\server2\share\" description="Root path for export destination. Use UNC path (\\server\share) with no trailing slash for network shares."
    ///     encrypted="false" />
    ///   <add name="ExportDestination2.ConnectToShare" value="True" description="Set to True to attempt authentication to network share."
    ///     encrypted="false" />
    ///   <add name="ExportDestination2.Domain" value="domain" description="Domain used for authentication to network share (computer name for local accounts)."
    ///     encrypted="false" />
    ///   <add name="ExportDestination2.UserName" value="user2" description="User name used for authentication to network share."
    ///     encrypted="false" />
    ///   <add name="ExportDestination2.Password" value="l2qlAwAPihJjoThH+G53BYT6BXHQr13D6Asdibl0rDmlrgRXvJmCwcP8uvkFRHr9"
    ///     description="Encrypted password used for authentication to network share."
    ///     encrypted="true" />
    ///   <add name="ExportDestination2.FileName" value="exportFile.txt" description="Path and file name of data export (do not include drive letter or UNC share). Prefix with slash when using UNC paths (\path\filename.txt)."
    ///     encrypted="false" />
    /// </exportDestinations>
    /// ]]>
    /// </code>
    /// </example>
    /// <seealso cref="ExportDestination"/>
    //[ToolboxBitmap(typeof(MultipleDestinationExporter))]
    public class MultipleDestinationExporter : Component, ISupportLifecycle, ISupportInitialize, IProvideStatus, IPersistSettings
    {
        #region [ Members ]

        // Nested Types

        /// <summary>
        /// Defines state information for an export.
        /// </summary>
        private class ExportState : IDisposable
        {
            #region [ Members ]

            // Fields
            private bool m_disposed;

            #endregion

            #region [ Constructors ]

            /// <summary>
            /// Creates a new <see cref="ExportState"/>.
            /// </summary>
            public ExportState()
            {
                WaitHandle = new AutoResetEvent(false);
            }

            /// <summary>
            /// Releases the unmanaged resources before the <see cref="ExportState"/> object is reclaimed by <see cref="GC"/>.
            /// </summary>
            ~ExportState()
            {
                Dispose(false);
            }

            #endregion

            #region [ Properties ]

            /// <summary>
            /// Gets or sets the source file name for the <see cref="ExportState"/>.
            /// </summary>
            public string SourceFileName
            {
                get;
                set;
            }

            /// <summary>
            /// Gets or sets the destination file name for the <see cref="ExportState"/>.
            /// </summary>
            public string DestinationFileName
            {
                get;
                set;
            }

            /// <summary>
            /// Gets or sets the event wait handle for the <see cref="ExportState"/>.
            /// </summary>
            public AutoResetEvent WaitHandle
            {
                get;
                set;
            }

            /// <summary>
            /// Gets or sets a flag that is used to determine if export process has timed out.
            /// </summary>
            public bool Timeout
            {
                get;
                set;
            }

            #endregion

            #region [ Methods ]

            /// <summary>
            /// Releases all the resources used by the <see cref="ExportState"/> object.
            /// </summary>
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            /// <summary>
            /// Releases the unmanaged resources used by the <see cref="ExportState"/> object and optionally releases the managed resources.
            /// </summary>
            /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
            protected virtual void Dispose(bool disposing)
            {
                if (!m_disposed)
                {
                    try
                    {
                        if (disposing)
                        {
                            if ((object)WaitHandle != null)
                                WaitHandle.Close();

                            WaitHandle = null;
                        }
                    }
                    finally
                    {
                        m_disposed = true;  // Prevent duplicate dispose.
                    }
                }
            }

            #endregion
        }

        // Constants

        /// <summary>
        /// Specifies the default value for the <see cref="ExportTimeout"/> property.
        /// </summary>
        public const int DefaultExportTimeout = Timeout.Infinite;

        /// <summary>
        /// Specifies the default value for the <see cref="PersistSettings"/> property.
        /// </summary>
        public const bool DefaultPersistSettings = true;

        /// <summary>
        /// Specifies the default value for the <see cref="SettingsCategory"/> property.
        /// </summary>
        public const string DefaultSettingsCategory = "ExportDestinations";

        /// <summary>
        /// Specifies the default value for the <see cref="MaximumRetryAttempts"/> property.
        /// </summary>
        public const int DefaultMaximumRetryAttempts = 4; // That is 4 retries plus the original attempt for a total of 5 attempts

        /// <summary>
        /// Specifies the default value for the <see cref="RetryDelayInterval"/> property.
        /// </summary>
        public const int DefaultRetryDelayInterval = 1000;

        // Events

        /// <summary>
        /// Occurs when the <see cref="MultipleDestinationExporter"/> object has been initialized.
        /// </summary>
        [Description("Occurs when the MultipleDestinationExporter object has been initialized.")]
        public event EventHandler Initialized;

        /// <summary>
        /// Occurs when status information for the <see cref="MultipleDestinationExporter"/> object is being reported.
        /// </summary>
        /// <remarks>
        /// <see cref="EventArgs{T}.Argument"/> is the status message being reported by the <see cref="MultipleDestinationExporter"/>.
        /// </remarks>
        [Description("Occurs when status information for the MultipleDestinationExporter object is being reported.")]
        public event EventHandler<EventArgs<string>> StatusMessage;

        /// <summary>
        /// Event is raised when there is an exception encountered while processing.
        /// </summary>
        /// <remarks>
        /// <see cref="EventArgs{T}.Argument"/> is the exception that was thrown.
        /// </remarks>
        [Description("Occurs when an exception occurs in the MultipleDestinationExporter.")]
        public event EventHandler<EventArgs<Exception>> ProcessException;

        // Fields
        private int m_exportTimeout;
        private bool m_persistSettings;
        private string m_settingsCategory;
        private long m_totalExports;
        private long m_failedExportAttempts;
        private Encoding m_textEncoding;
        private List<ExportDestination> m_exportDestinations;
        private bool m_exportInProgress;
        private object m_exportInProgressLock;
        private int m_maximumRetryAttempts;
        private int m_retryDelayInterval;
        private bool m_enabled;
        private bool m_disposed;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Initializes a new instance of the <see cref="MultipleDestinationExporter"/> class.
        /// </summary>
        public MultipleDestinationExporter()
            : this(DefaultSettingsCategory, DefaultExportTimeout)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultipleDestinationExporter"/> class.
        /// </summary>
        /// <param name="container"><see cref="IContainer"/> object that contains the <see cref="MultipleDestinationExporter"/>.</param>
        public MultipleDestinationExporter(IContainer container)
            : this()
        {
            if ((object)container != null)
                container.Add(this);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultipleDestinationExporter"/> class.
        /// </summary>
        /// <param name="settingsCategory">The config file settings category under which the export destinations are defined.</param>
        /// <param name="exportTimeout">The total allowed time in milliseconds for each export to execute.</param>
        public MultipleDestinationExporter(string settingsCategory, int exportTimeout)
            : base()
        {
            m_exportTimeout = exportTimeout;
            m_settingsCategory = settingsCategory;
            m_persistSettings = DefaultPersistSettings;
            m_maximumRetryAttempts = DefaultMaximumRetryAttempts;
            m_retryDelayInterval = DefaultRetryDelayInterval;
            m_textEncoding = Encoding.Default; // We use default ANSI page encoding for text based exports...
            m_exportInProgressLock = new object();
        }

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets or sets the total allowed time in milliseconds for each export to execute.
        /// </summary>
        /// <remarks>
        /// Set to Timeout.Infinite (-1) for no timeout.
        /// </remarks>
        [Category("Settings"), DefaultValue(DefaultExportTimeout), Description("Total allowed time for each export to execute, in milliseconds. Set to -1 for no specific timeout.")]
        public int ExportTimeout
        {
            get
            {
                return m_exportTimeout;
            }
            set
            {
                m_exportTimeout = value;
            }
        }

        /// <summary>
        /// Gets or sets the maximum number of retries that will be attempted during an export if the export fails.
        /// </summary>
        /// <remarks>
        /// Total file export attempts = 1 + <see cref="MaximumRetryAttempts"/>. Set to zero to only attempt export once.
        /// </remarks>
        [Category("Settings"), DefaultValue(DefaultMaximumRetryAttempts), Description("Maximum number of retries that will be attempted during an export if the export fails. Set to zero to only attempt export once.")]
        public int MaximumRetryAttempts
        {
            get
            {
                return m_maximumRetryAttempts;
            }
            set
            {
                m_maximumRetryAttempts = value;

                if (m_maximumRetryAttempts < 0)
                    m_maximumRetryAttempts = 0;
            }
        }

        /// <summary>
        /// Gets or sets the interval to wait, in milliseconds, before retrying an export if the export fails.
        /// </summary>
        [Category("Settings"), DefaultValue(DefaultRetryDelayInterval), Description("Interval to wait, in milliseconds, before retrying an export if the export fails.")]
        public int RetryDelayInterval
        {
            get
            {
                return m_retryDelayInterval;
            }
            set
            {
                m_retryDelayInterval = value;

                if (m_retryDelayInterval <= 0)
                    m_retryDelayInterval = DefaultRetryDelayInterval;
            }
        }

        /// <summary>
        /// Gets or sets a boolean value that indicates whether the settings of <see cref="MultipleDestinationExporter"/> object are 
        /// to be saved to the config file.
        /// </summary>
        [Category("Persistance"), DefaultValue(DefaultPersistSettings), Description("Indicates whether the settings of MultipleDestinationExporter object are to be saved to the config file.")]
        public bool PersistSettings
        {
            get
            {
                return m_persistSettings;
            }
            set
            {
                m_persistSettings = value;
            }
        }

        /// <summary>
        /// Gets or sets the category under which the settings of <see cref="MultipleDestinationExporter"/> object are to be saved
        /// to the config file if the <see cref="PersistSettings"/> property is set to true.
        /// </summary>
        /// <exception cref="ArgumentNullException">The value being assigned is null or empty string.</exception>
        [Category("Persistance"), DefaultValue(DefaultSettingsCategory), Description("Category under which the settings of MultipleDestinationExporter object are to be saved to the config file if the PersistSettings property is set to true.")]
        public string SettingsCategory
        {
            get
            {
                return m_settingsCategory;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                    throw new ArgumentNullException("value");

                m_settingsCategory = value;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="Encoding"/> to be used to encode text data being exported.
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public virtual Encoding TextEncoding
        {
            get
            {
                return m_textEncoding;
            }
            set
            {
                if ((object)value == null)
                {
                    m_textEncoding = Encoding.Default;
                }
                else
                {
                    m_textEncoding = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets a boolean value that indicates whether the <see cref="MultipleDestinationExporter"/> object is currently enabled.
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Enabled
        {
            get
            {
                return m_enabled;
            }
            set
            {
                m_enabled = value;
            }
        }

        /// <summary>
        /// Gets the total number exports performed successfully.
        /// </summary>
        [Browsable(false)]
        public long TotalExports
        {
            get
            {
                return m_totalExports;
            }
        }

        /// <summary>
        /// Gets a list of currently defined <see cref="ExportDestination"/>.
        /// </summary>
        /// <remarks>
        /// Use the <see cref="Initialize(IEnumerable{ExportDestination})"/> method to change the export destination collection.
        /// </remarks>
        [Category("Settings"), DesignerSerializationVisibility(DesignerSerializationVisibility.Content), Description("Gets a list of all the defined export destinations to be used by the MultipleDestinationExporter.")]
        public ReadOnlyCollection<ExportDestination> ExportDestinations
        {
            get
            {
                lock (this)
                {
                    if ((object)m_exportDestinations != null)
                        return new ReadOnlyCollection<ExportDestination>(m_exportDestinations);
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the unique identifier of the <see cref="MultipleDestinationExporter"/> object.
        /// </summary>
        [Browsable(false)]
        public string Name
        {
            get
            {
                // We just return the settings category name for unique identification of this component
                return m_settingsCategory;
            }
        }

        /// <summary>
        /// Gets the descriptive status of the <see cref="MultipleDestinationExporter"/> object.
        /// </summary>
        [Browsable(false)]
        public string Status
        {
            get
            {
                StringBuilder status = new StringBuilder();

                status.Append("     Configuration section: ");
                status.Append(m_settingsCategory);
                status.AppendLine();
                status.Append("            Export enabled: ");
                status.Append(m_enabled);
                status.AppendLine();
                status.Append("       Temporary file path: ");
                status.Append(Path.GetTempPath());
                status.AppendLine();
                status.AppendLine("       Export destinations: ");
                status.AppendLine();
                lock (this)
                {
                    int count = 1;

                    foreach (ExportDestination export in m_exportDestinations)
                    {
                        status.AppendFormat("         {0}: {1}\r\n", count.ToString().PadLeft(2, '0'), FilePath.TrimFileName(export.DestinationFile, 65));
                        count++;
                    }
                }
                status.AppendLine();
                status.Append("       File export timeout: ");
                status.Append(m_exportTimeout == Timeout.Infinite ? "Infinite" : m_exportTimeout + " milliseconds");
                status.AppendLine();
                status.Append("    Maximum retry attempts: ");
                status.Append(m_maximumRetryAttempts);
                status.AppendLine();
                status.Append("      Retry delay interval: ");
                status.Append(m_retryDelayInterval.ToString() + " milliseconds");
                status.AppendLine();
                status.Append("    Failed export attempts: ");
                status.Append(m_failedExportAttempts);
                status.AppendLine();
                status.Append("      Total exports so far: ");
                status.Append(m_totalExports);
                status.AppendLine();

                return status.ToString();
            }
        }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="MultipleDestinationExporter"/> object and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (!m_disposed)
            {
                try
                {
                    // This will be done regardless of whether the object is finalized or disposed.
                    if (disposing)
                    {
                        // This will be done only when the object is disposed by calling Dispose().
                        Shutdown();
                        SaveSettings();
                    }
                }
                finally
                {
                    m_disposed = true;          // Prevent duplicate dispose.
                    base.Dispose(disposing);    // Call base class Dispose().
                }
            }
        }

        /// <summary>
        /// Performs necessary operations before the <see cref="MultipleDestinationExporter"/> object properties are initialized.
        /// </summary>
        /// <remarks>
        /// <see cref="BeginInit()"/> should never be called by user-code directly. This method exists solely for use 
        /// by the designer if the <see cref="MultipleDestinationExporter"/> object is consumed through the designer surface of the IDE.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void BeginInit()
        {
            if (!DesignMode)
            {
                try
                {
                    // Nothing needs to be done before component is initialized.
                }
                catch (Exception)
                {
                    // Prevent the IDE from crashing when component is in design mode.
                }
            }
        }

        /// <summary>
        /// Performs necessary operations after the <see cref="MultipleDestinationExporter"/> object properties are initialized.
        /// </summary>
        /// <remarks>
        /// <see cref="EndInit()"/> should never be called by user-code directly. This method exists solely for use 
        /// by the designer if the <see cref="MultipleDestinationExporter"/> object is consumed through the designer surface of the IDE.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void EndInit()
        {
            if (!DesignMode)
            {
                try
                {
                    Initialize();
                }
                catch (Exception)
                {
                    // Prevent the IDE from crashing when component is in design mode.
                }
            }
        }

        /// <summary>
        /// Saves settings for the <see cref="MultipleDestinationExporter"/> object to the config file if the <see cref="PersistSettings"/> 
        /// property is set to true.
        /// </summary>
        /// <exception cref="ConfigurationErrorsException"><see cref="SettingsCategory"/> has a value of null or empty string.</exception>
        public void SaveSettings()
        {
            //if (m_persistSettings)
            //{
            //    // Ensure that settings category is specified.
            //    if (string.IsNullOrEmpty(m_settingsCategory))
            //        throw new ConfigurationErrorsException("SettingsCategory property has not been set");

            //    // Save settings under the specified category.
            //    ConfigurationFile config = ConfigurationFile.Current;
            //    CategorizedSettingsElementCollection settings = config.Settings[m_settingsCategory];

            //    settings.Clear();
            //    settings["ExportTimeout", true].Update(m_exportTimeout, "Total allowed time for each export to execute, in milliseconds. Set to -1 for no specific timeout.");
            //    settings["MaximumRetryAttempts", true].Update(m_maximumRetryAttempts, "Maximum number of retries that will be attempted during an export if the export fails. Set to zero to only attempt export once.");
            //    settings["RetryDelayInterval", true].Update(m_retryDelayInterval, "Interval to wait, in milliseconds, before retrying an export if the export fails.");

            //    lock (this)
            //    {
            //        settings["ExportCount", true].Update(m_exportDestinations.Count, "Total number of export files to produce.");
            //        for (int x = 0; x < m_exportDestinations.Count; x++)
            //        {
            //            settings[string.Format("ExportDestination{0}", x + 1), true].Update(m_exportDestinations[x].Share, "Root path for export destination. Use UNC path (\\\\server\\share) with no trailing slash for network shares.");
            //            settings[string.Format("ExportDestination{0}.ConnectToShare", x + 1), true].Update(m_exportDestinations[x].ConnectToShare, "Set to True to attempt authentication to network share.");
            //            settings[string.Format("ExportDestination{0}.Domain", x + 1), true].Update(m_exportDestinations[x].Domain, "Domain used for authentication to network share (computer name for local accounts).");
            //            settings[string.Format("ExportDestination{0}.UserName", x + 1), true].Update(m_exportDestinations[x].UserName, "User name used for authentication to network share.");
            //            settings[string.Format("ExportDestination{0}.Password", x + 1), true].Update(m_exportDestinations[x].Password, "Encrypted password used for authentication to network share.", true);
            //            settings[string.Format("ExportDestination{0}.FileName", x + 1), true].Update(m_exportDestinations[x].FileName, "Path and file name of data export (do not include drive letter or UNC share). Prefix with slash when using UNC paths (\\path\\filename.txt).");
            //        }
            //    }
            //    config.Save();
            //}
        }

        /// <summary>
        /// Loads saved settings for the <see cref="MultipleDestinationExporter"/> object from the config file if the <see cref="PersistSettings"/> 
        /// property is set to true.
        /// </summary>
        /// <exception cref="ConfigurationErrorsException"><see cref="SettingsCategory"/> has a value of null or empty string.</exception>
        public void LoadSettings()
        {
            //if (m_persistSettings)
            //{
            //    // Ensure that settings category is specified.
            //    if (string.IsNullOrEmpty(m_settingsCategory))
            //        throw new ConfigurationErrorsException("SettingsCategory property has not been set");

            //    // Load settings from the specified category.
            //    ConfigurationFile config = ConfigurationFile.Current;
            //    CategorizedSettingsElementCollection settings = config.Settings[m_settingsCategory];

            //    if (settings.Count == 0)
            //        return;    // Don't proceed if export destinations don't exist in config file.

            //    ExportDestination destination;
            //    string entryRoot;
            //    int count;

            //    m_exportTimeout = settings["ExportTimeout", true].ValueAs(m_exportTimeout);
            //    m_maximumRetryAttempts = settings["MaximumRetryAttempts", true].ValueAs(m_maximumRetryAttempts);
            //    m_retryDelayInterval = settings["RetryDelayInterval", true].ValueAs(m_retryDelayInterval);
            //    count = settings["ExportCount", true].ValueAsInt32();

            //    lock (this)
            //    {
            //        m_exportDestinations = new List<ExportDestination>(count);

            //        for (int x = 0; x < count; x++)
            //        {
            //            entryRoot = string.Format("ExportDestination{0}", x + 1);

            //            // Load export destination from configuration entries
            //            destination = new ExportDestination();
            //            destination.DestinationFile = settings[entryRoot, true].ValueAsString() + settings[string.Format("{0}.FileName", entryRoot), true].ValueAsString();
            //            destination.ConnectToShare = settings[string.Format("{0}.ConnectToShare", entryRoot), true].ValueAsBoolean();
            //            destination.Domain = settings[string.Format("{0}.Domain", entryRoot), true].ValueAsString();
            //            destination.UserName = settings[string.Format("{0}.UserName", entryRoot), true].ValueAsString();
            //            destination.Password = settings[string.Format("{0}.Password", entryRoot), true].ValueAsString();

            //            // Save new export destination if destination file name has been defined and is valid
            //            if (FilePath.IsValidFileName(destination.DestinationFile))
            //                m_exportDestinations.Add(destination);
            //        }
            //    }
            //}
        }

        /// <summary>
        /// Initializes (or reinitializes) <see cref="MultipleDestinationExporter"/> from configuration settings.
        /// </summary>
        /// <remarks>
        /// If not being used as a component (i.e., user creates their own instance of this class), this method
        /// must be called in order to initialize exports.  Event if used as a component this method can be
        /// called at anytime to reintialize the exports with new configuration information.
        /// </remarks>
        public void Initialize()
        {
            // We provide a simple default set of export destinations since no others are specified.
            Initialize(new ExportDestination[] { new ExportDestination("C:\\filename.txt", false, "domain", "username", "password") });
        }

        /// <summary>
        /// Initializes (or reinitializes) <see cref="MultipleDestinationExporter"/> from configuration settings.
        /// </summary>
        /// <param name="defaultDestinations">Provides a default set of export destinations if none exist in configuration settings.</param>
        /// <remarks>
        /// If not being used as a component (i.e., user creates their own instance of this class), this method
        /// must be called in order to initialize exports.  Even if used as a component this method can be
        /// called at anytime to reintialize the exports with new configuration information.
        /// </remarks>
        public void Initialize(IEnumerable<ExportDestination> defaultDestinations)
        {
            // So as to not delay calling thread due to share authentication, we perform initialization on another thread...
#if ThreadTracking
            ManagedThread thread = ManagedThreadPool.QueueUserWorkItem(Initialize, defaultDestinations.ToList());
            thread.Name = "TVA.IO.MultipleDestinationExporter.Initialize()";
#else
            ThreadPool.QueueUserWorkItem(Initialize, defaultDestinations.ToList());
#endif
        }

        private void Initialize(object state)
        {
            // In case we are reinitializing class, we shutdown any prior queue operations and close any existing network connections...
            Shutdown();

            // Retrieve any specified default export destinations
            lock (this)
            {
                m_exportDestinations = state as List<ExportDestination>;
            }

            // Load export destinations from the config file - if nothing is in config file yet,
            // the default settings (passed in via state) will be used instead. Consumers
            // wishing to dynamically change export settings in code will need to make sure
            // PersistSettings is false in order to load specified code settings instead of
            // those that may be saved in the configuration file
            LoadSettings();

            ExportDestination[] destinations;

            lock (this)
            {
                // Cache a local copy of export destinations to reduce lock time,
                // network share authentication may take some time
                destinations = m_exportDestinations.ToArray();
            }

            for (int x = 0; x < destinations.Length; x++)
            {
                // Connect to network shares if necessary
                if (destinations[x].ConnectToShare)
                {
#if MONO
                    OnStatusMessage("Network share authentication is not available on Mono deployments. Requested authentication not attempted for: {0}\\{1} to {2}...", destinations[x].Domain, destinations[x].UserName, destinations[x].Share);
#else
                    // Attempt connection to external network share
                    try
                    {
                        OnStatusMessage("Attempting network share authentication for user {0}\\{1} to {2}...", destinations[x].Domain, destinations[x].UserName, destinations[x].Share);

                        FilePath.ConnectToNetworkShare(destinations[x].Share, destinations[x].UserName, destinations[x].Password, destinations[x].Domain);

                        OnStatusMessage("Network share authentication to {0} succeeded.", destinations[x].Share);
                    }
                    catch (Exception ex)
                    {
                        // Something unexpected happened during attempt to connect to network share - so we'll report it...
                        OnProcessException(new IOException(string.Format("Network share authentication to {0} failed due to exception: {1}", destinations[x].Share, ex.Message), ex));
                    }
#endif
                }
            }

            m_enabled = true;

            // Notify that initialization is complete.
            OnInitialized();
        }

        // This is all of the needed dispose functionality, but since the class can be re-initialized this is a separate method
        private void Shutdown()
        {
            m_enabled = false;

            lock (this)
            {
                if ((object)m_exportDestinations != null)
                {
                    // We'll be nice and disconnect network shares when this class is disposed...
                    for (int x = 0; x < m_exportDestinations.Count; x++)
                    {
#if !MONO
                        if (m_exportDestinations[x].ConnectToShare)
                        {
                            try
                            {
                                FilePath.DisconnectFromNetworkShare(m_exportDestinations[x].Share);
                            }
                            catch (Exception ex)
                            {
                                // Something unexpected happened during attempt to disconnect from network share - so we'll report it...
                                OnProcessException(new IOException(string.Format("Network share disconnect from {0} failed due to exception: {1}", m_exportDestinations[x].Share, ex.Message), ex));
                            }
                        }
#endif
                    }
                }
            }
        }

        /// <summary>
        /// Raises the <see cref="Initialized"/> event.
        /// </summary>
        protected virtual void OnInitialized()
        {
            if ((object)Initialized != null)
                Initialized(this, EventArgs.Empty);
        }

        /// <summary>
        /// Raises the <see cref="StatusMessage"/> event.
        /// </summary>
        /// <param name="status">Status message to report.</param>
        /// <param name="args"><see cref="string.Format(string,object[])"/> parameters used for status message.</param>
        protected virtual void OnStatusMessage(string status, params object[] args)
        {
            if ((object)StatusMessage != null)
                StatusMessage(this, new EventArgs<string>(string.Format(status, args)));
        }

        /// <summary>
        /// Raises <see cref="ProcessException"/> event.
        /// </summary>
        /// <param name="ex">Processing <see cref="Exception"/>.</param>
        protected virtual void OnProcessException(Exception ex)
        {
            if ((object)ProcessException != null)
                ProcessException(this, new EventArgs<Exception>(ex));
        }

        /// <summary>
        /// Start multiple file export.
        /// </summary>
        /// <param name="fileData">Text based data to export to each destination.</param>
        /// <remarks>
        /// This is assumed to be the full content of the file to export. This class does not queue data since
        /// the export is not intended to append to an existing file but rather replace an existing one.
        /// </remarks>
        public void ExportData(string fileData)
        {
            ExportData(m_textEncoding.GetBytes(fileData));
        }

        /// <summary>
        /// Start multiple file export.
        /// </summary>
        /// <param name="fileData">Binary data to export to each destination.</param>
        /// <remarks>
        /// This is assumed to be the full content of the file to export. This class does not queue data since
        /// the export is not intended to append to an existing file but rather replace an existing one.
        /// </remarks>
        public void ExportData(byte[] fileData)
        {
            if (m_enabled)
            {
                // Ensure that only one export will be queued and exporting at once
                lock (m_exportInProgressLock)
                {
                    if (m_exportInProgress)
                    {
                        throw new InvalidOperationException("Export failed: cannot export data while another export attempt is already in progress.");
                    }
                    else
                    {
                        m_exportInProgress = true;
                        ThreadPool.QueueUserWorkItem(ExecuteExports, fileData);
                    }
                }
            }
            else
            {
                throw new InvalidOperationException("Export failed: exporter is not currently enabled.");
            }
        }

        private void ExecuteExports(object state)
        {
            // Outer try/finally is used only to make sure m_exportInProgress state is reset regardless of success or failure of export
            try
            {
                // Dereference file bytes to be exported
                byte[] fileData = state as byte[];

                if (m_enabled && (object)fileData != null)
                {
                    string filename = null;
                    ExportState[] exportStates = null;
                    ExportDestination[] destinations;

                    try
                    {
                        //  Get a temporary file name
                        filename = Path.GetTempFileName();

                        // Export data to the temporary file
                        File.WriteAllBytes(filename, fileData);

                        lock (this)
                        {
                            // Cache a local copy of export destinations to reduce lock time
                            destinations = m_exportDestinations.ToArray();
                        }

                        // Define a new export state for each export destination
                        exportStates = new ExportState[destinations.Length];

                        for (int i = 0; i < exportStates.Length; i++)
                        {
                            exportStates[i] = new ExportState()
                            {
                                SourceFileName = filename,
                                DestinationFileName = destinations[i].DestinationFile
                            };
                        }

                        // Spool threads to attempt copy of export files
                        for (int i = 0; i < destinations.Length; i++)
                        {
                            ThreadPool.QueueUserWorkItem(CopyFileToDestination, exportStates[i]);
                        }

                        // Wait for exports to complete - even if user specifies to wait indefinitely spooled copy routines
                        // will eventually return since there is a specified maximum retry count
                        if (!WaitHandle.WaitAll(exportStates.Select(exportState => exportState.WaitHandle).ToArray(), m_exportTimeout))
                        {
                            // Exports failed to complete in specified allowed time, set timeout flag for each export state
                            Array.ForEach(exportStates, exportState => exportState.Timeout = true);
                            OnStatusMessage("Timed out attempting export, waited for {0}.", Ticks.FromMilliseconds(m_exportTimeout).ToElapsedTimeString(2).ToLower());
                        }
                    }
                    catch (ThreadAbortException)
                    {
                        throw;  // This exception is normal, we'll just rethrow this back up the try stack
                    }
                    catch (Exception ex)
                    {
                        OnProcessException(new InvalidOperationException(string.Format("Exception encountered during export preparation: {0}", ex.Message), ex));
                    }
                    finally
                    {
                        // Dispose the export state wait handles
                        if ((object)exportStates != null)
                        {
                            foreach (ExportState exportState in exportStates)
                            {
                                exportState.Dispose();
                            }
                        }

                        // Delete the temporary file - we queue this up in case the export threads may still be trying their last copy attempt
                        ThreadPool.QueueUserWorkItem(DeleteTemporaryFile, filename);
                    }
                }
            }
            finally
            {
                // Synchronously reset export progress state
                lock (m_exportInProgressLock)
                {
                    m_exportInProgress = false;
                }
            }
        }

        private void CopyFileToDestination(object state)
        {
            ExportState exportState = null;
            Exception exportException = null;
            int failedExportCount = 0;

            try
            {
                exportState = state as ExportState;

                if ((object)exportState != null)
                {
                    // File copy may fail if destination is locked, so we setup to retry this operation
                    // waiting the specified period between attempts
                    for (int attempt = 0; attempt < 1 + m_maximumRetryAttempts; attempt++)
                    {
                        try
                        {
                            // Attempt to copy file to destination, overwriting if it already exists
                            File.Copy(exportState.SourceFileName, exportState.DestinationFileName, true);
                        }
                        catch (ThreadAbortException)
                        {
                            throw;  // This exception is normal, we'll just rethrow this back up the try stack
                        }
                        catch (Exception ex)
                        {
                            // Stack exception history to provide a full inner exception failure log for each export attempt
                            if ((object)exportException == null)
                                exportException = ex;
                            else
                                exportException = new IOException(string.Format("Attempt {0} exception: {1}", attempt + 1, ex.Message), exportException);

                            failedExportCount++;

                            // Abort retry attempts if export has timed out or maximum exports have been attempted
                            if (!m_enabled || exportState.Timeout || attempt >= m_maximumRetryAttempts)
                                throw exportException;
                            else
                                Thread.Sleep(m_retryDelayInterval);
                        }
                    }

                    // Track successful exports
                    m_totalExports++;
                }
            }
            catch (ThreadAbortException)
            {
                throw;  // This exception is normal, we'll just rethrow this back up the try stack
            }
            catch (Exception ex)
            {
                string destinationFileName = null;
                bool timeout = false;

                if ((object)exportState != null)
                {
                    destinationFileName = exportState.DestinationFileName;
                    timeout = exportState.Timeout;
                }

                OnProcessException(new InvalidOperationException(string.Format("Export attempt aborted {0} {1} exception{2} for \"{3}\" - {4}", timeout ? "due to timeout with" : "after", failedExportCount, failedExportCount > 1 ? "s" : "", destinationFileName.ToNonNullString("[undefined]"), ex.Message), ex));
            }
            finally
            {
                // Release waiting thread
                if ((object)exportState != null && exportState.WaitHandle != null)
                    exportState.WaitHandle.Set();

                // Track total number of failed export attempts
                Interlocked.Add(ref m_failedExportAttempts, failedExportCount);
            }
        }

        private void DeleteTemporaryFile(object state)
        {
            string filename = state as string;

            if (!string.IsNullOrEmpty(filename))
            {
                try
                {
                    // Hold up for the specified retry time in case the export threads may still be trying their last copy attempt. This is important
                    // if the timeouts are synchronized and there is one more export about to be attempted before the timeout flag is checked.
                    Thread.Sleep(m_retryDelayInterval);

                    // Delete the temporary file
                    if (File.Exists(filename))
                        File.Delete(filename);
                }
                catch (ThreadAbortException)
                {
                    throw;  // This exception is normal, we'll just rethrow this back up the try stack
                }
                catch (Exception ex)
                {
                    // Although errors are not expected from deleting the temporary file, we report any that may occur
                    OnProcessException(new InvalidOperationException(string.Format("Exception encountered while trying to remove temporary file: {0}", ex.Message), ex));
                }
            }
        }

        #endregion
    }
}