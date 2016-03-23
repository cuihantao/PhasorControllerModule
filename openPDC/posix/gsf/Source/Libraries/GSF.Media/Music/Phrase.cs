﻿//******************************************************************************************************
//  Phrase.cs - Gbtc
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
//  07/29/2009 - J. Ritchie Carroll
//       Generated original version of source code.
//  09/14/2009 - Stephen C. Wills
//       Added new header and license agreement.
//  12/14/2012 - Starlynn Danyelle Gilliam
//       Modified Header.
//
//******************************************************************************************************

#region [ Contributor License Agreements ]

/**************************************************************************\
   Copyright © 2009 - J. Ritchie Carroll
   All rights reserved.
  
   Redistribution and use in source and binary forms, with or without
   modification, are permitted provided that the following conditions
   are met:
  
      * Redistributions of source code must retain the above copyright
        notice, this list of conditions and the following disclaimer.
       
      * Redistributions in binary form must reproduce the above
        copyright notice, this list of conditions and the following
        disclaimer in the documentation and/or other materials provided
        with the distribution.
  
   THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDER "AS IS" AND ANY
   EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
   IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
   PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
   CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
   EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
   PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
   PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY
   OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
   (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
   OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
  
\**************************************************************************/

#endregion

using System.Collections.Generic;

namespace GSF.Media.Music
{
    /// <summary>
    /// Defines a repeatable series of notes that can be added to a song over and over,
    /// for example, the phrase of music defining the chorus.
    /// </summary>
	public class Phrase
	{
        #region [ Members ]

        // Fields
        private readonly List<Note[]> m_notes;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Constructs a new <see cref="Phrase"/>.
        /// </summary>
        public Phrase()
        {
            m_notes = new List<Note[]>();
        }

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Series of notes that define the phrase.
        /// </summary>
        public List<Note[]> Notes
        {
            get
            {
                return m_notes;
            }
        }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Add another predefined phrase of notes to this phrase.
        /// </summary>
        /// <param name="phrase">Phrase to add.</param>
        public void AddPhrase(Phrase phrase)
        {
            foreach (Note[] notes in phrase.Notes)
            {
                AddNotes(notes);
            }
        }

        /// <summary>
        /// Add a series of notes to the phrase.
        /// </summary>
        /// <param name="notes">Notes to add.</param>
        public void AddNotes(params Note[] notes)
        {
            m_notes.Add(notes);
        }

        #endregion
	}
}
