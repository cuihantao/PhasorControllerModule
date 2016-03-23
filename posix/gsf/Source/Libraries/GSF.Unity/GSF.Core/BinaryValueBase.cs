﻿//******************************************************************************************************
//  BinaryValueBase.cs - Gbtc
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
//  01/25/2008 - J. Ritchie Carroll
//       Initial version of source generated.
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

using System;

namespace GSF
{
    /// <summary>
    /// Represents the base class for a binary data sample stored as a byte array, but implicitly castable to most common native types.
    /// </summary>
    /// <typeparam name="TEndianOrder">Type of <see cref="EndianOrder"/> class used to transpose byte order of derived implementation of <see cref="BinaryValueBase{TEndianOrder}"/>.</typeparam>
    public abstract class BinaryValueBase<TEndianOrder> where TEndianOrder : EndianOrder
    {
        #region [ Members ]

        // Fields
        private TypeCode m_typeCode;    // TypeCode that this binary data sample represents
        private byte[] m_buffer;        // Binary representation of this data sample

        #endregion

        #region [ Constructors ]

        /// <summary>Creates a new binary value from the given byte array.</summary>
        /// <param name="typeCode">The type code of the native value that the binary value represents.</param>
        /// <param name="buffer">The buffer which contains the binary representation of the value.</param>
        /// <param name="startIndex">The offset in the buffer where the data starts.</param>
        /// <param name="length">The number of data bytes that make up the binary value.</param>
        /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="startIndex"/> is outside the range of the <paramref name="buffer"/> -or-
        /// <paramref name="length"/> is less than 0 -or-
        /// <paramref name="startIndex"/> and <paramref name="length"/> do not specify a valid region in the <paramref name="buffer"/>
        /// </exception>
        protected BinaryValueBase(TypeCode typeCode, byte[] buffer, int startIndex, int length)
        {
            if ((object)buffer == null)
                throw new ArgumentNullException("buffer");

            if (startIndex < 0)
                throw new ArgumentOutOfRangeException("startIndex", "cannot be negative");

            if (length < 0)
                throw new ArgumentOutOfRangeException("length", "cannot be negative");

            if (startIndex >= buffer.Length)
                throw new ArgumentOutOfRangeException("startIndex", "not a valid index in buffer");

            if (startIndex + length > buffer.Length)
                throw new ArgumentOutOfRangeException("length", "exceeds buffer size");

            m_typeCode = typeCode;
            m_buffer = new byte[length];

            // Extract specified region of source buffer as desired representation of binary value
            System.Buffer.BlockCopy(buffer, startIndex, m_buffer, 0, length);
        }

        #endregion

        #region [ Properties ]

        /// <summary>
        /// Gets or sets <see cref="TypeCode"/> that this binary data sample represents.
        /// </summary>
        public TypeCode TypeCode
        {
            get
            {
                return m_typeCode;
            }
            set
            {
                m_typeCode = value;
            }
        }

        /// <summary>
        /// Gets or sets the binary representation of this data sample.
        /// </summary>
        public byte[] Buffer
        {
            get
            {
                return m_buffer;
            }
            set
            {
                m_buffer = value;
            }
        }

        #endregion

        #region [ Methods ]

        /// <summary>
        /// Returns a byte from the <see cref="BinaryValueBase{TEndianOrder}.Buffer"/>.
        /// </summary>
        /// <returns>A byte from the <see cref="BinaryValueBase{TEndianOrder}.Buffer"/>.</returns>
        /// <exception cref="InvalidOperationException">Binary value buffer is too small to represent requested type.</exception>
        public Byte ToByte()
        {
            ValidateBufferLength(TypeCode.Byte, sizeof(Byte));
            return m_buffer[0];
        }

