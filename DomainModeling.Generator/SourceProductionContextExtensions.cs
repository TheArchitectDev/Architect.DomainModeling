using Microsoft.CodeAnalysis;

namespace Architect.DomainModeling.Generator
{
	/// <summary>
	/// Defines extension methods on <see cref="SourceProductionContext"/>.
	/// </summary>
	public static class SourceProductionContextExtensions
	{
		/// <summary>
		/// Shorthand extension method to report a diagnostic, with less boilerplate code.
		/// </summary>
		public static void ReportDiagnostic(this SourceProductionContext context, string id, string title, string description, DiagnosticSeverity severity, ISymbol? symbol = null)
		{
			context.ReportDiagnostic(id, title, description, severity, symbol?.Locations.FirstOrDefault());
		}

		/// <summary>
		/// Shorthand extension method to report a diagnostic, with less boilerplate code.
		/// </summary>
		public static void ReportDiagnostic(this SourceProductionContext context, string id, string title, string description, DiagnosticSeverity severity, Location? location)
		{
			context.ReportDiagnostic(Diagnostic.Create(
				new DiagnosticDescriptor(id, title, description, "Architect.DomainModeling", severity, isEnabledByDefault: true),
				location));
		}
	}
}
