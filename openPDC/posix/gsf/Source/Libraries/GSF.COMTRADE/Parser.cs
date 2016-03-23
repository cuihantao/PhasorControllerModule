﻿//******************************************************************************************************
//  Parser.cs - Gbtc
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
//  06/17/2012 - J. Ritchie Carroll
//       Generated original version of source code.
//  12/13/2012 - Starlynn Danyelle Gilliam
//       Modified Header.
//
//******************************************************************************************************

using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using GSF.IO;

namespace GSF.COMTRADE
{
    /// <summary>
    /// COMTRADE data file(s) parser.
    /// </summary>
    public class Parser : IDisposable
    {
        #region [ Members ]

        // Fields
        private Schema m_schema;
        private string m_fileName;
        private bool m_disposed;
        private FileStream[] m_fileStreams;
        private StreamReader[] m_fileReaders;
        private int m_streamIndex;
        private DateTime m_timestamp;
        private double[] m_values;
        private bool m_inferTimeFromSampleRates;

        #endregion

        #region [ Constructors ]

        /// <summary>
        /// Creates a new instance of the COMTRADE <see cref="Parser"/>.
        /// </summary>
        public Parser()
        {
            m_inferTimeFromSampleRates = true;
        }

        /// <summary>
        /// Releases the unmanaged resources before the <see cref="Parser"/> object is reclaimed by <see cref="GC"/>.
        /// </summary>
        ~Parser()
        {
            Dispose(false);
        }

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets or sets associated COMTRADE schema for this <see cref="Parser"/>.
        /// </summary>
        public Schema Schema
        {
            get
            {
                return m_schema;
            }
            set
            {
                m_schema = value;

                if ((object)m_schema != null)
                {
                    if (m_schema.TotalChannels > 0)
                        m_values = new double[m_schema.TotalChannels];
                    else
                        throw new InvalidOperationException("Invalid schema: total channels defined in schema is zero.");
                }
                else
                {
                    m_values = null;
                }
            }
        }

        /// <summary>
        /// Gets or sets COMTRADE data filename. If there are more than one data files in a set, this should be set to first file name in the set, e.g., DATA123.D00.
        /// </summary>
        public string FileName
        {
            get
            {
                return m_fileName;
            }
            set
            {
                m_fileName = value;
            }
        }

        /// <summary>
        /// Gets or sets flag that determines if time should be inferred from sample rates.
        /// </summary>
        public bool InferTimeFromSampleRates
        {
            get
            {
                return m_inferTimeFromSampleRates;
            }
            set
            {
                m_inferTimeFromSampleRates = value;
            }
        }

        /// <summary>
        /// Gets timestamp of current record.
        /// </summary>
        public DateTime Timestamp
        {
            get
            {
                return m_timestamp;
            }
        }

