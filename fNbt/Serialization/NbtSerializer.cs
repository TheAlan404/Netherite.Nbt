using System;
using System.Collections;
using System.Reflection;
using JetBrains.Annotations;

namespace fNbt.Serialization {
    /// <summary> Rudimentary Nbt serializer. Provides functionality to convert whole objects to/from Nbt format,
    /// using reflection to map object properties to Nbt compound fields. 
    /// Does not support lists or arrays of non-value types, multi-dimensional arrays, nested lists, or nested compounds. </summary>
    public class NbtSerializer {
        /// <summary> Converts given object into NbtTag representation. </summary>
        /// <param name="value"> Object to serialize. May be null. Null values get serialized to an empty Compound tag. </param>
        /// <param name="tagName"> Name of the object. May be null. </param>
        /// <returns> NbtTag representing the given value. </returns>
        /// <exception cref="NotSupportedException"> If serializing objects of the given type is not supported. </exception>
        [NotNull]
        public virtual NbtTag Serialize( [CanBeNull] object value, [CanBeNull] string tagName ) {
            if( value == null ) {
                return new NbtCompound( tagName );
            }
            Type type = value.GetType();

            // custom serialization
            INbtSerializable serializable = value as INbtSerializable;
            if( serializable != null ) {
                return serializable.Serialize( tagName );
            }

            // serialize primitive types
            if( type.IsPrimitive ) {
                if( value is bool ) {
                    return new NbtByte( tagName, (byte)( (bool)value ? 1 : 0 ) );
                } else if( value is byte || value is sbyte ) {
                    return new NbtByte( tagName, (byte)value );
                } else if( value is short || value is ushort || value is char ) {
                    return new NbtShort( tagName, (short)value );
                } else if( value is int || value is uint ) {
                    return new NbtInt( tagName, (int)value );
                } else if( value is long || value is ulong ) {
                    return new NbtLong( tagName, (long)value );
                } else if( value is float ) {
                    return new NbtFloat( tagName, (float)value );
                } else if( value is double ) {
                    return new NbtDouble( tagName, (double)value );
                } else {
                    throw new NotSupportedException( "Serializing objects of type " + type +
                                                     " is not supported by NbtSerializer." );
                }
            }

            // serialize strings
            string s = value as string;
            if( s != null ) {
                return new NbtString( tagName, s );
            }

            // serialize arrays
            if( type.IsArray ) {
                if( type.GetArrayRank() > 1 ) {
                    throw new NotSupportedException(
                        "Serializing multi-dimensional arrays is not supported by NbtSerializer." );
                }
                if( type.GetElementType() == typeof( byte ) ) {
                    return new NbtByteArray( tagName, (byte[])value );
                } else if( type.GetElementType() == typeof( int ) ) {
                    return new NbtIntArray( tagName, (int[])value );
                }
            }

            // serialize lists
            IList list = value as IList;
            if( list != null ) {
                return SerializeList( list, tagName );
            }

            // serialize everything else
            NbtCompound compound = new NbtCompound( tagName );
            foreach( PropertyInfo property in value.GetType().GetProperties() ) {
                if( !property.CanRead || Attribute.IsDefined( property, typeof( NbtIgnoreAttribute ) ) ) {
                    continue;
                }

                object propValue = property.GetValue( value, null );

                if( propValue == null && Attribute.IsDefined( property, typeof( IgnoreOnNullAttribute ) ) ) {
                    continue;
                }

                string name;
                Attribute[] nameAttributes = Attribute.GetCustomAttributes( property, typeof( TagNameAttribute ) );
                if( nameAttributes.Length != 0 ) {
                    name = ( (TagNameAttribute)nameAttributes[0] ).Name;
                } else {
                    name = property.Name;
                }

                if( propValue == null ) {
                    if( property.PropertyType.IsValueType ) {
                        propValue = Activator.CreateInstance( property.PropertyType );
                    } else if( property.PropertyType == typeof( string ) ) {
                        propValue = "";
                    }
                }

                compound.Add( Serialize( propValue, name ) );
            }

            return compound;
        }


        [NotNull]
        protected virtual NbtTag SerializeList( [NotNull] IList list, [CanBeNull] string tagName ) {
            NbtList resultList = new NbtList( tagName );
            foreach( object item in list ) {
                resultList.Add( Serialize( item, null ) );
            }
            return resultList;
        }


