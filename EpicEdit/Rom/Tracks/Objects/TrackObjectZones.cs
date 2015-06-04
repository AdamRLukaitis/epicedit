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
using EpicEdit.Rom.Tracks.AI;
using EpicEdit.Rom.Tracks.Road;

namespace EpicEdit.Rom.Tracks.Objects
{
    /// <summary>
    /// A collection of <see cref="TrackObject"/> zones.
    /// A track object only appears in a track if it is located within its designated zone.
    /// </summary>
    internal class TrackObjectZones
    {
        public const int Size = 10;

        /// <summary>
        /// The object zone grid size (horizontally and vertically).
        /// </summary>
        public const int GridSize = TrackMap.Size / TrackAIElement.Precision;

        /// <summary>
        /// The maximum value (horizontally and vertically) within the object zone grid.
        /// </summary>
        private const int GridLimit = GridSize - 1;

        private readonly TrackAI ai;

        private readonly byte[] frontZones;
        private readonly byte[] rearZones;

        public TrackObjectZones(byte[] data, TrackAI ai)
        {
            this.ai = ai;

            this.frontZones = new byte[4];
            this.frontZones[0] = data[0];
            this.frontZones[1] = Math.Max(this.frontZones[0], data[1]);
            this.frontZones[2] = Math.Max(this.frontZones[1], data[2]);
            this.frontZones[3] = Math.Max(this.frontZones[2], data[3]);

            this.rearZones = new byte[4];
            this.rearZones[0] = data[5];
            this.rearZones[1] = Math.Max(this.rearZones[0], data[6]);
            this.rearZones[2] = Math.Max(this.rearZones[1], data[7]);
            this.rearZones[3] = Math.Max(this.rearZones[2], data[8]);
        }

        private byte GetZoneIndex(bool frontZonesView, int aiElementIndex)
        {
            return frontZonesView ?
                TrackObjectZones.GetZoneIndexSub(this.frontZones, aiElementIndex) :
                TrackObjectZones.GetZoneIndexSub(this.rearZones, aiElementIndex);
        }

        private static byte GetZoneIndexSub(byte[] zones, int aiElementIndex)
        {
            for (byte i = 0; i < zones.Length; i++)
            {
                if (aiElementIndex < zones[i])
                {
                    return i;
                }
            }

            return 0;
        }

        public byte GetZoneValue(bool frontZonesView, int zoneIndex)
        {
            if (frontZonesView)
            {
                return this.frontZones[zoneIndex];
            }
            else
            {
                return this.rearZones[zoneIndex];
            }
        }

        public void SetZoneValue(bool frontZonesView, int zoneIndex, byte value)
        {
            if (frontZonesView)
            {
                this.frontZones[zoneIndex] = value;
            }
            else
            {
                this.rearZones[zoneIndex] = value;
            }
        }

        public byte[][] GetGrid(bool frontZonesView)
        {
            byte[][] zones;

            if (this.ai.ElementCount == 0)
            {
                zones = new byte[GridSize][];

                for (int y = 0; y < zones.Length; y++)
                {
                    zones[y] = new byte[GridSize];
                }

                return zones;
            }

            sbyte[][] sZones = TrackObjectZones.InitZones();
            this.FillGridFromAI(sZones, frontZonesView);
            zones = TrackObjectZones.GetGridFilledFromNearestTiles(sZones);

            return zones;
        }

        private static sbyte[][] InitZones()
        {
            sbyte[][] zones = new sbyte[GridSize][];

            for (int y = 0; y < zones.Length; y++)
            {
                zones[y] = new sbyte[GridSize];
            }

            for (int x = 0; x < zones[0].Length; x++)
            {
                zones[0][x] = -1;
            }

            for (int y = 1; y < zones.Length; y++)
            {
                Buffer.BlockCopy(zones[0], 0, zones[y], 0, zones[y].Length);
            }

            return zones;
        }

        private void FillGridFromAI(sbyte[][] zones, bool frontZonesView)
        {
            foreach (TrackAIElement aiElem in this.ai)
            {
                int aiElemIndex = this.ai.GetElementIndex(aiElem);
                sbyte zoneIndex = (sbyte)this.GetZoneIndex(frontZonesView, aiElemIndex);
                int left = aiElem.Zone.X / TrackAIElement.Precision;
                int top = aiElem.Zone.Y / TrackAIElement.Precision;
                int right = aiElem.Zone.Right / TrackAIElement.Precision;
                int bottom = aiElem.Zone.Bottom / TrackAIElement.Precision;

                switch (aiElem.ZoneShape)
                {
                    case Shape.Rectangle:
                        for (int y = top; y < bottom; y++)
                        {
                            for (int x = left; x < right; x++)
                            {
                                zones[y][x] = zoneIndex;
                            }
                        }
                        break;

                    case Shape.TriangleTopLeft:
                        for (int y = top; y < bottom; y++)
                        {
                            for (int x = left; x < right; x++)
                            {
                                zones[y][x] = zoneIndex;
                            }
                            right--;
                        }
                        break;

                    case Shape.TriangleTopRight:
                        for (int y = top; y < bottom; y++)
                        {
                            for (int x = left; x < right; x++)
                            {
                                zones[y][x] = zoneIndex;
                            }
                            left++;
                        }
                        break;

                    case Shape.TriangleBottomRight:
                        left = right - 1;
                        for (int y = top; y < bottom; y++)
                        {
                            for (int x = left; x < right; x++)
                            {
                                zones[y][x] = zoneIndex;
                            }
                            left--;
                        }
                        break;

                    case Shape.TriangleBottomLeft:
                        right = left + 1;
                        for (int y = top; y < bottom; y++)
                        {
                            for (int x = left; x < right; x++)
                            {
                                zones[y][x] = zoneIndex;
                            }
                            right++;
                        }
                        break;
                }
            }
        }

