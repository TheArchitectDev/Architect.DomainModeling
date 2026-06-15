using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Architect.DomainModeling.Analyzer.Analyzers;

/// <summary>
/// Prevents assignment of unvalidated enum values to members of an IDomainObject.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class UnvalidatedEnumMemberAssignmentAnalyzer : DiagnosticAnalyzer
{
	[System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "False positive.")]
	[System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008:Enable analyzer release tracking", Justification = "Not yet implemented.")]
	private static readonly DiagnosticDescriptor DiagnosticDescriptor = new DiagnosticDescriptor(
		id: "UnvalidatedEnumAssignmentToDomainobject",
		title: "Unvalidated enum assignment to domain object member",
		messageFormat: "The assigned value was not validated. Use the AsDefined(), AsDefinedFlags(), or AsUnvalidated() extension methods to specify the intent.",
		category: "Usage",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [DiagnosticDescriptor];

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.ReportDiagnostics);
		context.EnableConcurrentExecution();

		context.RegisterOperationAction(AnalyzeAssignment,
			OperationKind.SimpleAssignment,
			OperationKind.CoalesceAssignment);
	}

	private static void AnalyzeAssignment(OperationAnalysisContext context)
	{
		var assignment = (IAssignmentOperation)context.Operation;

		if (assignment.Target is not IMemberReferenceOperation memberRef)
			return;

		if (assignment.Value.Type is not { } assignedValueType)
			return;

		// Dig through nullable
		if (assignedValueType.IsNullable(out var nullableUnderlyingType))
			assignedValueType = nullableUnderlyingType;

		var memberType = memberRef.Type.IsNullable(out var memberNullableUnderlyingType) ? memberNullableUnderlyingType : memberRef.Type;
		if (memberType is not { TypeKind: TypeKind.Enum } enumType)
			return;

		// Only if target member is a member of some IDomainObject
		if (!memberRef.Member.ContainingType.AllInterfaces.Any(interf =>
			interf is { Name: "IDomainObject", ContainingNamespace: { Name: "DomainModeling", ContainingNamespace: { Name: "Architect", ContainingNamespace.IsGlobalNamespace: true } } }))
			return;

		// Flag each possible assigned value that is not either validated through one of the extension methods or an acceptable constant
		var locations = EnumerateUnvalidatedValues(assignment.Value, memberRef, enumType)
			.Select(operation => operation.Syntax.GetLocation());

		foreach (var location in locations)
		{
			var diagnostic = Diagnostic.Create(
				DiagnosticDescriptor,
				location);

			context.ReportDiagnostic(diagnostic);
		}
	}

	private static IEnumerable<IOperation> EnumerateUnvalidatedValues(IOperation operation, IMemberReferenceOperation member, ITypeSymbol enumType)
	{
		// Dig through up to two conversions
		var operationWithoutConversion = operation switch
		{
			IConversionOperation { Operand: IConversionOperation conversion } => conversion.Operand,
			IConversionOperation conversion => conversion.Operand,
			_ => operation,
		};

		// Recurse into the arms of ternaries and switch expressions
		if (operationWithoutConversion is IConditionalOperation conditional)
		{
			foreach (var result in EnumerateUnvalidatedValues(conditional.WhenTrue, member, enumType))
				yield return result;
			foreach (var result in conditional.WhenFalse is null ? [] : EnumerateUnvalidatedValues(conditional.WhenFalse, member, enumType))
				yield return result;
			yield break;
		}
		if (operationWithoutConversion is ISwitchExpressionOperation switchExpression)
		{
			foreach (var arm in switchExpression.Arms)
				foreach (var result in EnumerateUnvalidatedValues(arm.Value, member, enumType))
					yield return result;
			yield break;
		}

		// Ignore throw expressions
		if (operationWithoutConversion is IThrowOperation)
			yield break;

		// Ignore if validated by AsDefined() or the like
		if (IsValidatedWithExtensionMethod(operationWithoutConversion))
			yield break;

		var constantValue = operation.ConstantValue;

		// Dig through up to two conversions
		if (operation is IConversionOperation conversionOperation)
		{
			if (!constantValue.HasValue && conversionOperation.Operand.ConstantValue.HasValue)
				constantValue = conversionOperation.Operand.ConstantValue.Value;

			if (conversionOperation.Operand is IConversionOperation nestedConversionOperation)
			{
				if (!constantValue.HasValue && nestedConversionOperation.Operand.ConstantValue.HasValue)
					constantValue = nestedConversionOperation.Operand.ConstantValue.Value;
			}
		}

		// Ignore if assigning null or a defined constant
		if (constantValue.HasValue && (constantValue.Value is null || IsDefinedEnumConstantOrNullableThereof(enumType, constantValue.Value)))
			yield break;

		// Ignore if assigning default(T?) (i.e. null) or default (i.e. null) to a nullable member
		// Note: We need to use the "operation" var directly to correctly evaluate the conversions
		if (operation is IDefaultValueOperation or IConversionOperation { Operand: IDefaultValueOperation { ConstantValue.HasValue: false } } && member.Type.IsNullable(out _))
			yield break;

		yield return operation;
	}

	private static bool IsValidatedWithExtensionMethod(IOperation operation)
	{
		if (operation is not IInvocationOperation invocation)
			return false;

		var method = invocation.TargetMethod;
		method = method.ReducedFrom ?? method; // value.AsDefined() vs. EnumExtensions.AsDefined()

		if (method.Name is not "AsDefined" and not "AsDefinedFlags" and not "AsUnvalidated")
			return false;

		if (method.ContainingType is not { Name: "EnumExtensions", ContainingNamespace: { Name: "DomainModeling", ContainingNamespace: { Name: "Architect", ContainingNamespace.IsGlobalNamespace: true } } })
			return false;

		return true;
	}

	private static bool IsDefinedEnumConstantOrNullableThereof(ITypeSymbol enumType, object constantValue)
	{
		if (enumType is not INamedTypeSymbol { EnumUnderlyingType: { } } namedEnumType)
			return false;

		var binaryValue = GetBinaryValue(namedEnumType.EnumUnderlyingType, constantValue);

		var valueIsDefined = namedEnumType.GetMembers().Any(member =>
			member is IFieldSymbol { ConstantValue: { } value } && GetBinaryValue(namedEnumType.EnumUnderlyingType, value) == binaryValue);

		return valueIsDefined;
	}

	private static ulong? GetBinaryValue(ITypeSymbol enumUnderlyingType, object value)
	{
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
