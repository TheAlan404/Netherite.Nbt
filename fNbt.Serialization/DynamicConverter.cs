using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;

namespace fNbt.Serialization {
    internal struct DynamicConverter {
        readonly Type contractType;
        readonly SerializerOptions options;
        readonly TypeMetadata typeMetadata;


        public DynamicConverter(Type contractType, SerializerOptions options) {
            this.contractType = contractType;
            this.options = options;
            typeMetadata = TypeMetadata.ReadTypeMetadata(contractType);
        }


        static NbtTag SerializeDirectlyMappedType(string tagName, object value) {
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
            } else if (valueType == typeof(decimal)) {
                return new NbtDouble(tagName, (double)(decimal)value);
            } else {
                throw new NotSupportedException(valueType + " is not a directly mapped type");
            }
        }


        NbtTag SerializeIList(string tagName, IList valueAsArray, Type elementType, NullPolicy elementNullPolicy) {
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
                    list.Add(SerializeDirectlyMappedType(null, valueAsArray[i]));
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
                                list.Add(Serialize(SerializationUtil.GetDefaultValue(elementType), null));
                                    // TODO: skip iserializable
                                break;
                            case NullPolicy.Ignore:
                                continue;
                        }
                    } else {
                        list.Add(Serialize(valueAsArray[i], null));
                    }
                }
            } else {
                // serialize complex types
                for (int i = 0; i < valueAsArray.Count; i++) {
                    var value = valueAsArray[i];
                    if (value == null) {
                        switch (elementNullPolicy) {
                            case NullPolicy.Error:
                                throw new NullReferenceException("Null elements not allowed for tag " + tagName);
                            case NullPolicy.Ignore:
                                continue;
                            case NullPolicy.InsertDefault:
                                // TODO
                                break;
                        }
                    } else {
                        list.Add(Serialize(valueAsArray[i], null));
                    }
                }
            }
            return list;
        }


        NullPolicy GetElementPolicy(PropertyInfo prop) {
            if (typeMetadata.NullPolicies != null) {
                NullPolicy result;
                if (typeMetadata.NullPolicies.TryGetValue(prop, out result)) {
                    return result;
                }
            }
            return options.DefaultNullPolicy;
        }


        /// <summary> Serialize a single value of any type to a new tag. </summary>
        [NotNull]
        NbtTag Serialize([CanBeNull] object value, [CanBeNull] string tagName,
            NullPolicy thisNullPolicy = NullPolicy.Default, NullPolicy elementNullPolicy = NullPolicy.Default) {
            if (value == null) {
                switch (thisNullPolicy) {
                    case NullPolicy.InsertDefault:
                        Type tagType = SerializationUtil.FindTagType(contractType);
                        NbtTag tag = SerializationUtil.ConstructTag(tagType);
                        tag.Name = tagName;
                        return tag;
                    default:
                        throw new ArgumentNullException("value");
                }
            }

            Type realType = value.GetType();
            if (realType.IsPrimitive) {
                return SerializeDirectlyMappedType(tagName, value);
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
                return SerializeIList(tagName, valueAsArray, elementType, elementNullPolicy);
            }

            // value is INbtSerializable
            var serializable = value as INbtSerializable;
            if (serializable != null) {
                return serializable.Serialize(tagName);
            }

            // value is IList<?>
            if (realType.IsGenericType && realType.GetGenericTypeDefinition() == typeof(List<>)) {
                Type listType = realType.GetGenericArguments()[0];
                return SerializeIList(tagName, (IList)value, listType, elementNullPolicy);
            }

            // TODO: value is IDictionary<string,?>

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

            foreach (PropertyInfo property in typeMetadata.Properties) {
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

                string propTagName = typeMetadata.PropertyTagNames[property];
                NbtTag tag;
                if (propType.IsPrimitive) {
                    tag = SerializeDirectlyMappedType(propTagName, propValue);
                } else if (propType.IsArray || propType == typeof(string)) {
                    tag = Serialize(propValue, propTagName);
                } else {
                    tag = Serialize(propValue, propTagName);
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


        object Deserialize(NbtTag tag, bool skipInterfaceCheck = false) {
            if (!skipInterfaceCheck && typeof(INbtSerializable).IsAssignableFrom(contractType)) {
                var instance = (INbtSerializable)Activator.CreateInstance(contractType);
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
                            type = contractType.GetElementType() ?? typeof(object);
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
                        for (int i = 0; i < array.Length; i++) {
                            array.SetValue(Deserialize(list[i]), i);
                        }
                    }
                    return array;

                case NbtTagType.Compound:
                    var compound = (NbtCompound)tag;

                    object resultObject = Activator.CreateInstance(contractType);
                    foreach (PropertyInfo property in typeMetadata.Properties) {
                        if (!property.CanWrite) continue;
                        string name = typeMetadata.PropertyTagNames[property];

                        NbtTag node;
                        if (!compound.TryGet(name, out node)) continue;

                        object data;
                        if (typeof(INbtSerializable).IsAssignableFrom(property.PropertyType)) {
                            data = Activator.CreateInstance(property.PropertyType);
                            ((INbtSerializable)data).Deserialize(node);
                        } else {
                            data = Deserialize(node);
                        }

                        // Some manual casting for edge cases
                        if (property.PropertyType == typeof(bool) && data is byte) {
                            data = (byte)data == 1;
                        }
                        if (property.PropertyType == typeof(sbyte) && data is byte) {
                            data = (sbyte)(byte)data;
                        }
                        // TODO: map direct types

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
