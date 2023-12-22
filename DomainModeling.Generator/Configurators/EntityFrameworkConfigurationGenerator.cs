using System.Collections.Immutable;
using Architect.DomainModeling.Generator.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Architect.DomainModeling.Generator.Configurators;

[Generator]
public partial class EntityFrameworkConfigurationGenerator : SourceGenerator
{
	/// <summary>
	/// A value provider that returns a single boolean value indicating whether EF's ConfigureConventions() is being called.
	/// </summary>
	internal static IncrementalValueProvider<bool> CreateHasConfigureConventionsValueProvider(IncrementalGeneratorInitializationContext context)
	{
		var result = context.SyntaxProvider.CreateSyntaxProvider(FilterSyntaxNode, IsConfigureConventions)
			.Collect()
			.Select((bools, _) => bools.Any());

		return result;
	}

	internal static IncrementalValueProvider<(bool HasConfigureConventions, string AssemblyName)> CreateMetadataProvider(IncrementalGeneratorInitializationContext context)
	{
		var hasConfigureConventionsProvider = CreateHasConfigureConventionsValueProvider(context);
		var assemblyNameProvider = context.CompilationProvider.Select((compilation, _) => compilation.AssemblyName ?? compilation.Assembly.Name);
		var result = hasConfigureConventionsProvider.Combine(assemblyNameProvider);
		return result;
	}

	public override void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var assemblyNameProvider = context.CompilationProvider.Select((compilation, _) => compilation.AssemblyName ?? compilation.Assembly.Name);

		var hasConfigureConventionsProvider = CreateHasConfigureConventionsValueProvider(context);

		var assembliesContainingDomainModelConfiguratorsProvider = context.CompilationProvider
			.Combine(hasConfigureConventionsProvider)
			.Select(GetAssembliesContainingDomainModelConfigurators);

