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


    public static class NbtCompiler {
        // new ArgumentNullException(string)
        internal static readonly ConstructorInfo ArgumentNullExceptionCtor =
            typeof(ArgumentNullException).GetConstructor(new[] { typeof(string) });

        // new NullReferenceException(string)
        internal static readonly ConstructorInfo NullReferenceExceptionCtor =
            typeof(NullReferenceException).GetConstructor(new[] { typeof(string) });

        // (string)null -- used to select appropriate constructor/method overloads when creating unnamed tags
        internal static readonly Expression NullStringExpr = Expression.Constant( null, typeof( string ) );

#if DEBUG_NBTSERIALIZE_COMPILER
        // Console.WriteLine(string)
        static readonly MethodInfo ConsoleWriteLineInfo =
            typeof(Console).GetMethod("WriteLine", new[] { typeof(string) });


        // Used for debugging
        [NotNull]
        internal static Expression MarkLineExpr([NotNull] string message) {
            if (message == null) throw new ArgumentNullException("message");
            var stackTrace = new StackTrace();
            StackFrame caller = stackTrace.GetFrame(1);
            string line = message + " @ " + caller.GetMethod().Name;
            return Expression.Call(ConsoleWriteLineInfo, Expression.Constant(line));
        }
#endif



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
                NbtSerialize<T> newResult = CreateSerializerForType<T>();
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
                }
                return result;
            }
        }

        #endregion


        static readonly Dictionary<Type, Expression> ParentSerializers = new Dictionary<Type, Expression>();

        // NbtCompiler.CreateSerializerForType<T>()
        static readonly MethodInfo CreateSerializerForTypeInfo =
            typeof(NbtCompiler).GetMethods()
                                       .First(mi => mi.Name == "GetSerializerForType" && mi.IsGenericMethod);


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
        static NbtSerialize<T> CreateSerializerForType<T>() {
            // This allows our function to call itself, while it is still being built up.
            NbtSerialize<T> placeholderDelegate = null;
            // A closure is modified intentionally, at the end of this method:
            // placeholderDelegate will be replaced with reference to the compiled function.
            // ReSharper disable once AccessToModifiedClosure
            Expression<Func<NbtSerialize<T>>> placeholderExpr = () => placeholderDelegate;
            ParentSerializers.Add(typeof(T), placeholderExpr);
            try {
                // Define function arguments
                ParameterExpression argTagName = Expression.Parameter(typeof(string), "tagName");
                ParameterExpression argValue = Expression.Parameter(typeof(T), "value");

                // Define return value
                ParameterExpression varRootTag = Expression.Parameter(typeof(NbtCompound), "rootTag");
                LabelTarget returnTarget = Expression.Label(typeof(NbtCompound));

                var cr = new CallResolver();
                CodeEmitter emitter = new SerializeCodeEmitter( typeof( T ), argTagName, argValue, cr );

                // Create property serializers
                List<Expression> propSerializersList =
                    MakePropertySerializers( emitter, cr, typeof( T ), argValue, varRootTag );

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
                var vars = new List<ParameterExpression> {
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

                    emitter.GetPreamble(),

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

                GlobalCache.Add(typeof(T), compiledMethod);
                return compiledMethod;
            } finally {
                ParentSerializers.Remove(typeof(T));
            }
        }


        // Produces a list of expressions that, together, do the job of
        // producing all necessary NbtTags and adding them to the "varRootTag" compound tag.
        [NotNull]
        static List<Expression> MakePropertySerializers(
            [NotNull] CodeEmitter codeEmitter,
            [NotNull] CallResolver callResolver,
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
                    expressions.Add(codeEmitter.HandlePrimitiveOrEnum(tagName, property));
                    continue;
                }

                // serialize reference types that map directly to NBT tag types
                if (SerializationUtil.IsDirectlyMappedType(propType)) {
                    expressions.Add(codeEmitter.HandleDirectlyMappedType(tagName,property,selfPolicy));
                    continue;
                }

                // check if this type can handle its own serialization
                if (typeof(INbtSerializable).IsAssignableFrom(propType)) {
                    expressions.Add(codeEmitter.HandleINbtSerializable(tagName, property));
                    continue;
                }

                // serialize something that implements IList<>
                Type iListImpl = SerializationUtil.GetGenericInterfaceImpl(propType, typeof(IList<>));
                if (iListImpl != null) {
                    expressions.Add(codeEmitter.HandleIList(tagName,property,selfPolicy,elementPolicy));
                    continue;
                }

                // Skip serializing NbtTag properties
                if (typeof(NbtTag).IsAssignableFrom(propType)) {
                    expressions.Add(codeEmitter.HandleNbtTag(tagName, property, selfPolicy));
                    continue;
                }

                // Skip serializing NbtFile properties
                if (propType == typeof(NbtFile)) {
                    expressions.Add(codeEmitter.HandleNbtFile(tagName, property, selfPolicy));
                    continue;
                }

                // Compound expressions
                expressions.Add(codeEmitter.HandleCompoundObject(tagName,property,selfPolicy));
            }
            return expressions;
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
        internal static Expression MakeNullHandler([NotNull] ParameterExpression varValue, [NotNull] Expression getPropertyExpr,
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


        internal class CallResolver {
            readonly Dictionary<Type, ParameterExpression> parameters = new Dictionary<Type, ParameterExpression>();


            public bool HasParameters {
                get { return parameters.Count > 0; }
            }


            public IEnumerable<ParameterExpression> GetParameterList() {
                return parameters.Values;
            }


            public IEnumerable<Expression> GetParameterAssignmentList() {
                var assignmentExprs = new List<Expression>();
                foreach( KeyValuePair<Type, ParameterExpression> param in parameters ) {
                    Expression val = Expression.Invoke( ParentSerializers[param.Key] );
                    assignmentExprs.Add( Expression.Assign( param.Value, val ) );
                }
                return assignmentExprs;
            }


            [NotNull]
            public Expression MakeCall( [NotNull] Type type, [CanBeNull] string tagName,
                                        [NotNull] Expression objectExpr ) {
                Expression serializerExpr;
                if( ParentSerializers.TryGetValue( type, out serializerExpr ) ) {
                    // Dynamically resolved invoke -- for self-referencing/cross-referencing types
                    // We delay resolving the correct NbtSerialize delegate for type, since that delegate is still
                    // being created at this time. The resulting serialization code is a bit less efficient, but at least
                    // it has the correct behavior.
                    ParameterExpression paramExpr;
                    if( !parameters.TryGetValue( type, out paramExpr ) ) {
                        Type delegateType = typeof( NbtSerialize<> ).MakeGenericType( new[] { type } );
                        paramExpr = Expression.Parameter( delegateType, "serializerFor" + type.Name );
                        parameters.Add( type, paramExpr );
                    }

                    return Expression.Invoke( paramExpr,
                                             Expression.Constant( tagName, typeof( string ) ),
                                             objectExpr );
                } else {
                    // Statically resolved invoke
                    Delegate compoundSerializer = NbtCompiler.GetSerializerForType( type );
                    MethodInfo invokeMethodInfo = compoundSerializer.GetType().GetMethod( "Invoke" );
                    return Expression.Call( Expression.Constant( compoundSerializer ),
                                           invokeMethodInfo,
                                           Expression.Constant( tagName, typeof( string ) ), objectExpr );
                }
            }
        }
    }
}
