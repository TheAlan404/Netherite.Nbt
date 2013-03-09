using JetBrains.Annotations;

namespace fNbt.Serialization {
    /// <summary> Represents a type that handles serialization to/from NBT representation using custom methods. </summary>
    public interface INbtSerializable {
        /// <summary> Converts this object to NBT representation. </summary>
        /// <param name="tagName"> Name to assign to the produced tag. May be null. </param>
        /// <returns> NbtTag representation of this object. Must not be null. </returns>
        [NotNull]
        NbtTag Serialize( [CanBeNull] string tagName );

        /// <summary> Configures this object's state based on given NbtTag. </summary>
        /// <param name="tag"> NBT tag from which state should be loaded. </param>
        void Deserialize( [NotNull] NbtTag tag );
    }
}
