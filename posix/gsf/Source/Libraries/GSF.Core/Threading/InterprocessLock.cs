﻿//******************************************************************************************************
//  InterprocessLock.cs - Gbtc
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
//  03/21/2011 - J. Ritchie Carroll
//       Generated original version of source code.
//  06/30/2011 - Stephen Wills
//       Applying changes from Jian (Ryan) Zuo: updated to allow unauthorized users to attempt to grant
//       themselves lower than full access to existing mutexes and semaphores.
//  08/12/2011 - J. Ritchie Carroll
//       Modified creation methods such that locking natives are created in a synchronized fashion.
//  09/21/2011 - J. Ritchie Carroll
//       Added Mono implementation exception regions.
//  12/14/2012 - Starlynn Danyelle Gilliam
//       Modified Header.
//  12/18/2013 - J. Ritchie Carroll
//       Improved operational behavior.
//
//******************************************************************************************************

using System;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using GSF.Security.Cryptography;

namespace GSF.Threading
{
    /// <summary>
    /// Defines helper methods related to inter-process locking.
    /// </summary>
    public static class InterprocessLock
    {
        private const int MutexHash = 0;
        private const int SemaphoreHash = 1;

        /// <summary>
        /// Gets a uniquely named inter-process <see cref="Mutex"/> associated with the specified <paramref name="name"/> that identifies a source object
        /// needing concurrency locking.
        /// </summary>
        /// <param name="name">Identifying name of source object needing concurrency locking (e.g., a path and file name).</param>
        /// <returns>A uniquely named inter-process <see cref="Mutex"/> specific to <paramref name="name"/>; <see cref="Mutex"/> is created if it does not exist.</returns>
        /// <remarks>
        /// <para>
        /// This function uses a hash of the <paramref name="name"/> when creating the <see cref="Mutex"/>, not the actual <paramref name="name"/> - this way
        /// restrictions on the <paramref name="name"/> length do not need to be a user concern. All processes needing an inter-process <see cref="Mutex"/> need
        /// to use this same function to ensure access to the same <see cref="Mutex"/>.
        /// </para>
        /// <para>
        /// The <paramref name="name"/> can be a string of any length (must not be empty, null or white space) and is not case-sensitive. All hashes of the
        /// <paramref name="name"/> used to create the global <see cref="Mutex"/> are first converted to lower case.
        /// </para>
        /// <para>
        /// The <see cref="Mutex"/> created is "Global" meaning that it will be accessible to all active application sessions including terminal service
        /// sessions. This is accomplished internally by prefixing the <see cref="Mutex"/> name with "Global\"; it is not necessary for the user to be
        /// concerned with the length or contents of the <paramref name="name"/> in this method as long as the same <paramref name="name"/> is used for
        /// each application. Do not use this helper function if you need to create a specifically named or non-global <see cref="Mutex"/>, such as when
        /// you need to interact with another application using a <see cref="Mutex"/> that does not use this function.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Argument <paramref name="name"/> cannot be empty, null or white space.</exception>
        /// <exception cref="UnauthorizedAccessException">The named mutex exists, but the user does not have the minimum needed security access rights to use it.</exception>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static Mutex GetNamedMutex(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException("name", "Argument cannot be empty, null or white space.");

            Mutex namedMutex;
            bool mutexWasCreated;

            // Create a mutex name that is specific to an object (e.g., a path and file name).
            // Note that we use GetPasswordHash to create a short common name for the name parameter
            // that was passed into the function - this allows the parameter to be very long, e.g.,
            // a file path, and still meet minimum mutex name requirements.

            // Prefix mutex name with "Global\" such that mutex will apply to all active
            // application sessions in case terminal services is running.
            string mutexName = "Global\\" + Cipher.GetPasswordHash(name.ToLower(), MutexHash).Replace('\\', '-');

#if MONO
            // Mono Mutex implementations do not include ability to change access rules
            namedMutex = new Mutex(false, mutexName, out mutexWasCreated);
#else
            bool doesNotExist = false;

            // Attempt to open the named mutex
            try
            {
                namedMutex = Mutex.OpenExisting(mutexName, MutexRights.Synchronize | MutexRights.Modify);
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                namedMutex = null;
                doesNotExist = true;
            }

            // If mutex does not exist we attempt to create it
            if (doesNotExist)
            {
                try
                {
                    MutexSecurity security = new MutexSecurity();
                    security.AddAccessRule(new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), MutexRights.Synchronize | MutexRights.Modify, AccessControlType.Allow));
                    namedMutex = new Mutex(false, mutexName, out mutexWasCreated, security);
                }
                catch (UnauthorizedAccessException)
                {
                    // Named mutex exists now but current user doesn't have full control, attempt to open with minimum needed rights
                    namedMutex = Mutex.OpenExisting(mutexName, MutexRights.Synchronize | MutexRights.Modify);
                }
            }
