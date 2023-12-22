using Architect.DomainModeling.Generator.Common;
using Architect.DomainModeling.Generator.Configurators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Architect.DomainModeling.Generator;

[Generator]
public class EntityGenerator : SourceGenerator
{
	public override void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var provider = context.SyntaxProvider.CreateSyntaxProvider(FilterSyntaxNode, TransformSyntaxNode)
			.Where(generatable => generatable is not null)
			.DeduplicatePartials();

		context.RegisterSourceOutput(provider, GenerateSource!);

		var aggregatedProvider = provider
			.Collect()
			.Combine(EntityFrameworkConfigurationGenerator.CreateMetadataProvider(context));

		context.RegisterSourceOutput(aggregatedProvider, DomainModelConfiguratorGenerator.GenerateSourceForEntities!);
	}

	private static bool FilterSyntaxNode(SyntaxNode node, CancellationToken cancellationToken = default)
	{
		// Class or record class
		if (node is TypeDeclarationSyntax tds && tds is ClassDeclarationSyntax or RecordDeclarationSyntax { ClassOrStructKeyword.ValueText: "class" })
		{
			// With relevant attribute
			if (tds.HasAttributeWithPrefix("Entity"))
				return true;
		}

		return false;
	}

	private static Generatable? TransformSyntaxNode(GeneratorSyntaxContext context, CancellationToken cancellationToken = default)
	{
		var model = context.SemanticModel;
		var tds = (TypeDeclarationSyntax)context.Node;
		var type = model.GetDeclaredSymbol(tds);

		if (type is null)
			return null;

		// Only with the attribute
		if (type.GetAttribute("EntityAttribute", Constants.DomainModelingNamespace, arity: 0) is null)
			return null;

		// Only concrete
		if (type.IsAbstract)
			return null;

		// Only non-generic
		if (type.IsGenericType)
			return null;

		// Only non-nested
		if (type.IsNested())
			return null;

		var result = new Generatable()
		{
			TypeLocation = type.Locations.FirstOrDefault(),
			IsEntity = type.IsOrImplementsInterface(type => type.IsType(Constants.EntityInterfaceName, Constants.DomainModelingNamespace, arity: 0), out _),
			TypeName = type.Name, // Non-generic by filter
			ContainingNamespace = type.ContainingNamespace.ToString(),
		};

		var existingComponents = EntityTypeComponents.None;

		existingComponents |= EntityTypeComponents.DefaultConstructor.If(type.Constructors.Any(ctor =>
			!ctor.IsStatic && ctor.Parameters.Length == 0 /*&& ctor.DeclaringSyntaxReferences.Length > 0*/));

		result.ExistingComponents = existingComponents;

		return result;
	}

	private static void GenerateSource(SourceProductionContext context, Generatable generatable)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		// Require the expected inheritance
		if (!generatable.IsEntity)
		{
			context.ReportDiagnostic("EntityGeneratorUnexpectedInheritance", "Unexpected inheritance",
				"Type marked as entity lacks IEntity interface.", DiagnosticSeverity.Warning, generatable.TypeLocation);
			return;
		}
	}

	[Flags]
	internal enum EntityTypeComponents : ulong
	{
		None = 0,

		DefaultConstructor = 1 << 1,
	}

	internal sealed record Generatable : IGeneratable
	{
		public bool IsEntity { get; set; }
		public string TypeName { get; set; } = null!;
		public string ContainingNamespace { get; set; } = null!;
		public EntityTypeComponents ExistingComponents { get; set; }
		public SimpleLocation? TypeLocation { get; set; }
	}
}
