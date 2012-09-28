using System;

namespace LibNbt.Queries {
    [Serializable]
    public sealed class NbtQueryException : Exception {
        internal NbtQueryException( string message )
            : base( message ) {}
    }
}