namespace fNbt.Serialization {
    interface INbtSerializable {
        NbtTag Serialize( string tagName );

        void Deserialize( NbtTag tag );
    }
}
