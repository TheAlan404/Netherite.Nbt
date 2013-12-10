using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;

namespace fNbt.Serialization {
    public delegate NbtTag NbtSerialize<T>(string tagName, T value);


    public class NbtSerializationGen {
        // NbtCompound.Add(NbtTag)
        static readonly MethodInfo NbtCompoundAddMethod =
            typeof(NbtCompound).GetMethod("Add", new[] { typeof(NbtTag) });

        // NbtList.Add(NbtTag)
        static readonly MethodInfo NbtListAddMethod =
            typeof(NbtList).GetMethod("Add", new[] { typeof(NbtTag) });

        // new ArgumentNullException(string)
        static readonly ConstructorInfo ArgumentNullExceptionCtor =
            typeof( ArgumentNullException ).GetConstructor( new[] { typeof( string ) } );

        // new NullReferenceException(string)
        static readonly ConstructorInfo NullReferenceExceptionCtor =
            typeof( NullReferenceException ).GetConstructor( new[] { typeof( string ) } );

        // new NbtCompound(string)
        static readonly ConstructorInfo NbtCompoundCtor =
            typeof(NbtCompound).GetConstructor(new[] { typeof(string) });

        // new NbtList(string,NbtTagType)
        static readonly ConstructorInfo NbtListCtor =
            typeof(NbtList).GetConstructor(new[] { typeof(string), typeof(NbtTagType) });


        // Generates specialized methods for serializing objects of given Type to NBT
        [NotNull]
        public static NbtSerialize<T> CreateSerializerForType<T>() {
            // Define function arguments
            ParameterExpression argTagName = Expression.Parameter(typeof(string), "tagName");
            ParameterExpression argValue = Expression.Parameter(typeof(T), "value");

            // Define return value
            ParameterExpression varRootTag = Expression.Parameter(typeof(NbtCompound), "varRootTag");
            LabelTarget returnTarget = Expression.Label(typeof(NbtTag));

            // Create property serializers
            List<Expression> propSerializersList = MakePropertySerializers(typeof(T), argValue, varRootTag);
            Expression serializersExpr;
            if (propSerializersList.Count == 0) {
                serializersExpr = Expression.Empty();
            } else if (propSerializersList.Count == 1) {
                serializersExpr = propSerializersList[0];
            } else {
                serializersExpr = Expression.Block(propSerializersList);
            }

            // Construct the method body
            BlockExpression method = Expression.Block(
                new[] { varRootTag },

                // if( argValue == null )
                Expression.IfThen(
                    Expression.ReferenceEqual(argValue, Expression.Constant(null)),
                    //  throw new ArgumentNullException("value");
                    Expression.Throw(Expression.New(ArgumentNullExceptionCtor, Expression.Constant("value")))),

                // varRootTag = new NbtCompound(argTagName);
                Expression.Assign(varRootTag, Expression.New(NbtCompoundCtor, argTagName)),

                // (run the generated serializing code)
                serializersExpr,

                // return varRootTag;
                Expression.Return(returnTarget, varRootTag, typeof(NbtTag)),
                Expression.Label(returnTarget, Expression.Constant(null, typeof(NbtTag))));

            // compile
            return Expression.Lambda<NbtSerialize<T>>(method, argTagName, argValue).Compile();
        }


