using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.Serialization;
using Architect.DomainModeling.Conversions;
using Architect.DomainModeling.Tests.WrapperValueObjectTestTypes;
using Xunit;

namespace Architect.DomainModeling.Tests
{
	public class WrapperValueObjectTests
	{
		[Fact]
		public void StringComparison_WithNonStringType_ShouldThrow()
		{
			var instance = new IntValue(default);

			Assert.Throws<NotSupportedException>(() => instance.GetStringComparison());
		}

		[Fact]
		public void StringComparison_WithStringType_ShouldReturnExpectedResult()
		{
			var instance = new StringValue("");

			Assert.Equal(StringComparison.OrdinalIgnoreCase, instance.GetStringComparison());
		}

		[Fact]
		public void Construct_WithNull_ShouldThrow()
		{
			Assert.Throws<ArgumentNullException>(() => new StringValue(null!));
		}

		[Fact]
		public void ToString_Regularly_ShouldReturnExpectedResult()
		{
			Assert.Equal("1", new IntValue(1).ToString());
		}

		[Fact]
		public void GetHashCode_Regulary_ShouldReturnExpectedResult()
		{
			Assert.Equal(1.GetHashCode(), new IntValue(1).GetHashCode());
		}

		[Fact]
		public void GetHashCode_WithIgnoreCaseString_ShouldReturnExpectedResult()
		{
			var one = new StringValue("A").GetHashCode();
			var two = new StringValue("a").GetHashCode();
			Assert.Equal(one, two);
		}

		[Fact]
		public void GetHashCode_WithUnintializedObject_ShouldReturnExpectedResult()
		{
			var instance = (StringValue)FormatterServices.GetUninitializedObject(typeof(StringValue));
			Assert.Equal(0, instance.GetHashCode());
		}

		[Theory]
		[InlineData(null, null, true)] // Implementation should still handle null left operand as expected
		[InlineData(null, "", false)] // Custom collection's hash code always returns 1
		[InlineData("", null, false)] // Custom collection's hash code always returns 1
		[InlineData("A", "A", true)] // Custom collection's hash code always returns 1
		[InlineData("A", "B", true)] // Custom collection's hash code always returns 1
		public void GetHashCode_WithCustomEquatableCollection_ShouldHonorItsOverride(string one, string two, bool expectedResult)
		{
			var left = one is null
				? FormatterServices.GetUninitializedObject(typeof(CustomCollectionWrapperValueObject))
				: new CustomCollectionWrapperValueObject(new CustomCollectionWrapperValueObject.CustomCollection(one));
			var right = two is null
				? FormatterServices.GetUninitializedObject(typeof(CustomCollectionWrapperValueObject))
				: new CustomCollectionWrapperValueObject(new CustomCollectionWrapperValueObject.CustomCollection(two));

			var leftHashCode = left.GetHashCode();
			var rightHashCode = right.GetHashCode();

			Assert.Equal(expectedResult, leftHashCode == rightHashCode);
		}

		[Theory]
		[InlineData(0, 0, true)]
		[InlineData(0, 1, false)]
		[InlineData(1, -1, false)]
		public void Equals_Regularly_ShouldReturnExpectedResult(int one, int two, bool expectedResult)
		{
			var left = new IntValue(one);
			var right = new IntValue(two);
			Assert.Equal(expectedResult, left.Equals(right));
		}

		[Theory]
		[InlineData("", "", true)]
		[InlineData("A", "A", true)]
		[InlineData("A", "a", true)]
		[InlineData("A", "B", false)]
		public void Equals_WithIgnoreCaseString_ShouldReturnExpectedResult(string one, string two, bool expectedResult)
		{
			var left = new StringValue(one);
			var right = new StringValue(two);
			Assert.Equal(expectedResult, left.Equals(right));
		}

		[Fact]
		public void Equals_WithUnintializedObject_ShouldReturnExpectedResult()
		{
			var left = (StringValue)FormatterServices.GetUninitializedObject(typeof(StringValue));
			var right = new StringValue("Example");

			Assert.NotEqual(left, right);
			Assert.NotEqual(right, left);
		}

