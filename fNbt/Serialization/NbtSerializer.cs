using System;
using System.Collections.Generic;
using System.Reflection;

namespace fNbt.Serialization {
    public class NbtSerializer {
        public Type Type { get; set; }


        /// <summary> Decorates the given property or field with the specified NBT tag name. </summary>
        public NbtSerializer( Type type ) {
            Type = type;
        }


        public NbtTag Serialize( object value ) {
            return Serialize( value, "" );
        }


        public NbtTag Serialize( object value, string tagName ) {
            if( value is byte ) {
                return new NbtByte( tagName, (byte)value );
            } else if( value is bool ) {
                return new NbtByte( tagName, (byte)( (bool)value ? 1 : 0 ) );
            } else if( value is byte[] ) {
                return new NbtByteArray( tagName, (byte[])value );
            } else if( value is double ) {
                return new NbtDouble( tagName, (double)value );
            } else if( value is float ) {
                return new NbtFloat( tagName, (float)value );
            } else if( value is int ) {
                return new NbtInt( tagName, (int)value );
            } else if( value is int[] ) {
                return new NbtIntArray( tagName, (int[])value );
            } else if( value is long ) {
                return new NbtLong( tagName, (long)value );
            } else if( value is short ) {
                return new NbtShort( tagName, (short)value );
            } else if( value is string ) {
                return new NbtString( tagName, (string)value );
            } else {
                NbtCompound compound = new NbtCompound( tagName );

                if( value == null ) return compound;
                Attribute[] nameAttributes = Attribute.GetCustomAttributes( value.GetType(),
                                                                            typeof( TagNameAttribute ) );

                if( nameAttributes.Length > 0 ) {
                    compound = new NbtCompound( ( (TagNameAttribute)nameAttributes[0] ).Name );
                }

                List<PropertyInfo> chosenProperties = new List<PropertyInfo>();
                foreach( PropertyInfo p in Type.GetProperties() ) {
                    if( Attribute.GetCustomAttributes( p, typeof( NbtIgnoreAttribute ) ).Length == 0 ) {
                        chosenProperties.Add( p );
                    }
                }

                foreach( PropertyInfo property in chosenProperties ) {
                    if( !property.CanRead ) {
                        continue;
                    }

                    string name = property.Name;
                    nameAttributes = Attribute.GetCustomAttributes( property, typeof( TagNameAttribute ) );
                    Attribute ignoreOnNullAttribute = Attribute.GetCustomAttribute( property, typeof( IgnoreOnNullAttribute ) );
                    if( nameAttributes.Length != 0 ) {
                        name = ( (TagNameAttribute)nameAttributes[0] ).Name;
                    }

                    NbtSerializer innerSerializer = new NbtSerializer( property.PropertyType );
                    object propValue = property.GetValue( value, null );

                    if( propValue == null ) {
                        if( ignoreOnNullAttribute != null ) continue;
                        if( property.PropertyType.IsValueType ) {
                            propValue = Activator.CreateInstance( property.PropertyType );
                        } else if( property.PropertyType == typeof( string ) )
                            propValue = "";
                    }

                    NbtTag tag = innerSerializer.Serialize( propValue, name );
                    compound.Add( tag );
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
                foreach( PropertyInfo p in Type.GetProperties() ) {
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
                    object data = new NbtSerializer( property.PropertyType ).Deserialize( node );

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