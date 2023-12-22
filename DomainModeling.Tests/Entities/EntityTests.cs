using Xunit;

namespace Architect.DomainModeling.Tests.Entities;

public class EntityTests
{
	[Theory]
	[InlineData(null, false)]
	[InlineData(0, true)]
	[InlineData(1, false)]
	[InlineData(-1, false)]
	public void DefaultId_WithClassId_ShouldEquateAsExpected(int? value, bool expectedResult)
	{
		var instance = new ClassIdEntity(value is null ? null! : new ConcreteId() { Value = value.Value, });

		Assert.Equal(expectedResult, instance.HasDefaultId());
	}

	[Theory]
	[InlineData(null, false)]
	[InlineData(0, false)]
	[InlineData(1, true)]
	[InlineData(-1, true)]
	public void GetHashCode_WithClassId_ShouldEquateAsExpected(int? value, bool expectedResult)
	{
		var one = new ClassIdEntity(value is null ? null! : new ConcreteId() { Value = value.Value, });
		var two = new ClassIdEntity(value is null ? null! : new ConcreteId() { Value = value.Value, });

		Assert.Equal(expectedResult, one.GetHashCode().Equals(two.GetHashCode()));
	}

	[Theory]
	[InlineData(null, false)]
	[InlineData(0, false)]
	[InlineData(1, true)]
	[InlineData(-1, true)]
	public void Equals_WithClassId_ShouldEquateAsExpected(int? value, bool expectedResult)
	{
		var one = new ClassIdEntity(value is null ? null! : new ConcreteId() { Value = value.Value, });
		var two = new ClassIdEntity(value is null ? null! : new ConcreteId() { Value = value.Value, });

		Assert.Equal(one, one);
		Assert.Equal(two, two);
		Assert.Equal(expectedResult, one.Equals(two));
	}

	[Theory]
	[InlineData(null, true)]
	[InlineData(0UL, true)]
	[InlineData(1UL, false)]
	public void DefaultId_WithStructId_ShouldEquateAsExpected(ulong? value, bool expectedResult)
	{
		var instance = new StructIdEntity(value is null ? default : new UlongId(value.Value));

		Assert.Equal(expectedResult, instance.HasDefaultId());
	}

	[Theory]
	[InlineData(null, false)]
	[InlineData(0UL, false)]
	[InlineData(1UL, true)]
	public void GetHashCode_WithStructId_ShouldEquateAsExpected(ulong? value, bool expectedResult)
	{
		var one = new StructIdEntity(value is null ? default : new UlongId(value.Value));
		var two = new StructIdEntity(value is null ? default : new UlongId(value.Value));

		Assert.Equal(expectedResult, one.GetHashCode().Equals(two.GetHashCode()));
	}

	[Theory]
	[InlineData(null, false)]
	[InlineData(0UL, false)]
	[InlineData(1UL, true)]
	public void Equals_WithStructId_ShouldEquateAsExpected(ulong? value, bool expectedResult)
	{
		var one = new StructIdEntity(value is null ? default : new UlongId(value.Value));
		var two = new StructIdEntity(value is null ? default : new UlongId(value.Value));

		Assert.Equal(one, one);
		Assert.Equal(two, two);
		Assert.Equal(expectedResult, one.Equals(two));
	}

	[Theory]
	[InlineData(null, true)]
	[InlineData("", false)]
	[InlineData("1", false)]
	public void DefaultId_WithStringId_ShouldEquateAsExpected(string? value, bool expectedResult)
	{
		var instance = new StringIdEntity(value!);

		Assert.Equal(expectedResult, instance.HasDefaultId());
	}

	[Theory]
	[InlineData(null, false)]
	[InlineData("", true)]
	[InlineData("1", true)]
	public void GetHashCode_WithStringId_ShouldEquateAsExpected(string? value, bool expectedResult)
	{
		var one = new StringIdEntity(value!);
		var two = new StringIdEntity(value!);

		Assert.Equal(expectedResult, one.GetHashCode().Equals(two.GetHashCode()));
	}

