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
#if !NET35
using System.Numerics;
#endif // !NET35

namespace MsgPack
{
	partial struct Timestamp
	{
#if !NET35
		private static readonly BigInteger NanoToSecondsAsBigInteger = new BigInteger( 1000 * 1000 * 1000 );
#endif // !NET35

		/// <summary>
		///		Adds a specified <see cref="TimeSpan"/> to this instance.
		/// </summary>
		/// <param name="offset">A <see cref="TimeSpan"/> which represents offset. Note that this value can be negative.</param>
		/// <returns>The result <see cref="Timestamp"/>.</returns>
		/// <exception cref="OverflowException">
		///		The result of calculation overflows <see cref="MaxValue"/> or underflows <see cref="MinValue"/>.
		/// </exception>
		public Timestamp Add( TimeSpan offset )
		{
			long secondsOffset;
			int nanosOffset;
			FromTicks( offset.Ticks, out secondsOffset, out nanosOffset );
			var seconds = checked( this.unixEpochSeconds + secondsOffset );
			var nanos = this.nanoseconds + nanosOffset;
			if ( nanos > MaxNanoSeconds )
			{
				checked
				{
					seconds++;
				}
				nanos -= ( MaxNanoSeconds + 1 );
			}
			else if ( nanos < 0 )
			{
				checked
				{
					seconds--;
				}
				nanos = ( MaxNanoSeconds + 1 ) + nanos;
			}

			return new Timestamp( seconds, unchecked(( int )nanos) );
		}

		/// <summary>
		///		Subtracts a specified <see cref="TimeSpan"/> from this instance.
		/// </summary>
		/// <param name="offset">A <see cref="TimeSpan"/> which represents offset. Note that this value can be negative.</param>
		/// <returns>The result <see cref="Timestamp"/>.</returns>
		/// <exception cref="OverflowException">
		///		The result of calculation overflows <see cref="MaxValue"/> or underflows <see cref="MinValue"/>.
		/// </exception>
		public Timestamp Subtract( TimeSpan offset )
		{
			return this.Add( -offset );
		}

#if !NET35
		/// <summary>
		///		Adds a specified nanoseconds represented as a <see cref="BigInteger"/> from this instance.
		/// </summary>
		/// <param name="offsetNanoseconds">A <see cref="BigInteger"/> which represents offset. Note that this value can be negative.</param>
		/// <returns>The result <see cref="Timestamp"/>.</returns>
		/// <exception cref="OverflowException">
		///		The result of calculation overflows <see cref="MaxValue"/> or underflows <see cref="MinValue"/>.
		/// </exception>
		public Timestamp Add( BigInteger offsetNanoseconds )
		{
			BigInteger nanosecondsOffset;
			var secondsOffset = ( long )BigInteger.DivRem( offsetNanoseconds, NanoToSecondsAsBigInteger, out nanosecondsOffset );

			var seconds = checked( this.unixEpochSeconds + secondsOffset );
			var nanos = this.nanoseconds + unchecked( ( int )nanosecondsOffset );
			if ( nanos > MaxNanoSeconds )
			{
				checked
				{
					seconds++;
				}
				nanos -= ( MaxNanoSeconds + 1 );
			}
			else if ( nanos < 0 )
			{
				checked
				{
					seconds--;
				}
				nanos = ( MaxNanoSeconds + 1 ) + nanos;
			}

			return new Timestamp( seconds, unchecked(( int )nanos) );
		}

		/// <summary>
		///		Subtracts a specified nanoseconds represented as a <see cref="BigInteger"/> from this instance.
		/// </summary>
		/// <param name="offsetNanoseconds">A <see cref="BigInteger"/> which represents offset. Note that this value can be negative.</param>
		/// <returns>The result <see cref="Timestamp"/>.</returns>
		/// <exception cref="OverflowException">
		///		The result of calculation overflows <see cref="MaxValue"/> or underflows <see cref="MinValue"/>.
		/// </exception>
		public Timestamp Subtract( BigInteger offsetNanoseconds )
		{
			return this.Add( -offsetNanoseconds );
		}

