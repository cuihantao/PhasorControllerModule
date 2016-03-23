﻿//******************************************************************************************************
//  ClientRequestInfo.cs - Gbtc
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
//  05/02/2007 - Pinal C. Patel
//       Generated original version of source code.
//  09/14/2009 - Stephen C. Wills
//       Added new header and license agreement.
//  12/20/2012 - Starlynn Danyelle Gilliam
//       Modified Header.
//
//******************************************************************************************************

using System;

namespace GSF.ServiceProcess
{
    /// <summary>
    /// Represents information about a <see cref="ClientRequest"/> sent by <see cref="ClientHelper"/>.
    /// </summary>
    /// <seealso cref="ClientInfo"/>
    /// <seealso cref="ClientRequest"/>
    public class ClientRequestInfo
    {
        #region [ Constructors ]

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientRequestInfo"/> class.
        /// </summary>
        /// <param name="sender"><see cref="ClientInfo"/> object of the <paramref name="request"/> sender.</param>
        /// <param name="request"><see cref="ClientRequest"/> object sent by the <paramref name="sender"/>.</param>
        public ClientRequestInfo(ClientInfo sender, ClientRequest request)
        {
            Request = request;
            Sender = sender;
            ReceivedAt = DateTime.UtcNow;
        }

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets or sets the <see cref="ClientInfo"/> object of the <see cref="Request"/> sender.
        /// </summary>
        public ClientInfo Sender
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the <see cref="ClientRequest"/> object sent by the <see cref="Sender"/>.
        /// </summary>
        public ClientRequest Request
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the <see cref="DateTime"/> when the <see cref="Request"/> was received from the <see cref="Sender"/>.
        /// </summary>
        public DateTime ReceivedAt
        {
            get;
            set;
        }

        #endregion
    }
}
