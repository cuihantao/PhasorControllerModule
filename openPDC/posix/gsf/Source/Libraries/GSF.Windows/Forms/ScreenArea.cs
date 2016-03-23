﻿//******************************************************************************************************
//  ScreenArea.cs - Gbtc
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
//  10/01/2008 - J. Ritchie Carroll
//       Generated original version of source code.
//  08/10/2009 - Josh L. Patterson
//       Edited Comments.
//  09/14/2009 - Stephen C. Wills
//       Added new header and license agreement.
//  12/14/2012 - Starlynn Danyelle Gilliam
//       Modified Header.
//
//******************************************************************************************************

using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using GSF.Drawing;

namespace GSF.Windows.Forms
{
    /// <summary>Returns screen area statistics and capture functionality for all connected screens.</summary>
    public static class ScreenArea
    {
        /// <summary>
        /// Gets the least "x" coordinate of all screens on the system
        /// </summary>
        /// <returns>The smallest visible "x" screen coordinate</returns>
        public static int LeftMostBound
        {
            get
            {
                int leftBound = int.MaxValue;

                // Return the left-most "x" screen coordinate
                foreach (Screen display in Screen.AllScreens)
                {
                    if (leftBound > display.Bounds.X)
                        leftBound = display.Bounds.X;
                }

                return leftBound;
            }
        }

        /// <summary>
        /// Gets the greatest "x" coordinate of all screens on the system
        /// </summary>
        /// <returns>The largest visible "x" screen coordinate</returns>
        public static int RightMostBound
        {
            get
            {
                int rightBound = int.MinValue;

                // Return the right-most "x" screen coordinate
                foreach (Screen display in Screen.AllScreens)
                {
                    if (rightBound < display.Bounds.X + display.Bounds.Width)
                        rightBound = display.Bounds.X + display.Bounds.Width;
                }

                return rightBound;
            }
        }

        /// <summary>
        /// Gets the least "y" coordinate of all screens on the system
        /// </summary>
        /// <returns>The smallest visible "y" screen coordinate</returns>
        public static int TopMostBound
        {
            get
            {
                int topBound = int.MaxValue;

                // Return the top-most "y" screen coordinate
                foreach (Screen display in Screen.AllScreens)
                {
                    if (topBound > display.Bounds.Y)
                        topBound = display.Bounds.Y;
                }

                return topBound;
            }
        }

        /// <summary>
        /// Gets the greatest "y" coordinate of all screens on the system
        /// </summary>
        /// <returns>The largest visible "y" screen coordinate</returns>
        public static int BottomMostBound
        {
            get
            {
                int bottomBound = int.MinValue;

                // Return the bottom-most "y" screen coordinate
                foreach (Screen display in Screen.AllScreens)
                {
                    if (bottomBound < display.Bounds.Y + display.Bounds.Height)
                        bottomBound = display.Bounds.Y + display.Bounds.Height;
                }

                return bottomBound;
            }
        }

        /// <summary>
        /// Gets the width of the screen with the highest resolution.
        /// </summary>
        /// <returns>The width of the screen with the highest resolution.</returns>
        public static int MaximumWidth
        {
            get
            {
                int maxWidth = int.MinValue;

                // In this case we just get the largest screen height
                foreach (Screen display in Screen.AllScreens)
                {
                    if (maxWidth < display.Bounds.Width)
                        maxWidth = display.Bounds.Width;
                }

                return maxWidth;
            }
        }

        /// <summary>
        /// Gets the width of the screen with the lowest resolution.
        /// </summary>
        /// <returns>The width of the screen with the lowest resolution.</returns>
        public static int MinimumWidth
        {
            get
            {
                int minWidth = int.MaxValue;

                // In this case we just get the smallest screen height
                foreach (Screen display in Screen.AllScreens)
                {
                    if (minWidth > display.Bounds.Width)
                        minWidth = display.Bounds.Width;
                }

                return minWidth;
            }
        }

        /// <summary>
        /// Gets the height of the screen with the highest resolution.
        /// </summary>
        /// <returns>The height of the screen with the highest resolution.</returns>
        public static int MaximumHeight
        {
            get
            {
                int maxHeight = int.MinValue;

                // In this case we just get the largest screen height
                foreach (Screen display in Screen.AllScreens)
                {
                    if (maxHeight < display.Bounds.Height)
                        maxHeight = display.Bounds.Height;
                }

                return maxHeight;
            }
        }

