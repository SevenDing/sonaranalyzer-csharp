﻿/*
 * SonarQube C# Code Analysis
 * Copyright (C) 2015 SonarSource
 * dev@sonar.codehaus.org
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.Diagnostics;
using SonarQube.CSharp.CodeAnalysis.SonarQube.Settings;

namespace SonarQube.CSharp.CodeAnalysis.Runner
{
    public class RuleFinder
    {
        private readonly List<Type> diagnosticAnalyzers;
        public const string RuleDescriptionPathPattern = "SonarQube.CSharp.CodeAnalysis.Rules.Description.{0}.html";
        public const string RuleAssemblyName = "SonarQube.CSharp.CodeAnalysis";
        public const string RuleAssemblyFileName = RuleAssemblyName + ".dll";
        public const string RuleExtraAssemblyFileName = RuleAssemblyName + ".Extra.dll";

        public static IList<Assembly> GetRuleAssemblies()
        {
            return new[]
            {
                Assembly.LoadFrom(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, RuleAssemblyFileName)),
                Assembly.LoadFrom(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, RuleExtraAssemblyFileName))
            };
        }

        public RuleFinder()
        {
            diagnosticAnalyzers = GetRuleAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsSubclassOf(typeof (DiagnosticAnalyzer)))
                .Where(t => t.GetCustomAttributes<RuleAttribute>().Any())
                .ToList();
        }

        public IEnumerable<Type> GetParameterlessAnalyzerTypes()
        {
            return diagnosticAnalyzers
                .Where(analyzerType =>
                    !analyzerType.GetCustomAttributes<RuleAttribute>().First().Template)
                .Where(analyzerType =>
                    !analyzerType.GetProperties()
                        .Any(p => p.GetCustomAttributes<RuleParameterAttribute>().Any()));
        }

        public IEnumerable<Type> GetAllAnalyzerTypes()
        {
            return diagnosticAnalyzers;
        }
    }
}