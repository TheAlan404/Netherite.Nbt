using System;

namespace fNbt.Serialization {
    public static class NbtConvert {
        //==== OBJECT TO TAG ==================================================

        public static NbtTag MakeTag<T>(T obj, string tagName) {
            return MakeTag(typeof(T), obj, tagName, SerializerOptions.Defaults);
        }


        public static NbtTag MakeTag<T>(T obj, string tagName, SerializerOptions options) {
            return MakeTag(typeof(T), obj, tagName, options);
        }


        public static NbtTag MakeTag(Type type, object obj, string tagName) {
            return MakeTag(type, obj, tagName, SerializerOptions.Defaults);
        }


        public static NbtTag MakeTag(Type type, object obj, string tagName, SerializerOptions options) {
            NbtTag tag = SerializationUtil.ConstructTag(type);
            return FillTag(type, obj, tag, options);
        }


        public static NbtTag FillTag<T>(T obj, NbtTag tag) {
            return FillTag(typeof(T), obj, tag, SerializerOptions.Defaults);
        }


        public static NbtTag FillTag<T>(T obj, NbtTag tag, SerializerOptions options) {
            return FillTag(typeof(T), obj, tag, options);
        }


        public static NbtTag FillTag(Type type, object obj, NbtTag tag) {
            return FillTag(type, obj, tag, SerializerOptions.Defaults);
        }


        public static NbtTag FillTag(Type type, object obj, NbtTag tag, SerializerOptions options) {
            new DynamicConverter(type, options).FillTag(obj, tag);
            return tag;
        }


        //==== TAG TO OBJECT ==================================================

        public static T MakeObject<T>(NbtTag tag) {
            return (T)MakeObject(typeof(T), tag, SerializerOptions.Defaults);
        }


        public static T MakeObject<T>(NbtTag tag, SerializerOptions options) {
            return (T)MakeObject(typeof(T), tag, options);
        }


        public static object MakeObject(Type type, NbtTag tag) {
            return MakeObject(type, tag, SerializerOptions.Defaults);
        }


        public static object MakeObject(Type type, NbtTag tag, SerializerOptions options) {
            object instance = Activator.CreateInstance(type);
            return FillObject(type, instance, tag, options);
        }


        public static T FillObject<T>(NbtTag tag, T obj) {
            return (T)FillObject(typeof(T), obj, tag, SerializerOptions.Defaults);
        }


        public static T FillObject<T>(NbtTag tag, T obj, SerializerOptions options) {
            return (T)FillObject(typeof(T), obj, tag, options);
        }


        public static object FillObject(Type type, NbtTag tag, object obj) {
            return FillObject(type, obj, tag, SerializerOptions.Defaults);
        }


        public static object FillObject(Type type, object obj, NbtTag tag, SerializerOptions options) {
            new DynamicConverter(type, options).FillObject(obj, tag);
            return obj;
        }


        //==== MAKING CONVERTERS ==================================================

        public static NbtConverter<T> MakeConverter<T>() {
            return new NbtConverter<T>();
        }


        public static NbtConverter MakeConverter(Type valueType) {
            return new NbtConverter(valueType);
        }
    }
}
