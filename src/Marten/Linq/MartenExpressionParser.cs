using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using Baseline;
using Marten.Schema;
using Marten.Util;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Clauses.ResultOperators;

namespace Marten.Linq
{
    public class MartenExpressionParser
    {
        private static readonly string CONTAINS = nameof(string.Contains);
        private static readonly string STARTS_WITH = nameof(string.StartsWith);
        private static readonly string ENDS_WITH = nameof(string.EndsWith);

        private static readonly IDictionary<ExpressionType, string> _operators = new Dictionary<ExpressionType, string>
        {
            {ExpressionType.Equal, "="},
            {ExpressionType.NotEqual, "!="},
            {ExpressionType.GreaterThan, ">"},
            {ExpressionType.GreaterThanOrEqual, ">="},
            {ExpressionType.LessThan, "<"},
            {ExpressionType.LessThanOrEqual, "<="}
        };

        private readonly DocumentQuery _query;
        private readonly ISerializer _serializer;

        public MartenExpressionParser(DocumentQuery query, ISerializer serializer)
        {
            _query = query;
            _serializer = serializer;
        }

        public IWhereFragment ParseWhereFragment(IDocumentMapping mapping, Expression expression)
        {
            if (expression is BinaryExpression)
            {
                return GetWhereFragment(mapping, expression.As<BinaryExpression>());
            }

            if (expression.NodeType == ExpressionType.Call)
            {
                return GetMethodCall(mapping, expression.As<MethodCallExpression>());
            }

            if (expression is MemberExpression && expression.Type == typeof (bool))
            {
                var locator = JsonLocator(mapping, expression.As<MemberExpression>());
                return new WhereFragment("{0} = True".ToFormat(locator), true);
            }

            if (expression.NodeType == ExpressionType.Not)
            {
                return GetNotWhereFragment(mapping, expression.As<UnaryExpression>().Operand);
            }

            if (expression is SubQueryExpression)
            {
                return GetWhereFragment(mapping, expression.As<SubQueryExpression>());
            }

            throw new NotSupportedException();
        }

        private IWhereFragment GetWhereFragment(IDocumentMapping mapping, SubQueryExpression expression)
        {
            var queryType = expression.QueryModel.MainFromClause.ItemType;
            if (TypeMappings.HasTypeMapping(queryType))
            {
                var contains = expression.QueryModel.ResultOperators.OfType<ContainsResultOperator>().FirstOrDefault();
                if (contains != null)
                {
                    return ContainmentWhereFragment.SimpleArrayContains(_serializer, expression.QueryModel.MainFromClause.FromExpression, Value(contains.Item));
                }
            }

            if (expression.QueryModel.ResultOperators.Any(x => x is AnyResultOperator))
            {
                return new CollectionAnyContainmentWhereFragment(_serializer, expression);
            }

            throw new NotImplementedException();
        }

        private IWhereFragment GetNotWhereFragment(IDocumentMapping mapping, Expression expression)
        {
            if (expression is MemberExpression && expression.Type == typeof (bool))
            {
                var locator = JsonLocator(mapping, expression.As<MemberExpression>());
                return new WhereFragment("({0})::Boolean = False".ToFormat(locator));
            }

            if (expression.Type == typeof (bool) && expression.NodeType == ExpressionType.NotEqual &&
                expression is BinaryExpression)
            {
                var binaryExpression = expression.As<BinaryExpression>();
                var locator = JsonLocator(mapping, binaryExpression.Left);
                if (binaryExpression.Right.NodeType == ExpressionType.Constant &&
                    binaryExpression.Right.As<ConstantExpression>().Value == null)
                {
                    return new WhereFragment($"({locator})::Boolean IS NULL");
                }
            }

            throw new NotSupportedException();
        }

