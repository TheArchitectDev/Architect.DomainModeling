using System.Globalization;
using System.Runtime.Serialization;
using Xunit;

namespace Architect.DomainModeling.Tests.Entities;

public class SourceGeneratedIdentityTests
{
	/// <summary>
	/// ID instances should be able to have a default value, as is the case for a default instance of the ID type.
	/// </summary>
	[Fact]
	public void Construct_WithNull_ShouldSucceed()
	{
		_ = new StringId(null);
	}

	/// <summary>
	/// We have special-cased string-wrapping identities to use the empty string instead of null, even with a default instance.
	/// This is because we tend to use either a numeric primitive or string as the underlying type for an ID.
	/// String has the drawback of being nullable.
	/// By special-casing underlying strings to default to the empty string, we can guarantee that string-based IDs are never null, just like their numeric counterparts.
	/// </summary>
	[Fact]
	public void Value_WhenConstructedWithNullOrAsDefault_ShouldBeEmptyString()
	{
		Assert.Equal("", new StringId());
		Assert.Equal("", new StringId(null));
	}

	[Fact]
	public void ToString_Regularly_ShouldReturnExpectedResult()
	{
		Assert.Equal("0", new IntId().ToString());
		Assert.Equal("1", new IntId(1).ToString());
		Assert.Equal("", new StringId().ToString()); // Null is special-cased into ""
		Assert.Equal("", new StringId("").ToString());
		Assert.Equal("a", new StringId("a").ToString());
	}

	[Fact]
	public void GetHashCode_Regulary_ShouldReturnExpectedResult()
	{
		Assert.Equal(0.GetHashCode(), new IntId().GetHashCode());
		Assert.Equal(1.GetHashCode(), new IntId(1).GetHashCode());
		Assert.Equal("".GetHashCode(), new StringId().GetHashCode()); // Null is special-cased into ""
		Assert.Equal("0".GetHashCode(), new StringId("0").GetHashCode());
		Assert.Equal("a".GetHashCode(), new StringId("a").GetHashCode());
	}

	[Fact]
	public void GetHashCode_WithDiferentCasedStrings_ShouldReturnExpectedResult()
	{
		var one = new StringId("A").GetHashCode();
		var two = new StringId("a").GetHashCode();
		Assert.NotEqual(one, two);
	}

	[Fact]
	public void GetHashCode_WithUnintializedObject_ShouldReturnExpectedResult()
	{
		var instance1 = (StringId)FormatterServices.GetUninitializedObject(typeof(StringId));
		Assert.Equal("".GetHashCode(), instance1.GetHashCode());

		var instance2 = (ObjectId)FormatterServices.GetUninitializedObject(typeof(ObjectId));
		Assert.Equal(0, instance2.GetHashCode());
	}

	[Theory]
	[InlineData(0, 0, true)]
	[InlineData(0, 1, false)]
	[InlineData(1, 9, false)]
	public void Equals_WithInt_ShouldReturnExpectedResult(int one, int two, bool expectedResult)
	{
		var left = new IntId(one);
		var right = new IntId(two);
		Assert.Equal(expectedResult, left.Equals(right));
	}

	[Theory]
	[InlineData(null, null, true)]
	[InlineData(null, "", true)] // Null is special-cased into ""
	[InlineData("", null, true)] // Null is special-cased into ""
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

	[Fact]
	public void Equals_WithUnintializedObject_ShouldReturnExpectedResult()
	{
		var left1 = (StringId)FormatterServices.GetUninitializedObject(typeof(StringId));
		var right1 = new StringId("");
		Assert.Equal(left1, right1);
		Assert.Equal(right1, left1);

		var left2 = (ObjectId)FormatterServices.GetUninitializedObject(typeof(ObjectId));
		var right2 = new ObjectId(new ComparableObject());
		Assert.NotEqual(left2, right2);
		Assert.NotEqual(right2, left2);
	}

	[Theory]
	[InlineData(0, 0)]
	[InlineData(0, 1)]
	[InlineData(1, 9)]
	public void EqualityOperator_WithInt_ShouldMatchEquals(int one, int two)
	{
		var left = new IntId(one);
		var right = new IntId(two);
		Assert.Equal(left.Equals(right), left == right);
	}

