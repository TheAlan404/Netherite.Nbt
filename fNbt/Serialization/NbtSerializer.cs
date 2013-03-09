using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace fNbt.Serialization {
    public class NbtSerializer {
        public NbtTag Serialize( object value ) {
            return Serialize( value, null );
        }


        public NbtTag Serialize( object value, string tagName ) {
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


        NbtTag SerializeList( IList value, string tagName ) {
            NbtList list = new NbtList( tagName );
            foreach( object item in value ) {
                list.Add( Serialize( value ) );
            }
            return list;
        }


        public object Deserialize( NbtTag tag, Type type ) {
            // custom deserialization
            if( typeof( INbtSerializable ).IsAssignableFrom( type ) ) {
                INbtSerializable resultObject = (INbtSerializable)Activator.CreateInstance( type );
                resultObject.Deserialize( tag );
                return resultObject;
            }

            // deserialize value types (including primitives, Strings, ByteArrays, and IntArrays)
            if( tag.HasValue ) {
                return DeserializeTagValue( tag );
            }

            // deserialize lists
            NbtList list = tag as NbtList;
            if( list != null ) {
                IList resultObject = (IList)Activator.CreateInstance( type );
                foreach( NbtTag childTag in list ) {
                    if( childTag.HasValue ) {
                        resultObject.Add( DeserializeTagValue( childTag ) );
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

                    property.SetValue( resultObject, DeserializeTagValue( tag ), null );
                }
                return resultObject;

            }

            throw new NotSupportedException( "Could not deserialize tag " + tag );
        }


        object DeserializeTagValue( NbtTag tag ) {
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
                    throw new NotSupportedException( "Could not deserialize tag " + tag );
            }
        }
    }
}