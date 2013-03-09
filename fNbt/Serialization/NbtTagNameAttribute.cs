using System;

namespace fNbt.Serialization {
    /// <summary> Overrides NBT tag name associated with a property, as read/written by NbtSerializer. </summary>
    [AttributeUsage( AttributeTargets.Property, AllowMultiple = false )]
    public class TagNameAttribute : Attribute {
        /// <summary> NBT tag name associated with this property. </summary>
        public string Name { get; private set; }


        /// <summary> Decorates the given property or field with the specified NBT tag name. </summary>
        public TagNameAttribute( string name ) {
            Name = name;
        }
    }
}