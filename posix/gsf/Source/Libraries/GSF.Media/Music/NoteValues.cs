﻿//******************************************************************************************************
//  NoteValues.cs - Gbtc
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

using System;

namespace GSF.Media.Music
{
    /// <summary>American note value (♪) representing the relative duration of a note.</summary>
    /// <remarks>Note duration formula accessible via extension function "Duration()" for given note value.</remarks>
    public enum NoteValue
    {
        /// <summary>Quadruple whole note (i.e., 4 times the length of a whole note).</summary>
        Longa,
        /// <summary>Double whole note (i.e., 2 times the length of a whole note).</summary>
        Breve,
        /// <summary>Whole note.</summary>
        Whole,
        /// <summary>Half note (i.e., 1/2 the length of a whole note).</summary>
        Half,
        /// <summary>Quarter note (i.e., 1/4 the length of a whole note).</summary>
        Quarter,
        /// <summary>Eighth note (i.e., 1/8 the length of a whole note).</summary>
        Eighth,
        /// <summary>Sixteenth note (i.e., 1/16 the length of a whole note).</summary>
        Sixteenth,
        /// <summary>ThirtySecond note (i.e., 1/32 the length of a whole note).</summary>
        ThirtySecond,
        /// <summary>SixtyFourth note (i.e., 1/64 the length of a whole note).</summary>
        SixtyFourth,
        /// <summary>HundredTwentyEighth note (i.e., 1/128 the length of a whole note).</summary>
        HundredTwentyEighth,
        /// <summary>TwoHundredFiftySixth note (i.e., 1/256 the length of a whole note).</summary>
        TwoHundredFiftySixth,
        /// <summary>FiveHundredTwelfth note (i.e., 1/512 the length of a whole note).</summary>
        FiveHundredTwelfth,
        /// <summary>ThousandTwentyFourth note (i.e., 1/1024 the length of a whole note).</summary>
        ThousandTwentyFourth
    }

    /// <summary>British note value (♪) representing the relative duration of a note.</summary>
    /// <remarks>Note duration formula accessible via extension function "Duration()" for given note value.</remarks>
    public enum NoteValueBritish
    {
        /// <summary>Quadruple whole note (i.e., 4 times the length of a whole note).</summary>
        Longa,
        /// <summary>Double whole note (i.e., 2 times the length of a whole note).</summary>
        Breve,
        /// <summary>Whole note.</summary>
        Semibreve,
        /// <summary>Half note (i.e., 1/2 the length of a whole note).</summary>
        Minim,
        /// <summary>Quarter note (i.e., 1/4 the length of a whole note).</summary>
        Crotchet,
        /// <summary>Eighth note (i.e., 1/8 the length of a whole note).</summary>
        Quaver,
        /// <summary>Sixteenth note (i.e., 1/16 the length of a whole note).</summary>
        Semiquaver,
        /// <summary>ThirtySecond note (i.e., 1/32 the length of a whole note).</summary>
        Demisemiquaver,
        /// <summary>SixtyFourth note (i.e., 1/64 the length of a whole note).</summary>
        Hemidemisemiquaver,
        /// <summary>HundredTwentyEighth note (i.e., 1/128 the length of a whole note).</summary>
        Quasihemidemisemiquaver,
        /// <summary>TwoHundredFiftySixth note (i.e., 1/256 the length of a whole note).</summary>
        TwoHundredFiftySixth,
        /// <summary>FiveHundredTwelfth note (i.e., 1/512 the length of a whole note).</summary>
        FiveHundredTwelfth,
        /// <summary>ThousandTwentyFourth note (i.e., 1/1024 the length of a whole note).</summary>
        ThousandTwentyFourth
    }

    // Duration Equation
    //   Let    int i = index of desired note value
    //   Let    int r = index of reference note value
    //   Let double d = duration of note value i in terms of r
    //
    //   d = Math.Pow(2, 2 * (r - 1) - i) * Math.Pow(2, 2 - r);

    //                      // Index: Longa note duration:            offset = -2 - Index:
    //Longa,                //   0    1     = 1/4    * 4,     2^-2  = 4     -2
    //Breve,                //   1    1/2   = 1/8    * 4,     2^-3  = 8     -3        
    //Whole,                //   2    1/4   = 1/16   * 4,     2^-4  = 16    -4        
    //Half,                 //   3    1/8   = 1/32   * 4,     2^-5  = 32    -5        
    //Quarter,              //   4    1/16  = 1/64   * 4,     2^-6  = 64    -6
    //Eighth,               //   5    1/32  = 1/128  * 4,     2^-7  = 128   -7
    //Sixteenth,            //   6    1/64  = 1/256  * 4,     2^-8  = 256   -8
    //ThirtySecond,         //   7    1/128 = 1/512  * 4,     2^-9  = 512   -9
    //SixtyFourth,          //   8    1/256 = 1/1024 * 4,     2^-10 = 1024 -10
    //HundredTwentyEighth   //   9    1/512 = 1/2048 * 4,     2^-11 = 2048 -11

