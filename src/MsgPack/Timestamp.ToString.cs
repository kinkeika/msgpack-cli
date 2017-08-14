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
using System.Text;

namespace MsgPack
{
	partial struct Timestamp : IFormattable
	{
		private const string DefaultFormat = "o";

		/// <summary>
		///		Returns a <see cref="String"/> representation of this instance with the default format and the default format provider.
		/// </summary>
		/// <returns>
		///		A <see cref="String"/> representation of this instance.
		/// </returns>
		/// <remarks>
		///		<para>
		///			As of recommendation of the msgpack specification and consistency with <see cref="DateTime"/> and <see cref="DateTimeOffset"/>,
		///			this overload uses <c>"o"</c> for the <c>format</c> parameter and <c>null</c> for <c>formatProvider</c> parameter.
		///		</para>
		///		<para>
		///			The round trip format is <c>yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffffff'Z'</c> which 'fffffffff' nanoseconds. 
		///		</para>
		/// </remarks>
		public override string ToString()
		{
			return this.ToString( DefaultFormat, null );
		}

		/// <summary>
		///		Returns a <see cref="String"/> representation of this instance with the default format and the specified format provider.
		/// </summary>
		/// <param name="formatProvider">
		///		An <see cref="IFormatProvider"/> to provide culture specific format information.
		///		You can specify <c>null</c> for default behavior, which uses <see cref="CultureInfo.CurrentCulture"/>.
		///	</param>
		/// <returns>
		///		A <see cref="String"/> representation of this instance.
		/// </returns>
		/// <remarks>
		///		<para>
		///			As of recommendation of the msgpack specification and consistency with <see cref="DateTime"/> and <see cref="DateTimeOffset"/>,
		///			this overload uses <c>"o"</c> for <c>format</c> parameter.
		///		</para>
		///		<para>
		///			The round trip format is <c>yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffffff'Z'</c> which 'fffffffff' nanoseconds. 
		///		</para>
		/// </remarks>
		public string ToString( IFormatProvider formatProvider )
		{
			return this.ToString( DefaultFormat, formatProvider );
		}

		/// <summary>
		///		Returns a <see cref="String"/> representation of this instance with the specified format and the default format provider.
		/// </summary>
		/// <param name="format">
		///		A format string to specify output format. You can specify <c>null</c> for default behavior, which is interpreted as <c>"o"</c>.
		/// </param>
		/// <returns>
		///		A <see cref="String"/> representation of this instance.
		/// </returns>
		/// <remarks>
		///		<para>
		///			Currently, only <c>"o"</c> and <c>"O"</c> (ISO 8601 like round trip format) and <c>"s"</c> (ISO 8601 format) are supported.
		///			Other standard date time format and any custom date time format are not supported.
		///		</para>
		///		<para>
		///			The round trip format is <c>yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffffff'Z'</c> which 'fffffffff' nanoseconds. 
		///		</para>
		///		<para>
		///			As of recommendation of the msgpack specification and consistency with <see cref="DateTime"/> and <see cref="DateTimeOffset"/>,
		///			this overload uses <c>null</c> for <c>formatProvider</c> parameter.
		///		</para>
		/// </remarks>
		public string ToString( string format )
		{
			return this.ToString( format, null );
		}

		/// <summary>
		///		Returns a <see cref="String"/> representation of this instance with the default format and the specified format provider.
		/// </summary>
		/// <param name="format">
		///		A format string to specify output format. You can specify <c>null</c> for default behavior, which is interpreted as <c>"o"</c>.
		/// </param>
		/// <param name="formatProvider">
		///		An <see cref="IFormatProvider"/> to provide culture specific format information.
		///		You can specify <c>null</c> for default behavior, which uses <see cref="CultureInfo.CurrentCulture"/>.
		///	</param>
		/// <returns>
		///		A <see cref="String"/> representation of this instance.
		/// </returns>
		/// <exception cref="ArgumentException">
		///		<paramref name="format"/> is not valid.
		/// </exception>
		/// <remarks>
		///		<para>
		///			Currently, only <c>"o"</c> and <c>"O"</c> (ISO 8601 like round trip format) and <c>"s"</c> (ISO 8601 format) are supported.
		///			Other standard date time format and any custom date time format are not supported.
		///		</para>
		///		<para>
		///			The round trip format is <c>yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffffff'Z'</c> which 'fffffffff' nanoseconds. 
		///		</para>
		///		<para>
		///			As of recommendation of the msgpack specification and consistency with <see cref="DateTime"/> and <see cref="DateTimeOffset"/>,
		///			the default value of the <paramref name="format"/> is <c>"o"</c> (ISO 8601 like round-trip format)
		///			and the default value of the <paramref name="formatProvider"/> is <c>null</c> (<see cref="CultureInfo.CurrentCulture"/>.
		///			If you want to ensure interoperability for other implementation, specify <c>"s"</c> and <see cref="CultureInfo.InvariantCulture"/> resepectively.
		///		</para>
		/// </remarks>
		public string ToString( string format, IFormatProvider formatProvider )
		{
			switch( format ?? DefaultFormat )
			{
				case "o":
				case "O":
				{
					// round-trip
					return this.ToIso8601String( formatProvider, /* containsNanoseconds */true );
				}
				case "s":
				{
					// sortable(ISO-8601)
					return this.ToIso8601String( formatProvider, /* containsNanoseconds */false );
				}
				default:
				{
					throw new ArgumentException( "The specified format is not supported.", "format" );
				}
			}
		}

		private string ToIso8601String( IFormatProvider formatProvider, bool containsNanosecons)
		{
			var numberFormat = NumberFormatInfo.GetInstance( formatProvider );

			var buffer = new StringBuilder();
			if ( this.unixEpochSeconds < 0 )
			{
				buffer.Append( numberFormat.NegativeSign );
			}

			buffer.Append( this.Year );
			buffer.Append( DateDelimiter );
			buffer.Append( this.Month );
			buffer.Append( DateDelimiter );
			buffer.Append( this.Day );
			buffer.Append( DateTimeDelimiter );
			buffer.Append( this.Hour );
			buffer.Append( TimeDelimiter );
			buffer.Append( this.Minute );
			buffer.Append( TimeDelimiter );
			buffer.Append( this.Second );

			if ( containsNanosecons )
			{
				buffer.Append( SubsecondDelimiter );
				buffer.Append( this.nanoseconds.ToString( "000000000", formatProvider ) );
			}

			return buffer.ToString();
		}
	}
}
