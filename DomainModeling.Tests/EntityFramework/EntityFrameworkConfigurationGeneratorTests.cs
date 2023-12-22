using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Xunit;

namespace Architect.DomainModeling.Tests.EntityFramework;

public sealed class EntityFrameworkConfigurationGeneratorTests : IDisposable
{
	internal static bool AllowParameterizedConstructors = true;

	private string UniqueName { get; } = Guid.NewGuid().ToString("N");
	private TestDbContext DbContext { get; }

	public EntityFrameworkConfigurationGeneratorTests()
	{
		this.DbContext = new TestDbContext($"DataSource={this.UniqueName};Mode=Memory;Cache=Shared;");
		this.DbContext.Database.OpenConnection();
	}

	public void Dispose()
	{
		this.DbContext.Dispose();
	}

	[Fact]
	public void ConfigureConventions_WithAllExtensionsCalled_ShouldBeAbleToWorkWithAllDomainObjects()
	{
		var values = new ValueObjectForEF((Wrapper1ForEF)"One", (Wrapper2ForEF)2);
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

		Assert.Equal(2, reloadedEntity.Id.Value);
		Assert.Equal("One", reloadedEntity.Values.One);
		Assert.Equal(2m, reloadedEntity.Values.Two);
	}
}

internal sealed class TestDbContext(
	string connectionString)
	: DbContext(new DbContextOptionsBuilder<TestDbContext>().UseSqlite(connectionString).Options)
{
	protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
	{
		configurationBuilder.Conventions.Remove<ConstructorBindingConvention>();
		configurationBuilder.Conventions.Remove<RelationshipDiscoveryConvention>();
		configurationBuilder.Conventions.Remove<PropertyDiscoveryConvention>();

		configurationBuilder.ConfigureDomainModelConventions(domainModel =>
		{
			domainModel.ConfigureIdentityConventions();
			domainModel.ConfigureWrapperValueObjectConventions();
			domainModel.ConfigureEntityConventions();
			domainModel.ConfigureDomainEventConventions();
		});
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
	/// This lets us test if a constructorw as used or not.
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

[Entity]
internal sealed class EntityForEF : Entity<EntityForEFId, int>
{
	/// <summary>
	/// This lets us test if a constructorw as used or not.
	/// </summary>
	public bool HasFieldInitializerRun { get; } = true;

	public ValueObjectForEF Values { get; }

	public EntityForEF(ValueObjectForEF values)
		: base(id: 2)
	{
		if (!EntityFrameworkConfigurationGeneratorTests.AllowParameterizedConstructors)
			throw new InvalidOperationException("Deserialization was not allowed to use the parameterized constructors.");

		this.Values = values;
	}

#pragma warning disable CS8618 // Reconstitution constructor
	private EntityForEF()
		: base(default)
	{
	}
#pragma warning restore CS8618
}

[WrapperValueObject<string>]
internal sealed partial class Wrapper1ForEF
{
	protected override StringComparison StringComparison => StringComparison.Ordinal;

	/// <summary>
	/// This lets us test if a constructorw as used or not.
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
internal sealed partial class Wrapper2ForEF
{
	/// <summary>
	/// This lets us test if a constructorw as used or not.
	/// </summary>
	public bool HasFieldInitializerRun { get; } = true;

	public Wrapper2ForEF(decimal value)
	{
		if (!EntityFrameworkConfigurationGeneratorTests.AllowParameterizedConstructors)
			throw new InvalidOperationException("Deserialization was not allowed to use the parameterized constructors.");

		this.Value = value;
	}
}

[ValueObject]
internal sealed partial class ValueObjectForEF
{
	/// <summary>
	/// This lets us test if a constructorw as used or not.
	/// </summary>
	public bool HasFieldInitializerRun = true;

	public Wrapper1ForEF One { get; private init; }
	public Wrapper2ForEF Two { get; private init; }

	public ValueObjectForEF(Wrapper1ForEF one, Wrapper2ForEF two)
	{
		if (!EntityFrameworkConfigurationGeneratorTests.AllowParameterizedConstructors)
			throw new InvalidOperationException("Deserialization was not allowed to use the parameterized constructors.");

		this.One = one;
		this.Two = two;
	}
}