		[Theory]
		[InlineData(null, null, true)] // Implementation should still handle null left operand as expected
		[InlineData(null, "", false)] // Implementation should still handle null left operand as expected
		[InlineData("", null, true)] // Custom collection's equality always returns true
		[InlineData("A", "A", true)] // Custom collection's equality always returns true
		[InlineData("A", "B", true)] // Custom collection's equality always returns true
		public void Equals_WithCustomEquatableCollection_ShouldHonorItsOverride(string one, string two, bool expectedResult)
		{
			var left = one is null
				? FormatterServices.GetUninitializedObject(typeof(CustomCollectionWrapperValueObject))
				: new CustomCollectionWrapperValueObject(new CustomCollectionWrapperValueObject.CustomCollection(one));
			var right = two is null
				? FormatterServices.GetUninitializedObject(typeof(CustomCollectionWrapperValueObject))
				: new CustomCollectionWrapperValueObject(new CustomCollectionWrapperValueObject.CustomCollection(two));
			Assert.Equal(expectedResult, left.Equals(right));
		}

		[Theory]
		[InlineData(0, 0)]
		[InlineData(0, 1)]
		[InlineData(1, -1)]
		public void EqualityOperator_Regularly_ShouldMatchEquals(int one, int two)
		{
			var left = new IntValue(one);
			var right = new IntValue(two);
			Assert.Equal(left.Equals(right), left == right);
		}

		[Theory]
		[InlineData("", "")]
		[InlineData("A", "A")]
		[InlineData("A", "a")]
		[InlineData("A", "B")]
		public void EqualityOperator_WithIgnoreCaseString_ShouldMatchEquals(string one, string two)
		{
			var left = new StringValue(one);
			var right = new StringValue(two);
			Assert.Equal(left.Equals(right), left == right);
		}

		[Theory]
		[InlineData("", "")]
		[InlineData("A", "A")]
		[InlineData("A", "a")]
		[InlineData("A", "B")]
		public void CompareTo_WithEqualValuesAndIgnoreCaseString_ShouldHaveEqualityMatchingEquals(string one, string two)
		{
			var left = new StringValue(one);
			var right = new StringValue(two);
			Assert.Equal(left.Equals(right), left.CompareTo(right) == 0);
		}

		[Fact]
		public void CompareTo_WithoutExplicitInterface_ShouldNotBeImplemented()
		{
			var array = new[] { new IntValue(1), new IntValue(2) };

			Assert.Throws<InvalidOperationException>(() => Array.Sort(array));
		}

		[Fact]
		public void CompareTo_WithExplicitInterface_ShouldBeImplementedCorrectly()
		{
			var array = new[] { new StringValue("a"), new StringValue("A"), new StringValue("0"), };

			array = array.OrderBy(x => x).ToArray(); // Stable sort

			Assert.Equal("0", array[0].Value);
			Assert.Equal("a", array[1].Value); // Stable sort combined with ignore-case should have kept "a" before "A"
			Assert.Equal("A", array[2].Value);
		}

		[Theory]
		[InlineData(null, null, 0)]
		[InlineData(null, "", -1)]
		[InlineData("", null, +1)]
		[InlineData("", "", 0)]
		[InlineData("", "A", -1)]
		[InlineData("A", "", +1)]
		[InlineData("A", "a", 0)]
		[InlineData("a", "A", 0)]
		[InlineData("A", "B", -1)]
		[InlineData("AA", "A", +1)]
		public void CompareTo_WithIgnoreCaseString_ShouldReturnExpectedResult(string one, string two, int expectedResult)
		{
			var left = (StringValue)one;
			var right = (StringValue)two;

			Assert.Equal(expectedResult, Comparer<StringValue>.Default.Compare(left, right));
			Assert.Equal(-expectedResult, Comparer<StringValue>.Default.Compare(right, left));
		}

		[Theory]
		[InlineData(null, null, 0)]
		[InlineData(null, "", -1)]
		[InlineData("", null, +1)]
		[InlineData("", "", 0)]
		[InlineData("", "A", -1)]
		[InlineData("A", "", +1)]
		[InlineData("A", "a", 0)]
		[InlineData("a", "A", 0)]
		[InlineData("A", "B", -1)]
		[InlineData("AA", "A", +1)]
		public void GreaterThan_WithIgnoreCaseString_ShouldReturnExpectedResult(string one, string two, int expectedResult)
		{
			var left = (StringValue)one;
			var right = (StringValue)two;

			Assert.Equal(expectedResult > 0, left > right);
			Assert.Equal(expectedResult <= 0, left <= right);
		}

