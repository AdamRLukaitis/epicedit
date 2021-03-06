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
using EpicEdit.Rom.Settings;
using EpicEdit.Rom.Tracks;
using EpicEdit.Rom.Tracks.Items;
using EpicEdit.Rom.Utility;
using EpicEdit.UI.Tools;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Forms;

namespace EpicEdit.UI.SettingEdition
{
    /// <summary>
    /// Represents a collection of controls used to edit the probabilities for players to get each item.
    /// </summary>
    internal partial class ItemProbaEditor : UserControl
    {
        private ItemProbabilities itemProbabilities;
        private ItemProbability itemProbability;

        /// <summary>
        /// Flag to prevent events being fired in sequence from one control to another.
        /// </summary>
        private bool fireEvents;

        /// <summary>
        /// Raised when a pixel color has been selected.
        /// </summary>
        [Browsable(true), Category("Behavior")]
        public event EventHandler<EventArgs<Palette, int>> ColorSelected
        {
            add
            {
                this.mushroomPanel.ColorSelected += value;
                this.featherPanel.ColorSelected += value;
                this.starPanel.ColorSelected += value;
                this.bananaPanel.ColorSelected += value;
                this.greenPanel.ColorSelected += value;
                this.redPanel.ColorSelected += value;
                this.ghostPanel.ColorSelected += value;
                this.coinsPanel.ColorSelected += value;
                this.lightningPanel.ColorSelected += value;
            }
            remove
            {
                this.mushroomPanel.ColorSelected -= value;
                this.featherPanel.ColorSelected -= value;
                this.starPanel.ColorSelected -= value;
                this.bananaPanel.ColorSelected -= value;
                this.greenPanel.ColorSelected -= value;
                this.redPanel.ColorSelected -= value;
                this.ghostPanel.ColorSelected -= value;
                this.coinsPanel.ColorSelected -= value;
                this.lightningPanel.ColorSelected -= value;
            }
        }

        public ItemProbaEditor()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Sets the Value of the Control using the Description of the underlying Enum item.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ItemComboBoxFormat(object sender, ListControlConvertEventArgs e)
        {
            e.Value = UITools.GetDescription(e.Value);
        }

        #region Fields initialization and display

        public void Init()
        {
            this.fireEvents = false;

            this.itemProbabilities = Context.Game.Settings.ItemProbabilities;

            this.InitModeComboBox();
            this.InitSetComboBox();
            this.InitLapRankComboBox();
            this.InitItemBoxDisplayOptionComboBox();

            this.InitProbability();
            this.DisplayProbability();

            this.InitImages();

            this.fireEvents = true;
        }

        private void InitModeComboBox()
        {
            this.modeComboBox.BeginUpdate();
            this.modeComboBox.Items.Clear();

            foreach (TextItem textItem in Context.Game.Settings.ModeNames)
            {
                this.modeComboBox.Items.Add(textItem.FormattedValue);
            }

            this.modeComboBox.EndUpdate();
            this.modeComboBox.SelectedIndex = 0;
        }

        private void InitSetComboBox()
        {
            this.setComboBox.BeginUpdate();
            this.setComboBox.Items.Clear();

            for (int i = 0; i < ItemProbabilities.SetCount; i++)
            {
                this.setComboBox.Items.Add("Probability set " + (i + 1));
            }

            this.setComboBox.EndUpdate();
            this.setComboBox.SelectedIndex = 0;
        }

        private void InitLapRankComboBox()
        {
            this.lapRankComboBox.BeginUpdate();
            this.lapRankComboBox.Items.Clear();

            switch (this.modeComboBox.SelectedIndex)
            {
                case 0:
                    this.lapRankComboBox.Items.Add(ItemProbabilityGrandprixCondition.Lap1_1st);
                    this.lapRankComboBox.Items.Add(ItemProbabilityGrandprixCondition.Lap2To5_2ndTo4th);
                    this.lapRankComboBox.Items.Add(ItemProbabilityGrandprixCondition.Lap2To5_5thTo8th);
                    this.lapRankComboBox.SelectedIndex = 0;
                    break;

                case 1:
                    this.lapRankComboBox.Items.Add(ItemProbabilityMatchRaceCondition.Lap1);
                    this.lapRankComboBox.Items.Add(ItemProbabilityMatchRaceCondition.Lap2To5_1st);
                    this.lapRankComboBox.Items.Add(ItemProbabilityMatchRaceCondition.Lap2To5_2nd);
                    this.lapRankComboBox.SelectedIndex = 0;
                    break;
            }

            this.lapRankComboBox.EndUpdate();
        }

