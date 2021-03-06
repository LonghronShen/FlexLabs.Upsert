﻿using System;
using System.Linq.Expressions;

namespace FlexLabs.EntityFrameworkCore.Upsert
{
    /// <summary>
    /// A class that represents a known type of expression
    /// </summary>
    public class KnownExpressions
    {
        /// <summary>
        /// Initialises a new instance of the class
        /// </summary>
        /// <param name="sourceType">The type of the object that the property is read from</param>
        /// <param name="sourceProperty">The name of the property that is read</param>
        /// <param name="expressionType">The type of the operation being executed</param>
        /// <param name="value">The value used in the expression</param>
        public KnownExpressions(Type sourceType, string sourceProperty, ExpressionType expressionType, object value)
        {
            SourceType = sourceType;
            SourceProperty = sourceProperty;
            ExpressionType = expressionType;
            Value = value;
        }

        /// <summary>
        /// The type of the object that the property is read from
        /// </summary>
        public Type SourceType { get; }

        /// <summary>
        /// The name of the property that is read
        /// </summary>
        public string SourceProperty { get; }

        /// <summary>
        /// The type of the operation being executed
        /// </summary>
        public ExpressionType ExpressionType { get; }

        /// <summary>
        /// The value used in the expression
        /// </summary>
        public object Value { get; }
    }
}
