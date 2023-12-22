using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Architect.DomainModeling.Conversions;
using Architect.DomainModeling.Tests.IdentityTestTypes;
using Xunit;

namespace Architect.DomainModeling.Tests
{
	public class IdentityTests
	{
		[Fact]
		public void Construct_WithNull_ShouldSucceed()
		{
			_ = new StringId(null);
		}

		[Fact]
		public void ToString_Regularly_ShouldReturnExpectedResult()
		{
			Assert.Equal("1", new IntId(1).ToString());
		}

		[Fact]
		public void GetHashCode_Regulary_ShouldReturnExpectedResult()
		{
			Assert.Equal(1.GetHashCode(), new IntId(1).GetHashCode());
		}

		[Fact]
		public void GetHashCode_WithString_ShouldHonorCasing()
		{
			var one = new StringId("A").GetHashCode();
			var two = new StringId("a").GetHashCode();
			Assert.NotEqual(one, two);
		}

		[Fact]
		public void GetHashCode_WithIgnoreCaseString_ShouldIgnoreCasing()
		{
			var one = new IgnoreCaseStringId("A").GetHashCode();
			var two = new IgnoreCaseStringId("a").GetHashCode();
			Assert.Equal(one, two);
		}

		[Fact]
		public void GetHashCode_WithUnintializedObject_ShouldReturnExpectedResult()
		{
			var instance = (StringId)FormatterServices.GetUninitializedObject(typeof(StringId));
			Assert.Equal("".GetHashCode(), instance.GetHashCode());
		}

		[Theory]
		[InlineData(0, 0, true)]
		[InlineData(0, 1, false)]
		[InlineData(1, -1, false)]
		public void Equals_Regularly_ShouldReturnExpectedResult(int one, int two, bool expectedResult)
		{
			var left = new IntId(one);
			var right = new IntId(two);
			Assert.Equal(expectedResult, left.Equals(right));
		}

		[Theory]
		[InlineData(null, null, true)]
		[InlineData(null, "", true)]
		[InlineData("", null, true)]
		[InlineData("", "", true)]
		[InlineData("A", "A", true)]
		[InlineData("A", "a", false)]
		[InlineData("A", "B", false)]
		public void Equals_WithString_ShouldReturnExpectedResult(string one, string two, bool expectedResult)
		{
			var left = new StringId(one);
			var right = new StringId(two);
			Assert.Equal(expectedResult, left.Equals(right));
		}

		[Theory]
		[InlineData(null, null, true)]
		[InlineData(null, "", true)]
		[InlineData("", null, true)]
		[InlineData("", "", true)]
		[InlineData("A", "A", true)]
		[InlineData("A", "a", true)]
		[InlineData("A", "B", false)]
		public void Equals_WithIgnoreCaseString_ShouldReturnExpectedResult(string one, string two, bool expectedResult)
		{
			var left = new IgnoreCaseStringId(one);
			var right = new IgnoreCaseStringId(two);
			Assert.Equal(expectedResult, left.Equals(right));
		}

		[Fact]
		public void Equals_WithUnintializedObject_ShouldReturnExpectedResult()
		{
			var left = (StringId)FormatterServices.GetUninitializedObject(typeof(StringId));
			var right = new StringId("Example");

			Assert.NotEqual(left, right);
			Assert.NotEqual(right, left);

			Assert.Equal(new StringId(null), left);
			Assert.Equal(new StringId(""), left);
		}

		[Fact]
		public void ObjectEquals_WithRegularStruct_ShouldReturnExpectedResult()
		{
			var one = (object)new IntId(1);
			var alsoOne = (object)new IntId(1);
			var two = (object)new IntId(2);

			Assert.Equal(one, alsoOne);
			Assert.NotEqual(one, two);
		}

		[Fact]
		public void ObjectEquals_WithRecordStruct_ShouldReturnExpectedResult()
		{
			var one = (object)new DecimalId(1);
			var alsoOne = (object)new DecimalId(1);
			var two = (object)new DecimalId(2);

			Assert.Equal(one, alsoOne);
			Assert.NotEqual(one, two);
		}

		[Theory]
		[InlineData(0, 0)]
		[InlineData(0, 1)]
		[InlineData(1, -1)]
		public void EqualityOperator_Regularly_ShouldMatchEquals(int one, int two)
		{
			var left = new IntId(one);
			var right = new IntId(two);
			Assert.Equal(left.Equals(right), left == right);
		}

		[Theory]
		[InlineData("", "")]
		[InlineData("A", "A")]
		[InlineData("A", "a")]
		[InlineData("A", "B")]
		public void EqualityOperator_WithString_ShouldMatchEquals(string one, string two)
		{
			var left = new StringId(one);
			var right = new StringId(two);
			Assert.Equal(left.Equals(right), left == right);
		}

