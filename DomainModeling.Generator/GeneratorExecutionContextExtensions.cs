using Microsoft.CodeAnalysis;

namespace Architect.DomainModeling.Generator
{
	/// <summary>
	/// Defines extension methods on <see cref="GeneratorExecutionContext"/>.
	/// </summary>
	public static class GeneratorExecutionContextExtensions
	{
		/// <summary>
		/// Shorthand extension method to report a diagnostic, with less boilerplate code.
		/// </summary>
		public static void ReportDiagnostic(this GeneratorExecutionContext context, string id, string title, string description, DiagnosticSeverity severity, ISymbol? symbol = null)
		{
			context.ReportDiagnostic(Diagnostic.Create(
				new DiagnosticDescriptor(id, title, description, "Architect.DomainModeling", severity, isEnabledByDefault: true),
				symbol?.Locations.FirstOrDefault()));
		}
	}
}