		context.RegisterSourceOutput(assembliesContainingDomainModelConfiguratorsProvider.Combine(assemblyNameProvider), GenerateSource);
	}

	private static bool FilterSyntaxNode(SyntaxNode node, CancellationToken cancellationToken = default)
	{
		// Detect EF's presence by the ConfigureConventions() method, which is needed to use our extensions anyway
		if (node is MethodDeclarationSyntax mds && mds.Identifier.ValueText == "ConfigureConventions")
			return true;

		return false;
	}

	private static bool IsConfigureConventions(GeneratorSyntaxContext context, CancellationToken _)
	{
		if (context.Node is MethodDeclarationSyntax mds &&
			context.SemanticModel.GetDeclaredSymbol(mds) is IMethodSymbol methodSymbol &&
			methodSymbol.Name == "ConfigureConventions" &&
			methodSymbol.IsOverride &&
			methodSymbol.Parameters.Length == 1 &&
			methodSymbol.Parameters[0].Type.IsType("ModelConfigurationBuilder", "Microsoft.EntityFrameworkCore"))
			return true;

		return false;
	}

	private static Generatable GetAssembliesContainingDomainModelConfigurators((Compilation Compilation, bool HasConfigureConventions) input, CancellationToken _)
	{
		if (!input.HasConfigureConventions)
			return new Generatable();

		var ownAssemblyNamePrefix = input.Compilation.Assembly.Name;
		ownAssemblyNamePrefix = ownAssemblyNamePrefix.Substring(0, ownAssemblyNamePrefix.IndexOf('.') is int dotIndex and > 0 ? dotIndex : ownAssemblyNamePrefix.Length);

		var assembliesContainingIdentityConfigurator = new HashSet<string>() { input.Compilation.Assembly.Name };
		var assembliesContainingWrapperValueObjectConfigurator = new HashSet<string>() { input.Compilation.Assembly.Name };
		var assembliesContainingEntityConfigurator = new HashSet<string>() { input.Compilation.Assembly.Name };
		var assembliesContainingDomainEventConfigurator = new HashSet<string>() { input.Compilation.Assembly.Name };

		// Only consider referenced assemblies as long as they have the same top-level assembly name as the current one
		foreach (var assembly in input.Compilation.Assembly.EnumerateAssembliesRecursively(assembly => assembly.Name.StartsWith(ownAssemblyNamePrefix, StringComparison.Ordinal)))
		{
			// Although technically the type name is not a definitive indication, the simplicity of this check saves us a lot of work
			// Also, the type names are relatively specific
			foreach (var typeName in assembly.TypeNames)
			{
				switch (typeName)
				{
					case "IdentityDomainModelConfigurator":
						assembliesContainingIdentityConfigurator.Add(assembly.Name);
						break;
					case "WrapperValueObjectDomainModelConfigurator":
						assembliesContainingWrapperValueObjectConfigurator.Add(assembly.Name);
						break;
					case "EntityDomainModelConfigurator":
						assembliesContainingEntityConfigurator.Add(assembly.Name);
						break;
					case "DomainEventDomainModelConfigurator":
						assembliesContainingDomainEventConfigurator.Add(assembly.Name);
						break;
				}
			}
		}

		var result = new Generatable()
		{
			UsesEntityFrameworkConventions = true,
			ReferencedAssembliesWithIdentityConfigurator = assembliesContainingIdentityConfigurator.OrderBy(name => name).ToImmutableArray(),
			ReferencedAssembliesWithWrapperValueObjectConfigurator = assembliesContainingWrapperValueObjectConfigurator.OrderBy(name => name).ToImmutableArray(),
			ReferencedAssembliesWithEntityConfigurator = assembliesContainingEntityConfigurator.OrderBy(name => name).ToImmutableArray(),
			ReferencedAssembliesWithDomainEventConfigurator = assembliesContainingEntityConfigurator.OrderBy(name => name).ToImmutableArray(),
		};
		return result;
	}

	private static void GenerateSource(SourceProductionContext context, (Generatable Generatable, string AssemblyName) input)
	{
		context.CancellationToken.ThrowIfCancellationRequested();

		if (!input.Generatable.UsesEntityFrameworkConventions)
			return;

		var ownAssemblyName = input.AssemblyName;

		var identityConfigurationCalls = String.Join(
			$"{Environment.NewLine}\t\t\t",
			input.Generatable.ReferencedAssembliesWithIdentityConfigurator!.Value.Select(assemblyName => $"{assemblyName}.IdentityDomainModelConfigurator.ConfigureIdentities(concreteConfigurator);"));
		var wrapperValueObjectConfigurationCalls = String.Join(
			$"{Environment.NewLine}\t\t\t",
			input.Generatable.ReferencedAssembliesWithWrapperValueObjectConfigurator!.Value.Select(assemblyName => $"{assemblyName}.WrapperValueObjectDomainModelConfigurator.ConfigureWrapperValueObjects(concreteConfigurator);"));
		var entityConfigurationCalls = String.Join(
			$"{Environment.NewLine}\t\t\t",
			input.Generatable.ReferencedAssembliesWithEntityConfigurator!.Value.Select(assemblyName => $"{assemblyName}.EntityDomainModelConfigurator.ConfigureEntities(concreteConfigurator);"));
		var domainEventConfigurationCalls = String.Join(
			$"{Environment.NewLine}\t\t\t",
			input.Generatable.ReferencedAssembliesWithDomainEventConfigurator!.Value.Select(assemblyName => $"{assemblyName}.DomainEventDomainModelConfigurator.ConfigureDomainEvents(concreteConfigurator);"));

		var source = $@"
#if NET7_0_OR_GREATER

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using {Constants.DomainModelingNamespace};
using {Constants.DomainModelingNamespace}.Conversions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable enable

namespace {ownAssemblyName}
{{
	public static class EntityFrameworkDomainModelConfigurationExtensions
	{{
		/// <summary>
		/// Allows conventions to be configured for domain objects.
		/// Use the extension methods on the delegate parameter to enable specific conventions.
		/// </summary>
		public static ModelConfigurationBuilder ConfigureDomainModelConventions(this ModelConfigurationBuilder configurationBuilder, Action<IDomainModelConfigurator> domainModel)
		{{
			domainModel(new DomainModelConfigurator(configurationBuilder));
			return configurationBuilder;
		}}

		/// <summary>
		/// <para>
		/// Configures conventions for all marked <see cref=""IIdentity{{T}}""/> types.
		/// </para>
		/// <para>
		/// This configures conversions to and from the underlying type for properties of the identity types.
		/// It similarly configures the default type mapping for those types, which is used when queries encounter a type outside the context of a property, such as in CAST(), SUM(), AVG(), etc.
		/// </para>
		/// <para>
		/// Additionally, <see cref=""Decimal""/>-backed identities receive a mapping hint to use precision 28 and scale 0, a useful default for DistributedIds.
		/// </para>
		/// </summary>
		public static IDomainModelConfigurator ConfigureIdentityConventions(this IDomainModelConfigurator configurator)
		{{
			var concreteConfigurator = new EntityFrameworkIdentityConfigurator(configurator.ConfigurationBuilder);

			{identityConfigurationCalls}

			return configurator;
		}}

		/// <summary>
		/// <para>
		/// Configures conventions for all marked <see cref=""IWrapperValueObject{{TValue}}""/> types.
		/// </para>
		/// <para>
		/// This configures conversions to and from the underlying type for properties of the wrapper types.
		/// It similarly configures the default type mapping for those types, which is used when queries encounter a type outside the context of a property, such as in CAST(), SUM(), AVG(), etc.
		/// </para>
		/// </summary>
		public static IDomainModelConfigurator ConfigureWrapperValueObjectConventions(this IDomainModelConfigurator configurator)
		{{
			var concreteConfigurator = new EntityFrameworkWrapperValueObjectConfigurator(configurator.ConfigurationBuilder);

			{wrapperValueObjectConfigurationCalls}

			return configurator;
		}}

		/// <summary>
		/// <para>
		/// Configures conventions for all marked <see cref=""IEntity""/> types.
		/// </para>
		/// <para>
		/// This configures instantiation without the use of constructors.
		/// </para>
		/// </summary>
		public static IDomainModelConfigurator ConfigureEntityConventions(this IDomainModelConfigurator configurator)
		{{
			EntityFrameworkEntityConfigurator concreteConfigurator = null!;
			concreteConfigurator = new EntityFrameworkEntityConfigurator(() =>
			{{
			{entityConfigurationCalls}
			}});

			configurator.ConfigurationBuilder.Conventions.Add(_ => concreteConfigurator);

			return configurator;
		}}

		/// <summary>
		/// <para>
		/// Configures conventions for all marked domain event types.
		/// </para>
		/// <para>
		/// This configures instantiation without the use of constructors.
		/// </para>
		/// </summary>
		public static IDomainModelConfigurator ConfigureDomainEventConventions(this IDomainModelConfigurator configurator)
		{{
			EntityFrameworkEntityConfigurator concreteConfigurator = null!;
			concreteConfigurator = new EntityFrameworkEntityConfigurator(() =>
			{{
			{domainEventConfigurationCalls}
			}});

			configurator.ConfigurationBuilder.Conventions.Add(_ => concreteConfigurator);

			return configurator;
		}}
	}}

	public interface IDomainModelConfigurator
	{{
		ModelConfigurationBuilder ConfigurationBuilder {{ get; }}
	}}

	file sealed record class DomainModelConfigurator(
		ModelConfigurationBuilder ConfigurationBuilder)
		: IDomainModelConfigurator;

	file sealed record class EntityFrameworkIdentityConfigurator(ModelConfigurationBuilder ConfigurationBuilder)
		: {Constants.DomainModelingNamespace}.Configuration.IIdentityConfigurator
	{{
		public void ConfigureIdentity<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] TIdentity, TUnderlying>(
			in {Constants.DomainModelingNamespace}.Configuration.IIdentityConfigurator.Args _)
			where TIdentity : IIdentity<TUnderlying>, ISerializableDomainObject<TIdentity, TUnderlying>
			where TUnderlying : notnull, IEquatable<TUnderlying>, IComparable<TUnderlying>
		{{
			// Configure properties of the type
			this.ConfigurationBuilder.Properties<TIdentity>()
				.HaveConversion<IdentityValueObjectConverter<TIdentity, TUnderlying>>();

			// Configure non-property occurrences of the type, such as in CAST(), SUM(), AVG(), etc.
			this.ConfigurationBuilder.DefaultTypeMapping<TIdentity>()
				.HasConversion<IdentityValueObjectConverter<TIdentity, TUnderlying>>();

			// The converter's mapping hints are currently ignored by DefaultTypeMapping<T>, which is probably a bug: https://github.com/dotnet/efcore/issues/32533
			if (typeof(TUnderlying) == typeof(decimal))
				this.ConfigurationBuilder.DefaultTypeMapping<TIdentity>()
					.HasPrecision(28, 0);
		}}

		private sealed class IdentityValueObjectConverter<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] TModel, TProvider>
			: ValueConverter<TModel, TProvider>
			where TModel : ISerializableDomainObject<TModel, TProvider>
		{{
			public IdentityValueObjectConverter()
				: base(
					DomainObjectSerializer.CreateSerializeExpression<TModel, TProvider>(),
					DomainObjectSerializer.CreateDeserializeExpression<TModel, TProvider>(),
					new ConverterMappingHints(precision: 28, scale: 0)) // For decimal IDs
			{{
			}}
		}}
	}}

	file sealed record class EntityFrameworkWrapperValueObjectConfigurator(
		ModelConfigurationBuilder ConfigurationBuilder)
		: {Constants.DomainModelingNamespace}.Configuration.IWrapperValueObjectConfigurator
	{{
		public void ConfigureWrapperValueObject<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] TWrapper, TValue>(
			in {Constants.DomainModelingNamespace}.Configuration.IWrapperValueObjectConfigurator.Args _)
			where TWrapper : IWrapperValueObject<TValue>, ISerializableDomainObject<TWrapper, TValue>
			where TValue : notnull
		{{
			// Configure properties of the type
			this.ConfigurationBuilder.Properties<TWrapper>()
				.HaveConversion<WrapperValueObjectConverter<TWrapper, TValue>>();

			// Configure non-property occurrences of the type, such as in CAST(), SUM(), AVG(), etc.
			this.ConfigurationBuilder.DefaultTypeMapping<TWrapper>()
				.HasConversion<WrapperValueObjectConverter<TWrapper, TValue>>();
		}}

		private sealed class WrapperValueObjectConverter<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] TModel, TProvider>
			: ValueConverter<TModel, TProvider>
			where TModel : ISerializableDomainObject<TModel, TProvider>
		{{
			public WrapperValueObjectConverter()
				: base(
					DomainObjectSerializer.CreateSerializeExpression<TModel, TProvider>(),
					DomainObjectSerializer.CreateDeserializeExpression<TModel, TProvider>())
			{{
			}}
		}}
	}}

	file sealed record class EntityFrameworkEntityConfigurator(
		Action InvokeConfigurationCallbacks)
		: {Constants.DomainModelingNamespace}.Configuration.IEntityConfigurator, {Constants.DomainModelingNamespace}.Configuration.IDomainEventConfigurator, IEntityTypeAddedConvention, IModelFinalizingConvention
	{{
		private Dictionary<Type, IConventionEntityType> EntityTypeConventionsByType {{ get; }} = new Dictionary<Type, IConventionEntityType>();

		public void ProcessEntityTypeAdded(IConventionEntityTypeBuilder entityTypeBuilder, IConventionContext<IConventionEntityTypeBuilder> context)
		{{
			var type = entityTypeBuilder.Metadata.ClrType;
			if (!type.IsAbstract && !type.IsInterface && !type.IsGenericTypeDefinition)
				this.EntityTypeConventionsByType[entityTypeBuilder.Metadata.ClrType] = entityTypeBuilder.Metadata;
		}}

		public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
		{{
			this.InvokeConfigurationCallbacks();
		}}

		public void ConfigureEntity<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] TEntity>(
			in {Constants.DomainModelingNamespace}.Configuration.IEntityConfigurator.Args args)
			where TEntity : IEntity
		{{
			if (!this.EntityTypeConventionsByType.TryGetValue(typeof(TEntity), out var entityTypeConvention))
				return;

#pragma warning disable EF1001 // Internal EF Core API usage -- No public APIs are available for this yet, and interceptors do not work because EF demands a usable ctor even the interceptor would prevent ctor usage
			var entityType = entityTypeConvention as EntityType ?? throw new NotImplementedException($""{{entityTypeConvention.GetType().Name}} was received when {{nameof(EntityType)}} was expected. Either a non-entity was passed or internal changes to Entity Framework have broken this code."");
			entityType.ConstructorBinding = new UninitializedInstantiationBinding(typeof(TEntity), DomainObjectSerializer.CreateDeserializeExpression(typeof(TEntity)));
#pragma warning restore EF1001 // Internal EF Core API usage
		}}

		public void ConfigureDomainEvent<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] TDomainEvent>(
			in {Constants.DomainModelingNamespace}.Configuration.IDomainEventConfigurator.Args args)
			where TDomainEvent : IDomainObject
		{{
			if (!this.EntityTypeConventionsByType.TryGetValue(typeof(TDomainEvent), out var entityTypeConvention))
				return;

#pragma warning disable EF1001 // Internal EF Core API usage -- No public APIs are available for this yet, and interceptors do not work because EF demands a usable ctor even the interceptor would prevent ctor usage
			var entityType = entityTypeConvention as EntityType ?? throw new NotImplementedException($""{{entityTypeConvention.GetType().Name}} was received when {{nameof(EntityType)}} was expected. Either a non-entity was passed or internal changes to Entity Framework have broken this code."");
			entityType.ConstructorBinding = new UninitializedInstantiationBinding(typeof(TDomainEvent), DomainObjectSerializer.CreateDeserializeExpression(typeof(TDomainEvent)));
#pragma warning restore EF1001 // Internal EF Core API usage
		}}

		private sealed class UninitializedInstantiationBinding
			: InstantiationBinding
		{{
			private static readonly MethodInfo GetUninitializedObjectMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.GetUninitializedObject))!;

			public override Type RuntimeType {{ get; }}
			private Expression? Expression {{ get; }}

			public UninitializedInstantiationBinding(
				[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type runtimeType,
				Expression? expression = null)
				: base(Array.Empty<ParameterBinding>())
			{{
				this.RuntimeType = runtimeType;
				this.Expression = expression;
			}}

			public override Expression CreateConstructorExpression(ParameterBindingInfo bindingInfo)
			{{
				return this.Expression ??
					Expression.Convert(
						Expression.Call(method: GetUninitializedObjectMethod, arguments: Expression.Constant(this.RuntimeType)),
						this.RuntimeType);
			}}

			public override InstantiationBinding With(IReadOnlyList<ParameterBinding> parameterBindings)
			{{
				return this;
			}}
		}}
	}}
}}