		[Theory]
		[InlineData("", "")]
		[InlineData("A", "A")]
		[InlineData("A", "a")]
		[InlineData("A", "B")]
		public void EqualityOperator_WithIgnoreCaseString_ShouldMatchEquals(string one, string two)
		{
			var left = new IgnoreCaseStringId(one);
			var right = new IgnoreCaseStringId(two);
			Assert.Equal(left.Equals(right), left == right);
		}

		[Theory]
		[InlineData("", "")]
		[InlineData("A", "A")]
		[InlineData("A", "a")]
		[InlineData("A", "B")]
		public void CompareTo_WithEqualValuesAndString_ShouldHaveEqualityMatchingEquals(string one, string two)
		{
			var left = new StringId(one);
			var right = new StringId(two);
			Assert.Equal(left.Equals(right), left.CompareTo(right) == 0);
		}

		[Theory]
		[InlineData("", "")]
		[InlineData("A", "A")]
		[InlineData("A", "a")]
		[InlineData("A", "B")]
		public void CompareTo_WithEqualValuesAndIgnoreCaseString_ShouldHaveEqualityMatchingEquals(string one, string two)
		{
			var left = new IgnoreCaseStringId(one);
			var right = new IgnoreCaseStringId(two);
			Assert.Equal(left.Equals(right), left.CompareTo(right) == 0);
		}

		[Fact]
		public void CompareTo_WithoutExplicitInterface_ShouldBeImplemented()
		{
			var array = new[] { new IntId(1), new IntId(2) };

			Array.Sort(array); // Should not throw
		}

		[Fact]
		public void CompareTo_WithExplicitInterface_ShouldBeImplementedCorrectly()
		{
			var array1 = new[] { new StringId("a"), new StringId("A"), new StringId("0"), };

			array1 = array1.OrderBy(x => x).ToArray(); // Stable sort

			Assert.Equal("0", array1[0].Value);
			Assert.Equal("A", array1[1].Value);
			Assert.Equal("a", array1[2].Value);

			var array2 = new[] { new IgnoreCaseStringId("a"), new IgnoreCaseStringId("A"), new IgnoreCaseStringId("0"), };

			array2 = array2.OrderBy(x => x).ToArray(); // Stable sort

			Assert.Equal("0", array2[0].Value);
			Assert.Equal("a", array2[1].Value);
			Assert.Equal("A", array2[2].Value);
		}

		[Theory]
		[InlineData(null, null, 0)]
		[InlineData(null, "", 0)]
		[InlineData("", null, 0)]
		[InlineData("", "", 0)]
		[InlineData("", "A", -1)]
		[InlineData("A", "", +1)]
		[InlineData("A", "a", -1)]
		[InlineData("a", "A", +1)]
		[InlineData("A", "B", -1)]
		[InlineData("AA", "A", +1)]
		public void CompareTo_WithString_ShouldReturnExpectedResult(string one, string two, int expectedResult)
		{
			var left = (StringId)one;
			var right = (StringId)two;

			var result = Comparer<StringId>.Default.Compare(left, right);
			var reverseResult = Comparer<StringId>.Default.Compare(right, left);

			if (result != 0) result /= Math.Abs(result); // Normalize to 1 or -1
			if (reverseResult != 0) reverseResult /= Math.Abs(reverseResult); // Normalize to 1 or -1

			Assert.Equal(expectedResult, result);
			Assert.Equal(-expectedResult, reverseResult);
		}

		[Theory]
		[InlineData(null, null, 0)]
		[InlineData(null, "", 0)]
		[InlineData("", null, 0)]
		[InlineData("", "", 0)]
		[InlineData("", "A", -1)]
		[InlineData("A", "", +1)]
		[InlineData("A", "a", 0)]
		[InlineData("a", "A", 0)]
		[InlineData("A", "B", -1)]
		[InlineData("AA", "A", +1)]
		public void CompareTo_WithIgnoreCaseString_ShouldReturnExpectedResult(string one, string two, int expectedResult)
		{
			var left = (IgnoreCaseStringId)one;
			var right = (IgnoreCaseStringId)two;

			var result = Comparer<IgnoreCaseStringId>.Default.Compare(left, right);
			var reverseResult = Comparer<IgnoreCaseStringId>.Default.Compare(right, left);

			if (result != 0) result /= Math.Abs(result); // Normalize to 1 or -1
			if (reverseResult != 0) reverseResult /= Math.Abs(reverseResult); // Normalize to 1 or -1

			Assert.Equal(expectedResult, result);
			Assert.Equal(-expectedResult, reverseResult);
		}

