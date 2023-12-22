using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
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

			// Even with nested identity and/or wrapper value objects, no constructors should be hit
			var nestedInstance = value is null ? null : new JsonTestingStringWrapper(value.ToString()!, false);
			Assert.Equal(value is null ? "null" : $@"""{value}""", System.Text.Json.JsonSerializer.Serialize(nestedInstance));
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

			// Even with nested identity and/or wrapper value objects, no constructors should be hit
			var nestedInstance = value is null ? null : new JsonTestingStringWrapper(value.ToString()!, false);
			Assert.Equal(value is null ? "null" : $@"""{value}""", Newtonsoft.Json.JsonConvert.SerializeObject(nestedInstance));
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

			// Even with nested identity and/or wrapper value objects, no constructors should be hit
			Assert.Equal(value?.ToString(), json == "null" ? null : System.Text.Json.JsonSerializer.Deserialize<JsonTestingNestedStringWrapper>(json)?.Value.Value?.Value);
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

			// Even with nested identity and/or wrapper value objects, no constructors should be hit
			Assert.Equal(value?.ToString(), json == "null" ? null : Newtonsoft.Json.JsonConvert.DeserializeObject<JsonTestingNestedStringWrapper>(json)?.Value.Value?.Value);
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

			// Even with nested identity and/or wrapper value objects, no constructors should be hit
			Assert.Equal(KeyValuePair.Create(new JsonTestingStringWrapper(value.ToString(), false), true), System.Text.Json.JsonSerializer.Deserialize<Dictionary<JsonTestingStringWrapper, bool>>(json)?.Single());
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

			// Even with nested identity and/or wrapper value objects, no constructors should be hit
			Assert.Equal(expectedResult, System.Text.Json.JsonSerializer.Serialize(new Dictionary<JsonTestingStringWrapper, bool>() { [new JsonTestingStringWrapper(value.ToString(), false)] = true }));
		}

		[Fact]
		public void FormattableToString_InAllScenarios_ShouldReturnExpectedResult()
		{
			Assert.Equal("5", new IntValue(5).ToString(format: null, formatProvider: null));
			Assert.Equal("5", new StringValue("5").ToString(format: null, formatProvider: null));
			Assert.Equal("5", new FullySelfImplementedWrapperValueObject(5).ToString(format: null, formatProvider: null));
			Assert.Equal("5", new FormatAndParseTestingStringWrapper("5").ToString(format: null, formatProvider: null));

			Assert.Null(((StringValue)RuntimeHelpers.GetUninitializedObject(typeof(StringValue))).ToString(format: null, formatProvider: null));
		}

		[Fact]
		public void SpanFormattableTryFormat_InAllScenarios_ShouldReturnExpectedResult()
		{
			Span<char> result = stackalloc char[1];

			Assert.True(new IntValue(5).TryFormat(result, out var charsWritten, format: null, provider: null));
			Assert.Equal(1, charsWritten);
			Assert.Equal("5".AsSpan(), result);

			Assert.True(new StringValue("5").TryFormat(result, out charsWritten, format: null, provider: null));
			Assert.Equal(1, charsWritten);
			Assert.Equal("5".AsSpan(), result);

			Assert.True(new FullySelfImplementedWrapperValueObject(5).TryFormat(result, out charsWritten, format: null, provider: null));
			Assert.Equal(1, charsWritten);
			Assert.Equal("5".AsSpan(), result);

			Assert.True(new FormatAndParseTestingStringWrapper("5").TryFormat(result, out charsWritten, format: null, provider: null));
			Assert.Equal(1, charsWritten);
			Assert.Equal("5".AsSpan(), result);

			Assert.True(((StringValue)RuntimeHelpers.GetUninitializedObject(typeof(StringValue))).TryFormat(result, out charsWritten, format: null, provider: null));
			Assert.Equal(0, charsWritten);
		}

		[Fact]
		public void UtfSpanFormattableTryFormat_InAllScenarios_ShouldReturnExpectedResult()
		{
			Span<byte> result = stackalloc byte[1];

			Assert.True(new IntValue(5).TryFormat(result, out var bytesWritten, format: null, provider: null));
			Assert.Equal(1, bytesWritten);
			Assert.Equal("5"u8, result);

			Assert.True(new StringValue("5").TryFormat(result, out bytesWritten, format: null, provider: null));
			Assert.Equal(1, bytesWritten);
			Assert.Equal("5"u8, result);

			Assert.True(new FullySelfImplementedWrapperValueObject(5).TryFormat(result, out bytesWritten, format: null, provider: null));
			Assert.Equal(1, bytesWritten);
			Assert.Equal("5"u8, result);

			Assert.True(new FormatAndParseTestingStringWrapper("5").TryFormat(result, out bytesWritten, format: null, provider: null));
			Assert.Equal(1, bytesWritten);
			Assert.Equal("5"u8, result);

			Assert.True(((StringValue)RuntimeHelpers.GetUninitializedObject(typeof(StringValue))).TryFormat(result, out bytesWritten, format: null, provider: null));
			Assert.Equal(0, bytesWritten);
		}

		[Fact]
		public void ParsableTryParseAndParse_InAllScenarios_ShouldReturnExpectedResult()
		{
			var input = "5";

			Assert.True(IntValue.TryParse(input, provider: null, out var result1));
			Assert.Equal(5, result1.Value);
			Assert.Equal(result1, IntValue.Parse(input, provider: null));

			Assert.True(StringValue.TryParse(input, provider: null, out var result2));
			Assert.Equal("5", result2.Value);
			Assert.Equal(result2, StringValue.Parse(input, provider: null));

			Assert.True(FullySelfImplementedWrapperValueObject.TryParse(input, provider: null, out var result3));
			Assert.Equal(5, result3.Value);
			Assert.Equal(result3, FullySelfImplementedWrapperValueObject.Parse(input, provider: null));

			Assert.True(FormatAndParseTestingStringWrapper.TryParse(input, provider: null, out var result4));
			Assert.Equal("5", result4.Value?.Value.Value?.Value);
			Assert.Equal(result4, FormatAndParseTestingStringWrapper.Parse(input, provider: null));
		}

		[Fact]
		public void SpanParsableTryParseAndParse_InAllScenarios_ShouldReturnExpectedResult()
		{
			var input = "5".AsSpan();

			Assert.True(IntValue.TryParse(input, provider: null, out var result1));
			Assert.Equal(5, result1.Value);
			Assert.Equal(result1, IntValue.Parse(input, provider: null));

			Assert.True(StringValue.TryParse(input, provider: null, out var result2));
			Assert.Equal("5", result2.Value);
			Assert.Equal(result2, StringValue.Parse(input, provider: null));

			Assert.True(FullySelfImplementedWrapperValueObject.TryParse(input, provider: null, out var result3));
			Assert.Equal(5, result3.Value);
			Assert.Equal(result3, FullySelfImplementedWrapperValueObject.Parse(input, provider: null));

			Assert.True(FormatAndParseTestingStringWrapper.TryParse(input, provider: null, out var result4));
			Assert.Equal("5", result4.Value?.Value.Value?.Value);
			Assert.Equal(result4, FormatAndParseTestingStringWrapper.Parse(input, provider: null));
		}

		[Fact]
		public void Utf8SpanParsableTryParseAndParse_InAllScenarios_ShouldReturnExpectedResult()
		{
			var input = "5"u8;

			Assert.True(IntValue.TryParse(input, provider: null, out var result1));
			Assert.Equal(5, result1.Value);
			Assert.Equal(result1, IntValue.Parse(input, provider: null));

			Assert.True(StringValue.TryParse(input, provider: null, out var result2));
			Assert.Equal("5", result2.Value);
			Assert.Equal(result2, StringValue.Parse(input, provider: null));

			Assert.True(FullySelfImplementedWrapperValueObject.TryParse(input, provider: null, out var result3));
			Assert.Equal(5, result3.Value);
			Assert.Equal(result3, FullySelfImplementedWrapperValueObject.Parse(input, provider: null));

			Assert.True(FormatAndParseTestingStringWrapper.TryParse(input, provider: null, out var result4));
			Assert.Equal("5", result4.Value?.Value.Value?.Value);
			Assert.Equal(result4, FormatAndParseTestingStringWrapper.Parse(input, provider: null));
		}
	}

	// Use a namespace, since our source generators dislike nested types
	namespace WrapperValueObjectTestTypes
	{
		// Should compile in spite of already consisting of multiple partials, both with and without the attribute
		[WrapperValueObject<int>]
		public sealed partial class AlreadyPartial
		{
		}

		// Should compile in spite of already consisting of multiple partials, both with and without the attribute
		public sealed partial class AlreadyPartial : WrapperValueObject<int>
		{
		}

		// Should compile in spite of already consisting of multiple partials, both with and without the attribute
		public partial class AlreadyPartial
		{
		}

		// Should be recognized in spite of the attribute and the base class to be defined on different partials
		[WrapperValueObject<int>]
		public sealed partial class OtherAlreadyPartial
		{
		}

		// Should be recognized in spite of the attribute and the base class to be defined on different partials
		public sealed partial class OtherAlreadyPartial : WrapperValueObject<int>
		{
		}

		[WrapperValueObject<int>]
		public sealed partial class WrapperValueObjectWithIIdentity : IIdentity<int>
		{
		}

		[WrapperValueObject<int>]
		public sealed partial class IntValue : WrapperValueObject<int>
		{
			public StringComparison GetStringComparison() => this.StringComparison;

			public int Value { get; private init; }
		}

		[WrapperValueObject<string>]
		public sealed partial class StringValue : IComparable<StringValue>
		{
			protected override StringComparison StringComparison => StringComparison.OrdinalIgnoreCase;

			public StringComparison GetStringComparison() => this.StringComparison;
		}

		[WrapperValueObject<decimal>]
		public sealed partial class DecimalValue
		{
		}

		[WrapperValueObject<CustomCollection>]
		public sealed partial class CustomCollectionWrapperValueObject
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

		/// <summary>
		/// Should merely compile.
		/// </summary>
		[WrapperValueObject<string[]>]
		public sealed partial class StringArrayValue
		{
		}

		/// <summary>
		/// Should merely compile.
		/// </summary>
		[WrapperValueObject<decimal?[]>]
		public sealed partial class DecimalArrayValue : WrapperValueObject<decimal?[]>
		{
		}

		[WrapperValueObject<FormatAndParseTestingNestedStringWrapper>]
		internal partial class FormatAndParseTestingStringWrapper
		{
			public FormatAndParseTestingStringWrapper(string value)
			{
				this.Value = new FormatAndParseTestingNestedStringWrapper(new FormatAndParseTestingStringId(new StringValue(value)));
			}
		}
		[WrapperValueObject<FormatAndParseTestingStringId>]
		internal partial class FormatAndParseTestingNestedStringWrapper
		{
		}
		[IdentityValueObject<StringValue>]
		internal partial struct FormatAndParseTestingStringId : IComparable<FormatAndParseTestingStringId>
		{
		}

		[WrapperValueObject<JsonTestingNestedStringWrapper>]
		internal partial class JsonTestingStringWrapper
		{
			public JsonTestingStringWrapper(JsonTestingNestedStringWrapper _)
			{
				throw new Exception("This constructor should not be used. This lets tests confirm that concerns such as deserialization correctly avoid constructors.");
			}
			public JsonTestingStringWrapper(string value, bool _)
			{
				this.Value = new JsonTestingNestedStringWrapper(value, false);
			}
			public string ToString(string? format, IFormatProvider? formatProvider)
			{
				throw new Exception("Serialization should have delegated to the wrapped value.");
			}
			public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
			{
				throw new Exception("Serialization should have delegated to the wrapped value.");
			}
			public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
			{
				throw new Exception("Serialization should have delegated to the wrapped value.");
			}
		}
		[WrapperValueObject<JsonTestingStringId>]
		internal partial class JsonTestingNestedStringWrapper
		{
			public JsonTestingNestedStringWrapper(JsonTestingStringId _)
			{
				throw new Exception("This constructor should not be used. This lets tests confirm that concerns such as deserialization correctly avoid constructors.");
			}
			public JsonTestingNestedStringWrapper(string value, bool _)
			{
				this.Value = new JsonTestingStringId(value, false);
			}
			public string ToString(string? format, IFormatProvider? formatProvider)
			{
				throw new Exception("Serialization should have delegated to the wrapped value.");
			}
			public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
			{
				throw new Exception("Serialization should have delegated to the wrapped value.");
			}
			public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
			{
				throw new Exception("Serialization should have delegated to the wrapped value.");
			}
		}
		[IdentityValueObject<StringValue>]
		internal partial struct JsonTestingStringId : IComparable<JsonTestingStringId>
		{
			public JsonTestingStringId(StringValue? _)
			{
				throw new Exception("This constructor should not be used. This lets tests confirm that concerns such as deserialization correctly avoid constructors.");
			}
			public JsonTestingStringId(string value, bool _)
			{
				this.Value = new StringValue(value);
			}
			public string ToString(string? format, IFormatProvider? formatProvider)
			{
				throw new Exception("Serialization should have delegated to the wrapped value.");
			}
			public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
			{
				throw new Exception("Serialization should have delegated to the wrapped value.");
			}
			public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider)
			{
				throw new Exception("Serialization should have delegated to the wrapped value.");
			}
		}

		/// <summary>
		/// Should merely compile.
		/// </summary>
		[WrapperValueObject<int>]
		[System.Text.Json.Serialization.JsonConverter(typeof(JsonConverter))]
		[Newtonsoft.Json.JsonConverter(typeof(NewtonsoftJsonConverter))]
		internal sealed partial class FullySelfImplementedWrapperValueObject
			: WrapperValueObject<int>,
			IComparable<FullySelfImplementedWrapperValueObject>,
