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
using System.Collections.Generic;

namespace EpicEdit.Rom
{
    /// <summary>
    /// Buffer which contains all the changes that need to be saved back into the ROM.
    /// </summary>
    public class SaveBuffer
    {
        private byte[] romBuffer;
        private Queue<byte[]> savedData;
        private Range zone;

        /// <summary>
        /// Gets the current index value.
        /// </summary>
        public int Index { get; private set; }

        public SaveBuffer(byte[] romBuffer)
        {
            this.romBuffer = romBuffer;
            this.savedData = new Queue<byte[]>();

            int zoneStart = Game.RomSize.Size512;
            int zoneEnd = Math.Min(this.romBuffer.Length, Game.RomSize.Size1024);
            this.zone = new Range(zoneStart, zoneEnd);
            this.Index = this.zone.Start;
        }

        public void Add(byte[] data)
        {
            this.savedData.Enqueue(data);
            this.Index += data.Length;
        }

        public bool Includes(int offset)
        {
            return this.zone.Includes(offset);
        }

        public byte[] GetRomBuffer()
        {
            // We return the romBuffer rather than simply calling the
            // SaveToRomBuffer void method, because the romBuffer reference
            // may have changed if the ResizeRomBuffer method was called.
            this.SaveToRomBuffer();
            return this.romBuffer;
        }

        private void SaveToRomBuffer()
        {
            this.CheckDataSize();

            // Save data to buffer
            int index = this.zone.Start;
            foreach (byte[] dataBlock in savedData)
            {
                Array.Copy(dataBlock, 0, this.romBuffer, index, dataBlock.Length);
                index += dataBlock.Length;
            }

            // Wipe out the rest of the zone
            for (int i = index; i < this.zone.End; i++)
            {
                this.romBuffer[i] = 0xFF;
            }
        }

        private void CheckDataSize()
        {
            // Compute total size of all the saved data to make sure it fits
            int savedDataSize = 0;
            foreach (byte[] dataBlock in this.savedData)
            {
                savedDataSize += dataBlock.Length;
            }

            // Check if all the saved data fits in the zone
            if (savedDataSize > this.zone.Length)
            {
                if (savedDataSize <= Game.RomSize.Size512)
                {
                    if (this.zone.Length == 0 && // If the ROM is 512 KiB (ie: the original SMK ROM size)
                        savedDataSize > Game.RomSize.Size256) // And if the data that needs to be saved is over 256 Kib
                    {
                        this.ExpandRomBuffer(Game.RomSize.Size512);
                    }
                    else
                    {
                        // The ROM size is 512 or 768 KiB
                        // and can be expanded by 256 KiB to make all the data fit
                        this.ExpandRomBuffer(Game.RomSize.Size256);
                    }

                    this.zone.End = this.romBuffer.Length;
                }
                else
                {
                    // The data doesn't fit and we can't expand the ROM for more free space
                    throw new InvalidOperationException("It's not possible to fit more data in this ROM.");
                }
            }
        }

        /// <summary>
        /// Expands the ROM buffer by the given value.
        /// </summary>
        /// <param name="expandValue">Number of bytes added to the buffer.</param>
        private void ExpandRomBuffer(int expandValue)
        {
            this.ResizeRomBuffer(this.romBuffer.Length + expandValue);
        }

        /// <summary>
        /// Resize the ROM buffer to the given size.
        /// </summary>
        /// <param name="newSize">New ROM buffer length.</param>
        private void ResizeRomBuffer(int size)
        {
            if (size > Game.RomSize.Size8192)
            {
                throw new ArgumentOutOfRangeException("newSize", "The ROM can't be expanded because the maximum size has been reached.");
            }

            byte[] resizedRomBuffer = new byte[size];
            Array.Copy(this.romBuffer, resizedRomBuffer, this.romBuffer.Length);

            this.romBuffer = resizedRomBuffer;
        }
    }
}