		[Theory]
		[InlineData(null, null, 0)]
		[InlineData(null, "", 0)]
		[InlineData("", null, 0)]
		[InlineData("", "", 0)]
		[InlineData("", "A", -1)]
		[InlineData("A", "", +1)]
		[InlineData("A", "a", -1)]
		[InlineData("a", "A", +1)]
		[InlineData("A", "B", -1)]
		[InlineData("AA", "A", +1)]
		public void GreaterThan_WithString_ShouldReturnExpectedResult(string one, string two, int expectedResult)
		{
			var left = (StringId)one;
			var right = (StringId)two;

			Assert.Equal(expectedResult > 0, left > right);
			Assert.Equal(expectedResult <= 0, left <= right);
		}

		[Theory]
		[InlineData(null, null, 0)]
		[InlineData(null, "", 0)]
		[InlineData("", null, 0)]
		[InlineData("", "", 0)]
		[InlineData("", "A", -1)]
		[InlineData("A", "", +1)]
		[InlineData("A", "a", -1)]
		[InlineData("a", "A", +1)]
		[InlineData("A", "B", -1)]
		[InlineData("AA", "A", +1)]
		public void LessThan_WithString_ShouldReturnExpectedResult(string one, string two, int expectedResult)
		{
			var left = (StringId)one;
			var right = (StringId)two;

			Assert.Equal(expectedResult < 0, left < right);
			Assert.Equal(expectedResult >= 0, left >= right);
		}

		[Theory]
		[InlineData(null, null)]
		[InlineData(0, 0)]
		[InlineData(1, 1)]
		public void CastToUnderlyingType_Regularly_ShouldReturnExpectedResult(int? value, int? expectedResult)
		{
			var instance = value is null ? (IntId?)null : new IntId(value.Value);

			if (expectedResult is null)
				Assert.Throws<InvalidOperationException>(() => (int)instance!);
			else
				Assert.Equal(expectedResult, (int)instance!);
		}

		[Theory]
		[InlineData(null, null)]
		[InlineData(0, 0)]
		[InlineData(1, 1)]
		public void CastToNullableUnderlyingType_Regularly_ShouldReturnExpectedResult(int? value, int? expectedResult)
		{
			var instance = value is null ? (IntId?)null : new IntId(value.Value);

			var result = (int?)instance;

			Assert.Equal(expectedResult, result);
		}

		[Theory]
		[InlineData(0, 0)]
		[InlineData(1, 1)]
		public void CastFromUnderlyingType_Regularly_ShouldReturnExpectedResult(int value, int expectedResult)
		{
			Assert.Equal(new IntId(expectedResult), (IntId)value);
		}

		[Theory]
		[InlineData(null, null)]
		[InlineData(0, 0)]
		[InlineData(1, 1)]
		public void CastFromNullableUnderlyingType_Regularly_ShouldReturnExpectedResult(int? value, int? expectedResult)
		{
			var result = (IntId?)value;

			Assert.Equal(expectedResult, result?.Value);
		}

		[Theory]
		[InlineData(null)]
		[InlineData(0)]
		[InlineData(1)]
		public void SerializeWithSystemTextJson_Regularly_ShouldReturnExpectedResult(int? value)
		{
			var intInstance = (IntId?)value;
			Assert.Equal(value?.ToString() ?? "null", System.Text.Json.JsonSerializer.Serialize(intInstance));
			if (intInstance is not null) Assert.Equal(value?.ToString() ?? "null", System.Text.Json.JsonSerializer.Serialize(intInstance.Value));

			var stringInstance = (StringId?)value?.ToString();
			Assert.Equal(value is null ? "null" : $@"""{value}""", System.Text.Json.JsonSerializer.Serialize(stringInstance));
			if (stringInstance is not null) Assert.Equal(value is null ? "null" : $@"""{value}""", System.Text.Json.JsonSerializer.Serialize(stringInstance.Value));

			// Even with nested identity and/or wrapper value objects, no constructors should be hit
			var nestedInstance = value is null ? (JsonTestingIntId?)null : new JsonTestingIntId(value.Value, false);
			Assert.Equal(value is null ? "null" : $@"{value}", System.Text.Json.JsonSerializer.Serialize(nestedInstance));
			if (nestedInstance is not null) Assert.Equal(value is null ? "null" : $@"{value}", System.Text.Json.JsonSerializer.Serialize(nestedInstance.Value));
		}

