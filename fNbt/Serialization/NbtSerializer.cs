using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace fNbt.Serialization {
    public class NbtSerializer {
        public Type Type { get; set; }

        PropertyInfo[] propertyCache;
        Dictionary<PropertyInfo, string> customTagNames;
        HashSet<PropertyInfo> ignoreOnNull; 

        /// <summary> Decorates the given property or field with the specified. </summary>
        public NbtSerializer( Type type ) {
            Type = type;
        }


        public NbtTag Serialize( object value, bool skipInterfaceCheck = false ) {
            return Serialize( value, "", skipInterfaceCheck );
        }


        PropertyInfo[] GetProperties() {
            try {
                if( propertyCache == null ) {
                    propertyCache =
                        Type.GetProperties()
                            .Where( p => !Attribute.GetCustomAttributes( p, typeof( NbtIgnoreAttribute ) ).Any() )
                            .Where( p => p.CanRead )
                            .ToArray();
                    customTagNames = new Dictionary<PropertyInfo, string>();

                    foreach( PropertyInfo property in propertyCache ) {
                        // read tag name
                        Attribute[] nameAttributes = Attribute.GetCustomAttributes( property, typeof( TagNameAttribute ) );
                        string tagName;
                        if( nameAttributes.Length != 0 ) {
                            tagName = ( (TagNameAttribute)nameAttributes[0] ).Name;
                        } else {
                            tagName = property.Name;
                        }
                        customTagNames.Add( property, tagName );

                        // read IgnoreOnNull attribute
                        Attribute ignoreOnNullAttribute = Attribute.GetCustomAttribute( property,
                            typeof( IgnoreOnNullAttribute ) );
                        if( ignoreOnNullAttribute != null ) {
                            if( ignoreOnNull == null ) {
                                ignoreOnNull = new HashSet<PropertyInfo>();
                            }
                            ignoreOnNull.Add( property );
                        }
                    }
                }

            } catch {
                // roll back on error
                propertyCache = null;
                ignoreOnNull = null;
                customTagNames = null;
                throw;
            }
            return propertyCache;
        }



        public NbtTag Serialize( object value, string tagName, bool skipInterfaceCheck = false ) {
            if( !skipInterfaceCheck && value is INbtSerializable ) {
                return ( (INbtSerializable)value ).Serialize( tagName );
            } else if( value is NbtTag ) {
                return (NbtTag)value;
            } else if( value is byte ) {
                return new NbtByte( tagName, (byte)value );
            } else if( value is sbyte ) {
                return new NbtByte( tagName, (byte)(sbyte)value );
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
            } else if( value is uint ) {
                return new NbtInt( tagName, (int)(uint)value );
            } else if( value is int[] ) {
                return new NbtIntArray( tagName, (int[])value );
            } else if( value is long ) {
                return new NbtLong( tagName, (long)value );
            } else if( value is ulong ) {
                return new NbtLong( tagName, (long)(ulong)value );
            } else if( value is short ) {
                return new NbtShort( tagName, (short)value );
            } else if( value is ushort ) {
                return new NbtShort( tagName, (short)(ushort)value );
            } else if( value is string ) {
                return new NbtString( tagName, (string)value );
            } else if( Type.IsArray ) {
                Type elementType = value.GetType().GetElementType();
                var array = value as Array;
                var listType = NbtTagType.Compound;
                if( elementType == typeof( byte ) || elementType == typeof( sbyte ) )
                    listType = NbtTagType.Byte;
                else if( elementType == typeof( bool ) )
                    listType = NbtTagType.Byte;
                else if( elementType == typeof( byte[] ) )
                    listType = NbtTagType.ByteArray;
                else if( elementType == typeof( double ) )
                    listType = NbtTagType.Double;
                else if( elementType == typeof( float ) )
                    listType = NbtTagType.Float;
                else if( elementType == typeof( int ) || elementType == typeof( uint ) )
                    listType = NbtTagType.Int;
                else if( elementType == typeof( int[] ) )
                    listType = NbtTagType.IntArray;
                else if( elementType == typeof( long ) || elementType == typeof( ulong ) )
                    listType = NbtTagType.Long;
                else if( elementType == typeof( short ) || elementType == typeof( ushort ) )
                    listType = NbtTagType.Short;
                else if( elementType == typeof( string ) )
                    listType = NbtTagType.String;
                var list = new NbtList( tagName, listType );
                var innerSerializer = new NbtSerializer( elementType );
                for( int i = 0; i < array.Length; i++ ) {
                    list.Add( innerSerializer.Serialize( array.GetValue( i ) ) );
                }
                return list;

            } else if( value is NbtFile ) {
                return ( (NbtFile)value ).RootTag;

            } else {
                var compound = new NbtCompound( tagName );
                if( value == null ) return compound;

                foreach( PropertyInfo property in GetProperties() ) {
                    var innerSerializer = new NbtSerializer( property.PropertyType );
                    object propValue = property.GetValue( value, null );
                    if( propValue == null ) {
                        if( ignoreOnNull.Contains( property ) ) continue;
                        if( property.PropertyType.IsValueType ) {
                            propValue = Activator.CreateInstance( property.PropertyType );
                        } else if( property.PropertyType == typeof( string ) ) {
                            propValue = "";
                        }
                    }
                    NbtTag tag = innerSerializer.Serialize( propValue, customTagNames[property] );
                    compound.Add( tag );
                }

                return compound;
            }
        }


        public object Deserialize( NbtTag value, bool skipInterfaceCheck = false ) {
            if( !skipInterfaceCheck && typeof( INbtSerializable ).IsAssignableFrom( Type ) ) {
                var instance = (INbtSerializable)Activator.CreateInstance( Type );
                instance.Deserialize( value );
                return instance;
            }
            switch( value.TagType ) {
                case NbtTagType.Byte:
                    return ( (NbtByte)value ).Value;

                case NbtTagType.ByteArray:
                    return ( (NbtByteArray)value ).Value;

                case NbtTagType.Double:
                    return ( (NbtDouble)value ).Value;

                case NbtTagType.Float:
                    return ( (NbtFloat)value ).Value;

                case NbtTagType.Int:
                    return ( (NbtInt)value ).Value;

                case NbtTagType.IntArray:
                    return ( (NbtIntArray)value ).Value;

                case NbtTagType.Long:
                    return ( (NbtLong)value ).Value;

                case NbtTagType.Short:
                    return ( (NbtShort)value ).Value;

                case NbtTagType.String:
                    return ( (NbtString)value ).Value;

                case NbtTagType.List:
                    var list = (NbtList)value;
                    Type type;
                    switch( list.ListType ) {
                        case NbtTagType.Byte:
                            type = typeof( byte );
                            break;
                        case NbtTagType.ByteArray:
                            type = typeof( byte[] );
                            break;
                        case NbtTagType.Compound:
                            type = Type.GetElementType() ?? typeof( object );
                            break;
                        case NbtTagType.Double:
                            type = typeof( double );
                            break;
                        case NbtTagType.Float:
                            type = typeof( float );
                            break;
                        case NbtTagType.Int:
                            type = typeof( int );
                            break;
                        case NbtTagType.IntArray:
                            type = typeof( int[] );
                            break;
                        case NbtTagType.Long:
                            type = typeof( long );
                            break;
                        case NbtTagType.Short:
                            type = typeof( short );
                            break;
                        case NbtTagType.String:
                            type = typeof( string );
                            break;
                        default:
                            throw new NotSupportedException( "The NBT list type '" + list.TagType + "' is not supported." );
                    }
                    Array array = Array.CreateInstance( type, list.Count );
                    var innerSerializer = new NbtSerializer( type );
                    for( int i = 0; i < array.Length; i++ ) {
                        array.SetValue( innerSerializer.Deserialize( list[i] ), i );
                    }
                    return array;

                case NbtTagType.Compound:
                    var compound = value as NbtCompound;

                    object resultObject = Activator.CreateInstance( Type );
                    foreach (PropertyInfo property in GetProperties()) {
                        if( !property.CanWrite ) continue;
                        string name = property.Name;
                        Attribute[] nameAttributes = Attribute.GetCustomAttributes( property, typeof( TagNameAttribute ) );

                        if( nameAttributes.Length != 0 ) {
                            name = ( (TagNameAttribute)nameAttributes[0] ).Name;
                        }
                        NbtTag node = compound.Tags.SingleOrDefault( a => a.Name == name );
                        if( node == null ) continue;

                        object data;
                        if( typeof( INbtSerializable ).IsAssignableFrom( property.PropertyType ) ) {
                            data = Activator.CreateInstance( property.PropertyType );
                            ( (INbtSerializable)data ).Deserialize( node );
                        } else {
                            data = new NbtSerializer( property.PropertyType ).Deserialize( node );
                        }

                        // Some manual casting for edge cases
                        if( property.PropertyType == typeof( bool )
                            && data is byte ) {
                            data = (byte)data == 1;
                        }
                        if( property.PropertyType == typeof( sbyte ) && data is byte ) {
                            data = (sbyte)(byte)data;
                        }

                        property.SetValue( resultObject, data, null );
                    }

                    return resultObject;
            }

            throw new NotSupportedException( "The node type '" + value.GetType() + "' is not supported." );
        }
    }
}