		[Theory]
		[InlineData(null, null, 0)]
		[InlineData(null, "", -1)]
		[InlineData("", null, +1)]
		[InlineData("", "", 0)]
		[InlineData("", "A", -1)]
		[InlineData("A", "", +1)]
		[InlineData("A", "a", 0)]
		[InlineData("a", "A", 0)]
		[InlineData("A", "B", -1)]
		[InlineData("AA", "A", +1)]
		public void LessThan_WithIgnoreCaseString_ShouldReturnExpectedResult(string one, string two, int expectedResult)
		{
			var left = (StringValue)one;
			var right = (StringValue)two;

			Assert.Equal(expectedResult < 0, left < right);
			Assert.Equal(expectedResult >= 0, left >= right);
		}

		[Theory]
		[InlineData(null, null)]
		[InlineData(0, 0)]
		[InlineData(1, 1)]
		public void CastToUnderlyingType_Regularly_ShouldReturnExpectedResult(int? value, int? expectedResult)
		{
			var instance = value is null ? null : new IntValue(value.Value);

			if (expectedResult is null)
				Assert.Throws<NullReferenceException>(() => (int)instance!);
			else
				Assert.Equal(expectedResult, (int)instance!);
		}

		[Theory]
		[InlineData(null, null)]
		[InlineData(0, 0)]
		[InlineData(1, 1)]
		public void CastToNullableUnderlyingType_Regularly_ShouldReturnExpectedResult(int? value, int? expectedResult)
		{
			var instance = value is null ? null : new IntValue(value.Value);

			var result = (int?)instance;

			Assert.Equal(expectedResult, result);
		}

		[Theory]
		[InlineData(0, 0)]
		[InlineData(1, 1)]
		public void CastFromUnderlyingType_Regularly_ShouldReturnExpectedResult(int value, int expectedResult)
		{
			Assert.Equal(new IntValue(expectedResult), (IntValue)value);
		}

		[Theory]
		[InlineData(null, null)]
		[InlineData(0, 0)]
		[InlineData(1, 1)]
		public void CastFromNullableUnderlyingType_Regularly_ShouldReturnExpectedResult(int? value, int? expectedResult)
		{
			var result = (IntValue?)value;

			Assert.Equal(expectedResult, result?.Value);
		}

		[Theory]
		[InlineData(null)]
		[InlineData(0)]
		[InlineData(1)]
		public void SerializeWithSystemTextJson_Regularly_ShouldReturnExpectedResult(int? value)
		{
			var intInstance = (IntValue?)value;
			Assert.Equal(value?.ToString() ?? "null", System.Text.Json.JsonSerializer.Serialize(intInstance));

			var stringInstance = (StringValue?)value?.ToString();
			Assert.Equal(value is null ? "null" : $@"""{value}""", System.Text.Json.JsonSerializer.Serialize(stringInstance));
		}

		[Theory]
		[InlineData(null)]
		[InlineData(0)]
		[InlineData(1)]
		public void SerializeWithNewtonsoftJson_Regularly_ShouldReturnExpectedResult(int? value)
		{
			var intInstance = (IntValue?)value;
			Assert.Equal(value?.ToString() ?? "null", Newtonsoft.Json.JsonConvert.SerializeObject(intInstance));

			var stringInstance = (StringValue?)value?.ToString();
			Assert.Equal(value is null ? "null" : $@"""{value}""", Newtonsoft.Json.JsonConvert.SerializeObject(stringInstance));
		}

		/// <summary>
		/// ValueObjects should (de)serialize decimals normally, which is the most expected behavior.
		/// Some special-casing exists only for generated identities.
		/// </summary>
		[Theory]
		[InlineData(null)]
		[InlineData(0)]
		[InlineData(1)]
		public void SerializeWithSystemTextJson_WithDecimal_ShouldReturnExpectedResult(int? value)
		{
			// Attempt to mess with the stringification, which should have no effect
			CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");

			var instance = (DecimalValue?)value;

			Assert.Equal(value?.ToString() ?? "null", System.Text.Json.JsonSerializer.Serialize(instance));
		}