		[Theory]
		[InlineData(null)]
		[InlineData(0)]
		[InlineData(1)]
		public void SerializeWithNewtonsoftJson_Regularly_ShouldReturnExpectedResult(int? value)
		{
			var intInstance = (IntId?)value;
			Assert.Equal(value?.ToString() ?? "null", Newtonsoft.Json.JsonConvert.SerializeObject(intInstance));
			if (intInstance is not null) Assert.Equal(value?.ToString() ?? "null", Newtonsoft.Json.JsonConvert.SerializeObject(intInstance.Value));

			var stringInstance = (StringId?)value?.ToString();
			Assert.Equal(value is null ? "null" : $@"""{value}""", Newtonsoft.Json.JsonConvert.SerializeObject(stringInstance));
			if (stringInstance is not null) Assert.Equal(value is null ? "null" : $@"""{value}""", Newtonsoft.Json.JsonConvert.SerializeObject(stringInstance.Value));

			// Even with nested identity and/or wrapper value objects, no constructors should be hit
			var nestedInstance = value is null ? (JsonTestingIntId?)null : new JsonTestingIntId(value.Value, false);
			Assert.Equal(value is null ? "null" : $@"{value}", Newtonsoft.Json.JsonConvert.SerializeObject(nestedInstance));
			if (nestedInstance is not null) Assert.Equal(value is null ? "null" : $@"{value}", Newtonsoft.Json.JsonConvert.SerializeObject(nestedInstance.Value));
		}

		/// <summary>
		/// Generated identities should special-case decimals by treating them as strings, to protect systems that cannot handle large decimals.
		/// </summary>
		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		public void SerializeWithSystemTextJson_WithDecimal_ShouldReturnExpectedResult(int value)
		{
			// Attempt to mess with the stringification, which should have no effect
			CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");

			Assert.Equal($@"""{value}""", Newtonsoft.Json.JsonConvert.SerializeObject((DecimalId)value));
			Assert.Equal($@"""{value}""", Newtonsoft.Json.JsonConvert.SerializeObject((DecimalId?)value));
		}

		/// <summary>
		/// Generated identities should special-case decimals by treating them as strings, to protect systems that cannot handle large decimals.
		/// </summary>
		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		public void SerializeWithNewtonsoftJson_WithDecimal_ShouldReturnExpectedResult(int value)
		{
			// Attempt to mess with the stringification, which should have no effect
			CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("nl-NL");

			Assert.Equal($@"""{value}""", Newtonsoft.Json.JsonConvert.SerializeObject((DecimalId)value));
			Assert.Equal($@"""{value}""", Newtonsoft.Json.JsonConvert.SerializeObject((DecimalId?)value));
		}

		[Theory]
		[InlineData("null", null)]
		[InlineData("0", 0)]
		[InlineData("1", 1)]
		public void DeserializeWithSystemTextJson_Regularly_ShouldReturnExpectedResult(string json, int? value)
		{
			Assert.Equal(value, System.Text.Json.JsonSerializer.Deserialize<IntId?>(json)?.Value);
			if (json != "null") Assert.Equal(value, System.Text.Json.JsonSerializer.Deserialize<IntId>(json).Value);

			// Even with nested identity and/or wrapper value objects, no constructors should be hit
			Assert.Equal(value, System.Text.Json.JsonSerializer.Deserialize<JsonTestingIntId?>(json)?.Value?.Value.Value);
			if (json != "null") Assert.Equal(value!.Value, System.Text.Json.JsonSerializer.Deserialize<JsonTestingIntId>(json).Value?.Value.Value);

			json = json == "null" ? json : $@"""{json}""";
			Assert.Equal(value?.ToString(), System.Text.Json.JsonSerializer.Deserialize<StringId?>(json)?.Value);
			if (json != "null") Assert.Equal(value?.ToString(), System.Text.Json.JsonSerializer.Deserialize<StringId>(json).Value);
		}

		[Theory]
		[InlineData("null", null)]
		[InlineData("0", 0)]
		[InlineData("1", 1)]
		public void DeserializeWithNewtonsoftJson_Regularly_ShouldReturnExpectedResult(string json, int? value)
		{
			Assert.Equal(value, Newtonsoft.Json.JsonConvert.DeserializeObject<IntId?>(json)?.Value);
			if (json != "null") Assert.Equal(value, Newtonsoft.Json.JsonConvert.DeserializeObject<IntId>(json).Value);

			// Even with nested identity and/or wrapper value objects, no constructors should be hit
			Assert.Equal(value, Newtonsoft.Json.JsonConvert.DeserializeObject<JsonTestingIntId?>(json)?.Value?.Value.Value);
			if (json != "null") Assert.Equal(value!.Value, Newtonsoft.Json.JsonConvert.DeserializeObject<JsonTestingIntId>(json).Value?.Value.Value);

			json = json == "null" ? json : $@"""{json}""";
			Assert.Equal(value?.ToString(), Newtonsoft.Json.JsonConvert.DeserializeObject<StringId?>(json)?.Value);
			if (json != "null") Assert.Equal(value?.ToString(), Newtonsoft.Json.JsonConvert.DeserializeObject<StringId>(json).Value);
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

			Assert.Equal(value, System.Text.Json.JsonSerializer.Deserialize<DecimalId?>(json)?.Value);

			if (json != "null")
				Assert.Equal((decimal)value!, System.Text.Json.JsonSerializer.Deserialize<DecimalId>(json).Value);
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

			Assert.Equal(value, Newtonsoft.Json.JsonConvert.DeserializeObject<DecimalId?>(json)?.Value);

			if (json != "null")
				Assert.Equal((decimal)value!, Newtonsoft.Json.JsonConvert.DeserializeObject<DecimalId>(json).Value);
		}

