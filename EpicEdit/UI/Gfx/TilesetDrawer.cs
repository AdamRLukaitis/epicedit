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

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

using EpicEdit.Rom.Tracks;

namespace EpicEdit.UI.Gfx
{
    /// <summary>
    /// Provides the ability to paint the graphics of a tileset.
    /// </summary>
    public sealed class TilesetDrawer : IDisposable
    {
        public const int Zoom = 2;

        private Control control;
        private Tile[] tileset;
        private Size imageSize;

        private Bitmap tilesetCache;
        private Pen tilesetPen;

        public TilesetDrawer(Control control)
        {
            this.control = control;

            this.tilesetPen = new Pen(Color.FromArgb(150, 255, 0, 0));

            // The following member is initialized so it can be disposed of
            // in each function without having to check if it's null beforehand
            this.tilesetCache = new Bitmap(1, 1, PixelFormat.Format32bppPArgb);
        }

        public void SetTileset(Tile[] tileset)
        {
            this.tileset = tileset;

            this.UpdateCache();
        }

        private void UpdateCache()
        {
            this.tilesetCache.Dispose();

            int imageWidth = this.control.Width / Zoom;
            int imageHeight = (this.tileset.Length / (imageWidth / Tile.Size)) * Tile.Size;
            this.imageSize = new Size(imageWidth, imageHeight);

            this.tilesetCache = new Bitmap(this.imageSize.Width, this.imageSize.Height, PixelFormat.Format32bppPArgb);
            using (Graphics g = Graphics.FromImage(this.tilesetCache))
            {
                for (int x = 0; x < 8; x++)
                {
                    for (int y = 0; y < 32; y++)
                    {
                        Tile tile = this.tileset[x + (y * Tile.Size)];
                        g.DrawImage(tile.Bitmap, x * Tile.Size, y * Tile.Size);
                    }
                }
            }
        }

        public void DrawTileset(Graphics g, byte selectedTile)
        {
            using (Bitmap image = new Bitmap(this.imageSize.Width, this.imageSize.Height, PixelFormat.Format32bppPArgb))
            {
                g.InterpolationMode = InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = PixelOffsetMode.Half; // Solves a GDI+ bug which crops scaled images

                using (Graphics backBuffer = Graphics.FromImage(image))
                {
                    backBuffer.DrawImage(this.tilesetCache, 0, 0,
                                         this.imageSize.Width,
                                         this.imageSize.Height);

                    int tilePosX = selectedTile % Tile.Size;
                    int tilePosY = selectedTile / Tile.Size;
                    Point selectedTilePosition = new Point(tilePosX, tilePosY);

                    backBuffer.DrawRectangle(this.tilesetPen,
                                             selectedTilePosition.X * Tile.Size,
                                             selectedTilePosition.Y * Tile.Size,
                                             Tile.Size - 1,
                                             Tile.Size - 1);
                }
                g.DrawImage(image, 0, 0,
                            image.Width * Zoom,
                            image.Height * Zoom);
            }
        }

        public void Dispose()
        {
            this.tilesetCache.Dispose();
            this.tilesetPen.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