		/// <summary>
		/// ValueObjects should (de)serialize decimals normally, which is the most expected behavior.
		/// Some special-casing exists only for generated identities.
		/// </summary>
		[Theory]
		[InlineData(null)]
		[InlineData(0)]
		[InlineData(1)]
		public void SerializeWithNewtonsoftJson_WithDecimal_ShouldReturnExpectedResult(int? value)
		{
			// Attempt to mess with the stringification, which should have no effect
			CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("nl-NL");

			var instance = (DecimalValue?)value;

			// Newtonsoft appends ".0" for some reason
			Assert.Equal(value is null ? "null" : $"{value}.0", Newtonsoft.Json.JsonConvert.SerializeObject(instance));
		}

		[Theory]
		[InlineData("null", null)]
		[InlineData("0", 0)]
		[InlineData("1", 1)]
		public void DeserializeWithSystemTextJson_Regularly_ShouldReturnExpectedResult(string json, int? value)
		{
			Assert.Equal(value, System.Text.Json.JsonSerializer.Deserialize<IntValue>(json)?.Value);

			json = json == "null" ? json : $@"""{json}""";
			Assert.Equal(value?.ToString(), System.Text.Json.JsonSerializer.Deserialize<StringValue>(json)?.Value);
		}

		[Theory]
		[InlineData("null", null)]
		[InlineData("0", 0)]
		[InlineData("1", 1)]
		public void DeserializeWithNewtonsoftJson_Regularly_ShouldReturnExpectedResult(string json, int? value)
		{
			Assert.Equal(value, Newtonsoft.Json.JsonConvert.DeserializeObject<IntValue>(json)?.Value);

			json = json == "null" ? json : $@"""{json}""";
			Assert.Equal(value?.ToString(), Newtonsoft.Json.JsonConvert.DeserializeObject<StringValue>(json)?.Value);
		}

		/// <summary>
		/// ValueObjects should (de)serialize decimals normally, which is the most expected behavior.
		/// Some special-casing exists only for generated identities.
		/// </summary>
		[Theory]
		[InlineData("null", null)]
		[InlineData("0", 0)]
		[InlineData("1", 1)]
		public void DeserializeWithSystemTextJson_WithDecimal_ShouldReturnExpectedResult(string json, int? value)
		{
			// Attempt to mess with the deserialization, which should have no effect
			CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("nl-NL");

			Assert.Equal(value, System.Text.Json.JsonSerializer.Deserialize<DecimalValue>(json)?.Value);
		}

		/// <summary>
		/// ValueObjects should (de)serialize decimals normally, which is the most expected behavior.
		/// Some special-casing exists only for generated identities.
		/// </summary>
		[Theory]
		[InlineData("null", null)]
		[InlineData("0", 0)]
		[InlineData("1", 1)]
		public void DeserializeWithNewtonsoftJson_WithDecimal_ShouldReturnExpectedResult(string json, int? value)
		{
			// Attempt to mess with the deserialization, which should have no effect
			CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");

			Assert.Equal(value, Newtonsoft.Json.JsonConvert.DeserializeObject<DecimalValue>(json)?.Value);
		}

		[Theory]
		[InlineData("0", 0)]
		[InlineData("1", 1)]
		public void ReadAsPropertyNameWithSystemTextJson_Regularly_ShouldReturnExpectedResult(string json, int value)
		{
			json = $$"""{ "{{json}}": true }""";

			Assert.Equal(KeyValuePair.Create((IntValue)value, true), System.Text.Json.JsonSerializer.Deserialize<Dictionary<IntValue, bool>>(json)?.Single());

			Assert.Equal(KeyValuePair.Create((StringValue)value.ToString(), true), System.Text.Json.JsonSerializer.Deserialize<Dictionary<StringValue, bool>>(json)?.Single());

			Assert.Equal(KeyValuePair.Create((DecimalValue)value, true), System.Text.Json.JsonSerializer.Deserialize<Dictionary<DecimalValue, bool>>(json)?.Single());
		}

		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		public void WriteAsPropertyNameWithSystemTextJson_Regularly_ShouldReturnExpectedResult(int value)
		{
			var expectedResult = $$"""{"{{value}}":true}""";

			Assert.Equal(expectedResult, System.Text.Json.JsonSerializer.Serialize(new Dictionary<IntValue, bool>() { [(IntValue)value] = true }));

			Assert.Equal(expectedResult, System.Text.Json.JsonSerializer.Serialize(new Dictionary<StringValue, bool>() { [(StringValue)value.ToString()] = true }));

			Assert.Equal(expectedResult, System.Text.Json.JsonSerializer.Serialize(new Dictionary<DecimalValue, bool>() { [(DecimalValue)value] = true }));
		}
	}

