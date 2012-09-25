using System;

namespace LibNbt.Exceptions {
    public class NbtQueryException : Exception {
        public NbtQueryException( string message ) : base( message ) {}
    }
}