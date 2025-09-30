using System.Diagnostics.CodeAnalysis;
using Architect.DomainModeling.Configuration;
using Architect.DomainModeling.Conversions;
using Architect.DomainModeling.Tests.Common;
using Architect.DomainModeling.Tests.IdentityTestTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Architect.DomainModeling.Tests.EntityFramework;

public sealed class EntityFrameworkConfigurationGeneratorTests : IDisposable
{
	internal static bool AllowParameterizedConstructors = true;

	private ILoggerFactory LoggerFactory { get; }
	private CapturingLoggerProvider CapturingLoggerProvider { get; }

	private string UniqueName { get; } = Guid.NewGuid().ToString("N");
	private TestDbContext DbContext { get; }

	public EntityFrameworkConfigurationGeneratorTests()
	{
		this.CapturingLoggerProvider = new CapturingLoggerProvider();
		this.LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(options => options
			.SetMinimumLevel(LogLevel.Debug)
			.AddProvider(this.CapturingLoggerProvider));

		this.DbContext = new TestDbContext($"DataSource={this.UniqueName};Mode=Memory;Cache=Shared;", this.LoggerFactory);
		this.DbContext.Database.OpenConnection();
	}

	public void Dispose()
	{
		this.DbContext.Dispose();
	}

	[Fact]
	public void ConfigureConventions_WithAllExtensionsCalled_ShouldBeAbleToWorkWithAllDomainObjects()
	{
		var values = new ValueObjectForEF(
			(Wrapper1ForEF)"One",
			(Wrapper2ForEF)2,
			new FormatAndParseTestingIntId(3),
			new LazyStringWrapper(new Lazy<string>("4")),
			new LazyIntWrapper(new Lazy<int>(5)),
			new NumericStringId("6"));
		var entity = new EntityForEF(values);
		var domainEvent = new DomainEventForEF(id: 2, ignored: null!);

		this.DbContext.Database.EnsureCreated();
		this.DbContext.AddRange(entity, domainEvent);
		this.DbContext.SaveChanges();
		this.DbContext.ChangeTracker.Clear();

		// Throw if deserialization attempts to use the parameterized constructors
		AllowParameterizedConstructors = false;

		var reloadedEntity = this.DbContext.Set<EntityForEF>().Single();
		var reloadedDomainEvent = this.DbContext.Set<DomainEventForEF>().Single();

		// Confirm that construction happened as expected
		Assert.Throws<MissingMethodException>(Activator.CreateInstance<Wrapper1ForEF>); // Should have no default ctor
		Assert.Throws<MissingMethodException>(Activator.CreateInstance<Wrapper2ForEF>); // Should have no default ctor
		Assert.Throws<MissingMethodException>(Activator.CreateInstance<DomainEventForEF>); // Should have no default ctor
		Assert.False(reloadedDomainEvent.HasFieldInitializerRun); // Has no default ctor, so should have used GetUninitializedObject
		Assert.True(reloadedEntity.HasFieldInitializerRun); // Has default ctor that should have been used
		Assert.True(reloadedEntity.Values.HasFieldInitializerRun); // Should have generated default ctor that should have been used
		Assert.True(reloadedEntity.Values.One.HasFieldInitializerRun); // Should have generated default ctor that should have been used
		Assert.True(reloadedEntity.Values.Two.HasFieldInitializerRun); // Should have generated default ctor that should have been used

		Assert.Equal(2, reloadedDomainEvent.Id);

		Assert.Equal("A", reloadedEntity.Id.Value);
		Assert.Equal("One", reloadedEntity.Values.One);
		Assert.Equal(2m, reloadedEntity.Values.Two);
		Assert.Equal(3, reloadedEntity.Values.Three.Value?.Value.Value);
		Assert.Equal("4", reloadedEntity.Values.Four.Value.Value);
		Assert.Equal(5, reloadedEntity.Values.Five.Value.Value);
		Assert.Equal("6", reloadedEntity.Values.Six?.Value);

		// This property should be mapped to int via ICoreValueWrapper<NumericStringId, int>
		var mappingForStringWithCustomIntCore = this.DbContext.Model.FindEntityType(typeof(EntityForEF))?.FindNavigation(nameof(EntityForEF.Values))?.TargetEntityType
			.FindProperty(nameof(EntityForEF.Values.Six));
		var columnTypeForStringWrapperWithCustomIntCore = mappingForStringWithCustomIntCore?.GetColumnType();
		var providerClrTypeForStringWrapperWithCustomIntCore = mappingForStringWithCustomIntCore?.GetValueConverter()?.ProviderClrType;
		Assert.Equal("INTEGER", columnTypeForStringWrapperWithCustomIntCore);
		Assert.Equal(typeof(int), providerClrTypeForStringWrapperWithCustomIntCore);

		// Case-sensitivity should be honored, even during key comparisons
		Assert.Same(reloadedEntity, this.DbContext.Set<EntityForEF>().Find(new EntityForEFId("a")));

		// The database's collation should have been made ignore-case by our ConfigureIdentityConventions() options
		Assert.Same(reloadedEntity, this.DbContext.Set<EntityForEF>().SingleOrDefault(x => x.Id == "a"));

		// The logs should warn that Wrapper1ForEF has a mismatching collation in the database
		var logs = this.CapturingLoggerProvider.Logs;
		var warning = Assert.Single(logs, log => log.StartsWith("[Warning]"));
		Assert.Equal("[Warning] Architect.DomainModeling.Tests.EntityFramework.ValueObjectForEF.One uses OrdinalIgnoreCase comparisons, but the default SQLite database collation acts more like Ordinal - use the options in ConfigureIdentityConventions() and ConfigureWrapperValueObjectConventions() to specify default collations, or configure property collations manually", warning);

		// The logs should show that certain collations were set
		Assert.Contains(logs, log => log.Equals("[Debug] Set collation BINARY for SomeStringId properties based on the type's case-sensitivity"));
		Assert.Contains(logs, log => log.Equals("[Debug] Set collation NOCASE for EntityForEFId properties based on the type's case-sensitivity"));
	}
}

