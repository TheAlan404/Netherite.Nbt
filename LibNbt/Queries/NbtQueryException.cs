using System;

namespace LibNbt.Queries {
    public class NbtQueryException : Exception {
        public NbtQueryException( string message ) : base( message ) {}
    }
}