		[Theory]
		[InlineData("0", 0)]
		[InlineData("1", 1)]
		public void ReadAsPropertyNameWithSystemTextJson_Regularly_ShouldReturnExpectedResult(string json, int value)
		{
			json = $$"""{ "{{json}}": true }""";

			Assert.Equal(KeyValuePair.Create((IntId)value, true), System.Text.Json.JsonSerializer.Deserialize<Dictionary<IntId, bool>>(json)?.Single());

			Assert.Equal(KeyValuePair.Create((StringId)value.ToString(), true), System.Text.Json.JsonSerializer.Deserialize<Dictionary<StringId, bool>>(json)?.Single());

			Assert.Equal(KeyValuePair.Create((DecimalId)value, true), System.Text.Json.JsonSerializer.Deserialize<Dictionary<DecimalId, bool>>(json)?.Single());

			// Even with nested identity and/or wrapper value objects, no constructors should be hit
			Assert.Equal(KeyValuePair.Create(new JsonTestingIntId(value, false), true), System.Text.Json.JsonSerializer.Deserialize<Dictionary<JsonTestingIntId, bool>>(json)?.Single());
		}

		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		public void WriteAsPropertyNameWithSystemTextJson_Regularly_ShouldReturnExpectedResult(int value)
		{
			var expectedResult = $$"""{"{{value}}":true}""";

			Assert.Equal(expectedResult, System.Text.Json.JsonSerializer.Serialize(new Dictionary<IntId, bool>() { [value] = true }));

			Assert.Equal(expectedResult, System.Text.Json.JsonSerializer.Serialize(new Dictionary<StringId, bool>() { [value.ToString()] = true }));

			Assert.Equal(expectedResult, System.Text.Json.JsonSerializer.Serialize(new Dictionary<DecimalId, bool>() { [value] = true }));

			// Even with nested identity and/or wrapper value objects, no constructors should be hit
			Assert.Equal(expectedResult, System.Text.Json.JsonSerializer.Serialize(new Dictionary<JsonTestingIntId, bool>() { [new JsonTestingIntId(value, false)] = true }));
		}

		[Fact]
		public void FormattableToString_InAllScenarios_ShouldReturnExpectedResult()
		{
			Assert.Equal("5", new IntId(5).ToString(format: null, formatProvider: null));
			Assert.Equal("5", new StringId("5").ToString(format: null, formatProvider: null));
			Assert.Equal("5", new FullySelfImplementedIdentity(5).ToString(format: null, formatProvider: null));
			Assert.Equal("5", new FormatAndParseTestingIntId(5).ToString(format: null, formatProvider: null));

			Assert.Equal("", ((FormatAndParseTestingIntId)RuntimeHelpers.GetUninitializedObject(typeof(FormatAndParseTestingIntId))).ToString(format: null, formatProvider: null));
		}

		[Fact]
		public void SpanFormattableTryFormat_InAllScenarios_ShouldReturnExpectedResult()
		{
			Span<char> result = stackalloc char[1];

			Assert.True(new IntId(5).TryFormat(result, out var charsWritten, format: null, provider: null));
			Assert.Equal(1, charsWritten);
			Assert.Equal("5".AsSpan(), result);

			Assert.True(new StringId("5").TryFormat(result, out charsWritten, format: null, provider: null));
			Assert.Equal(1, charsWritten);
			Assert.Equal("5".AsSpan(), result);

			Assert.True(new FullySelfImplementedIdentity(5).TryFormat(result, out charsWritten, format: null, provider: null));
			Assert.Equal(1, charsWritten);
			Assert.Equal("5".AsSpan(), result);

			Assert.True(new FormatAndParseTestingIntId(5).TryFormat(result, out charsWritten, format: null, provider: null));
			Assert.Equal(1, charsWritten);
			Assert.Equal("5".AsSpan(), result);

			Assert.True(((FormatAndParseTestingIntId)RuntimeHelpers.GetUninitializedObject(typeof(FormatAndParseTestingIntId))).TryFormat(result, out charsWritten, format: null, provider: null));
			Assert.Equal(0, charsWritten);
		}