	[Theory]
	[InlineData(null, false)]
	[InlineData("", true)]
	[InlineData("1", true)]
	public void Equals_WithStringId_ShouldEquateAsExpected(string? value, bool expectedResult)
	{
		var one = new StringIdEntity(value!);
		var two = new StringIdEntity(value!);

		Assert.Equal(one, one);
		Assert.Equal(two, two);
		Assert.Equal(expectedResult, one.Equals(two));
	}

	[Fact]
	public void Equals_WithSameIdTypeAndValueButDifferentEntityType_ShouldEquateAsExpected()
	{
		var one = new StringIdEntity("1");
		var two = new OtherStringIdEntity("1");

		Assert.NotEqual((Entity<string>)one, two);
	}

	[Theory]
	[InlineData(null, true)]
	[InlineData("", true)]
	[InlineData("1", false)]
	public void DefaultId_WithStringWrappingId_ShouldEquateAsExpected(string? value, bool expectedResult)
	{
		var instance = new StringWrappingIdEntity(value!);

		Assert.Equal(expectedResult, instance.HasDefaultId());
	}

	[Theory]
	[InlineData(null, false)] // Null and empty string are both treated as the default ID value (and represented as "")
	[InlineData("", false)] // Null and empty string are both treated as the default ID value (and represented as "")
	[InlineData("1", true)]
	public void GetHashCode_WithStringWrappingId_ShouldEquateAsExpected(string? value, bool expectedResult)
	{
		var one = new StringWrappingIdEntity(value!);
		var two = new StringWrappingIdEntity(value!);

		Assert.Equal(expectedResult, one.GetHashCode().Equals(two.GetHashCode()));
	}

	[Theory]
	[InlineData(null, false)] // Null and empty string are both treated as the default ID value (and represented as "")
	[InlineData("", false)] // Null and empty string are both treated as the default ID value (and represented as "")
	[InlineData("1", true)]
	public void Equals_WithStringWrappingId_ShouldEquateAsExpected(string? value, bool expectedResult)
	{
		var one = new StringWrappingIdEntity(value!);
		var two = new StringWrappingIdEntity(value!);

		Assert.Equal(one, one);
		Assert.Equal(two, two);
		Assert.Equal(expectedResult, one.Equals(two));
	}

	[Theory]
	[InlineData(null, true)]
	[InlineData(0, false)] // Interface cannot be constructed, so default is null
	[InlineData(1, false)]
	[InlineData(-1, false)]
	public void DefaultId_WithInterfaceId_ShouldEquateAsExpected(int? value, bool expectedResult)
	{
		var instance = new InterfaceIdEntity(value is null ? null! : new ConcreteId() { Value = value.Value, });

		Assert.Equal(expectedResult, instance.HasDefaultId());
	}

	[Theory]
	[InlineData(null, false)]
	[InlineData(0, true)] // Interface cannot be constructed, so default is null
	[InlineData(1, true)]
	[InlineData(-1, true)]
	public void GetHashCode_WithInterfaceId_ShouldEquateAsExpected(int? value, bool expectedResult)
	{
		var one = new InterfaceIdEntity(value is null ? null! : new ConcreteId() { Value = value.Value, });
		var two = new InterfaceIdEntity(value is null ? null! : new ConcreteId() { Value = value.Value, });

		Assert.Equal(expectedResult, one.GetHashCode().Equals(two.GetHashCode()));
	}

	[Theory]
	[InlineData(null, false)]
	[InlineData(0, true)] // Interface cannot be constructed, so default is null
	[InlineData(1, true)]
	[InlineData(-1, true)]
	public void Equals_WithInterfaceId_ShouldEquateAsExpected(int? value, bool expectedResult)
	{
		var one = new InterfaceIdEntity(value is null ? null! : new ConcreteId() { Value = value.Value, });
		var two = new InterfaceIdEntity(value is null ? null! : new ConcreteId() { Value = value.Value, });

		Assert.Equal(one, one);
		Assert.Equal(two, two);
		Assert.Equal(expectedResult, one.Equals(two));
	}