        private IWhereFragment GetMethodCall(IDocumentMapping mapping, MethodCallExpression expression)
        {
            // TODO -- generalize this mess
            if (expression.Method.Name == CONTAINS)
            {
                var @object = expression.Object;

                if (@object.Type == typeof (string))
                {
                    var locator = JsonLocator(mapping, @object);
                    var value = Value(expression.Arguments.Single()).As<string>();
                    return new WhereFragment("{0} like ?".ToFormat(locator), "%" + value + "%");
                }

                if (@object.Type.IsGenericEnumerable())
                {
                    var value = Value(expression.Arguments.Single());
                    return ContainmentWhereFragment.SimpleArrayContains(_serializer, @object,
                        value);
                }
            }

            if (expression.Method.Name == STARTS_WITH)
            {
                var @object = expression.Object;
                if (@object.Type == typeof (string))
                {
                    var locator = JsonLocator(mapping, @object);
                    var value = Value(expression.Arguments.Single()).As<string>();
                    return new WhereFragment("{0} like ?".ToFormat(locator), value + "%");
                }
            }

            if (expression.Method.Name == ENDS_WITH)
            {
                var @object = expression.Object;
                if (@object.Type == typeof (string))
                {
                    var locator = JsonLocator(mapping, @object);
                    var value = Value(expression.Arguments.Single()).As<string>();
                    return new WhereFragment("{0} like ?".ToFormat(locator), "%" + value);
                }
            }

            throw new NotImplementedException();
        }

        public IWhereFragment GetWhereFragment(IDocumentMapping mapping, BinaryExpression binary)
        {
            if (_operators.ContainsKey(binary.NodeType))
            {
                return buildSimpleWhereClause(mapping, binary);
            }


            switch (binary.NodeType)
            {
                case ExpressionType.AndAlso:
                    return new CompoundWhereFragment("and", ParseWhereFragment(mapping, binary.Left),
                        ParseWhereFragment(mapping, binary.Right));

                case ExpressionType.OrElse:
                    return new CompoundWhereFragment("or", ParseWhereFragment(mapping, binary.Left),
                        ParseWhereFragment(mapping, binary.Right));
            }

            throw new NotSupportedException();
        }

        private IWhereFragment buildSimpleWhereClause(IDocumentMapping mapping, BinaryExpression binary)
        {
            var op = _operators[binary.NodeType];

            var value = Value(binary.Right);

            if (mapping.PropertySearching == PropertySearching.ContainmentOperator &&
                binary.NodeType == ExpressionType.Equal && value != null)
            {
                return new ContainmentWhereFragment(_serializer, binary);
            }

            var jsonLocator = JsonLocator(mapping, binary.Left);

            if (value == null)
            {
                var sql = binary.NodeType == ExpressionType.NotEqual
                    ? $"{jsonLocator} is not null"
                    : $"{jsonLocator} is null";

                return new WhereFragment(sql);
            }


            return new WhereFragment("{0} {1} ?".ToFormat(jsonLocator, op), value);
        }

        public static object Value(Expression expression)
        {
            if (expression is PartialEvaluationExceptionExpression)
            {
                var partialEvaluationExceptionExpression = expression.As<PartialEvaluationExceptionExpression>();
                var inner = partialEvaluationExceptionExpression.Exception;

                throw new BadLinqExpressionException($"Error in value expression inside of the query for '{partialEvaluationExceptionExpression.EvaluatedExpression}'. See the inner exception:", inner);
            }

            if (expression is ConstantExpression)
            {
                // TODO -- handle nulls
                // TODO -- check out more types here.
                return expression.As<ConstantExpression>().Value;
            }

            throw new NotSupportedException();
        }

        // TODO -- use the mapping off of DocumentQuery later
        public string JsonLocator(IDocumentMapping mapping, Expression expression)
        {
            var visitor = new FindMembers();
            visitor.Visit(expression);

            //return new JsonLocatorField(visitor.Members.ToArray()).SqlLocator;

            var field = visitor.GetField(mapping);

            _query.RegisterField(field);

            return field.SqlLocator;
        }
    }

    [Serializable]
    public class BadLinqExpressionException : Exception
    {
        public BadLinqExpressionException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected BadLinqExpressionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}