		[Fact]
		public void UtfSpanFormattableTryFormat_InAllScenarios_ShouldReturnExpectedResult()
		{
			Span<byte> result = stackalloc byte[1];

			Assert.True(new IntId(5).TryFormat(result, out var bytesWritten, format: null, provider: null));
			Assert.Equal(1, bytesWritten);
			Assert.Equal("5"u8, result);

			Assert.True(new StringId("5").TryFormat(result, out bytesWritten, format: null, provider: null));
			Assert.Equal(1, bytesWritten);
			Assert.Equal("5"u8, result);

			Assert.True(new FullySelfImplementedIdentity(5).TryFormat(result, out bytesWritten, format: null, provider: null));
			Assert.Equal(1, bytesWritten);
			Assert.Equal("5"u8, result);

			Assert.True(new FormatAndParseTestingIntId(5).TryFormat(result, out bytesWritten, format: null, provider: null));
			Assert.Equal(1, bytesWritten);
			Assert.Equal("5"u8, result);

			Assert.True(((FormatAndParseTestingIntId)RuntimeHelpers.GetUninitializedObject(typeof(FormatAndParseTestingIntId))).TryFormat(result, out bytesWritten, format: null, provider: null));
			Assert.Equal(0, bytesWritten);
		}

		[Fact]
		public void ParsableTryParseAndParse_InAllScenarios_ShouldReturnExpectedResult()
		{
			var input = "5";

			Assert.True(IntId.TryParse(input, provider: null, out var result1));
			Assert.Equal(5, result1.Value);
			Assert.Equal(result1, IntId.Parse(input, provider: null));

			Assert.True(StringId.TryParse(input, provider: null, out var result2));
			Assert.Equal("5", result2.Value);
			Assert.Equal(result2, StringId.Parse(input, provider: null));

			Assert.True(FullySelfImplementedIdentity.TryParse(input, provider: null, out var result3));
			Assert.Equal(5, result3.Value);
			Assert.Equal(result3, FullySelfImplementedIdentity.Parse(input, provider: null));

			Assert.True(FormatAndParseTestingIntId.TryParse(input, provider: null, out var result4));
			Assert.Equal(5, result4.Value?.Value.Value);
			Assert.Equal(result4, FormatAndParseTestingIntId.Parse(input, provider: null));
		}

		[Fact]
		public void SpanParsableTryParseAndParse_InAllScenarios_ShouldReturnExpectedResult()
		{
			var input = "5".AsSpan();

			Assert.True(IntId.TryParse(input, provider: null, out var result1));
			Assert.Equal(5, result1.Value);
			Assert.Equal(result1, IntId.Parse(input, provider: null));

			Assert.True(StringId.TryParse(input, provider: null, out var result2));
			Assert.Equal("5", result2.Value);
			Assert.Equal(result2, StringId.Parse(input, provider: null));

			Assert.True(FullySelfImplementedIdentity.TryParse(input, provider: null, out var result3));
			Assert.Equal(5, result3.Value);
			Assert.Equal(result3, FullySelfImplementedIdentity.Parse(input, provider: null));

			Assert.True(FormatAndParseTestingIntId.TryParse(input, provider: null, out var result4));
			Assert.Equal(5, result4.Value?.Value.Value);
			Assert.Equal(result4, FormatAndParseTestingIntId.Parse(input, provider: null));
		}

		[Fact]
		public void Utf8SpanParsableTryParseAndParse_InAllScenarios_ShouldReturnExpectedResult()
		{
			var input = "5"u8;

			Assert.True(IntId.TryParse(input, provider: null, out var result1));
			Assert.Equal(5, result1.Value);
			Assert.Equal(result1, IntId.Parse(input, provider: null));

			Assert.True(StringId.TryParse(input, provider: null, out var result2));
			Assert.Equal("5", result2.Value);
			Assert.Equal(result2, StringId.Parse(input, provider: null));

			Assert.True(FullySelfImplementedIdentity.TryParse(input, provider: null, out var result3));
			Assert.Equal(5, result3.Value);
			Assert.Equal(result3, FullySelfImplementedIdentity.Parse(input, provider: null));

			Assert.True(FormatAndParseTestingIntId.TryParse(input, provider: null, out var result4));
			Assert.Equal(5, result4.Value?.Value.Value);
			Assert.Equal(result4, FormatAndParseTestingIntId.Parse(input, provider: null));
		}
	}

	// Use a namespace, since our source generators dislike nested types
	namespace IdentityTestTypes
	{
		[IdentityValueObject<int>]
		internal partial struct IntId
		{
			public int Value { get; }
		}

		[IdentityValueObject<decimal>]
		internal partial record struct DecimalId;

		[IdentityValueObject<string>]
		internal partial record struct StringId;

