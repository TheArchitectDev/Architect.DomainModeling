using Architect.DomainModeling.Tests.DummyBuilderTestTypes;
using Xunit;

namespace Architect.DomainModeling.Tests
{
	public class DummyBuilderTests
	{
		[Fact]
		public void Build_Regularly_ShouldReturnExpectedResult()
		{
			var expectedCreationDateTime = new DateTime(2000, 01, 01, 00, 00, 00, DateTimeKind.Utc).ToLocalTime();

			var result = new TestEntityDummyBuilder().Build();

			Assert.Equal(expectedCreationDateTime, result.CreationDateTime);
			Assert.Equal(DateTimeKind.Local, result.CreationDateTime.Kind);
			Assert.Equal(1, result.Count);
			Assert.Equal("Currency", result.Amount.Currency);
			Assert.Equal(1m, result.Amount.Amount.Value);
		}

		[Fact]
		public void Build_WithCustomizations_ShouldReturnExpectedResult()
		{
			var expectedCreationDateTime = new DateTime(3000, 01, 01, 00, 00, 00, DateTimeKind.Utc).ToLocalTime();

			var result = new TestEntityDummyBuilder()
				.WithCreationDateTime(DateTime.UnixEpoch)
				.WithCreationDateTime("3000-01-01") // DateTimes get a numeric overload
				.WithCreationDate(DateOnly.MaxValue)
				.WithCreationDate("1970-01-01")
				.WithCreationTime(TimeOnly.MaxValue)
				.WithCreationTime(null) // Overloads must be resolvable to a preferred overload for null (achieved with a dummy optional parameter for the non-preferred overload(s))
				.WithCreationTime("02:03:04")
				.WithCount(7)
				.WithAmount(new Money("OtherCurrency", (Amount)1.23m))
				.WithNotAProperty("Whatever")
				.Build();

			Assert.Equal(expectedCreationDateTime, result.CreationDateTime);
			Assert.Equal(DateTimeKind.Local, result.CreationDateTime.Kind);
			Assert.Equal(new DateOnly(1970, 01, 01), result.CreationDate);
			Assert.Equal(new TimeOnly(02, 03, 04), result.CreationTime);
			Assert.Equal(new DateTime(2000, 01, 01, 00, 00, 00, DateTimeKind.Utc), result.ModificationDateTime); // Generated default
			Assert.Equal(7, result.Count);
			Assert.Equal("OtherCurrency", result.Amount.Currency);
			Assert.Equal(1.23m, result.Amount.Amount.Value);
		}

		[Fact]
		public void Build_WithStringWrapperValueObject_ShouldUseEntityConstructorParameterName()
		{
			var result = new StringWrapperTestingDummyBuilder().Build();

			Assert.Equal("FirstName", result.FirstName.Value); // Generated wrapper
			Assert.Equal("LastName", result.LastName.Value); // Manual wrapper
		}
	}

	// Use a namespace, since our source generators dislike nested types
	// We will test a somewhat realistic setup: an Entity with some scalars and a ValueObject that itself contains a WrapperValueObject
	namespace DummyBuilderTestTypes
	{
		[DummyBuilder<TestEntity>]
		public sealed partial class TestEntityDummyBuilder
		{
			// Demonstrate that we can take priority over the generated members
			public TestEntityDummyBuilder WithCreationDateTime(DateTime value) => this.With(b => b.CreationDateTime = value);
		}

		[Entity]
		public sealed class TestEntity : Entity<TestEntityId, string>
		{
			public DateTime CreationDateTime { get; }
			public DateOnly CreationDate { get; }
			public TimeOnly CreationTime { get; }
			public DateTimeOffset ModificationDateTime { get; }
			public ushort Count { get; }
			public Money Amount { get; }