        /// <summary>
        /// Returns a 16-bit signed integer, accounting for <see cref="EndianOrder"/>, converted from the <see cref="BinaryValueBase{TEndianOrder}.Buffer"/>.
        /// </summary>
        /// <returns>A 16-bit signed integer, accounting for <see cref="EndianOrder"/>,  converted from the <see cref="BinaryValueBase{TEndianOrder}.Buffer"/>.</returns>
        /// <exception cref="InvalidOperationException">Binary value buffer is too small to represent requested type.</exception>
        public Int16 ToInt16()
        {
            ValidateBufferLength(TypeCode.Int16, sizeof(Int16));
            return s_endianOrder.ToInt16(m_buffer, 0);
        }

        /// <summary>
        /// Returns a 16-bit unsigned integer, accounting for <see cref="EndianOrder"/>, converted from the <see cref="BinaryValueBase{TEndianOrder}.Buffer"/>.
        /// </summary>
        /// <returns>A 16-bit unsigned integer, accounting for <see cref="EndianOrder"/>,  converted from the <see cref="BinaryValueBase{TEndianOrder}.Buffer"/>.</returns>
        /// <exception cref="InvalidOperationException">Binary value buffer is too small to represent requested type.</exception>
        [CLSCompliant(false)]
        public UInt16 ToUInt16()
        {
            ValidateBufferLength(TypeCode.UInt16, sizeof(UInt16));
            return s_endianOrder.ToUInt16(m_buffer, 0);
        }

        /// <summary>
        /// Returns a 24-bit signed integer, accounting for <see cref="EndianOrder"/>, converted from the <see cref="BinaryValueBase{TEndianOrder}.Buffer"/>.
        /// </summary>
        /// <returns>A 24-bit signed integer, accounting for <see cref="EndianOrder"/>,  converted from the <see cref="BinaryValueBase{TEndianOrder}.Buffer"/>.</returns>
        /// <exception cref="InvalidOperationException">Binary value buffer is too small to represent requested type.</exception>
        public Int24 ToInt24()
        {
            // There is no system type code for Int24
            if (m_buffer.Length < 3)
                throw new InvalidOperationException("Binary value buffer is too small to represent a Int24 - buffer length needs to be at least 3");

            return s_endianOrder.ToInt24(m_buffer, 0);
        }

        /// <summary>
        /// Returns a 24-bit unsigned integer, accounting for <see cref="EndianOrder"/>, converted from the <see cref="BinaryValueBase{TEndianOrder}.Buffer"/>.
        /// </summary>
        /// <returns>A 24-bit unsigned integer, accounting for <see cref="EndianOrder"/>,  converted from the <see cref="BinaryValueBase{TEndianOrder}.Buffer"/>.</returns>
        /// <exception cref="InvalidOperationException">Binary value buffer is too small to represent requested type.</exception>
        [CLSCompliant(false)]
        public UInt24 ToUInt24()
        {
            // There is no system type code for UInt24
            if (m_buffer.Length < 3)
                throw new InvalidOperationException("Binary value buffer is too small to represent a UInt24 - buffer length needs to be at least 3");

            return s_endianOrder.ToUInt24(m_buffer, 0);
        }

        /// <summary>
        /// Returns a 32-bit signed integer, accounting for <see cref="EndianOrder"/>, converted from the <see cref="BinaryValueBase{TEndianOrder}.Buffer"/>.
        /// </summary>
        /// <returns>A 32-bit signed integer, accounting for <see cref="EndianOrder"/>,  converted from the <see cref="BinaryValueBase{TEndianOrder}.Buffer"/>.</returns>
        /// <exception cref="InvalidOperationException">Binary value buffer is too small to represent requested type.</exception>
        public Int32 ToInt32()
        {
            ValidateBufferLength(TypeCode.Int32, sizeof(Int32));
            return s_endianOrder.ToInt32(m_buffer, 0);
        }

