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

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarLint.Common;
using SonarLint.Common.Sqale;
using SonarLint.Helpers;

namespace SonarLint.Rules.CSharp
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    [SqaleConstantRemediation("5min")]
    [SqaleSubCharacteristic(SqaleSubCharacteristic.Understandability)]
    [Rule(DiagnosticId, RuleSeverity, Title, IsActivatedByDefault)]
    [Tags(Tag.Convention)]
    public class ClassShouldNotBeAbstract : DiagnosticAnalyzer
    {
        internal const string DiagnosticId = "S1694";
        internal const string Title = "An abstract class should have both abstract and concrete methods";
        internal const string Description =
            "The purpose of an abstract class is to provide some heritable behaviors while also defining methods which must be " +
            "implemented by sub-classes. A class with no abstract methods that was made abstract purely to prevent instantiation " +
            "should be converted to a concrete class (i.e. remove the \"abstract\" keyword) with a private constructor. A class " +
            "with only abstract methods and no inheritable behavior should be converted to an interface.";
        internal const string MessageFormat = "Convert this \"abstract\" class to {0}";
        internal const string MessageToInterface = "an interface";
        internal const string MessageToConcreteClass = "a concrete class with a private constructor";
        internal const string Category = SonarLint.Common.Category.Design;
        internal const Severity RuleSeverity = Severity.Minor;
        internal const bool IsActivatedByDefault = false;

        internal static readonly DiagnosticDescriptor Rule =
            new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category,
                RuleSeverity.ToDiagnosticSeverity(), IsActivatedByDefault,
                helpLinkUri: DiagnosticId.GetHelpLink(),
                description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSymbolAction(
                c =>
                {
                    var symbol = c.Symbol as INamedTypeSymbol;
                    if (symbol == null ||
                        !symbol.IsAbstract ||
                        symbol.TypeKind != TypeKind.Class ||
                        ClassHasInheritedAbstractMembers(symbol))
                    {
                        return;
                    }

                    if (AbstractClassShouldBeInterface(symbol))
                    {
                        ReportClass(symbol, MessageToInterface, c);
                        return;
                    }

                    if (AbstractClassShouldBeConcreteClass(symbol))
                    {
                        ReportClass(symbol, MessageToConcreteClass, c);
                        return;
                    }
                },
                SymbolKind.NamedType);
        }

        private static bool ClassHasInheritedAbstractMembers(INamedTypeSymbol classSymbol)
        {
            var baseTypes = GetAllBaseTypes(classSymbol);
            var abstractMethods = baseTypes.SelectMany(baseType => GetAllAbstractMethods(baseType));
            var baseTypesAndSelf = baseTypes.Concat(new[] { classSymbol }).ToList();
            var overrideMethods = baseTypesAndSelf.SelectMany(baseType => GetAllOverrideMethods(baseType));
            var overriddenMethods = overrideMethods.Select(m => m.OverriddenMethod);
            var stillAbstractMethods = abstractMethods.Except(overriddenMethods);

            return stillAbstractMethods.Any();
        }

        private static IEnumerable<IMethodSymbol> GetAllAbstractMethods(INamedTypeSymbol classSymbol)
        {
            return GetAllMethods(classSymbol).Where(m => m.IsAbstract);
        }
        private static IEnumerable<IMethodSymbol> GetAllOverrideMethods(INamedTypeSymbol classSymbol)
        {
            return GetAllMethods(classSymbol).Where(m => m.IsOverride);
        }

        private static IList<INamedTypeSymbol> GetAllBaseTypes(INamedTypeSymbol classSymbol)
        {
            var list = new List<INamedTypeSymbol>();
            var baseType = classSymbol.BaseType;
            while (baseType != null)
            {
                list.Add(baseType);
                baseType = baseType.BaseType;
            }
            return list;
        }

        private static void ReportClass(INamedTypeSymbol symbol, string message, SymbolAnalysisContext c)
        {
            foreach (var declaringSyntaxReference in symbol.DeclaringSyntaxReferences)
            {
                var classDeclaration = declaringSyntaxReference.GetSyntax() as ClassDeclarationSyntax;
                if (classDeclaration != null)
                {
                    c.ReportDiagnosticIfNonGenerated(
                        Diagnostic.Create(Rule, classDeclaration.Identifier.GetLocation(), message),
                        c.Compilation);
                }
            }
        }

        private static bool AbstractClassShouldBeInterface(INamedTypeSymbol classSymbol)
        {
            var methods = GetAllMethods(classSymbol);
            return classSymbol.BaseType.SpecialType == SpecialType.System_Object &&
                   methods.Any() &&
                   methods.All(method => method.IsAbstract);
        }

        private static bool AbstractClassShouldBeConcreteClass(INamedTypeSymbol classSymbol)
        {
            var methods = GetAllMethods(classSymbol);
            return !methods.Any() ||
                   methods.All(method => !method.IsAbstract);
        }

        private static IList<IMethodSymbol> GetAllMethods(INamedTypeSymbol classSymbol)
        {
            return classSymbol.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(method => !method.IsImplicitlyDeclared || !ConstructorKinds.Contains(method.MethodKind))
                .ToList();
        }

        private static readonly MethodKind[] ConstructorKinds =
        {
            MethodKind.Constructor,
            MethodKind.SharedConstructor
        };
    }
}