	[Theory]
	[InlineData(null, null)]
	[InlineData(null, "")]
	[InlineData("", null)]
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
	[InlineData(null, null)]
	[InlineData(null, "")]
	[InlineData("", null)]
	[InlineData("", "")]
	[InlineData("A", "A")]
	[InlineData("A", "a")]
	[InlineData("A", "B")]
	public void CompareTo_Regularly_ShouldHaveEqualityMatchingEquals(string one, string two)
	{
		var left = new StringId(one);
		var right = new StringId(two);

		Assert.Equal(left.Equals(right), left.CompareTo(right) == 0);
	}

	[Fact]
	public void CompareTo_WithoutExplicitInterface_ShouldBeImplementedorrectly()
	{
		{
			var array = new[] { new IntId(2), new IntId(1) };

			Array.Sort(array);

			Assert.Equal(1, array[0].Value);
			Assert.Equal(2, array[1].Value);
		}

		{
			var array = new[] { new StringId(""), new StringId("a"), new StringId("A"), new StringId("0"), new StringId(null), };

			array = array.OrderBy(x => x).ToArray(); // Stable sort

			Assert.Equal("", array[0].Value); // Null is special-cased into ""
			Assert.Equal("", array[1].Value);
			Assert.Equal("0", array[2].Value);
			Assert.Equal("A", array[3].Value);
			Assert.Equal("a", array[4].Value);
		}
	}

	[Theory]
	[InlineData(null, null, 0)]
	[InlineData(null, "", 0)] // Null is special-cased into ""
	[InlineData("", null, 0)] // Null is special-cased into ""
	[InlineData("", "", 0)]
	[InlineData("", "A", -1)]
	[InlineData("A", "", +1)]
	[InlineData("A", "a", -1)]
	[InlineData("a", "A", +1)]
	[InlineData("A", "B", -1)]
	[InlineData("AA", "A", +1)]
	public void CompareTo_Regularly_ShouldReturnExpectedResult(string one, string two, int expectedResult)
	{
		var left = (StringId)one;
		var right = (StringId)two;

		Assert.Equal(expectedResult, NormalizeComparisonResult(Comparer<StringId>.Default.Compare(left, right)));
		Assert.Equal(-expectedResult, NormalizeComparisonResult(Comparer<StringId>.Default.Compare(right, left)));
	}

	[Theory]
	[InlineData(null, null, 0)]
	[InlineData(null, "", 0)] // Null is special-cased into ""
	[InlineData("", null, 0)] // Null is special-cased into ""
	[InlineData("", "", 0)]
	[InlineData("", "A", -1)]
	[InlineData("A", "", +1)]
	[InlineData("A", "a", -1)]
	[InlineData("a", "A", +1)]
	[InlineData("A", "B", -1)]
	[InlineData("AA", "A", +1)]
	public void GreaterThan_Regularly_ShouldReturnExpectedResult(string one, string two, int expectedResult)
	{
		var left = (StringId)one;
		var right = (StringId)two;

		Assert.Equal(expectedResult > 0, left > right);
		Assert.Equal(expectedResult <= 0, left <= right);
	}

	[Theory]
	[InlineData(null, null, 0)]
	[InlineData(null, "", 0)] // Null is special-cased into ""
	[InlineData("", null, 0)] // Null is special-cased into ""
	[InlineData("", "", 0)]
	[InlineData("", "A", -1)]
	[InlineData("A", "", +1)]
	[InlineData("A", "a", -1)]
	[InlineData("a", "A", +1)]
	[InlineData("A", "B", -1)]
	[InlineData("AA", "A", +1)]
	public void LessThan_Regularly_ShouldReturnExpectedResult(string one, string two, int expectedResult)
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
		IntId? instance = value is null ? null : new IntId(value.Value);