    //                      // Index: Breve note duration:            offset = 0 - Index:
    //Longa,                //   0    2     =   1    * 2,     2^0   = 1      0
    //Breve,                //   1    1     = 1/2    * 2,     2^-1  = 1/2   -1        
    //Whole,                //   2    1/2   = 1/4    * 2,     2^-2  = 1/4   -2        
    //Half,                 //   3    1/4   = 1/8    * 2,     2^-3  = 1/8   -3        
    //Quarter,              //   4    1/8   = 1/16   * 2,     2^-4  = 1/16  -4
    //Eighth,               //   5    1/16  = 1/32   * 2,     2^-5  = 1/32  -5
    //Sixteenth,            //   6    1/32  = 1/64   * 2,     2^-6  = 1/64  -6
    //ThirtySecond,         //   7    1/64  = 1/128  * 2,     2^-7  = 1/128 -7
    //SixtyFourth,          //   8    1/128 = 1/256  * 2,     2^-8  = 1/256 -8
    //HundredTwentyEighth   //   9    1/256 = 1/512  * 2,     2^-9  = 1/512 -9

    //                      // Index: Whole note Duration:            offset = 2 - Index:
    //Longa,                //   0    4     =   4    * 1,     2^2   = 4      2
    //Breve,                //   1    2     =   2    * 1,     2^1   = 2      1 
    //Whole,                //   2    1     =   1    * 1,     2^0   = 1      0
    //Half,                 //   3    1/2   = 1/2    * 1,     2^-1  = 1/2   -1
    //Quarter,              //   4    1/4   = 1/4    * 1,     2^-2  = 1/4   -2
    //Eighth,               //   5    1/8   = 1/8    * 1,     2^-3  = 1/8   -3
    //Sixteenth,            //   6    1/16  = 1/16   * 1,     2^-4  = 1/16  -4
    //ThirtySecond,         //   7    1/32  = 1/32   * 1,     2^-5  = 1/32  -5
    //SixtyFourth,          //   8    1/64  = 1/64   * 1,     2^-6  = 1/64  -6
    //HundredTwentyEighth   //   9    1/128 = 1/128  * 1,     2^-7  = 1/128 -7

    //                      // Index: Half note duration:             offset = 4 - Index:
    //Longa,                //   0    8     =  16    * (1/2), 2^4   = 16     4
    //Breve,                //   1    4     =   8    * (1/2), 2^3   = 8      3        
    //Whole,                //   2    2     =   4    * (1/2), 2^2   = 4      2        
    //Half,                 //   3    1     =   2    * (1/2), 2^1   = 2      1        
    //Quarter,              //   4    1/2   =   1    * (1/2), 2^0   = 1      0
    //Eighth,               //   5    1/4   = 1/2    * (1/2), 2^-1  = 1/2   -1
    //Sixteenth,            //   6    1/8   = 1/4    * (1/2), 2^-2  = 1/4   -2
    //ThirtySecond,         //   7    1/16  = 1/8    * (1/2), 2^-3  = 1/8   -3
    //SixtyFourth,          //   8    1/32  = 1/16   * (1/2), 2^-4  = 1/16  -4
    //HundredTwentyEighth   //   9    1/64  = 1/32   * (1/2), 2^-5  = 1/32  -5

    //                      // Index: Quarter note duration:          offset = 6 - Index:
    //Longa,                //   0    16    =  64    * (1/4), 2^6   = 64     6
    //Breve,                //   1    8     =  32    * (1/4), 2^5   = 32     5
    //Whole,                //   2    4     =  16    * (1/4), 2^4   = 16     4
    //Half,                 //   3    2     =   8    * (1/4), 2^3   = 8      3
    //Quarter,              //   4    1     =   4    * (1/4), 2^2   = 4      2
    //Eighth,               //   5    1/2   =   2    * (1/4), 2^1   = 2      1
    //Sixteenth,            //   6    1/4   =   1    * (1/4), 2^0   = 1      0
    //ThirtySecond,         //   7    1/8   = 1/2    * (1/4), 2^-1  = 1/2   -1
    //SixtyFourth,          //   8    1/16  = 1/4    * (1/4), 2^-2  = 1/4   -2
    //HundredTwentyEighth   //   9    1/32  = 1/8    * (1/4), 2^-3  = 1/8   -3

