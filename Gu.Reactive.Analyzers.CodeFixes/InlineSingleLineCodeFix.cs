namespace Gu.Reactive.Analyzers.CodeFixes
{
    using System.Collections.Immutable;
    using System.Composition;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CodeActions;
    using Microsoft.CodeAnalysis.CodeFixes;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Editing;

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(InlineSingleLineCodeFix))]
    [Shared]
    public class InlineSingleLineCodeFix : CodeFixProvider
    {
        /// <inheritdoc/>
        public override ImmutableArray<string> FixableDiagnosticIds { get; } =
            ImmutableArray.Create(GUREA08InlineSingleLine.DiagnosticId);

        /// <inheritdoc/>
        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        /// <inheritdoc/>
        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken)
                                          .ConfigureAwait(false);
            foreach (var diagnostic in context.Diagnostics)
            {
                var token = syntaxRoot.FindToken(diagnostic.Location.SourceSpan.Start);
                if (string.IsNullOrEmpty(token.ValueText) ||
                    token.IsMissing)
                {
                    continue;
                }

                var invocation = syntaxRoot.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true).FirstAncestorOrSelf<InvocationExpressionSyntax>();
                if (invocation != null)
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            "Inline.",
                            cancellationToken => ApplyInlineSingleLineAsync(cancellationToken, context, invocation),
                            nameof(InlineSingleLineCodeFix)),
                        diagnostic);
                    continue;
                }
            }
        }

        private static async Task<Document> ApplyInlineSingleLineAsync(CancellationToken cancellationToken, CodeFixContext context, InvocationExpressionSyntax invocation)
        {
            var editor = await DocumentEditor.CreateAsync(context.Document, cancellationToken)
                                             .ConfigureAwait(false);
            var semanticModel = editor.SemanticModel;
            var method = (IMethodSymbol)semanticModel.GetSymbolSafe(invocation.Expression, cancellationToken);
            var methodDeclaration = (MethodDeclarationSyntax)method.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken);
            var expression = methodDeclaration.ExpressionBody?.Expression ??
                             ((ReturnStatementSyntax)methodDeclaration.Body.Statements[0]).Expression;

            if (invocation.ArgumentList?.Arguments.Count == 1 &&
                semanticModel.GetSymbolSafe(invocation.ArgumentList.Arguments[0].Expression, cancellationToken) is IParameterSymbol parameter)
            {
                expression = Rename.Parameter(method.Parameters[0], parameter, semanticModel, cancellationToken, expression);
                editor.ReplaceNode(
                    invocation,
                    expression.WithLeadingTrivia(invocation.Expression.GetLeadingTrivia()));
                editor.RemoveNode(methodDeclaration);
                return editor.GetChangedDocument();
            }

            return editor.OriginalDocument;
        }

        private class Rename : CSharpSyntaxRewriter
        {
            private readonly IParameterSymbol @from;
            private readonly IParameterSymbol to;
            private readonly SemanticModel semanticModel;
            private readonly CancellationToken cancellationToken;

            private Rename(IParameterSymbol from, IParameterSymbol to, SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                this.@from = @from;
                this.to = to;
                this.semanticModel = semanticModel;
                this.cancellationToken = cancellationToken;
            }

            public static T Parameter<T>(
                IParameterSymbol from,
                IParameterSymbol to,
                SemanticModel semanticModel,
                CancellationToken cancellationToken,
                T node)
                where T : SyntaxNode
            {
                if (from.Name == to.Name)
                {
                    return node;
                }

                var rename = new Rename(@from, to, semanticModel, cancellationToken);
                return (T)rename.Visit(node);
            }

            public override SyntaxNode VisitIdentifierName(IdentifierNameSyntax node)
            {
                if (node.Identifier.ValueText == this.from.Name)
                {
                    if (SymbolComparer.Equals(this.semanticModel.GetSymbolSafe(node, this.cancellationToken), this.from))
                    {
                        return SyntaxFactory.IdentifierName(this.to.Name);
                    }
                }

                return base.VisitIdentifierName(node);
            }
        }
    }
}