        private static byte[][] GetGridFilledFromNearestTiles(sbyte[][] zones)
        {
            byte[][] newZones = new byte[zones.Length][];

            for (int y = 0; y < zones.Length; y++)
            {
                newZones[y] = new byte[zones[y].Length];
                for (int x = 0; x < zones[y].Length; x++)
                {
                    if (zones[y][x] != -1)
                    {
                        newZones[y][x] = (byte)zones[y][x];
                        continue;
                    }

                    int depth = 1;
                    sbyte zoneIndex = -1;
                    while (zoneIndex == -1)
                    {
                        sbyte matchFound;

                        matchFound = TrackObjectZones.GetTopRightNearestTile(zones, x, y, depth);
                        if (matchFound > zoneIndex)
                        {
                            zoneIndex = matchFound;
                        }

                        matchFound = TrackObjectZones.GetBottomRightNearestTile(zones, x, y, depth);
                        if (matchFound > zoneIndex)
                        {
                            zoneIndex = matchFound;
                        }

                        matchFound = TrackObjectZones.GetBottomLeftNearestTile(zones, x, y, depth);
                        if (matchFound > zoneIndex)
                        {
                            zoneIndex = matchFound;
                        }

                        matchFound = TrackObjectZones.GetTopLeftNearestTile(zones, x, y, depth);
                        if (matchFound > zoneIndex)
                        {
                            zoneIndex = matchFound;
                        }

                        depth++;
                    }

                    newZones[y][x] = (byte)zoneIndex;
                }
            }

            return newZones;
        }

        private static sbyte GetTopRightNearestTile(sbyte[][] zones, int x, int y, int depth)
        {
            sbyte matchFound = -1;

            int x2 = x;
            int y2 = y - depth;

            if (y2 < 0)
            {
                x2 -= y2;
                y2 = 0;
            }

            while (x2 <= GridLimit && y2 <= y)
            {
                if (zones[y2][x2] > matchFound)
                {
                    matchFound = (sbyte)zones[y2][x2];
                }

                x2++;
                y2++;
            }

            return matchFound;
        }

        private static sbyte GetBottomRightNearestTile(sbyte[][] zones, int x, int y, int depth)
        {
            sbyte matchFound = -1;

            int x2 = x + depth;
            int y2 = y;

            if (x2 > GridLimit)
            {
                y2 += x2 - GridLimit;
                x2 = GridLimit;
            }

            while (x2 >= x && y2 <= GridLimit)
            {
                if (zones[y2][x2] > matchFound)
                {
                    matchFound = (sbyte)zones[y2][x2];
                }

                x2--;
                y2++;
            }

            return matchFound;
        }

        private static sbyte GetBottomLeftNearestTile(sbyte[][] zones, int x, int y, int depth)
        {
            sbyte matchFound = -1;

            int x2 = x;
            int y2 = y + depth;

            if (y2 > GridLimit)
            {
                x2 -= y2 - GridLimit;
                y2 = GridLimit;
            }

            while (x2 >= 0 && y2 >= y)
            {
                if (zones[y2][x2] > matchFound)
                {
                    matchFound = (sbyte)zones[y2][x2];
                }

                x2--;
                y2--;
            }

            return matchFound;
        }

        private static sbyte GetTopLeftNearestTile(sbyte[][] zones, int x, int y, int depth)
        {
            sbyte matchFound = -1;

            int x2 = x - depth;
            int y2 = y;

            if (x2 < 0)
            {
                y2 += x2;
                x2 = 0;
            }

            while (x2 <= x && y2 >= 0)
            {
                if (zones[y2][x2] > matchFound)
                {
                    matchFound = (sbyte)zones[y2][x2];
                }

                x2++;
                y2--;
            }

            return matchFound;
        }

        /// <summary>
        /// Returns the TrackObjectZones data as a byte array, in the format the SMK ROM expects.
        /// </summary>
        /// <returns>The TrackObjectZones bytes.</returns>
        public byte[] GetBytes()
        {
            byte[] data =
            {
                this.frontZones[0],
                this.frontZones[1],
                this.frontZones[2],
                this.frontZones[3],
                0xFF,
                this.rearZones[0],
                this.rearZones[1],
                this.rearZones[2],
                this.rearZones[3],
                0xFF
            };

            return data;
        }
    }
}
