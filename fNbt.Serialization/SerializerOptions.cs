namespace fNbt.Serialization {
    public class SerializerOptions {
        public NullPolicy DefaultNullPolicy { get; set; }
        public NullPolicy DefaultElementNullPolicy { get; set; }
        public MissingPolicy DefaultMissingPolicy { get; set; }
        public bool IgnoreISerializable { get; set; }
        public string[] ConstructorParameters { get; set; }
        public string[] IgnoredProperties { get; set; }
    }
}