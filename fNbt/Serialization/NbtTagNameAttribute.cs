using System;

namespace fNbt.Serialization {
    [AttributeUsage( AttributeTargets.Property, AllowMultiple = false )]
    public class TagNameAttribute : Attribute {
        public string Name { get; set; }


        /// <summary> Decorates the given property or field with the specified NBT tag name. </summary>
        public TagNameAttribute( string name ) {
            Name = name;
        }
    }
}