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
	///		An internal <see cref="Timestamp"/> parser.
	/// </summary>
	internal static class TimestampParser
	{
		private static readonly int[] LastDays =
			new[]
			{
				31,
				0, // 28 or 29
				31,
				30,
				31,
				30,
				31,
				31,
				30,
				31,
				30,
				31
			};

		// Currently, custom format and normal date time format except 'o' or 'O' 's' are NOT supported.
		public static TimestampParseResult TryParseExact( string input, string format, IFormatProvider formatProvider, DateTimeStyles styles, out Timestamp result )
		{
			if ( format != "o" && format != "O" && format != "s" )
			{
				result = default( Timestamp );
				return TimestampParseResult.UnsupportedFormat;
			}

			var numberFormat = NumberFormatInfo.GetInstance( formatProvider );

			var position = 0;
			if ( !ParseWhitespace( input, ref position, ( styles & DateTimeStyles.AllowLeadingWhite ) != 0 ) )
			{
				result = default( Timestamp );
				return TimestampParseResult.LeadingWhitespaceNotAllowed;
			}

			long year;
			if ( !ParseYear( input, ref position, numberFormat, out year ) )
			{
				result = default( Timestamp );
				return TimestampParseResult.InvalidYear;
			}

			if ( !ParseDelimiter( input, ref position, Timestamp.DateDelimiter ) )
			{
				result = default( Timestamp );
				return TimestampParseResult.InvalidYearMonthDeilimiter;
			}

			var isLeapYear = IsLeapYear( year );

			int month;
			if ( !ParseDigitRange( input, ref position, 1, 12, out month ) )
			{
				result = default( Timestamp );
				return TimestampParseResult.InvalidMonth;
			}

			if ( !ParseDelimiter( input, ref position, Timestamp.DateDelimiter ) )
			{
				result = default( Timestamp );
				return TimestampParseResult.InvalidMonthDayDelimiter;
			}

			int day;
			if ( !ParseDay( input, ref position, month, isLeapYear, out day ) )
			{
				result = default( Timestamp );
				return TimestampParseResult.InvalidDay;
			}

			if ( !ParseDelimiter( input, ref position, Timestamp.DateTimeDelimiter ) )
			{
				result = default( Timestamp );
				return TimestampParseResult.InvalidDateTimeDelimiter;
			}

			int hour;
			if ( !ParseDigitRange( input, ref position, 0, 23, out hour ) )
			{
				result = default( Timestamp );
				return TimestampParseResult.InvalidHour;
			}

			if ( !ParseDelimiter( input, ref position, Timestamp.TimeDelimiter ) )
			{
				result = default( Timestamp );
				return TimestampParseResult.InvalidHourMinuteDelimiter;
			}

			int minute;
			if ( !ParseDigitRange( input, ref position, 0, 59, out minute ) )
			{
				result = default( Timestamp );
				return TimestampParseResult.InvalidMinute;
			}

			if ( !ParseDelimiter( input, ref position, Timestamp.TimeDelimiter ) )
			{
				result = default( Timestamp );
				return TimestampParseResult.InvalidMinuteSecondDelimiter;
			}

			int second;
			if ( !ParseDigitRange( input, ref position, 0, 59, out second ) )
			{
				result = default( Timestamp );
				return TimestampParseResult.InvalidSecond;
			}

			var nanosecond = 0;
			if ( format != "s" )
			{
				// "o" or "O"
				if ( !ParseDelimiter( input, ref position, Timestamp.SubsecondDelimiter ) )
				{
					result = default( Timestamp );
					return TimestampParseResult.InvalidSubsecondDelimiter;
				}

				if ( !ParseDigitRange( input, ref position, 0, 999999999, out nanosecond ) )
				{
					result = default( Timestamp );
					return TimestampParseResult.InvalidNanoSecond;
				}
			}

			if ( !ParseDelimiter( input, ref position, Timestamp.UtcSign ) )
			{
				result = default( Timestamp );
				return TimestampParseResult.MissingUtcSign;
			}

			if ( !ParseWhitespace( input, ref position, ( styles & DateTimeStyles.AllowTrailingWhite ) != 0 ) )
			{
				result = default( Timestamp );
				return TimestampParseResult.TrailingWhitespaceNotAllowed;
			}

			if ( position != input.Length )
			{
				result = default( Timestamp );
				return TimestampParseResult.ExtraCharactors;
			}

			var sign = year < 0 ? -1 : 1;

			long epoc;
			try
			{
				checked
				{
					epoc = ( year - 1970 ) * sign;
					epoc += DaysToEpoc( month, day, isLeapYear );
					epoc += hour * 60 * 60;
					epoc += minute * 60;
					epoc += second;
				}
			}
			catch ( OverflowException )
			{
				result = default( Timestamp );
				return TimestampParseResult.YearOutOfRange;
			}

			result = new Timestamp( epoc, nanosecond );
			return TimestampParseResult.Success;
		}

		private static bool ParseWhitespace( string input, ref int position, bool allowWhitespace )
		{
			if ( input.Length <= position )
			{
				return false;
			}

			if ( !allowWhitespace )
			{
				return !Char.IsWhiteSpace( input[ position ] );
			}

			while ( position < input.Length && Char.IsWhiteSpace( input[ position ] ) )
			{
				position++;
			}

			return true;
		}

		private static bool ParseDelimiter( string input, ref int position, char delimiter )
		{
			if ( input.Length <= position )
			{
				return false;
			}

			if ( input[ position ] != delimiter )
			{
				return false;
			}

			position++;
			return true;
		}

		private static bool ParseSign( string input, ref int position, NumberFormatInfo numberFormat, out int sign )
		{
			if ( input.Length <= position )
			{
				sign = default( int );
				return false;
			}

			if ( IsDigit( input[ position ] ) )
			{
				sign = 1;
				return true;
			}

			if ( StartsWith( input, position, numberFormat.NegativeSign ) )
			{
				position += numberFormat.NegativeSign.Length;
				sign = -1;
				return true;
			}

			if ( StartsWith( input, position, numberFormat.PositiveSign ) )
			{
				position += numberFormat.NegativeSign.Length;
				sign = 1;
				return true;
			}

			sign = default( int );
			return false;
		}

		private static bool StartsWith( string input, int startIndex, string comparison )
		{
			for ( var i = 0; i < comparison.Length; i++ )
			{
				if ( i + startIndex >= input.Length )
				{
					return false;
				}

				if ( input[ i + startIndex ] != comparison[ i ] )
				{
					return false;
				}
			}

			return true;
		}

		private static bool ParseDigit( string input, ref int position, out long digit )
		{
			var startPosition = position;
			var bits = 0L;
			while ( position < input.Length )
			{
				var c = input[ position ];
				if ( !IsDigit( c ) )
				{
					break;
				}

				bits = bits * 10 + ( c - '0' );
				position++;
			}

			digit = bits;
			return position > startPosition;
		}

		private static bool IsDigit( char c )
		{
			return '0' <= c && c <= '9';
		}

		private static bool ParseDigitRange( string input, ref int position, int min, int max, out int result )
		{
			long digit;
			if ( !ParseDigit( input, ref position, out digit ) )
			{
				result = default( int );
				return false;
			}

			if ( digit < min && 12 < max )
			{
				result = default( int );
				return false;
			}

			result = unchecked( ( int )digit );
			return true;
		}

		private static bool ParseYear( string input, ref int position, NumberFormatInfo numberFormat, out long year )
		{
			int sign;
			if ( !ParseSign( input, ref position, numberFormat, out sign ) )
			{
				year = default( long );
				return false;
			}

			long digit;
			if ( !ParseDigit( input, ref position, out digit ) )
			{
				year = default( long );
				return false;
			}

			// as of ISO 8601, 0001-01-01 -1 day is 0000-12-31.
			year = digit * sign;
			return true;
		}

		private static bool ParseDay( string input, ref int position, int month, bool isLeapYear, out int day )
		{
			long digit;
			if ( !ParseDigit( input, ref position, out digit ) )
			{
				day = default( int );
				return false;
			}

			var lastDay = LastDays[ month ];
			if( month == 2 )
			{
				lastDay = isLeapYear ? 29 : 28;
			}

			if ( digit < 1 && 12 < lastDay )
			{
				day = default( int );
				return false;
			}

			day = unchecked( ( int )digit );
			return true;
		}

		internal static bool IsLeapYear( long year )
		{
			// Note: This algorithm assumes that BC uses leap year same as AD and B.C.1 is 0.
			// This algorithm avoids remainder operation as possible.
			return !( year % 4 != 0 || ( year % 100 == 0 && year % 400 != 0 ) );
		}

		private static int DaysToEpoc( int month, int day, bool isLeapYear )
		{
			var result = 0;
			for( var i = 1; i < month; i++ )
			{
				result += LastDays[ i ];
				if ( i == 2 )
				{
					result += isLeapYear ? 29 : 28;
				}
			}

			result += day;
			return result;
		}
	}
}