		[IdentityValueObject<string>]
		internal partial struct IgnoreCaseStringId
		{
			internal StringComparison StringComparison => StringComparison.OrdinalIgnoreCase;
		}

		[IdentityValueObject<FormatAndParseTestingIntWrapper>]
		internal readonly partial struct FormatAndParseTestingIntId
		{
			public FormatAndParseTestingIntId(int value)
			{
				this.Value = new FormatAndParseTestingIntWrapper(value);
			}
		}
		[WrapperValueObject<IntId>]
		internal partial class FormatAndParseTestingIntWrapper : IComparable<FormatAndParseTestingIntWrapper>
		{
			public FormatAndParseTestingIntWrapper(int value)
			{
				this.Value = new IntId(value);
			}
		}

		[IdentityValueObject<JsonTestingIntWrapper>]
		internal readonly partial struct JsonTestingIntId
		{
			public JsonTestingIntId(FormatAndParseTestingIntWrapper _)
			{
				throw new Exception("This constructor should not be used. This lets tests confirm that concerns such as deserialization correctly avoid constructors.");
			}
			public JsonTestingIntId(int value, bool _)
			{
				this.Value = new JsonTestingIntWrapper(value, false);
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
		[WrapperValueObject<IntId>]
		internal partial class JsonTestingIntWrapper : IComparable<JsonTestingIntWrapper>
		{
			public JsonTestingIntWrapper(IntId _)
			{
				throw new Exception("This constructor should not be used. This lets tests confirm that concerns such as deserialization correctly avoid constructors.");
			}
			public JsonTestingIntWrapper(int value, bool _)
			{
				this.Value = new IntId(value);
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
		[IdentityValueObject<int>]
		[System.Text.Json.Serialization.JsonConverter(typeof(JsonConverter))]
		[Newtonsoft.Json.JsonConverter(typeof(NewtonsoftJsonConverter))]
		internal readonly partial struct FullySelfImplementedIdentity
			: IIdentity<int>,
			IEquatable<FullySelfImplementedIdentity>,
			IComparable<FullySelfImplementedIdentity>,
#if NET7_0_OR_GREATER
			ISpanFormattable,
			ISpanParsable<FullySelfImplementedIdentity>,
#endif
#if NET8_0_OR_GREATER
			IUtf8SpanFormattable,
			IUtf8SpanParsable<FullySelfImplementedIdentity>,
#endif
			ISerializableDomainObject<FullySelfImplementedIdentity, int>
		{
			public int Value { get; private init; }

			public FullySelfImplementedIdentity(int value)
			{
				this.Value = value;
			}

			public override int GetHashCode()
			{
				return this.Value.GetHashCode();
			}

			public override bool Equals(object? other)
			{
				return other is FullySelfImplementedIdentity otherId && this.Equals(otherId);
			}

			public bool Equals(FullySelfImplementedIdentity other)
			{
				return this.Value.Equals(other.Value);
			}

			public int CompareTo(FullySelfImplementedIdentity other)
			{
				return this.Value.CompareTo(other.Value);
			}

			public override string ToString()
			{
				return this.Value.ToString("0.#");
			}

			/// <summary>
			/// Serializes a domain object as a plain value.
			/// </summary>
			int ISerializableDomainObject<FullySelfImplementedIdentity, int>.Serialize()
			{
				return this.Value;
			}

			/// <summary>
			/// Deserializes a plain value back into a domain object without any validation.
			/// </summary>
			static FullySelfImplementedIdentity ISerializableDomainObject<FullySelfImplementedIdentity, int>.Deserialize(int value)
			{
				return new FullySelfImplementedIdentity() { Value = value };
			}

			public static bool operator ==(FullySelfImplementedIdentity left, FullySelfImplementedIdentity right) => left.Equals(right);
			public static bool operator !=(FullySelfImplementedIdentity left, FullySelfImplementedIdentity right) => !(left == right);

			public static bool operator >(FullySelfImplementedIdentity left, FullySelfImplementedIdentity right) => left.CompareTo(right) > 0;
			public static bool operator <(FullySelfImplementedIdentity left, FullySelfImplementedIdentity right) => left.CompareTo(right) < 0;
			public static bool operator >=(FullySelfImplementedIdentity left, FullySelfImplementedIdentity right) => left.CompareTo(right) >= 0;
			public static bool operator <=(FullySelfImplementedIdentity left, FullySelfImplementedIdentity right) => left.CompareTo(right) <= 0;

			public static implicit operator FullySelfImplementedIdentity(int value) => new FullySelfImplementedIdentity(value);
			public static implicit operator int(FullySelfImplementedIdentity id) => id.Value;

			[return: NotNullIfNotNull(nameof(value))]
			public static implicit operator FullySelfImplementedIdentity?(int? value) => value is null ? null : new FullySelfImplementedIdentity(value.Value);
			[return: NotNullIfNotNull(nameof(id))]
			public static implicit operator int?(FullySelfImplementedIdentity? id) => id?.Value;

			#region Formatting & Parsing

#if NET7_0_OR_GREATER

			public string ToString(string? format, IFormatProvider? formatProvider) =>
				FormattingHelper.ToString(this.Value, format, formatProvider);

			public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
				FormattingHelper.TryFormat(this.Value, destination, out charsWritten, format, provider);

			public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out FullySelfImplementedIdentity result) =>
				ParsingHelper.TryParse(s, provider, out int value)
					? (result = (FullySelfImplementedIdentity)value) is var _
					: !((result = default) is var _);

			public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out FullySelfImplementedIdentity result) =>
				ParsingHelper.TryParse(s, provider, out int value)
					? (result = (FullySelfImplementedIdentity)value) is var _
					: !((result = default) is var _);

			public static FullySelfImplementedIdentity Parse(string s, IFormatProvider? provider) =>
				(FullySelfImplementedIdentity)ParsingHelper.Parse<int>(s, provider);

			public static FullySelfImplementedIdentity Parse(ReadOnlySpan<char> s, IFormatProvider? provider) =>
				(FullySelfImplementedIdentity)ParsingHelper.Parse<int>(s, provider);

#endif

#if NET8_0_OR_GREATER

			public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format, IFormatProvider? provider) =>
				FormattingHelper.TryFormat(this.Value, utf8Destination, out bytesWritten, format, provider);

			public static bool TryParse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider, [MaybeNullWhen(false)] out FullySelfImplementedIdentity result) =>
				ParsingHelper.TryParse(utf8Text, provider, out int value)
					? (result = (FullySelfImplementedIdentity)value) is var _
					: !((result = default) is var _);

			public static FullySelfImplementedIdentity Parse(ReadOnlySpan<byte> utf8Text, IFormatProvider? provider) =>
				(FullySelfImplementedIdentity)ParsingHelper.Parse<int>(utf8Text, provider);

#endif

			#endregion

			private sealed class JsonConverter : System.Text.Json.Serialization.JsonConverter<FullySelfImplementedIdentity>
			{
				public override FullySelfImplementedIdentity Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options) =>
					DomainObjectSerializer.Deserialize<FullySelfImplementedIdentity, int>(System.Text.Json.JsonSerializer.Deserialize<int>(ref reader, options)!);

				public override void Write(System.Text.Json.Utf8JsonWriter writer, FullySelfImplementedIdentity value, System.Text.Json.JsonSerializerOptions options) =>
					System.Text.Json.JsonSerializer.Serialize(writer, DomainObjectSerializer.Serialize<FullySelfImplementedIdentity, int>(value), options);

				public override FullySelfImplementedIdentity ReadAsPropertyName(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options) =>
					DomainObjectSerializer.Deserialize<FullySelfImplementedIdentity, int>(
						((System.Text.Json.Serialization.JsonConverter<int>)options.GetConverter(typeof(int))).ReadAsPropertyName(ref reader, typeToConvert, options));

				public override void WriteAsPropertyName(System.Text.Json.Utf8JsonWriter writer, FullySelfImplementedIdentity value, System.Text.Json.JsonSerializerOptions options) =>
					((System.Text.Json.Serialization.JsonConverter<int>)options.GetConverter(typeof(int))).WriteAsPropertyName(
						writer,
						DomainObjectSerializer.Serialize<FullySelfImplementedIdentity, int>(value)!, options);
			}

			private sealed class NewtonsoftJsonConverter : Newtonsoft.Json.JsonConverter
			{
				public override bool CanConvert(Type objectType) =>
					objectType == typeof(FullySelfImplementedIdentity) || objectType == typeof(FullySelfImplementedIdentity?);

				public override object? ReadJson(Newtonsoft.Json.JsonReader reader, Type objectType, object? existingValue, Newtonsoft.Json.JsonSerializer serializer) =>
					reader.Value is null && (!typeof(FullySelfImplementedIdentity).IsValueType || objectType != typeof(FullySelfImplementedIdentity)) // Null data for a reference type or nullable value type
						? (FullySelfImplementedIdentity?)null
						: DomainObjectSerializer.Deserialize<FullySelfImplementedIdentity, int>(serializer.Deserialize<int>(reader)!);

				public override void WriteJson(Newtonsoft.Json.JsonWriter writer, object? value, Newtonsoft.Json.JsonSerializer serializer) =>
					serializer.Serialize(writer, value is not FullySelfImplementedIdentity instance ? (object?)null : DomainObjectSerializer.Serialize<FullySelfImplementedIdentity, int>(instance));
			}
		}
	}
}
