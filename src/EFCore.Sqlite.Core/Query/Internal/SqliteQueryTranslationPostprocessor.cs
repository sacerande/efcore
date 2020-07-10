// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Sqlite.Internal;

namespace Microsoft.EntityFrameworkCore.Sqlite.Query.Internal
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public class SqliteQueryTranslationPostprocessor : RelationalQueryTranslationPostprocessor
    {
        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public SqliteQueryTranslationPostprocessor(
            QueryTranslationPostprocessorDependencies dependencies,
            RelationalQueryTranslationPostprocessorDependencies relationalDependencies,
            QueryCompilationContext queryCompilationContext)
            : base(dependencies, relationalDependencies, queryCompilationContext)
        {
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public override Expression Process(Expression query)
        {
            var result = base.Process(query);

            if (result is ShapedQueryExpression shapedQueryExpression)
            {
                var applyValidator = new ApplyValidatingVisitor();
                applyValidator.Visit(shapedQueryExpression.QueryExpression);
                if (!applyValidator.ApplyFound)
                {
                    applyValidator.Visit(shapedQueryExpression.ShaperExpression);
                }

                if (applyValidator.ApplyFound)
                {
                    throw new InvalidOperationException(SqliteStrings.ApplyNotSupported);
                }
            }

            return result;
        }

        private class ApplyValidatingVisitor : ExpressionVisitor
        {
            public bool ApplyFound { get; private set; } = false;

            protected override Expression VisitExtension(Expression extensionExpression)
            {
                if (extensionExpression is SelectExpression selectExpression
                    && selectExpression.Tables.Any(t => t is CrossApplyExpression || t is OuterApplyExpression))
                {
                    ApplyFound = true;

                    return extensionExpression;
                }

                return base.VisitExtension(extensionExpression);
            }
        }
    }
}
