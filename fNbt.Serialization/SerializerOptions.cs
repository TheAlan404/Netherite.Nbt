using System;
using System.Linq;

namespace fNbt.Serialization {
    public class SerializerOptions : IEquatable<SerializerOptions> {
        public static SerializerOptions Defaults { get; private set; }
        static SerializerOptions() {
            Defaults = new SerializerOptions();
        }


        public NullPolicy DefaultNullPolicy { get; set; }
        public NullPolicy DefaultElementNullPolicy { get; set; }
        public MissingPolicy DefaultMissingPolicy { get; set; }
        public bool IgnoreISerializable { get; set; }
        public string[] IgnoredProperties { get; set; }

        public SerializerOptions() {
            DefaultNullPolicy = NullPolicy.Error;
            DefaultElementNullPolicy = NullPolicy.Error;
            DefaultMissingPolicy = MissingPolicy.Error;
            IgnoreISerializable = false;
            IgnoredProperties = null;
        }


        public override int GetHashCode() {
            unchecked {
                int hashCode = (int)DefaultNullPolicy;
                hashCode = (hashCode*397) ^ (int)DefaultElementNullPolicy;
                hashCode = (hashCode*397) ^ (int)DefaultMissingPolicy;
                hashCode = (hashCode*397) ^ IgnoreISerializable.GetHashCode();
                hashCode = (hashCode*397) ^ (IgnoredProperties != null ? IgnoredProperties.GetHashCode() : 0);
                return hashCode;
            }
        }

        public bool Equals(SerializerOptions other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return DefaultNullPolicy == other.DefaultNullPolicy &&
                   DefaultElementNullPolicy == other.DefaultElementNullPolicy &&
                   DefaultMissingPolicy == other.DefaultMissingPolicy &&
                   IgnoreISerializable.Equals(other.IgnoreISerializable) &&
                   IgnoredProperties.SequenceEqual(other.IgnoredProperties);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((SerializerOptions)obj);
        }
    }
}