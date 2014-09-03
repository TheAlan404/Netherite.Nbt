using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using fNbt.Serialization.Compiled;
using JetBrains.Annotations;

namespace fNbt.Serialization {
    public class NbtSerializer {
        public NbtSerializer(Type contractType)
            : this(contractType, SerializerOptions.Defaults) {}


        public NbtSerializer([NotNull] Type contractType, [NotNull] SerializerOptions options) {
            if (contractType == null) throw new ArgumentNullException("contractType");
            if (options == null) throw new ArgumentNullException("options");
            this.contractType = contractType;
            this.options = options;
            typeMetadata = ReadPropertyInfo(contractType);
        }


        readonly SerializerOptions options;
        TypeMetadata typeMetadata;
        readonly Type contractType;
        NbtSerialize compiledSerializeDelegate;
        NbtDeserialize compiledDeserializeDelegate;


        public void Compile() {
            if (compiledSerializeDelegate == null) {
                compiledSerializeDelegate = NbtCompiler.GetSerializer(contractType);
            }
            if (compiledDeserializeDelegate == null) {
                compiledDeserializeDelegate = NbtCompiler.GetDeserializer(contractType);
            }
            // These fields are only needed for non-compiled serialization. Let's free that memory!
            typeMetadata = null;
        }


        public NbtTag MakeTag(object obj) {
            if (!contractType.IsInstanceOfType(obj)) {
                throw new ArgumentException("Invalid type! Expected an object of type " + contractType);
            }
            throw new NotImplementedException();
        }


        public NbtTag FillTag(object obj, NbtTag tag) {
            if (!contractType.IsInstanceOfType(obj)) {
                throw new ArgumentException("Invalid type! Expected an object of type " + contractType);
            }
            throw new NotImplementedException();
        }


        public object MakeObject(NbtTag tag) {
            throw new NotImplementedException();
        }


        public object FillObject(object obj, NbtTag tag) {
            throw new NotImplementedException();
        }


        static readonly ConcurrentDictionary<Type, TypeMetadata> TypeMetadataCache =
            new ConcurrentDictionary<Type, TypeMetadata>();


        // Read and store metadata about given type, for non-compiled serialization/deserialization
        // This only needs to be called once, on the very first serialization/deserialization call.
        static TypeMetadata ReadPropertyInfo(Type type) {
            TypeMetadata typeMeta;
            if (!TypeMetadataCache.TryGetValue(type, out typeMeta)) {
                // If meta cache does not contain this type yet, lock and double-check
                lock (TypeMetadataCache) {
                    if (!TypeMetadataCache.TryGetValue(type, out typeMeta)) {
                        // If meta cache still does not contain this type, fetch info and store it in cache
                        typeMeta = new TypeMetadata(type);
                        TypeMetadataCache.TryAdd(type, typeMeta);
                    }
                }
            }
            return typeMeta;
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
                                list.Add(Serialize(SerializationUtil.GetDefaultValue(elementType), null)); // TODO: skip iserializable
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
                var innerSerializer = new NbtSerializer(elementType);
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
                        list.Add(innerSerializer.Serialize(valueAsArray[i], null));
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
            return NullPolicy.Default;
        }


        public NbtTag Serialize(object value) {
            return Serialize(value, "");
        }


        public NbtTag Serialize(object value, string tagName,
                                NullPolicy thisNullPolicy = NullPolicy.Error,
                                NullPolicy elementNullPolicy = NullPolicy.Error) {
            if (compiledSerializeDelegate != null) {
                return compiledSerializeDelegate(tagName, value);
            }

            if (value == null) {
                switch (thisNullPolicy) {
                    case NullPolicy.InsertDefault:
                        return new NbtCompound(tagName);
                    default:
                        throw new ArgumentNullException("value");
                }
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
                return SerializeList(tagName, valueAsArray, elementType, elementNullPolicy);
            }

            // value is INbtSerializable
            var serializable = value as INbtSerializable;
            if ( serializable != null) {
                return serializable.Serialize(tagName);
            }

            // value is IList<?>
            if (realType.IsGenericType && realType.GetGenericTypeDefinition() == typeof(List<>)) {
                Type listType = realType.GetGenericArguments()[0];
                return SerializeList(tagName, (IList)value, listType, elementNullPolicy);
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
                    tag = SerializePrimitiveType(propTagName, propValue);
                } else if (propType.IsArray || propType == typeof(string)) {
                    tag = Serialize(propValue, propTagName);
                } else {
                    var innerSerializer = new NbtSerializer(property.PropertyType, options);
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
            if (!skipInterfaceCheck && typeof(INbtSerializable).IsAssignableFrom(contractType)) {
                var instance = (INbtSerializable)Activator.CreateInstance(contractType); // TODO: options.constructor
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
                        var innerSerializer = new NbtSerializer(type, options);
                        for (int i = 0; i < array.Length; i++) {
                            array.SetValue(innerSerializer.Deserialize(list[i]), i);
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
                            data = new NbtSerializer(property.PropertyType, options).Deserialize(node);
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


    // Convenience class for working with strongly-typed NbtSerializers. Handy if type is known at compile time.
    public class NbtSerializer<T> : NbtSerializer {
        internal NbtSerializer()
            : base(typeof(T)) { }


        public NbtTag MakeTag(T obj) {
            return base.MakeTag(obj);
        }


        public T MakeObject(NbtTag tag) {
            return (T)base.MakeObject(tag);
        }
    }
}
