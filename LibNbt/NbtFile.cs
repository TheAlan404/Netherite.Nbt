using System;
using System.IO;
using System.IO.Compression;
using JetBrains.Annotations;
using LibNbt.Queries;
using LibNbt.Tags;

namespace LibNbt {
    public class NbtFile {
        const int BufferSize = 8192;
        const string ZLibNotice = "ZLib compression is not currently supported by LibNbt.";

        [CanBeNull]
        protected string FileName { get; set; }


        protected NbtCompression FileCompression { get; set; }


        [CanBeNull]
        public NbtCompound RootTag { get; set; }


        public NbtFile()
            : this( null, NbtCompression.AutoDetect ) {}


        public NbtFile( [CanBeNull] string fileName, NbtCompression compression ) {
            FileName = fileName;
            FileCompression = compression;
        }


        public virtual void LoadFromFile() {
            if( FileName == null ) {
                throw new NullReferenceException( "FileName is null." );
            }
            LoadFromFile( FileName, FileCompression );
        }


        public virtual void LoadFromFile( [NotNull] string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            LoadFromFile( fileName, NbtCompression.AutoDetect );
        }


        public virtual void LoadFromFile( [NotNull] string fileName, NbtCompression compression ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            if( !File.Exists( fileName ) ) {
                throw new FileNotFoundException( String.Format( "Could not find NBT file: {0}", fileName ),
                                                 fileName );
            }

            FileName = fileName;
            FileCompression = compression;

            using( FileStream readFileStream = File.OpenRead( fileName ) ) {
                LoadFromStream( readFileStream, FileCompression );
            }
        }


        public virtual void LoadFromStream( [NotNull] Stream stream, NbtCompression compression ) {
            if( stream == null ) throw new ArgumentNullException( "stream" );

            // detect compression, based on the first byte
            if( compression == NbtCompression.AutoDetect ) {
                if( !stream.CanSeek ) {
                    throw new NotSupportedException( "Cannot auto-detect compression on a stream that's not seekable." );
                }
                int firstByte = stream.ReadByte();
                switch( firstByte ) {
                    case -1:
                        throw new EndOfStreamException();

                    case (byte)NbtTagType.Compound: // 0x0A
                        compression = NbtCompression.None;
                        break;

                    case 0x1F:
                        // gzip magic number
                        compression = NbtCompression.GZip;
                        break;

                    default:
                        // zlib does not have a "magic number" in its header,
                        // but lower nibble of the first byte should be 0b1000 (8)
                        if( ( firstByte & 0x0F ) == 8 ) {
                            compression = NbtCompression.ZLib;
                        } else {
                            throw new InvalidDataException( "Could not auto-detect compression format." );
                        }
                        break;
                }
                stream.Seek( -1, SeekOrigin.Current );
            }

            switch( compression ) {
                case NbtCompression.GZip:
                    using( var decStream = new GZipStream( stream, CompressionMode.Decompress, true ) ) {
                        LoadFromStreamInternal( new BufferedStream( decStream, BufferSize ) );
                    }
                    break;

                case NbtCompression.None:
                    LoadFromStreamInternal( stream );
                    break;

                case NbtCompression.ZLib:
                    throw new NotImplementedException( ZLibNotice );

                default:
                    throw new ArgumentOutOfRangeException( "compression" );
            }
        }


        protected void LoadFromStreamInternal( [NotNull] Stream fileStream ) {
            if( fileStream == null ) throw new ArgumentNullException( "fileStream" );

            // Make sure the first byte in this file is the tag for a TAG_Compound
            if( fileStream.ReadByte() == (int)NbtTagType.Compound ) {
                var rootCompound = new NbtCompound();
                rootCompound.ReadTag( new NbtReader( fileStream ), true );

                RootTag = rootCompound;
            } else {
                throw new InvalidDataException( "File format does not start with a TAG_Compound" );
            }
        }


        public virtual void SaveToFile( [NotNull] string fileName, NbtCompression compression ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );

            using( FileStream saveFile = File.Create( fileName ) ) {
                SaveToStream( saveFile, compression );
            }
        }


        public virtual void SaveToStream( [NotNull] Stream stream, NbtCompression compression ) {
            if( stream == null ) throw new ArgumentNullException( "stream" );

            switch( compression ) {
                case NbtCompression.AutoDetect:
                    throw new ArgumentException( "AutoDetect is not a valid NbtCompression value for saving." );
                case NbtCompression.ZLib:
                    throw new NotImplementedException( ZLibNotice );
                case NbtCompression.GZip:
                case NbtCompression.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException( "compression" );
            }

            // do not write anything for empty tags
            if( RootTag == null ) return;

            if( compression == NbtCompression.GZip ) {
                using( var compressStream = new GZipStream( stream, CompressionMode.Compress, true ) ) {
                    // use a buffered stream to avoid gzipping in small increments (which has a lot of overhead)
                    BufferedStream bufferedStream = new BufferedStream( compressStream, BufferSize );
                    RootTag.WriteTag( new NbtWriter( bufferedStream ), true );
                    bufferedStream.Flush();
                }
            } else {
                RootTag.WriteTag( new NbtWriter( stream ), true );
            }
        }


        #region Query

        public NbtTag Query( [NotNull] string queryString ) {
            if( queryString == null ) throw new ArgumentNullException( "queryString" );
            return Query<NbtTag>( queryString );
        }


        public T Query<T>( [NotNull] string queryString ) where T : NbtTag {
            if( queryString == null ) throw new ArgumentNullException( "queryString" );
            if( RootTag == null ) return null;
            var tagQuery = new TagQuery( queryString );
            return RootTag.Query<T>( tagQuery );
        }

        #endregion

    }
}