    /// <summary>Defines extension functions related to note value enumerations.</summary>
    public static class NoteValueExtensions
    {
        /// <summary>
        /// Returns source note value duration.  For example, 0.25 will be returned for
        /// a quater note, 1.0 will be returned for a whole note, etc.
        /// </summary>
        /// <param name="source">Source note value.</param>
        /// <returns>Duration of note value.</returns>
        public static double Duration(this NoteValue source)
        {
            return Duration((int)source, 0);
        }
        
        /// <summary>
        /// Returns source note value duration.  For example, 0.25 will be returned for
        /// a crotchet note, 1.0 will be returned for a semibreve note, etc.
        /// </summary>
        /// <param name="source">Source note value.</param>
        /// <returns>Duration of note value.</returns>
        public static double Duration(this NoteValueBritish source)
        {
            return Duration((int)source, 0);
        }

        /// <summary>
        /// Returns source note value duration.  For example, 0.25 will be returned for
        /// a quater note, 1.0 will be returned for a whole note, etc.
        /// </summary>
        /// <param name="source">Source note value.</param>
        /// <param name="dots">Total dotted note length extensions to apply.</param>
        /// <returns>Duration of note value.</returns>
        public static double Duration(this NoteValue source, int dots)
        {
            return Duration((int)source, dots);
        }

        /// <summary>
        /// Returns source note value duration.  For example, 0.25 will be returned for
        /// a crotchet note, 1.0 will be returned for a semibreve note, etc.
        /// </summary>
        /// <param name="source">Source note value.</param>
        /// <param name="dots">Total dotted note length extensions to apply.</param>
        /// <returns>Duration of note value.</returns>
        public static double Duration(this NoteValueBritish source, int dots)
        {
            return Duration((int)source, dots);
        }

        /// <summary>
        /// Returns source note value duration in terms of given reference note value.
        /// For example, if measure size is 3/4 then reference is quarter notes and returned
        /// value will be equivalent number of quarter notes for given source note.
        /// </summary>
        /// <param name="source">Source note value.</param>
        /// <param name="reference">Reference note value.</param>
        /// <returns>Duration of note value in terms of specified reference note value.</returns>
        public static double Duration(this NoteValue source, NoteValue reference)
        {
            return Duration((int)source, (int)reference, 0);
        }

        /// <summary>
        /// Returns source note value duration in terms of given reference note value.
        /// For example, if measure size is 3/4 then reference is quarter notes and returned
        /// value will be equivalent number of quarter notes for given source note.
        /// </summary>
        /// <param name="source">Source note value.</param>
        /// <param name="reference">Reference note value.</param>
        /// <returns>Duration of note value in terms of specified reference note value.</returns>
        public static double Duration(this NoteValueBritish source, NoteValueBritish reference)
        {
            return Duration((int)source, (int)reference, 0);
        }

        /// <summary>
        /// Returns source note value duration in terms of given reference note value.
        /// For example, if measure size is 3/4 then reference is quarter notes and returned
        /// value will be equivalent number of quarter notes for given source note.
        /// </summary>
        /// <param name="source">Source note value.</param>
        /// <param name="reference">Reference note value.</param>
        /// <param name="dots">Total dotted note length extensions to apply.</param>
        /// <returns>Duration of note value in terms of specified reference note value.</returns>
        public static double Duration(this NoteValue source, NoteValue reference, int dots)
        {
            return Duration((int)source, (int)reference, dots);
        }

        /// <summary>
        /// Returns source note value duration in terms of given reference note value.
        /// For example, if measure size is 3/4 then reference is quarter notes and returned
        /// value will be equivalent number of quarter notes for given source note.
        /// </summary>
        /// <param name="source">Source note value.</param>
        /// <param name="reference">Reference note value.</param>
        /// <param name="dots">Total dotted note length extensions to apply.</param>
        /// <returns>Duration of note value in terms of specified reference note value.</returns>
        public static double Duration(this NoteValueBritish source, NoteValueBritish reference, int dots)
        {
            return Duration((int)source, (int)reference, dots);
        }

        // Relative duration of given note r
        private static double Duration(int r, int dots)
        {
            return DotLength(Math.Pow(2, 2 - r), dots);
        }

        // Relative duration of given note i, in terms of r
        private static double Duration(int i, int r, int dots)
        {
            return DotLength(Math.Pow(2, 2 * (r - 1) - i) * Duration(r, 0), dots);
        }

        private static double DotLength(double duration, int dots)
        {
            if (dots > 0) return  2 * duration - (duration / Math.Pow(2, dots));
            return duration;
        }
    }
}
