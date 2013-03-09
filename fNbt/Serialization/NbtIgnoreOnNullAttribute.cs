using System;

namespace fNbt.Serialization {
    /// <summary> Presence of this attribute instructs NbtSerializer to skip an object's property if its value is null.
    /// By default, NbtSerializer adds an empty NbtCompound tag for null values instead. </summary>
    [AttributeUsage( AttributeTargets.Property, AllowMultiple = false )]
    public sealed class NbtIgnoreOnNullAttribute : Attribute {}
}