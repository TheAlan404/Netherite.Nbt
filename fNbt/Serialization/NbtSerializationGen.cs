// Uncomment this define to enable printing debug information to stdout
#define DEBUG_NBTSERIALIZE_COMPILER

// Hiding erroneous warnings -- see http://youtrack.jetbrains.com/issue/RSRP-333085
// ReSharper disable PossiblyMistakenUseOfParamsMethod
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;

#if DEBUG_NBTSERIALIZE_COMPILER
using System.Diagnostics;
#endif

namespace fNbt.Serialization {
    public delegate NbtCompound NbtSerialize<T>(string tagName, T value);


    internal class CallResolver {
        public CallResolver(Dictionary<Type,Expression> parentSerializers) {
            this.parentSerializers = parentSerializers;
        }


        readonly Dictionary<Type, Expression> parentSerializers;
        readonly Dictionary<Type,ParameterExpression> parameters = new Dictionary<Type, ParameterExpression>();


        public bool HasParameters {
            get { return parameters.Count > 0; }
        }


        public ParameterExpression[] GetParameterList() {
            return parameters.Values.ToArray();
        }


        public Expression[] GetParameterAssignmentList() {
            List<Expression> assignmentExprs = new List<Expression>();
            foreach (KeyValuePair<Type, ParameterExpression> param in parameters) {
                Expression val = Expression.Invoke(parentSerializers[param.Key]);
                assignmentExprs.Add(Expression.Assign(param.Value, val));
            }
            return assignmentExprs.ToArray();
        }


        public Expression MakeCall([NotNull] Type type, [CanBeNull] string tagName,
                                   [NotNull] Expression objectExpr) {
            Expression parentSerializer;
            if( parentSerializers.TryGetValue( type, out parentSerializer ) ) {
                // Dynamically resolved invoke -- for self-referencing/cross-referencing types
                // We delay resolving the correct NbtSerialize delegate for type, since that delegate is still
                // being created at this time. The resulting serialization code is a bit less efficient, but at least
                // it has the correct behavior.
                ParameterExpression paramExpr;
                if (!parameters.TryGetValue(type, out paramExpr)) {
                    Type delegateType = typeof(NbtSerialize<>).MakeGenericType(new[] { type });
                    paramExpr = Expression.Parameter(delegateType);
                    parameters.Add(type,paramExpr);
                }

                return Expression.Invoke(paramExpr,
                                         Expression.Constant(tagName, typeof(string)),
                                         objectExpr);

            } else {
                // Statically resolved invoke
                Delegate compoundSerializer = NbtSerializationGen.GetSerializerForType( type );
                MethodInfo invokeMethodInfo = compoundSerializer.GetType().GetMethod( "Invoke" );
                return Expression.Call( Expression.Constant( compoundSerializer ),
                                       invokeMethodInfo,
                                       Expression.Constant( tagName, typeof( string ) ), objectExpr );
            }
        }
    }


    public static class NbtSerializationGen {
        // NbtCompound.Add(NbtTag)
        static readonly MethodInfo NbtCompoundAddMethod =
            typeof(NbtCompound).GetMethod("Add", new[] { typeof(NbtTag) });

        // NbtList.Add(NbtTag)
        static readonly MethodInfo NbtListAddMethod =
            typeof(NbtList).GetMethod("Add", new[] { typeof(NbtTag) });

        // new ArgumentNullException(string)
        static readonly ConstructorInfo ArgumentNullExceptionCtor =
            typeof(ArgumentNullException).GetConstructor(new[] { typeof(string) });

        // new NullReferenceException(string)
        static readonly ConstructorInfo NullReferenceExceptionCtor =
            typeof(NullReferenceException).GetConstructor(new[] { typeof(string) });

        // new NbtCompound(string)
        static readonly ConstructorInfo NbtCompoundCtor =
            typeof(NbtCompound).GetConstructor(new[] { typeof(string) });

        // new NbtList(string,NbtTagType)
        static readonly ConstructorInfo NbtListCtor =
            typeof(NbtList).GetConstructor(new[] { typeof(string), typeof(NbtTagType) });

