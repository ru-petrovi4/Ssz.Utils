using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace Ssz.Utils
{    
    public abstract class DynamicClass
    {
        #region public functions

        public override string ToString()
        {
            PropertyInfo[] props = GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
            var sb = new StringBuilder();
            sb.Append("{");
            for (int i = 0; i < props.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(props[i].Name);
                sb.Append("=");
                sb.Append(props[i].GetValue(this, null));
            }
            sb.Append("}");
            return sb.ToString();
        }

        #endregion
    }

    public class DynamicProperty
    {
        #region construction and destruction

        public DynamicProperty(string name, Type type)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (type == null) throw new ArgumentNullException("type");
            _name = name;
            _type = type;
        }

        #endregion

        #region public functions

        public string Name
        {
            get { return _name; }
        }

        public Type Type
        {
            get { return _type; }
        }

        #endregion

        #region private fields

        private readonly string _name;
        private readonly Type _type;

        #endregion
    }

    public static class DynamicExpression
    {
        #region public functions

        /// <summary>
        ///     Preconditions: predefinedTypes != Null.
        /// </summary>
        /// <param name="predefinedTypes"></param>
        public static void Initialize(Type[] predefinedTypes)
        {
            if (predefinedTypes == null) throw new ArgumentNullException(@"predefinedTypes");

            ExpressionParser.PredefinedTypes = ExpressionParser.PredefinedTypes.Concat(predefinedTypes).ToArray();
        }

        public static Expression Parse(Type resultType, string expression, params object[] values)
        {
            var parser = new ExpressionParser(null, expression, values);
            return parser.Parse(resultType);
        }

        public static LambdaExpression ParseLambda(Type itType, Type resultType, string expression,
            params object[] values)
        {
            return ParseLambda(new[] { Expression.Parameter(itType, "") }, resultType, expression, values);
        }

        public static LambdaExpression ParseLambda(ParameterExpression[] parameters, Type? resultType, string expression,
            params object[] values)
        {
            var parser = new ExpressionParser(parameters, expression, values);
            return Expression.Lambda(parser.Parse(resultType), parameters);
        }

        public static Expression<Func<T, S>> ParseLambda<T, S>(string expression, params object[] values)
        {
            return (Expression<Func<T, S>>)ParseLambda(typeof(T), typeof(S), expression, values);
        }

        public static Type CreateClass(params DynamicProperty[] properties)
        {
            return ClassFactory.Instance.GetDynamicClass(properties);
        }

        public static Type CreateClass(IEnumerable<DynamicProperty> properties)
        {
            return ClassFactory.Instance.GetDynamicClass(properties);
        }

        #endregion
    }

    internal class DynamicOrdering
    {
        #region public functions

        public Expression? Selector;
        public bool Ascending;

        #endregion
    }

    internal class Signature : IEquatable<Signature>
    {
        #region construction and destruction

        public Signature(IEnumerable<DynamicProperty> properties)
        {
            Properties = properties.ToArray();
            HashCode = 0;
            foreach (DynamicProperty p in Properties)
            {
                HashCode ^= p.Name.GetHashCode() ^ p.Type.GetHashCode();
            }
        }

        #endregion

        #region public functions

        public DynamicProperty[] Properties;
        public readonly int HashCode;

        public override int GetHashCode()
        {
            return HashCode;
        }

        public override bool Equals(object? obj)
        {
            var other = obj as Signature;
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other);
        }

        public bool Equals(Signature? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (Properties.Length != other.Properties.Length) return false;
            for (int i = 0; i < Properties.Length; i++)
            {
                if (Properties[i].Name != other.Properties[i].Name ||
                    Properties[i].Type != other.Properties[i].Type) return false;
            }
            return true;
        }

        #endregion
    }

    internal class ClassFactory
    {
        #region construction and destruction

        static ClassFactory() // Trigger lazy initialization of static fields
        {
        }

        private ClassFactory()
        {
            var name = new AssemblyName("DynamicClasses");
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);
#if ENABLE_LINQ_PARTIAL_TRUST
            new ReflectionPermission(PermissionState.Unrestricted).Assert();
#endif
            try
            {
                _module = assembly.DefineDynamicModule("Module");
            }
            finally
            {
#if ENABLE_LINQ_PARTIAL_TRUST
                PermissionSet.RevertAssert();
#endif
            }
            _classes = new Dictionary<Signature, Type>();
            _rwLock = new ReaderWriterLock();
        }

        #endregion

        #region public functions

        public static readonly ClassFactory Instance = new ClassFactory();

        public Type GetDynamicClass(IEnumerable<DynamicProperty> properties)
        {
            _rwLock.AcquireReaderLock(Timeout.Infinite);
            try
            {
                var signature = new Signature(properties);
                Type? type;
                if (!_classes.TryGetValue(signature, out type))
                {
                    type = CreateDynamicClass(signature.Properties);
                    _classes.Add(signature, type);
                }
                return type;
            }
            finally
            {
                _rwLock.ReleaseReaderLock();
            }
        }

        #endregion

        #region private functions

        private Type CreateDynamicClass(DynamicProperty[] properties)
        {
            LockCookie cookie = _rwLock.UpgradeToWriterLock(Timeout.Infinite);
            try
            {
                string typeName = "DynamicClass" + (_classCount + 1);
#if ENABLE_LINQ_PARTIAL_TRUST
                new ReflectionPermission(PermissionState.Unrestricted).Assert();
#endif
                try
                {
                    TypeBuilder tb = _module.DefineType(typeName, TypeAttributes.Class |
                                                                  TypeAttributes.Public, typeof(DynamicClass));
                    FieldInfo[] fields = GenerateProperties(tb, properties);
                    GenerateEquals(tb, fields);
                    GenerateGetHashCode(tb, fields);
                    Type? result = tb.CreateType();
                    if (result == null) throw new InvalidOperationException();
                    _classCount++;
                    return result;
                }
                finally
                {
#if ENABLE_LINQ_PARTIAL_TRUST
                    PermissionSet.RevertAssert();
#endif
                }
            }
            finally
            {
                _rwLock.DowngradeFromWriterLock(ref cookie);
            }
        }

        private FieldInfo[] GenerateProperties(TypeBuilder tb, DynamicProperty[] properties)
        {
            var fields = new FieldInfo[properties.Length];
            for (int i = 0; i < properties.Length; i++)
            {
                DynamicProperty dp = properties[i];
                FieldBuilder fb = tb.DefineField("_" + dp.Name, dp.Type, FieldAttributes.Private);
                PropertyBuilder pb = tb.DefineProperty(dp.Name, PropertyAttributes.HasDefault, dp.Type, null);
                MethodBuilder mbGet = tb.DefineMethod("get_" + dp.Name,
                    MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                    dp.Type, Type.EmptyTypes);
                ILGenerator genGet = mbGet.GetILGenerator();
                genGet.Emit(OpCodes.Ldarg_0);
                genGet.Emit(OpCodes.Ldfld, fb);
                genGet.Emit(OpCodes.Ret);
                MethodBuilder mbSet = tb.DefineMethod("set_" + dp.Name,
                    MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                    null, new[] { dp.Type });
                ILGenerator genSet = mbSet.GetILGenerator();
                genSet.Emit(OpCodes.Ldarg_0);
                genSet.Emit(OpCodes.Ldarg_1);
                genSet.Emit(OpCodes.Stfld, fb);
                genSet.Emit(OpCodes.Ret);
                pb.SetGetMethod(mbGet);
                pb.SetSetMethod(mbSet);
                fields[i] = fb;
            }
            return fields;
        }

        private void GenerateEquals(TypeBuilder tb, IEnumerable<FieldInfo> fields)
        {
            MethodBuilder mb = tb.DefineMethod("Equals",
                MethodAttributes.Public | MethodAttributes.ReuseSlot |
                MethodAttributes.Virtual | MethodAttributes.HideBySig,
                typeof(bool), new[] { typeof(object) });
            ILGenerator gen = mb.GetILGenerator();
            LocalBuilder other = gen.DeclareLocal(tb);
            Label next = gen.DefineLabel();
            gen.Emit(OpCodes.Ldarg_1);
            gen.Emit(OpCodes.Isinst, tb);
            gen.Emit(OpCodes.Stloc, other);
            gen.Emit(OpCodes.Ldloc, other);
            gen.Emit(OpCodes.Brtrue_S, next);
            gen.Emit(OpCodes.Ldc_I4_0);
            gen.Emit(OpCodes.Ret);
            gen.MarkLabel(next);
            foreach (FieldInfo field in fields)
            {
                Type ft = field.FieldType;
                Type ct = typeof(EqualityComparer<>).MakeGenericType(ft);
                next = gen.DefineLabel();
                MethodInfo? m = ct.GetMethod("get_Default");
                if (m == null) throw new InvalidOperationException();
                gen.EmitCall(OpCodes.Call, m, null);
                gen.Emit(OpCodes.Ldarg_0);
                gen.Emit(OpCodes.Ldfld, field);
                gen.Emit(OpCodes.Ldloc, other);
                gen.Emit(OpCodes.Ldfld, field);
                m = ct.GetMethod("Equals", new[] { ft, ft });
                if (m == null) throw new InvalidOperationException();
                gen.EmitCall(OpCodes.Callvirt, m, null);
                gen.Emit(OpCodes.Brtrue_S, next);
                gen.Emit(OpCodes.Ldc_I4_0);
                gen.Emit(OpCodes.Ret);
                gen.MarkLabel(next);
            }
            gen.Emit(OpCodes.Ldc_I4_1);
            gen.Emit(OpCodes.Ret);
        }

        private void GenerateGetHashCode(TypeBuilder tb, IEnumerable<FieldInfo> fields)
        {
            MethodBuilder mb = tb.DefineMethod("GetHashCode",
                MethodAttributes.Public | MethodAttributes.ReuseSlot |
                MethodAttributes.Virtual | MethodAttributes.HideBySig,
                typeof(int), Type.EmptyTypes);
            ILGenerator gen = mb.GetILGenerator();
            gen.Emit(OpCodes.Ldc_I4_0);
            foreach (FieldInfo field in fields)
            {
                Type ft = field.FieldType;
                Type ct = typeof(EqualityComparer<>).MakeGenericType(ft);
                MethodInfo? m = ct.GetMethod("get_Default");
                if (m == null) throw new InvalidOperationException();
                gen.EmitCall(OpCodes.Call, m, null);
                gen.Emit(OpCodes.Ldarg_0);
                gen.Emit(OpCodes.Ldfld, field);
                m = ct.GetMethod("GetHashCode", new[] { ft });
                if (m == null) throw new InvalidOperationException();
                gen.EmitCall(OpCodes.Callvirt, m, null);
                gen.Emit(OpCodes.Xor);
            }
            gen.Emit(OpCodes.Ret);
        }

        #endregion

        #region private fields

        private readonly ModuleBuilder _module;
        private readonly Dictionary<Signature, Type> _classes;
        private int _classCount;
        private readonly ReaderWriterLock _rwLock;

        #endregion
    }

    public sealed class ParseException : Exception
    {
        #region construction and destruction

        public ParseException(string message, int position)
            : base(message)
        {
            _position = position;
        }

        #endregion

        #region public functions

        public int Position
        {
            get { return _position; }
        }

        public override string ToString()
        {
            return string.Format(Res.ParseExceptionFormat, Message, _position);
        }

        #endregion

        #region private fields

        private readonly int _position;

        #endregion
    }

    internal class ExpressionParser
    {
        private struct Token
        {
            public TokenId Id;
            public string Text;
            public int Pos;
        }

        private enum TokenId
        {
            Unknown,
            End,
            Identifier,
            StringLiteral,
            IntegerLiteral,
            RealLiteral,
            Exclamation,
            Percent,
            Amphersand,
            OpenParen,
            CloseParen,
            Asterisk,
            Plus,
            Comma,
            Minus,
            Dot,
            Slash,
            Colon,
            LessThan,
            Equal,
            GreaterThan,
            Question,
            OpenBracket,
            CloseBracket,
            Bar,
            ExclamationEqual,
            DoubleAmphersand,
            LessThanEqual,
            LessGreater,
            DoubleEqual,
            GreaterThanEqual,
            DoubleBar,
            Lambda
        }

        private interface ILogicalSignatures
        {
            void F(bool x, bool y);
            void F(bool? x, bool? y);
        }

        private interface IArithmeticSignatures
        {
            void F(int x, int y);
            void F(uint x, uint y);
            void F(long x, long y);
            void F(ulong x, ulong y);
            void F(float x, float y);
            void F(double x, double y);
            void F(decimal x, decimal y);
            void F(int? x, int? y);
            void F(uint? x, uint? y);
            void F(long? x, long? y);
            void F(ulong? x, ulong? y);
            void F(float? x, float? y);
            void F(double? x, double? y);
            void F(decimal? x, decimal? y);
        }

        private interface IRelationalSignatures : IArithmeticSignatures
        {
            void F(string x, string y);
            void F(char x, char y);
            void F(DateTime x, DateTime y);
            void F(TimeSpan x, TimeSpan y);
            void F(char? x, char? y);
            void F(DateTime? x, DateTime? y);
            void F(TimeSpan? x, TimeSpan? y);
        }

        private interface IEqualitySignatures : IRelationalSignatures
        {
            void F(bool x, bool y);
            void F(bool? x, bool? y);
        }

        private interface IAddSignatures : IArithmeticSignatures
        {
            void F(DateTime x, TimeSpan y);
            void F(TimeSpan x, TimeSpan y);
            void F(DateTime? x, TimeSpan? y);
            void F(TimeSpan? x, TimeSpan? y);
        }

        private interface ISubtractSignatures : IAddSignatures
        {
            void F(DateTime x, DateTime y);
            void F(DateTime? x, DateTime? y);
        }

        private interface INegationSignatures
        {
            void F(int x);
            void F(long x);
            void F(float x);
            void F(double x);
            void F(decimal x);
            void F(int? x);
            void F(long? x);
            void F(float? x);
            void F(double? x);
            void F(decimal? x);
        }

        private interface INotSignatures
        {
            void F(bool x);
            void F(bool? x);
        }

        private interface IEnumerableSignatures
        {
            void Where(bool predicate);
            void Any();
            void Any(bool predicate);
            void All(bool predicate);
            void Count();
            void Count(bool predicate);
            void Min(object selector);
            void Max(object selector);
            void Sum(int selector);
            void Sum(int? selector);
            void Sum(long selector);
            void Sum(long? selector);
            void Sum(float selector);
            void Sum(float? selector);
            void Sum(double selector);
            void Sum(double? selector);
            void Sum(decimal selector);
            void Sum(decimal? selector);
            void Average(int selector);
            void Average(int? selector);
            void Average(long selector);
            void Average(long? selector);
            void Average(float selector);
            void Average(float? selector);
            void Average(double selector);
            void Average(double? selector);
            void Average(decimal selector);
            void Average(decimal? selector);
            void Take(int count);
            void Union(IQueryable right);
            void Select(LambdaExpression exp);
            void OrderBy(LambdaExpression exp);
            void OrderByDescending(LambdaExpression exp);
        }

        internal static Type[] PredefinedTypes =
        {
            typeof (Object),
            typeof (Boolean),
            typeof (Char),
            typeof (String),
            typeof (SByte),
            typeof (Byte),
            typeof (Int16),
            typeof (UInt16),
            typeof (Int32),
            typeof (UInt32),
            typeof (Int64),
            typeof (UInt64),
            typeof (Single),
            typeof (Double),
            typeof (Decimal),
            typeof (DateTime),
            typeof (TimeSpan),
            typeof (Guid),
            typeof (Math),
            typeof (Convert),
            typeof (Any)
        };

        private static readonly Expression TrueLiteral = Expression.Constant(true);
        private static readonly Expression FalseLiteral = Expression.Constant(false);
        private static readonly Expression NullLiteral = Expression.Constant(null);

        private const string KeywordIt = "it";
        private const string KeywordIif = "iif";
        private const string KeywordNew = "new";

        private static Dictionary<string, object>? _keywords;

        private readonly Dictionary<string, object> _symbols;
        private IDictionary<string, object>? _externals;
        private readonly IDictionary<string, object> _internals = new Dictionary<string, object>();
        private readonly Dictionary<Expression, string> _literals;
        private ParameterExpression? _it;
        private readonly string _text;
        private int _textPos;
        private readonly int _textLen;
        private char _ch;
        private Token _token;

        public ExpressionParser(ParameterExpression[]? parameters, string expression, object[]? values)
        {            
            if (_keywords == null) _keywords = CreateKeywords();
            _symbols = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            _literals = new Dictionary<Expression, string>();
            if (parameters != null) ProcessParameters(parameters);
            if (values != null) ProcessValues(values);
            _text = expression;
            _textLen = _text.Length;
            SetTextPos(0);
            NextToken();
        }

        private void ProcessParameters(ParameterExpression[] parameters)
        {
            foreach (ParameterExpression pe in parameters)
                if (!String.IsNullOrEmpty(pe.Name))
                    AddSymbol(pe.Name, pe);
            if (parameters.Length == 1 && String.IsNullOrEmpty(parameters[0].Name))
                _it = parameters[0];
        }

        private void ProcessValues(object[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                object value = values[i];
                if (i == values.Length - 1 && value is IDictionary<string, object>)
                {
                    _externals = (IDictionary<string, object>)value;
                }
                else
                {
                    AddSymbol("@" + i.ToString(CultureInfo.InvariantCulture), value);
                }
            }
        }

        private void AddSymbol(string name, object value)
        {
            if (_symbols.ContainsKey(name))
                throw ParseError(Res.DuplicateIdentifier, name);
            _symbols.Add(name, value);
        }

        public Expression Parse(Type? resultType)
        {
            int exprPos = _token.Pos;
            Expression? expr = ParseExpression();
            if (resultType != null)
                if ((expr = PromoteExpression(expr, resultType, true)) == null)
                    throw ParseError(exprPos, Res.ExpressionTypeMismatch, GetTypeName(resultType));
            ValidateToken(TokenId.End, Res.SyntaxError);
            return expr;
        }

#pragma warning disable 0219
        public IEnumerable<DynamicOrdering> ParseOrdering()
        {
            var orderings = new List<DynamicOrdering>();
            while (true)
            {
                Expression expr = ParseExpression();
                bool ascending = true;
                if (TokenIdentifierIs("asc") || TokenIdentifierIs("ascending"))
                {
                    NextToken();
                }
                else if (TokenIdentifierIs("desc") || TokenIdentifierIs("descending"))
                {
                    NextToken();
                    ascending = false;
                }
                orderings.Add(new DynamicOrdering { Selector = expr, Ascending = ascending });
                if (_token.Id != TokenId.Comma) break;
                NextToken();
            }
            ValidateToken(TokenId.End, Res.SyntaxError);
            return orderings;
        }
#pragma warning restore 0219

        // ?: operator
        private Expression ParseExpression()
        {
            int errorPos = _token.Pos;
            Expression expr = ParseLambda();
            if (_token.Id == TokenId.Question)
            {
                NextToken();
                Expression expr1 = ParseExpression();
                ValidateToken(TokenId.Colon, Res.ColonExpected);
                NextToken();
                Expression expr2 = ParseExpression();
                expr = GenerateConditional(expr, expr1, expr2, errorPos);
            }
            return expr;
        }

        /// <summary>
        ///     => operator
        ///     Added Support for projection operator
        /// </summary>
        /// <returns></returns>
        private Expression ParseLambda()
        {
            int errorPos = _token.Pos;
            Expression expr = ParseLogicalOr();
            if (_token.Id == TokenId.Lambda)
            {
                if (_it == null) throw new InvalidOperationException();
                if (_token.Id == TokenId.Lambda && _it.Type == expr.Type)
                {
                    NextToken();
                    if (_token.Id == TokenId.Identifier || _token.Id == TokenId.OpenParen)
                    {
                        Expression right = ParseExpression();
                        return Expression.Lambda(right, new[] { (ParameterExpression)expr });
                    }
                    ValidateToken(TokenId.OpenParen, Res.OpenParenExpected);
                }
            }
            return expr;
        }

        // ||, or operator
        private Expression ParseLogicalOr()
        {
            Expression left = ParseLogicalAnd();
            while (_token.Id == TokenId.DoubleBar || TokenIdentifierIs("or"))
            {
                Token op = _token;
                NextToken();
                Expression right = ParseLogicalAnd();
                CheckAndPromoteOperands(typeof(ILogicalSignatures), op.Text, ref left, ref right, op.Pos);
                left = Expression.OrElse(left, right);
            }
            return left;
        }

        // &&, and operator
        private Expression ParseLogicalAnd()
        {
            Expression left = ParseComparison();
            while (_token.Id == TokenId.DoubleAmphersand || TokenIdentifierIs("and"))
            {
                Token op = _token;
                NextToken();
                Expression right = ParseComparison();
                CheckAndPromoteOperands(typeof(ILogicalSignatures), op.Text, ref left, ref right, op.Pos);
                left = Expression.AndAlso(left, right);
            }
            return left;
        }

        // =, ==, !=, <>, >, >=, <, <= operators
        private Expression ParseComparison()
        {
            Expression left = ParseAdditive();
            while (_token.Id == TokenId.Equal || _token.Id == TokenId.DoubleEqual ||
                   _token.Id == TokenId.ExclamationEqual || _token.Id == TokenId.LessGreater ||
                   _token.Id == TokenId.GreaterThan || _token.Id == TokenId.GreaterThanEqual ||
                   _token.Id == TokenId.LessThan || _token.Id == TokenId.LessThanEqual)
            {
                Token op = _token;
                NextToken();
                Expression right = ParseAdditive();
                bool isEquality = op.Id == TokenId.Equal || op.Id == TokenId.DoubleEqual ||
                                  op.Id == TokenId.ExclamationEqual || op.Id == TokenId.LessGreater;
                if (isEquality && !left.Type.IsValueType && !right.Type.IsValueType)
                {
                    if (left.Type != right.Type)
                    {
                        if (left.Type.IsAssignableFrom(right.Type))
                        {
                            right = Expression.Convert(right, left.Type);
                        }
                        else if (right.Type.IsAssignableFrom(left.Type))
                        {
                            left = Expression.Convert(left, right.Type);
                        }
                        else
                        {
                            throw IncompatibleOperandsError(op.Text, left, right, op.Pos);
                        }
                    }
                }
                else if (IsEnumType(left.Type) || IsEnumType(right.Type))
                {
                    if (left.Type != right.Type)
                    {
                        Expression? e;
                        if ((e = PromoteExpression(right, left.Type, true)) != null)
                        {
                            right = e;
                        }
                        else if ((e = PromoteExpression(left, right.Type, true)) != null)
                        {
                            left = e;
                        }
                        else
                        {
                            throw IncompatibleOperandsError(op.Text, left, right, op.Pos);
                        }
                    }
                }
                else
                {
                    CheckAndPromoteOperands(isEquality ? typeof(IEqualitySignatures) : typeof(IRelationalSignatures),
                        op.Text, ref left, ref right, op.Pos);
                }
                switch (op.Id)
                {
                    case TokenId.Equal:
                    case TokenId.DoubleEqual:
                        left = GenerateEqual(left, right);
                        break;
                    case TokenId.ExclamationEqual:
                    case TokenId.LessGreater:
                        left = GenerateNotEqual(left, right);
                        break;
                    case TokenId.GreaterThan:
                        left = GenerateGreaterThan(left, right);
                        break;
                    case TokenId.GreaterThanEqual:
                        left = GenerateGreaterThanEqual(left, right);
                        break;
                    case TokenId.LessThan:
                        left = GenerateLessThan(left, right);
                        break;
                    case TokenId.LessThanEqual:
                        left = GenerateLessThanEqual(left, right);
                        break;
                }
            }
            return left;
        }

        // +, -, &, | operators
        private Expression ParseAdditive()
        {
            Expression left = ParseMultiplicative();
            while (_token.Id == TokenId.Plus || _token.Id == TokenId.Minus ||
                   _token.Id == TokenId.Amphersand)
            {
                Token op = _token;
                NextToken();
                Expression right = ParseMultiplicative();
                switch (op.Id)
                {
                    case TokenId.Plus:
                        if (left.Type == typeof (string) || right.Type == typeof (string))
                        {
                            left = GenerateStringConcat(left, right);
                        }
                        else
                        {
                            CheckAndPromoteOperands(typeof(IAddSignatures), op.Text, ref left, ref right, op.Pos);
                            left = GenerateAdd(left, right);
                        }
                        break;
                    case TokenId.Minus:
                        CheckAndPromoteOperands(typeof(ISubtractSignatures), op.Text, ref left, ref right, op.Pos);
                        left = GenerateSubtract(left, right);
                        break;
                    case TokenId.Amphersand:
                        left = Expression.And(left, right);
                        break;
                    case TokenId.Bar:
                        left = Expression.Or(left, right);
                        break;
                }
            }
            return left;
        }

        // *, /, %, mod operators
        private Expression ParseMultiplicative()
        {
            Expression left = ParseUnary();
            while (_token.Id == TokenId.Asterisk || _token.Id == TokenId.Slash ||
                   _token.Id == TokenId.Percent || TokenIdentifierIs("mod"))
            {
                Token op = _token;
                NextToken();
                Expression right = ParseUnary();
                CheckAndPromoteOperands(typeof(IArithmeticSignatures), op.Text, ref left, ref right, op.Pos);
                switch (op.Id)
                {
                    case TokenId.Asterisk:
                        left = Expression.Multiply(left, right);
                        break;
                    case TokenId.Slash:
                        left = Expression.Divide(left, right);
                        break;
                    case TokenId.Percent:
                    case TokenId.Identifier:
                        left = Expression.Modulo(left, right);
                        break;
                }
            }
            return left;
        }

        // -, !, not unary operators
        private Expression ParseUnary()
        {
            if (_token.Id == TokenId.Minus || _token.Id == TokenId.Exclamation ||
                TokenIdentifierIs("not"))
            {
                Token op = _token;
                NextToken();
                if (op.Id == TokenId.Minus && (_token.Id == TokenId.IntegerLiteral ||
                                               _token.Id == TokenId.RealLiteral))
                {
                    _token.Text = "-" + _token.Text;
                    _token.Pos = op.Pos;
                    return ParsePrimary();
                }
                Expression expr = ParseUnary();
                if (op.Id == TokenId.Minus)
                {
                    CheckAndPromoteOperand(typeof(INegationSignatures), op.Text, ref expr, op.Pos);
                    expr = Expression.Negate(expr);
                }
                else
                {
                    CheckAndPromoteOperand(typeof(INotSignatures), op.Text, ref expr, op.Pos);
                    expr = Expression.Not(expr);
                }
                return expr;
            }
            return ParsePrimary();
        }

        private Expression ParsePrimary()
        {
            Expression expr = ParsePrimaryStart();
            while (true)
            {
                if (_token.Id == TokenId.Dot)
                {
                    NextToken();
                    expr = ParseMemberAccess(null, expr);
                }
                else if (_token.Id == TokenId.OpenBracket)
                {
                    expr = ParseElementAccess(expr);
                }
                else
                {
                    break;
                }
            }
            return expr;
        }

        private Expression ParsePrimaryStart()
        {
            switch (_token.Id)
            {
                case TokenId.Identifier:
                    return ParseIdentifier();
                case TokenId.StringLiteral:
                    return ParseStringLiteral();
                case TokenId.IntegerLiteral:
                    return ParseIntegerLiteral();
                case TokenId.RealLiteral:
                    return ParseRealLiteral();
                case TokenId.OpenParen:
                    return ParseParenExpression();
                default:
                    throw ParseError(Res.ExpressionExpected);
            }
        }

        private Expression ParseStringLiteral()
        {
            ValidateToken(TokenId.StringLiteral);
            char quote = _token.Text[0];
            string s = _token.Text.Substring(1, _token.Text.Length - 2);
            int start = 0;
            while (true)
            {
                int i = s.IndexOf(quote, start);
                if (i < 0) break;
                s = s.Remove(i, 1);
                start = i + 1;
            }
            if (quote == '\'')
            {
                if (s.Length != 1)
                    throw ParseError(Res.InvalidCharacterLiteral);
                NextToken();
                return CreateLiteral(s[0], s);
            }
            NextToken();
            return CreateLiteral(s, s);
        }

        private Expression ParseIntegerLiteral()
        {
            ValidateToken(TokenId.IntegerLiteral);
            string text = _token.Text;
            if (text[0] != '-')
            {
                ulong value;
                if (!UInt64.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
                    throw ParseError(Res.InvalidIntegerLiteral, text);
                NextToken();
                if (value <= Int32.MaxValue) return CreateLiteral((int)value, text);
                if (value <= UInt32.MaxValue) return CreateLiteral((uint)value, text);
                if (value <= Int64.MaxValue) return CreateLiteral((long)value, text);
                return CreateLiteral(value, text);
            }
            else
            {
                long value;
                if (!Int64.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
                    throw ParseError(Res.InvalidIntegerLiteral, text);
                NextToken();
                if (value >= Int32.MinValue && value <= Int32.MaxValue)
                    return CreateLiteral((int)value, text);
                return CreateLiteral(value, text);
            }
        }

        private Expression ParseRealLiteral()
        {
            ValidateToken(TokenId.RealLiteral);
            string text = _token.Text;
            object? value = null;
            char last = text[text.Length - 1];
            if (last == 'F' || last == 'f')
            {
                float f;
                if (Single.TryParse(text.Substring(0, text.Length - 1), NumberStyles.Any, CultureInfo.InvariantCulture, out f)) value = f;
            }
            else
            {
                double d;
                if (Double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out d)) value = d;
            }
            if (value == null) throw ParseError(Res.InvalidRealLiteral, text);
            NextToken();
            return CreateLiteral(value, text);
        }

        private Expression CreateLiteral(object value, string text)
        {
            ConstantExpression expr = Expression.Constant(value);
            _literals.Add(expr, text);
            return expr;
        }

        private Expression ParseParenExpression()
        {
            ValidateToken(TokenId.OpenParen, Res.OpenParenExpected);
            NextToken();
            Expression e = ParseExpression();
            ValidateToken(TokenId.CloseParen, Res.CloseParenOrOperatorExpected);
            NextToken();
            return e;
        }

        private Expression ParseIdentifier()
        {
            ValidateToken(TokenId.Identifier);
            if (_keywords == null) throw new InvalidOperationException();            
            if (_keywords.TryGetValue(_token.Text, out object? value))
            {
                if (value is Type) return ParseTypeAccess((Type)value);
                if (value.ToString() == KeywordIt) return ParseIt();
                if (value.ToString() == KeywordIif) return ParseIif();
                if (value.ToString() == KeywordNew) return ParseNew();
                NextToken();
                return (Expression)value;
            }
            //if (symbols.TryGetValue(token.text, out value) ||
            //    externals != null && externals.TryGetValue(token.text, out value)) {
            if (_symbols.TryGetValue(_token.Text, out value) ||
                _externals != null && _externals.TryGetValue(_token.Text, out value) ||
                _internals.TryGetValue(_token.Text, out value))
            {
                var expr = value as Expression;
                if (expr == null)
                {
                    expr = Expression.Constant(value);
                }
                else
                {
                    var lambda = expr as LambdaExpression;
                    if (lambda != null) return ParseLambdaInvocation(lambda);
                }
                NextToken();
                return expr;
            }
            if (_it != null) return ParseMemberAccess(null, _it);
            throw ParseError(Res.UnknownIdentifier, _token.Text);
        }

        private Expression ParseIt()
        {
            if (_it == null)
                throw ParseError(Res.NoItInScope);
            NextToken();
            return _it;
        }

        private Expression ParseIif()
        {
            int errorPos = _token.Pos;
            NextToken();
            Expression[] args = ParseArgumentList();
            if (args.Length != 3)
                throw ParseError(errorPos, Res.IifRequiresThreeArgs);
            return GenerateConditional(args[0], args[1], args[2], errorPos);
        }

        private Expression GenerateConditional(Expression test, Expression expr1, Expression expr2, int errorPos)
        {
            if (test.Type != typeof(bool))
                throw ParseError(errorPos, Res.FirstExprMustBeBool);
            if (expr1.Type != expr2.Type)
            {
                Expression? expr1as2 = expr2 != NullLiteral ? PromoteExpression(expr1, expr2.Type, true) : null;
                Expression? expr2as1 = expr1 != NullLiteral ? PromoteExpression(expr2, expr1.Type, true) : null;
                if (expr1as2 != null && expr2as1 == null)
                {
                    expr1 = expr1as2;
                }
                else if (expr2as1 != null && expr1as2 == null)
                {
                    expr2 = expr2as1;
                }
                else
                {
                    string type1 = expr1 != NullLiteral ? expr1.Type.Name : "null";
                    string type2 = expr2 != NullLiteral ? expr2.Type.Name : "null";
                    if (expr1as2 != null && expr2as1 != null)
                        throw ParseError(errorPos, Res.BothTypesConvertToOther, type1, type2);
                    throw ParseError(errorPos, Res.NeitherTypeConvertsToOther, type1, type2);
                }
            }
            return Expression.Condition(test, expr1, expr2);
        }

        private Expression ParseNew()
        {
            NextToken();
            ValidateToken(TokenId.OpenParen, Res.OpenParenExpected);
            NextToken();
            var properties = new List<DynamicProperty>();
            var expressions = new List<Expression>();
            while (true)
            {
                int exprPos = _token.Pos;
                Expression expr = ParseExpression();
                string propName;
                if (TokenIdentifierIs("as"))
                {
                    NextToken();
                    propName = GetIdentifier();
                    NextToken();
                }
                else
                {
                    var me = expr as MemberExpression;
                    if (me == null) throw ParseError(exprPos, Res.MissingAsClause);
                    propName = me.Member.Name;
                }
                expressions.Add(expr);
                properties.Add(new DynamicProperty(propName, expr.Type));
                if (_token.Id != TokenId.Comma) break;
                NextToken();
            }
            ValidateToken(TokenId.CloseParen, Res.CloseParenOrCommaExpected);
            NextToken();
            Type type = DynamicExpression.CreateClass(properties);
            var bindings = new MemberBinding[properties.Count];
            for (int i = 0; i < bindings.Length; i++)
            {
                var propertyInfo = type.GetProperty(properties[i].Name);
                if (propertyInfo == null) throw new InvalidOperationException();
                bindings[i] = Expression.Bind(propertyInfo, expressions[i]);
            }                
            return Expression.MemberInit(Expression.New(type), bindings);
        }

        private Expression ParseLambdaInvocation(LambdaExpression lambda)
        {
            int errorPos = _token.Pos;
            NextToken();
            Expression[] args = ParseArgumentList();
            MethodBase? method;
            if (FindMethod(lambda.Type, "Invoke", false, args, out method) != 1)
                throw ParseError(errorPos, Res.ArgsIncompatibleWithLambda);
            return Expression.Invoke(lambda, args);
        }

        private Expression ParseTypeAccess(Type type)
        {
            int errorPos = _token.Pos;
            NextToken();
            if (_token.Id == TokenId.Question)
            {
                if (!type.IsValueType || IsNullableType(type))
                    throw ParseError(errorPos, Res.TypeHasNoNullableForm, GetTypeName(type));
                type = typeof(Nullable<>).MakeGenericType(type);
                NextToken();
            }
            if (_token.Id == TokenId.OpenParen)
            {
                Expression[] args = ParseArgumentList();
                MethodBase? method;
                switch (FindBestMethod(type.GetConstructors(), args, out method))
                {
                    case 0:
                        if (args.Length == 1)
                            return GenerateConversion(args[0], type, errorPos);
                        throw ParseError(errorPos, Res.NoMatchingConstructor, GetTypeName(type));
                    case 1:
                        var constructorInfo = method as ConstructorInfo;
                        if (constructorInfo == null) throw new InvalidOperationException();
                        return Expression.New(constructorInfo, args);
                    default:
                        throw ParseError(errorPos, Res.AmbiguousConstructorInvocation, GetTypeName(type));
                }
            }
            ValidateToken(TokenId.Dot, Res.DotOrOpenParenExpected);
            NextToken();
            return ParseMemberAccess(type, null);
        }

        private Expression GenerateConversion(Expression expr, Type type, int errorPos)
        {
            Type exprType = expr.Type;
            if (exprType == type) return expr;
            if (exprType.IsValueType && type.IsValueType)
            {
                if ((IsNullableType(exprType) || IsNullableType(type)) &&
                    GetNonNullableType(exprType) == GetNonNullableType(type))
                    return Expression.Convert(expr, type);
                if ((IsNumericType(exprType) || IsEnumType(exprType)) &&
                    (IsNumericType(type)) || IsEnumType(type))
                    return Expression.ConvertChecked(expr, type);
            }
            if (exprType.IsAssignableFrom(type) || type.IsAssignableFrom(exprType) ||
                exprType.IsInterface || type.IsInterface)
                return Expression.Convert(expr, type);
            throw ParseError(errorPos, Res.CannotConvertValue,
                GetTypeName(exprType), GetTypeName(type));
        }

        /// <summary>
        ///     Parsing begins here
        /// </summary>
        /// <param name="type"></param>
        /// <param name="instance"></param>
        /// <returns></returns>
        private Expression ParseMemberAccess(Type? type, Expression? instance)
        {
            if (instance != null) type = instance.Type;
            if (type == null) throw new InvalidOperationException();
            int errorPos = _token.Pos;
            string id = GetIdentifier();
            NextToken();
            if (_token.Id == TokenId.OpenParen)
            {
                if (instance != null && type != typeof(string))
                {
                    Type? enumerableType = FindGenericType(typeof(IQueryable<>), type);
                    if (enumerableType != null)
                    {
                        Type elementType = enumerableType.GetGenericArguments()[0];
                        return ParseAggregate(instance, elementType, id, errorPos);
                    }
                }
                Expression[] args = ParseArgumentList();
                MethodBase? mb;                
                switch (FindMethod(type, id, instance == null, args, out mb))
                {
                    case 0:
                        throw ParseError(errorPos, Res.NoApplicableMethod,
                            id, GetTypeName(type));
                    case 1:
                        MethodInfo? method = mb as MethodInfo;
                        if (method == null) throw new InvalidOperationException();
                        if (!IsPredefinedType(method.DeclaringType))
                            throw ParseError(errorPos, Res.MethodsAreInaccessible, GetTypeName(method.DeclaringType));
                        if (method.ReturnType == typeof(void))
                            throw ParseError(errorPos, Res.MethodIsVoid,
                                id, GetTypeName(method.DeclaringType));
                        return Expression.Call(instance, method, args);
                    default:
                        throw ParseError(errorPos, Res.AmbiguousMethodInvocation,
                            id, GetTypeName(type));
                }
            }
            else
            {
                MemberInfo? member = FindPropertyOrField(type, id, instance == null);
                //if (member == null)
                //    throw ParseError(errorPos, Res.UnknownPropertyOrField,
                //        id, GetTypeName(type));
                if (member == null)
                {
                    if (_it == null) throw new InvalidOperationException();
                    if (_token.Id == TokenId.Lambda && _it.Type == type)
                    {
                        // This might be an internal variable for use within a lambda expression, so store it as such
                        _internals.Add(id, _it);
                        NextToken();
                        Expression right = ParseExpression();
                        return right;
                    }
                    else
                    {
                        throw ParseError(errorPos, Res.UnknownPropertyOrField,
                            id, GetTypeName(type));
                    }
                }
                return member is PropertyInfo
                    ? Expression.Property(instance, (PropertyInfo)member)
                    : Expression.Field(instance, (FieldInfo)member);
            }
        }

        private static Type? FindGenericType(Type generic, Type? type)
        {
            while (type != null && type != typeof(object))
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == generic) return type;
                if (generic.IsInterface)
                {
                    foreach (Type intfType in type.GetInterfaces())
                    {
                        Type? found = FindGenericType(generic, intfType);
                        if (found != null) return found;
                    }
                }
                type = type.BaseType;
            }
            return null;
        }

        private Expression ParseAggregate(Expression instance, Type elementType, string methodName, int errorPos)
        {
            ParameterExpression? outerIt = _it;
            ParameterExpression innerIt = _it == null
                ? Expression.Parameter(elementType, "")
                : Expression.Parameter(elementType, _it.ToString());
            _it = innerIt;
            Expression[] args = ParseArgumentList();
            _it = outerIt;
            MethodBase? signature;
            if (FindMethod(typeof(IEnumerableSignatures), methodName, false, args, out signature) != 1)
                throw ParseError(errorPos, Res.NoApplicableAggregate, methodName);
            Type[] typeArgs;
            if (signature == null) throw new InvalidOperationException();
            switch (signature.Name)
            {
                case "Min":
                case "Max":
                    typeArgs = new[] { elementType, args[0].Type };
                    break;
                case "Select":
                    typeArgs = new[] { elementType, args[0].Type.GetGenericArguments().Last() };
                    break;
                case "OrderBy":
                    typeArgs = new[] { elementType, args[0].Type.GetGenericArguments().Last() };
                    break;
                case "OrderByDescending":
                    typeArgs = new[] { elementType, args[0].Type.GetGenericArguments().Last() };
                    break;
                default:
                    typeArgs = new[] { elementType };
                    break;
            }
            if (args.Length == 0)
            {
                args = new[] { instance };
            }
            else
            {
                if (args[0].NodeType == ExpressionType.Constant)
                {
                    args = new[] { instance, args[0] };
                }
                else
                {
                    if (signature.GetParameters().Last().ParameterType == typeof(IQueryable)
                        || signature.GetParameters().Last().ParameterType == typeof(IEnumerable)
                        || args[0] is LambdaExpression)
                    {
                        args = new[] { instance, args[0] };
                    }
                    else
                    {
                        args = new[] { instance, Expression.Lambda(args[0], innerIt) };
                    }
                }
            }

            return Expression.Call(typeof(Queryable), signature.Name, typeArgs, args);
        }

        private Expression[] ParseArgumentList()
        {
            ValidateToken(TokenId.OpenParen, Res.OpenParenExpected);
            NextToken();
            Expression[] args = _token.Id != TokenId.CloseParen ? ParseArguments() : new Expression[0];
            ValidateToken(TokenId.CloseParen, Res.CloseParenOrCommaExpected);
            NextToken();
            return args;
        }

        private Expression[] ParseArguments()
        {
            var argList = new List<Expression>();
            while (true)
            {
                argList.Add(ParseExpression());
                if (_token.Id != TokenId.Comma) break;
                NextToken();
            }
            return argList.ToArray();
        }

        private Expression ParseElementAccess(Expression expr)
        {
            int errorPos = _token.Pos;
            ValidateToken(TokenId.OpenBracket, Res.OpenParenExpected);
            NextToken();
            Expression[] args = ParseArguments();
            ValidateToken(TokenId.CloseBracket, Res.CloseBracketOrCommaExpected);
            NextToken();
            if (expr.Type.IsArray)
            {
                if (expr.Type.GetArrayRank() != 1 || args.Length != 1)
                    throw ParseError(errorPos, Res.CannotIndexMultiDimArray);
                Expression? index = PromoteExpression(args[0], typeof(int), true);
                if (index == null)
                    throw ParseError(errorPos, Res.InvalidIndex);
                return Expression.ArrayIndex(expr, index);
            }
            else
            {
                MethodBase? mb;
                switch (FindIndexer(expr.Type, args, out mb))
                {
                    case 0:
                        throw ParseError(errorPos, Res.NoApplicableIndexer,
                            GetTypeName(expr.Type));
                    case 1:
                        var methodInfo = mb as MethodInfo;
                        if (methodInfo == null) throw new InvalidOperationException();
                        return Expression.Call(expr, methodInfo, args);
                    default:
                        throw ParseError(errorPos, Res.AmbiguousIndexerInvocation,
                            GetTypeName(expr.Type));
                }
            }
        }

        private static bool IsPredefinedType(Type? type)
        {
            foreach (Type t in PredefinedTypes) if (t == type) return true;
            return false;
        }

        private static bool IsNullableType(Type? type)
        {
            if (type == null) return false;
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        private static Type GetNonNullableType(Type type)
        {
            return IsNullableType(type) ? type.GetGenericArguments()[0] : type;
        }

        private static string GetTypeName(Type? type)
        {
            if (type == null) return "<null>";
            Type baseType = GetNonNullableType(type);
            string s = baseType.Name;
            if (type != baseType) s += '?';
            return s;
        }

        private static bool IsNumericType(Type type)
        {
            return GetNumericTypeKind(type) != 0;
        }

        private static bool IsSignedIntegralType(Type type)
        {
            return GetNumericTypeKind(type) == 2;
        }

        private static bool IsUnsignedIntegralType(Type type)
        {
            return GetNumericTypeKind(type) == 3;
        }

        private static int GetNumericTypeKind(Type type)
        {
            type = GetNonNullableType(type);
            if (type.IsEnum) return 0;
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Char:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    return 1;
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                    return 2;
                case TypeCode.Byte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return 3;
                default:
                    return 0;
            }
        }

        private static bool IsEnumType(Type type)
        {
            return GetNonNullableType(type).IsEnum;
        }

        private void CheckAndPromoteOperand(Type signatures, string opName, ref Expression expr, int errorPos)
        {
            Expression[] args = { expr };
            MethodBase? method;
            if (FindMethod(signatures, "F", false, args, out method) != 1)
                throw ParseError(errorPos, Res.IncompatibleOperand,
                    opName, GetTypeName(args[0].Type));
            expr = args[0];
        }

        private void CheckAndPromoteOperands(Type signatures, string opName, ref Expression left, ref Expression right,
            int errorPos)
        {
            Expression[] args = { left, right };
            MethodBase? method;
            if (FindMethod(signatures, "F", false, args, out method) != 1)
                throw IncompatibleOperandsError(opName, left, right, errorPos);
            left = args[0];
            right = args[1];
        }

        private Exception IncompatibleOperandsError(string opName, Expression left, Expression right, int pos)
        {
            return ParseError(pos, Res.IncompatibleOperands,
                opName, GetTypeName(left.Type), GetTypeName(right.Type));
        }

        private MemberInfo? FindPropertyOrField(Type type, string memberName, bool staticAccess)
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.DeclaredOnly |
                                 (staticAccess ? BindingFlags.Static : BindingFlags.Instance);
            foreach (Type t in SelfAndBaseTypes(type))
            {
                MemberInfo[] members = t.FindMembers(MemberTypes.Property | MemberTypes.Field,
                    flags, Type.FilterNameIgnoreCase, memberName);
                if (members.Length != 0) return members[0];
            }
            return null;
        }

        private int FindMethod(Type type, string methodName, bool staticAccess, Expression[] args, out MethodBase? method)
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.DeclaredOnly |
                                 (staticAccess ? BindingFlags.Static : BindingFlags.Instance);
            foreach (Type t in SelfAndBaseTypes(type))
            {
                MemberInfo[] members = t.FindMembers(MemberTypes.Method,
                    flags, Type.FilterNameIgnoreCase, methodName).ToArray();
                int count = FindBestMethod(members.Cast<MethodBase>(), args, out method);
                if (count != 0) return count;
                /*
                MemberInfo[] extensionMembers = GetExtensionMethods(t, methodName).ToArray();
                count = FindBestMethod(extensionMembers.Cast<MethodBase>(), args, out method);
                if (count != 0) return count;
                */
            }
            method = null;
            return 0;
        }

        private IEnumerable<MemberInfo> GetExtensionMethods(Type extendedType, string methodName)
        {
            IEnumerable<MethodInfo> query = from type in GetType().Assembly.GetTypes()
                                            where type.IsSealed && !type.IsGenericType && !type.IsNested
                                            from method in type.GetMethods(BindingFlags.Static
                                                                           | BindingFlags.Public | BindingFlags.NonPublic)
                                            where method.IsDefined(typeof(ExtensionAttribute), false)
                                            where method.Name == methodName
                                            where method.GetParameters()[0].ParameterType.IsAssignableFrom(extendedType)
                                            select method;
            return query;
        }


        private int FindIndexer(Type type, Expression[] args, out MethodBase? method)
        {
            foreach (Type t in SelfAndBaseTypes(type))
            {
                MemberInfo[] members = t.GetDefaultMembers();
                if (members.Length != 0)
                {
                    IEnumerable<MethodBase> methods = members.
                        OfType<PropertyInfo>().
                        Select(p => p.GetGetMethod() as MethodBase).
                        Where(m => m != null).OfType<MethodBase>();
                    int count = FindBestMethod(methods, args, out method);
                    if (count != 0) return count;
                }
            }
            method = null;
            return 0;
        }

        private static IEnumerable<Type> SelfAndBaseTypes(Type type)
        {
            if (type.IsInterface)
            {
                var types = new List<Type>();
                AddInterface(types, type);
                return types;
            }
            return SelfAndBaseClasses(type);
        }

        private static IEnumerable<Type> SelfAndBaseClasses(Type? type)
        {
            while (type != null)
            {
                yield return type;
                type = type.BaseType;
            }
        }

        private static void AddInterface(List<Type> types, Type type)
        {
            if (!types.Contains(type))
            {
                types.Add(type);
                foreach (Type t in type.GetInterfaces()) AddInterface(types, t);
            }
        }

        private class MethodData
        {
            #region public functions

            public MethodBase? MethodBase;
            public ParameterInfo[] Parameters = new ParameterInfo[0];
            public Expression[] Args = new Expression[0];

            #endregion
        }

        private int FindBestMethod(IEnumerable<MethodBase> methods, Expression[] args, out MethodBase? method)
        {
            MethodData[] applicable = methods.
                Select(
                    m =>
                        new MethodData
                        {
                            MethodBase = m,
                            Parameters =
                                m.GetParameters()
                        })
                .
                Where(m => IsApplicable(m, args)).
                ToArray();
            if (applicable.Length > 1)
            {
                applicable = applicable.
                    Where(m => applicable.All(n => m == n || IsBetterThan(args, m, n))).
                    ToArray();
            }
            if (applicable.Length == 1)
            {
                MethodData md = applicable[0];
                for (int i = 0; i < args.Length; i++) args[i] = md.Args[i];
                method = md.MethodBase;
            }
            else
            {
                method = null;
            }
            return applicable.Length;
        }

        private bool IsApplicable(MethodData method, Expression[] args)
        {
            if (method.Parameters.Length != args.Length) return false;
            var promotedArgs = new Expression[args.Length];
            for (int i = 0; i < args.Length; i++)
            {
                ParameterInfo pi = method.Parameters[i];
                if (pi.IsOut) return false;
                Expression? promoted = PromoteExpression(args[i], pi.ParameterType, false);
                if (promoted == null) return false;
                promotedArgs[i] = promoted;
            }
            method.Args = promotedArgs;
            return true;
        }

        private Expression? PromoteExpression(Expression expr, Type type, bool exact)
        {
            try
            {
                if (expr.Type == type || ((expr is LambdaExpression) && ((LambdaExpression)expr).ReturnType == type) ||
                    (expr is LambdaExpression) && type == (typeof(LambdaExpression))) return expr;
                    //if (expr.Type == type || ((expr is LambdaExpression) && (type is typeof(LambdaExpression))) return expr;
                if (expr is ConstantExpression)
                {
                    var ce = (ConstantExpression)expr;
                    if (ce == NullLiteral)
                    {
                        if (!type.IsValueType || IsNullableType(type))
                            return Expression.Constant(null, type);
                    }
                    else
                    {
                        string? text;
                        if (_literals.TryGetValue(ce, out text))
                        {
                            Type target = GetNonNullableType(type);
                            Object? value = null;
                            switch (Type.GetTypeCode(ce.Type))
                            {
                                case TypeCode.Int32:
                                case TypeCode.UInt32:
                                case TypeCode.Int64:
                                case TypeCode.UInt64:
                                    value = ParseNumber(text, target);
                                    break;
                                case TypeCode.Double:
                                    if (target == typeof(decimal)) value = ParseNumber(text, target);
                                    break;
                                case TypeCode.String:
                                    value = ParseEnum(text, target);
                                    break;
                            }
                            if (value != null)
                            {
                                if (type.IsAssignableFrom(value.GetType()))
                                    return Expression.Constant(value, type);
                                else
                                    return null;
                            }                                
                        }
                    }
                }
                if (IsCompatibleWith(expr.Type, type))
                {
                    if (type.IsValueType || exact) return Expression.Convert(expr, type);
                    return expr;
                }
            }
            catch (Exception)
            {
            }
            return null;
        }

        private static object? ParseNumber(string text, Type type)
        {
            if (text.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase))
            {
                switch (Type.GetTypeCode(GetNonNullableType(type)))
                {
                    case TypeCode.SByte:
                        sbyte sb;
                        if (sbyte.TryParse(text, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out sb)) return sb;
                        break;
                    case TypeCode.Byte:
                        byte b;
                        if (byte.TryParse(text, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out b)) return b;
                        break;
                    case TypeCode.Int16:
                        short s;
                        if (short.TryParse(text, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out s)) return s;
                        break;
                    case TypeCode.UInt16:
                        ushort us;
                        if (ushort.TryParse(text, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out us)) return us;
                        break;
                    case TypeCode.Int32:
                        int i;
                        if (int.TryParse(text, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out i)) return i;
                        break;
                    case TypeCode.UInt32:
                        uint ui;
                        if (uint.TryParse(text, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out ui)) return ui;
                        break;
                    case TypeCode.Int64:
                        long l;
                        if (long.TryParse(text, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out l)) return l;
                        break;
                    case TypeCode.UInt64:
                        ulong ul;
                        if (ulong.TryParse(text, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out ul)) return ul;
                        break;
                    case TypeCode.Single:
                        float f;
                        if (float.TryParse(text, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out f)) return f;
                        break;
                    case TypeCode.Double:
                        double d;
                        if (double.TryParse(text, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out d)) return d;
                        break;
                    case TypeCode.Decimal:
                        decimal e;
                        if (decimal.TryParse(text, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out e)) return e;
                        break;
                }
            }
            else
            {
                switch (Type.GetTypeCode(GetNonNullableType(type)))
                {
                    case TypeCode.SByte:
                        sbyte sb;
                        if (sbyte.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out sb)) return sb;
                        break;
                    case TypeCode.Byte:
                        byte b;
                        if (byte.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out b)) return b;
                        break;
                    case TypeCode.Int16:
                        short s;
                        if (short.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out s)) return s;
                        break;
                    case TypeCode.UInt16:
                        ushort us;
                        if (ushort.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out us)) return us;
                        break;
                    case TypeCode.Int32:
                        int i;
                        if (int.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out i)) return i;
                        break;
                    case TypeCode.UInt32:
                        uint ui;
                        if (uint.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out ui)) return ui;
                        break;
                    case TypeCode.Int64:
                        long l;
                        if (long.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out l)) return l;
                        break;
                    case TypeCode.UInt64:
                        ulong ul;
                        if (ulong.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out ul)) return ul;
                        break;
                    case TypeCode.Single:
                        float f;
                        if (float.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out f)) return f;
                        break;
                    case TypeCode.Double:
                        double d;
                        if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out d)) return d;
                        break;
                    case TypeCode.Decimal:
                        decimal e;
                        if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out e)) return e;
                        break;
                }
            }
            return null;
        }

        private static object? ParseEnum(string name, Type type)
        {
            if (type.IsEnum)
            {
                MemberInfo[] memberInfos = type.FindMembers(MemberTypes.Field,
                    BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Static,
                    Type.FilterNameIgnoreCase, name);
                if (memberInfos.Length != 0) return ((FieldInfo)memberInfos[0]).GetValue(null);
            }
            return null;
        }

        private static bool IsCompatibleWith(Type source, Type target)
        {
            if (source == target) return true;
            if (!target.IsValueType) return target.IsAssignableFrom(source);
            Type st = GetNonNullableType(source);
            Type tt = GetNonNullableType(target);
            if (st != source && tt == target) return false;
            TypeCode sc = st.IsEnum ? TypeCode.Object : Type.GetTypeCode(st);
            TypeCode tc = tt.IsEnum ? TypeCode.Object : Type.GetTypeCode(tt);
            switch (sc)
            {
                case TypeCode.SByte:
                    switch (tc)
                    {
                        case TypeCode.SByte:
                        case TypeCode.Int16:
                        case TypeCode.Int32:
                        case TypeCode.Int64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                    }
                    break;
                case TypeCode.Byte:
                    switch (tc)
                    {
                        case TypeCode.Byte:
                        case TypeCode.Int16:
                        case TypeCode.UInt16:
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                        case TypeCode.UInt64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                    }
                    break;
                case TypeCode.Int16:
                    switch (tc)
                    {
                        case TypeCode.Int16:
                        case TypeCode.Int32:
                        case TypeCode.Int64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                    }
                    break;
                case TypeCode.UInt16:
                    switch (tc)
                    {
                        case TypeCode.UInt16:
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                        case TypeCode.UInt64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                    }
                    break;
                case TypeCode.Int32:
                    switch (tc)
                    {
                        case TypeCode.Int32:
                        case TypeCode.Int64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                    }
                    break;
                case TypeCode.UInt32:
                    switch (tc)
                    {
                        case TypeCode.UInt32:
                        case TypeCode.Int64:
                        case TypeCode.UInt64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                    }
                    break;
                case TypeCode.Int64:
                    switch (tc)
                    {
                        case TypeCode.Int64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                    }
                    break;
                case TypeCode.UInt64:
                    switch (tc)
                    {
                        case TypeCode.UInt64:
                        case TypeCode.Single:
                        case TypeCode.Double:
                        case TypeCode.Decimal:
                            return true;
                    }
                    break;
                case TypeCode.Single:
                    switch (tc)
                    {
                        case TypeCode.Single:
                        case TypeCode.Double:
                            return true;
                    }
                    break;
                default:
                    if (st == tt) return true;
                    break;
            }
            return false;
        }

        private static bool IsBetterThan(Expression[] args, MethodData m1, MethodData m2)
        {
            bool better = false;
            for (int i = 0; i < args.Length; i++)
            {
                int c = CompareConversions(args[i].Type,
                    m1.Parameters[i].ParameterType,
                    m2.Parameters[i].ParameterType);
                if (c < 0) return false;
                if (c > 0) better = true;
            }
            return better;
        }

        // Return 1 if s -> t1 is a better conversion than s -> t2
        // Return -1 if s -> t2 is a better conversion than s -> t1
        // Return 0 if neither conversion is better
        private static int CompareConversions(Type s, Type t1, Type t2)
        {
            if (t1 == t2) return 0;
            if (s == t1) return 1;
            if (s == t2) return -1;
            bool t1t2 = IsCompatibleWith(t1, t2);
            bool t2t1 = IsCompatibleWith(t2, t1);
            if (t1t2 && !t2t1) return 1;
            if (t2t1 && !t1t2) return -1;
            if (IsSignedIntegralType(t1) && IsUnsignedIntegralType(t2)) return 1;
            if (IsSignedIntegralType(t2) && IsUnsignedIntegralType(t1)) return -1;
            return 0;
        }

        private Expression GenerateEqual(Expression left, Expression right)
        {
            return Expression.Equal(left, right);
        }

        private Expression GenerateNotEqual(Expression left, Expression right)
        {
            return Expression.NotEqual(left, right);
        }

        private Expression GenerateGreaterThan(Expression left, Expression right)
        {
            if (left.Type == typeof(string))
            {
                return Expression.GreaterThan(
                    GenerateStaticMethodCall("Compare", left, right),
                    Expression.Constant(0)
                    );
            }
            return Expression.GreaterThan(left, right);
        }

        private Expression GenerateGreaterThanEqual(Expression left, Expression right)
        {
            if (left.Type == typeof(string))
            {
                return Expression.GreaterThanOrEqual(
                    GenerateStaticMethodCall("Compare", left, right),
                    Expression.Constant(0)
                    );
            }
            return Expression.GreaterThanOrEqual(left, right);
        }

        private Expression GenerateLessThan(Expression left, Expression right)
        {
            if (left.Type == typeof(string))
            {
                return Expression.LessThan(
                    GenerateStaticMethodCall("Compare", left, right),
                    Expression.Constant(0)
                    );
            }
            return Expression.LessThan(left, right);
        }

        private Expression GenerateLessThanEqual(Expression left, Expression right)
        {
            if (left.Type == typeof(string))
            {
                return Expression.LessThanOrEqual(
                    GenerateStaticMethodCall("Compare", left, right),
                    Expression.Constant(0)
                    );
            }
            return Expression.LessThanOrEqual(left, right);
        }

        private Expression GenerateAdd(Expression left, Expression right)
        {
            if (left.Type == typeof(string) && right.Type == typeof(string))
            {
                return GenerateStaticMethodCall("Concat", left, right);
            }
            return Expression.Add(left, right);
        }

        private Expression GenerateSubtract(Expression left, Expression right)
        {
            return Expression.Subtract(left, right);
        }

        private Expression GenerateStringConcat(Expression left, Expression right)
        {
            var methodInfo = typeof(string).GetMethod("Concat", new[] { typeof(object), typeof(object) });
            if (methodInfo == null) throw new InvalidOperationException();
            return Expression.Call(null, methodInfo,
                new[] { left, right });
        }

        private MethodInfo? GetStaticMethod(string methodName, Expression left, Expression right)
        {
            return left.Type.GetMethod(methodName, new[] { left.Type, right.Type });
        }

        private Expression GenerateStaticMethodCall(string methodName, Expression left, Expression right)
        {
            var methodInfo = GetStaticMethod(methodName, left, right);
            if (methodInfo == null) throw new InvalidOperationException();
            return Expression.Call(null, methodInfo, new[] { left, right });
        }

        private void SetTextPos(int pos)
        {
            _textPos = pos;
            _ch = _textPos < _textLen ? _text[_textPos] : '\0';
        }

        private void NextChar()
        {
            if (_textPos < _textLen) _textPos++;
            _ch = _textPos < _textLen ? _text[_textPos] : '\0';
        }

        private void NextToken()
        {
            while (Char.IsWhiteSpace(_ch)) NextChar();
            TokenId t;
            int tokenPos = _textPos;
            switch (_ch)
            {
                case '!':
                    NextChar();
                    if (_ch == '=')
                    {
                        NextChar();
                        t = TokenId.ExclamationEqual;
                    }
                    else
                    {
                        t = TokenId.Exclamation;
                    }
                    break;
                case '%':
                    NextChar();
                    t = TokenId.Percent;
                    break;
                case '&':
                    NextChar();
                    if (_ch == '&')
                    {
                        NextChar();
                        t = TokenId.DoubleAmphersand;
                    }
                    else
                    {
                        t = TokenId.Amphersand;
                    }
                    break;
                case '(':
                    NextChar();
                    t = TokenId.OpenParen;
                    break;
                case ')':
                    NextChar();
                    t = TokenId.CloseParen;
                    break;
                case '*':
                    NextChar();
                    t = TokenId.Asterisk;
                    break;
                case '+':
                    NextChar();
                    t = TokenId.Plus;
                    break;
                case ',':
                    NextChar();
                    t = TokenId.Comma;
                    break;
                case '-':
                    NextChar();
                    t = TokenId.Minus;
                    break;
                case '.':
                    NextChar();
                    t = TokenId.Dot;
                    break;
                case '/':
                    NextChar();
                    t = TokenId.Slash;
                    break;
                case ':':
                    NextChar();
                    t = TokenId.Colon;
                    break;
                case '<':
                    NextChar();
                    if (_ch == '=')
                    {
                        NextChar();
                        t = TokenId.LessThanEqual;
                    }
                    else if (_ch == '>')
                    {
                        NextChar();
                        t = TokenId.LessGreater;
                    }
                    else
                    {
                        t = TokenId.LessThan;
                    }
                    break;
                case '=':
                    NextChar();
                    if (_ch == '=')
                    {
                        NextChar();
                        t = TokenId.DoubleEqual;
                    }
                    else if (_ch == '>')
                    {
                        NextChar();
                        t = TokenId.Lambda;
                    }
                    else
                    {
                        t = TokenId.Equal;
                    }
                    break;
                case '>':
                    NextChar();
                    if (_ch == '=')
                    {
                        NextChar();
                        t = TokenId.GreaterThanEqual;
                    }
                    else
                    {
                        t = TokenId.GreaterThan;
                    }
                    break;
                case '?':
                    NextChar();
                    t = TokenId.Question;
                    break;
                case '[':
                    NextChar();
                    t = TokenId.OpenBracket;
                    break;
                case ']':
                    NextChar();
                    t = TokenId.CloseBracket;
                    break;
                case '|':
                    NextChar();
                    if (_ch == '|')
                    {
                        NextChar();
                        t = TokenId.DoubleBar;
                    }
                    else
                    {
                        t = TokenId.Bar;
                    }
                    break;
                case '"':
                case '\'':
                    char quote = _ch;
                    do
                    {
                        NextChar();
                        while (_textPos < _textLen && _ch != quote) NextChar();
                        if (_textPos == _textLen)
                            throw ParseError(_textPos, Res.UnterminatedStringLiteral);
                        NextChar();
                    } while (_ch == quote);
                    t = TokenId.StringLiteral;
                    break;
                default:
                    if (Char.IsLetter(_ch) || _ch == '@' || _ch == '_')
                    {
                        do
                        {
                            NextChar();
                        } while (Char.IsLetterOrDigit(_ch) || _ch == '_');
                        t = TokenId.Identifier;
                        break;
                    }
                    if (Char.IsDigit(_ch))
                    {
                        t = TokenId.IntegerLiteral;
                        do
                        {
                            NextChar();
                        } while (Char.IsDigit(_ch));
                        if (_ch == '.')
                        {
                            t = TokenId.RealLiteral;
                            NextChar();
                            ValidateDigit();
                            do
                            {
                                NextChar();
                            } while (Char.IsDigit(_ch));
                        }
                        if (_ch == 'E' || _ch == 'e')
                        {
                            t = TokenId.RealLiteral;
                            NextChar();
                            if (_ch == '+' || _ch == '-') NextChar();
                            ValidateDigit();
                            do
                            {
                                NextChar();
                            } while (Char.IsDigit(_ch));
                        }
                        if (_ch == 'F' || _ch == 'f') NextChar();
                        break;
                    }
                    if (_textPos == _textLen)
                    {
                        t = TokenId.End;
                        break;
                    }
                    throw ParseError(_textPos, Res.InvalidCharacter, _ch);
            }
            _token.Id = t;
            _token.Text = _text.Substring(tokenPos, _textPos - tokenPos);
            _token.Pos = tokenPos;
        }

        private bool TokenIdentifierIs(string id)
        {
            return _token.Id == TokenId.Identifier && String.Equals(id, _token.Text, StringComparison.OrdinalIgnoreCase);
        }

        private string GetIdentifier()
        {
            ValidateToken(TokenId.Identifier, Res.IdentifierExpected);
            string id = _token.Text;
            if (id.Length > 1 && id[0] == '@') id = id.Substring(1);
            return id;
        }

        private void ValidateDigit()
        {
            if (!Char.IsDigit(_ch)) throw ParseError(_textPos, Res.DigitExpected);
        }

        private void ValidateToken(TokenId t, string errorMessage)
        {
            if (_token.Id != t) throw ParseError(errorMessage);
        }

        private void ValidateToken(TokenId t)
        {
            if (_token.Id != t) throw ParseError(Res.SyntaxError);
        }

        private Exception ParseError(string format, params object[] args)
        {
            return ParseError(_token.Pos, format, args);
        }

        private Exception ParseError(int pos, string format, params object[] args)
        {
            return new ParseException(string.Format(CultureInfo.CurrentCulture, format, args), pos);
        }

        private static Dictionary<string, object> CreateKeywords()
        {
            var d = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                {"true", TrueLiteral},
                {"false", FalseLiteral},
                {"null", NullLiteral},
                {KeywordIt, KeywordIt},
                {KeywordIif, KeywordIif},
                {KeywordNew, KeywordNew}
            };
            foreach (Type type in PredefinedTypes) d.Add(type.Name, type);
            return d;
        }
    }

    internal static class Res
    {
        #region public functions

        public const string DuplicateIdentifier = "The identifier '{0}' was defined more than once";
        public const string ExpressionTypeMismatch = "Expression of type '{0}' expected";
        public const string ExpressionExpected = "Expression expected";
        public const string InvalidCharacterLiteral = "Character literal must contain exactly one character";
        public const string InvalidIntegerLiteral = "Invalid integer literal '{0}'";
        public const string InvalidRealLiteral = "Invalid real literal '{0}'";
        public const string UnknownIdentifier = "Unknown identifier '{0}'";
        public const string NoItInScope = "No 'it' is in scope";
        public const string IifRequiresThreeArgs = "The 'iif' function requires three arguments";
        public const string FirstExprMustBeBool = "The first expression must be of type 'Boolean'";
        public const string BothTypesConvertToOther = "Both of the types '{0}' and '{1}' convert to the other";
        public const string NeitherTypeConvertsToOther = "Neither of the types '{0}' and '{1}' converts to the other";
        public const string MissingAsClause = "Expression is missing an 'as' clause";
        public const string ArgsIncompatibleWithLambda = "Argument list incompatible with lambda expression";
        public const string TypeHasNoNullableForm = "Type '{0}' has no nullable form";
        public const string NoMatchingConstructor = "No matching constructor in type '{0}'";
        public const string AmbiguousConstructorInvocation = "Ambiguous invocation of '{0}' constructor";
        public const string CannotConvertValue = "A value of type '{0}' cannot be converted to type '{1}'";
        public const string NoApplicableMethod = "No applicable method '{0}' exists in type '{1}'";
        public const string MethodsAreInaccessible = "Methods on type '{0}' are not accessible";
        public const string MethodIsVoid = "Method '{0}' in type '{1}' does not return a value";
        public const string AmbiguousMethodInvocation = "Ambiguous invocation of method '{0}' in type '{1}'";
        public const string UnknownPropertyOrField = "No property or field '{0}' exists in type '{1}'";
        public const string NoApplicableAggregate = "No applicable aggregate method '{0}' exists";
        public const string CannotIndexMultiDimArray = "Indexing of multi-dimensional arrays is not supported";
        public const string InvalidIndex = "Array index must be an integer expression";
        public const string NoApplicableIndexer = "No applicable indexer exists in type '{0}'";
        public const string AmbiguousIndexerInvocation = "Ambiguous invocation of indexer in type '{0}'";
        public const string IncompatibleOperand = "Operator '{0}' incompatible with operand type '{1}'";
        public const string IncompatibleOperands = "Operator '{0}' incompatible with operand types '{1}' and '{2}'";
        public const string UnterminatedStringLiteral = "Unterminated string literal";
        public const string InvalidCharacter = "Syntax error '{0}'";
        public const string DigitExpected = "Digit expected";
        public const string SyntaxError = "Syntax error";
        public const string TokenExpected = "{0} expected";
        public const string ParseExceptionFormat = "{0} (at index {1})";
        public const string ColonExpected = "':' expected";
        public const string OpenParenExpected = "'(' expected";
        public const string CloseParenOrOperatorExpected = "')' or operator expected";
        public const string CloseParenOrCommaExpected = "')' or ',' expected";
        public const string DotOrOpenParenExpected = "'.' or '(' expected";
        public const string OpenBracketExpected = "'[' expected";
        public const string CloseBracketOrCommaExpected = "']' or ',' expected";
        public const string IdentifierExpected = "Identifier expected";

        #endregion
    }
}