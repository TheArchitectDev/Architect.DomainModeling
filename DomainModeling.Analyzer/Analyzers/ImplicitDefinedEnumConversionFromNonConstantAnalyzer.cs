using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Architect.DomainModeling.Analyzer.Analyzers;

/// <summary>
/// Prevents attempts to implicilty convert from a non-constant TEnum value to DefinedEnum&lt;TEnum&gt; or DefinedEnum&lt;TEnum, TPrimitive&gt;.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ImplicitDefinedEnumConversionFromNonConstantAnalyzer : DiagnosticAnalyzer
{
	[System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "False positive.")]
	[System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008:Enable analyzer release tracking", Justification = "Not yet implemented.")]
	private static readonly DiagnosticDescriptor DiagnosticDescriptor = new DiagnosticDescriptor(
		id: "ImplicitConversionFromUnvalidatedEnumToDefinedEnum",
		title: "Implicit conversion from unvalidated enum to DefinedEnum",
		messageFormat: "Only a defined enum constant may be implicitly converted to DefinedEnum. For non-constant values, use DefinedEnum.Create(), a constructor, or an explicit conversion.",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Error,
		isEnabledByDefault: true);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [DiagnosticDescriptor];

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.ReportDiagnostics);
		context.EnableConcurrentExecution();

		context.RegisterOperationAction(AnalyzeConversion, OperationKind.Conversion);
	}

	private static void AnalyzeConversion(OperationAnalysisContext context)
	{
		var conversion = (IConversionOperation)context.Operation;

		// Only implicit conversions are relevant to us
		if (!conversion.IsImplicit)
			return;

		var from = conversion.Operand.Type;
		var to = conversion.Type;

		// Dig through nullables
		if (from.IsNullable(out var nullableUnderlyingType))
			from = nullableUnderlyingType;
		if (to.IsNullable(out nullableUnderlyingType))
			to = nullableUnderlyingType;

		// Only from enum is relevant to us
		if (from is not { TypeKind: TypeKind.Enum } enumType)
			return;

		// Only to DefinedEnum is relevant to us
		if (to is not
			INamedTypeSymbol
			{
				IsGenericType: true, Arity: 1 or 2, Name: "DefinedEnum",
				ContainingNamespace: { Name: "DomainModeling", ContainingNamespace: { Name: "Architect", ContainingNamespace.IsGlobalNamespace: true } }
			})
			return;

		// Produce an error if the implicit conversion is not coming from a defined constant value of the enum's type
		if (!IsDefinedEnumConstant(enumType, conversion.Operand.ConstantValue))
		{
			var diagnostic = Diagnostic.Create(
				DiagnosticDescriptor,
				conversion.Syntax.GetLocation());

			context.ReportDiagnostic(diagnostic);
		}
	}

	private static bool IsDefinedEnumConstant(ITypeSymbol enumType, Optional<object?> constantValue)
	{
		if (!constantValue.HasValue)
			return false;

		if (enumType is not INamedTypeSymbol { EnumUnderlyingType: { } } namedEnumType)
			return false;

		var binaryValue = GetBinaryValue(namedEnumType.EnumUnderlyingType, constantValue.Value);

		var valueIsDefined = namedEnumType.GetMembers().Any(member =>
			member is IFieldSymbol { ConstantValue: var value } && GetBinaryValue(namedEnumType.EnumUnderlyingType, value) == binaryValue);

		return valueIsDefined;
	}

	private static ulong? GetBinaryValue(ITypeSymbol enumUnderlyingType, object? value)
	{
		if (value is null)
			return null;

		return (enumUnderlyingType.SpecialType, Type.GetTypeCode(value.GetType())) switch
		{
			(SpecialType.System_Byte, TypeCode.Byte) => Convert.ToByte(value),
			(SpecialType.System_SByte, TypeCode.SByte) => (ulong)Convert.ToSByte(value),
			(SpecialType.System_UInt16, TypeCode.UInt16) => Convert.ToUInt16(value),
			(SpecialType.System_Int16, TypeCode.Int16) => (ulong)Convert.ToInt16(value),
			(SpecialType.System_UInt32, TypeCode.UInt32) => Convert.ToUInt32(value),
			(SpecialType.System_Int32, TypeCode.Int32) => (ulong)Convert.ToInt32(value),
			(SpecialType.System_UInt64, TypeCode.UInt64) => Convert.ToUInt64(value),
			(SpecialType.System_Int64, TypeCode.Int64) => (ulong)Convert.ToInt64(value),
			_ => null,
		};
	}
}