        // (string)null -- used to select appropriate constructor/method overloads when creating unnamed tags
        static readonly Expression NullStringExpr = Expression.Constant(null, typeof(string));

#if DEBUG_NBTSERIALIZE_COMPILER
        // Console.WriteLine(string)
        static readonly MethodInfo ConsoleWriteLineInfo =
            typeof(Console).GetMethod("WriteLine", new[] { typeof(string) });


        // Used for debugging
        [NotNull]
        static Expression MarkLineExpr([NotNull] string message) {
            if (message == null) throw new ArgumentNullException("message");
            var stackTrace = new StackTrace();
            StackFrame caller = stackTrace.GetFrame(1);
            string line = message + " @ " + caller.GetMethod().Name;
            return Expression.Call(ConsoleWriteLineInfo, Expression.Constant(line));
        }
#endif

        const string NullElementMessage = "Null elements not allowed inside a list.";


        #region Caching

        static readonly object CacheLock = new object();
        static readonly Dictionary<Type, Delegate> GlobalCache = new Dictionary<Type, Delegate>();


        [NotNull]
        public static NbtSerialize<T> GetSerializerForType<T>() {
            lock (CacheLock) {
                Delegate result;
                if (GlobalCache.TryGetValue(typeof(T), out result)) {
                    return (NbtSerialize<T>)result;
                }
                NbtSerialize<T> newResult = CreateSerializerForType<T>(null);
                GlobalCache.Add(typeof(T), newResult);
                return newResult;
            }
        }


        [NotNull]
        public static Delegate GetSerializerForType([NotNull] Type t) {
            if (t == null) throw new ArgumentNullException("t");
            lock (CacheLock) {
                Delegate result;
                if (!GlobalCache.TryGetValue(t, out result)) {
                    result = CreateSerializerForType(t);
                    GlobalCache.Add(t, result);
                }
                return result;
            }
        }

        #endregion


        // NbtSerializationGen.CreateSerializerForType<T>()
        static readonly MethodInfo CreateSerializerForTypeInfo =
            typeof(NbtSerializationGen).GetMethods().First(mi => mi.Name == "GetSerializerForType" && mi.IsGenericMethod);


        // create and invoke a variant of CreateSerializerForType<T> for given type
        [NotNull]
        static Delegate CreateSerializerForType([NotNull] Type t) {
            if (t == null) throw new ArgumentNullException("t");
            MethodInfo genericMethodToCall =
                CreateSerializerForTypeInfo.MakeGenericMethod(new[] { t });
            return (Delegate)genericMethodToCall.Invoke(null, new object[0]);
        }


