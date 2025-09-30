using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Architect.DomainModeling.CodeFixProviders;

internal static class SyntaxNodeExtensions
{
	/// <summary>
	/// Inspecs the node, such as a root node, for its newline trivia.
	/// Returns ElasticCarriageReturnLineFeed if \r\n is the most common, and ElasticLineFeed otherwise.
	/// </summary>
	public static SyntaxTrivia GetNewlineTrivia(this SyntaxNode node)
	{
		var allTrivia = node.DescendantTrivia(descendIntoTrivia: true);

		var (nCount, rnCount) = (0, 0);

		foreach (var trivia in allTrivia)
		{
			if (!trivia.IsKind(SyntaxKind.EndOfLineTrivia))
				continue;

			var length = trivia.Span.Length;
			var lengthIsOne = length == 1;
			var lengthIsTwo = length == 2;
			nCount += Unsafe.As<bool, int>(ref lengthIsOne);
			rnCount += Unsafe.As<bool, int>(ref lengthIsTwo);
		}

		return rnCount > nCount
			? SyntaxFactory.ElasticCarriageReturnLineFeed
			: SyntaxFactory.ElasticLineFeed;
	}
}