        /// <summary>
        /// Gets the height of the screen with the lowest resolution.
        /// </summary>
        /// <returns>The height of the screen with the lowest resolution.</returns>
        public static int MinimumHeight
        {
            get
            {
                int minHeight = int.MaxValue;

                // In this case we just get the smallest screen height
                foreach (Screen display in Screen.AllScreens)
                {
                    if (minHeight > display.Bounds.Height)
                        minHeight = display.Bounds.Height;
                }

                return minHeight;
            }
        }

        /// <summary>
        /// Gets the total width of all the screens relative to their arrangement.
        /// </summary>
        /// <returns>The total width of all the screens relative to their arrangement.</returns>
        public static int TotalWidth
        {
            get
            {
                return RightMostBound - LeftMostBound;
            }
        }

        /// <summary>
        /// Gets the total height of all the screens relative to their arrangement.
        /// </summary>
        /// <returns>The total height of all the screens relative to their arrangement.</returns>
        public static int TotalHeight
        {
            get
            {
                return BottomMostBound - TopMostBound;
            }
        }

        /// <summary>
        /// Performs screen capture over all monitors.
        /// </summary>
        /// <returns>Captured screen image over all monitors.</returns>
        /// <remarks>
        /// Size captured will be for total width and height of all screens relative to their arrangement.
        /// An image square will be created large enough to cover all screens for the capture.
        /// </remarks>
        public static Bitmap Capture()
        {
            return Capture(new Rectangle(LeftMostBound, TopMostBound, TotalWidth, TotalHeight));
        }

        /// <summary>
        /// Performs screen capture over all monitors.
        /// </summary>
        /// <param name="imageFormat">Desired <see cref="ImageFormat"/> for captured <see cref="Bitmap"/>.</param>
        /// <returns>Captured screen image over all monitors.</returns>
        /// <remarks>
        /// Size captured will be for total width and height of all screens relative to their arrangement.
        /// An image square will be created large enough to cover all screens for the capture.
        /// </remarks>
        public static Bitmap Capture(ImageFormat imageFormat)
        {
            return Capture(new Rectangle(LeftMostBound, TopMostBound, TotalWidth, TotalHeight), imageFormat);
        }

        /// <summary>
        /// Performs screen capture for given <see cref="Screen"/>.
        /// </summary>
        /// <param name="captureScreen">A <see cref="Screen"/> object to capture.</param>
        /// <returns>Captured screen image for given <see cref="Screen"/>.</returns>
        public static Bitmap Capture(Screen captureScreen)
        {
            return Capture(captureScreen.Bounds);
        }

        /// <summary>
        /// Performs screen capture for given <see cref="Screen"/> and <see cref="ImageFormat"/>.
        /// </summary>
        /// <param name="captureScreen">Desired <see cref="Screen"/> to capture</param>
        /// <param name="imageFormat">Desired <see cref="ImageFormat"/> for captured <see cref="Bitmap"/>.</param>
        /// <returns>Captured screen image for given <see cref="Screen"/>.</returns>
        public static Bitmap Capture(Screen captureScreen, ImageFormat imageFormat)
        {
            return Capture(captureScreen.Bounds, imageFormat);
        }

        /// <summary>
        /// Performs a screen capture the given <see cref="Rectangle"/>.
        /// </summary>
        /// <param name="captureArea">Screen area <see cref="Rectangle"/> to capture.</param>
        /// <returns>Captured screen image for given area.</returns>
        public static Bitmap Capture(Rectangle captureArea)
        {
            return Capture(captureArea, ImageFormat.Bmp);
        }

        /// <summary>
        /// Performs a screen capture the given <see cref="Rectangle"/> and <see cref="ImageFormat"/>.
        /// </summary>
        /// <param name="captureArea">Screen area <see cref="Rectangle"/> to capture.</param>
        /// <param name="imageFormat">Desired <see cref="ImageFormat"/> for captured <see cref="Bitmap"/>.</param>
        /// <returns>Captured screen image for given area.</returns>
        public static Bitmap Capture(Rectangle captureArea, ImageFormat imageFormat)
        {
            // Create a blank image of the specified size.
            using (Bitmap screenCaptureImage = new Bitmap(captureArea.Width, captureArea.Height))
            {
                using (Graphics screenCaptureGraphics = Graphics.FromImage(screenCaptureImage))
                {
                    // Copy the area of the screen to the blank image.
                    screenCaptureGraphics.CopyFromScreen(captureArea.X, captureArea.Y, 0, 0, captureArea.Size);
                }

                // We'll return the captured screenshot in the specified image format.
                return screenCaptureImage.ConvertTo(imageFormat, true);
            }
        }
    }
}