        // Produces a list of expressions that, together, do the job of
        // producing all necessary NbtTags and adding them to the "varRootTag" compound tag.
        [NotNull]
        static List<Expression> MakePropertySerializers(Type type,
                                                        ParameterExpression argValue,
                                                        ParameterExpression varRootTag) {
            var expressions = new List<Expression>();

            foreach( PropertyInfo property in GetSerializableProperties( type ) ) {
                Type propType = property.PropertyType;

                // read tag name
                Attribute nameAttribute = Attribute.GetCustomAttribute(property, typeof(TagNameAttribute));
                string tagName;
                if( nameAttribute != null) {
                    tagName = ((TagNameAttribute)nameAttribute).Name;
                } else {
                    tagName = property.Name;
                }

                // read NullPolicy attribute
                NullPolicyAttribute ignoreOnNullAttribute =
                    (NullPolicyAttribute)Attribute.GetCustomAttribute(property, typeof(NullPolicyAttribute));
                NullPolicy selfPolicy = NullPolicy.Default,
                           elementPolicy = NullPolicy.Default;
                if (ignoreOnNullAttribute != null) {
                    selfPolicy = ignoreOnNullAttribute.SelfPolicy;
                    elementPolicy = ignoreOnNullAttribute.ElementPolicy;
                }

                // simple serialization for primitive types
                if (propType.IsPrimitive) {
                    Expression newTagExpr = MakeTagForPrimitiveType(tagName, argValue, property);
                    expressions.Add(Expression.Call(varRootTag, NbtCompoundAddMethod, newTagExpr));
                    continue;
                }

                // serialize reference types that map directly to NBT tag types
                if (propType == typeof(string) ||
                    propType == typeof(byte[]) ||
                    propType == typeof(int[])) {
                    Expression serializeStripPropExpr =
                        SerializePropertyDirectly(argValue, varRootTag, property, tagName, selfPolicy);
                    expressions.Add(serializeStripPropExpr);
                    continue;
                }

                // check if this type can handle its own serialization
                if (propType.IsAssignableFrom(typeof(INbtSerializable))) {
                    MethodInfo serializeMethod = propType.GetMethod("Serialize", new[] { typeof(string) });
                    Expression propValue = Expression.MakeMemberAccess(argValue, property);
                    Expression newTagExpr = Expression.Call(propValue, serializeMethod, Expression.Constant(tagName));
                    expressions.Add(Expression.Call(varRootTag, NbtCompoundAddMethod, newTagExpr));
                    continue;
                }

                // serialize something that implements IList<>
                Type iListImpl = GetGenericInterfaceImpl(propType, typeof(IList<>));
                if (iListImpl != null) {
                    Type elementType = iListImpl.GetGenericArguments()[0];

                    if (elementType.IsPrimitive) {
                        expressions.Add(SerializeIListOfPrimitives(elementType,
                                                                   argValue, varRootTag,
                                                                   property, tagName));
                    } else {
                        throw new NotImplementedException("TODO: ILists of non-primitives");
                    }
                    continue;
                }

                // Skip serializing NbtTag properties
                if (propType.IsAssignableFrom(typeof(NbtTag))) {
                    // TODO
                    throw new NotImplementedException("TODO: NbtTag");
                }

                // Skip serializing NbtFile properties
                if (propType == typeof(NbtFile)) {
                    Expression propValue = Expression.MakeMemberAccess(argValue, property);
                    PropertyInfo fileRoot = typeof(NbtFile).GetProperty("RootTag");
                    Expression fileRootGetterExpr = Expression.MakeMemberAccess(propValue, fileRoot);
                    expressions.Add(Expression.Call(varRootTag, NbtCompoundAddMethod, fileRootGetterExpr));
                    continue;
                }

                // TODO: treat property as a compound tag
            }
            return expressions;
        }


        [CanBeNull]
        static Type GetGenericInterfaceImpl(Type concreteType, Type genericInterface) {
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
        }


        [CanBeNull]
        static MethodInfo GetGenericInterfaceMethodImpl(Type concreteType, Type genericInterface, string methodName,
                                                        Type[] methodParams) {
            // Find a specific "flavor" of the implementation
            Type impl = GetGenericInterfaceImpl(concreteType, genericInterface);
            if (impl == null) {
                throw new ArgumentException(concreteType + " does not implement " + genericInterface);
            }

            MethodInfo interfaceMethod = impl.GetMethod(methodName, methodParams);
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

                if (interfaceMethod == null) {
                    throw new ArgumentException(genericInterface + " does not contain method " + methodName);
                }

                int methodIndex = Array.IndexOf(implMap.InterfaceMethods, interfaceMethod);
                MethodInfo concreteMethod = implMap.TargetMethods[methodIndex];
                return concreteMethod;
            }
        }