internal sealed class TestDbContext(
	string connectionString, ILoggerFactory loggerFactory)
	: DbContext(new DbContextOptionsBuilder<TestDbContext>()
		.UseLoggerFactory(loggerFactory)
		.UseSqlite(connectionString).Options)
{
	[SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "Suppression is necessary.")]
	[SuppressMessage("Usage", "CA2263:Prefer generic overload when type is known", Justification = "We have no generic info for types received from callbacks.")]
	protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
	{
		configurationBuilder.Conventions.Remove(typeof(ConstructorBindingConvention));
		configurationBuilder.Conventions.Remove(typeof(RelationshipDiscoveryConvention));
		configurationBuilder.Conventions.Remove(typeof(PropertyDiscoveryConvention));

		configurationBuilder.ConfigureDomainModelConventions(domainModel =>
		{
			domainModel.ConfigureIdentityConventions(new IdentityConfigurationOptions() { CaseSensitiveCollation = "BINARY", IgnoreCaseCollation = "NOCASE", });
			domainModel.ConfigureWrapperValueObjectConventions();
			domainModel.ConfigureEntityConventions();
			domainModel.ConfigureDomainEventConventions();

			domainModel.CustomizeWrapperValueObjectConventions(context =>
			{
				// For a wrapper whose core type EF does not support, overwriting the conventions with our own should work
				if (context.CoreType == typeof(Lazy<string>))
				{
					context.ConfigurationBuilder.Properties(context.ModelType)
						.HaveConversion(typeof(LazyStringWrapperConverter));
					context.ConfigurationBuilder.DefaultTypeMapping(context.ModelType)
						.HasConversion(typeof(LazyStringWrapperConverter));
				}
			});
		});
	}

	private class LazyStringWrapperConverter : ValueConverter<LazyStringWrapper, string>
	{
		public LazyStringWrapperConverter()
			: base(
				v => v.Value.Value,
				v => new LazyStringWrapper(new Lazy<string>(v)))
		{
		}
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		// Configure only which entities, properties, and keys exist
		// Do not configure any conversions or constructor bindings, to see that our conventions handle those

		modelBuilder.Entity<EntityForEF>(builder =>
		{
			builder.Property(x => x.Id);

			builder.OwnsOne(x => x.Values, values =>
			{
				values.Property(x => x.One);
				values.Property(x => x.Two);
				values.Property(x => x.Three);
				values.Property(x => x.Four);
				values.Property(x => x.Five);
				values.Property(x => x.Six);
			});

			builder.HasKey(x => x.Id);
		});

		modelBuilder.Entity<DomainEventForEF>(builder =>
		{
			builder.Property(x => x.Id);

			builder.HasKey(x => x.Id);
		});
	}
}

[DomainEvent]
internal sealed class DomainEventForEF : IDomainObject
{
	/// <summary>
	/// This lets us test if a constructor is used or not.
	/// </summary>
	public bool HasFieldInitializerRun { get; } = true;

	public DomainEventForEFId Id { get; set; } = 1;

	public DomainEventForEF(DomainEventForEFId id, object ignored)
	{
		if (!EntityFrameworkConfigurationGeneratorTests.AllowParameterizedConstructors)
			throw new InvalidOperationException("Deserialization was not allowed to use the parameterized constructors.");

		_ = ignored;

		this.Id = id;
	}
}
[IdentityValueObject<decimal>]
public readonly partial record struct DomainEventForEFId;

[IdentityValueObject<string>]
public partial record struct EntityForEFId
{
	private StringComparison StringComparison => StringComparison.OrdinalIgnoreCase;
}

