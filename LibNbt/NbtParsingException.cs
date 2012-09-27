using System;

namespace LibNbt {
    [Serializable]
    public class NbtParsingException : Exception {
        public NbtParsingException( string message )
            : base( message ) { }
    }
}