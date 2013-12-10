using System;
using System.Collections.Generic;

namespace fNbt.Serialization {
    static class SerializationUtil {
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
            return type == typeof(byte[]) ||
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
            { typeof(NbtByte), NbtTagType.Byte},
            { typeof(NbtByteArray), NbtTagType.ByteArray},
            { typeof(NbtDouble), NbtTagType.Double},
            { typeof(NbtFloat), NbtTagType.Float},
            { typeof(NbtInt), NbtTagType.Int},
            { typeof(NbtIntArray), NbtTagType.IntArray},
            { typeof(NbtLong), NbtTagType.Long},
            { typeof(NbtShort), NbtTagType.Short},
            { typeof(NbtString), NbtTagType.String}
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
    }
}
