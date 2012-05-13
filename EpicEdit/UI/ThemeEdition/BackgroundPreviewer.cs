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
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

using EpicEdit.Rom.Tracks.Scenery;
using EpicEdit.UI.Gfx;

namespace EpicEdit.UI.ThemeEdition
{
    /// <summary>
    /// Represents a background previewer.
    /// </summary>
    internal partial class BackgroundPreviewer : UserControl
    {
        private BackgroundPreviewDrawer drawer;
        private Timer repaintTimer;

        public BackgroundPreviewer()
        {
            this.InitializeComponent();
            this.drawer = new BackgroundPreviewDrawer();

            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);

            this.repaintTimer = new Timer();
            this.repaintTimer.Interval = 30;
            this.repaintTimer.Tick += delegate
            {
                this.Invalidate();
                this.drawer.IncrementFrame();
            };
        }

        public bool Paused
        {
            get { return !this.repaintTimer.Enabled; }
        }

        public void Play()
        {
            this.repaintTimer.Start();
        }

        public void Stop()
        {
            this.repaintTimer.Stop();
        }

        public void LoadBackground(Background background)
        {
            this.drawer.LoadBackground(background);
            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            this.drawer.DrawBackground(e.Graphics);
        }
    }
}