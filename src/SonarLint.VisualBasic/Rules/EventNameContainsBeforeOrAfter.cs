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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarLint.Common;
using SonarLint.Common.Sqale;
using SonarLint.Helpers;

namespace SonarLint.Rules.VisualBasic
{
    [DiagnosticAnalyzer(LanguageNames.VisualBasic)]
    [SqaleConstantRemediation("5min")]
    [SqaleSubCharacteristic(SqaleSubCharacteristic.Understandability)]
    [Rule(DiagnosticId, RuleSeverity, Title, IsActivatedByDefault)]
    [Tags(Tag.Convention)]
    public class EventNameContainsBeforeOrAfter : DiagnosticAnalyzer
    {
        internal const string DiagnosticId = "S2349";
        internal const string Title = "Event names should not have \"Before\" or \"After\" as a prefix or suffix";
        internal const string Description =
            "\"After\" and \"Before\" prefixes or suffixes should not be used to indicate pre and post events. The concepts of " +
            "before and after should be given to events using the present and past tense.";
        internal const string MessageFormat = "Rename this event to remove the \"{0}\" {1}.";
        internal const string Category = SonarLint.Common.Category.Naming;
        internal const Severity RuleSeverity = Severity.Minor;
        internal const bool IsActivatedByDefault = true;

        internal static readonly DiagnosticDescriptor Rule =
            new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category,
                RuleSeverity.ToDiagnosticSeverity(), IsActivatedByDefault,
                helpLinkUri: DiagnosticId.GetHelpLink(),
                description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        private const string PrefixLiteral = "prefix";
        private const string SuffixLiteral = "suffix";
        private const string AfterLiteral = "after";
        private const string BeforeLiteral = "before";

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeActionInNonGenerated(
                c =>
                {
                    var eventStatement = (EventStatementSyntax)c.Node;
                    var name = eventStatement.Identifier.ValueText;

                    string part;
                    string matched;

                    if (name.StartsWith(BeforeLiteral, System.StringComparison.InvariantCultureIgnoreCase))
                    {
                        part = PrefixLiteral;
                        matched = name.Substring(0, BeforeLiteral.Length);
                    }
                    else if (name.StartsWith(AfterLiteral, System.StringComparison.InvariantCultureIgnoreCase))
                    {
                        part = PrefixLiteral;
                        matched = name.Substring(0, AfterLiteral.Length);
                    }
                    else if (name.EndsWith(BeforeLiteral, System.StringComparison.InvariantCultureIgnoreCase))
                    {
                        part = SuffixLiteral;
                        matched = name.Substring(name.Length - 1 - BeforeLiteral.Length);
                    }
                    else if (name.EndsWith(AfterLiteral, System.StringComparison.InvariantCultureIgnoreCase))
                    {
                        part = SuffixLiteral;
                        matched = name.Substring(name.Length - 1 - AfterLiteral.Length);
                    }
                    else
                    {
                        return;
                    }

                    c.ReportDiagnostic(Diagnostic.Create(Rule, eventStatement.Identifier.GetLocation(), matched, part));
                },
                SyntaxKind.EventStatement);
        }
    }
}