		if (expectedResult is null)
			Assert.Throws<InvalidOperationException>(() => (int)instance);
		else
			Assert.Equal(expectedResult, (int)instance);
	}

	[Theory]
	[InlineData(null, null)]
	[InlineData(0, 0)]
	[InlineData(1, 1)]
	public void CastToNullableUnderlyingType_Regularly_ShouldReturnExpectedResult(int? value, int? expectedResult)
	{
		IntId? instance = value is null ? null : new IntId(value.Value);

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

		var stringInstance = (StringId)value?.ToString();
		Assert.Equal(value is null ? @"""""" : $@"""{value}""", System.Text.Json.JsonSerializer.Serialize(stringInstance)); // Null is special-cased into ""
	}

	[Theory]
	[InlineData(null)]
	[InlineData(0)]
	[InlineData(1)]
	public void SerializeWithNewtonsoftJson_Regularly_ShouldReturnExpectedResult(int? value)
	{
		var intInstance = (IntId?)value;
		Assert.Equal(value?.ToString() ?? "null", Newtonsoft.Json.JsonConvert.SerializeObject(intInstance));

		var stringInstance = (StringId)value?.ToString();
		Assert.Equal(value is null ? @"""""" : $@"""{value}""", Newtonsoft.Json.JsonConvert.SerializeObject(stringInstance)); // Null is special-cased into ""
	}

	/// <summary>
	/// Longer numeric identities should serialize to a JavaScript-safe JSON value (string), since they are likely to exceeds JavaScript's numeric capacity.
	/// </summary>
	[Theory]
	[InlineData(null)]
	[InlineData(0)]
	[InlineData(1)]
	public void SerializeWithSystemTextJson_WithDecimal_ShouldReturnExpectedResult(int? value)
	{
		// Attempt to mess with the stringification, which should have no effect
		CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");

		var instance = (DecimalId?)value;

		Assert.Equal(value is null ? "null" : $@"""{value}""", System.Text.Json.JsonSerializer.Serialize(instance));
	}

	/// <summary>
	/// Longer numeric identities should serialize to a JavaScript-safe JSON value (string), since they are likely to exceeds JavaScript's numeric capacity.
	/// </summary>
	[Theory]
	[InlineData(null)]
	[InlineData(0)]
	[InlineData(1)]
	public void SerializeWithNewtonsoftJson_WithDecimal_ShouldReturnExpectedResult(int? value)
	{
		// Attempt to mess with the stringification, which should have no effect
		CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("nl-NL");

		var instance = (DecimalId?)value;

		Assert.Equal(value is null ? "null" : $@"""{value}""", System.Text.Json.JsonSerializer.Serialize(instance));
	}

	[Theory]
	[InlineData("null", null)]
	[InlineData("0", 0)]
	[InlineData("1", 1)]
	public void DeserializeWithSystemTextJson_Regularly_ShouldReturnExpectedResult(string json, int? value)
	{
		Assert.Equal(value, System.Text.Json.JsonSerializer.Deserialize<IntId?>(json)?.Value);
		if (value is null)
			Assert.Throws<System.Text.Json.JsonException>(() => System.Text.Json.JsonSerializer.Deserialize<IntId>(json));
		else
			Assert.Equal(value, System.Text.Json.JsonSerializer.Deserialize<IntId>(json).Value);

		json = json == "null" ? json : $@"""{json}""";
		Assert.Equal(value?.ToString(), System.Text.Json.JsonSerializer.Deserialize<StringId?>(json)?.Value);
		Assert.Equal(value?.ToString() ?? "", System.Text.Json.JsonSerializer.Deserialize<StringId>(json).Value); // Null is special-cased into ""
	}

	[Theory]
	[InlineData("null", null)]
	[InlineData("0", 0)]
	[InlineData("1", 1)]
	public void DeserializeWithNewtonsoftJson_Regularly_ShouldReturnExpectedResult(string json, int? value)
	{
		Assert.Equal(value, Newtonsoft.Json.JsonConvert.DeserializeObject<IntId?>(json)?.Value);
		if (value is null)
			Assert.Throws<Newtonsoft.Json.JsonSerializationException>(() => Newtonsoft.Json.JsonConvert.DeserializeObject<IntId>(json));
		else
			Assert.Equal(value, Newtonsoft.Json.JsonConvert.DeserializeObject<IntId>(json).Value);

		json = json == "null" ? json : $@"""{json}""";
		Assert.Equal(value?.ToString(), Newtonsoft.Json.JsonConvert.DeserializeObject<StringId?>(json)?.Value);
		Assert.Equal(value?.ToString() ?? "", Newtonsoft.Json.JsonConvert.DeserializeObject<StringId>(json).Value); // Null is special-cased into ""
	}

	/// <summary>
	/// Longer numeric identities should serialize to a JavaScript-safe JSON value (string), since they are likely to exceeds JavaScript's numeric capacity.
	/// </summary>
	[Theory]
	[InlineData(@"null", null)]
	[InlineData(@"0", 0UL)]
	[InlineData(@"""0""", 0UL)]
	[InlineData(@"1", 1UL)]
	[InlineData(@"""1""", 1UL)]
	[InlineData(@"11111111111111111111", 11111111111111111111UL)]
	[InlineData(@"""11111111111111111111""", 11111111111111111111UL)]
	public void DeserializeWithSystemTextJson_WithDecimal_ShouldReturnExpectedResult(string json, ulong? value)
	{
		// Attempt to mess with the deserialization, which should have no effect
		CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("nl-NL");

		if (value is null)
			Assert.Throws<System.Text.Json.JsonException>(() => System.Text.Json.JsonSerializer.Deserialize<DecimalId>(json));
		else
			Assert.Equal(value.Value, System.Text.Json.JsonSerializer.Deserialize<DecimalId>(json).Value);
	}

	/// <summary>
	/// Longer numeric identities should serialize to a JavaScript-safe JSON value (string), since they are likely to exceeds JavaScript's numeric capacity.
	/// </summary>
	[Theory]
	[InlineData(@"null", null)]
	[InlineData(@"0", 0UL)]
	[InlineData(@"""0""", 0UL)]
	[InlineData(@"1", 1UL)]
	[InlineData(@"""1""", 1UL)]
	[InlineData(@"11111111111111111111", 11111111111111111111UL)]
	[InlineData(@"""11111111111111111111""", 11111111111111111111UL)]
	public void DeserializeWithNewtonsoftJson_WithDecimal_ShouldReturnExpectedResult(string json, ulong? value)
	{
		// Attempt to mess with the deserialization, which should have no effect
		CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("nl-NL");

		if (value is null)
			Assert.Throws<Newtonsoft.Json.JsonSerializationException>(() => Newtonsoft.Json.JsonConvert.DeserializeObject<DecimalId>(json));
		else
			Assert.Equal(value.Value, Newtonsoft.Json.JsonConvert.DeserializeObject<DecimalId>(json).Value);
	}

	/// <summary>
	/// Normalizes the result of a comparison operation to either -1, 0, or +1.
	/// </summary>
	private static int NormalizeComparisonResult(int result)
	{
		if (result < 0) return -1;
		if (result > 0) return +1;
		return 0;
	}

	private sealed class StringBasedEntity : Entity<StringId, string>
	{
		public StringBasedEntity(StringId id)
			: base(id)
		{
		}
	}

	private sealed class IntBasedEntity : Entity<IntId, int>
	{
		public IntBasedEntity(IntId id)
			: base(id)
		{
		}
	}

	private sealed class DecimalBasedEntity : Entity<DecimalId, decimal>
	{
		public DecimalBasedEntity(DecimalId id)
			: base(id)
		{
		}
	}

	public sealed class ObjectBasedEntity : Entity<ObjectId, ComparableObject>
	{
		public ObjectBasedEntity(ObjectId id)
			: base(id)
		{
		}
	}

	public sealed class ComparableObject : IEquatable<ComparableObject>, IComparable<ComparableObject>
	{
		public override int GetHashCode() => 1;
		public override bool Equals(object obj) => obj is ComparableObject other && this.Equals(other);
		public bool Equals(ComparableObject other) => other is not null;
		public int CompareTo(ComparableObject other) => other is null ? +1 : 0;
	}
}
