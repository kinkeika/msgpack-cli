#region -- License Terms --
//
// MessagePack for CLI
//
// Copyright (C) 2010-2015 FUJIWARA, Yusuke
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//
#endregion -- License Terms --

using System;
using System.Globalization;

namespace MsgPack
{
	/// <summary>
	///		Represents high resolution timestamp for MessagePack eco-system.
	/// </summary>
	/// <remarks>
	///     The <c>timestamp</c> consists of 64bit Unix epoc seconds and 32bit unsigned nanoseconds offset from the calculated datetime with the epoc.
	///     So this type supports wider range than <see cref="System.DateTime" /> and <see cref="System.DateTimeOffset" /> and supports 1 or 10 nano seconds precision.
	///     However, this type does not support local date time and time zone information, so this type always represents UTC time.
	/// </remarks>
#if FEATURE_BINARY_SERIALIZATION
    [Serializable]
#endif // FEATURE_BINARY_SERIALIZATION
	public partial struct Timestamp
	{
		/// <summary>
		///		MessagePack ext type code for msgpack timestamp type.
		/// </summary>
		public const byte TypeCode = 0xFF;

		/// <summary>
		///		An instance represents zero. This is 1970-01-01T00:00:00.000000000.
		/// </summary>
		public static readonly Timestamp Zero = new Timestamp( 0, 0 );

		/// <summary>
		///		An instance represents minimum value of this instance. This is <c>[<see cref="Int64.MinValue"/>, 0]</c> in encoded format.
		/// </summary>
		public static readonly Timestamp MinValue = new Timestamp( Int64.MinValue, 0 );

		/// <summary>
		///		An instance represents maximum value of this instance. This is <c>[<see cref="Int64.MaxValue"/>, 999999999]</c> in encoded format.
		/// </summary>
		public static readonly Timestamp MaxValue = new Timestamp( Int64.MaxValue, MaxNanoSeconds );

		private const long MinUnixEpochSecondsForTicks = -62135596800L;
		private const long MaxUnixEpochSecondsForTicks = 253402300799;

		private const int MaxNanoSeconds = 999999999;

		private const long UnixEpochTicks = 621355968000000000;
		private const long UnixEpochInSeconds = 62135596800;
		private const int SecondsToTicks = 10 * 1000 * 1000;
		private const int NanoToTicks = 100;
		private const int SecondsToNanos = 1000 * 1000 * 1000;

		private readonly long unixEpochSeconds;
		private readonly uint nanoseconds; // 0 - 999,999,999

		/// <summary>
		///		Initializes a new instance of <see cref="Timestamp"/> structure.
		/// </summary>
		/// <param name="unixEpochSeconds">A unit epoc seconds part of the msgpack timestamp.</param>
		/// <param name="nanoseconds">A unit nanoseconds part of the msgpack timestamp.</param>
		/// <exception cref="ArgumentOutOfRangeException">
		///		<paramref name="nanoseconds"/> is negative or is greater than <c>999,999,999</c> exclusive.
		/// </exception>
		public Timestamp( long unixEpochSeconds, int nanoseconds )
		{
			if ( nanoseconds > MaxNanoSeconds || nanoseconds < 0 )
			{
				throw new ArgumentOutOfRangeException( "nanoseconds", "nanoseconds must be non negative value and lessor than 999,999,999." );
			}

			this.unixEpochSeconds = unixEpochSeconds;
			this.nanoseconds = unchecked( ( uint )nanoseconds );
		}
	}
}
