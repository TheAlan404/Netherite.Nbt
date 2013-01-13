using System.Diagnostics;
using System.IO;
using NUnit.Framework;

namespace fNbt.Test {
    [TestFixture]
    public sealed class NbtReaderTests {
        [Test]
        public void ReadBigFileUncompressed() {
            using( FileStream fs = File.OpenRead( "TestFiles/bigtest.nbt" ) ) {
                NbtReader reader = new NbtReader( fs );
                reader.SkipEndTags = false;
                while( reader.ReadToFollowing() ) {
                    Debug.Write( new string( '\t', reader.Depth ) );
                    Debug.Write( "#" + reader.TagsRead + ". " + reader.TagType );
                    if( reader.HasLength ) {
                        Debug.Write( "[" + reader.TagLength + "]" );
                    }
                    Debug.Write( "\t" + reader.TagName );
                    if( reader.HasValue ) {
                        Debug.Write( " = " + reader.ReadValue() );
                    }
                    Debug.WriteLine( "" );
                }
            }
        }
    }
}