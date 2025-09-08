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
			methodSymbol.Parameters[0].Type.IsType("ModelConfigurationBuilder", "Microsoft", "EntityFrameworkCore"))
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
			ReferencedAssembliesWithDomainEventConfigurator = assembliesContainingDomainEventConfigurator.OrderBy(name => name).ToImmutableArray(),
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
			$"{Environment.NewLine}\t\t\t\t",
			input.Generatable.ReferencedAssembliesWithIdentityConfigurator!.Value.Select(assemblyName => $"{assemblyName}.IdentityDomainModelConfigurator.ConfigureIdentities(concreteConfigurator);"));
		var wrapperValueObjectConfigurationCalls = String.Join(
			$"{Environment.NewLine}\t\t\t\t",
			input.Generatable.ReferencedAssembliesWithWrapperValueObjectConfigurator!.Value.Select(assemblyName => $"{assemblyName}.WrapperValueObjectDomainModelConfigurator.ConfigureWrapperValueObjects(concreteConfigurator);"));
		var entityConfigurationCalls = String.Join(
			$"{Environment.NewLine}\t\t\t",
			input.Generatable.ReferencedAssembliesWithEntityConfigurator!.Value.Select(assemblyName => $"{assemblyName}.EntityDomainModelConfigurator.ConfigureEntities(concreteConfigurator);"));
		var domainEventConfigurationCalls = String.Join(
			$"{Environment.NewLine}\t\t\t",
			input.Generatable.ReferencedAssembliesWithDomainEventConfigurator!.Value.Select(assemblyName => $"{assemblyName}.DomainEventDomainModelConfigurator.ConfigureDomainEvents(concreteConfigurator);"));

		var source = $@"
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Architect.DomainModeling;
using Architect.DomainModeling.Configuration;
using Architect.DomainModeling.Conversions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
		/// This configures conversions to and from the core type for properties of identity types.
		/// It similarly configures the default type mapping for those types, which is used when queries encounter a type outside the context of a property, such as in CAST(), SUM(), AVG(), etc.
		/// </para>
		/// <para>
		/// Additionally, <see langword=""string""/>-backed identities receive a <em>provider</em> value comparer matching their own <see cref=""StringComparison""/>s.
		/// This is important because Entity Framework performs all comparisons of <em>keys</em> on the core (provider) values.
		/// This method also warns if the collation (or the provider's default) mismatches the type's <see cref=""StringComparison""/>.
		/// </para>
		/// <para>
		/// Additionally, <see langword=""decimal""/>-backed identities receive a mapping hint to use precision 28 and scale 0, a useful default for DistributedIds.
		/// </para>
		/// </summary>
		/// <param name=""options"">If given, the method also applies any options specified.</param>
		public static IDomainModelConfigurator ConfigureIdentityConventions(this IDomainModelConfigurator configurator, IdentityConfigurationOptions? options = null)
		{{
			// Apply Identity configuration (immediate)
			{{
				var concreteConfigurator = new EntityFrameworkIdentityConfigurator(configurator.ConfigurationBuilder, options);
				// Call configurator for each Identity type
				{identityConfigurationCalls}
			}}

			// Apply common ValueWrapper configuration (deferred)
			{{
				ValueWrapperConfigurator concreteConfigurator = null!;
				configurator.ConfigurationBuilder.Conventions.Add(serviceProvider => concreteConfigurator = new ValueWrapperConfigurator(
					configurator.ConfigurationBuilder,
					serviceProvider.GetRequiredService<IDiagnosticsLogger<DbLoggerCategory.Model.Validation>>(),
					serviceProvider.GetService<IDatabaseProvider>(),
					() =>
					{{
				// Call configurator for each Identity type
				{identityConfigurationCalls}
					}},
					options));
			}}

			return configurator;
		}}

		/// <summary>
		/// <para>
		/// Configures conventions for all marked <see cref=""IWrapperValueObject{{TValue}}""/> types.
		/// </para>
		/// <para>
		/// This configures conversions to and from the core type for properties of the wrapper types.
		/// It similarly configures the default type mapping for those types, which is used when queries encounter a type outside the context of a property, such as in CAST(), SUM(), AVG(), etc.
		/// </para>
		/// <para>
		/// Additionally, <see langword=""string""/>-backed wrappers receive a <em>provider</em> value comparer matching their own <see cref=""StringComparison""/>s.
		/// This is important because Entity Framework performs all comparisons of <em>keys</em> on the core (provider) values.
		/// This method also warns if the collation (or the provider's default) mismatches the type's <see cref=""StringComparison""/>.
		/// </para>
		/// </summary>
		/// <param name=""options"">If given, the method also applies any options specified.</param>
		public static IDomainModelConfigurator ConfigureWrapperValueObjectConventions(this IDomainModelConfigurator configurator, WrapperValueObjectConfigurationOptions? options = null)
		{{
			// Apply WrapperValueObject configuration (immediate)
			{{
				var concreteConfigurator = new EntityFrameworkWrapperValueObjectConfigurator(configurator.ConfigurationBuilder, options);
				// Call configurator for each WrapperValueObject type
				{wrapperValueObjectConfigurationCalls}
			}}

			// Apply common ValueWrapper configuration (deferred)
			{{
				ValueWrapperConfigurator concreteConfigurator = null!;
				configurator.ConfigurationBuilder.Conventions.Add(serviceProvider => concreteConfigurator = new ValueWrapperConfigurator(
					configurator.ConfigurationBuilder,
					serviceProvider.GetRequiredService<IDiagnosticsLogger<DbLoggerCategory.Model.Validation>>(),
					serviceProvider.GetService<IDatabaseProvider>(),
					() =>
					{{
				// Call configurator for each WrapperValueObject type
				{wrapperValueObjectConfigurationCalls}
					}},
					options));
			}}

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
			// Call configurator for each Entity type
			{entityConfigurationCalls}
			}});

			// Apply Entity configuration (deferred)
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
			// Call configurator for each DomainEvent type
			{domainEventConfigurationCalls}
			}});

			// Apply DomainEvent configuration (deferred)
			configurator.ConfigurationBuilder.Conventions.Add(_ => concreteConfigurator);

			return configurator;
		}}

		/// <summary>
		/// <para>
		/// Configures custom conventions on marked <see cref=""IIdentity{{T}}""/> types, via a simple callback per type.
		/// </para>
		/// <para>
		/// For example, configure every identity type wrapping a <see langword=""string""/> to have a max length, fixed length, and collation.
		/// </para>
		/// <para>
		/// To receive generic callbacks instead, create a concrete implementation of <see cref=""IIdentityConfigurator""/>, and use <see cref=""IdentityDomainModelConfigurator.ConfigureIdentities""/> to initiate the callbacks to its generic method.
		/// </para>
		/// </summary>
		public static IDomainModelConfigurator CustomizeIdentityConventions(this IDomainModelConfigurator configurator, Action<CustomizingIdentityConfigurator.Context> callback)
		{{
			var concreteConfigurator = new CustomizingIdentityConfigurator(configurator.ConfigurationBuilder, callback);
			// Call configurator for each Identity type
			{identityConfigurationCalls}
			return configurator;
		}}

		/// <summary>
		/// <para>
		/// Configures custom conventions on marked <see cref=""IWrapperValueObject{{TValue}}""/> types, via a simple callback per type.
		/// </para>
		/// <para>
		/// For example, configure every wrapper type wrapping a <see langword=""decimal""/> to have a certain precision.
		/// </para>
		/// <para>
		/// To receive generic callbacks instead, create a concrete implementation of <see cref=""IWrapperValueObjectConfigurator""/>, and use <see cref=""WrapperValueObjectDomainModelConfigurator.ConfigureWrapperValueObjects""/> to initiate the callbacks to its generic method.
		/// </para>
		/// </summary>
		public static IDomainModelConfigurator CustomizeWrapperValueObjectConventions(this IDomainModelConfigurator configurator, Action<CustomizingWrapperValueObjectConfigurator.Context> callback)
		{{
			var concreteConfigurator = new CustomizingWrapperValueObjectConfigurator(configurator.ConfigurationBuilder, callback);
			// Call configurator for each WrapperValueObject type
			{wrapperValueObjectConfigurationCalls}
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

	file sealed record class ValueWrapperConfigurator(
		ModelConfigurationBuilder ConfigurationBuilder,
		IDiagnosticsLogger<DbLoggerCategory.Model.Validation> DiagnosticLogger,
		IDatabaseProvider? DatabaseProvider,
		Action InvokeConfigurationCallbacks,
		ValueWrapperConfigurationOptions? Options)
		: IIdentityConfigurator, IWrapperValueObjectConfigurator, IModelInitializedConvention, IModelFinalizingConvention
	{{
		private Dictionary<Type, StringComparison> DesiredCaseSensitivityPerType {{ get; }} = [];

		internal static bool IsStringWrapperWithKnownCaseSensitivity<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] TWrapper, TCore>(
			[NotNullWhen(true)] out StringComparison caseSensitivity)
			where TWrapper : ICoreValueWrapper<TWrapper, TCore>
		{{
			caseSensitivity = default;
			if (typeof(TCore) != typeof(string) || GaugeDesiredCaseSensitivity<TWrapper, TCore>() is not {{ }} value)
				return false;
			caseSensitivity = value;
			return true;
		}}

		internal static string? GetApplicableCollationFromOptions(StringComparison caseSensitivity, ValueWrapperConfigurationOptions? options)
		{{
			return caseSensitivity switch
			{{
				StringComparison.Ordinal when options?.CaseSensitiveCollation is {{ }} collation => collation,
				StringComparison.OrdinalIgnoreCase when options?.IgnoreCaseCollation is {{ }} collation => collation,
				_ => null,
			}};
		}}

		public void ProcessModelInitialized(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
		{{
			this.InvokeConfigurationCallbacks();
		}}

		public void ConfigureIdentity<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] TIdentity, TUnderlying, TCore>(
			in IIdentityConfigurator.Args args)
			where TIdentity : IIdentity<TUnderlying>, IDirectValueWrapper<TIdentity, TUnderlying>, ICoreValueWrapper<TIdentity, TCore>
			where TUnderlying : notnull, IEquatable<TUnderlying>, IComparable<TUnderlying>
		{{
			this.ApplyConfiguration<TIdentity, TCore>();
		}}

		public void ConfigureWrapperValueObject<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] TWrapper, TValue, TCore>(
			in IWrapperValueObjectConfigurator.Args args)
			where TWrapper : IWrapperValueObject<TValue>, IDirectValueWrapper<TWrapper, TValue>, ICoreValueWrapper<TWrapper, TCore>
			where TValue : notnull
		{{
			this.ApplyConfiguration<TWrapper, TCore>();
		}}

		private void ApplyConfiguration<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] TWrapper, TCore>()
			where TWrapper : ICoreValueWrapper<TWrapper, TCore>
		{{
			// For string wrappers where we can ascertain the desired case-sensitivity
			if (IsStringWrapperWithKnownCaseSensitivity<TWrapper, TCore>(out var caseSensitivity))
			{{
				// Remember the case-sensitivity to use in model finalizing
				this.DesiredCaseSensitivityPerType[typeof(TWrapper)] = caseSensitivity;

				// Log the collation set by the Identity/WrapperValueObject configurator, which needed to set this before user code, without waiting for access to a logger, so that user code could still override
				if (GetApplicableCollationFromOptions(caseSensitivity, this.Options) is string collation && this.DiagnosticLogger.Logger.IsEnabled(LogLevel.Debug))
					this.DiagnosticLogger.Logger.LogDebug(""Set collation {{TargetCollation}} for {{WrapperType}} properties based on the type's case-sensitivity"", collation, typeof(TWrapper).Name);
			}}
		}}

		public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
		{{
			var providerDefaultCaseSensitivity = GaugeProviderDefaultCaseSensitivity(this.DatabaseProvider?.Name, out var providerFriendlyName);

			foreach (var property in modelBuilder.Metadata.GetEntityTypes().SelectMany(entityBuilder => entityBuilder.GetProperties()))
			{{
				// We only care about values mapped to strings, and only where they are wrapper types that we know the desired case-sensitivity for
				if (property.GetValueConverter()?.ProviderClrType != typeof(string) || !this.DesiredCaseSensitivityPerType.TryGetValue(property.ClrType, out var desiredCaseSensitivity))
					continue;

				// If the database's behavior mismatches the model's behavior, then warn
				var actualCaseSensitivity = GaugeCaseSensitivity(property, providerDefaultCaseSensitivity, providerFriendlyName, out var collationIndicator, out var isDeliberateChoice);
				if (actualCaseSensitivity is not null && actualCaseSensitivity != desiredCaseSensitivity && !isDeliberateChoice && this.DiagnosticLogger.Logger.IsEnabled(LogLevel.Warning))
				{{
					this.DiagnosticLogger.Logger.LogWarning(
						""{{Entity}}.{{Property}} uses {{DesiredCaseSensitivity}} comparisons, but the {{collationIndicator}} database collation acts more like {{ActualCaseSensitivity}} - use the options in ConfigureIdentityConventions() and ConfigureWrapperValueObjectConventions() to specify default collations, or configure property collations manually"",
						property.DeclaringType.Name,
						property.Name,
						desiredCaseSensitivity,
						collationIndicator,
						actualCaseSensitivity);
				}}
			}}

			this.DesiredCaseSensitivityPerType.Clear();
			this.DesiredCaseSensitivityPerType.TrimExcess();
		}}

		private static StringComparison? GaugeDesiredCaseSensitivity<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] TWrapper, TCore>()
			where TWrapper : ICoreValueWrapper<TWrapper, TCore>
		{{
			System.Diagnostics.Debug.Assert(typeof(TCore) == typeof(string), ""This method is intended only for string wrappers."");
			try
			{{
				var comparisonResult = EqualityComparer<TWrapper>.Default.Equals(
					DomainObjectSerializer.Deserialize<TWrapper, TCore>((TCore)(object)""A""),
					DomainObjectSerializer.Deserialize<TWrapper, TCore>((TCore)(object)""a""));
				return comparisonResult
					? StringComparison.OrdinalIgnoreCase
					: StringComparison.Ordinal;
			}}
			catch
			{{
				return null;
			}}
		}}

		/// <summary>
		/// Gauges the case-sensitivity of the given <paramref name=""property""/>'s collation, with fallback to the given <paramref name=""providerDefaultCaseSensitivity""/>.
		/// Returns null if the case-sensitivity cannot be determined.
		/// </summary>
		/// <param name=""isDeliberateChoice"">Deliberate choices may warrant permitting discrepancies, whereas accidental discrepancies are cause for alarm.</param>
		private static StringComparison? GaugeCaseSensitivity(
			IConventionProperty property,
			StringComparison? providerDefaultCaseSensitivity, string providerFriendlyName,
			out string? collationIndicator, out bool isDeliberateChoice)
		{{
			collationIndicator = property.GetCollation();
			isDeliberateChoice = true;
			if (collationIndicator is null)
			{{
				collationIndicator = property.DeclaringType.Model.GetCollation();
				isDeliberateChoice = false;
			}}
			var result = GaugeCaseSensitivity(collationIndicator);
			if (result is null)
			{{
				result = providerDefaultCaseSensitivity;
				collationIndicator = $""default {{providerFriendlyName}}"";
				isDeliberateChoice = false;
			}}
			return result;
		}}

		/// <summary>
		/// Gauges the case-sensitivity of the given <paramref name=""collationName""/>, or null if we cannot determine one.
		/// The implementation is familiar with: [Azure] SQL Server, PostgreSQL, MySQL, SQLite.
		/// </summary>
		private static StringComparison? GaugeCaseSensitivity(string? collationName)
		{{
			var collationNameSpan = collationName.AsSpan();
			return collationNameSpan switch
			{{
				_ when collationNameSpan.Contains(""_BIN"", StringComparison.OrdinalIgnoreCase) => StringComparison.Ordinal, // SQL Server, MySQL
				_ when collationNameSpan.Contains(""_CS"", StringComparison.OrdinalIgnoreCase) => StringComparison.Ordinal, // SQL Server, MySQL
				_ when collationNameSpan.Contains(""BINARY"", StringComparison.OrdinalIgnoreCase) => StringComparison.Ordinal, // SQLite
				_ when collationNameSpan.Contains(""_CI"", StringComparison.OrdinalIgnoreCase) => StringComparison.OrdinalIgnoreCase, // SQL Server, MySQL
				_ when collationNameSpan.Contains(""NOCASE"", StringComparison.OrdinalIgnoreCase) => StringComparison.OrdinalIgnoreCase, // SQLite
				_ when collationNameSpan.Contains(""ks-level1"", StringComparison.OrdinalIgnoreCase) => StringComparison.OrdinalIgnoreCase, // Postgres
				_ when collationNameSpan.Contains(""ks-primary"", StringComparison.OrdinalIgnoreCase) => StringComparison.OrdinalIgnoreCase, // Postgres
				_ => null,
			}};
		}}

		/// <summary>
		/// Gauges the default case-sensitivity of the given <paramref name=""providerName""/>, or null if we cannot determine one.
		/// The implementation is familiar with: [Azure] SQL Server, PostgreSQL, MySQL, SQLite.
		/// </summary>
		private static StringComparison? GaugeProviderDefaultCaseSensitivity(string? providerName, out string providerFriendlyName)
		{{
			var providerNameSpan = providerName.AsSpan();
			var (result, friendlyName) = providerNameSpan switch
			{{
				_ when providerNameSpan.Contains(""SQLServer"", StringComparison.OrdinalIgnoreCase) => (StringComparison.OrdinalIgnoreCase, ""SQL Server""),
				_ when providerNameSpan.Contains(""MySQL"", StringComparison.OrdinalIgnoreCase) => (StringComparison.OrdinalIgnoreCase, ""MySQL""),
				_ when providerNameSpan.Contains(""Postgres"", StringComparison.OrdinalIgnoreCase) => (StringComparison.Ordinal, ""PostgreSQL""),
				_ when providerNameSpan.Contains(""npgsql"", StringComparison.OrdinalIgnoreCase) => (StringComparison.Ordinal, ""PostgreSQL""),
				_ when providerNameSpan.Contains(""SQLite"", StringComparison.OrdinalIgnoreCase) => (StringComparison.Ordinal, ""SQLite""),
				_ => ((StringComparison?)null, ""unknown""),
			}};
			providerFriendlyName = friendlyName;
			return result;
		}}
	}}

	file sealed record class EntityFrameworkIdentityConfigurator(
		ModelConfigurationBuilder ConfigurationBuilder,
		IdentityConfigurationOptions? Options = null)
		: IIdentityConfigurator
	{{
		private static readonly ConverterMappingHints DecimalIdConverterMappingHints = new ConverterMappingHints(precision: 28, scale: 0); // For decimal IDs

		public void ConfigureIdentity<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] TIdentity, TUnderlying, TCore>(
			in IIdentityConfigurator.Args args)
			where TIdentity : IIdentity<TUnderlying>, IDirectValueWrapper<TIdentity, TUnderlying>, ICoreValueWrapper<TIdentity, TCore>
			where TUnderlying : notnull, IEquatable<TUnderlying>, IComparable<TUnderlying>
		{{
			// Configure properties of the type
			this.ConfigurationBuilder.Properties<TIdentity>()
				.HaveConversion<IdentityConverter<TIdentity, TCore>>();

			// Configure non-property occurrences of the type, such as in CAST(), SUM(), AVG(), etc.
			this.ConfigurationBuilder.DefaultTypeMapping<TIdentity>()
				.HasConversion<IdentityConverter<TIdentity, TCore>>();

			// The converter's mapping hints are currently ignored by DefaultTypeMapping<T>, which is probably a bug: https://github.com/dotnet/efcore/issues/32533
			if (typeof(TCore) == typeof(decimal))
				this.ConfigurationBuilder.DefaultTypeMapping<TIdentity>()
					.HasPrecision(28, 0);

			// For string wrappers where we can ascertain the desired case-sensitivity
			if (ValueWrapperConfigurator.IsStringWrapperWithKnownCaseSensitivity<TIdentity, TCore>(out var caseSensitivity))
			{{
				var comparerType = caseSensitivity switch
				{{
					StringComparison.Ordinal => typeof(OrdinalStringComparer),
					StringComparison.OrdinalIgnoreCase => typeof(OrdinalIgnoreCaseStringComparer),
					_ => null,
				}};
				this.ConfigurationBuilder.Properties<TIdentity>()
					.HaveConversion(conversionType: typeof(IdentityConverter<TIdentity, TCore>), comparerType: null, providerComparerType: comparerType);

				if (ValueWrapperConfigurator.GetApplicableCollationFromOptions(caseSensitivity, this.Options) is string targetCollation)
					this.ConfigurationBuilder.Properties<TIdentity>()
						.UseCollation(targetCollation);
			}}
		}}

		private sealed class IdentityConverter<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] TModel, TProvider>
			: ValueConverter<TModel, TProvider>
			where TModel : IValueWrapper<TModel, TProvider>
		{{
			public IdentityConverter()
				: base(
					model => DomainObjectSerializer.Serialize<TModel, TProvider>(model)!,
					provider => DomainObjectSerializer.Deserialize<TModel, TProvider>(provider)!,
					mappingHints: typeof(TProvider) == typeof(decimal) ? EntityFrameworkIdentityConfigurator.DecimalIdConverterMappingHints : null)
			{{
			}}
		}}
	}}

	file sealed record class EntityFrameworkWrapperValueObjectConfigurator(
		ModelConfigurationBuilder ConfigurationBuilder,
		WrapperValueObjectConfigurationOptions? Options = null)
		: IWrapperValueObjectConfigurator
	{{
		public void ConfigureWrapperValueObject<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] TWrapper, TValue, TCore>(
			in IWrapperValueObjectConfigurator.Args args)
			where TWrapper : IWrapperValueObject<TValue>, IDirectValueWrapper<TWrapper, TValue>, ICoreValueWrapper<TWrapper, TCore>
			where TValue : notnull
		{{
			// Configure properties of the type
			this.ConfigurationBuilder.Properties<TWrapper>()
				.HaveConversion<WrapperValueObjectConverter<TWrapper, TCore>>();

			// Configure non-property occurrences of the type, such as in CAST(), SUM(), AVG(), etc.
			this.ConfigurationBuilder.DefaultTypeMapping<TWrapper>()
				.HasConversion<WrapperValueObjectConverter<TWrapper, TCore>>();

			// For string wrappers where we can ascertain the desired case-sensitivity
			if (ValueWrapperConfigurator.IsStringWrapperWithKnownCaseSensitivity<TWrapper, TCore>(out var caseSensitivity))
			{{
				var comparerType = caseSensitivity switch
				{{
					StringComparison.Ordinal => typeof(OrdinalStringComparer),
					StringComparison.OrdinalIgnoreCase => typeof(OrdinalIgnoreCaseStringComparer),
					_ => null,
				}};
				this.ConfigurationBuilder.Properties<TWrapper>()
					.HaveConversion(conversionType: typeof(WrapperValueObjectConverter<TWrapper, TCore>), comparerType: null, providerComparerType: comparerType);

				if (ValueWrapperConfigurator.GetApplicableCollationFromOptions(caseSensitivity, this.Options) is string targetCollation)
					this.ConfigurationBuilder.Properties<TWrapper>()
						.UseCollation(targetCollation);
			}}
		}}

		private sealed class WrapperValueObjectConverter<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] TModel, TProvider>
			: ValueConverter<TModel, TProvider>
			where TModel : IValueWrapper<TModel, TProvider>
		{{
			public WrapperValueObjectConverter()
				: base(
					model => DomainObjectSerializer.Serialize<TModel, TProvider>(model)!,
					provider => DomainObjectSerializer.Deserialize<TModel, TProvider>(provider)!,
					mappingHints: null)
			{{
			}}
		}}
	}}

	file sealed record class EntityFrameworkEntityConfigurator(
		Action InvokeConfigurationCallbacks)
		: IEntityConfigurator, IDomainEventConfigurator, IEntityTypeAddedConvention, IModelFinalizingConvention
	{{
		private Dictionary<Type, IConventionEntityType> EntityTypeConventionsByType {{ get; }} = [];

		public void ProcessEntityTypeAdded(IConventionEntityTypeBuilder entityTypeBuilder, IConventionContext<IConventionEntityTypeBuilder> context)
		{{
			var type = entityTypeBuilder.Metadata.ClrType;
			if (!type.IsAbstract && !type.IsInterface && !type.IsGenericTypeDefinition)
				this.EntityTypeConventionsByType[entityTypeBuilder.Metadata.ClrType] = entityTypeBuilder.Metadata;
		}}

		public void ProcessModelFinalizing(IConventionModelBuilder modelBuilder, IConventionContext<IConventionModelBuilder> context)
		{{
			this.InvokeConfigurationCallbacks();

			// Clean up
			this.EntityTypeConventionsByType.Clear();
			this.EntityTypeConventionsByType.TrimExcess();
		}}

		public void ConfigureEntity<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] TEntity>(
			in IEntityConfigurator.Args args)
			where TEntity : IEntity
		{{
			if (!this.EntityTypeConventionsByType.TryGetValue(typeof(TEntity), out var entityTypeConvention))
				return;

#pragma warning disable EF1001 // Internal EF Core API usage -- No public APIs are available for this yet, and interceptors do not work because EF demands a usable ctor even the interceptor would prevent ctor usage
			var entityType = entityTypeConvention as EntityType ?? throw new NotImplementedException($""{{entityTypeConvention.GetType().Name}} was received when {{nameof(EntityType)}} was expected. Either a non-entity was passed or internal changes to Entity Framework have broken this code."");
			entityType.ConstructorBinding = UninitializedInstantiationBinding.Create(() => DomainObjectSerializer.Deserialize<TEntity>());
#pragma warning restore EF1001 // Internal EF Core API usage
		}}

		public void ConfigureDomainEvent<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] TDomainEvent>(
			in IDomainEventConfigurator.Args args)
			where TDomainEvent : IDomainObject
		{{
			if (!this.EntityTypeConventionsByType.TryGetValue(typeof(TDomainEvent), out var entityTypeConvention))
				return;

#pragma warning disable EF1001 // Internal EF Core API usage -- No public APIs are available for this yet, and interceptors do not work because EF demands a usable ctor even the interceptor would prevent ctor usage
			var entityType = entityTypeConvention as EntityType ?? throw new NotImplementedException($""{{entityTypeConvention.GetType().Name}} was received when {{nameof(EntityType)}} was expected. Either a non-entity was passed or internal changes to Entity Framework have broken this code."");
			entityType.ConstructorBinding = UninitializedInstantiationBinding.Create(() => DomainObjectSerializer.Deserialize<TDomainEvent>());
#pragma warning restore EF1001 // Internal EF Core API usage
		}}

		private sealed class UninitializedInstantiationBinding
			: InstantiationBinding
		{{
			[SuppressMessage(""Trimming"", ""IL2111:Method with DynamicallyAccessedMembersAttribute is accessed via reflection"", Justification = ""Fallback only, and we have annotated the input we take for this."")]
			private static readonly MethodInfo GetUninitializedObjectMethod = typeof(RuntimeHelpers).GetMethod(nameof(RuntimeHelpers.GetUninitializedObject))!;

			public override Type RuntimeType {{ get; }}
			private Expression Expression {{ get; }}

			public static UninitializedInstantiationBinding Create<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] T>(
				Expression<Func<T>> expression)
			{{
				return new UninitializedInstantiationBinding(typeof(T), expression.Body);
			}}

			public UninitializedInstantiationBinding(
				[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type runtimeType,
				Expression? expression = null)
				: base(Array.Empty<ParameterBinding>())
			{{
				this.RuntimeType = runtimeType;
				this.Expression = expression ??
					Expression.Convert(
						Expression.Call(method: GetUninitializedObjectMethod, arguments: Expression.Constant(this.RuntimeType)),
						this.RuntimeType);
			}}

			public override Expression CreateConstructorExpression(ParameterBindingInfo bindingInfo)
			{{
				return this.Expression;
			}}

			public override InstantiationBinding With(IReadOnlyList<ParameterBinding> parameterBindings)
			{{
				return this;
			}}
		}}
	}}

	file sealed class OrdinalStringComparer : ValueComparer<string>
	{{
		public OrdinalStringComparer()
			: base(
				equalsExpression: (left, right) => String.Equals(left, right, StringComparison.Ordinal),
				hashCodeExpression: value => String.GetHashCode(value, StringComparison.Ordinal),
				snapshotExpression: value => value)
		{{
		}}
	}}

	public sealed record class CustomizingIdentityConfigurator(
		ModelConfigurationBuilder ConfigurationBuilder,
		Action<CustomizingIdentityConfigurator.Context> Callback)
		: IIdentityConfigurator
	{{
		public readonly struct Context
		{{
			public ModelConfigurationBuilder ConfigurationBuilder {{ get; init; }}
			public Type ModelType {{ get; init; }}
			public Type UnderlyingType {{ get; init; }}
			public Type CoreType {{ get; init; }}
			public IIdentityConfigurator.Args Args {{ get; init; }}
		}}

		public void ConfigureIdentity<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] TIdentity, TUnderlying, TCore>(
			in IIdentityConfigurator.Args args)
			where TIdentity : IIdentity<TUnderlying>, IDirectValueWrapper<TIdentity, TUnderlying>, ICoreValueWrapper<TIdentity, TCore>
			where TUnderlying : notnull, IEquatable<TUnderlying>, IComparable<TUnderlying>
		{{
			var customizationArgs = new Context()
			{{
				ConfigurationBuilder = this.ConfigurationBuilder,
				ModelType = typeof(TIdentity),
				UnderlyingType = typeof(TUnderlying),
				CoreType = typeof(TCore),
				Args = args,
			}};
			this.Callback.Invoke(customizationArgs);
		}}
	}}

	public sealed record class CustomizingWrapperValueObjectConfigurator(
		ModelConfigurationBuilder ConfigurationBuilder,
		Action<CustomizingWrapperValueObjectConfigurator.Context> Callback)
		: IWrapperValueObjectConfigurator
	{{
		public readonly struct Context
		{{
			public ModelConfigurationBuilder ConfigurationBuilder {{ get; init; }}
			public Type ModelType {{ get; init; }}
			public Type UnderlyingType {{ get; init; }}
			public Type CoreType {{ get; init; }}
			public IWrapperValueObjectConfigurator.Args Args {{ get; init; }}
		}}

		public void ConfigureWrapperValueObject<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] TWrapper, TValue, TCore>(in IWrapperValueObjectConfigurator.Args args)
			where TWrapper : IWrapperValueObject<TValue>, IDirectValueWrapper<TWrapper, TValue>, ICoreValueWrapper<TWrapper, TCore>
			where TValue : notnull
		{{
			var customizationArgs = new Context()
			{{
				ConfigurationBuilder = this.ConfigurationBuilder,
				ModelType = typeof(TWrapper),
				UnderlyingType = typeof(TValue),
				CoreType = typeof(TCore),
				Args = args,
			}};
			this.Callback.Invoke(customizationArgs);
		}}
	}}

	file sealed class OrdinalIgnoreCaseStringComparer : ValueComparer<string>
	{{
		public OrdinalIgnoreCaseStringComparer()
			: base(
				equalsExpression: (left, right) => String.Equals(left, right, StringComparison.OrdinalIgnoreCase),
				hashCodeExpression: value => String.GetHashCode(value, StringComparison.OrdinalIgnoreCase),
				snapshotExpression: value => value)
		{{
		}}
	}}
}}
";

		AddSource(context, source, "EntityFrameworkDomainModelConfigurationExtensions", $"Architect.DomainModeling.EntityFramework");
	}

	internal sealed record Generatable
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
