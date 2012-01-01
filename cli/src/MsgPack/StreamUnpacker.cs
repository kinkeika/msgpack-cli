﻿#region -- License Terms --
//
// MessagePack for CLI
//
// Copyright (C) 2010 FUJIWARA, Yusuke
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
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MsgPack
{
	/// <summary>
	///		Stream based unpacker.
	/// </summary>
	internal sealed class StreamUnpacker : Unpacker
	{
		/// <summary>
		///		Default buffer size.
		/// </summary>
		/// <remarks>
		///		This value is subject to change.
		/// </remarks>
		public static readonly int DefaultBufferSize = 1024 * 64;

		/// <summary>
		///		Actual unpackaging strategy.
		/// </summary>
		private readonly StreamingUnpacker _unpacker = new StreamingUnpacker();

		/// <summary>
		///		If current position MAY be in tail of source then true, otherwise false.
		/// </summary>
		/// <remarks>
		///		This value should be refered via <see cref="IsInTailUltimately"/>.
		/// </remarks>
		private bool _mayInTail;

		/// <summary>
		///		Queue of successors of data source.
		/// </summary>
		private readonly Queue<DataSource> _successorSources = new Queue<DataSource>();

		/// <summary>
		///		Current data source.
		/// </summary>
		private DataSource _currentSource;

		/// <summary>
		///		Last unpacked data or null.
		/// </summary>
		private MessagePackObject? _data;

		/// <summary>
		///		Get last unpacked data.
		/// </summary>
		/// <value>
		///		Last unpacked data or null.
		/// </value>
		public sealed override MessagePackObject? Data
		{
			get { return this._data; }
		}
		
		/// <summary>
		///		Gets a value indicating whether this instance is positioned to array header.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is positioned to array header; otherwise, <c>false</c>.
		/// </value>
		public sealed override bool IsArrayHeader
		{
			get { return this._unpacker.IsInArrayHeader; }
		}

		/// <summary>
		///		Gets a value indicating whether this instance is positioned to map header.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is positioned to map header; otherwise, <c>false</c>.
		/// </value>
		public sealed override bool IsMapHeader
		{
			get { return this._unpacker.IsInMapHeader; }
		}

		/// <summary>
		///		Gets the items count for current array or map.
		/// </summary>
		/// <value>
		///		The items count for current array or map.
		/// </value>
		/// <exception cref="InvalidOperationException">
		///		Both of the <see cref="IsArrayHeader"/> and <see cref="IsMapHeader"/> are <c>false</c>.
		/// </exception>
		public sealed override long ItemsCount
		{
			get
			{
				if ( !this.IsArrayHeader && !this.IsMapHeader )
				{
					throw new InvalidOperationException( "This instance is not positioned to Array nor Map header." );
				}

				return this._unpacker.UnpackingItemsCount;
			}
		}

		private bool _isInStart = true;

		/// <summary>
		///		Gets a value indicating whether this instance is in start position.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this instance is in start; otherwise, <c>false</c>.
		/// </value>
		public sealed override bool IsInStart
		{
			get { return this._isInStart; }
		}

		/// <summary>
		///		Gets the underlying stream to handle direct API.
		/// </summary>
		protected sealed override Stream UnderlyingStream
		{
			get { return this._currentSource.Stream; }
		}

		/// <summary>
		///		Initialize new instance with default sized on memory buffer.
		/// </summary>
		public StreamUnpacker() : this( new MemoryStream( DefaultBufferSize ), true ) { }

		/// <summary>
		///		Initialize new instance using specified <see cref="Stream"/> as source.
		///		This instance will have <see cref="Stream"/> ownership.
		/// </summary>
		/// <param name="source">Source <see cref="Stream"/>.</param>
		/// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
		public StreamUnpacker( Stream source ) : this( source, true ) { }

		/// <summary>
		///		Initialize new instance using specified <see cref="Stream"/> as source.
		/// </summary>
		/// <param name="source">Source <see cref="Stream"/>.</param>
		/// <param name="ownsStream">If you want to dispose stream when this instance is disposed, then true.</param>
		/// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
		public StreamUnpacker( Stream source, bool ownsStream )
		{
			if ( source == null )
			{
				throw new ArgumentNullException( "source" );
			}

			this._currentSource = new DataSource( source, ownsStream );
		}

		/// <summary>
		///		Initialize new instance using specified <see cref="Byte"/>[] as source.
		/// </summary>
		/// <param name="initialData">Source <see cref="Byte"/>[].</param>
		/// <exception cref="ArgumentNullException"><paramref name="initialData"/> is null.</exception>
		public StreamUnpacker( byte[] initialData ) : this( initialData, 0, initialData == null ? 0 : initialData.Length ) { }

		/// <summary>
		///		Initialize new instance using specified <see cref="Byte"/>[] as source.
		/// </summary>
		/// <param name="initialData">Source <see cref="Byte"/>[].</param>
		/// <param name="offset">Offset of <paramref name="initialData"/> to copy.</param>
		/// <param name="count">Count of <paramref name="initialData"/> to copy.</param>
		/// <exception cref="ArgumentNullException"><paramref name="initialData"/> is null.</exception>
		public StreamUnpacker( byte[] initialData, int offset, int count )
		{
			Validation.ValidateBuffer( initialData, offset, count, "initialData", "count", true );

			this._currentSource = new DataSource( initialData );
		}

		/// <summary>
		///		Initialize new instance using specified <see cref="IEnumerable&lt;T&gt;">IEnumerable</see>&lt;<see cref="Byte"/>&gt; as source.
		/// </summary>
		/// <param name="source">Source <see cref="IEnumerable&lt;T&gt;">IEnumerable</see>&lt;<see cref="Byte"/>&gt;.</param>
		/// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
		public StreamUnpacker( IEnumerable<byte> source )
		{
			if ( source == null )
			{
				throw new ArgumentNullException( "source" );
			}

			this._currentSource = new DataSource( source );
		}

		/// <summary>
		///		Clean up internal resources.
		/// </summary>
		protected sealed override void Dispose( bool disposing )
		{
			var source = this._currentSource;
			if ( source.Stream != null && source.OwnsStream )
			{
				source.Stream.Dispose();
				this._currentSource = default( DataSource );
			}

			foreach ( var successor in this._successorSources.ToArray() )
			{
				if ( successor.Stream != null && successor.OwnsStream )
				{
					successor.Stream.Dispose();
				}
			}

			this._successorSources.Clear();

			base.Dispose( disposing );
		}

		/// <summary>
		///		Starts unpacking of current subtree.
		/// </summary>
		/// <returns>
		///		<see cref="Unpacker"/> to unpack current subtree.
		///		This will not be <c>null</c>.
		/// </returns>
		protected sealed override Unpacker ReadSubtreeCore()
		{
			return new SubtreeUnpacker( this );
		}

		/// <summary>
		///		Reads next Message Pack entry.
		/// </summary>
		/// <returns>
		///		<c>true</c>, if position is sucessfully move to next entry;
		///		<c>false</c>, if position reaches the tail of the Message Pack stream.
		/// </returns>
		protected sealed override bool ReadCore()
		{
			return this.Read( UnpackingMode.PerEntry );
		}

		/// <summary>
		///		Read subtree item from current stream.
		/// </summary>
		/// <returns>
		///		<c>true</c>, if position is sucessfully move to next entry;
		///		<c>false</c>, if position reaches the tail of the Message Pack stream.
		/// </returns>
		/// <remarks>
		///		This method only be called from <see cref="SubtreeUnpacker"/>.
		/// </remarks>
		internal bool ReadSubtreeItem()
		{
			return this.ReadCore();
		}
		
		private bool Read( UnpackingMode unpackingMode )
		{
			this._isInStart = false;
			while ( !this.IsInTailUltimately() )
			{
				var data = this._unpacker.Unpack( this._currentSource.Stream, unpackingMode );
				if ( data != null )
				{
					this._data = data;
					return true;
				}
				else
				{
					this._mayInTail = true;
				}
			}

			return false;
		}

		// FIXME: Quota

		/// <summary>
		///		Determins this instance is in tail of all data sources.
		///		This method deque successors when needed.
		/// </summary>
		/// <returns>If this instance is in tail of all data sources then true, otherwise false.</returns>
		private bool IsInTailUltimately()
		{
			if ( !this._mayInTail )
			{
				return false;
			}

			if ( this._currentSource.Stream.CanSeek && this._currentSource.Stream.Position < this._currentSource.Stream.Length )
			{
				return false;
			}

			if ( this._successorSources.Count == 0 )
			{
				return true;
			}

			this._currentSource = this._successorSources.Dequeue();
			this._isInStart = true;
			return false;
		}

		/// <summary>
		///		Feeds new data source.
		/// </summary>
		/// <param name="stream">New data source to feed. This will not be <c>null</c>.</param>
		/// <param name="ownsStream">If <paramref name="stream"/> should be disposed in this instance then true.</param>
		protected sealed override void FeedCore( Stream stream, bool ownsStream )
		{
			this._successorSources.Enqueue( new DataSource( stream, ownsStream ) );
		}

		/// <summary>
		///		Encapselates Stream and ownership information.
		/// </summary>
		private struct DataSource
		{
			/// <summary>
			///		Indicates whether this unpacker should <see cref="IDisposable.Dispose"/> <see cref="Stream"/>.
			/// </summary>
			public readonly bool OwnsStream;

			/// <summary>
			///		Underlying stream of this source. This value could be null.
			/// </summary>
			public readonly Stream Stream;

			public DataSource( IEnumerable<byte> source )
			{
				// TODO: more efficient custom stream?
				this.Stream = new MemoryStream( source.ToArray() );
				this.OwnsStream = false;
			}

			public DataSource( Stream stream, bool ownsStream )
			{
				this.Stream = stream;
				this.OwnsStream = ownsStream;
			}
		}
	}
}
