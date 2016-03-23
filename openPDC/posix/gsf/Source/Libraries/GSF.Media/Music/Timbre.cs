﻿//******************************************************************************************************
//  Timbre.cs - Gbtc
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
    /// <summary>
    /// Provides a function signature for methods that produce an amplitude representing the
    /// acoustic pressure of a represented musical timbre for the given time.
    /// </summary>
    /// <param name="frequency">Fundamental frequency of the desired note in Hz.</param>
    /// <param name="sampleIndex">Sample index (represents time anywhere from zero to full length of song).</param>
    /// <param name="samplePeriod">If useful, total period for note in whole samples per second (i.e., seconds of time * <paramref name="sampleRate"/>) over which to compute timbre.</param>
    /// <param name="sampleRate">Number of samples per second.</param>
    /// <returns>The amplitude of the represented musical timbre (a value between zero and one) at the given time.</returns>
    public delegate double TimbreFunction(double frequency, long sampleIndex, long samplePeriod, int sampleRate);

    /// <summary>
    /// Defines a few timbre functions.
    /// </summary>
    public static class Timbre
	{
        /// <summary>
        /// Computes the angular frequency for the given time.
        /// </summary>
        /// <param name="frequency">Frequency in Hz.</param>
        /// <param name="sampleIndex">Sample index (represents time anywhere from zero to full length of song).</param>
        /// <param name="sampleRate">Number of samples per second.</param>
        /// <returns>The computed angular frequency in radians per second at given time.</returns>
        public static double AngularFrequency(double frequency, long sampleIndex, double sampleRate)
        {
            // 2 PI f t
            //      f = Frequency (Hz)
            //      t = period    (Seconds)

            return (2.0D * Math.PI * frequency) * (sampleIndex / sampleRate);
        }

        /// <summary>
        /// Generates a pure tone for the given frequency and time.
        /// </summary>
        /// <param name="frequency">Fundamental frequency of the desired note in Hz.</param>
        /// <param name="sampleIndex">Sample index (represents time anywhere from zero to full length of song).</param>
        /// <param name="samplePeriod">If useful, total period for note in whole samples per second (i.e., seconds of time * <paramref name="sampleRate"/>) over which to compute timbre.</param>
        /// <param name="sampleRate">Number of samples per second.</param>
        /// <returns>The amplitude for a pure tone at the given time.</returns>
        /// <remarks>
        /// This method computes an amplitude representing the acoustic pressure of a
        /// pure tone of the given frequency for the given time.
        /// </remarks>
        public static double PureTone(double frequency, long sampleIndex, long samplePeriod, int sampleRate)
        {
            return Math.Sin(AngularFrequency(frequency, sampleIndex, sampleRate));
        }

        /// <summary>
        /// Generates a basic note for the given frequency and time.
        /// </summary>
        /// <param name="frequency">Fundamental frequency of the desired note in Hz.</param>
        /// <param name="sampleIndex">Sample index (represents time anywhere from zero to full length of song).</param>
        /// <param name="samplePeriod">If useful, total period for note in whole samples per second (i.e., seconds of time * <paramref name="sampleRate"/>) over which to compute timbre.</param>
        /// <param name="sampleRate">Number of samples per second.</param>
        /// <returns>The amplitude for a basic note at the given time.</returns>
        /// <remarks>
        /// <para>
        /// This method computes an amplitude representing the acoustic pressure of a
        /// basic note of the given frequency for the given time.
        /// </para>
        /// <para>
        /// </para>
        /// This timbre algorithm combines the simulated piano and the odd harmonic series
        /// algoriths to produce a pleasant sounding note.
        /// </remarks>
        public static double BasicNote(double frequency, long sampleIndex, long samplePeriod, int sampleRate)
        {
            return (Piano(frequency, sampleIndex, samplePeriod, sampleRate) +
                    OddHarmonicSeries(frequency, sampleIndex, samplePeriod, sampleRate)) / 2.0D;
        }

        /// <summary>
        /// Generates a simulated piano note for the given frequency and time.
        /// </summary>
        /// <param name="frequency">Fundamental frequency of the desired note in Hz.</param>
        /// <param name="sampleIndex">Sample index (represents time anywhere from zero to full length of song).</param>
        /// <param name="samplePeriod">If useful, total period for note in whole samples per second (i.e., seconds of time * <paramref name="sampleRate"/>) over which to compute timbre.</param>
        /// <param name="sampleRate">Number of samples per second.</param>
        /// <returns>The amplitude for a simulated piano note at the given time.</returns>
        /// <remarks>
        /// This method computes an amplitude representing the acoustic pressure of a
        /// simulated piano note of the given frequency for the given time.
        /// </remarks>
        public static double Piano(double frequency, long sampleIndex, long samplePeriod, int sampleRate)
        {
            return (
                (70.2137233659572D * Math.Cos(AngularFrequency(frequency * 0.72337962962963D, sampleIndex, sampleRate) + 2.15595560508634D)) +
                (32.6386857925095D * Math.Cos(AngularFrequency(frequency * 1.44675925925926D, sampleIndex, sampleRate) - 1.34187877930484D)) +
                (6.46251265801616D * Math.Cos(AngularFrequency(frequency * 2.17013888888889D, sampleIndex, sampleRate) - 2.94209422121373D)) +
                (12.7763287253431D * Math.Cos(AngularFrequency(frequency * 2.89351851851852D, sampleIndex, sampleRate) + 2.43247608525533D)) +
                (26.8756025924165D * Math.Cos(AngularFrequency(frequency * 3.61689814814815D, sampleIndex, sampleRate) + 3.06242942291676D)) +
                (16.7595425274989D * Math.Cos(AngularFrequency(frequency * 4.34027777777778D, sampleIndex, sampleRate) - 1.0881672416834D)) +
                (8.61450310871165D * Math.Cos(AngularFrequency(frequency * 5.06365740740741D, sampleIndex, sampleRate) - 3.01123327342246D)) +
                (6.76189590128027D * Math.Cos(AngularFrequency(frequency * 5.78703703703704D, sampleIndex, sampleRate) + 1.64626744449415D)) +
                (4.11732226561935D * Math.Cos(AngularFrequency(frequency * 6.51041666666667D, sampleIndex, sampleRate) + 0.603920626612006D))) / 185.220116937353D;
        }

        /// <summary>
        /// Generates a simulated flute note for the given frequency and time.
        /// </summary>
        /// <param name="frequency">Fundamental frequency of the desired note in Hz.</param>
        /// <param name="sampleIndex">Sample index (represents time anywhere from zero to full length of song).</param>
        /// <param name="samplePeriod">If useful, total period for note in whole samples per second (i.e., seconds of time * <paramref name="sampleRate"/>) over which to compute timbre.</param>
        /// <param name="sampleRate">Number of samples per second.</param>
        /// <returns>The amplitude for a simulated flute note at the given time.</returns>
        /// <remarks>
        /// This method computes an amplitude representing the acoustic pressure of a
        /// simulated flute note of the given frequency for the given time.
        /// </remarks>
        public static double Flute(double frequency, long sampleIndex, long samplePeriod, int sampleRate)
        {
            return (
                (9.05727062296345D * Math.Cos(AngularFrequency(frequency * 4.12453297595841D, sampleIndex, sampleRate) + 1.60462505309668D)) +
                (3.3050840324057D * Math.Cos(AngularFrequency(frequency * 8.49168553873791D, sampleIndex, sampleRate) - 1.92072594933431D)) +
                (3.35470429751368D * Math.Cos(AngularFrequency(frequency * 12.8588381015174D, sampleIndex, sampleRate) - 0.464413055268356D))) / 15.7170589528828D;
        }

        /// <summary>
        /// Generates a simulated acoustic guitar note for the given frequency and time.
        /// </summary>
        /// <param name="frequency">Fundamental frequency of the desired note in Hz.</param>
        /// <param name="sampleIndex">Sample index (represents time anywhere from zero to full length of song).</param>
        /// <param name="samplePeriod">If useful, total period for note in whole samples per second (i.e., seconds of time * <paramref name="sampleRate"/>) over which to compute timbre.</param>
        /// <param name="sampleRate">Number of samples per second.</param>
        /// <returns>The amplitude for a acoustic simulated guitar note at the given time.</returns>
        /// <remarks>
        /// This method computes an amplitude representing the acoustic pressure of a
        /// simulated acoustic guitar note of the given frequency for the given time.
        /// </remarks>
        public static double AcousticGuitar(double frequency, long sampleIndex, long samplePeriod, int sampleRate)
        {
            return (
                (12000D * Math.Cos(AngularFrequency(frequency * 0.589687726942629D, sampleIndex, sampleRate) + 3D)) +
                (3489.55321772347D * Math.Cos(AngularFrequency(frequency * 0.164533405954975D, sampleIndex, sampleRate) + 1D)) +
                (856.743340090216D * Math.Cos(AngularFrequency(frequency * 4.36013525780683D, sampleIndex, sampleRate) + 2.39181899274207D)) +
                (739.597913265917D * Math.Cos(AngularFrequency(frequency * 8.72027051561365D, sampleIndex, sampleRate) - 1.96195311808947D))) / 17085.8944710796D;
        }

        /// <summary>
        /// Generates a simulated bass guitar note for the given frequency and time.
        /// </summary>
        /// <param name="frequency">Fundamental frequency of the desired note in Hz.</param>
        /// <param name="sampleIndex">Sample index (represents time anywhere from zero to full length of song).</param>
        /// <param name="samplePeriod">If useful, total period for note in whole samples per second (i.e., seconds of time * <paramref name="sampleRate"/>) over which to compute timbre.</param>
        /// <param name="sampleRate">Number of samples per second.</param>
        /// <returns>The amplitude for a simulated bass guitar note at the given time.</returns>
        /// <remarks>
        /// This method computes an amplitude representing the acoustic pressure of a
        /// simulated bass guitar note of the given frequency for the given time.
        /// </remarks>
        public static double BassGuitar(double frequency, long sampleIndex, long samplePeriod, int sampleRate)
        {
            return (
                (1595.02256927522D * Math.Cos(AngularFrequency(frequency * 0.185137407789627D, sampleIndex, sampleRate) - 0.574574744703629D)) +
                (3489.55321772347D * Math.Cos(AngularFrequency(frequency * 0.370274815579253D, sampleIndex, sampleRate) - 0.936478578271564D)) +
                (1610.28352837899D * Math.Cos(AngularFrequency(frequency * 0.55541222336888D, sampleIndex, sampleRate) + 2.55112147201879D)) +
                (9015.99102300765D * Math.Cos(AngularFrequency(frequency * 0.740549631158506D, sampleIndex, sampleRate) - 1.28523213535845D)) +
                (7849.4441069613D * Math.Cos(AngularFrequency(frequency * 0.925687038948133D, sampleIndex, sampleRate) + 1.90667513893081D)) +
                (3137.17792493101D * Math.Cos(AngularFrequency(frequency * 1.11082444673776D, sampleIndex, sampleRate) + 1.77658315162925D)) +
                (2118.91625761195D * Math.Cos(AngularFrequency(frequency * 1.29596185452739D, sampleIndex, sampleRate) + 1.7219352726054D)) +
                (1285.80991499134D * Math.Cos(AngularFrequency(frequency * 1.48109926231701D, sampleIndex, sampleRate) + 1.41294924277346D)) +
                (4522.76758774399D * Math.Cos(AngularFrequency(frequency * 1.66623667010664D, sampleIndex, sampleRate) + 2.31226657354236D))) / 34624.9661306249D;
        }

        /// <summary>
        /// Generates a simulated electric guitar note for the given frequency and time.
        /// </summary>
        /// <param name="frequency">Fundamental frequency of the desired note in Hz.</param>
        /// <param name="sampleIndex">Sample index (represents time anywhere from zero to full length of song).</param>
        /// <param name="samplePeriod">If useful, total period for note in whole samples per second (i.e., seconds of time * <paramref name="sampleRate"/>) over which to compute timbre.</param>
        /// <param name="sampleRate">Number of samples per second.</param>
        /// <returns>The amplitude for a simulated electric guitar note at the given time.</returns>
        /// <remarks>
        /// This method computes an amplitude representing the acoustic pressure of a
        /// simulated electric guitar note of the given frequency for the given time.
        /// </remarks>
        public static double ElectricGuitar(double frequency, long sampleIndex, long samplePeriod, int sampleRate)
        {
            return (
                (16107.474214089D * Math.Cos(AngularFrequency(frequency * 0.485239173642166D, sampleIndex, sampleRate) + 0.878364865481147D)) +
                (14087.0088730627D * Math.Cos(AngularFrequency(frequency * 0.970478347284333D, sampleIndex, sampleRate) - 2.98645760242543D)) +
                (8735.01788701509D * Math.Cos(AngularFrequency(frequency * 1.4557175209265D, sampleIndex, sampleRate) - 0.395796372471435D)) +
                (3973.79454412557D * Math.Cos(AngularFrequency(frequency * 1.94095669456867D, sampleIndex, sampleRate) + 2.23661099705174D)) +
                (1102.84347421078D * Math.Cos(AngularFrequency(frequency * 2.42619586821083D, sampleIndex, sampleRate) - 1.38641414143595D)) +
                (162.235485574335D * Math.Cos(AngularFrequency(frequency * 2.911435041853D, sampleIndex, sampleRate) - 0.458018924851457D))) / 44168.3744780774D;
        }

        /// <summary>
        /// Generates a simulated bell note for the given frequency and time.
        /// </summary>
        /// <param name="frequency">Fundamental frequency of the desired note in Hz.</param>
        /// <param name="sampleIndex">Sample index (represents time anywhere from zero to full length of song).</param>
        /// <param name="samplePeriod">If useful, total period for note in whole samples per second (i.e., seconds of time * <paramref name="sampleRate"/>) over which to compute timbre.</param>
        /// <param name="sampleRate">Number of samples per second.</param>
        /// <returns>The amplitude for a simulated bell note at the given time.</returns>
        /// <remarks>
        /// This method computes an amplitude representing the acoustic pressure of a
        /// simulated bell note of the given frequency for the given time.
        /// </remarks>
        public static double Bell(double frequency, long sampleIndex, long samplePeriod, int sampleRate)
        {
            return (
                (11.8586691553945D * Math.Cos(AngularFrequency(frequency * 0.5822870083706D, sampleIndex, sampleRate) - 0.585043165898236D)) +
                (5.91936035806859D * Math.Cos(AngularFrequency(frequency * 1.1645740167412D, sampleIndex, sampleRate) + 0.695123064084268D)) +
                (8.81245139134363D * Math.Cos(AngularFrequency(frequency * 1.55276535565493D, sampleIndex, sampleRate) - 1.33711302251612D)) +
                (12.2611666622597D * Math.Cos(AngularFrequency(frequency * 2.13505236402553D, sampleIndex, sampleRate) + 1.29258157515533D)) +
                (9.52103654697751D * Math.Cos(AngularFrequency(frequency * 2.911435041853D, sampleIndex, sampleRate) + 1.51305419736917D)) +
                (19.8983034658111D * Math.Cos(AngularFrequency(frequency * 4.46420039750793D, sampleIndex, sampleRate) - 0.481803561576583D)) +
                (22.6449781174184D * Math.Cos(AngularFrequency(frequency * 6.21106142261973D, sampleIndex, sampleRate) - 2.86053196663233D)) +
                (33.301340324476D * Math.Cos(AngularFrequency(frequency * 9.31659213392959D, sampleIndex, sampleRate) + 2.93581848593649D)) +
                (6.681420554653D * Math.Cos(AngularFrequency(frequency * 9.8988791423002D, sampleIndex, sampleRate) - 1.1476776907146D)) +
                (8.57727309655362D * Math.Cos(AngularFrequency(frequency * 11.0634531590414D, sampleIndex, sampleRate) - 1.92631144339D)) +
                (10.1407468995286D * Math.Cos(AngularFrequency(frequency * 13.0044098536101D, sampleIndex, sampleRate) - 0.579132583187011D)) +
                (1.14697118371238D * Math.Cos(AngularFrequency(frequency * 15.5276535565493D, sampleIndex, sampleRate) - 0.533998003179541D)) +
                (4.87273548610198D * Math.Cos(AngularFrequency(frequency * 17.2745145816611D, sampleIndex, sampleRate) - 1.03348081908558D)) +
                (2.32220544429305D * Math.Cos(AngularFrequency(frequency * 22.3210019875397D, sampleIndex, sampleRate) + 2.89661207668278D))) / 157.958658686592D;
        }

        /// <summary>
        /// Generates a simulated big bell note for the given frequency and time.
        /// </summary>
        /// <param name="frequency">Fundamental frequency of the desired note in Hz.</param>
        /// <param name="sampleIndex">Sample index (represents time anywhere from zero to full length of song).</param>
        /// <param name="samplePeriod">If useful, total period for note in whole samples per second (i.e., seconds of time * <paramref name="sampleRate"/>) over which to compute timbre.</param>
        /// <param name="sampleRate">Number of samples per second.</param>
        /// <returns>The amplitude for a simulated big bell note at the given time.</returns>
        /// <remarks>
        /// This method computes an amplitude representing the acoustic pressure of a
        /// simulated big bell note of the given frequency for the given time.
        /// </remarks>
        public static double BigBell(double frequency, long sampleIndex, long samplePeriod, int sampleRate)
        {
            return (
                (2.74232785316433D * Math.Cos(AngularFrequency(frequency * 0.460977214960058D, sampleIndex, sampleRate) - 2.15914124733981D)) +
                (1.55105992674992D * Math.Cos(AngularFrequency(frequency * 1.38293164488017D, sampleIndex, sampleRate) + 2.48145847054026D)) +
                (1.14626706346255D * Math.Cos(AngularFrequency(frequency * 2.53537468228032D, sampleIndex, sampleRate) - 1.94085120106431D)) +
                (0.365721729927815D * Math.Cos(AngularFrequency(frequency * 3.45732911220044D, sampleIndex, sampleRate) + 1.54815118644636D)) +
                (0.264483787961699D * Math.Cos(AngularFrequency(frequency * 3.91830632716049D, sampleIndex, sampleRate) + 1.73402928921419D)) +
                (0.211877801368119D * Math.Cos(AngularFrequency(frequency * 5.5317265795207D, sampleIndex, sampleRate) + 1.80195288951592D))) / 6.28173816263444D;
        }

        /// <summary>
        /// Generates a simulated chime bell note for the given frequency and time.
        /// </summary>
        /// <param name="frequency">Fundamental frequency of the desired note in Hz.</param>
        /// <param name="sampleIndex">Sample index (represents time anywhere from zero to full length of song).</param>
        /// <param name="samplePeriod">If useful, total period for note in whole samples per second (i.e., seconds of time * <paramref name="sampleRate"/>) over which to compute timbre.</param>
        /// <param name="sampleRate">Number of samples per second.</param>
        /// <returns>The amplitude for a simulated chime bell note at the given time.</returns>
        /// <remarks>
        /// This method computes an amplitude representing the acoustic pressure of a
        /// simulated chime bell note of the given frequency for the given time.
        /// </remarks>
        public static double ChimeBell(double frequency, long sampleIndex, long samplePeriod, int sampleRate)
        {
            return (
                (10.611209264611D * Math.Cos(AngularFrequency(frequency * 0.837037574532737D, sampleIndex, sampleRate) + 2.63563741494359D)) +
                (12.7805320933663D * Math.Cos(AngularFrequency(frequency * 2.23210019875397D, sampleIndex, sampleRate) - 2.78168435797271D)) +
                (15.6108088926578D * Math.Cos(AngularFrequency(frequency * 3.0691377732867D, sampleIndex, sampleRate) + 2.78073285773659D)) +
                (12.6222113284421D * Math.Cos(AngularFrequency(frequency * 4.18518787266369D, sampleIndex, sampleRate) - 0.497781974864526D)) +
                (16.6844576119022D * Math.Cos(AngularFrequency(frequency * 6.41728807141765D, sampleIndex, sampleRate) + 2.26495277534172D)) +
                (20.7634525834236D * Math.Cos(AngularFrequency(frequency * 8.92840079501586D, sampleIndex, sampleRate) - 2.53270740774491D)) +
                (33.4322806693617D * Math.Cos(AngularFrequency(frequency * 13.3926011925238D, sampleIndex, sampleRate) + 3.38304770932294E-02D)) +
                (10.7429223904271D * Math.Cos(AngularFrequency(frequency * 14.2296387670565D, sampleIndex, sampleRate) - 0.486547875634811D)) +
                (9.19639240625629D * Math.Cos(AngularFrequency(frequency * 15.903713916122D, sampleIndex, sampleRate) - 1.24037360428308D))) / 142.444267240448D;
        }

        /// <summary>
        /// Generates a simulated xylophone note for the given frequency and time.
        /// </summary>
        /// <param name="frequency">Fundamental frequency of the desired note in Hz.</param>
        /// <param name="sampleIndex">Sample index (represents time anywhere from zero to full length of song).</param>
        /// <param name="samplePeriod">If useful, total period for note in whole samples per second (i.e., seconds of time * <paramref name="sampleRate"/>) over which to compute timbre.</param>
        /// <param name="sampleRate">Number of samples per second.</param>
        /// <returns>The amplitude for a simulated xylophone note at the given time.</returns>
        /// <remarks>
        /// This method computes an amplitude representing the acoustic pressure of a
        /// simulated xylophone note of the given frequency for the given time.
        /// </remarks>
        public static double Xylophone(double frequency, long sampleIndex, long samplePeriod, int sampleRate)
        {
            return (
                (4.11908004642792D * Math.Cos(AngularFrequency(frequency * 0.826310056186217D, sampleIndex, sampleRate) + 2.7760677807271D)) +
                (37.8253440139155D * Math.Cos(AngularFrequency(frequency * 6.33504376409433D, sampleIndex, sampleRate) - 1.88399408289362D)) +
                (56.6005882242386D * Math.Cos(AngularFrequency(frequency * 25.6156117417727D, sampleIndex, sampleRate) - 1.0863847947086D)) +
                (56.6005882242387D * Math.Cos(AngularFrequency(frequency * 44.8961797194511D, sampleIndex, sampleRate) + 1.08638479470859D)) +
                (37.8253440139155D * Math.Cos(AngularFrequency(frequency * 64.1767476971295D, sampleIndex, sampleRate) + 1.88399408289362D)) +
                (4.11908004642791D * Math.Cos(AngularFrequency(frequency * 69.6854814050377D, sampleIndex, sampleRate) - 2.77606778072711D))) / 197.090024569164D;
        }

        /// <summary>
        /// Generates a simulated clarinet note for the given frequency and time.
        /// </summary>
        /// <param name="frequency">Fundamental frequency of the desired note in Hz.</param>
        /// <param name="sampleIndex">Sample index (represents time anywhere from zero to full length of song).</param>
        /// <param name="samplePeriod">If useful, total period for note in whole samples per second (i.e., seconds of time * <paramref name="sampleRate"/>) over which to compute timbre.</param>
        /// <param name="sampleRate">Number of samples per second.</param>
        /// <returns>The amplitude for a simulated clarinet note at the given time.</returns>
        /// <remarks>
        /// This method computes an amplitude representing the acoustic pressure of a
        /// simulated clarinet note of the given frequency for the given time.
        /// </remarks>
        public static double Clarinet(double frequency, long sampleIndex, long samplePeriod, int sampleRate)
        {
            double wt, r1;

            wt = AngularFrequency(frequency, sampleIndex, sampleRate);

            // Simulated Clarinet equation - see http://www.phy.mtu.edu/~suits/clarinet.html
            // s(t) = sin(wt) + 0.75 *      sin(3 * wt) + 0.5 *      sin(5 * wt) + 0.14 *      sin(7 * wt) + 0.5 *      sin(9 * wt) + 0.12 *      sin(11 * wt) + 0.17 *      sin(13 * wt)
            r1 = (Math.Sin(wt) + 0.75 * Math.Sin(3 * wt) + 0.5 * Math.Sin(5 * wt) + 0.14 * Math.Sin(7 * wt) + 0.5 * Math.Sin(9 * wt) + 0.12 * Math.Sin(11 * wt) + 0.17 * Math.Sin(13 * wt)) / 3.18D;

            return r1;
        }

        /// <summary>
        /// Generates a simulated organ note for the given frequency and time.
        /// </summary>
        /// <param name="frequency">Fundamental frequency of the desired note in Hz.</param>
        /// <param name="sampleIndex">Sample index (represents time anywhere from zero to full length of song).</param>
        /// <param name="samplePeriod">If useful, total period for note in whole samples per second (i.e., seconds of time * <paramref name="sampleRate"/>) over which to compute timbre.</param>
        /// <param name="sampleRate">Number of samples per second.</param>
        /// <returns>The amplitude for a simulated clarinet note at the given time.</returns>
        /// <remarks>
        /// This method computes an amplitude representing the acoustic pressure of a second-order
        /// harmonic series approaching a square wave (i.e., Sin(f) + Sin(3f)/3) of the given
        /// frequency for the given time to simulate an organ sound.
        /// </remarks>
        public static double Organ(double frequency, long sampleIndex, long samplePeriod, int sampleRate)
        {
            return ComputeHarmonicSeries(frequency, sampleIndex, sampleRate, 1, 3);
        }

        /// <summary>
        /// Generates an odd harmonic series for the given frequency and time.
        /// </summary>
        /// <param name="frequency">Fundamental frequency of the desired note in Hz.</param>
        /// <param name="sampleIndex">Sample index (represents time anywhere from zero to full length of song).</param>
        /// <param name="samplePeriod">If useful, total period for note in whole samples per second (i.e., seconds of time * <paramref name="sampleRate"/>) over which to compute timbre.</param>
        /// <param name="sampleRate">Number of samples per second.</param>
        /// <returns>The amplitude for a simulated clarinet note at the given time.</returns>
        /// <remarks>
        /// This method computes an amplitude representing the acoustic pressure of an
        /// odd harmonic series of the given frequency for the given time.
        /// Algorithm: Sin(f) + Sin(3f)/3 + Sin(5f)/5, etc.
        /// </remarks>
        public static double OddHarmonicSeries(double frequency, long sampleIndex, long samplePeriod, int sampleRate)
        {
            return ComputeHarmonicSeries(frequency, sampleIndex, sampleRate, 1, 25);
        }

        /// <summary>
        /// Generates an even harmonic series for the given frequency and time.
        /// </summary>
        /// <param name="frequency">Fundamental frequency of the desired note in Hz.</param>
        /// <param name="sampleIndex">Sample index (represents time anywhere from zero to full length of song).</param>
        /// <param name="samplePeriod">If useful, total period for note in whole samples per second (i.e., seconds of time * <paramref name="sampleRate"/>) over which to compute timbre.</param>
        /// <param name="sampleRate">Number of samples per second.</param>
        /// <returns>The amplitude for a simulated clarinet note at the given time.</returns>
        /// <remarks>
        /// This method computes an amplitude representing the acoustic pressure of an
        /// even harmonic series of the given frequency for the given time.
        /// Algorithm: Sin(2f) + Sin(4f)/3 + Sin(6f)/5, etc.
        /// </remarks>
        public static double EvenHarmonicSeries(double frequency, long sampleIndex, long samplePeriod, int sampleRate)
        {
            return ComputeHarmonicSeries(frequency, sampleIndex, sampleRate, 2, 26);
        }

        // Computes a basic harmonic series
        private static double ComputeHarmonicSeries(double frequency, long sampleIndex, double sampleRate, int offset, int order)
        {
            double wt, r1 = 0.0D, total = 0.0D;
            int divisor;

            wt = AngularFrequency(frequency, sampleIndex, sampleRate);

            // Generate harmonic series
            for (int x = offset; x <= order; x += 2)
            {
                divisor = (x - offset + 1);
                r1 += Math.Sin(x * wt) / divisor;
                total += divisor;
            }

            // Evenly distribute the series between 0 and 1
            return r1 / total;
        }
    }
}
