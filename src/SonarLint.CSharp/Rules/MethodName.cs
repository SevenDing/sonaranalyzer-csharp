﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2015 SonarSource
 * sonarqube@googlegroups.com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02
 */

using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarLint.Common;
using SonarLint.Common.Sqale;
using SonarLint.Helpers;

namespace SonarLint.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [SqaleSubCharacteristic(SqaleSubCharacteristic.Readability)]
    [SqaleConstantRemediation("5min")]
    [Rule(DiagnosticId, RuleSeverity, Title, IsActivatedByDefault)]
    [Tags(Tag.Convention)]
    public class MethodName : DiagnosticAnalyzer
    {
        internal const string DiagnosticId = "S100";
        internal const string Title = "Method name should comply with a naming convention";
        internal const string Description =
            "Shared naming conventions allow teams to collaborate efficiently. This rule checks that all method names match a provided " +
            "regular expression.";
        internal const string MessageFormat = "Rename this method \"{1}\" to match the regular expression {0}";
        internal const string Category = SonarLint.Common.Category.Naming;
        internal const Severity RuleSeverity = Severity.Minor;
        internal const bool IsActivatedByDefault = false;

        internal static readonly DiagnosticDescriptor Rule =
            new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category,
                RuleSeverity.ToDiagnosticSeverity(), IsActivatedByDefault,
                helpLinkUri: DiagnosticId.GetHelpLink(),
                description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        private const string DefaultValueConvention = "^[A-Z][a-zA-Z0-9_]*[a-zA-Z0-9]$";

        [RuleParameter("format", PropertyType.String, "Regular expression used to check the method names against",
            DefaultValueConvention)]
        public string Convention { get; set; } = DefaultValueConvention;

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeActionInNonGenerated(
                c =>
                {
                    var identifierNode = ((MethodDeclarationSyntax)c.Node).Identifier;

                    if (!Regex.IsMatch(identifierNode.Text, Convention))
                    {
                        c.ReportDiagnostic(Diagnostic.Create(Rule, identifierNode.GetLocation(), Convention, identifierNode.Text));
                    }
                },
                SyntaxKind.MethodDeclaration);
        }
    }
}
