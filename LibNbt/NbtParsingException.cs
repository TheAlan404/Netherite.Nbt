using System;

namespace LibNbt {
    /// <summary> Exception thrown when given stream was not in valid NBT format. </summary>
    [Serializable]
    public sealed class NbtParsingException : Exception {
        internal NbtParsingException( string message )
            : base( message ) { }
    }
}