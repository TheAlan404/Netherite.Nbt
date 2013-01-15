using System;

namespace fNbt.Serialization {
    [AttributeUsage( AttributeTargets.Property, Inherited = false, AllowMultiple = false )]
    public sealed class IgnoreOnNullAttribute : Attribute { }
}