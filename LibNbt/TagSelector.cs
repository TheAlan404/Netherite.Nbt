using JetBrains.Annotations;

namespace LibNbt {
    /// <summary> Delegate used to skip loading certain tags of an NBT stream/file. 
    /// The callback should return "true" for any tag that should be read, and "false" for any tag that should be skipped. </summary>
    /// <param name="tag"> Tag that is being read. Note that tag's value has not yet been read at this time. </param>
    public delegate bool TagSelector( NbtTag tag );
}