        // Generates specialized methods for serializing objects of given Type to NBT
        [NotNull]
        static NbtSerialize<T> CreateSerializerForType<T>(Dictionary<Type, Expression> parentSerializers) {
            if (parentSerializers == null) {
                parentSerializers = new Dictionary<Type, Expression>();
            }

            // This allows our function to call itself, while it is still being built up.
            NbtSerialize<T> placeholderDelegate = null;
            // A closure is modified intentionally, at the end of this method:
            // placeholderDelegate will be replaced with reference to the compiled function.
            // ReSharper disable once AccessToModifiedClosure
            Expression<Func<NbtSerialize<T>>> placeholderExpr = () => placeholderDelegate;
            parentSerializers.Add(typeof(T), placeholderExpr);
            CallResolver cr = new CallResolver(parentSerializers);

            // Define function arguments
            ParameterExpression argTagName = Expression.Parameter(typeof(string), "tagName");
            ParameterExpression argValue = Expression.Parameter(typeof(T), "value");

            // Define return value
            ParameterExpression varRootTag = Expression.Parameter(typeof(NbtCompound), "rootTag");
            LabelTarget returnTarget = Expression.Label(typeof(NbtCompound));

            // Create property serializers
            List<Expression> propSerializersList =
                MakePropertySerializers(cr, typeof(T), argValue, varRootTag);

            if (cr.HasParameters) {
                propSerializersList.InsertRange(0, cr.GetParameterAssignmentList());
            }

            Expression serializersExpr;
            if (propSerializersList.Count == 0) {
                serializersExpr = Expression.Empty();
            } else if (propSerializersList.Count == 1) {
                serializersExpr = propSerializersList[0];
            } else {
                serializersExpr = Expression.Block(propSerializersList);
            }

            // Create function-wide variables -- includes root tag and serializer delegates
            List<ParameterExpression> vars = new List<ParameterExpression> {
                varRootTag
            };
            if (cr.HasParameters) {
                vars.AddRange(cr.GetParameterList());
            }

            // Construct the method body
            BlockExpression method = Expression.Block(
                vars,

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
                Expression.Return(returnTarget, varRootTag, typeof(NbtCompound)),
                Expression.Label(returnTarget, Expression.Constant(null, typeof(NbtCompound))));

            // compile
            Expression<NbtSerialize<T>> methodLambda =
                Expression.Lambda<NbtSerialize<T>>(method, argTagName, argValue);

#if DEBUG_NBTSERIALIZE_COMPILER
            // When in debug mode, print the expression tree to stdout.
            PropertyInfo propInfo =
                typeof(Expression)
                    .GetProperty("DebugView",
                                 BindingFlags.FlattenHierarchy | BindingFlags.NonPublic | BindingFlags.Instance);

            var debugView = (string)propInfo.GetValue(methodLambda, null);
            Console.WriteLine(debugView);
#endif

            NbtSerialize<T> compiledMethod = methodLambda.Compile();

            // modify the closure created earlier, to allow recursive calls
            placeholderDelegate = compiledMethod;

            // Now that method is compiled, indirection is no longer needed
            parentSerializers.Remove(typeof(T));
            return compiledMethod;
        }


