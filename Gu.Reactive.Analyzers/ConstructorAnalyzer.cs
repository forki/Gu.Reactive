﻿namespace Gu.Reactive.Analyzers
{
    using System;
    using System.Collections.Immutable;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ConstructorAnalyzer : DiagnosticAnalyzer
    {
        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
            GUREA02ObservableAndCriteriaMustMatch.Descriptor,
            GUREA06DontNewCondition.Descriptor,
            GUREA09ObservableBeforeCriteria.Descriptor,
            GUREA13SyncParametersAndArgs.Descriptor);

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(Handle, SyntaxKind.BaseConstructorInitializer, SyntaxKind.ObjectCreationExpression);
        }

        private static void Handle(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            if (context.Node is ConstructorInitializerSyntax initializer &&
                context.SemanticModel.GetSymbolSafe(initializer, context.CancellationToken) is IMethodSymbol baseCtor)
            {
                if (baseCtor.ContainingType == KnownSymbol.Condition)
                {
                    if (TryGetObservableAndCriteriaMismatch(initializer.ArgumentList, baseCtor, context, out var observedText, out var criteriaText, out var missingText))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(GUREA02ObservableAndCriteriaMustMatch.Descriptor, initializer.GetLocation(), observedText, criteriaText, missingText));
                    }

                    if (baseCtor.Parameters[0].Type == KnownSymbol.FuncOfT &&
                        baseCtor.Parameters[1].Type == KnownSymbol.IObservableOfT)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(GUREA09ObservableBeforeCriteria.Descriptor, initializer.ArgumentList.GetLocation()));
                    }
                }
                else if (baseCtor.ContainingType.IsEither(KnownSymbol.AndCondition, KnownSymbol.OrCondition) &&
                    HasMatchingArgumentAndParameterPositions(initializer, context) == false)
                {
                    context.ReportDiagnostic(Diagnostic.Create(GUREA13SyncParametersAndArgs.Descriptor, initializer.ArgumentList.GetLocation()));
                }
            }
            else if (context.Node is ObjectCreationExpressionSyntax objectCreation &&
                     context.SemanticModel.GetSymbolSafe(objectCreation, context.CancellationToken) is IMethodSymbol ctor)
            {
                if (ctor.ContainingType == KnownSymbol.Condition)
                {
                    if (TryGetObservableAndCriteriaMismatch(objectCreation.ArgumentList, ctor, context, out var observedText, out var criteriaText, out var missingText))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(GUREA02ObservableAndCriteriaMustMatch.Descriptor, objectCreation.GetLocation(), observedText, criteriaText, missingText));
                    }

                    if (ctor.Parameters[0].Type == KnownSymbol.FuncOfT &&
                        ctor.Parameters[1].Type == KnownSymbol.IObservableOfT)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(GUREA09ObservableBeforeCriteria.Descriptor, objectCreation.ArgumentList.GetLocation()));
                    }
                }

                if (ctor.ContainingType.Is(KnownSymbol.Condition))
                {
                    context.ReportDiagnostic(Diagnostic.Create(GUREA06DontNewCondition.Descriptor, objectCreation.GetLocation()));
                }
            }
        }

        private static bool TryGetObservableAndCriteriaMismatch(ArgumentListSyntax argumentList, IMethodSymbol ctor, SyntaxNodeAnalysisContext context, out string observedText, out string criteriaText, out string missingText)
        {
            var observableArg = ObservableArg(argumentList, ctor);
            var criteriaArg = CriteriaArg(argumentList, ctor);
            using (var observableIdentifiers = IdentifierNameWalker.Create(observableArg, Search.Recursive, context.SemanticModel, context.CancellationToken))
            {
                using (var criteriaIdentifiers = IdentifierNameWalker.Create(criteriaArg, Search.Recursive, context.SemanticModel, context.CancellationToken))
                {
                    using (var observed = SetPool<IPropertySymbol>.Create())
                    {
                        foreach (var name in observableIdentifiers.Item.IdentifierNames)
                        {
                            if (context.SemanticModel.GetSymbolSafe(name, context.CancellationToken) is IPropertySymbol property)
                            {
                                observed.Item.Add(property);
                            }
                        }

                        using (var usedInCriteria = SetPool<IPropertySymbol>.Create())
                        {
                            foreach (var name in criteriaIdentifiers.Item.IdentifierNames)
                            {
                                if (context.SemanticModel.GetSymbolSafe(name, context.CancellationToken) is IPropertySymbol property)
                                {
                                    if (!property.ContainingType.IsValueType &&
                                        !property.IsGetOnly())
                                    {
                                        usedInCriteria.Item.Add(property);
                                    }
                                }
                            }

                            using (var missing = SetPool<IPropertySymbol>.Create())
                            {
                                missing.Item.UnionWith(usedInCriteria.Item);
                                missing.Item.ExceptWith(observed.Item);
                                if (missing.Item.Count != 0)
                                {
                                    observedText = string.Join(Environment.NewLine, observed.Item.Select(p => $"  {p}"));
                                    criteriaText = string.Join(Environment.NewLine, usedInCriteria.Item.Select(p => $"  {p}"));
                                    missingText = string.Join(Environment.NewLine, missing.Item.Select(p => $"  {p}"));
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            observedText = null;
            criteriaText = null;
            missingText = null;
            return false;
        }

        private static bool? HasMatchingArgumentAndParameterPositions(ConstructorInitializerSyntax initializer, SyntaxNodeAnalysisContext context)
        {
            if (initializer?.ArgumentList == null)
            {
                return null;
            }

            if (context.SemanticModel.GetDeclaredSymbolSafe(initializer.Parent, context.CancellationToken) is IMethodSymbol ctor)
            {
                if (ctor.Parameters.Length != initializer.ArgumentList.Arguments.Count)
                {
                    return null;
                }

                for (var i = 0; i < initializer.ArgumentList.Arguments.Count; i++)
                {
                    var argument = initializer.ArgumentList.Arguments[i];
                    if (argument.Expression is IdentifierNameSyntax argName &&
                        argName.Identifier.ValueText != ctor.Parameters[i].Name)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static ArgumentSyntax ObservableArg(ArgumentListSyntax argumentList, IMethodSymbol ctor)
        {
            if (ctor.Parameters[0].Type == KnownSymbol.FuncOfT &&
                ctor.Parameters[1].Type == KnownSymbol.IObservableOfT)
            {
                return argumentList.Arguments[1];
            }

            return argumentList.Arguments[0];
        }

        private static ArgumentSyntax CriteriaArg(ArgumentListSyntax argumentList, IMethodSymbol ctor)
        {
            if (ctor.Parameters[0].Type == KnownSymbol.FuncOfT &&
                ctor.Parameters[1].Type == KnownSymbol.IObservableOfT)
            {
                return argumentList.Arguments[0];
            }

            return argumentList.Arguments[1];
        }
    }
}