#endif
";

		AddSource(context, source, "EntityFrameworkDomainModelConfigurationExtensions", $"{Constants.DomainModelingNamespace}.EntityFramework");
	}

	internal sealed record Generatable : IGeneratable
	{
		public bool UsesEntityFrameworkConventions { get; set; }
		/// <summary>
		/// The referenced assemblies that contain a specific, generated type.
		/// Does not include the target assembly, since types are currently being generated for that.
		/// </summary>
		public StructuralList<ImmutableArray<string>, string>? ReferencedAssembliesWithIdentityConfigurator { get; set; }
		/// <summary>
		/// The referenced assemblies that contain a specific, generated type.
		/// Does not include the target assembly, since types are currently being generated for that.
		/// </summary>
		public StructuralList<ImmutableArray<string>, string>? ReferencedAssembliesWithWrapperValueObjectConfigurator { get; set; }
		/// <summary>
		/// The referenced assemblies that contain a specific, generated type.
		/// Does not include the target assembly, since types are currently being generated for that.
		/// </summary>
		public StructuralList<ImmutableArray<string>, string>? ReferencedAssembliesWithEntityConfigurator { get; set; }
		/// <summary>
		/// The referenced assemblies that contain a specific, generated type.
		/// Does not include the target assembly, since types are currently being generated for that.
		/// </summary>
		public StructuralList<ImmutableArray<string>, string>? ReferencedAssembliesWithDomainEventConfigurator { get; set; }
	}
}
