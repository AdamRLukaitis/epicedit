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
using EpicEdit.Rom.Tracks;
using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace EpicEdit.UI.TrackEdition
{
    /// <summary>
    /// Represents a collection of controls to edit <see cref="EpicEdit.Rom.Tracks.Start.LapLine"/> and <see cref="EpicEdit.Rom.Tracks.Start.GPStartPosition"/> objects.
    /// </summary>
    internal partial class StartControl : UserControl
    {
        /// <summary>
        /// The current track.
        /// </summary>
        private Track track;

        /// <summary>
        /// Gets or sets the current track.
        /// </summary>
        [Browsable(false), DefaultValue(typeof(Track), "")]
        public Track Track
        {
            get => this.track;
            set
            {
                if (this.track == value)
                {
                    return;
                }

                if (this.track is GPTrack oldGPTrack)
                {
                    oldGPTrack.StartPosition.PropertyChanged -= this.gpTrack_StartPosition_PropertyChanged;
                }

                this.track = value;

                if (!(this.track is GPTrack gpTrack))
                {
                    this.gpTrackGroupBox.Enabled = false;
                }
                else
                {
                    this.gpTrackGroupBox.Enabled = true;

                    // NOTE: Temporarily detach the secondRowNumericUpDown.ValueChanged event handler
                    // so that the current precision does not alter the second row offset on track load.
                    this.secondRowNumericUpDown.ValueChanged -= this.SecondRowValueLabelNumericUpDownValueChanged;
                    this.secondRowNumericUpDown.Value = gpTrack.StartPosition.SecondRowOffset;
                    this.secondRowNumericUpDown.ValueChanged += this.SecondRowValueLabelNumericUpDownValueChanged;

                    this.secondRowTrackBar.Value = gpTrack.StartPosition.SecondRowOffset;
                    gpTrack.StartPosition.PropertyChanged += this.gpTrack_StartPosition_PropertyChanged;
                }
            }
        }

        public StartControl()
        {
            this.InitializeComponent();
            this.SetPrecision();
        }

        private void gpTrack_StartPosition_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == PropertyNames.GPStartPosition.SecondRowOffset)
            {
                GPTrack gpTrack = this.track as GPTrack;
                this.secondRowNumericUpDown.Value = gpTrack.StartPosition.SecondRowOffset;
                this.secondRowTrackBar.Value = gpTrack.StartPosition.SecondRowOffset;
            }
        }

        public int Precision { get; private set; }

        public bool LapLineAndDriverPositionsBound => this.startBindCheckBox.Checked;

        private void SecondRowValueLabelNumericUpDownValueChanged(object sender, EventArgs e)
        {
            GPTrack gpTrack = this.track as GPTrack;
            gpTrack.StartPosition.SecondRowOffset = this.GetPrecisionValue((int)this.secondRowNumericUpDown.Value);

            // Make sure the UI reflects the validated SecondRowOffset value
            this.secondRowNumericUpDown.Value = gpTrack.StartPosition.SecondRowOffset;
        }

        private void SecondRowTrackBarScroll(object sender, EventArgs e)
        {
            GPTrack gpTrack = this.track as GPTrack;
            gpTrack.StartPosition.SecondRowOffset = this.GetPrecisionValue(this.secondRowTrackBar.Value);
        }

        private void SecondRowTrackBarValueChanged(object sender, EventArgs e)
        {
            GPTrack gpTrack = this.track as GPTrack;
            // Make sure the UI reflects the validated SecondRowOffset value
            this.secondRowTrackBar.Value = gpTrack.StartPosition.SecondRowOffset;
        }

        private void StepRadioButtonCheckedChanged(object sender, EventArgs e)
        {
            RadioButton button = sender as RadioButton;

            // Avoid calling the method twice (once for the button that was previously checked, then the one newly checked)
            if (button.Checked)
            {
                this.SetPrecision();
            }
        }

        private void SetPrecision()
        {
            this.Precision =
                this.step1pxRadioButton.Checked ? 1 :
                this.step4pxRadioButton.Checked ? 4 :
                8;

            this.secondRowNumericUpDown.Increment = this.Precision;
            this.secondRowTrackBar.SmallChange = this.Precision;
            this.secondRowTrackBar.LargeChange = this.Precision * 5;
        }

        private int GetPrecisionValue(int value)
        {
            return (value / this.Precision) * this.Precision;
        }
    }
}
