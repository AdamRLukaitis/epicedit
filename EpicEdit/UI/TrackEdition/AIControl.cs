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
using System.Reflection;
using System.Windows.Forms;

using EpicEdit.Rom.Tracks;
using EpicEdit.Rom.Tracks.AI;
using EpicEdit.UI.Tools;

namespace EpicEdit.UI.TrackEdition
{
	/// <summary>
	/// Represents a collection of controls to edit <see cref="TrackAI"/>.
	/// </summary>
	public partial class AIControl : UserControl
	{
		[Browsable(true)]
		public event EventHandler<EventArgs> DataChanged;

		[Browsable(true)]
		public event EventHandler<EventArgs> DeleteRequested;

		/// <summary>
		/// The AI of the current track.
		/// </summary>
		private TrackAI trackAI = null;

		/// <summary>
		/// The selected AI element.
		/// </summary>
		private TrackAIElement selectedAIElem;

		/// <summary>
		/// Gets the selected AI element.
		/// </summary>
		[Browsable(false)]
		public TrackAIElement SelectedAIElem
		{
			get
			{
				return this.selectedAIElem;
			}
		}

		[Browsable(false), DefaultValue(typeof(TrackAI), "")]
		public TrackAI TrackAI
		{
			get
			{
				return this.trackAI;
			}
			set
			{
				this.trackAI = value;

				this.SetSelectedAIElement(null);
				this.SetMaximumAIElementIndex();
			}
		}

		public AIControl()
		{
			this.InitializeComponent();

			this.shapeComboBox.DataSource = Enum.GetValues(typeof(Shape));
			this.shapeComboBox.SelectedIndexChanged += new EventHandler(this.ShapeComboBoxSelectedIndexChanged);
		}

		private void ShapeComboBoxFormat(object sender, ListControlConvertEventArgs e)
		{
			UITools.SetValueFromEnumDescription(e);
		}

		public void SetSelectedAIElement(TrackAIElement aiElement)
		{
			this.selectedAIElem = aiElement;

			if (this.selectedAIElem == null)
			{
				this.selectedAIElementGroupBox.Enabled = false;
			}
			else
			{
				this.selectedAIElementGroupBox.Enabled = true;

				this.indexNumericUpDown.ValueChanged -= new EventHandler(this.IndexNumericUpDownValueChanged);
				this.indexNumericUpDown.Value = this.trackAI.GetElementIndex(this.selectedAIElem);
				this.indexNumericUpDown.ValueChanged += new EventHandler(this.IndexNumericUpDownValueChanged);

				this.speedNumericUpDown.ValueChanged -= new EventHandler(this.SpeedNumericUpDownValueChanged);
				this.speedNumericUpDown.Value = this.selectedAIElem.Speed + 1;
				this.speedNumericUpDown.ValueChanged += new EventHandler(this.SpeedNumericUpDownValueChanged);

				this.shapeComboBox.SelectedIndexChanged -= new EventHandler(this.ShapeComboBoxSelectedIndexChanged);
				this.shapeComboBox.SelectedItem = this.selectedAIElem.ZoneShape;
				this.shapeComboBox.SelectedIndexChanged += new EventHandler(this.ShapeComboBoxSelectedIndexChanged);
			}

			// Force controls to refresh so that the new data shows up instantly
			// NOTE: we could do this.selectedAIElementGroupBox.Refresh(); instead
			// but that would cause some minor flickering
			this.indexNumericUpDown.Refresh();
			this.speedNumericUpDown.Refresh();
			this.shapeComboBox.Refresh();
		}

		private void IndexNumericUpDownValueChanged(object sender, EventArgs e)
		{
			int oldIndex = this.trackAI.GetElementIndex(this.selectedAIElem);
			int newIndex = (int)this.indexNumericUpDown.Value;
			this.trackAI.ChangeElementIndex(oldIndex, newIndex);
			
			this.DataChanged(this, EventArgs.Empty);
		}

		private void SpeedNumericUpDownValueChanged(object sender, EventArgs e)
		{
			this.selectedAIElem.Speed = (byte)(this.speedNumericUpDown.Value - 1);

			this.DataChanged(this, EventArgs.Empty);
		}

		private void ShapeComboBoxSelectedIndexChanged(object sender, EventArgs e)
		{
			Shape newShape = (Shape)this.shapeComboBox.SelectedValue;
			this.selectedAIElem.ChangeShape(newShape);

			this.DataChanged(this, EventArgs.Empty);
		}

		private void DeleteButtonClick(object sender, EventArgs e)
		{
			this.DeleteRequested(this, EventArgs.Empty);
		}

		public void SetMaximumAIElementIndex()
		{
			this.indexNumericUpDown.ValueChanged -= new EventHandler(this.IndexNumericUpDownValueChanged);
			this.indexNumericUpDown.Maximum = this.trackAI.ElementCount - 1;
			this.indexNumericUpDown.ValueChanged += new EventHandler(this.IndexNumericUpDownValueChanged);
		}
	}
}