#endif

            return namedMutex;
        }

        /// <summary>
        /// Gets a uniquely named inter-process <see cref="Semaphore"/> associated with the specified <paramref name="name"/> that identifies a source object
        /// needing concurrency locking.
        /// </summary>
        /// <param name="name">Identifying name of source object needing concurrency locking (e.g., a path and file name).</param>
        /// <param name="maximumCount">The maximum number of requests for the semaphore that can be granted concurrently.</param>
        /// <param name="initialCount">The initial number of requests for the semaphore that can be granted concurrently, or -1 to default to <paramref name="maximumCount"/>.</param>
        /// <returns>A uniquely named inter-process <see cref="Semaphore"/> specific to <paramref name="name"/>; <see cref="Semaphore"/> is created if it does not exist.</returns>
        /// <remarks>
        /// <para>
        /// This function uses a hash of the <paramref name="name"/> when creating the <see cref="Semaphore"/>, not the actual <paramref name="name"/> - this way
        /// restrictions on the <paramref name="name"/> length do not need to be a user concern. All processes needing an inter-process <see cref="Semaphore"/> need
        /// to use this same function to ensure access to the same <see cref="Semaphore"/>.
        /// </para>
        /// <para>
        /// The <paramref name="name"/> can be a string of any length (must not be empty, null or white space) and is not case-sensitive. All hashes of the
        /// <paramref name="name"/> used to create the global <see cref="Semaphore"/> are first converted to lower case.
        /// </para>
        /// <para>
        /// The <see cref="Semaphore"/> created is "Global" meaning that it will be accessible to all active application sessions including terminal service
        /// sessions. This is accomplished internally by prefixing the <see cref="Semaphore"/> name with "Global\"; it is not necessary for the user to be
        /// concerned with the length or contents of the <paramref name="name"/> in this method as long as the same <paramref name="name"/> is used for
        /// each application. Do not use this helper function if you need to create a specifically named or non-global <see cref="Semaphore"/>, such as when
        /// you need to interact with another application using a <see cref="Semaphore"/> that does not use this function.
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Argument <paramref name="name"/> cannot be empty, null or white space.</exception>
        /// <exception cref="UnauthorizedAccessException">The named semaphore exists, but the user does not have the minimum needed security access rights to use it.</exception>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static Semaphore GetNamedSemaphore(string name, int maximumCount = 10, int initialCount = -1)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException("name", "Argument cannot be empty, null or white space.");

            Semaphore namedSemaphore;
            bool semaphoreWasCreated;

            if (initialCount < 0)
                initialCount = maximumCount;

            // Create a semaphore name that is specific to an object (e.g., a path and file name).
            // Note that we use GetPasswordHash to create a short common name for the name parameter
            // that was passed into the function - this allows the parameter to be very long, e.g.,
            // a file path, and still meet minimum semaphore name requirements.

            // Prefix semaphore name with "Global\" such that semaphore will apply to all active
            // application sessions in case terminal services is running. Note that this is necessary
            // even though .NET documentation does not state it - IL disassembly shows direct calls
            // to WinAPI OpenSemaphore and CreateSemaphore which clearly document this:
            // http://msdn.microsoft.com/en-us/library/windows/desktop/ms684326(v=vs.85).aspx
            string semaphoreName = "Global\\" + Cipher.GetPasswordHash(name.ToLower(), SemaphoreHash).Replace('\\', '-');

#if MONO
            // Mono Semaphore implementations do not include ability to change access rules
            namedSemaphore = new Semaphore(initialCount, maximumCount, semaphoreName, out semaphoreWasCreated);
#else
            bool doesNotExist = false;

            // Attempt to open the named semaphore with minimum needed rights
            try
            {
                namedSemaphore = Semaphore.OpenExisting(semaphoreName, SemaphoreRights.Synchronize | SemaphoreRights.Modify);
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                namedSemaphore = null;
                doesNotExist = true;
            }

            // If semaphore does not exist we attempt to create it
            if (doesNotExist)
            {
                try
                {
                    SemaphoreSecurity security = new SemaphoreSecurity();
                    security.AddAccessRule(new SemaphoreAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), SemaphoreRights.Synchronize | SemaphoreRights.Modify, AccessControlType.Allow));
                    namedSemaphore = new Semaphore(initialCount, maximumCount, semaphoreName, out semaphoreWasCreated, security);
                }
                catch (UnauthorizedAccessException)
                {
                    // Named semaphore exists now but current user doesn't have full control, attempt to open with minimum needed rights
                    namedSemaphore = Semaphore.OpenExisting(semaphoreName, SemaphoreRights.Synchronize | SemaphoreRights.Modify);
                }
            }
#endif

            return namedSemaphore;
        }
    }
}
