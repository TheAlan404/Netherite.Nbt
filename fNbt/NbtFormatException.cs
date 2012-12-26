using System;

namespace fNbt {
    /// <summary> Exception thrown when a format violation is detected while parsing or saving an NBT file. </summary>
    [Serializable]
    public sealed class NbtFormatException : Exception {
        internal NbtFormatException( string message )
            : base( message ) {}
    }
}