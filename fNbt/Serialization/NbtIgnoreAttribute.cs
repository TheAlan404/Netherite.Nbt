using System;

namespace fNbt.Serialization {
    /// <summary> Presence of this attribute instructs NbtSerializer to always
    /// skip an object's property when serializing/deserializing. </summary>
    [AttributeUsage( AttributeTargets.Property, AllowMultiple = false )]
    public class NbtIgnoreAttribute : Attribute {}
}