        // Produces a list of expressions that, together, do the job of
        // producing all necessary NbtTags and adding them to the "varRootTag" compound tag.
        [NotNull]
        static List<Expression> MakePropertySerializers([NotNull] CallResolver parentSerializers,
                                                        [NotNull] Type type, [NotNull] ParameterExpression argValue,
                                                        [NotNull] ParameterExpression varRootTag) {
            var expressions = new List<Expression>();

            foreach (PropertyInfo property in GetSerializableProperties(type)) {
                Type propType = property.PropertyType;

#if DEBUG_NBTSERIALIZE_COMPILER
                expressions.Add(MarkLineExpr("Serializing " + property));
#endif

                // read tag name
                Attribute nameAttribute = Attribute.GetCustomAttribute(property, typeof(TagNameAttribute));
                string tagName;
                if (nameAttribute != null) {
                    tagName = ((TagNameAttribute)nameAttribute).Name;
                } else {
                    tagName = property.Name;
                }

                // read NullPolicy attribute
                var nullPolicyAttr =
                    (NullPolicyAttribute)Attribute.GetCustomAttribute(property, typeof(NullPolicyAttribute));
                var selfPolicy = NullPolicy.Default;
                var elementPolicy = NullPolicy.Default;
                if (nullPolicyAttr != null) {
                    selfPolicy = nullPolicyAttr.SelfPolicy;
                    elementPolicy = nullPolicyAttr.ElementPolicy;
                }

                // simple serialization for primitive types
                if (propType.IsPrimitive || propType.IsEnum) {
                    // Find a mapping from PropertyType to closest NBT equivalent
                    Type convertedType = GetConvertedType(property.PropertyType);

                    // property getter
                    Expression getPropertyExpr = Expression.MakeMemberAccess(argValue, property);

                    // create a new instance of the appropriate tag
                    Expression newTagExpr = MakeNbtTagCtor(convertedType, Expression.Constant(tagName, typeof(string)),
                                                           getPropertyExpr);
                    expressions.Add(Expression.Call(varRootTag, NbtCompoundAddMethod, newTagExpr));
                    continue;
                }

                // serialize reference types that map directly to NBT tag types
                if (SerializationUtil.IsDirectlyMappedType(propType)) {
                    Expression serializeStripPropExpr =
                        SerializePropertyDirectly(argValue, varRootTag, property, tagName, selfPolicy);
                    expressions.Add(serializeStripPropExpr);
                    continue;
                }

                // check if this type can handle its own serialization
                if (typeof(INbtSerializable).IsAssignableFrom(propType)) {
                    MethodInfo serializeMethod = propType.GetMethod("Serialize", new[] { typeof(string) });
                    Expression propValue = Expression.MakeMemberAccess(argValue, property);
                    Expression newTagExpr = Expression.Call(propValue, serializeMethod, Expression.Constant(tagName));
                    expressions.Add(Expression.Call(varRootTag, NbtCompoundAddMethod, newTagExpr));
                    continue;
                }

                // serialize something that implements IList<>
                Type iListImpl = SerializationUtil.GetGenericInterfaceImpl(propType, typeof(IList<>));
                if (iListImpl != null) {
                    Type elementType = iListImpl.GetGenericArguments()[0];
                    Expression getPropertyExpr = Expression.MakeMemberAccess(argValue, property);
                    Expression newExpr = SerializeIList(parentSerializers,getPropertyExpr, elementType, property.PropertyType,
                                                        tagName, selfPolicy, elementPolicy,
                                                        MakePropertyNullMessage(property),
                                                        expr => Expression.Call(varRootTag, NbtCompoundAddMethod, expr));
                    expressions.Add(newExpr);
                    continue;
                }

                // Skip serializing NbtTag properties
                if (typeof(NbtTag).IsAssignableFrom(propType)) {
                    // Add tag directly to the list
                    Expression newExpr =
                        MakeNbtTagHandler(argValue, varRootTag,
                                          property, tagName, selfPolicy,
                                          expr => expr);
                    expressions.Add(newExpr);
                    continue;
                }

                // Skip serializing NbtFile properties
                if (propType == typeof(NbtFile)) {
                    // Add NbtFile's root tag directly to the list
                    PropertyInfo rootTagProp = typeof(NbtFile).GetProperty("RootTag");
                    Expression newExpr =
                        MakeNbtTagHandler(argValue, varRootTag,
                                          property, tagName, selfPolicy,
                                          expr => Expression.MakeMemberAccess(expr, rootTagProp));
                    expressions.Add(newExpr);
                    continue;
                }

                // Compound expressions
                Expression newCompoundExpr =
                    MakeNbtTagHandler(argValue, varRootTag,
                                      property, tagName, selfPolicy,
                                      expr => parentSerializers.MakeCall(propType, tagName, expr));
                expressions.Add(newCompoundExpr);
            }
            return expressions;
        }


        // Creates expression that handles a property directly convertible to an NbtTag object.
        // Value of <property> should be convertible by <conversionFunc> to an expression that evaluates to an NbtTag object.
        // At run time, if the value is not null, the NbtTag is added to <varRootTag> (which is an NbtCompound).
        // Otherwise if value is null,
        // a) If NullPolicy=Error, a NullReferenceException is thrown
        // b) If NullPolicy=Ignore, nothing happens
        // c) If NullPolicy=InsertDefault, an empty NbtCompound tag is added to <varRootTag>
        [NotNull]
        static Expression MakeNbtTagHandler([NotNull] ParameterExpression argValue,
                                            [NotNull] ParameterExpression varRootTag, [NotNull] PropertyInfo property,
                                            [NotNull] string tagName, NullPolicy selfPolicy,
                                            [NotNull] Func<ParameterExpression, Expression> conversionFunc) {
            // declare a local var, which will hold the property's value
            ParameterExpression varValue = Expression.Parameter(property.PropertyType);

            // Primary path, adds the root tag of the NbtFile
            Expression makeTagExpr = Expression.Call(varRootTag, NbtCompoundAddMethod, conversionFunc(varValue));

            // Fallback path, in case value is null and NullPolicy is InsertDefaults
            Expression defaultVal = Expression.New(NbtCompoundCtor, Expression.Constant(tagName));
            Expression defaultValExpr = Expression.Call(varRootTag, NbtCompoundAddMethod, defaultVal);

            // Getter for the property value
            Expression getPropertyExpr = Expression.MakeMemberAccess(argValue, property);

            // Generate the appropriate enclosing expressions, which choose path depending on NullPolicy
            return MakeNullHandler(varValue,
                                   getPropertyExpr, selfPolicy, makeTagExpr,
                                   defaultValExpr, MakePropertyNullMessage(property));
        }