#if NET7_0_OR_GREATER
			ISpanFormattable,
			ISpanParsable<FullySelfImplementedWrapperValueObject>,
#endif
#if NET8_0_OR_GREATER
			IUtf8SpanFormattable,
			IUtf8SpanParsable<FullySelfImplementedWrapperValueObject>,
#endif
			ISerializableDomainObject<FullySelfImplementedWrapperValueObject, int>
		{
			protected sealed override StringComparison StringComparison => throw new NotSupportedException("This operation applies to string-based value objects only.");

			public int Value { get; private init; }

			public FullySelfImplementedWrapperValueObject(int value)
			{
				this.Value = value;
			}

			[Obsolete("This constructor exists for deserialization purposes only.")]
			private FullySelfImplementedWrapperValueObject()
			{
			}

			public sealed override int GetHashCode()
			{
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

			public sealed override string ToString()
			{
				return this.Value.ToString();
			}

			/// <summary>
			/// Serializes a domain object as a plain value.
			/// </summary>
			int ISerializableDomainObject<FullySelfImplementedWrapperValueObject, int>.Serialize()
			{
				return this.Value;
			}

			/// <summary>
			/// Deserializes a plain value back into a domain object without any validation.
			/// </summary>
			static FullySelfImplementedWrapperValueObject ISerializableDomainObject<FullySelfImplementedWrapperValueObject, int>.Deserialize(int value)
			{
#pragma warning disable CS0618 // Obsolete constructor is intended for us
				return new FullySelfImplementedWrapperValueObject() { Value = value };
#pragma warning restore CS0618
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

			#region Formatting & Parsing

#if NET7_0_OR_GREATER

			public string ToString(string? format, IFormatProvider? formatProvider) =>
				FormattingHelper.ToString(this.Value, format, formatProvider);

			public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
				FormattingHelper.TryFormat(this.Value, destination, out charsWritten, format, provider);

			public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out FullySelfImplementedWrapperValueObject result) =>
				ParsingHelper.TryParse(s, provider, out int value)
					? (result = (FullySelfImplementedWrapperValueObject)value) is var _
					: !((result = default) is var _);

			public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out FullySelfImplementedWrapperValueObject result) =>
				ParsingHelper.TryParse(s, provider, out int value)
					? (result = (FullySelfImplementedWrapperValueObject)value) is var _
					: !((result = default) is var _);

			public static FullySelfImplementedWrapperValueObject Parse(string s, IFormatProvider? provider) =>
				(FullySelfImplementedWrapperValueObject)ParsingHelper.Parse<int>(s, provider);

			public static FullySelfImplementedWrapperValueObject Parse(ReadOnlySpan<char> s, IFormatProvider? provider) =>
				(FullySelfImplementedWrapperValueObject)ParsingHelper.Parse<int>(s, provider);

#endif

#if NET8_0_OR_GREATER

			public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
				FormattingHelper.TryFormat(this.Value, utf8Destination, out bytesWritten, format, provider);

			public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, [MaybeNullWhen(false)] out FullySelfImplementedWrapperValueObject result) =>
				ParsingHelper.TryParse(utf8Text, provider, out int value)
					? (result = (FullySelfImplementedWrapperValueObject)value) is var _
					: !((result = default) is var _);

			public static FullySelfImplementedWrapperValueObject Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider) =>
				(FullySelfImplementedWrapperValueObject)ParsingHelper.Parse<int>(utf8Text, provider);

