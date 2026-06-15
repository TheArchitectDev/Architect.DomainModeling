using Architect.DomainModeling.Generator.Common;
using Microsoft.CodeAnalysis;

namespace Architect.DomainModeling.Generator;

/// <summary>
/// Defines extension methods on <see cref="SourceProductionContext"/>.
/// </summary>
internal static class DiagnosticReportingExtensions
{
	/// <summary>
	/// <para>
	/// Shorthand extension method to report a diagnostic, with less boilerplate code.
	/// </para>
	/// <para>
	/// This overload is only available when the compilation is available at generation time.
	/// </para>
	/// </summary>
	public static void ReportDiagnostic(this SourceProductionContext context, string id, string title, string description, DiagnosticSeverity severity, ISymbol? symbol = null)
	{
		context.ReportDiagnostic(id: id, title: title, description: description, severity: severity, location: symbol?.Locations.FirstOrDefault());
	}

	/// <summary>
	/// <para>
	/// Shorthand extension method to report a diagnostic, with less boilerplate code.
	/// </para>
	/// <para>
	/// This overload is only available when the compilation is available at generation time.
	/// </para>
	/// </summary>
	private static void ReportDiagnostic(this SourceProductionContext context, string id, string title, string description, DiagnosticSeverity severity, Location? location)
	{
		context.ReportDiagnostic(Diagnostic.Create(
			new DiagnosticDescriptor(id: id, title: title, messageFormat: description, category: "Design", defaultSeverity: severity, isEnabledByDefault: true),
			location));
	}

	/// <summary>
	/// <para>
	/// Shorthand extension method to report a diagnostic, with less boilerplate code.
	/// </para>
	/// <para>
	/// This overload makes use of the properly cacheable <see cref="SimpleLocation"/>, because <see cref="Location"/> should not be passed between source generation steps.
	/// </para>
	/// </summary>
	public static void ReportDiagnostic(this SourceProductionContext context, string id, string title, string description, DiagnosticSeverity severity, SimpleLocation? location)
	{
		context.ReportDiagnostic(Diagnostic.Create(
			new DiagnosticDescriptor(id: id, title: title, messageFormat: description, category: "Design", defaultSeverity: severity, isEnabledByDefault: true),
			location));
	}
}