        // Creates serialization code for properties with value type that is an array or an IList<T>.
        // For byte[] and int[], use SerializePropertyDirectly(...) instead -- it's more efficient.
        [NotNull]
        static Expression SerializeIList(CallResolver parentSerializers,
                                         [NotNull] Expression getIListExpr, [NotNull] Type elementType,
                                         [NotNull] Type listType, [CanBeNull] string tagName,
                                         NullPolicy selfPolicy, NullPolicy elementPolicy,
                                         [NotNull] string nullMessage,
                                         [NotNull] Func<Expression, Expression> processTagExpr) {
            // Declare locals
            ParameterExpression varIList = Expression.Parameter(listType, "iList");
            ParameterExpression varListTag = Expression.Parameter(typeof(NbtList), "listTag");
            ParameterExpression varLength = Expression.Parameter(typeof(int), "length");
            ParameterExpression varIndex = Expression.Parameter(typeof(int), "i");

            // Find getter for this IList
            MethodInfo countGetterImpl, itemGetterImpl;

            if (listType.IsArray) {
                // Although Array claims to implement IList<>, there is no way to retrieve
                // the interface implementation: it's handled in an unusual way by the runtime.
                // So we have to resort to getting Length/GetValue instead of Count/Item
                countGetterImpl = listType.GetProperty("Length").GetGetMethod();
                itemGetterImpl = listType.GetMethod("GetValue", new[] { typeof(int) });
            } else {
                // For non-array IList<> types, grab this.Count getter (which maps to get_Count())
                countGetterImpl = SerializationUtil.GetGenericInterfaceMethodImpl(
                    listType,
                    typeof(ICollection<>),
                    "get_Count",
                    new Type[0]);
                // ...and the getter for indexer this[int], which maps to get_Item(int)
                itemGetterImpl = SerializationUtil.GetGenericInterfaceMethodImpl(
                    listType,
                    typeof(IList<>),
                    "get_Item",
                    new[] { typeof(int) });
            }
            Expression getElementExpr = Expression.Call(varIList, itemGetterImpl, varIndex);
            Expression getCountExpr = Expression.Call(varIList, countGetterImpl);

            Expression handleOneElementExpr;

            if (elementType.IsPrimitive || elementType.IsEnum) {
                //=== Serializing arrays/lists of primitives and enums ===
                // tag.Add( new NbtTag(null, <getElementExpr>) );
                handleOneElementExpr =
                    Expression.Call(varListTag,
                                    NbtListAddMethod,
                                    MakeNbtTagCtor(elementType, NullStringExpr, getElementExpr));

            } else if (SerializationUtil.IsDirectlyMappedType(elementType)) {
                //=== Serializing arrays/lists of directly-mapped reference types (byte[], int[], string) ===

                // declare a local var, which will hold the property's value
                ParameterExpression varValue = Expression.Parameter(elementType, "value");

                // Add conversion/casting logic, if needed
                getElementExpr = MakeConversionToDirectType(elementType, getElementExpr);

                // Primary path, in case element value is not null:
                // listTag.Add(new NbtTag(null, <getElementExpr>));
                Expression addElementExpr =
                    Expression.Call(varListTag,
                                    NbtListAddMethod,
                                    MakeNbtTagCtor(elementType, NullStringExpr, getElementExpr));

                // Fallback path, in case element value is null and elementPolicy is InsertDefaults:
                // Add a default-value tag to the list: listTag.Add(new NbtTag(null, <default>))
                Expression defaultElementExpr =
                    Expression.Call(varListTag,
                                    NbtListAddMethod,
                                    MakeNbtTagCtor(elementType,
                                                   NullStringExpr,
                                                   Expression.Constant(SerializationUtil.GetDefaultValue(elementType))));

                // generate the appropriate enclosing expressions, depending on NullPolicy
                handleOneElementExpr = MakeNullHandler(varValue,
                                                       getElementExpr,
                                                       elementPolicy,
                                                       addElementExpr,
                                                       defaultElementExpr,
                                                       NullElementMessage);
            } else {
                //=== Serializing arrays/lists of everything else ===

                // Check if this is an IList-of-ILists
                Type iListImpl = SerializationUtil.GetGenericInterfaceImpl(elementType, typeof(IList<>));
                bool elementIsIList = (iListImpl != null);
                getElementExpr = Expression.Convert(getElementExpr, elementType);

                if (elementIsIList) {
                    Type subElementType = iListImpl.GetGenericArguments()[0];
                    handleOneElementExpr =
                        SerializeIList(parentSerializers,
                                       getElementExpr,
                                       subElementType,
                                       elementType,
                                       null,
                                       elementPolicy,
                                       elementPolicy,
                                       NullElementMessage,
                                       subListExpr => Expression.Call(varListTag, NbtListAddMethod, subListExpr));
                } else {
                    // Get NbtSerialize<T> method for elementType
                    Expression makeElementTagExpr = parentSerializers.MakeCall(elementType, null, getElementExpr);

                    // declare a local var, which will hold the element's value
                    ParameterExpression varElementValue = Expression.Parameter(elementType, "elementValue");

                    // Primary path, adds the newly-made Compound tag to our list
                    Expression addSerializedCompoundExpr =
                        Expression.Call(varListTag, NbtListAddMethod, makeElementTagExpr);

                    // Fallback path, in case element's value is null and NullPolicy is InsertDefaults
                    Expression addEmptyCompoundExpr =
                        Expression.Call(varListTag, NbtListAddMethod, Expression.New(NbtCompoundCtor, NullStringExpr));

                    // Generate the appropriate enclosing expressions, which choose path depending on NullPolicy
                    handleOneElementExpr = MakeNullHandler(varElementValue,
                                                           getElementExpr,
                                                           elementPolicy,
                                                           addSerializedCompoundExpr,
                                                           addEmptyCompoundExpr,
                                                           NullElementMessage);
                }
            }

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

                        // <process and add one element to the list>
                        handleOneElementExpr,

                        // ++i;
                        Expression.PreIncrementAssign(varIndex)),
                    loopBreak);

