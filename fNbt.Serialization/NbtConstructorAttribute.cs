using System;
using JetBrains.Annotations;

namespace fNbt.Serialization {
    public class NbtConstructorAttribute : Attribute {
        public readonly string[] PropertyNames;

        public NbtConstructorAttribute([NotNull] params string[] propertyNames) {
            if (propertyNames == null) throw new ArgumentNullException("propertyNames");
            PropertyNames = propertyNames;
        }
    }
}
