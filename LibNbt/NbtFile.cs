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


        public virtual void LoadFile() {
            if( FileName == null ) {
                throw new NullReferenceException( "FileName is null." );
            }
            LoadFile( FileName, FileCompression );
        }


        public virtual void LoadFile( [NotNull] string fileName ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            LoadFile( fileName, NbtCompression.AutoDetect );
        }


        public virtual void LoadFile( [NotNull] string fileName, NbtCompression compression ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );
            if( !File.Exists( fileName ) ) {
                throw new FileNotFoundException( String.Format( "Could not find NBT file: {0}", fileName ),
                                                 fileName );
            }

            FileName = fileName;
            FileCompression = compression;

            using( FileStream readFileStream = File.OpenRead( fileName ) ) {
                LoadFile( readFileStream, FileCompression );
            }
        }


        public virtual void LoadFile( [NotNull] Stream fileStream, NbtCompression compression ) {
            if( fileStream == null ) throw new ArgumentNullException( "fileStream" );

            // detect compression, based on the first byte
            if( compression == NbtCompression.AutoDetect ) {
                int firstByte = fileStream.ReadByte();
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
                fileStream.Seek( -1, SeekOrigin.Current );
            }

            switch( compression ) {
                case NbtCompression.GZip:
                    using( var decStream = new GZipStream( fileStream, CompressionMode.Decompress ) ) {
                        using( var memStream = new MemoryStream( (int)fileStream.Length ) ) {
                            var buffer = new byte[4096];
                            int bytesRead;
                            while( ( bytesRead = decStream.Read( buffer, 0, buffer.Length ) ) != 0 ) {
                                memStream.Write( buffer, 0, bytesRead );
                            }

                            LoadFileInternal( memStream );
                        }
                    }
                    break;

                case NbtCompression.None:
                    LoadFileInternal( fileStream );
                    break;

                case NbtCompression.ZLib:
                    throw new NotImplementedException( ZLibNotice );

                default:
                    throw new ArgumentOutOfRangeException( "compression" );
            }
        }


        protected void LoadFileInternal( [NotNull] Stream fileStream ) {
            if( fileStream == null ) throw new ArgumentNullException( "fileStream" );

            // Make sure the stream is at the beginning
            fileStream.Seek( 0, SeekOrigin.Begin );

            // Make sure the first byte in this file is the tag for a TAG_Compound
            if( fileStream.ReadByte() == (int)NbtTagType.Compound ) {
                var rootCompound = new NbtCompound();
                rootCompound.ReadTag( fileStream );

                RootTag = rootCompound;
            } else {
                throw new InvalidDataException( "File format does not start with a TAG_Compound" );
            }
        }


        public virtual void SaveFile( [NotNull] string fileName, NbtCompression compression ) {
            if( fileName == null ) throw new ArgumentNullException( "fileName" );

            using( FileStream saveFile = File.Create( fileName ) ) {
                SaveFile( saveFile, compression );
            }
        }


        public virtual void SaveFile( [NotNull] Stream fileStream, NbtCompression compression ) {
            if( fileStream == null ) throw new ArgumentNullException( "fileStream" );

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
                using( var compressStream = new GZipStream( fileStream, CompressionMode.Compress ) ) {
                    // use a buffered stream to avoid gzipping in small increments (which has a lot of overhead)
                    using( BufferedStream bs = new BufferedStream( compressStream, BufferSize ) ) {
                        RootTag.WriteTag( bs );
                    }
                }
            } else {
                RootTag.WriteTag( fileStream );
            }
        }


        public NbtTag Query( [NotNull] string query ) {
            if( query == null ) throw new ArgumentNullException( "query" );
            return Query<NbtTag>( query );
        }


        public T Query<T>( [NotNull] string query ) where T : NbtTag {
            if( query == null ) throw new ArgumentNullException( "query" );
            if( RootTag == null ) return null;
            var tagQuery = new TagQuery( query );
            return RootTag.Query<T>( tagQuery );
        }
    }
}