        /// <summary>
        /// Gets values of current record.
        /// </summary>
        public double[] Values
        {
            get
            {
                return m_values;
            }
        }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Releases all the resources used by the <see cref="Parser"/> object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="Parser"/> object and optionally releases the managed resources.
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
                        CloseFiles();
                    }
                }
                finally
                {
                    m_disposed = true;  // Prevent duplicate dispose.
                }
            }
        }

        /// <summary>
        /// Opens all COMTRADE data file streams.
        /// </summary>
        public void OpenFiles()
        {
            if (string.IsNullOrWhiteSpace(m_fileName))
                throw new InvalidOperationException("First COMTRADE data file name was not specified, cannot open files.");

            if (!File.Exists(m_fileName))
                throw new FileNotFoundException(string.Format("Specified COMTRADE data file \"{0}\" was not found, cannot open files.", m_fileName));

            // Get all data files in the collection
            const string FileRegex = @"(?:\.dat|\.d\d\d)$";
            string directory = FilePath.GetDirectoryName(m_fileName);
            string rootFileName = FilePath.GetFileNameWithoutExtension(m_fileName);
            string extension = FilePath.GetExtension(m_fileName).Substring(0, 2) + "*";

            string[] fileNames = FilePath.GetFileList(Path.Combine(directory, rootFileName + extension))
                .Where(fileName => Regex.IsMatch(fileName, FileRegex, RegexOptions.IgnoreCase))
                .OrderBy(fileName => fileName, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            // Create a new file stream for each file
            m_fileStreams = new FileStream[fileNames.Length];

            for (int i = 0; i < fileNames.Length; i++)
            {
                m_fileStreams[i] = new FileStream(fileNames[i], FileMode.Open, FileAccess.Read, FileShare.Read);
            }

            m_streamIndex = 0;
        }

        /// <summary>
        /// Closes all COMTRADE data file streams.
        /// </summary>
        public void CloseFiles()
        {
            if ((object)m_fileStreams != null)
            {
                foreach (FileStream fileStream in m_fileStreams)
                {
                    if ((object)fileStream != null)
                    {
                        fileStream.Close();
                        fileStream.Dispose();
                    }
                }
            }

            m_fileStreams = null;
        }

        /// <summary>
        /// Reads next COMTRADE record.
        /// </summary>
        /// <returns><c>true</c> if read succeeded; otherwise <c>false</c> if end of data set was reached.</returns>
        public bool ReadNext()
        {
            if ((object)m_fileStreams == null)
                throw new InvalidOperationException("COMTRADE data files are not open, cannot read next record.");

            if ((object)m_schema == null)
                throw new InvalidOperationException("No COMTRADE schema has been defined, cannot read records.");

            if (m_streamIndex > m_fileStreams.Length)
                throw new EndOfStreamException("All COMTRADE data has been read, cannot read more records.");

            if (m_schema.FileType == FileType.Ascii)
                return ReadNextAscii();

            return ReadNextBinary();
        }

        // Handle ASCII file read
        private bool ReadNextAscii()
        {
            if ((object)m_fileReaders == null)
            {
                m_fileReaders = new StreamReader[m_fileStreams.Length];

                for (int i = 0; i < m_fileStreams.Length; i++)
                    m_fileReaders[i] = new StreamReader(m_fileStreams[i]);
            }

            // Read next line of record values
            StreamReader reader = m_fileReaders[m_streamIndex];
            string line = reader.ReadLine();
            string[] elems = ((object)line != null) ? line.Split(',') : null;

            // See if we have reached the end of this file
            if ((object)elems == null || elems.Length != m_values.Length + 2)
            {
                if (reader.EndOfStream)
                {
                    m_streamIndex++;

                    // There is more to read if there is another file
                    return m_streamIndex < m_fileStreams.Length && ReadNext();
                }

                throw new InvalidOperationException("COMTRADE schema does not match number of elements found in ASCII data file.");
            }

            // Parse row of data
            uint sample = uint.Parse(elems[0]);

            // Get timestamp of this record
            m_timestamp = DateTime.MinValue;

            // If sample rates are defined, this is the preferred method for timestamp resolution
            if (m_inferTimeFromSampleRates && m_schema.SampleRates.Length > 0)
            {
                // Find rate for given sample
                SampleRate sampleRate = m_schema.SampleRates.LastOrDefault(sr => sample <= sr.EndSample);

                if (sampleRate.Rate > 0.0D)
                    m_timestamp = new DateTime(Ticks.FromSeconds(1.0D / sampleRate.Rate * sample) + m_schema.StartTime.Value);
            }

            // Fall back on specified microsecond time
            if (m_timestamp == DateTime.MinValue)
                m_timestamp = new DateTime(Ticks.FromMicroseconds(uint.Parse(elems[1]) * m_schema.TimeFactor) + m_schema.StartTime.Value);

            // Parse all record values
            for (int i = 0; i < m_values.Length; i++)
            {
                m_values[i] = double.Parse(elems[i + 2]);

                if (i < m_schema.AnalogChannels.Length)
                {
                    m_values[i] *= m_schema.AnalogChannels[i].Multiplier;
                    m_values[i] += m_schema.AnalogChannels[i].Adder;
                }
            }

            return true;
        }

        // Handle binary file read
        private bool ReadNextBinary()
        {
            FileStream currentFile = m_fileStreams[m_streamIndex];
            int recordLength = m_schema.BinaryRecordLength;
            byte[] buffer = null;

            buffer = new byte[recordLength];

            // Read next record from file
            int bytesRead = currentFile.Read(buffer, 0, recordLength);

            // See if we have reached the end of this file
            if (bytesRead == 0)
            {
                m_streamIndex++;

                // There is more to read if there is another file
                return m_streamIndex < m_fileStreams.Length && ReadNext();
            }

            if (bytesRead == recordLength)
            {
                int index = 0;

                // Read sample index
                uint sample = LittleEndian.ToUInt32(buffer, index);
                index += 4;

                // Get timestamp of this record
                m_timestamp = DateTime.MinValue;

                // If sample rates are defined, this is the preferred method for timestamp resolution
                if (m_inferTimeFromSampleRates && m_schema.SampleRates.Length > 0)
                {
                    // Find rate for given sample
                    SampleRate sampleRate = m_schema.SampleRates.LastOrDefault(sr => sample <= sr.EndSample);

                    if (sampleRate.Rate > 0.0D)
                        m_timestamp = new DateTime(Ticks.FromSeconds(1.0D / sampleRate.Rate * sample) + m_schema.StartTime.Value);
                }

                // Read microsecond timestamp
                uint microseconds = LittleEndian.ToUInt32(buffer, index);
                index += 4;

                // Fall back on specified microsecond time
                if (m_timestamp == DateTime.MinValue)
                    m_timestamp = new DateTime(Ticks.FromMicroseconds(microseconds * m_schema.TimeFactor) + m_schema.StartTime.Value);

                // Parse all analog record values
                for (int i = 0; i < m_schema.AnalogChannels.Length; i++)
                {
                    // Read next value
                    m_values[i] = LittleEndian.ToInt16(buffer, index) * m_schema.AnalogChannels[i].Multiplier + m_schema.AnalogChannels[i].Adder;
                    index += 2;
                }

                int valueIndex = m_schema.AnalogChannels.Length;
                int digitalWords = m_schema.DigitalWords;
                ushort digitalWord;

                for (int i = 0; i < digitalWords; i++)
                {
                    // Read next digital word
                    digitalWord = LittleEndian.ToUInt16(buffer, index);
                    index += 2;

                    // Distribute each bit of digital word through next 16 digital values
                    for (int j = 0; j < 16 && valueIndex < m_values.Length; j++, valueIndex++)
                    {
                        m_values[valueIndex] = digitalWord.CheckBits(BitExtensions.BitVal(j)) ? 1.0D : 0.0D;
                    }
                }
            }
            else
            {
                throw new InvalidOperationException("Failed to read enough bytes from COMTRADE file for a record as defined by schema - possible schema/data file mismatch or file corruption.");
            }

            return true;
        }

        #endregion
    }
}
