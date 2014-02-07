using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;

namespace fNbt.Serialization {
    internal static class SerializationUtil {
        // Gets default value for directly-mapped reference types, to substitute a null
        public static object GetDefaultValue(Type type) {
            if (type == typeof(string)) {
                return String.Empty;
            } else if (type == typeof(int[])) {
                return new int[0];
            } else if (type == typeof(byte[])) {
                return new byte[0];
            } else if (type.IsArray) {
                return Activator.CreateInstance(type);
            } else {
                throw new ArgumentException();
            }
        }


        public static bool IsDirectlyMappedType(Type type) {
            return type.IsPrimitive || type.IsEnum ||
                   type == typeof(byte[]) ||
                   type == typeof(int[]) ||
                   type == typeof(string);
        }


        // mapping of directly-usable types to their NbtTag subtypes
        public static readonly Dictionary<Type, Type> TypeToTagMap = new Dictionary<Type, Type> {
            { typeof(byte), typeof(NbtByte) },
            { typeof(short), typeof(NbtShort) },
            { typeof(int), typeof(NbtInt) },
            { typeof(long), typeof(NbtLong) },
            { typeof(float), typeof(NbtFloat) },
            { typeof(double), typeof(NbtDouble) },
            { typeof(byte[]), typeof(NbtByteArray) },
            { typeof(int[]), typeof(NbtIntArray) },
            { typeof(string), typeof(NbtString) }
        };


        public static readonly Dictionary<Type, NbtTagType> TypeToTagTypeEnum = new Dictionary<Type, NbtTagType> {
            { typeof(NbtByte), NbtTagType.Byte },
            { typeof(NbtByteArray), NbtTagType.ByteArray },
            { typeof(NbtDouble), NbtTagType.Double },
            { typeof(NbtFloat), NbtTagType.Float },
            { typeof(NbtInt), NbtTagType.Int },
            { typeof(NbtIntArray), NbtTagType.IntArray },
            { typeof(NbtLong), NbtTagType.Long },
            { typeof(NbtShort), NbtTagType.Short },
            { typeof(NbtString), NbtTagType.String },
            { typeof(NbtCompound), NbtTagType.Compound },
            { typeof(NbtList), NbtTagType.List }
        };


        // mapping of convertible value types to directly-usable primitive types
        public static readonly Dictionary<Type, Type> PrimitiveConversionMap = new Dictionary<Type, Type> {
            { typeof(bool), typeof(byte) },
            { typeof(sbyte), typeof(byte) },
            { typeof(ushort), typeof(short) },
            { typeof(char), typeof(short) },
            { typeof(uint), typeof(int) },
            { typeof(ulong), typeof(long) },
            { typeof(decimal), typeof(double) }
        };


        [CanBeNull]
        public static Type GetGenericInterfaceImpl(Type concreteType, Type genericInterface) {
            if (genericInterface.IsGenericTypeDefinition) {
                if (concreteType.IsGenericType && concreteType.GetGenericTypeDefinition() == genericInterface) {
                    // concreteType itself is the desired generic interface
                    return concreteType;
                } else {
                    // Check if concreteType implements the desired generic interface ONCE
                    // Double implementations (e.g. Foo : Bar<T1>, Bar<T2>) are not acceptable.
                    return concreteType.GetInterfaces()
                                       .SingleOrDefault(x => x.IsGenericType &&
                                                             x.GetGenericTypeDefinition() == genericInterface);
                }
            } else {
                return genericInterface;
            }
        }


        public static Type GetStringIDictionaryImpl( Type concreteType ) {
            return concreteType.GetInterfaces().FirstOrDefault(
                iFace => iFace.IsGenericType &&
                         iFace.GetGenericTypeDefinition() == typeof(IDictionary<,>) &&
                         iFace.GetGenericArguments()[0] == typeof(string));
        }


        [NotNull]
        public static MethodInfo GetGenericInterfaceMethodImpl(Type concreteType, Type genericInterface,
                                                               string methodName, Type[] methodParams) {
            // Find a specific generic implementation of the interface
            Type impl = GetGenericInterfaceImpl(concreteType, genericInterface);
            if (impl == null) {
                throw new ArgumentException(concreteType + " does not implement " + genericInterface);
            }

            MethodInfo interfaceMethod = impl.GetMethod(methodName, methodParams);
            if (interfaceMethod == null) {
                throw new ArgumentException(genericInterface + " does not contain method " + methodName);
            }

            if (impl.IsInterface) {
                // if concreteType is itself an interface (e.g. IList<> implements ICollection<>),
                // We don't need to look up the interface implementation map. We can just return
                // the interface's method directly.
                return interfaceMethod;
            } else {
                // If concreteType is a class, we need to get a MethodInfo for its specific implementation.
                // We cannot just call "GetMethod()" on the concreteType, because explicit implementations
                // may cause ambiguity.
                InterfaceMapping implMap = concreteType.GetInterfaceMap(impl);

                int methodIndex = Array.IndexOf(implMap.InterfaceMethods, interfaceMethod);
                MethodInfo concreteMethod = implMap.TargetMethods[methodIndex];
                return concreteMethod;
            }
        }
    }
}