#endif

			#endregion

			private sealed class JsonConverter : System.Text.Json.Serialization.JsonConverter<FullySelfImplementedWrapperValueObject>
			{
				public override FullySelfImplementedWrapperValueObject Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options) =>
					DomainObjectSerializer.Deserialize<FullySelfImplementedWrapperValueObject, int>(System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!);

				public override void Write(System.Text.Json.Utf8JsonWriter writer, FullySelfImplementedWrapperValueObject value, System.Text.Json.JsonSerializerOptions options) =>
					System.Text.Json.JsonSerializer.Serialize(writer, DomainObjectSerializer.Serialize<FullySelfImplementedWrapperValueObject, int>(value), options);

				public override FullySelfImplementedWrapperValueObject ReadAsPropertyName(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options) =>
					DomainObjectSerializer.Deserialize<FullySelfImplementedWrapperValueObject, int>(
						((System.Text.Json.Serialization.JsonConverter<int>)options.GetConverter(typeof(int))).ReadAsPropertyName(ref reader, typeToConvert, options));

				public override void WriteAsPropertyName(System.Text.Json.Utf8JsonWriter writer, FullySelfImplementedWrapperValueObject value, System.Text.Json.JsonSerializerOptions options) =>
					((System.Text.Json.Serialization.JsonConverter<int>)options.GetConverter(typeof(int))).WriteAsPropertyName(
						writer,
						DomainObjectSerializer.Serialize<FullySelfImplementedWrapperValueObject, int>(value)!, options);
			}

			private sealed class NewtonsoftJsonConverter : Newtonsoft.Json.JsonConverter
			{
				public override bool CanConvert(Type objectType) =>
					objectType == typeof(FullySelfImplementedWrapperValueObject);

				public override object? ReadJson(Newtonsoft.Json.JsonReader reader, Type objectType, object? existingValue, Newtonsoft.Json.JsonSerializer serializer) =>
					reader.Value is null && (!typeof(FullySelfImplementedWrapperValueObject).IsValueType || objectType != typeof(FullySelfImplementedWrapperValueObject)) // Null data for a reference type or nullable value type
						? (FullySelfImplementedWrapperValueObject?)null
						: DomainObjectSerializer.Deserialize<FullySelfImplementedWrapperValueObject, int>(serializer.Deserialize<int>(reader)!);

				public override void WriteJson(Newtonsoft.Json.JsonWriter writer, object? value, Newtonsoft.Json.JsonSerializer serializer) =>
					serializer.Serialize(writer, value is not FullySelfImplementedWrapperValueObject instance ? (object?)null : DomainObjectSerializer.Serialize<FullySelfImplementedWrapperValueObject, int>(instance));
			}
		}
	}
}