            // new NbtList(tagName, NbtTagType.*)
            Expression makeListTagExpr =
                Expression.New(NbtListCtor,
                               Expression.Constant(tagName, typeof(string)),
                               Expression.Constant(GetNbtTagType(elementType)));

            // Fallback path, in case value our IList null and NullPolicy is InsertDefaults:
            // Add an empty list to root.
            Expression defaultValExpr = processTagExpr(makeListTagExpr);

            // Primary path, in case our IList is not null:
            // Package the list-building loop into a neat block, with locals
            Expression makeTagExpr = Expression.Block(
                new[] { varListTag, varIndex, varLength },

                // NbtList listTag = new NbtList(tagName, NbtTagType.*);
                Expression.Assign(varListTag, makeListTagExpr),

                // int length = iList.Count;
                Expression.Assign(varLength, getCountExpr),

                // int i=0;
                Expression.Assign(varIndex, Expression.Constant(0)),

                // (fill the list tag)
                mainLoop,

                // rootTag.Add( listTag );
                processTagExpr(varListTag));

            // Generate the appropriate enclosing expressions, which choose path depending on NullPolicy
            return MakeNullHandler(varIList, getIListExpr, selfPolicy,
                                   makeTagExpr, defaultValExpr, nullMessage);
        }


        // Creates an NbtTag constructor for given tag name and value expressions.
        // valueType must be a primitive or an enum. Casting and conversion are added as needed.
        [NotNull]
        static NewExpression MakeNbtTagCtor([NotNull] Type valueType, [NotNull] Expression tagNameExpr,
                                            [NotNull] Expression tagValueExpr) {
            if (!SerializationUtil.IsDirectlyMappedType(valueType)) {
                throw new ArgumentException("Given type must be primitive, enum, string, byte[], or int[]", "valueType");
            }

            // Add conversion logic, if needed
            tagValueExpr = MakeConversionToDirectType(valueType, tagValueExpr);

            // Find an NbtTag subtype for given type. Given type must be primitive or enum.
            // For example: byte -> NbtByte; int[] -> NbtIntArray, etc 
            Type elementTagType = SerializationUtil.TypeToTagMap[tagValueExpr.Type];

            // Find appropriate constructor
            ConstructorInfo tagCtor = elementTagType.GetConstructor(new[] { typeof(string), tagValueExpr.Type });
            // ReSharper disable once AssignNullToNotNullAttribute -- constructor will never be null
            return Expression.New(tagCtor, tagNameExpr, tagValueExpr);
        }


        // Perform any necessary conversion to go from tagValueExpr to closest directly-mapped value type
        [NotNull]
        static Expression MakeConversionToDirectType([NotNull] Type valueType, [NotNull] Expression tagValueExpr) {
            // Add casting/conversion, if needed
            Type convertedType = GetConvertedType(valueType);

            // boxed values returned by Array.GetValue() needs to be cast to bool first
            if (valueType != tagValueExpr.Type) {
                tagValueExpr = Expression.Convert(tagValueExpr, valueType);
            }

            if (valueType == typeof(bool)) {
                // Special handling for booleans: (<tagValueExpr> ? (byte)1 : (byte)0)
                return Expression.Condition(tagValueExpr,
                                            Expression.Constant((byte)1), Expression.Constant((byte)0));
            } else if (valueType != convertedType) {
                // special handling (casting) for enums and sbyte/ushort/char/uint/ulong/decimal
                return Expression.Convert(tagValueExpr, convertedType);
            } else {
                return tagValueExpr;
            }
        }


        // Finds an NBT primitive that is closest to the given type.
        // If given type must is not a primitive or enum, then the original type is returned.
        // For example: bool -> byte; char -> short, etc
        [NotNull]
        static Type GetConvertedType([NotNull] Type rawType) {
            if (rawType == null) throw new ArgumentNullException("rawType");
            if (rawType.IsEnum) {
                rawType = Enum.GetUnderlyingType(rawType);
            }

            Type convertedType;
            if (!SerializationUtil.PrimitiveConversionMap.TryGetValue(rawType, out convertedType)) {
                convertedType = rawType;
            }

            return convertedType;
        }


        // Finds a NbtTagType for given value type.
        // NbtTagType.Compound is returned for any value type that is not a primitive/enum/array/IList<T>
        // For example: int -> NbtTagType.Int; List<string> -> NbtTagType.List; etc
        static NbtTagType GetNbtTagType([NotNull] Type rawValueType) {
            Type convertedType = GetConvertedType(rawValueType);

            Type directTagType;
            if (SerializationUtil.TypeToTagMap.TryGetValue(convertedType, out directTagType)) {
                return SerializationUtil.TypeToTagTypeEnum[directTagType];
            }

            Type iListImpl = SerializationUtil.GetGenericInterfaceImpl(rawValueType, typeof(IList<>));
            if (iListImpl != null) {
                return NbtTagType.List;
            } else {
                return NbtTagType.Compound;
            }
        }


        // Generates an expression that creates an NbtTag for given property of a directly-mappable types.
        // Directly-mappable types are: primitives, enums, byte[], int[], and string.
        [NotNull]
        static Expression SerializePropertyDirectly([NotNull] ParameterExpression argValue,
                                                    [NotNull] ParameterExpression varRootTag,
                                                    [NotNull] PropertyInfo property, [NotNull] string tagName,
                                                    NullPolicy selfPolicy) {
            // declare a local var, which will hold the property's value
            ParameterExpression varValue = Expression.Parameter(property.PropertyType);

            // Fallback path, in case value is null and NullPolicy is InsertDefaults
            Expression defaultVal = Expression.Constant(SerializationUtil.GetDefaultValue(property.PropertyType));
            // varRootTag.Add( new NbtTag(tagName, <defaultVal>) );
            Expression defaultValExpr =
                Expression.Call(varRootTag, NbtCompoundAddMethod,
                                MakeNbtTagCtor(property.PropertyType,
                                               Expression.Constant(tagName, typeof(string)),
                                               defaultVal));

            // varRootTag.Add( new NbtTag(tagName, <varValue>) );
            Expression makeTagExpr =
                Expression.Call(varRootTag, NbtCompoundAddMethod,
                                MakeNbtTagCtor(property.PropertyType,
                                               Expression.Constant(tagName, typeof(string)),
                                               varValue));

            // Getter for the property value
            Expression getPropertyExpr = Expression.MakeMemberAccess(argValue, property);

            // generate the appropriate enclosing expressions, depending on NullPolicy
            return MakeNullHandler(varValue, getPropertyExpr, selfPolicy, makeTagExpr,
                                   defaultValExpr, MakePropertyNullMessage(property));
        }


        // Generate a message for a NullReferenceException to be thrown if given property's value is null
        [NotNull]
        static string MakePropertyNullMessage([NotNull] PropertyInfo prop) {
            return string.Format("Property {0}.{1} cannot be null.",
                                 // ReSharper disable once PossibleNullReferenceException
                                 prop.DeclaringType.Name, prop.Name);
        }


        // Generates and returns "glue" (enclosing expression) that combines the given expressions together based on given NullPolicy.
        // The returned expression does this:
        // 1) Evaluates <getPropertyExpr> and assigns it to <varValue>
        // 2) If value is non-null, evaluates <nonNullExpr>
        // 3) If value is null:
        //    a) if NullPolicy=Error, throws a NullReferenceException with given <exceptionMessage>
        //    b) if NullPolicy=Ignore, does nothing
        //    c) if NullPolicy=InsertDefault, evaluates <defaultValExpr>
        [NotNull]
        static Expression MakeNullHandler([NotNull] ParameterExpression varValue, [NotNull] Expression getPropertyExpr,
                                          NullPolicy policy, [NotNull] Expression nonNullExpr,
                                          [NotNull] Expression defaultValExpr, [NotNull] string exceptionMessage) {
            // locate the getter for this property
            Expression ifExpr;

            if (policy == NullPolicy.Error) {
                ifExpr = Expression.IfThenElse(
                    // if (value==null) throw new NullReferenceException(exceptionMessage)
                    Expression.ReferenceEqual(varValue, Expression.Constant(null)),
                    Expression.Throw(
                        Expression.New(NullReferenceExceptionCtor, Expression.Constant(exceptionMessage))),
                    // else <nonNullExpr>
                    nonNullExpr);

            } else if (policy == NullPolicy.Ignore) {
                ifExpr = Expression.IfThen(
                    // if (value!=null) <nonNullExpr>
                    Expression.Not(Expression.ReferenceEqual(varValue, Expression.Constant(null))),
                    nonNullExpr);

            } else if (policy == NullPolicy.InsertDefault) {
                ifExpr = Expression.IfThenElse(
                    // if (value==null) <defaultValExpr>
                    Expression.ReferenceEqual(varValue, Expression.Constant(null)),
                    defaultValExpr,
                    // else <nonNullExpr>
                    nonNullExpr);

            } else {
                throw new ArgumentOutOfRangeException("Unrecognized value for NullPolicy: " + policy);
            }

            return Expression.Block(
                // var varValue = value.ThisProperty;
                new[] { varValue },
                Expression.Assign(varValue, getPropertyExpr),
                // (check if value is null, and do something)
                ifExpr);
        }


        // Get a list of all serializable (readable, non-ignored, instance) properties for given type
        [NotNull]
        static IEnumerable<PropertyInfo> GetSerializableProperties([NotNull] Type type) {
            return type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy)
                       .Where(p => Attribute.GetCustomAttribute(p, typeof(NbtIgnoreAttribute)) == null)
                       .Where(p => p.CanRead)
                       .ToArray();
        }
    }
}