        private void InitItemBoxDisplayOptionComboBox()
        {
            this.itemBoxDisplayOptions.BeginUpdate();
            this.itemBoxDisplayOptions.Items.Clear();

            switch (this.modeComboBox.SelectedIndex)
            {
                case 0:
                    this.itemBoxDisplayOptions.Items.Add(ItemBoxDisplay.NoGhosts);
                    this.itemBoxDisplayOptions.Items.Add(ItemBoxDisplay.NoGhostsOrFeathers);
                    break;

                case 1:
                    this.itemBoxDisplayOptions.Items.Add(ItemBoxDisplay.AllItems);
                    this.itemBoxDisplayOptions.Items.Add(ItemBoxDisplay.NoFeathers);
                    this.itemBoxDisplayOptions.Items.Add(ItemBoxDisplay.NoCoinsOrLightnings);
                    this.itemBoxDisplayOptions.Items.Add(ItemBoxDisplay.NoGhosts);
                    this.itemBoxDisplayOptions.Items.Add(ItemBoxDisplay.NoGhostsOrFeathers);
                    break;

                case 2:
                    this.itemBoxDisplayOptions.Items.Add(ItemBoxDisplay.NoCoinsOrLightnings);
                    break;
            }

            this.itemBoxDisplayOptions.EndUpdate();
        }

        private void InitProbability()
        {
            switch (this.modeComboBox.SelectedIndex)
            {
                case 0: // GP
                    this.itemProbability = this.itemProbabilities.GetGrandprixProbability(this.setComboBox.SelectedIndex, (ItemProbabilityGrandprixCondition)this.lapRankComboBox.SelectedItem);
                    if (this.itemProbability.DisplayedItems != ItemBoxDisplay.NoGhosts &&
                        this.itemProbability.DisplayedItems != ItemBoxDisplay.NoGhostsOrFeathers)
                    {
                        this.itemProbability.DisplayedItems = ItemBoxDisplay.NoGhosts;
                    }
                    break;

                case 1: // Match Race
                    this.itemProbability = this.itemProbabilities.GetMatchRaceProbability(this.setComboBox.SelectedIndex, (ItemProbabilityMatchRaceCondition)this.lapRankComboBox.SelectedItem);
                    break;

                case 2: // Battle Mode
                    this.itemProbability = this.itemProbabilities.GetBattleModeProbability();
                    this.itemProbability.DisplayedItems = ItemBoxDisplay.NoCoinsOrLightnings;
                    break;
            }
        }