	[Theory]
	[InlineData(null, true)]
	[InlineData(0, false)] // Abstract cannot be constructed, so default is null
	[InlineData(1, false)]
	[InlineData(-1, false)]
	public void DefaultId_WithAbstractId_ShouldEquateAsExpected(int? value, bool expectedResult)
	{
		var instance = new AbstractIdEntity(value is null ? null! : new ConcreteId() { Value = value.Value, });

		Assert.Equal(expectedResult, instance.HasDefaultId());
	}

	[Theory]
	[InlineData(null, false)]
	[InlineData(0, true)] // Abstract cannot be constructed, so default is null
	[InlineData(1, true)]
	[InlineData(-1, true)]
	public void GetHashCode_WithAbstractId_ShouldEquateAsExpected(int? value, bool expectedResult)
	{
		var one = new AbstractIdEntity(value is null ? null! : new ConcreteId() { Value = value.Value, });
		var two = new AbstractIdEntity(value is null ? null! : new ConcreteId() { Value = value.Value, });

		Assert.Equal(expectedResult, one.GetHashCode().Equals(two.GetHashCode()));
	}

	[Theory]
	[InlineData(null, false)]
	[InlineData(0, true)] // Abstract cannot be constructed, so default is null
	[InlineData(1, true)]
	[InlineData(-1, true)]
	public void Equals_WithAbstractId_ShouldEquateAsExpected(int? value, bool expectedResult)
	{
		var one = new AbstractIdEntity(value is null ? null! : new ConcreteId() { Value = value.Value, });
		var two = new AbstractIdEntity(value is null ? null! : new ConcreteId() { Value = value.Value, });

		Assert.Equal(one, one);
		Assert.Equal(two, two);
		Assert.Equal(expectedResult, one.Equals(two));
	}

	private sealed class StructIdEntity : Entity<UlongId, ulong>
	{
		public StructIdEntity(ulong id)
			: base(new UlongId(id))
		{
		}

		public bool HasDefaultId() => Equals(this.Id, DefaultId);
	}

	private sealed class ClassIdEntity : Entity<ConcreteId>
	{
		public ClassIdEntity(ConcreteId id)
			: base(id)
		{
		}

		public bool HasDefaultId() => Equals(this.Id, DefaultId);
	}

	private sealed class StringIdEntity : Entity<string>
	{
		public StringIdEntity(string id)
			: base(id)
		{
		}

		public bool HasDefaultId() => Equals(this.Id, DefaultId);
	}

	private sealed class OtherStringIdEntity : Entity<string>
	{
		public OtherStringIdEntity(string id)
			: base(id)
		{
		}
	}

	private sealed class StringWrappingIdEntity : Entity<StringBasedId, string>
	{
		public StringWrappingIdEntity(StringBasedId id)
			: base(id)
		{
		}

		public bool HasDefaultId() => Equals(this.Id, DefaultId);
	}

	private sealed class InterfaceIdEntity : Entity<IId>
	{
		public InterfaceIdEntity(IId id)
			: base(id)
		{
		}

		public bool HasDefaultId() => Equals(this.Id, DefaultId);
	}

	private sealed class AbstractIdEntity : Entity<AbstractId>
	{
		public AbstractIdEntity(AbstractId id)
			: base(id)
		{
		}

		public bool HasDefaultId() => Equals(this.Id, DefaultId);
	}

	private interface IId : IEquatable<IId>
	{
	}

	private abstract class AbstractId : IEquatable<AbstractId>, IId
	{
		public abstract override int GetHashCode();
		public abstract override bool Equals(object? obj);
		public bool Equals(AbstractId? other) => this.Equals((object?)other);
		public bool Equals(IId? other) => this.Equals((object?)other);
	}

	private sealed class ConcreteId : AbstractId, IEquatable<ConcreteId>
	{
		public override int GetHashCode() => this.Value.GetHashCode();
		public override bool Equals(object? obj) => obj is ConcreteId other && Equals(this.Value, other.Value);
		public bool Equals(ConcreteId? other) => this.Equals((object?)other);

		public int Value { get; set; }
	}
}