			/// <summary>
			/// The type's simplest non-default constructor should be used by the builder.
			/// </summary>
			/// <param name="notAProperty">Used and discarded. Used to display that ctor params are leading, not properties.</param>
			public TestEntity(DateTimeOffset creationDateTime, DateOnly creationDate, TimeOnly? creationTime, DateTimeOffset modificationDateTime, ushort count, Money amount, string notAProperty)
				: base(new TestEntityId(Guid.NewGuid().ToString("N")))
			{
				this.CreationDateTime = creationDateTime.LocalDateTime;
				this.CreationDate = creationDate;
				this.CreationTime = creationTime ?? default;
				this.ModificationDateTime = modificationDateTime;
				this.Count = count;
				this.Amount = amount;

				Console.WriteLine(notAProperty);
			}

			[Obsolete("Just here to confirm that the generated source code is not invoking it.", error: true)]
			public TestEntity(DateTimeOffset creationDateTime, DateOnly creationDate, TimeOnly creationTime, DateTimeOffset modificationDateTime, ushort count, Money amount, string notAProperty, string moreComplexConstructor)
				: this(creationDateTime, creationDate, creationTime, modificationDateTime, count, amount, notAProperty)
			{
				throw new Exception($"The {nameof(moreComplexConstructor)} should not have been used: {moreComplexConstructor}.");
			}

			[Obsolete("Just here to confirm that the generated source code is not invoking it.", error: true)]
			public TestEntity()
				: base(new TestEntityId(Guid.NewGuid().ToString("N")))
			{
				throw new Exception($"The default constructor should not have been used.");
			}
		}

		[WrapperValueObject<decimal>]
		public sealed partial class Amount
		{
			// The type's simplest non-default constructor should be used by the builder. It is source-generated.

			[Obsolete("Just here to confirm that the generated source code is not invoking it.", error: true)]
			public Amount(decimal value, string moreComplexConstructor)
			{
				throw new Exception($"The {nameof(moreComplexConstructor)} should not have been used: {moreComplexConstructor}.");
			}
		}

		[ValueObject]
		public sealed partial class Money
		{
			public string Currency { get; private init; }
			public Amount Amount { get; private init; }

			/// <summary>
			/// The type's simplest non-default constructor should be used by the builder.
			/// </summary>
			public Money(string currency, Amount amount)
			{
				this.Currency = currency ?? throw new ArgumentNullException(nameof(currency));
				this.Amount = amount;
			}

			[Obsolete("Just here to confirm that the generated source code is not invoking it.", error: true)]
			public Money()
			{
				throw new Exception("The default constructor should not have been used.");
			}

			[Obsolete("Just here to confirm that the generated source code is not invoking it.", error: true)]
			public Money(string currency, Amount amount, string moreComplexConstructor)
				: this(currency, amount)
			{
				throw new Exception($"The {nameof(moreComplexConstructor)} should not have been used: {moreComplexConstructor}.");
			}
		}

		public sealed class EmptyType
		{
		}

		[Obsolete("Should merely compile.", error: true)]
		[DummyBuilder<EmptyType>]
		public sealed partial class EmptyTypeDummyBuilder
		{
		}

		[WrapperValueObject<string>]
		public sealed partial class StringWrapper : WrapperValueObject<string>
		{
			protected override StringComparison StringComparison => StringComparison.Ordinal;
		}

		public sealed class ManualStringWrapper : WrapperValueObject<string>
		{
			protected override StringComparison StringComparison => StringComparison.Ordinal;
			public override string ToString() => this.Value;

			public string Value { get; }

			public ManualStringWrapper(string value)
			{
				this.Value = value ?? throw new ArgumentNullException(nameof(value));
			}
		}

		public sealed partial class StringWrapperTestingEntity : Entity<int>
		{
			public StringWrapper FirstName { get; }
			public ManualStringWrapper LastName { get; }

			public StringWrapperTestingEntity(StringWrapper firstName, ManualStringWrapper lastName)
				: base(default)
			{
				this.FirstName = firstName;
				this.LastName = lastName;
			}
		}

		[DummyBuilder<StringWrapperTestingEntity>]
		public sealed partial class StringWrapperTestingDummyBuilder
		{
		}
	}
}