		/// <summary>
		///		Calculates a difference this instance and a specified <see cref="Timestamp"/> in nanoseconds.
		/// </summary>
		/// <param name="other">A <see cref="Timestamp"/> to be differentiated.</param>
		/// <returns>A <see cref="BigInteger"/> which represents difference in nanoseconds.</returns>
		public BigInteger Subtract( Timestamp other )
		{
			var seconds = new BigInteger( this.unixEpochSeconds ) - other.unixEpochSeconds;
			var nanos = this.nanoseconds + ( MaxNanoSeconds + 1 - other.nanoseconds );
			if ( nanos > MaxNanoSeconds )
			{
				seconds++;
				nanos -= ( MaxNanoSeconds + 1 );
			}
			else if ( nanos < 0 )
			{
				seconds--;
				nanos = ( MaxNanoSeconds + 1 ) + nanos;
			}

			return seconds * SecondsToNanos + nanos;
		}
#endif // !NET35

		/// <summary>
		///		Calculates a <see cref="Timestamp"/> with specified <see cref="Timestamp"/> and an offset represented as <see cref="TimeSpan"/>.
		/// </summary>
		/// <param name="value">A <see cref="Timestamp"/>.</param>
		/// <param name="offset">An offset in <see cref="TimeSpan"/>. This value can be negative.</param>
		/// <returns>A <see cref="Timestamp"/>.</returns>
		public static Timestamp operator +( Timestamp value, TimeSpan offset )
		{
			return value.Add( offset );
		}

		/// <summary>
		///		Calculates a <see cref="Timestamp"/> with specified <see cref="Timestamp"/> and an offset represented as <see cref="TimeSpan"/>.
		/// </summary>
		/// <param name="value">A <see cref="Timestamp"/>.</param>
		/// <param name="offset">An offset in <see cref="TimeSpan"/>. This value can be negative.</param>
		/// <returns>A <see cref="Timestamp"/>.</returns>
		public static Timestamp operator -( Timestamp value, TimeSpan offset )
		{
			return value.Subtract( offset );
		}

#if !NET35

		/// <summary>
		///		Calculates a <see cref="Timestamp"/> with specified <see cref="Timestamp"/> and a nanoseconds offset represented as <see cref="BigInteger"/>.
		/// </summary>
		/// <param name="value">A <see cref="Timestamp"/>.</param>
		/// <param name="offsetNanoseconds">An offset in nanoseconds as <see cref="BigInteger"/>. This value can be negative.</param>
		/// <returns>A <see cref="Timestamp"/>.</returns>
		public static Timestamp operator +( Timestamp value, BigInteger offsetNanoseconds )
		{
			return value.Add( offsetNanoseconds );
		}

		/// <summary>
		///		Calculates a <see cref="Timestamp"/> with specified <see cref="Timestamp"/> and a nanoseconds represented as <see cref="BigInteger"/>.
		/// </summary>
		/// <param name="value">A <see cref="Timestamp"/>.</param>
		/// <param name="offsetNanoseconds">An offset in nanoseconds as <see cref="BigInteger"/>. This value can be negative.</param>
		/// <returns>A <see cref="Timestamp"/>.</returns>
		public static Timestamp operator -( Timestamp value, BigInteger offsetNanoseconds )
		{
			return value.Subtract( offsetNanoseconds );
		}
		
		/// <summary>
		///		Calculates a difference between specified two <see cref="Timestamp"/>s in nanoseconds.
		/// </summary>
		/// <param name="left">A <see cref="Timestamp"/>.</param>
		/// <param name="right">A <see cref="Timestamp"/>.</param>
		/// <returns>A <see cref="BigInteger"/> which represents difference in nanoseconds.</returns>
		public static BigInteger operator -( Timestamp left, Timestamp right )
		{
			return left.Subtract( right );
		}
#endif // !NET35
	}
}