[Entity]
internal sealed class EntityForEF : Entity<EntityForEFId>
{
	/// <summary>
	/// This lets us test if a constructor is used or not.
	/// </summary>
	public bool HasFieldInitializerRun { get; } = true;

	public ValueObjectForEF Values { get; }

	public EntityForEF(ValueObjectForEF values)
		: base(id: "A")
	{
		if (!EntityFrameworkConfigurationGeneratorTests.AllowParameterizedConstructors)
			throw new InvalidOperationException("Deserialization was not allowed to use the parameterized constructors.");

		this.Values = values;
	}

#pragma warning disable IDE0079 // Remove unnecessary suppression -- Suppression below is falsely flagged as unnecessary
#pragma warning disable CS8618 // Reconstitution constructor
	private EntityForEF()
		: base(default)
	{
	}
#pragma warning restore CS8618
#pragma warning restore IDE0079
}

[WrapperValueObject<string>]
internal sealed partial class Wrapper1ForEF
{
	private StringComparison StringComparison => StringComparison.OrdinalIgnoreCase;

	/// <summary>
	/// This lets us test if a constructor is used or not.
	/// </summary>
	public bool HasFieldInitializerRun { get; } = true;

	public Wrapper1ForEF(string value)
	{
		if (!EntityFrameworkConfigurationGeneratorTests.AllowParameterizedConstructors)
			throw new InvalidOperationException("Deserialization was not allowed to use the parameterized constructors.");

		this.Value = value ?? throw new ArgumentNullException(nameof(value));
	}
}

[WrapperValueObject<decimal>]
internal sealed partial class Wrapper2ForEF : WrapperValueObject<decimal>
{
	/// <summary>
	/// This lets us test if a constructor is used or not.
	/// </summary>
	public bool HasFieldInitializerRun { get; } = true;

	public Wrapper2ForEF(decimal value)
	{
		if (!EntityFrameworkConfigurationGeneratorTests.AllowParameterizedConstructors)
			throw new InvalidOperationException("Deserialization was not allowed to use the parameterized constructors.");

		this.Value = value;
	}
}

[WrapperValueObject<Lazy<string>>]
internal sealed partial class LazyStringWrapper
{
}

[WrapperValueObject<Lazy<int>>]
internal sealed partial class LazyIntWrapper : ICoreValueWrapper<LazyIntWrapper, int> // Custom core value
{
	// Manual interface implementation to support custom core value
	int IValueWrapper<LazyIntWrapper, int>.Value => this.Value.Value;
	static LazyIntWrapper IValueWrapper<LazyIntWrapper, int>.Create(int value) => new LazyIntWrapper(new Lazy<int>(value));
	int IValueWrapper<LazyIntWrapper, int>.Serialize() => this.Value.Value;
	static LazyIntWrapper IValueWrapper<LazyIntWrapper, int>.Deserialize(int value) => DomainObjectSerializer.Deserialize<LazyIntWrapper, Lazy<int>>(new Lazy<int>(value));
}

[IdentityValueObject<string>]
internal partial struct NumericStringId : ICoreValueWrapper<NumericStringId, int> // Custom core value
{
	// Manual interface implementation to support custom core value
	int IValueWrapper<NumericStringId, int>.Value => Int32.Parse(this.Value);
	static NumericStringId IValueWrapper<NumericStringId, int>.Create(int value) => new NumericStringId(value.ToString());
	int IValueWrapper<NumericStringId, int>.Serialize() => Int32.Parse(this.Value);
	static NumericStringId IValueWrapper<NumericStringId, int>.Deserialize(int value) => DomainObjectSerializer.Deserialize<NumericStringId, string>(value.ToString());
}

[ValueObject]
internal sealed partial class ValueObjectForEF
{
	/// <summary>
	/// This lets us test if a constructor is used or not.
	/// </summary>
	public bool HasFieldInitializerRun = true;

	public Wrapper1ForEF One { get; private init; }
	public Wrapper2ForEF Two { get; private init; }
	public FormatAndParseTestingIntId Three { get; private init; }
	public LazyStringWrapper Four { get; private init; }
	public LazyIntWrapper Five { get; private init; }
	public NumericStringId? Six { get; private init; }

	public ValueObjectForEF(
		Wrapper1ForEF one,
		Wrapper2ForEF two,
		FormatAndParseTestingIntId three,
		LazyStringWrapper four,
		LazyIntWrapper five,
		NumericStringId? six)
	{
		if (!EntityFrameworkConfigurationGeneratorTests.AllowParameterizedConstructors)
			throw new InvalidOperationException("Deserialization was not allowed to use the parameterized constructors.");

		this.One = one;
		this.Two = two;
		this.Three = three;
		this.Four = four;
		this.Five = five;
		this.Six = six;
	}
}
