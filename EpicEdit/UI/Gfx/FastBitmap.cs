#region GPL statement
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
using System.Drawing.Imaging;

namespace EpicEdit.UI.Gfx
{
    internal unsafe class FastBitmap
    {
        private struct PixelData
        {
            public byte blue;
            public byte green;
            public byte red;
            public byte alpha;
        }

        private readonly Bitmap subject;
        private int subjectWidth;
        private BitmapData bitmapData;
        private Byte* pBase;

        public FastBitmap(Bitmap subjectBitmap)
        {
            this.subject = subjectBitmap;
            this.LockBitmap();
        }

        public void Release()
        {
            this.UnlockBitmap();
        }

        public static implicit operator Bitmap(FastBitmap fBitmap)
        {
            return fBitmap.subject;
        }

        public Bitmap ToBitmap()
        {
            return this.subject;
        }

        public Bitmap Bitmap => this.subject;

        public void SetPixel(int x, int y, Color color)
        {
            PixelData* p = this.PixelAt(x, y);
            p->alpha = color.A;
            p->red = color.R;
            p->green = color.G;
            p->blue = color.B;
        }

        public Color GetPixel(int x, int y)
        {
            PixelData* p = this.PixelAt(x, y);
            return Color.FromArgb(p->alpha, p->red, p->green, p->blue);
        }

        private PixelData* PixelAt(int x, int y)
        {
            return (PixelData*)(this.pBase + y * this.subjectWidth + x * sizeof(PixelData));
        }

        private void LockBitmap()
        {
            Rectangle bounds = new Rectangle(Point.Empty, this.subject.Size);

            this.subjectWidth = bounds.Width * sizeof(PixelData);
            if (this.subjectWidth % 4 != 0)
            {
                this.subjectWidth = 4 * (this.subjectWidth / 4 + 1);
            }

            this.bitmapData = this.subject.LockBits(bounds, ImageLockMode.ReadWrite, PixelFormat.Format32bppPArgb);
            this.pBase = (Byte*)this.bitmapData.Scan0.ToPointer();
        }

        private void UnlockBitmap()
        {
            this.subject.UnlockBits(this.bitmapData);
            this.bitmapData = null;
            this.pBase = null;
        }
    }
}
