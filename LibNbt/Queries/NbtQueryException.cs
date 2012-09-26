using System;

namespace LibNbt.Queries {
    [Serializable]
    public class NbtQueryException : Exception {
        public NbtQueryException( string message ) : base( message ) {}
    }
}