        /// <summary> Gets value from a simple value-type NbtTag. Does not support Lists or Compounds.
        /// Use Deserialize(NbtTag,Type) overload to deserialize complex tags. </summary>
        /// <param name="tag"> Tag to parse. </param>
        /// <returns> Value of the given tag. </returns>
        /// <exception cref="ArgumentNullException"> <paramref name="tag"/> is null. </exception>
        /// <exception cref="NotSupportedException"> <paramref name="tag"/> is not a value tag. </exception>
        public virtual object Deserialize( [NotNull] NbtTag tag ) {
            if( tag == null ) {
                throw new ArgumentNullException( "tag" );
            }
            switch( tag.TagType ) {
                case NbtTagType.Byte:
                    return tag.ByteValue;
                case NbtTagType.ByteArray:
                    return tag.ByteArrayValue;
                case NbtTagType.Double:
                    return tag.DoubleValue;
                case NbtTagType.Float:
                    return tag.FloatValue;
                case NbtTagType.Int:
                    return tag.IntValue;
                case NbtTagType.IntArray:
                    return tag.IntArrayValue;
                case NbtTagType.Long:
                    return tag.LongValue;
                case NbtTagType.Short:
                    return tag.ShortValue;
                case NbtTagType.String:
                    return tag.StringValue;
                default:
                    throw new NotSupportedException(
                        "Deserialize(NbtTag) can only handle value-type tags. " +
                        "For lists and compounds, use Deserialize(NbtTag,Type) overload." );
            }
        }


        /// <summary> Creates an object from the given NbtTag. For primitive types, strings, byte arrays, and int arrays.
        /// If <paramref name="tag"/> is of type List, expects given <paramref name="type"/> to implement IList.
        /// If <paramref name="tag"/> is of type Compound, object properties will be matched to compound's child tags by name.
        /// For List and Compound tags, <paramref name="type"/> is expected to provide a public parameterless constructor. </summary>
        /// <param name="tag"> Tag to parse. </param>
        /// <param name="type"> Expected type of the resulting object. </param>
        /// <returns> Given tag interpreted as an object of the given type. </returns>
        /// <exception cref="ArgumentNullException"> <paramref name="tag"/> or <paramref name="type"/> is null. </exception>
        /// <exception cref="NotSupportedException"> <paramref name="tag"/> cannot be deserialized (e.g. is a list of non-value tags). </exception>
        /// <exception cref="MissingMethodException"> Given <paramref name="type"/> does not provide a public parameterless constructor. </exception>
        public virtual object Deserialize( [NotNull] NbtTag tag, [NotNull] Type type ) {
            if( tag == null ) {
                throw new ArgumentNullException( "tag" );
            }
            if( type == null ) {
                throw new ArgumentNullException( "type" );
            }
            // custom deserialization
            if( typeof( INbtSerializable ).IsAssignableFrom( type ) ) {
                INbtSerializable resultObject = (INbtSerializable)Activator.CreateInstance( type );
                resultObject.Deserialize( tag );
                return resultObject;
            }

            // deserialize value types (including primitives, Strings, ByteArrays, and IntArrays)
            if( tag.HasValue ) {
                return Deserialize( tag );
            }

            // deserialize lists
            NbtList list = tag as NbtList;
            if( list != null ) {
                IList resultObject = (IList)Activator.CreateInstance( type );
                foreach( NbtTag childTag in list ) {
                    if( childTag.HasValue ) {
                        resultObject.Add( Deserialize( childTag ) );
                    } else {
                        throw new NotSupportedException(
                            "List deserialization is only supported for lists of value tags." );
                    }
                }
                return resultObject;
            }

            // deserializing compounds
            NbtCompound compound = tag as NbtCompound;
            if( compound != null ) {
                object resultObject = Activator.CreateInstance( type );
                foreach( PropertyInfo property in type.GetProperties() ) {
                    if( !property.CanWrite || Attribute.IsDefined( property, typeof( NbtIgnoreAttribute ) ) ) {
                        continue;
                    }

                    string name;
                    Attribute[] nameAttributes = Attribute.GetCustomAttributes( property, typeof( TagNameAttribute ) );
                    if( nameAttributes.Length != 0 ) {
                        name = ( (TagNameAttribute)nameAttributes[0] ).Name;
                    } else {
                        name = property.Name;
                    }

                    NbtTag node = compound.Get( name );
                    if( node == null ) continue;

                    property.SetValue( resultObject, Deserialize( tag ), null );
                }
                return resultObject;

            }

            throw new NotSupportedException( "Could not deserialize tag " + tag );
        }
    }
}