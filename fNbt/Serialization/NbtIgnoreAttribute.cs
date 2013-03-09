using System;

namespace fNbt.Serialization {
    [AttributeUsage( AttributeTargets.Property, AllowMultiple = false )]
    public class NbtIgnoreAttribute : Attribute {}
}