        /// <summary>
        /// Returns a 32-bit unsigned integer, accounting for <see cref="EndianOrder"/>, converted from the <see cref="BinaryValueBase{TEndianOrder}.Buffer"/>.
        /// </summary>
        /// <returns>A 32-bit unsigned integer, accounting for <see cref="EndianOrder"/>,  converted from the <see cref="BinaryValueBase{TEndianOrder}.Buffer"/>.</returns>
        /// <exception cref="InvalidOperationException">Binary value buffer is too small to represent requested type.</exception>
        [CLSCompliant(false)]
        public UInt32 ToUInt32()
        {
            ValidateBufferLength(TypeCode.UInt32, sizeof(UInt32));
            return s_endianOrder.ToUInt32(m_buffer, 0);
        }

        /// <summary>
        /// Returns a 64-bit signed integer, accounting for <see cref="EndianOrder"/>, converted from the <see cref="BinaryValueBase{TEndianOrder}.Buffer"/>.
        /// </summary>
        /// <returns>A 64-bit signed integer, accounting for <see cref="EndianOrder"/>,  converted from the <see cref="BinaryValueBase{TEndianOrder}.Buffer"/>.</returns>
        /// <exception cref="InvalidOperationException">Binary value buffer is too small to represent requested type.</exception>
        public Int64 ToInt64()
        {
            ValidateBufferLength(TypeCode.Int64, sizeof(Int64));
            return s_endianOrder.ToInt64(m_buffer, 0);
        }

        /// <summary>
        /// Returns a 64-bit unsigned integer, accounting for <see cref="EndianOrder"/>, converted from the <see cref="BinaryValueBase{TEndianOrder}.Buffer"/>.
        /// </summary>
        /// <returns>A 64-bit unsigned integer, accounting for <see cref="EndianOrder"/>,  converted from the <see cref="BinaryValueBase{TEndianOrder}.Buffer"/>.</returns>
        /// <exception cref="InvalidOperationException">Binary value buffer is too small to represent requested type.</exception>
        [CLSCompliant(false)]
        public UInt64 ToUInt64()
        {
            ValidateBufferLength(TypeCode.UInt64, sizeof(UInt64));
            return s_endianOrder.ToUInt64(m_buffer, 0);
        }

        /// <summary>
        /// Returns a single-precision floating point number, accounting for <see cref="EndianOrder"/>, converted from the <see cref="BinaryValueBase{TEndianOrder}.Buffer"/>.
        /// </summary>
        /// <returns>A single-precision floating point number, accounting for <see cref="EndianOrder"/>,  converted from the <see cref="BinaryValueBase{TEndianOrder}.Buffer"/>.</returns>
        /// <exception cref="InvalidOperationException">Binary value buffer is too small to represent requested type.</exception>
        public Single ToSingle()
        {
            ValidateBufferLength(TypeCode.Single, sizeof(Single));
            return s_endianOrder.ToSingle(m_buffer, 0);
        }

        /// <summary>
        /// Returns a double-precision floating point number, accounting for <see cref="EndianOrder"/>, converted from the <see cref="BinaryValueBase{TEndianOrder}.Buffer"/>.
        /// </summary>
        /// <returns>A double-precision floating point number, accounting for <see cref="EndianOrder"/>,  converted from the <see cref="BinaryValueBase{TEndianOrder}.Buffer"/>.</returns>
        /// <exception cref="InvalidOperationException">Binary value buffer is too small to represent requested type.</exception>
        public Double ToDouble()
        {
            ValidateBufferLength(TypeCode.Double, sizeof(Double));
            return s_endianOrder.ToDouble(m_buffer, 0);
        }

        private void ValidateBufferLength(TypeCode typeCode, int size)
        {
            if (m_buffer.Length < size)
                throw new InvalidOperationException(string.Format("Binary value buffer is too small to represent a {0} - buffer length needs to be at least {1}", typeCode, size));
        }

        #endregion

        #region [ Static ]

        // Because each "typed" instance will be it's own class - each will have its own static variable instance

        /// <summary>
        /// <see cref="EndianOrder"/> instance used to transpose byte order of derived implementation of <see cref="BinaryValueBase{TEndianOrder}"/>.
        /// </summary>
        protected static TEndianOrder s_endianOrder;

        #endregion
    }
}