        private void DisplayProbability()
        {
            // Back up the fireEvents value to restore it at the end of the method
            bool fireEventsBefore = this.fireEvents;

            // Disable events being fired by updating the various fields
            this.fireEvents = false;

            this.coinsLabel.Enabled =
                this.coinsPanel.LooksEnabled =
                this.coinsNumericUpDown.Enabled =
                this.coinsPctLabel.Enabled =
                this.itemProbability.DisplayedItems != ItemBoxDisplay.NoCoinsOrLightnings;

            this.featherLabel.Enabled =
                this.featherPanel.LooksEnabled =
                this.featherNumericUpDown.Enabled =
                this.featherPctLabel.Enabled =
                this.itemProbability.DisplayedItems != ItemBoxDisplay.NoFeathers &&
                this.itemProbability.DisplayedItems != ItemBoxDisplay.NoGhostsOrFeathers;

            this.ghostLabel.Enabled =
                this.ghostPanel.LooksEnabled =
                this.ghostNumericUpDown.Enabled =
                this.ghostPctLabel.Enabled =
                this.itemProbability.DisplayedItems != ItemBoxDisplay.NoGhosts &&
                this.itemProbability.DisplayedItems != ItemBoxDisplay.NoGhostsOrFeathers;

            this.lightningLabel.Enabled =
                this.lightningPanel.LooksEnabled =
                this.lightningValue.Enabled =
                this.lightningPctLabel.Enabled =
                this.itemProbability.DisplayedItems != ItemBoxDisplay.NoCoinsOrLightnings;

            this.ghostNumericUpDown.ReadOnly = !this.lightningLabel.Enabled;

            CultureInfo ci = CultureInfo.CurrentCulture;

            int total = this.itemProbability.Total;
            this.mushroomNumericUpDown.Value = this.itemProbability.Mushroom;
            this.featherNumericUpDown.Value = this.itemProbability.Feather;
            this.starNumericUpDown.Value = this.itemProbability.Star;
            this.bananaNumericUpDown.Value = this.itemProbability.Banana;
            this.greenNumericUpDown.Value = this.itemProbability.GreenShell;
            this.redNumericUpDown.Value = this.itemProbability.RedShell;
            this.ghostNumericUpDown.Value = this.itemProbability.Ghost;
            this.coinsNumericUpDown.Value = this.itemProbability.Coins;
            this.lightningValue.Text = this.itemProbability.Lightning.ToString(ci);
            this.totalValue.Text = total.ToString(ci);
            this.itemBoxDisplayOptions.SelectedItem = this.itemProbability.DisplayedItems;

            this.mushroomPctLabel.Text = ((float)this.itemProbability.Mushroom / total).ToString("P1", ci);
            this.featherPctLabel.Text = ((float)this.itemProbability.Feather / total).ToString("P1", ci);
            this.starPctLabel.Text = ((float)this.itemProbability.Star / total).ToString("P1", ci);
            this.bananaPctLabel.Text = ((float)this.itemProbability.Banana / total).ToString("P1", ci);
            this.greenPctLabel.Text = ((float)this.itemProbability.GreenShell / total).ToString("P1", ci);
            this.redPctLabel.Text = ((float)this.itemProbability.RedShell / total).ToString("P1", ci);
            this.ghostPctLabel.Text = ((float)this.itemProbability.Ghost / total).ToString("P1", ci);
            this.coinsPctLabel.Text = ((float)this.itemProbability.Coins / total).ToString("P1", ci);
            this.lightningPctLabel.Text = ((float)this.itemProbability.Lightning / total).ToString("P1", ci);
            this.totalPctLabel.Text = 1.ToString("P1", ci);

            this.fireEvents = fireEventsBefore;
        }

        private void InitImages()
        {
            this.mushroomPanel.UpdateImage();
            this.featherPanel.UpdateImage();
            this.starPanel.UpdateImage();
            this.bananaPanel.UpdateImage();
            this.greenPanel.UpdateImage();
            this.redPanel.UpdateImage();
            this.ghostPanel.UpdateImage();
            this.coinsPanel.UpdateImage();
            this.lightningPanel.UpdateImage();
        }

        #endregion Fields initialization and display

        #region Event handlers

        private void ValueChanged(object sender, EventArgs e)
        {
            if (!this.fireEvents)
            {
                // This event may be fired because of DisplayProbability, skip in that case
                return;
            }

            // Set the values into the item probabilities object
            if (this.itemProbability.DisplayedItems == ItemBoxDisplay.NoCoinsOrLightnings)
            {
                // Set the Ghost value to 0, then affect the Lightning value to it later.
                // This way, the Lightning value will always be 0, and the difference will
                // be affected to the Ghost value instead.
                this.itemProbability.Ghost = 0;
            }
            else
            {
                this.itemProbability.Ghost = (int)this.ghostNumericUpDown.Value;
            }

            this.itemProbability.Mushroom = (int)this.mushroomNumericUpDown.Value;
            this.itemProbability.Feather = (int)this.featherNumericUpDown.Value;
            this.itemProbability.Star = (int)this.starNumericUpDown.Value;
            this.itemProbability.Banana = (int)this.bananaNumericUpDown.Value;
            this.itemProbability.GreenShell = (int)this.greenNumericUpDown.Value;
            this.itemProbability.RedShell = (int)this.redNumericUpDown.Value;
            this.itemProbability.Coins = (int)this.coinsNumericUpDown.Value;

            if (this.itemProbability.DisplayedItems == ItemBoxDisplay.NoCoinsOrLightnings)
            {
                this.itemProbability.Ghost = this.itemProbability.Lightning;
            }

            this.itemProbability.DisplayedItems = (ItemBoxDisplay)this.itemBoxDisplayOptions.SelectedItem;

            // Then redisplay them as validation occurs within the object
            this.DisplayProbability();
        }

