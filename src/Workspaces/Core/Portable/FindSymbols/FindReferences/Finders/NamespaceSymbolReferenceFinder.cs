﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
    internal class NamespaceSymbolReferenceFinder : AbstractReferenceFinder<INamespaceSymbol>
    {
        private static readonly SymbolDisplayFormat s_globalNamespaceFormat = new(SymbolDisplayGlobalNamespaceStyle.Included);

        protected override bool CanFind(INamespaceSymbol symbol)
            => true;

        protected override async Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(
            INamespaceSymbol symbol,
            Project project,
            IImmutableSet<Document>? documents,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            var documentsWithName = await FindDocumentsAsync(project, documents, cancellationToken, GetNamespaceIdentifierName(symbol)).ConfigureAwait(false);
            var documentsWithGlobalAttributes = await FindDocumentsWithGlobalAttributesAsync(project, documents, cancellationToken).ConfigureAwait(false);
            return documentsWithGlobalAttributes.Concat(documentsWithName);
        }

        private static string GetNamespaceIdentifierName(INamespaceSymbol symbol)
        {
            return symbol.IsGlobalNamespace
                ? symbol.ToDisplayString(s_globalNamespaceFormat)
                : symbol.Name;
        }

        protected override async ValueTask<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
            INamespaceSymbol symbol,
            Document document,
            SemanticModel semanticModel,
            FindReferencesSearchOptions options,
            CancellationToken cancellationToken)
        {
            var identifierName = GetNamespaceIdentifierName(symbol);
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

            var tokens = await GetIdentifierOrGlobalNamespaceTokensWithTextAsync(
                document, semanticModel, identifierName, cancellationToken).ConfigureAwait(false);
            var nonAliasReferences = await FindReferencesInTokensAsync(
                symbol,
                document,
                semanticModel,
                tokens,
                t => syntaxFacts.TextMatch(t.ValueText, identifierName),
                cancellationToken).ConfigureAwait(false);

            var aliasReferences = await FindAliasReferencesAsync(nonAliasReferences, symbol, document, semanticModel, cancellationToken).ConfigureAwait(false);

            var suppressionReferences = await FindReferencesInDocumentInsideGlobalSuppressionsAsync(document, semanticModel, symbol, cancellationToken).ConfigureAwait(false);
            return nonAliasReferences.Concat(aliasReferences, suppressionReferences);
        }
    }
}
