using System;

namespace fNbt.Serialization {
    [AttributeUsage( AttributeTargets.Property, AllowMultiple = false )]
    public sealed class IgnoreOnNullAttribute : Attribute {}
}