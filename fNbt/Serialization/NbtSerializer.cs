using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace fNbt.Serialization {
    public class NbtSerializer {
        public Type Type { get; set; }


        /// <summary> Decorates the given property or field with the specified. </summary>
        public NbtSerializer(Type type) {
            Type = type;
        }


        PropertyInfo[] properties;
        Dictionary<PropertyInfo, string> propertyTagNames;
        Dictionary<PropertyInfo, NullPolicy> nullPolicies;
        Dictionary<PropertyInfo, NullPolicy> elementNullPolicies;
        bool propertyInfoRead;


        void ReadPropertyInfo() {
            try {
                properties =
                    Type.GetProperties()
                        .Where(p => !Attribute.GetCustomAttributes(p, typeof(NbtIgnoreAttribute)).Any())
                        .ToArray();
                propertyTagNames = new Dictionary<PropertyInfo, string>();

                foreach (PropertyInfo property in properties) {
                    // read tag name
                    Attribute[] nameAttributes = Attribute.GetCustomAttributes(property, typeof(TagNameAttribute));
                    string tagName;
                    if (nameAttributes.Length != 0) {
                        tagName = ((TagNameAttribute)nameAttributes[0]).Name;
                    } else {
                        tagName = property.Name;
                    }
                    propertyTagNames.Add(property, tagName);

                    // read IgnoreOnNull attribute
                    NullPolicyAttribute nullPolicyAttr =
                        (NullPolicyAttribute)Attribute.GetCustomAttribute(property, typeof(NullPolicyAttribute));
                    if (nullPolicyAttr != null) {
                        if (nullPolicyAttr.SelfPolicy != NullPolicy.Default) {
                            if (nullPolicies == null) {
                                nullPolicies = new Dictionary<PropertyInfo, NullPolicy>();
                            }
                            nullPolicies.Add(property, nullPolicyAttr.SelfPolicy);
                        }
                        if (nullPolicyAttr.ElementPolicy != NullPolicy.Default) {
                            if (elementNullPolicies == null) {
                                elementNullPolicies = new Dictionary<PropertyInfo, NullPolicy>();
                            }
                            elementNullPolicies.Add( property, nullPolicyAttr.ElementPolicy );
                        }
                    }
                }
                propertyInfoRead = true;
            } catch {
                // roll back on error
                properties = null;
                nullPolicies = null;
                elementNullPolicies = null;
                propertyTagNames = null;
                propertyInfoRead = false;
                throw;
            }
        }


        NbtTag SerializePrimitiveType(string tagName, object value) {
            Type valueType = value.GetType();
            if (valueType == typeof(byte)) {
                return new NbtByte(tagName, (byte)value);
            } else if (valueType == typeof(sbyte)) {
                return new NbtByte(tagName, (byte)(sbyte)value);
            } else if (valueType == typeof(bool)) {
                return new NbtByte(tagName, (bool)value ? (byte)1 : (byte)0);
            } else if (valueType == typeof(double)) {
                return new NbtDouble(tagName, (double)value);
            } else if (valueType == typeof(float)) {
                return new NbtFloat(tagName, (float)value);
            } else if (valueType == typeof(int)) {
                return new NbtInt(tagName, (int)value);
            } else if (valueType == typeof(uint)) {
                return new NbtInt(tagName, (int)(uint)value);
            } else if (valueType == typeof(long)) {
                return new NbtLong(tagName, (long)value);
            } else if (valueType == typeof(ulong)) {
                return new NbtLong(tagName, (long)(ulong)value);
            } else if (valueType == typeof(short)) {
                return new NbtShort(tagName, (short)value);
            } else if (valueType == typeof(ushort)) {
                return new NbtShort(tagName, (short)(ushort)value);
            } else if (valueType == typeof(char)) {
                return new NbtShort(tagName, (short)(char)value);
            } else {
                throw new NotSupportedException();
            }
        }


        NbtTag SerializeList(string tagName, IList valueAsArray, Type elementType, NullPolicy elementNullPolicy) {
            NbtTagType listType;
            if (elementType == typeof(byte) || elementType == typeof(sbyte) || elementType == typeof(bool)) {
                listType = NbtTagType.Byte;
            } else if (elementType == typeof(byte[])) {
                listType = NbtTagType.ByteArray;
            } else if (elementType == typeof(double)) {
                listType = NbtTagType.Double;
            } else if (elementType == typeof(float)) {
                listType = NbtTagType.Float;
            } else if (elementType == typeof(int) || elementType == typeof(uint)) {
                listType = NbtTagType.Int;
            } else if (elementType == typeof(int[])) {
                listType = NbtTagType.IntArray;
            } else if (elementType == typeof(long) || elementType == typeof(ulong)) {
                listType = NbtTagType.Long;
            } else if (elementType == typeof(short) || elementType == typeof(ushort)) {
                listType = NbtTagType.Short;
            } else if (elementType == typeof(string)) {
                listType = NbtTagType.String;
            } else {
                listType = NbtTagType.Compound;
            }

            var list = new NbtList(tagName, listType);

            if (elementType.IsPrimitive) {
                // speedy serialization for basic types
                for (int i = 0; i < valueAsArray.Count; i++) {
                    list.Add(SerializePrimitiveType(null, valueAsArray[i]));
                }

            } else if (SerializationUtil.IsDirectlyMappedType(elementType)) {
                // speedy serialization for directly-mapped types
                for (int i = 0; i < valueAsArray.Count; i++) {
                    var value = valueAsArray[i];
                    if (value == null) {
                        switch (elementNullPolicy) {
                            case NullPolicy.Error:
                                throw new NullReferenceException("Null elements not allowed for tag " + tagName);
                            case NullPolicy.InsertDefault:
                                list.Add(Serialize(SerializationUtil.GetDefaultValue(elementType), true));
                                break;
                            case NullPolicy.Ignore:
                                continue;
                        }
                    } else {
                        list.Add(Serialize(valueAsArray[i], true));
                    }
                }

            } else {
                // serialize complex types
                var innerSerializer = new NbtSerializer(elementType);
                for( int i = 0; i < valueAsArray.Count; i++ ) {
                    var value = valueAsArray[i];
                    if( value == null ) {
                        switch( elementNullPolicy ) {
                            case NullPolicy.Error:
                                throw new NullReferenceException( "Null elements not allowed for tag " + tagName );
                            case NullPolicy.Ignore:
                                continue;
                            case NullPolicy.InsertDefault:
                                // TODO
                                break;
                        }
                    } else {
                        list.Add( innerSerializer.Serialize( valueAsArray[i], null ) );
                    }
                }
            }
            return list;
        }


        NullPolicy GetElementPolicy(PropertyInfo prop) {
            if (nullPolicies != null) {
                NullPolicy result;
                if (nullPolicies.TryGetValue(prop, out result)) {
                    return result;
                }
            }
            return NullPolicy.Default;
        }


        public NbtTag Serialize(object value, bool skipInterfaceCheck = false) {
            return Serialize(value, "", skipInterfaceCheck);
        }


        public NbtTag Serialize(object value, string tagName, bool skipInterfaceCheck = false,
                                NullPolicy thisNullPolicy = NullPolicy.Error,
                                NullPolicy elementNullPolicy = NullPolicy.Error) {
            if (value == null) {
                return new NbtCompound(tagName);
            }

            Type realType = value.GetType();
            if (realType.IsPrimitive) {
                return SerializePrimitiveType(tagName, value);
            }

            var valueAsString = value as string;
            if (valueAsString != null) {
                return new NbtString(tagName, valueAsString);
            }

            // Serialize arrays
            var valueAsArray = value as Array;
            if (valueAsArray != null) {
                var valueAsByteArray = value as byte[];
                if (valueAsByteArray != null) {
                    return new NbtByteArray(tagName, valueAsByteArray);
                }

                var valueAsIntArray = value as int[];
                if (valueAsIntArray != null) {
                    return new NbtIntArray(tagName, valueAsIntArray);
                }

                Type elementType = realType.GetElementType();
                return SerializeList( tagName, valueAsArray, elementType, elementNullPolicy );
            }

            if (!skipInterfaceCheck && value is INbtSerializable) {
                return ((INbtSerializable)value).Serialize(tagName);
            }

            // Serialize ILists
            if (realType.IsGenericType && realType.GetGenericTypeDefinition() == typeof(List<>)) {
                Type listType = realType.GetGenericArguments()[0];
                return SerializeList( tagName, (IList)value, listType, elementNullPolicy );
            }

            // Skip serializing NbtTags and NbtFiles
            var valueAsTag = value as NbtTag;
            if (valueAsTag != null) {
                return valueAsTag;
            }
            var file = value as NbtFile;
            if (file != null) {
                return file.RootTag;
            }

            // Fallback for compound tags
            var compound = new NbtCompound(tagName);
            if (!propertyInfoRead) ReadPropertyInfo();

            foreach (PropertyInfo property in properties) {
                if (!property.CanRead) continue;
                Type propType = property.PropertyType;
                object propValue = property.GetValue(value, null);

                // Handle null property values
                if (propValue == null) {
                    NullPolicy selfNullPolicy = GetElementPolicy(property);
                    switch (selfNullPolicy) {
                        case NullPolicy.Ignore:
                            continue;
                        case NullPolicy.Error:
                            throw new NullReferenceException("Null values not allowed for property " + property.Name);
                        case NullPolicy.InsertDefault:
                            propValue = SerializationUtil.GetDefaultValue(propType);
                            break;
                    }
                }

                string propTagName = propertyTagNames[property];
                NbtTag tag;
                if (propType.IsPrimitive) {
                    tag = SerializePrimitiveType(propTagName, propValue);
                } else if (propType.IsArray || propType == typeof(string)) {
                    tag = Serialize(propValue, propTagName);
                } else {
                    var innerSerializer = new NbtSerializer(property.PropertyType);
                    tag = innerSerializer.Serialize(propValue, propTagName);
                }
                compound.Add(tag);
            }

            return compound;
        }


        static object DeserializeSimpleType(NbtTag tag) {
            switch (tag.TagType) {
                case NbtTagType.Byte:
                    return ((NbtByte)tag).Value;

                case NbtTagType.Double:
                    return ((NbtDouble)tag).Value;

                case NbtTagType.Float:
                    return ((NbtFloat)tag).Value;

                case NbtTagType.Int:
                    return ((NbtInt)tag).Value;

                case NbtTagType.Long:
                    return ((NbtLong)tag).Value;

                case NbtTagType.Short:
                    return ((NbtShort)tag).Value;

                case NbtTagType.ByteArray:
                    return ((NbtByteArray)tag).Value;

                case NbtTagType.IntArray:
                    return ((NbtIntArray)tag).Value;

                case NbtTagType.String:
                    return ((NbtString)tag).Value;

                default:
                    throw new NotSupportedException();
            }
        }


        public object Deserialize(NbtTag tag, bool skipInterfaceCheck = false) {
            if (!skipInterfaceCheck && typeof(INbtSerializable).IsAssignableFrom(Type)) {
                var instance = (INbtSerializable)Activator.CreateInstance(Type);
                instance.Deserialize(tag);
                return instance;
            }
            switch (tag.TagType) {
                case NbtTagType.List:
                    var list = (NbtList)tag;
                    Type type;
                    switch (list.ListType) {
                        case NbtTagType.Byte:
                            type = typeof(byte);
                            break;
                        case NbtTagType.ByteArray:
                            type = typeof(byte[]);
                            break;
                        case NbtTagType.List:
                            type = Type.GetElementType() ?? typeof(object);
                            break;
                        case NbtTagType.Double:
                            type = typeof(double);
                            break;
                        case NbtTagType.Float:
                            type = typeof(float);
                            break;
                        case NbtTagType.Int:
                            type = typeof(int);
                            break;
                        case NbtTagType.IntArray:
                            type = typeof(int[]);
                            break;
                        case NbtTagType.Long:
                            type = typeof(long);
                            break;
                        case NbtTagType.Short:
                            type = typeof(short);
                            break;
                        case NbtTagType.String:
                            type = typeof(string);
                            break;
                        default:
                            throw new NotSupportedException("The NBT list type '" + list.TagType +
                                                            "' is not supported.");
                    }
                    Array array = Array.CreateInstance(type, list.Count);
                    if (type.IsPrimitive || type.IsArray || type == typeof(string)) {
                        for (int i = 0; i < array.Length; i++) {
                            array.SetValue(DeserializeSimpleType(list[i]), i);
                        }
                    } else {
                        var innerSerializer = new NbtSerializer(type);
                        for (int i = 0; i < array.Length; i++) {
                            array.SetValue(innerSerializer.Deserialize(list[i]), i);
                        }
                    }
                    return array;

                case NbtTagType.Compound:
                    if (!propertyInfoRead) ReadPropertyInfo();
                    var compound = (NbtCompound)tag;

                    object resultObject = Activator.CreateInstance(Type);
                    foreach (PropertyInfo property in properties) {
                        if (!property.CanWrite) continue;
                        string name = propertyTagNames[property];

                        NbtTag node;
                        if (!compound.TryGet(name, out node)) continue;

                        object data;
                        if (typeof(INbtSerializable).IsAssignableFrom(property.PropertyType)) {
                            data = Activator.CreateInstance(property.PropertyType);
                            ((INbtSerializable)data).Deserialize(node);
                        } else {
                            data = new NbtSerializer(property.PropertyType).Deserialize(node);
                        }

                        // Some manual casting for edge cases
                        if (property.PropertyType == typeof(bool) && data is byte) {
                            data = (byte)data == 1;
                        }
                        if (property.PropertyType == typeof(sbyte) && data is byte) {
                            data = (sbyte)(byte)data;
                        }

                        if (property.PropertyType.IsInstanceOfType(data)) {
                            property.SetValue(resultObject, data, null);
                        } else {
                            property.SetValue(resultObject, Convert.ChangeType(data, property.PropertyType), null);
                        }
                    }

                    return resultObject;

                default:
                    return DeserializeSimpleType(tag);
            }
        }
    }
}