        static Expression SerializeIListOfPrimitives(Type elementType,
                                                     ParameterExpression argValue,
                                                     ParameterExpression varRootTag,
                                                     PropertyInfo property,
                                                     string tagName) {
            // Declare locals
            ParameterExpression varIList = Expression.Parameter(property.PropertyType);
            ParameterExpression varListTag = Expression.Parameter(typeof(NbtList));
            ParameterExpression varLength = Expression.Parameter(typeof(int));
            ParameterExpression varIndex = Expression.Parameter(typeof(int));

            // Find getter for this IList
            Expression getPropertyExpr = Expression.MakeMemberAccess(argValue, property);
            MethodInfo countGetterImpl, itemGetterImpl;

            if (property.PropertyType.IsArray) {
                // Although Array claims to implement IList<>, there is no way to retrieve
                // the interface implementation: it's handled in an unusual way by the runtime.
                // So we have to resort to getting Length/GetValue instead of Count/Item
                countGetterImpl = property.PropertyType.GetProperty("Length").GetGetMethod();
                itemGetterImpl = property.PropertyType.GetMethod("GetValue", new[] { typeof(int) });

            } else {
                // For non-array IList<> types, grab this.Count getter (which maps to get_Count())
                countGetterImpl = GetGenericInterfaceMethodImpl(
                    property.PropertyType, typeof(ICollection<>), "get_Count", new Type[0]);
                // ...and the getter for indexer this[int], which maps to get_Item(int)
                itemGetterImpl = GetGenericInterfaceMethodImpl(
                    property.PropertyType, typeof(IList<>), "get_Item", new[] { typeof(int) });
            }
            Expression getElementExpr = Expression.Call(varIList, itemGetterImpl, varIndex);
            Expression getCountExpr = Expression.Call(varIList, countGetterImpl);

            // Find the correct NbtTag type for elements
            Type convertedType;
            if (!SerializationUtil.PrimitiveConversionMap.TryGetValue(elementType, out convertedType)) {
                convertedType = elementType;
            }
            Type elementTagType = SerializationUtil.TypeToTagMap[convertedType];

            // Handle element type conversion
            if (elementType == typeof(bool)) {
                // bools returned from Array.GetValue() need to be cast
                if (getElementExpr.Type != typeof(bool)) {
                    getElementExpr = Expression.Convert(getElementExpr, typeof(bool));
                }
                // special handling for bool-to-byte
                getElementExpr = Expression.Condition(getElementExpr,
                                                      Expression.Constant((byte)1), Expression.Constant((byte)0));
            } else if (elementType != convertedType || getElementExpr.Type != elementType) {
                // Special handling (casting) for sbyte/ushort/char/uint/ulong/decimal
                // Also, anything returned by Array.GetValue() needs to be cast
                getElementExpr = Expression.Convert(getElementExpr, convertedType);
            }

            // create "new NbtTag(...)" expression
            ConstructorInfo tagCtor = elementTagType.GetConstructor(new[] { typeof(string), convertedType });
            Expression constructElementTagExpr = Expression.New(tagCtor,
                                                                Expression.Constant(null, typeof(string)),
                                                                getElementExpr);

            // Arrange tag construction in a loop
            LabelTarget loopBreak = Expression.Label(typeof(void));
            Expression mainLoop = 
                // while (true)
                Expression.Loop(
                    Expression.Block(
                        // if (i >= length) break;
                        Expression.IfThen(
                            Expression.GreaterThanOrEqual(varIndex, varLength),
                            Expression.Break(loopBreak)),

                        // tag.Add( new NbtTag(...) );
                        Expression.Call(varListTag, NbtListAddMethod, constructElementTagExpr),

                        // ++i;
                        Expression.PreIncrementAssign(varIndex)),
                    loopBreak);

            // Package everything together into a neat block, with locals
            return Expression.Block(
                new[] { varIList, varListTag, varIndex, varLength },

                // IList<> iList = value.ThisProperty;
                Expression.Assign(varIList, getPropertyExpr),

                // NbtList listTag = new NbtList(tagName, NbtTagType.*);
                Expression.Assign(
                    varListTag,
                    Expression.New(NbtListCtor,
                                   Expression.Constant(tagName),
                                   Expression.Constant( SerializationUtil.TypeToTagTypeEnum[elementTagType] ) ) ),

                // int length = iList.Count;
                Expression.Assign( varLength, getCountExpr),

                // int i=0;
                Expression.Assign(varIndex, Expression.Constant(0)),

                // (fill the list tag)
                mainLoop,

                // rootTag.Add( listTag );
                Expression.Call(varRootTag, NbtCompoundAddMethod, varListTag));
        }


