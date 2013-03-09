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
            if( type.IsPrimitive ) {
                if( value is bool ) {
                    return new NbtByte( tagName, (byte)((bool)value ? 1 : 0) );
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

            } else if( value is string ) {
                return new NbtString( tagName, (string)value );

            }else if( type.IsArray ) {
                if( type.GetArrayRank() > 1 ) {
                    throw new NotSupportedException(
                        "Serializing multi-dimensional arrays is not supported by NbtSerializer." );
                }
                if( type.GetElementType() == typeof( byte ) ) {
                    return new NbtByteArray( tagName, (byte[])value );
                } else if( type.GetElementType() == typeof( int ) ) {
                    return new NbtIntArray( tagName, (int[])value );
                } else {
                    throw new NotImplementedException( "Todo: add list serialization" );
                }

            } else if( value is IList ) {
                throw new NotImplementedException( "Todo: add list serialization" );

            } else {
                NbtCompound compound = new NbtCompound( tagName );

                List<PropertyInfo> properties = new List<PropertyInfo>();
                foreach( PropertyInfo property in typeof( object ).GetProperties() ) {
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
                        } else if( property.PropertyType == typeof( string ) )
                            propValue = "";
                    }

                    compound.Add( Serialize( propValue, name ) );
                }

                return compound;
            }
        }


        public object Deserialize ( NbtTag value ) {
            if( value is NbtByte ) {
                return ( (NbtByte)value ).Value;
            } else if( value is NbtByteArray ) {
                return ( (NbtByteArray)value ).Value;
            } else if( value is NbtDouble ) {
                return ( (NbtDouble)value ).Value;
            } else if( value is NbtFloat ) {
                return ( (NbtFloat)value ).Value;
            } else if( value is NbtInt ) {
                return ( (NbtInt)value ).Value;
            } else if( value is NbtIntArray ) {
                return ( (NbtIntArray)value ).Value;
            } else if( value is NbtLong ) {
                return ( (NbtLong)value ).Value;
            } else if( value is NbtShort ) {
                return ( (NbtShort)value ).Value;
            } else if( value is NbtString ) {
                return ( (NbtString)value ).Value;
            } else if( value is NbtCompound ) {
                var compound = value as NbtCompound;

                List<PropertyInfo> properties = new List<PropertyInfo>();
                foreach( PropertyInfo p in typeof(object).GetProperties() ) {
                    if( Attribute.GetCustomAttributes( p, typeof( NbtIgnoreAttribute ) ).Length == 0 ) {
                        properties.Add( p );
                    }
                }

                var resultObject = Activator.CreateInstance( Type );
                foreach( var property in properties ) {
                    if( !property.CanWrite ) {
                        continue;
                    }
                    string name = property.Name;
                    Attribute[] nameAttributes = Attribute.GetCustomAttributes( property, typeof( TagNameAttribute ) );

                    if( nameAttributes.Length != 0 ) {
                        name = ( (TagNameAttribute)nameAttributes[0] ).Name;
                    }

                    var node = compound.Get<NbtTag>( name );
                    if( node == null ) continue;
                    object data = Deserialize( node );

                    if( property.PropertyType == typeof( bool ) && data is byte ) {
                        data = (byte)data == 1;
                    }

                    property.SetValue( resultObject, data, null );
                }

                return resultObject;
            }

            throw new NotSupportedException( "The node type '" + value.GetType() + "' is not supported." );
        }
    }
}