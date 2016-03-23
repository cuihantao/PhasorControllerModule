//******************************************************************************************************
//  MemberInfoExtensions.cs - Gbtc
//
//  Copyright � 2012, Grid Protection Alliance.  All Rights Reserved.
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
//  01/30/2009 - J. Ritchie Carroll
//       Generated original version of source code.
//  09/14/2009 - Stephen C. Wills
//       Added new header and license agreement.
//  12/14/2012 - Starlynn Danyelle Gilliam
//       Modified Header.
//
//******************************************************************************************************

using System;
using System.Linq;
using System.Reflection;

namespace GSF.Reflection
{
    /// <summary>
    /// Defines extensions methods related to <see cref="MemberInfo"/> objects and derived types (e.g., <see cref="FieldInfo"/>,
    /// <see cref="PropertyInfo"/>, <see cref="MethodInfo"/>, etc.)
    /// </summary>
    public static class MemberInfoExtensions
    {
        /// <summary>
        /// Attempts to get the specified <paramref name="attribute"/> from a <see cref="MemberInfo"/> object, returning <c>true</c> if it does.
        /// </summary>
        /// <param name="member">The <see cref="MemberInfo"/> object over which to search attributes.</param>
        /// <param name="attribute">The <see cref="Attribute"/> that was found, if any.</param>
        /// <returns><c>true</c> if <paramref name="attribute"/> was found; otherwise <c>false</c>.</returns>
        /// <typeparam name="TMemberInfo"><see cref="MemberInfo"/> or derived type to get <see cref="Attribute"/> from.</typeparam>
        /// <typeparam name="TAttribute"><see cref="Type"/> of <see cref="Attribute"/> to attempt to retrieve.</typeparam>
        /// <remarks>
        /// If more than of the same type of attribute exists on the member, only the first one is returned.
        /// </remarks>
        public static bool TryGetAttribute<TMemberInfo, TAttribute>(this TMemberInfo member, out TAttribute attribute)
            where TMemberInfo : MemberInfo
            where TAttribute : Attribute
        {
            object[] customAttributes = member.GetCustomAttributes(typeof(TAttribute), true);

            if (customAttributes.Length > 0)
            {
                attribute = customAttributes[0] as TAttribute;
                return true;
            }

            attribute = null;
            return false;
        }

        /// <summary>
        /// Attempts to get the specified <paramref name="attributes"/> from a <see cref="MemberInfo"/> object, returning <c>true</c> if it does.
        /// </summary>
        /// <param name="member">The <see cref="MemberInfo"/> object over which to search attributes.</param>
        /// <param name="attributes">The array of <see cref="Attribute"/> objects that were found, if any.</param>
        /// <returns><c>true</c> if <paramref name="attributes"/> was found; otherwise <c>false</c>.</returns>
        /// <typeparam name="TMemberInfo"><see cref="MemberInfo"/> or derived type to get <see cref="Attribute"/> from.</typeparam>
        /// <typeparam name="TAttribute"><see cref="Type"/> of <see cref="Attribute"/> to attempt to retrieve.</typeparam>
        public static bool TryGetAttributes<TMemberInfo, TAttribute>(this TMemberInfo member, out TAttribute[] attributes)
            where TMemberInfo : MemberInfo
            where TAttribute : Attribute
        {
            object[] customAttributes = member.GetCustomAttributes(typeof(TAttribute), true);

            if (customAttributes.Length > 0)
            {
                attributes = customAttributes.Cast<TAttribute>().ToArray();
                return true;
            }

            attributes = null;
            return false;
        }

        /// <summary>
        /// Attempts to get the specified <paramref name="attribute"/> from a <see cref="MemberInfo"/> object, returning <c>true</c> if it does.
        /// </summary>
        /// <param name="member">The <see cref="MemberInfo"/> object over which to search attributes.</param>
        /// <param name="attributeType">The actual type of the <see cref="Attribute"/> to look for.</param>
        /// <param name="attribute">The <see cref="Attribute"/> that was found, if any.</param>
        /// <returns><c>true</c> if <paramref name="attribute"/> was found; otherwise <c>false</c>.</returns>
        /// <typeparam name="TMemberInfo"><see cref="MemberInfo"/> or derived type to get <see cref="Attribute"/> from.</typeparam>
        /// <remarks>
        /// If more than of the same type of attribute exists on the member, only the first one is returned.
        /// </remarks>
        public static bool TryGetAttribute<TMemberInfo>(this TMemberInfo member, Type attributeType, out Attribute attribute)
            where TMemberInfo : MemberInfo
        {
            object[] customAttributes = member.GetCustomAttributes(attributeType, true);

            if (customAttributes.Length > 0)
            {
                attribute = customAttributes[0] as Attribute;
                return true;
            }

            attribute = null;
            return false;
        }

        /// <summary>
        /// Attempts to get the specified <paramref name="attributes"/> from a <see cref="MemberInfo"/> object, returning <c>true</c> if it does.
        /// </summary>
        /// <param name="member">The <see cref="MemberInfo"/> object over which to search attributes.</param>
        /// <param name="attributeType">The actual type of the <see cref="Attribute"/> objects to look for.</param>
        /// <param name="attributes">The array of <see cref="Attribute"/> objects that were found, if any.</param>
        /// <returns><c>true</c> if <paramref name="attributes"/> was found; otherwise <c>false</c>.</returns>
        /// <typeparam name="TMemberInfo"><see cref="MemberInfo"/> or derived type to get <see cref="Attribute"/> from.</typeparam>
        public static bool TryGetAttributes<TMemberInfo>(this TMemberInfo member, Type attributeType, out Attribute[] attributes)
            where TMemberInfo : MemberInfo
        {
            object[] customAttributes = member.GetCustomAttributes(attributeType, true);

            if (customAttributes.Length > 0)
            {
                attributes = customAttributes.Cast<Attribute>().ToArray();
                return true;
            }

            attributes = null;
            return false;
        }
    }
}
