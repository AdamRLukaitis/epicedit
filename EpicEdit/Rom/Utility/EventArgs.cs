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

namespace EpicEdit.Rom.Utility
{
    /// <summary>
    /// EventArgs that take one generic parameter.
    /// </summary>
    internal class EventArgs<T> : EventArgs
    {
        public EventArgs(T value)
        {
            this.value = value;
        }

        private readonly T value;
        public T Value => this.value;
    }

    /// <summary>
    /// EventArgs that take two generic parameters.
    /// </summary>
    internal class EventArgs<T, U> : EventArgs
    {
        public EventArgs(T value1, U value2)
        {
            this.value1 = value1;
            this.value2 = value2;
        }

        private readonly T value1;
        public T Value1 => this.value1;

        private readonly U value2;
        public U Value2 => this.value2;
    }
}