        private void ModeComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            if (!this.fireEvents)
            {
                return;
            }

            // Disable events so they don't get fired multiple times by other controls being updated
            this.fireEvents = false;

            this.InitLapRankComboBox();
            this.InitItemBoxDisplayOptionComboBox();
            this.lapRankComboBox.Enabled = this.modeComboBox.SelectedIndex != 2;
            this.setComboBox.Enabled = this.modeComboBox.SelectedIndex != 2;
            if (this.modeComboBox.SelectedIndex == 2)
            {
                this.setComboBox.SelectedItem = null;
            }
            else if (this.setComboBox.SelectedItem == null)
            {
                this.setComboBox.SelectedIndex = 0;
            }

            this.itemBoxDisplayOptions.Enabled = this.modeComboBox.SelectedIndex != 2;
            this.itemBoxDisplayOptionLabel.Enabled = this.modeComboBox.SelectedIndex != 2;

            this.InitProbability();
            this.DisplayProbability();

            this.fireEvents = true;
        }

        private void LapRankComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            if (!this.fireEvents)
            {
                return;
            }

            this.InitProbability();
            this.DisplayProbability();
        }

        private void SetComboBoxSelectedIndexChanged(object sender, EventArgs e)
        {
            if (!this.fireEvents)
            {
                return;
            }

            this.InitProbability();
            this.DisplayProbability();
        }

        private Theme theme;

        [Browsable(false), DefaultValue(typeof(Theme), "")]
        public Theme Theme
        {
            get => this.theme;
            set
            {
                if (this.theme == value)
                {
                    return;
                }

                if (this.theme != null)
                {
                    for (int i = 0; i < Palettes.SpritePaletteStart; i++)
                    {
                        this.theme.Palettes[i].ColorGraphicsChanged -= this.palette_ColorsGraphicsChanged;
                        this.theme.Palettes[i].ColorsGraphicsChanged -= this.palette_ColorsGraphicsChanged;
                    }
                }

                this.theme = value;

                for (int i = 0; i < Palettes.SpritePaletteStart; i++)
                {
                    this.theme.Palettes[i].ColorGraphicsChanged += this.palette_ColorsGraphicsChanged;
                    this.theme.Palettes[i].ColorsGraphicsChanged += this.palette_ColorsGraphicsChanged;
                }

                this.UpdateIcons(this.theme);
            }
        }

        private void palette_ColorsGraphicsChanged(object sender, EventArgs e)
        {
            this.InitImages();
        }

        public void ShowTrackData(Track track)
        {
            if (track is GPTrack gpTrack)
            {
                this.modeComboBox.SelectedIndex = 0;
                this.setComboBox.SelectedIndex = gpTrack.ItemProbabilityIndex;
                this.lapRankComboBox.SelectedIndex = 0;
            }
            else
            {
                this.modeComboBox.SelectedIndex = 2;
            }
        }

        private void UpdateIcons(Theme theme)
        {
            this.mushroomPanel.Theme = theme;
            this.featherPanel.Theme = theme;
            this.starPanel.Theme = theme;
            this.bananaPanel.Theme = theme;
            this.greenPanel.Theme = theme;
            this.redPanel.Theme = theme;
            this.ghostPanel.Theme = theme;
            this.coinsPanel.Theme = theme;
            this.lightningPanel.Theme = theme;
        }

        private void ResetButtonClick(object sender, EventArgs e)
        {
            this.itemProbability.Reset();
            this.DisplayProbability();
        }

        private void ImportProbabilitiesButtonClick(object sender, EventArgs e)
        {
            if (UITools.ShowImportBinaryDataDialog(this.itemProbabilities.SetBytes))
            {
                this.Init();
            }
        }

        private void ExportProbabilitiesButtonClick(object sender, EventArgs e)
        {
            UITools.ShowExportBinaryDataDialog(this.itemProbabilities.GetBytes, "Item probabilities");
        }

        #endregion Event handlers
    }
}
