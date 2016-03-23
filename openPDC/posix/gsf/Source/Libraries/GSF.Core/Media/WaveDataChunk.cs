﻿//******************************************************************************************************
//  WaveDataChunk.cs - Gbtc
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
//  07/29/2009 - J. Ritchie Carroll
//       Generated original version of source code.
//  09/14/2009 - Stephen C. Wills
//       Added new header and license agreement.
//  10/8/2012 - Danyelle Gilliam
//        Modified Header
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

using GSF.Parsing;
using System;
using System.Collections.Generic;
using System.IO;

namespace GSF.Media
{
    /// <summary>
    /// Represents the data chunk in a WAVE media format file.
    /// </summary>
    public class WaveDataChunk : RiffChunk, ISupportBinaryImage
    {
        #region [ Members ]

        // Constants

        /// <summary>
        /// Type ID of a WAVE data chunk.
        /// </summary>
        public const string RiffTypeID = "data";

        // Fields
        private WaveFormatChunk m_waveFormat;
        private List<LittleBinaryValue[]> m_sampleBlocks;
        private int m_chunkSize;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Constructs a new WAVE data chunk for the specified format.
        /// </summary>
        /// <param name="waveFormat"><see cref="WaveFormatChunk"/> that describes this <see cref="WaveDataChunk"/>.</param>
        public WaveDataChunk(WaveFormatChunk waveFormat)
            : base(RiffTypeID)
        {
            m_waveFormat = waveFormat;
            m_sampleBlocks = new List<LittleBinaryValue[]>();
            m_chunkSize = -1;
        }

        /// <summary>Reads a new WAVE format section from the specified stream.</summary>
        /// <param name="preRead">Pre-parsed <see cref="RiffChunk"/> header.</param>
        /// <param name="source">Source stream to read data from.</param>
        /// <param name="waveFormat">Format of the data section to be parsed.</param>
        /// <exception cref="InvalidOperationException">WAVE format or extra parameters section too small, wave file corrupted.</exception>
        public WaveDataChunk(RiffChunk preRead, Stream source, WaveFormatChunk waveFormat)
            : base(preRead, RiffTypeID)
        {
            m_waveFormat = waveFormat;
            m_sampleBlocks = new List<LittleBinaryValue[]>();
            m_chunkSize = -1;

            int blockSize = waveFormat.BlockAlignment;
            int sampleSize = waveFormat.BitsPerSample / 8;
            int channels = waveFormat.Channels;
            TypeCode sampleTypeCode = m_waveFormat.GetSampleTypeCode();
            LittleBinaryValue[] sampleBlock;
            byte[] buffer = BufferPool.TakeBuffer(blockSize);

            try
            {
                int bytesRead = source.Read(buffer, 0, blockSize);

                while (bytesRead == blockSize)
                {
                    // Create a new sample block, one little-endian formatted binary sample value for each channel
                    sampleBlock = new LittleBinaryValue[channels];

                    for (int x = 0; x < channels; x++)
                    {
                        sampleBlock[x] = new LittleBinaryValue(sampleTypeCode, buffer, x * sampleSize, sampleSize);
                    }

                    m_sampleBlocks.Add(sampleBlock);

                    bytesRead = source.Read(buffer, 0, blockSize);
                }
            }
            finally
            {
                if (buffer != null)
                    BufferPool.ReturnBuffer(buffer);
            }
        }

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets or sets associated <see cref="WaveFormatChunk"/> that defines the format of the data in this <see cref="WaveDataChunk"/>.
        /// </summary>
        public WaveFormatChunk WaveFormat
        {
            get
            {
                return m_waveFormat;
            }
            set
            {
                m_waveFormat = value;
            }
        }

        /// <summary>
        /// Gets list of little-endian formatted sample data blocks of this <see cref="WaveDataChunk"/>.
        /// </summary>
        public List<LittleBinaryValue[]> SampleBlocks
        {
            get
            {
                return m_sampleBlocks;
            }
        }

        /// <summary>
        /// Gets the chunk size of this <see cref="WaveDataChunk"/>.
        /// </summary>
        public override int ChunkSize
        {
            get
            {
                if (m_chunkSize == -1)
                    return m_sampleBlocks.Count * m_waveFormat.BlockAlignment;

                return m_chunkSize;
            }
            set
            {
                m_chunkSize = value;
            }
        }

        /// <summary>
        /// Gets length of the binary representation of this <see cref="WaveDataChunk"/>.
        /// </summary>
        public override int BinaryLength
        {
            get
            {
                return base.BinaryLength + ChunkSize;
            }
        }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Generates a binary representation of this <see cref="WaveDataChunk"/> and copies it into the given buffer.
        /// </summary>
        /// <param name="buffer">Buffer used to hold generated binary image of the source object.</param>
        /// <param name="startIndex">0-based starting index in the <paramref name="buffer"/> to start writing.</param>
        /// <returns>The number of bytes written to the <paramref name="buffer"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="startIndex"/> or <see cref="ISupportBinaryImage.BinaryLength"/> is less than 0 -or- 
        /// <paramref name="startIndex"/> and <see cref="ISupportBinaryImage.BinaryLength"/> will exceed <paramref name="buffer"/> length.
        /// </exception>
        public override int GenerateBinaryImage(byte[] buffer, int startIndex)
        {
            int length = BinaryLength;

            buffer.ValidateParameters(startIndex, length);

            int blockSize = m_waveFormat.BlockAlignment;
            int sampleSize = m_waveFormat.BitsPerSample / 8;
            LittleBinaryValue[] sampleChannels;

            startIndex += base.GenerateBinaryImage(buffer, startIndex);

            for (int block = 0; block < m_sampleBlocks.Count; block++)
            {
                sampleChannels = m_sampleBlocks[block];

                for (int sample = 0; sample < sampleChannels.Length; sample++)
                {
                    Buffer.BlockCopy(sampleChannels[sample].Buffer, 0, buffer, startIndex + block * blockSize + sample * sampleSize, sampleSize);
                }
            }

            return length;
        }

        // Data chunk is usually too large to parsed from a single binary image
        int ISupportBinaryImage.ParseBinaryImage(byte[] buffer, int startIndex, int length)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates a deeply cloned copy of the <see cref="WaveDataChunk"/>.
        /// </summary>
        /// <returns>A deeply cloned copy of the <see cref="WaveDataChunk"/>.</returns>
        public new WaveDataChunk Clone()
        {
            WaveDataChunk waveDataChunk = new WaveDataChunk(m_waveFormat);

            // Deep clone sample blocks
            foreach (LittleBinaryValue[] samples in m_sampleBlocks)
            {
                waveDataChunk.SampleBlocks.Add(WaveFile.CloneSampleBlock(samples));
            }

            return waveDataChunk;
        }

        #endregion
    }
}
