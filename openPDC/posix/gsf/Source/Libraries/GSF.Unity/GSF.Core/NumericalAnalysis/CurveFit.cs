﻿//*******************************************************************************************************
//  CurveFit.cs - Gbtc
//
//  Tennessee Valley Authority, 2009
//  No copyright is claimed pursuant to 17 USC § 105.  All Other Rights Reserved.
//
//  This software is made freely available under the TVA Open Source Agreement (see below).
//
//  Code Modification History:
//  -----------------------------------------------------------------------------------------------------
//  01/24/2006 - J. Ritchie Carroll
//       Generated original version of source code.
//  09/17/2008 - J. Ritchie Carroll
//      Converted to C#.
//  08/07/2009 - Josh L. Patterson
//       Edited Comments.
//  09/14/2009 - Stephen C. Wills
//       Added new header and license agreement.
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace GSF.NumericalAnalysis
{
    /// <summary>
    /// Linear regression algorithm.
    /// </summary>
    public static class CurveFit
    {
        /// <summary>
        /// Computes linear regression over given values.
        /// </summary>
        /// <param name="polynomialOrder">An <see cref="Int32"/> for the polynomial order.</param>
        /// <param name="values">A list of values.</param>
        /// <returns>An array of <see cref="Double"/> values.</returns>
        public static double[] Compute(int polynomialOrder, IEnumerable<Point> values)
        {
            return Compute(polynomialOrder, values.Select(point => point.X).ToList(), values.Select(point => point.Y).ToList());
        }

        /// <summary>
        /// Computes linear regression over given values.
        /// </summary>
        /// <param name="polynomialOrder">An <see cref="Int32"/> for the polynomial order.</param>
        /// <param name="xValues">A list of <see cref="Double"/> x-values.</param>
        /// <param name="yValues">A list of <see cref="Double"/> y-values.</param>
        /// <returns>An array of <see cref="Double"/> values.</returns>
        public static double[] Compute(int polynomialOrder, IList<double> xValues, IList<double> yValues)
        {
            if ((object)xValues == null)
                throw new ArgumentNullException("xValues");

            if ((object)yValues == null)
                throw new ArgumentNullException("yValues");

            if (xValues.Count != yValues.Count)
                throw new ArgumentException("Point count for x-values and y-values must be equal");

            if (!(xValues.Count >= polynomialOrder + 1))
                throw new ArgumentException("Point count must be greater than requested polynomial order");

            if (!(polynomialOrder >= 1) && (polynomialOrder <= 7))
                throw new ArgumentOutOfRangeException("polynomialOrder", "Polynomial order must be between 1 and 7");

            // Curve fit function (courtesy of Brian Fox from DatAWare client code)
            double[] coeffs = new double[8];
            double[] sum = new double[22];
            double[] v = new double[12];
            double[,] b = new double[12, 13];
            double p, divB, fMultB, sigma;
            int ls, lb, lv, i1, i, j, k, l;
            int pointCount = xValues.Count;

            ls = polynomialOrder * 2;
            lb = polynomialOrder + 1;
            lv = polynomialOrder;
            sum[0] = pointCount;

            for (i = 0; i < pointCount; i++)
            {
                p = 1.0;
                v[0] = v[0] + yValues[i];

                for (j = 1; j <= lv; j++)
                {
                    p = xValues[i] * p;
                    sum[j] = sum[j] + p;
                    v[j] = v[j] + yValues[i] * p;
                }

                for (j = lb; j <= ls; j++)
                {
                    p = xValues[i] * p;
                    sum[j] = sum[j] + p;
                }
            }

            for (i = 0; i <= lv; i++)
            {
                for (k = 0; k <= lv; k++)
                {
                    b[k, i] = sum[k + i];
                }
            }

            for (k = 0; k <= lv; k++)
            {
                b[k, lb] = v[k];
            }

            for (l = 0; l <= lv; l++)
            {
                divB = b[0, 0];
                for (j = l; j <= lb; j++)
                {
                    if (divB == 0)
                        divB = 1;
                    b[l, j] = b[l, j] / divB;
                }

                i1 = l + 1;

                if (i1 - lb < 0)
                {
                    for (i = i1; i <= lv; i++)
                    {
                        fMultB = b[i, l];
                        for (j = l; j <= lb; j++)
                        {
                            b[i, j] = b[i, j] - b[l, j] * fMultB;
                        }
                    }
                }
                else
                {
                    break;
                }
            }

            coeffs[lv] = b[lv, lb];
            i = lv;

            do
            {
                sigma = 0;
                for (j = i; j <= lv; j++)
                {
                    sigma = sigma + b[i - 1, j] * coeffs[j];
                }
                i--;
                coeffs[i] = b[i, lb] - sigma;
            }
            while (i - 1 > 0);

            #region [ Old Code ]

            //    For i = 1 To 7
            //        Debug.Print "Coeffs(" & i & ") = " & Coeffs(i)
            //    Next i

            //For i = 1 To 60
            //    '        CalcY(i).TTag = xValues(1) + ((i - 1) / (xValues(pointCount) - xValues(1)))

            //    CalcY(i).TTag = ((i - 1) / 59) * xValues(pointCount) - xValues(1)
            //    CalcY(i).Value = Coeffs(1)

            //    For j = 1 To polynomialOrder
            //        CalcY(i).Value = CalcY(i).Value + Coeffs(j + 1) * CalcY(i).TTag ^ j
            //    Next
            //Next

            //    SSERROR = 0
            //    For i = 1 To pointCount
            //        SSERROR = SSERROR + (yValues(i) - CalcY(i).Value) * (yValues(i) - CalcY(i).Value)
            //    Next i
            //    SSERROR = SSERROR / (pointCount - polynomialOrder)
            //    sError = SSERROR

            #endregion

            // Return slopes...
            return coeffs;
        }

        /// <summary>
        /// Uses least squares linear regression to estimate the coefficients a, b, and c
        /// from the given (x,y,z) data points for the equation z = a + bx + cy.
        /// </summary>
        /// <param name="zValues">z-value array</param>
        /// <param name="xValues">x-value array</param>
        /// <param name="yValues">y-value array</param>
        /// <param name="a">the out a coefficient</param>
        /// <param name="b">the out b coefficient</param>
        /// <param name="c">the out c coefficient</param>
        public static void LeastSquares(double[] zValues, double[] xValues, double[] yValues, out double a, out double b, out double c)
        {
            double n = zValues.Length;

            double xSum = 0;
            double ySum = 0;
            double zSum = 0;

            double xySum = 0;
            double xzSum = 0;
            double yzSum = 0;

            double xxSum = 0;
            double yySum = 0;

            double[,] coeff = new double[3, 4];

            for (int i = 0; i < n; i++)
            {
                double x = xValues[i];
                double y = yValues[i];
                double z = zValues[i];

                xSum += x;
                ySum += y;
                zSum += z;

                xySum += x * y;
                xzSum += x * z;
                yzSum += y * z;

                xxSum += x * x;
                yySum += y * y;
            }

            coeff[0, 0] = zSum;
            coeff[0, 1] = n;
            coeff[0, 2] = xSum;
            coeff[0, 3] = ySum;

            coeff[1, 0] = xzSum - (xSum * zSum) / n;
            coeff[1, 1] = 0;
            coeff[1, 2] = xxSum - (xSum * xSum) / n;
            coeff[1, 3] = xySum - (xSum * ySum) / n;

            coeff[2, 0] = yzSum - (ySum * zSum) / n - (coeff[1, 0] * coeff[1, 3]) / coeff[1, 2];
            coeff[2, 1] = 0;
            coeff[2, 2] = 0;
            coeff[2, 3] = yySum - (ySum * ySum) / n - (coeff[1, 3] * coeff[1, 3]) / coeff[1, 2];

            c = coeff[2, 0] / coeff[2, 3];
            b = (coeff[1, 0] - c * coeff[1, 3]) / coeff[1, 2];
            a = (coeff[0, 0] - b * coeff[0, 2] - c * coeff[0, 3]) / coeff[0, 1];
        }
    }
}