        // Creates a NbtString tag for given string property.
        // If the property's value is null, we either...
        // 1) Skip creating this tag (if ignoreOnNull is true), or
        // 2) Create a tag with empty-string value
        [NotNull]
        static Expression SerializePropertyDirectly(ParameterExpression argValue,
                                                    ParameterExpression varRootTag,
                                                    PropertyInfo property,
                                                    string tagName,
                                                    NullPolicy selfPolicy) {
            // locate the getter for this property
            Expression getPropertyExpr = Expression.MakeMemberAccess(argValue, property);

            Type tagType = SerializationUtil.TypeToTagMap[property.PropertyType];
            ConstructorInfo tagCtor = tagType.GetConstructor(new[] { typeof(string), property.PropertyType });

            if (selfPolicy == NullPolicy.Error || selfPolicy == NullPolicy.Ignore) {
                // declare a local var, which will hold the property's value
                ParameterExpression varValue = Expression.Parameter(property.PropertyType);

                // varRootTag.Add( new NbtString(tagName, varValue) );
                Expression makeTagExpr =
                    Expression.Call(
                        varRootTag,
                        NbtCompoundAddMethod,
                        Expression.New(tagCtor, Expression.Constant(tagName), varValue));

                Expression ifExpr;

                if (selfPolicy == NullPolicy.Error) {
                    string exceptionMessage = "Property " + property.Name + " cannot be null.";
                    // if(value==null)
                    ifExpr =
                        Expression.IfThenElse(
                            Expression.ReferenceEqual(varValue, Expression.Constant(null)),
                            // throw new NullReferenceExeption(...)
                            Expression.Throw(
                                Expression.New(NullReferenceExceptionCtor,
                                               Expression.Constant(exceptionMessage))),
                            // else (add a tag)
                            makeTagExpr);
                } else {
                    // if(varValue != null)
                    ifExpr =
                        Expression.IfThen(
                            Expression.Not(Expression.ReferenceEqual(varValue, Expression.Constant(null))),
                            // (add a tag)
                            makeTagExpr);
                }

                return Expression.Block(
                    // var varValue = argValue.ThisProperty;
                    new[] { varValue },
                    Expression.Assign(varValue, getPropertyExpr),
                    // (check if value is null, and do something)
                    ifExpr);

            } else if (selfPolicy == NullPolicy.InsertDefault) {
                Expression defaultVal = Expression.Constant( SerializationUtil.GetDefaultValue( property.PropertyType ) );

                // varRootTag.Add( new NbtString(tagName, varValue.ThisProperty ?? defaultVal) )
                return Expression.Call(
                    varRootTag,
                    NbtCompoundAddMethod,
                    Expression.New(
                        tagCtor,
                        Expression.Constant(tagName),
                        Expression.Coalesce(getPropertyExpr, defaultVal)));

            } else {
                throw new ArgumentOutOfRangeException("Unrecognized value for NullPolicy: " + selfPolicy);
            }
        }



        // Creates a tag constructor for given primitive-type property
        [NotNull]
        static NewExpression MakeTagForPrimitiveType(string tagName, Expression argValue, PropertyInfo property) {
            // check if conversion is necessary
            Type convertedType;
            if( !SerializationUtil.PrimitiveConversionMap.TryGetValue( property.PropertyType, out convertedType ) ) {
                convertedType = property.PropertyType;
            }

            // find the tag constructor
            Type tagType = SerializationUtil.TypeToTagMap[convertedType];
            ConstructorInfo tagCtor = tagType.GetConstructor(new[] { typeof(string), convertedType });

            // add a cast, if needed
            Expression getPropertyExpr = Expression.MakeMemberAccess(argValue, property);
            if (property.PropertyType == typeof(bool)) {
                // special handling for bool-to-byte
                getPropertyExpr = Expression.Condition(Expression.IsTrue(getPropertyExpr),
                                                       Expression.Constant((byte)1), Expression.Constant((byte)0));
            } else if (property.PropertyType != convertedType) {
                // special handling (casting) for sbyte/ushort/char/uint/ulong/decimal
                getPropertyExpr = Expression.Convert(getPropertyExpr, convertedType);
            }

            // create a new instance of the appropriate tag
            return Expression.New(tagCtor, Expression.Constant(tagName), getPropertyExpr);
        }


        // Get a list of all serializable (readable, non-ignored, instance) properties for given type
        [NotNull]
        static IEnumerable<PropertyInfo> GetSerializableProperties(Type type) {
            return type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy)
                       .Where(p => !Attribute.GetCustomAttributes(p, typeof(NbtIgnoreAttribute)).Any())
                       .Where(p => p.CanRead)
                       .ToArray();
        }
    }
}