	// Use a namespace, since our source generators dislike nested types
	namespace WrapperValueObjectTestTypes
	{
		// Should compile in spite of already consisting of two partials
		[SourceGenerated]
		public sealed partial class AlreadyPartial : WrapperValueObject<int>
		{
		}

		// Should compile in spite of already consisting of two partials
		public sealed partial class AlreadyPartial : WrapperValueObject<int>
		{
		}

		// Should be recognized in spite of the SourceGeneratedAttribute and the base class to be defined on different partials
		[SourceGenerated]
		public sealed partial class OtherAlreadyPartial
		{
		}

		// Should be recognized in spite of the SourceGeneratedAttribute and the base class to be defined on different partials
		public sealed partial class OtherAlreadyPartial : WrapperValueObject<int>
		{
		}

		[SourceGenerated]
		public sealed partial class WrapperValueObjectWithIIdentity : WrapperValueObject<int>, IIdentity<int>
		{
		}

		[SourceGenerated]
		public sealed partial class IntValue : WrapperValueObject<int>
		{
			public StringComparison GetStringComparison() => this.StringComparison;
		}

		[SourceGenerated]
		public sealed partial class StringValue : WrapperValueObject<string>, IComparable<StringValue>
		{
			protected override StringComparison StringComparison => StringComparison.OrdinalIgnoreCase;

			public StringComparison GetStringComparison() => this.StringComparison;
		}

		[SourceGenerated]
		public sealed partial class DecimalValue : WrapperValueObject<decimal>
		{
		}

		[SourceGenerated]
		public sealed partial class CustomCollectionWrapperValueObject : WrapperValueObject<CustomCollectionWrapperValueObject.CustomCollection>
		{
			public class CustomCollection : IReadOnlyCollection<int>
			{
				public override int GetHashCode() => 1;
				public override bool Equals(object? other) => true;
				public int Count => throw new NotSupportedException();
				public IEnumerator<int> GetEnumerator() => throw new NotSupportedException();
				IEnumerator IEnumerable.GetEnumerator() => throw new NotSupportedException();

				public string Value { get; }

				public CustomCollection(string value)
				{
					this.Value = value ?? throw new ArgumentNullException(nameof(value));
				}
			}
		}

		[SourceGenerated]
		[Obsolete("Should merely compile.", error: true)]
		public sealed partial class StringArrayValue : WrapperValueObject<string?[]>
		{
		}

		[SourceGenerated]
		[Obsolete("Should merely compile.", error: true)]
		public sealed partial class DecimalArrayValue : WrapperValueObject<decimal?[]>
		{
		}

		/// <summary>
		/// Should merely compile.
		/// </summary>
		[Obsolete("Should merely compile.", error: true)]
		[SourceGenerated]
		[System.Text.Json.Serialization.JsonConverter(typeof(JsonConverter))]
		[Newtonsoft.Json.JsonConverter(typeof(NewtonsoftJsonConverter))]
		internal sealed partial class FullySelfImplementedWrapperValueObject : WrapperValueObject<int>, IComparable<FullySelfImplementedWrapperValueObject>
		{
			protected sealed override StringComparison StringComparison => throw new NotSupportedException("This operation applies to string-based value objects only.");

			public int Value { get; }

			public FullySelfImplementedWrapperValueObject(int value)
			{
				this.Value = value;
			}

			public sealed override string ToString()
			{
				return this.Value.ToString();
			}

