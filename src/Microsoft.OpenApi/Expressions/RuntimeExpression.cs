﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System;
using Microsoft.OpenApi.Exceptions;
using Microsoft.OpenApi.Properties;

namespace Microsoft.OpenApi.Expressions
{
    /// <summary>
    /// Base class for the Open API runtime expression.
    /// </summary>
    public abstract class RuntimeExpression : IEquatable<RuntimeExpression>
    {
        /// <summary>
        /// The dollar sign prefix for a runtime expression.
        /// </summary>
        public const string Prefix = "$";

        /// <summary>
        /// The expression string.
        /// </summary>
        public abstract string Expression { get; }

        /// <summary>
        /// Build the runtime expression from input string.
        /// </summary>
        /// <param name="expression">The runtime expression.</param>
        /// <returns>The built runtime expression object.</returns>
        public static RuntimeExpression Build(string expression)
        {
            Utils.CheckArgumentNullOrEmpty(expression);

            if (!expression.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            {
                return new CompositeExpression(expression);
            }

            // $url
            if (expression.Equals(UrlExpression.Url, StringComparison.Ordinal))
            {
                return new UrlExpression();
            }

            // $method
            if (expression.Equals(MethodExpression.Method, StringComparison.Ordinal))
            {
                return new MethodExpression();
            }

            // $statusCode
            if (expression.Equals(StatusCodeExpression.StatusCode, StringComparison.Ordinal))
            {
                return new StatusCodeExpression();
            }

            // $request.
            if (expression.StartsWith(RequestExpression.Request, StringComparison.Ordinal))
            {
                var subString = expression.Substring(RequestExpression.Request.Length);
                var source = SourceExpression.Build(subString);
                return new RequestExpression(source);
            }

            // $response.
            if (expression.StartsWith(ResponseExpression.Response, StringComparison.Ordinal))
            {
                var subString = expression.Substring(ResponseExpression.Response.Length);
                var source = SourceExpression.Build(subString);
                return new ResponseExpression(source);
            }

            throw new OpenApiException(string.Format(SRResource.RuntimeExpressionHasInvalidFormat, expression));
        }

        /// <summary>
        /// GetHashCode implementation for IEquatable.
        /// </summary>
        public override int GetHashCode()
        {
            return Expression.GetHashCode();
        }

        /// <summary>
        /// Equals implementation for IEquatable.
        /// </summary>
        public override bool Equals(object? obj)
        {
            return Equals(obj as RuntimeExpression);
        }

        /// <summary>
        /// Equals implementation for object of the same type.
        /// </summary>
        public bool Equals(RuntimeExpression? obj)
        {
            return obj != null && obj.Expression == Expression;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Expression;
        }
    }
}
