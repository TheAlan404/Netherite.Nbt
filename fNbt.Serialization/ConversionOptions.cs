using System;
using System.Linq;

namespace fNbt.Serialization {
    public class ConversionOptions : IEquatable<ConversionOptions> {
        public static ConversionOptions Defaults { get; private set; }
        static ConversionOptions() {
            Defaults = new ConversionOptions();
        }
        
        public NullPolicy SelfNullPolicy { get; set; }
        public NullPolicy DefaultNullPolicy { get; set; }
        public NullPolicy DefaultElementNullPolicy { get; set; }
        public MissingPolicy DefaultMissingPolicy { get; set; }
        public bool IgnoreISerializable { get; set; }
        public string[] IgnoredProperties { get; set; }
    }
}