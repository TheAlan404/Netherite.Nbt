namespace fNbt.Serialization {
    public class SerializerOptions {
        public static SerializerOptions Defaults { get; private set; }

        public NullPolicy DefaultNullPolicy { get; set; }
        public NullPolicy DefaultElementNullPolicy { get; set; }
        public MissingPolicy DefaultMissingPolicy { get; set; }
        public bool IgnoreISerializable { get; set; }
        public string[] ConstructorParameters { get; set; }
        public string[] IgnoredProperties { get; set; }
    }
}