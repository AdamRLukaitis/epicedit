﻿#region GPL statement
/*Epic Edit is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, version 3 of the License.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.*/
#endregion

using EpicEdit.Rom;
using EpicEdit.UI.Gfx;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace EpicEdit.UI.ThemeEdition
{
    internal partial class ColorPicker : UserControl
    {
        [Browsable(true), Category("Behavior")]
        public event EventHandler<EventArgs> ColorChanged;

        #region Private members

        /// <summary>
        /// The image of the top bar, goes from red to green to blue and back to red.
        /// </summary>
        private Bitmap basicColorsBitmap;

        /// <summary>
        /// The cache for basicColorsBitmap.
        /// </summary>
        private Bitmap basicColorsCache;

        /// <summary>
        /// Cached size for basicColorsBitmap / basicColorsCache.
        /// </summary>
        private Size basicColorsSize;

        /// <summary>
        /// The selected color from basicColorsBitmap.
        /// </summary>
        private RomColor selectedBasicColor = RomColor.From5BitRgb(31, 1, 0);

        /// <summary>
        /// The image where a selected color goes to black, white and gray.
        /// </summary>
        private Bitmap shadesBitmap;

        /// <summary>
        /// The cache for the shadesBitmap.
        /// </summary>
        private Bitmap shadesCache;

        /// <summary>
        /// Cached size for shadesBitmap / shadesCache.
        /// </summary>
        private Size shadesSize;

        /// <summary>
        /// The highlighted color from shadesBitmap.
        /// </summary>
        private Point selectedShadeLocation;

        // The selected color.
        private Color selectedColor;

        /// <summary>
        /// Used to prevent loops when certain clicks are performed in different UI controls.
        /// </summary>
        private bool fireEvents;

        /// <summary>
        /// True when the 8-bit colors are being changed by the user, to avoid updating the 8-bit color values twice
        /// (first with the value input by the user, then by the automatic 5-bit to 8-bit color conversion).
        /// </summary>
        private bool updating8BitColors;

        private bool basicColorsMouseDown;

        private bool shadesPictureMouseDown;

        #endregion Private members

        /// <summary>
        /// Gets or sets the selected color.
        /// </summary>
        /// <returns>The color.</returns>
        public RomColor SelectedColor
        {
            get => this.selectedColor;
            set => this.SetColor(value);
        }

        public ColorPicker()
        {
            this.InitializeComponent();

            this.basicColorsSize = this.basicColorsPictureBox.ClientSize;
            this.shadesSize = this.shadesPictureBox.ClientSize;

            // Initializing Bitmaps in order to avoid having to check if they're null before disposal
            this.basicColorsBitmap = this.shadesBitmap = this.shadesCache = new Bitmap(1, 1, PixelFormat.Format32bppPArgb);

            this.InitBasicColorsBitmapCache();

            int x = 0;
            RomColor basicColor = this.basicColorsCache.GetPixel(x, 0);
            this.SetColor(x, basicColor, basicColor);
        }

        /// <summary>
        /// Sets the new color.
        /// </summary>
        /// <param name="x">X position in the basic colors.</param>
        private void SetColor(int x)
        {
            // Make sure we are not out of bounds
            if (x < 0)
            {
                x = 0;
            }
            else if (x >= this.basicColorsSize.Width)
            {
                x = this.basicColorsSize.Width - 1;
            }

            this.InvalidateBasicColorsSelection();
            this.selectedBasicColor = this.DrawBasicColorsBitmap(x);
            this.InvalidateBasicColorsSelection();
            this.basicColorsPictureBox.Refresh();

            this.InitShadesCache();
            RomColor shadeColor = this.DrawShadesBitmap(this.selectedShadeLocation.X, this.selectedShadeLocation.Y);
            this.shadesPictureBox.Refresh();

            this.SetColorSub(shadeColor);
        }

        /// <summary>
        /// Sets the new color.
        /// Automatically detects where the color exists in the shades and basic colors.
        /// </summary>
        /// <param name="color">The color.</param>
        private void SetColor(RomColor color)
        {
            // This method is called whenever the user changes the RGB values manually.
            // FIXME: Out of 32768 colors (32 * 32 * 32 possible combinations), we're able to find the
            // wanted color within the shades for 32198 colors. For the 570 remaining colors, the color
            // is not within the currently-displayed shades, and so we do not draw the selection circle
            // around it in the shades (e.g: R=6, G=7, B=19). Not sure how to fix this,
            // but it may be a rounding issue somewhere.
            color = color.To5Bit();
            RomColor basicColor = ColorPicker.FindBasicColor(color);
            int x = this.FindColorIndex(basicColor);
            this.SetColor(x, basicColor, color);
        }

        private void SetColor(int x, RomColor basicColor, RomColor color)
        {
            if (this.selectedBasicColor != basicColor)
            {
                this.InvalidateBasicColorsSelection();
                this.selectedBasicColor = basicColor;
                this.DrawBasicColorsBitmap(x);
                this.InvalidateBasicColorsSelection();

                this.InitShadesCache();
            }

            this.DrawShadesBitmap(color);
            this.shadesPictureBox.Refresh();

            this.SetColorSub(color);
        }

        private void SetColorSub(RomColor color)
        {
            this.selectedColor = color;

            this.fireEvents = false;

            this.red5NumericUpDown.Value = color.Red5Bit;
            this.green5NumericUpDown.Value = color.Green5Bit;
            this.blue5NumericUpDown.Value = color.Blue5Bit;

            if (!this.updating8BitColors)
            {
                this.red8NumericUpDown.Value = color.Red;
                this.green8NumericUpDown.Value = color.Green;
                this.blue8NumericUpDown.Value = color.Blue;
            }

            this.fireEvents = true;
        }

        #region Bitmap drawing

        /// <summary>
        /// Returns the bounds of the passed selection coordinates.
        /// </summary>
        /// <param name="x">X value.</param>
        /// <param name="y">Y value.</param>
        /// <returns>The bounds of the passed selection coordinates.</returns>
        private static Rectangle GetSelectionBounds(int x, int y)
        {
            return new Rectangle(x - 3, y - 3, 6, 6);
        }

        private void InvalidateBasicColorsSelection()
        {
            Rectangle rec = ColorPicker.GetSelectionBounds(this.FindColorIndex(this.selectedBasicColor), this.basicColorsSize.Height / 2);
            rec.Inflate(1, 1);
            this.basicColorsPictureBox.Invalidate(rec);
        }

        private void InvalidateShadesSelection()
        {
            Rectangle rec = ColorPicker.GetSelectionBounds(this.selectedShadeLocation.X, this.selectedShadeLocation.Y);
            rec.Inflate(1, 1);
            this.shadesPictureBox.Invalidate(rec);
        }

        /// <summary>
        /// Draws the basic colors with the circle around a certain x position.
        /// </summary>
        /// <param name="x">The position to select.</param>
        /// <returns>The color at x.</returns>
        private RomColor DrawBasicColorsBitmap(int x)
        {
            RomColor color = this.basicColorsCache.GetPixel(x, 0);
            this.DrawBasicColorsBitmap(color, x);
            return color;
        }

        /// <summary>
        /// Draws the basic colors with the circle around a certain color.
        /// </summary>
        /// <param name="color">The color to select.</param>
        /// <param name="x">The color location.</param>
        private void DrawBasicColorsBitmap(RomColor color, int x)
        {
            this.basicColorsBitmap.Dispose();
            this.basicColorsBitmap = this.basicColorsCache.Clone() as Bitmap;

            using (Graphics g = Graphics.FromImage(this.basicColorsBitmap))
            using (Pen pen = new Pen(color.Opposite()))
            {
                int y = this.basicColorsSize.Height / 2;
                g.DrawEllipse(pen, ColorPicker.GetSelectionBounds(x, y));
            }
        }

        /// <summary>
        /// Initializes the basicColorsCache member.
        /// </summary>
        private void InitBasicColorsBitmapCache()
        {
            int width = this.basicColorsSize.Width;
            int height = this.basicColorsSize.Height;

            using (Bitmap tempBitmap = new Bitmap(width, 1, PixelFormat.Format32bppPArgb))
            {
                FastBitmap fTempBitmap = new FastBitmap(tempBitmap);

                // Red to yellow
                for (byte index = 0; index <= 30; index++) // Skip the last one (31) because it is the same value as the first one of the next loop
                {
                    Color color = RomColor.From5BitRgb(31, index, 0);
                    fTempBitmap.SetPixel(index, 0, color);
                }

                // Yellow to green
                for (byte index = 31; index >= 1; index--) // Skip the last one (0) because it is the same value as the first one of the next loop
                {
                    Color color = RomColor.From5BitRgb(index, 31, 0);
                    fTempBitmap.SetPixel((31 - index) + 31, 0, color);
                }

                // Green to cyan
                for (byte index = 0; index <= 30; index++) // Skip the last one (31) because it is the same value as the first one of the next loop
                {
                    Color color = RomColor.From5BitRgb(0, 31, index);
                    fTempBitmap.SetPixel(index + 62, 0, color);
                }

                // Cyan to blue
                for (byte index = 31; index >= 1; index--) // Skip the last one (0) because it is the same value as the first one of the next loop
                {
                    Color color = RomColor.From5BitRgb(0, index, 31);
                    fTempBitmap.SetPixel((31 - index) + 93, 0, color);
                }

                // Blue to purple
                for (byte index = 0; index <= 30; index++) // Skip the last one (31) because it is the same value as the first one of the next loop
                {
                    Color color = RomColor.From5BitRgb(index, 0, 31);
                    fTempBitmap.SetPixel(index + 124, 0, color);
                }

                // Purple to red
                for (byte index = 31; index >= 1; index--) // Skip the last one (0) because it is the same value as the first one of the first loop
                {
                    Color color = RomColor.From5BitRgb(31, 0, index);
                    fTempBitmap.SetPixel((31 - index) + 155, 0, color);
                }

                fTempBitmap.Release();

                this.basicColorsCache = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
                using (Graphics g = Graphics.FromImage(this.basicColorsCache))
                {
                    g.PixelOffsetMode = PixelOffsetMode.Half;
                    g.InterpolationMode = InterpolationMode.NearestNeighbor;
                    g.DrawImage(tempBitmap, 0, 0, width, height);
                }
            }
        }

        /// <summary>
        /// Draws the shades of a color with the circle around a certain x, y position.
        /// </summary>
        /// <param name="x">The x coordinate to select.</param>
        /// <param name="y">The y coordinate to select.</param>
        /// <returns>The color at x, y.</returns>
        private RomColor DrawShadesBitmap(int x, int y)
        {
            this.selectedShadeLocation = new Point(x, y);
            RomColor selectedColor = this.shadesCache.GetPixel(x, y);

            this.shadesBitmap.Dispose();
            this.shadesBitmap = this.shadesCache.Clone() as Bitmap;

            using (Graphics g = Graphics.FromImage(this.shadesBitmap))
            using (Pen pen = new Pen(this.GetPenColor(x, y)))
            {
                g.DrawEllipse(pen, ColorPicker.GetSelectionBounds(x, y));
            }

            return selectedColor;
        }

        /// <summary>
        /// Draws the shades of a color with the circle around a certain color.
        /// This is used to find a color that user might have entered manually.
        /// It does not always draw the ellipse as not all colors are necessarily displayed.
        /// </summary>
        /// <param name="color">The color to draw a circle around.</param>
        private void DrawShadesBitmap(RomColor color)
        {
            this.shadesBitmap.Dispose();
            this.shadesBitmap = this.shadesCache.Clone() as Bitmap;

            FastBitmap fShadesBitmap = new FastBitmap(this.shadesBitmap);

            // The shades image is scaled by a factor of 2, so skip every other pixel
            for (int y = 0; y < this.shadesSize.Height; y += 2)
            {
                for (int x = 0; x < this.shadesSize.Width; x += 2)
                {
                    RomColor selectedColor = fShadesBitmap.GetPixel(x, y);
                    if (selectedColor == color)
                    {
                        fShadesBitmap.Release();
                        this.selectedShadeLocation = new Point(x, y);
                        using (Graphics g = Graphics.FromImage(this.shadesBitmap))
                        using (Pen pen = new Pen(this.GetPenColor(x, y)))
                        {
                            g.DrawEllipse(pen, ColorPicker.GetSelectionBounds(x, y));
                        }
                        return;
                    }
                }
            }

            fShadesBitmap.Release();
            this.selectedShadeLocation = new Point(63, 0);
        }

        /// <summary>
        /// Gets the color at a specific location.
        /// </summary>
        /// <param name="x">The x coordinate.</param>
        /// <param name="y">The y coordinate.</param>
        /// <returns>The color.</returns>
        private RomColor GetPenColor(int x, int y)
        {
            if (y > 63)
            {
                // Optimize contrast
                y = 127 - y;
            }

            return ((RomColor)this.shadesCache.GetPixel(x, y)).Opposite();
        }

        /// <summary>
        /// Initializes the shadesCache member.
        /// </summary>
        private void InitShadesCache()
        {
            int width = this.shadesSize.Width;
            int height = this.shadesSize.Height;
            int size = width / 2; // Unscaled image size
            int halfSize = size / 2;
            int x, y;

            // Generate the grays from black to white, these are at the bottom of the square, left to right
            RomColor[] grays = new RomColor[size];
            IEnumerator<RomColor> graysIte = RomColor.From5BitRgb(0, 0, 0).GetEnumerator(RomColor.From5BitRgb(31, 31, 31), size);
            x = 0;
            while (graysIte.MoveNext())
            {
                grays[x] = graysIte.Current.To5Bit();
                x++;
            }

            using (Bitmap tempBitmap = new Bitmap(size, size, PixelFormat.Format32bppPArgb))
            {
                FastBitmap fTempBitmap = new FastBitmap(tempBitmap);

                // Draw from black (top left) to our selected color (in the middle at the top)
                IEnumerator<RomColor> colorsIte = RomColor.From5BitRgb(0, 0, 0).GetEnumerator(this.selectedBasicColor, halfSize);
                x = 0;
                while (colorsIte.MoveNext())
                {
                    IEnumerator<RomColor> toGrayIte = colorsIte.Current.To5Bit().GetEnumerator(grays[x], size);
                    y = 0;
                    // Draw the vertical colors that goes from our shade (our color to black) to the gray variation at the bottom
                    while (toGrayIte.MoveNext())
                    {
                        RomColor color = toGrayIte.Current.To5Bit();
                        fTempBitmap.SetPixel(x, y, color);
                        y++;
                    }

                    x++;
                }

                // Draw from white (top right) to our selected color (in the middle at the top)
                colorsIte = this.selectedBasicColor.GetEnumerator(RomColor.From5BitRgb(31, 31, 31), halfSize);
                x = 0;
                while (colorsIte.MoveNext())
                {
                    IEnumerator<RomColor> toGrayIte = colorsIte.Current.To5Bit().GetEnumerator(grays[x + halfSize], size);
                    y = 0;
                    // Draw the vertical colors that goes from our shade (our color to white) to the gray variation at the bottom
                    while (toGrayIte.MoveNext())
                    {
                        RomColor color = toGrayIte.Current.To5Bit();
                        fTempBitmap.SetPixel(x + halfSize, y, color);
                        y++;
                    }

                    x++;
                }

                fTempBitmap.Release();

                this.shadesCache.Dispose();
                this.shadesCache = new Bitmap(width, height, PixelFormat.Format32bppPArgb);
                using (Graphics g = Graphics.FromImage(this.shadesCache))
                {
                    g.PixelOffsetMode = PixelOffsetMode.Half;
                    g.InterpolationMode = InterpolationMode.NearestNeighbor;
                    g.DrawImage(tempBitmap, 0, 0, width, height);
                }
            }
        }

        #endregion Bitmap drawing

        #region Event handlers

        /// <summary>
        /// Catches when the user moves in the basic colors while clicked.
        /// </summary>
        /// <param name="sender">The control which raised the event.</param>
        /// <param name="e">The mouse event arguments.</param>
        private void BasicColorsPictureBoxMouseMove(object sender, MouseEventArgs e)
        {
            if (this.basicColorsMouseDown)
            {
                this.BasicColorsClicked(e.X);
            }
        }

        /// <summary>
        /// Catches a user click in the basic colors.
        /// </summary>
        /// <param name="sender">The control which raised the event.</param>
        /// <param name="e">The event arguments.</param>
        private void BasicColorsPictureBoxClick(object sender, EventArgs e)
        {
            if (e is MouseEventArgs me)
            {
                this.BasicColorsClicked(me.X);
            }
        }

        /// <summary>
        /// Redraws shades and new color based on the location of the click in the basic colors.
        /// </summary>
        /// <param name="x">Location of the click.</param>
        private void BasicColorsClicked(int x)
        {
            this.SetColor(x);

            this.ColorChanged(this, EventArgs.Empty);
        }

        /// <summary>
        /// Catches when the user moves in the shades while clicked.
        /// </summary>
        /// <param name="sender">The control which raised the event.</param>
        /// <param name="e">The mouse event arguments.</param>
        private void ShadesPictureBoxMouseMove(object sender, MouseEventArgs e)
        {
            if (this.shadesPictureMouseDown)
            {
                this.ShadesClicked(e.X, e.Y);
            }
        }

        /// <summary>
        /// Catches a user click in the shades.
        /// </summary>
        /// <param name="sender">The control which raised the event.</param>
        /// <param name="e">The event arguments.</param>
        private void ShadesPictureBoxClick(object sender, EventArgs e)
        {
            if (e is MouseEventArgs me)
            {
                this.ShadesClicked(me.X, me.Y);
            }
        }

        private void ShadesClicked(int x, int y)
        {
            // Make sure we are not out of bounds
            if (x < 0)
            {
                x = 0;
            }
            else if (x >= this.shadesBitmap.Width)
            {
                x = this.shadesBitmap.Width - 1;
            }

            if (y < 0)
            {
                y = 0;
            }
            else if (y >= this.shadesBitmap.Height)
            {
                y = this.shadesBitmap.Height - 1;
            }

            this.InvalidateShadesSelection();
            RomColor color = this.DrawShadesBitmap(x, y);
            this.InvalidateShadesSelection();
            this.shadesPictureBox.Refresh();

            this.SetColorSub(color);

            this.ColorChanged(this, EventArgs.Empty);
        }

        /// <summary>
        /// Catches the user changing numbers for a 5-bit (0-31) color.
        /// </summary>
        /// <param name="sender">The control which raised the event.</param>
        /// <param name="e">The event arguments.</param>
        private void Color5BitNumericUpDownValueChanged(object sender, EventArgs e)
        {
            if (!this.fireEvents)
            {
                return;
            }

            RomColor color = RomColor.From5BitRgb((byte)red5NumericUpDown.Value, (byte)green5NumericUpDown.Value, (byte)blue5NumericUpDown.Value);
            this.SetColor(color);

            this.ColorChanged(this, EventArgs.Empty);
        }

        /// <summary>
        /// Catches the user changing numbers for an 8-bit (0-255) color.
        /// </summary>
        /// <param name="sender">The control which raised the event.</param>
        /// <param name="e">The event arguments.</param>
        private void Color8BitNumericUpDownValueChanged(object sender, EventArgs e)
        {
            if (!this.fireEvents)
            {
                return;
            }

            this.updating8BitColors = true;

            this.red5NumericUpDown.Value = RomColor.ConvertTo5BitColor((byte)red8NumericUpDown.Value);
            this.green5NumericUpDown.Value = RomColor.ConvertTo5BitColor((byte)green8NumericUpDown.Value);
            this.blue5NumericUpDown.Value = RomColor.ConvertTo5BitColor((byte)blue8NumericUpDown.Value);

            this.updating8BitColors = false;
        }

        #region Paint

        private void BasicColorsPictureBoxPaint(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawImage(this.basicColorsBitmap, 0, 0);
        }

        private void ShadesPictureBoxPaint(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawImage(this.shadesBitmap, 0, 0);
        }

        #endregion Paint

        private void BasicColorsPictureBoxMouseDown(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                this.basicColorsMouseDown = true;
            }
        }

        private void BasicColorsPictureBoxMouseUp(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                this.basicColorsMouseDown = false;
            }
        }

        private void ShadesPictureBoxMouseDown(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                this.shadesPictureMouseDown = true;
            }
        }

        private void ShadesPictureBoxMouseUp(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                this.shadesPictureMouseDown = false;
            }
        }

        private void NumericUpDownEnter(object sender, EventArgs e)
        {
            NumericUpDown control = sender as NumericUpDown;
            control.Select(0, control.Text.Length);
        }

        private void BasicColorsPictureBoxMouseLeave(object sender, EventArgs e)
        {
            this.basicColorsMouseDown = false;
        }

        private void ShadesPictureBoxMouseLeave(object sender, EventArgs e)
        {
            this.shadesPictureMouseDown = false;
        }

        #endregion Event handlers

        #region Find colors

        /// <summary>
        /// Finds the basic color of any RomColor.
        /// A basic color always has one of the three components equal to 255 (8 bit).
        /// </summary>
        /// <param name="color">The color from which to retrieve the basic color.</param>
        /// <returns>The basic color.</returns>
        public static RomColor FindBasicColor(RomColor color)
        {
            if (color.Red == color.Green && color.Green == color.Blue)
            {
                // If the color passed is a neutral gray (any gray from white to black with R = G = B),
                // then this color will be found in any shade, pass the default red.
                return Color.Red;
            }

            byte min = Math.Min(Math.Min(color.Red, color.Green), color.Blue);
            byte max = Math.Max(Math.Max(color.Red, color.Green), color.Blue);

            color.Red = ColorPicker.GetBasicColorChannel(color.Red, min, max);
            color.Green = ColorPicker.GetBasicColorChannel(color.Green, min, max);
            color.Blue = ColorPicker.GetBasicColorChannel(color.Blue, min, max);

            return color.To5Bit();
        }

        private static byte GetBasicColorChannel(byte value, byte min, byte max)
        {
            // The color channel(s) with the highest value will have a value of 255 (1 or 2 channels)
            // The color channel(s) with the smallest value will have a value of 0 (1 or 2 channels)
            // The remaining color channel between both bounds, if any, will have its value calculated
            return
                value == max ? byte.MaxValue :
                value == min ? byte.MinValue :
                (byte)(Math.Ceiling(255d * (value - min) / (max - min)));
        }

        private int FindColorIndex(RomColor color)
        {
            FastBitmap fBasicColors = new FastBitmap(this.basicColorsCache);
            for (int x = 0; x < this.basicColorsSize.Width; x++)
            {
                RomColor selectedColor = fBasicColors.GetPixel(x, 0);
                if (selectedColor == color)
                {
                    fBasicColors.Release();
                    return x;
                }
            }

            fBasicColors.Release();
            throw new ArgumentException("Invalid basic color.", nameof(color));
        }

        #endregion Find colors
    }
}