			public sealed override int GetHashCode()
			{
				// Null-safety protects instances from FormatterServices.GetUninitializedObject()
				return this.Value.GetHashCode();
			}

			public sealed override bool Equals(object? other)
			{
				return other is FullySelfImplementedWrapperValueObject otherValue && this.Equals(otherValue);
			}

			public bool Equals(FullySelfImplementedWrapperValueObject? other)
			{
				return other is not null && this.Value.Equals(other.Value);
			}

			public int CompareTo(FullySelfImplementedWrapperValueObject? other)
			{
				return other is null
					? +1
					: this.Value.CompareTo(other.Value);
			}

			public static bool operator ==(FullySelfImplementedWrapperValueObject? left, FullySelfImplementedWrapperValueObject? right) => left is null ? right is null : left.Equals(right);
			public static bool operator !=(FullySelfImplementedWrapperValueObject? left, FullySelfImplementedWrapperValueObject? right) => !(left == right);

			public static bool operator >(FullySelfImplementedWrapperValueObject? left, FullySelfImplementedWrapperValueObject? right) => left is not null && left.CompareTo(right) > 0;
			public static bool operator <(FullySelfImplementedWrapperValueObject? left, FullySelfImplementedWrapperValueObject? right) => left is null ? right is not null : left.CompareTo(right) < 0;
			public static bool operator >=(FullySelfImplementedWrapperValueObject? left, FullySelfImplementedWrapperValueObject? right) => !(left < right);
			public static bool operator <=(FullySelfImplementedWrapperValueObject? left, FullySelfImplementedWrapperValueObject? right) => !(left > right);

			public static explicit operator FullySelfImplementedWrapperValueObject(int value) => new FullySelfImplementedWrapperValueObject(value);
			public static implicit operator int(FullySelfImplementedWrapperValueObject instance) => instance.Value;

			[return: NotNullIfNotNull(nameof(value))]
			public static explicit operator FullySelfImplementedWrapperValueObject?(int? value) => value is null ? null : new FullySelfImplementedWrapperValueObject(value.Value);
			[return: NotNullIfNotNull(nameof(instance))]
			public static implicit operator int?(FullySelfImplementedWrapperValueObject? instance) => instance?.Value;

			private sealed class JsonConverter : System.Text.Json.Serialization.JsonConverter<FullySelfImplementedWrapperValueObject>
			{
				public override FullySelfImplementedWrapperValueObject Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
				{
					return (FullySelfImplementedWrapperValueObject)System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options);
				}

				public override void Write(System.Text.Json.Utf8JsonWriter writer, FullySelfImplementedWrapperValueObject value, System.Text.Json.JsonSerializerOptions options)
				{
					System.Text.Json.JsonSerializer.Serialize(writer, value.Value, options);
				}

#if NET7_0_OR_GREATER
				public override FullySelfImplementedWrapperValueObject ReadAsPropertyName(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
				{
					return (FullySelfImplementedWrapperValueObject)reader.GetParsedString<int>(CultureInfo.InvariantCulture);
				}

				public override void WriteAsPropertyName(System.Text.Json.Utf8JsonWriter writer, FullySelfImplementedWrapperValueObject value, System.Text.Json.JsonSerializerOptions options)
				{
					writer.WritePropertyName(value.Value.Format(stackalloc char[64], default, CultureInfo.InvariantCulture));
				}
#endif
			}

			private sealed class NewtonsoftJsonConverter : Newtonsoft.Json.JsonConverter
			{
				public override bool CanConvert(Type objectType)
				{
					return objectType == typeof(FullySelfImplementedWrapperValueObject);
				}

				public override void WriteJson(Newtonsoft.Json.JsonWriter writer, object? value, Newtonsoft.Json.JsonSerializer serializer)
				{
					if (value is null)
						serializer.Serialize(writer, null);
					else
						serializer.Serialize(writer, ((FullySelfImplementedWrapperValueObject)value).Value);
				}

				public override object? ReadJson(Newtonsoft.Json.JsonReader reader, Type objectType, object? existingValue, Newtonsoft.Json.JsonSerializer serializer)
				{
					return (FullySelfImplementedWrapperValueObject?)serializer.Deserialize<int?>(reader);
				}
			}
		}
	}
}
