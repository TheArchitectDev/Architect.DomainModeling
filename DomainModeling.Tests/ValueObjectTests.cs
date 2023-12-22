using System.Collections;
using System.Collections.Immutable;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using Architect.DomainModeling.Tests.ValueObjectTestTypes;
using Xunit;

namespace Architect.DomainModeling.Tests
{
	public class ValueObjectTests
	{
		private const string SurrogatePair = "💩";
		private static readonly char SurrogateLeftChar = SurrogatePair[0];
		private static readonly char SurrogateRightChar = SurrogatePair[1];

		[Fact]
		public void ContainsNonAlphanumericCharacters_Regularly_ShouldReturnExpectedResult()
		{
			for (var i = 0; i < 256; i++)
			{
				var chr = (char)i;

				var isAlphanumeric = (chr >= '0' && chr <= '9') || (chr >= 'A' && chr <= 'Z') || (chr >= 'a' && chr <= 'z');
				var result = ManualValueObject.ContainsNonAlphanumericCharacters(chr.ToString());

				if (isAlphanumeric)
					Assert.False(result, $"{nameof(ManualValueObject.ContainsNonAlphanumericCharacters)} for '{chr}' ({i}) should have been false, but was true.");
				else
					Assert.True(result, $"{nameof(ManualValueObject.ContainsNonAlphanumericCharacters)} for '{chr}' ({i}) should have been true, but was false.");

				var longVersion = new string(chr, count: 33);
				var longResult = ManualValueObject.ContainsNonAlphanumericCharacters(longVersion);
				Assert.Equal(result, longResult);
			}

			Assert.True(ManualValueObject.ContainsNonAlphanumericCharacters('ë'.ToString()), $"{nameof(ManualValueObject.ContainsNonAlphanumericCharacters)} for 'ë' ({(int)'ë'}) should have been true, but was false.");
			Assert.True(ManualValueObject.ContainsNonAlphanumericCharacters('α'.ToString()), $"{nameof(ManualValueObject.ContainsNonAlphanumericCharacters)} for 'α' ({(int)'α'}) should have been true, but was false.");
			Assert.True(ManualValueObject.ContainsNonAlphanumericCharacters(SurrogatePair), $"{nameof(ManualValueObject.ContainsNonAlphanumericCharacters)} for {SurrogatePair} should have been true, but was false.");
			Assert.True(ManualValueObject.ContainsNonAlphanumericCharacters(SurrogateLeftChar.ToString()), $"{nameof(ManualValueObject.ContainsNonAlphanumericCharacters)} for '{SurrogateLeftChar}' ({(int)SurrogateLeftChar}) should have been true, but was false.");
			Assert.True(ManualValueObject.ContainsNonAlphanumericCharacters(SurrogateRightChar.ToString()), $"{nameof(ManualValueObject.ContainsNonAlphanumericCharacters)} for '{SurrogateRightChar}' ({(int)SurrogateRightChar}) should have been true, but was false.");
		}

		[Theory]
		[InlineData(" 12345678901234561234567890123456", true)]
		[InlineData("12345678901234561234567890123456 ", true)]
		[InlineData("_12345678901234561234567890123456", true)]
		[InlineData("12345678901234561234567890123456_", true)]
		[InlineData("12345678901234561234567890123456ë", true)]
		[InlineData("12345678901234561234567890123456💩", true)]
		[InlineData("1234567890123456123456789012345💩", true)]
		[InlineData("123456789012345612345678901234💩", true)]
		[InlineData("💩12345678901234561234567890123456", true)]
		[InlineData("12345678901234💩561234567890123456", true)]
		public void ContainsNonAlphanumericCharacters_WithLongInput_ShouldReturnExpectedResult(string text, bool expectedResult)
		{
			var result = ManualValueObject.ContainsNonAlphanumericCharacters(text);

			Assert.Equal(expectedResult, result);
		}

		[Fact]
		public void ContainsNonWordCharacters_Regularly_ShouldReturnExpectedResult()
		{
			for (var i = 0; i < 256; i++)
			{
				var chr = (char)i;

				var isWord = (chr >= '0' && chr <= '9') || (chr >= 'A' && chr <= 'Z') || (chr >= 'a' && chr <= 'z') || chr == '_';
				var result = ManualValueObject.ContainsNonWordCharacters(chr.ToString());

				if (isWord)
					Assert.False(result, $"{nameof(ManualValueObject.ContainsNonWordCharacters)} for '{chr}' ({i}) should have been false, but was true.");
				else
					Assert.True(result, $"{nameof(ManualValueObject.ContainsNonWordCharacters)} for '{chr}' ({i}) should have been true, but was false.");

				var longVersion = new string(chr, count: 33);
				var longResult = ManualValueObject.ContainsNonWordCharacters(longVersion);
				Assert.Equal(result, longResult);
			}

			Assert.True(ManualValueObject.ContainsNonWordCharacters('ë'.ToString()), $"{nameof(ManualValueObject.ContainsNonWordCharacters)} for 'ë' ({(int)'ë'}) should have been true, but was false.");
			Assert.True(ManualValueObject.ContainsNonWordCharacters('α'.ToString()), $"{nameof(ManualValueObject.ContainsNonWordCharacters)} for 'α' ({(int)'α'}) should have been true, but was false.");
			Assert.True(ManualValueObject.ContainsNonWordCharacters(SurrogatePair), $"{nameof(ManualValueObject.ContainsNonWordCharacters)} for {SurrogatePair} should have been true, but was false.");
			Assert.True(ManualValueObject.ContainsNonWordCharacters(SurrogateLeftChar.ToString()), $"{nameof(ManualValueObject.ContainsNonWordCharacters)} for '{SurrogateLeftChar}' ({(int)SurrogateLeftChar}) should have been true, but was false.");
			Assert.True(ManualValueObject.ContainsNonWordCharacters(SurrogateRightChar.ToString()), $"{nameof(ManualValueObject.ContainsNonWordCharacters)} for '{SurrogateRightChar}' ({(int)SurrogateRightChar}) should have been true, but was false.");
		}

		[Theory]
		[InlineData(" 12345678901234561234567890123456", true)]
		[InlineData("12345678901234561234567890123456 ", true)]
		[InlineData("_12345678901234561234567890123456", false)]
		[InlineData("12345678901234561234567890123456_", false)]
		[InlineData("12345678901234561234567890123456ë", true)]
		[InlineData("12345678901234561234567890123456💩", true)]
		[InlineData("1234567890123456123456789012345💩", true)]
		[InlineData("123456789012345612345678901234💩", true)]
		[InlineData("💩12345678901234561234567890123456", true)]
		[InlineData("12345678901234💩561234567890123456", true)]
		public void ContainsNonWordCharacters_WithLongInput_ShouldReturnExpectedResult(string text, bool expectedResult)
		{
			var result = ManualValueObject.ContainsNonWordCharacters(text);

			Assert.Equal(expectedResult, result);
		}

		[Fact]
		public void ContainsNonAsciiCharacters_Regularly_ShouldReturnExpectedResult()
		{
			for (var i = 0; i < 256; i++)
			{
				var chr = (char)i;

				var isAscii = chr <= SByte.MaxValue;
				var result = ManualValueObject.ContainsNonAsciiCharacters(chr.ToString());

				if (isAscii)
					Assert.False(result, $"{nameof(ManualValueObject.ContainsNonAsciiCharacters)} for '{chr}' ({i}) should have been false, but was true.");
				else
					Assert.True(result, $"{nameof(ManualValueObject.ContainsNonAsciiCharacters)} for '{chr}' ({i}) should have been true, but was false.");

				var longVersion = new string(chr, count: 33);
				var longResult = ManualValueObject.ContainsNonAsciiCharacters(longVersion);
				Assert.Equal(result, longResult);
			}

			Assert.True(ManualValueObject.ContainsNonAsciiCharacters('ë'.ToString()), $"{nameof(ManualValueObject.ContainsNonAsciiCharacters)} for 'ë' ({(int)'ë'}) should have been true, but was false.");
			Assert.True(ManualValueObject.ContainsNonAsciiCharacters('α'.ToString()), $"{nameof(ManualValueObject.ContainsNonAsciiCharacters)} for 'α' ({(int)'α'}) should have been true, but was false.");
			Assert.True(ManualValueObject.ContainsNonAsciiCharacters(SurrogatePair), $"{nameof(ManualValueObject.ContainsNonAsciiCharacters)} for {SurrogatePair} should have been true, but was false.");
			Assert.True(ManualValueObject.ContainsNonAsciiCharacters(SurrogateLeftChar.ToString()), $"{nameof(ManualValueObject.ContainsNonAsciiCharacters)} for '{SurrogateLeftChar}' ({(int)SurrogateLeftChar}) should have been true, but was false.");
			Assert.True(ManualValueObject.ContainsNonAsciiCharacters(SurrogateRightChar.ToString()), $"{nameof(ManualValueObject.ContainsNonAsciiCharacters)} for '{SurrogateRightChar}' ({(int)SurrogateRightChar}) should have been true, but was false.");
		}

		[Theory]
		[InlineData(" 12345678901234561234567890123456", false)]
		[InlineData("12345678901234561234567890123456 ", false)]
		[InlineData("_12345678901234561234567890123456", false)]
		[InlineData("12345678901234561234567890123456_", false)]
		[InlineData("12345678901234561234567890123456ë", true)]
		[InlineData("12345678901234561234567890123456💩", true)]
		[InlineData("1234567890123456123456789012345💩", true)]
		[InlineData("123456789012345612345678901234💩", true)]
		[InlineData("💩12345678901234561234567890123456", true)]
		[InlineData("12345678901234💩561234567890123456", true)]
		public void ContainsNonAsciiCharacters_WithLongInput_ShouldReturnExpectedResult(string text, bool expectedResult)
		{
			var result = ManualValueObject.ContainsNonAsciiCharacters(text);

			Assert.Equal(expectedResult, result);
		}

		[Fact]
		public void ContainsNonAsciiOrNonPrintableCharacters_Regularly_ShouldReturnExpectedResult()
		{
			for (var i = 0; i < 256; i++)
			{
				var chr = (char)i;

				var isPrintableAscii = chr >= ' ' && chr < SByte.MaxValue;
				var result = ManualValueObject.ContainsNonAsciiOrNonPrintableCharacters(chr.ToString());

				if (isPrintableAscii)
					Assert.False(result, $"{nameof(ManualValueObject.ContainsNonAsciiOrNonPrintableCharacters)} for '{chr}' ({i}) should have been false, but was true.");
				else
					Assert.True(result, $"{nameof(ManualValueObject.ContainsNonAsciiOrNonPrintableCharacters)} for '{chr}' ({i}) should have been true, but was false.");

				var longVersion = new string(chr, count: 33);
				var longResult = ManualValueObject.ContainsNonAsciiOrNonPrintableCharacters(longVersion);
				Assert.Equal(result, longResult);
			}

			Assert.True(ManualValueObject.ContainsNonAsciiOrNonPrintableCharacters('ë'.ToString()), $"{nameof(ManualValueObject.ContainsNonAsciiOrNonPrintableCharacters)} for 'ë' ({(int)'ë'}) should have been true, but was false.");
			Assert.True(ManualValueObject.ContainsNonAsciiOrNonPrintableCharacters('α'.ToString()), $"{nameof(ManualValueObject.ContainsNonAsciiOrNonPrintableCharacters)} for 'α' ({(int)'α'}) should have been true, but was false.");
			Assert.True(ManualValueObject.ContainsNonAsciiOrNonPrintableCharacters(SurrogatePair), $"{nameof(ManualValueObject.ContainsNonAsciiOrNonPrintableCharacters)} for {SurrogatePair} should have been true, but was false.");
			Assert.True(ManualValueObject.ContainsNonAsciiOrNonPrintableCharacters(SurrogateLeftChar.ToString()), $"{nameof(ManualValueObject.ContainsNonAsciiOrNonPrintableCharacters)} for '{SurrogateLeftChar}' ({(int)SurrogateLeftChar}) should have been true, but was false.");
			Assert.True(ManualValueObject.ContainsNonAsciiOrNonPrintableCharacters(SurrogateRightChar.ToString()), $"{nameof(ManualValueObject.ContainsNonAsciiOrNonPrintableCharacters)} for '{SurrogateRightChar}' ({(int)SurrogateRightChar}) should have been true, but was false.");
		}

		[Theory]
		[InlineData(" 12345678901234561234567890123456", false)]
		[InlineData("12345678901234561234567890123456 ", false)]
		[InlineData("_12345678901234561234567890123456", false)]
		[InlineData("12345678901234561234567890123456_", false)]
		[InlineData("12345678901234561234567890123456ë", true)]
		[InlineData("12345678901234561234567890123456💩", true)]
		[InlineData("1234567890123456123456789012345💩", true)]
		[InlineData("123456789012345612345678901234💩", true)]
		[InlineData("💩12345678901234561234567890123456", true)]
		[InlineData("12345678901234💩561234567890123456", true)]
		[InlineData("12345678901234561234567890123456", true)] // Ends with an invisible control character
		[InlineData("12345678901234561234567890123456\0", true)]
		[InlineData("1234567890123456123456789012345\0", true)]
		[InlineData("123456789012345612345678901234\0", true)]
		[InlineData("\012345678901234561234567890123456", true)]
		[InlineData("12345678901234\0561234567890123456", true)]
		[InlineData("\n12345678901234561234567890123456", true)]
		[InlineData("12345678901234\n561234567890123456", true)]
		public void ContainsNonAsciiOrNonPrintableCharacters_WithLongInput_ShouldReturnExpectedResult(string text, bool expectedResult)
		{
			var result = ManualValueObject.ContainsNonAsciiOrNonPrintableCharacters(text);

			Assert.Equal(expectedResult, result);
		}

		[Fact]
		public void ContainsNonAsciiOrNonPrintableCharacters_WithoutFlagNewLinesAndTabs_ShouldReturnExpectedResult()
		{
			for (var i = 0; i < 256; i++)
			{
				var chr = (char)i;

				var isPrintableAscii = chr >= ' ' && chr < SByte.MaxValue;
				var isPrintableAsciiOrNewLineOrTab = isPrintableAscii || chr == '\r' || chr == '\n' || chr == '\t';
				var result = ManualValueObject.ContainsNonAsciiOrNonPrintableCharacters(chr.ToString(), flagNewLinesAndTabs: false);

				if (isPrintableAsciiOrNewLineOrTab)
					Assert.False(result, $"{nameof(ManualValueObject.ContainsNonAsciiOrNonPrintableCharacters)} (allowing newlines and tabs) for '{chr}' ({i}) should have been false, but was true.");
				else
					Assert.True(result, $"{nameof(ManualValueObject.ContainsNonAsciiOrNonPrintableCharacters)} (allowing newlines and tabs) for '{chr}' ({i}) should have been true, but was false.");

				var longVersion = new string(chr, count: 33);
				var longResult = ManualValueObject.ContainsNonAsciiOrNonPrintableCharacters(longVersion, flagNewLinesAndTabs: false);
				Assert.Equal(result, longResult);
			}
		}

		[Theory]
		[InlineData(" 12345678901234561234567890123456", false)]
		[InlineData("12345678901234561234567890123456 ", false)]
		[InlineData("_12345678901234561234567890123456", false)]
		[InlineData("12345678901234561234567890123456_", false)]
		[InlineData("12345678901234561234567890123456ë", true)]
		[InlineData("12345678901234561234567890123456💩", true)]
		[InlineData("1234567890123456123456789012345💩", true)]
		[InlineData("123456789012345612345678901234💩", true)]
		[InlineData("💩12345678901234561234567890123456", true)]
		[InlineData("12345678901234💩561234567890123456", true)]
		[InlineData("12345678901234561234567890123456", true)] // Ends with an invisible control character
		[InlineData("12345678901234561234567890123456\0", true)]
		[InlineData("1234567890123456123456789012345\0", true)]
		[InlineData("123456789012345612345678901234\0", true)]
		[InlineData("\012345678901234561234567890123456", true)]
		[InlineData("12345678901234\0561234567890123456", true)]
		[InlineData("\n12345678901234561234567890123456", false)]
		[InlineData("12345678901234\n561234567890123456", false)]
		public void ContainsNonAsciiOrNonPrintableCharacters_WithoutFlagNewLinesAndTabsAndWithLongInput_ShouldReturnExpectedResult(string text, bool expectedResult)
		{
			var result = ManualValueObject.ContainsNonAsciiOrNonPrintableCharacters(text, flagNewLinesAndTabs: false);

			Assert.Equal(expectedResult, result);
		}

		[Fact]
		public void ContainsNonAsciiOrNonPrintableOrWhitespaceCharacters_Regularly_ShouldReturnExpectedResult()
		{
			for (var i = 0; i < 256; i++)
			{
				var chr = (char)i;

				var isPrintableNonWhitespaceAscii = chr > ' ' && chr < SByte.MaxValue;
				var result = ManualValueObject.ContainsNonAsciiOrNonPrintableOrWhitespaceCharacters(chr.ToString());

				if (isPrintableNonWhitespaceAscii)
					Assert.False(result, $"{nameof(ManualValueObject.ContainsNonAsciiOrNonPrintableOrWhitespaceCharacters)} for '{chr}' ({i}) should have been false, but was true.");
				else
					Assert.True(result, $"{nameof(ManualValueObject.ContainsNonAsciiOrNonPrintableOrWhitespaceCharacters)} for '{chr}' ({i}) should have been true, but was false.");

				var longVersion = new string(chr, count: 33);
				var longResult = ManualValueObject.ContainsNonAsciiOrNonPrintableOrWhitespaceCharacters(longVersion);
				Assert.Equal(result, longResult);
			}

			Assert.True(ManualValueObject.ContainsNonAsciiOrNonPrintableOrWhitespaceCharacters('ë'.ToString()), $"{nameof(ManualValueObject.ContainsNonAsciiOrNonPrintableOrWhitespaceCharacters)} for 'ë' ({(int)'ë'}) should have been true, but was false.");
			Assert.True(ManualValueObject.ContainsNonAsciiOrNonPrintableOrWhitespaceCharacters('α'.ToString()), $"{nameof(ManualValueObject.ContainsNonAsciiOrNonPrintableOrWhitespaceCharacters)} for 'α' ({(int)'α'}) should have been true, but was false.");
			Assert.True(ManualValueObject.ContainsNonAsciiOrNonPrintableOrWhitespaceCharacters(SurrogatePair), $"{nameof(ManualValueObject.ContainsNonAsciiOrNonPrintableOrWhitespaceCharacters)} for {SurrogatePair} should have been true, but was false.");
			Assert.True(ManualValueObject.ContainsNonAsciiOrNonPrintableOrWhitespaceCharacters(SurrogateLeftChar.ToString()), $"{nameof(ManualValueObject.ContainsNonAsciiOrNonPrintableOrWhitespaceCharacters)} for '{SurrogateLeftChar}' ({(int)SurrogateLeftChar}) should have been true, but was false.");
			Assert.True(ManualValueObject.ContainsNonAsciiOrNonPrintableOrWhitespaceCharacters(SurrogateRightChar.ToString()), $"{nameof(ManualValueObject.ContainsNonAsciiOrNonPrintableOrWhitespaceCharacters)} for '{SurrogateRightChar}' ({(int)SurrogateRightChar}) should have been true, but was false.");
		}

		[Theory]
		[InlineData(" 12345678901234561234567890123456", true)]
		[InlineData("12345678901234561234567890123456 ", true)]
		[InlineData("_12345678901234561234567890123456", false)]
		[InlineData("12345678901234561234567890123456_", false)]
		[InlineData("12345678901234561234567890123456ë", true)]
		[InlineData("12345678901234561234567890123456💩", true)]
		[InlineData("1234567890123456123456789012345💩", true)]
		[InlineData("123456789012345612345678901234💩", true)]
		[InlineData("💩12345678901234561234567890123456", true)]
		[InlineData("12345678901234💩561234567890123456", true)]
		[InlineData("12345678901234561234567890123456", true)] // Ends with an invisible control character
		[InlineData("12345678901234561234567890123456\0", true)]
		[InlineData("1234567890123456123456789012345\0", true)]
		[InlineData("123456789012345612345678901234\0", true)]
		[InlineData("\012345678901234561234567890123456", true)]
		[InlineData("12345678901234\0561234567890123456", true)]
		[InlineData("\n12345678901234561234567890123456", true)]
		[InlineData("12345678901234\n561234567890123456", true)]
		public void ContainsNonAsciiOrNonPrintableOrWhitespaceCharacters_WithLongInput_ShouldReturnExpectedResult(string text, bool expectedResult)
		{
			var result = ManualValueObject.ContainsNonAsciiOrNonPrintableOrWhitespaceCharacters(text);

			Assert.Equal(expectedResult, result);
		}

		[Fact]
		public void ContainsNonPrintableCharacters_WithFlagNewLinesAndTabs_ShouldReturnExpectedResult()
		{
			for (var i = 0; i < UInt16.MaxValue; i++)
			{
				var chr = (char)i;

				var category = Char.GetUnicodeCategory(chr);
				var isPrintable = category != UnicodeCategory.Control && category != UnicodeCategory.PrivateUse && category != UnicodeCategory.OtherNotAssigned;

				var span = MemoryMarshal.CreateReadOnlySpan(ref chr, length: 1);
				var result = ManualValueObject.ContainsNonPrintableCharacters(span, flagNewLinesAndTabs: true);

				if (isPrintable)
					Assert.False(result, $"{nameof(ManualValueObject.ContainsNonPrintableCharacters)} (disallowing newlines and tabs) for '{chr}' ({i}) should have been false, but was true.");
				else
					Assert.True(result, $"{nameof(ManualValueObject.ContainsNonPrintableCharacters)} (disallowing newlines and tabs) for '{chr}' ({i}) should have been true, but was false.");

				var longVersion = new string(chr, count: 33);
				var longResult = ManualValueObject.ContainsNonPrintableCharacters(longVersion, flagNewLinesAndTabs: true);
				Assert.Equal(result, longResult);
			}
		}

		[Fact]
		public void ContainsNonPrintableCharacters_WithoutFlagNewLinesAndTabs_ShouldReturnExpectedResult()
		{
			for (var i = 0; i < UInt16.MaxValue; i++)
			{
				var chr = (char)i;

				var category = Char.GetUnicodeCategory(chr);
				var isPrintable = category != UnicodeCategory.Control && category != UnicodeCategory.PrivateUse && category != UnicodeCategory.OtherNotAssigned;
				isPrintable = isPrintable || chr == '\r' || chr == '\n' || chr == '\t';

				var span = MemoryMarshal.CreateReadOnlySpan(ref chr, length: 1);
				var result = ManualValueObject.ContainsNonPrintableCharacters(span, flagNewLinesAndTabs: false);

				if (isPrintable)
					Assert.False(result, $"{nameof(ManualValueObject.ContainsNonPrintableCharacters)} (allowing newlines and tabs) for '{chr}' ({i}) should have been false, but was true.");
				else
					Assert.True(result, $"{nameof(ManualValueObject.ContainsNonPrintableCharacters)} (allowing newlines and tabs) for '{chr}' ({i}) should have been true, but was false.");

				var longVersion = new string(chr, count: 33);
				var longResult = ManualValueObject.ContainsNonPrintableCharacters(longVersion, flagNewLinesAndTabs: false);
				Assert.Equal(result, longResult);
			}
		}

		[Theory]
		[InlineData("_12345678901234561234567890123456", false)]
		[InlineData("12345678901234561234567890123456_", false)]
		[InlineData("12345678901234561234567890123456ë", false)]
		[InlineData("12345678901234561234567890123456💩", false)]
		[InlineData("1234567890123456123456789012345💩", false)]
		[InlineData("123456789012345612345678901234💩", false)]
		[InlineData("💩12345678901234561234567890123456", false)]
		[InlineData("12345678901234💩561234567890123456", false)]
		[InlineData("12345678901234561234567890123456", true)] // Ends with an invisible control character
		[InlineData("12345678901234561234567890123456\0", true)]
		[InlineData("1234567890123456123456789012345\0", true)]
		[InlineData("123456789012345612345678901234\0", true)]
		[InlineData("\012345678901234561234567890123456", true)]
		[InlineData("12345678901234\0561234567890123456", true)]
		public void ContainsNonPrintableCharacters_WithLongInput_ShouldReturnExpectedResult(string text, bool expectedResult)
		{
			var result = ManualValueObject.ContainsNonPrintableCharacters(text, flagNewLinesAndTabs: true);

			Assert.Equal(expectedResult, result);
		}

		[Fact]
		public void ContainsWhitespaceOrNonPrintableCharacters_Regularly_ShouldReturnExpectedResult()
		{
			for (var i = 0; i < UInt16.MaxValue; i++)
			{
				var chr = (char)i;

				var category = Char.GetUnicodeCategory(chr);
				var mismatches = category != UnicodeCategory.SpaceSeparator && category != UnicodeCategory.LineSeparator && category != UnicodeCategory.ParagraphSeparator &&
					category != UnicodeCategory.Control && category != UnicodeCategory.PrivateUse && category != UnicodeCategory.OtherNotAssigned;

				var span = MemoryMarshal.CreateReadOnlySpan(ref chr, length: 1);
				var result = ManualValueObject.ContainsWhitespaceOrNonPrintableCharacters(span);

				if (mismatches)
					Assert.False(result, $"{nameof(ManualValueObject.ContainsNonPrintableCharacters)} (disallowing newlines and tabs) for '{chr}' ({i}) should have been false, but was true.");
				else
					Assert.True(result, $"{nameof(ManualValueObject.ContainsNonPrintableCharacters)} (disallowing newlines and tabs) for '{chr}' ({i}) should have been true, but was false.");

				var longVersion = new string(chr, count: 33);
				var longResult = ManualValueObject.ContainsWhitespaceOrNonPrintableCharacters(longVersion);
				Assert.Equal(result, longResult);
			}
		}

		[Fact]
		public void ContainsNonPrintableCharactersOrDoubleQuotes_WithFlagNewLinesAndTabs_ShouldReturnExpectedResult()
		{
			for (var i = 0; i < UInt16.MaxValue; i++)
			{
				var chr = (char)i;

				var category = Char.GetUnicodeCategory(chr);
				var isPrintable = category != UnicodeCategory.Control && category != UnicodeCategory.PrivateUse && category != UnicodeCategory.OtherNotAssigned;
				var isNotDoubleQuote = chr != '"';

				var span = MemoryMarshal.CreateReadOnlySpan(ref chr, length: 1);
				var result = ManualValueObject.ContainsNonPrintableCharactersOrDoubleQuotes(span, flagNewLinesAndTabs: true);

				if (isPrintable && isNotDoubleQuote)
					Assert.False(result, $"{nameof(ManualValueObject.ContainsNonPrintableCharactersOrDoubleQuotes)} (disallowing newlines and tabs) for '{chr}' ({i}) should have been false, but was true.");
				else
					Assert.True(result, $"{nameof(ManualValueObject.ContainsNonPrintableCharactersOrDoubleQuotes)} (disallowing newlines and tabs) for '{chr}' ({i}) should have been true, but was false.");

				var longVersion = new string(chr, count: 33);
				var longResult = ManualValueObject.ContainsNonPrintableCharactersOrDoubleQuotes(longVersion, flagNewLinesAndTabs: true);
				Assert.Equal(result, longResult);
			}
		}

		[Fact]
		public void ContainsNonPrintableCharactersOrDoubleQuotes_WithoutFlagNewLinesAndTabs_ShouldReturnExpectedResult()
		{
			for (var i = 0; i < UInt16.MaxValue; i++)
			{
				var chr = (char)i;

				var category = Char.GetUnicodeCategory(chr);
				var isPrintable = category != UnicodeCategory.Control && category != UnicodeCategory.PrivateUse && category != UnicodeCategory.OtherNotAssigned;
				isPrintable = isPrintable || chr == '\r' || chr == '\n' || chr == '\t';
				var isNotDoubleQuote = chr != '"';

				var span = MemoryMarshal.CreateReadOnlySpan(ref chr, length: 1);
				var result = ManualValueObject.ContainsNonPrintableCharactersOrDoubleQuotes(span, flagNewLinesAndTabs: false);

				if (isPrintable && isNotDoubleQuote)
					Assert.False(result, $"{nameof(ManualValueObject.ContainsNonPrintableCharactersOrDoubleQuotes)} (allowing newlines and tabs) for '{chr}' ({i}) should have been false, but was true.");
				else
					Assert.True(result, $"{nameof(ManualValueObject.ContainsNonPrintableCharactersOrDoubleQuotes)} (allowing newlines and tabs) for '{chr}' ({i}) should have been true, but was false.");

				var longVersion = new string(chr, count: 33);
				var longResult = ManualValueObject.ContainsNonPrintableCharactersOrDoubleQuotes(longVersion, flagNewLinesAndTabs: false);
				Assert.Equal(result, longResult);
			}
		}

		[Theory]
		[InlineData("_12345678901234561234567890123456", false)]
		[InlineData("12345678901234561234567890123456_", false)]
		[InlineData("12345678901234561234567890123456ë", false)]
		[InlineData("12345678901234561234567890123456💩", false)]
		[InlineData("1234567890123456123456789012345💩", false)]
		[InlineData("123456789012345612345678901234💩", false)]
		[InlineData("💩12345678901234561234567890123456", false)]
		[InlineData("12345678901234💩561234567890123456", false)]
		[InlineData("12345678901234561234567890123456", true)] // Ends with an invisible control character
		[InlineData("12345678901234561234567890123456\0", true)]
		[InlineData("1234567890123456123456789012345\0", true)]
		[InlineData("123456789012345612345678901234\0", true)]
		[InlineData("\012345678901234561234567890123456", true)]
		[InlineData("12345678901234561234567890123456\"", true)]
		[InlineData("1234567890123456123456789012345\"", true)]
		[InlineData("123456789012345612345678901234\"", true)]
		[InlineData("12345678901234\0561234567890123456", true)]
		[InlineData("\"12345678901234561234567890123456", true)]
		[InlineData("12345678901234\"561234567890123456", true)]
		public void ContainsNonPrintableCharactersOrDoubleQuotes_WithLongInput_ShouldReturnExpectedResult(string text, bool expectedResult)
		{
			var result = ManualValueObject.ContainsNonPrintableCharactersOrDoubleQuotes(text, flagNewLinesAndTabs: true);

			Assert.Equal(expectedResult, result);
		}

		[Theory]
		[InlineData("_12345678901234561234567890123456", false)]
		[InlineData("12345678901234561234567890123456_", false)]
		[InlineData("12345678901234561234567890123456ë", false)]
		[InlineData("12345678901234561234567890123456💩", false)]
		[InlineData("1234567890123456123456789012345💩", false)]
		[InlineData("123456789012345612345678901234💩", false)]
		[InlineData("💩12345678901234561234567890123456", false)]
		[InlineData("12345678901234💩561234567890123456", false)]
		[InlineData("12345678901234561234567890123456", true)] // Ends with an invisible control character
		[InlineData("12345678901234561234567890123456\0", true)]
		[InlineData("1234567890123456123456789012345\0", true)]
		[InlineData("123456789012345612345678901234\0", true)]
		[InlineData("\012345678901234561234567890123456", true)]
		[InlineData("12345678901234\0561234567890123456", true)]
		[InlineData(" 12345678901234561234567890123456", true)]
		[InlineData("12345678901234 561234567890123456", true)]
		public void ContainsWhitespaceOrNonPrintableCharacters_WithLongInput_ShouldReturnExpectedResult(string text, bool expectedResult)
		{
			var result = ManualValueObject.ContainsWhitespaceOrNonPrintableCharacters(text);

			Assert.Equal(expectedResult, result);
		}

		[Fact]
		public void StringComparison_WithNonStringType_ShouldThrow()
		{
			var instance = new IntValue(default, default);

			Assert.Throws<NotSupportedException>(() => instance.GetStringComparison());
		}

		[Fact]
		public void StringComparison_WithStringType_ShouldReturnExpectedResult()
		{
			var instance = new DefaultComparingStringValue("");

			Assert.Equal(StringComparison.Ordinal, instance.GetStringComparison());
		}

		[Fact]
		public void StringComparison_Overridden_ShouldReturnExpectedResult()
		{
			var instance = new StringValue("", "");

			Assert.Equal(StringComparison.OrdinalIgnoreCase, instance.GetStringComparison());
		}

		[Fact]
		public void ToString_Regularly_ShouldReturnExpectedResult()
		{
			Assert.Equal("{IntValue One=1 Two=1}", new IntValue(1, 1).ToString());
			Assert.Equal(@"{StringValue One=A Two=a}", new StringValue("A", "a").ToString());
		}

		[Fact]
		public void GetHashCode_Regulary_ShouldReturnExpectedResult()
		{
			Assert.Equal(HashCode.Combine(1, 1), new IntValue(1, 1).GetHashCode());
		}

		[Fact]
		public void GetHashCode_WithIgnoreCaseString_ShouldReturnExpectedResult()
		{
			var one = new StringValue("A", "a").GetHashCode();
			var two = new StringValue("a", "A").GetHashCode();
			Assert.Equal(one, two);
		}

		[Fact]
		public void GetHashCode_WithImmutableArray_ShouldReturnExpectedResult()
		{
			var one = new ImmutableArrayValueObject(new[] { "A" }).GetHashCode();
			var two = new ImmutableArrayValueObject(new[] { "A" }).GetHashCode();
			Assert.Equal(one, two);

			var three = new ImmutableArrayValueObject(new[] { "a" }).GetHashCode();
			Assert.NotEqual(one, three); // Note that the collection elements define their own GetHashCode() and do not care about the parent ValueObject's StringComparison value, by design
		}

		[Theory]
		[InlineData(null, null, true)] // Implementation should still handle null left operand as expected
		[InlineData(null, "", false)] // Custom collection's hash code always returns 1
		[InlineData("", null, false)] // Custom collection's hash code always returns 1
		[InlineData("A", "A", true)] // Custom collection's hash code always returns 1
		[InlineData("A", "B", true)] // Custom collection's hash code always returns 1
		public void GetHashCode_WithCustomEquatableCollection_ShouldHonorItsOverride(string? one, string? two, bool expectedResult)
		{
			var left = new CustomCollectionValueObject() { Values = one is null ? null : new CustomCollectionValueObject.CustomCollection(one) };
			var right = new CustomCollectionValueObject() { Values = two is null ? null : new CustomCollectionValueObject.CustomCollection(two) };

			var leftHashCode = left.GetHashCode();
			var rightHashCode = right.GetHashCode();

			Assert.Equal(expectedResult, leftHashCode == rightHashCode);
		}

		[Fact]
		public void Equals_WithNullValueVsNull_ShouldReturnExpectedResult()
		{
			var nullValued = new DefaultComparingStringValue(value: null);

			Assert.False(nullValued.Equals(null));
		}

		[Theory]
		[InlineData(0, 0, true)]
		[InlineData(0, 1, false)]
		[InlineData(1, -1, false)]
		public void Equals_Regularly_ShouldReturnExpectedResult(int one, int two, bool expectedResult)
		{
			var left = new IntValue(7, one);
			var right = new IntValue(7, two);
			Assert.Equal(expectedResult, left.Equals(right));

			left = new IntValue(7, one);
			right = new IntValue(-7, two);
			Assert.False(left.Equals(right));

			left = new IntValue(one, 7);
			right = new IntValue(two, -7);
			Assert.False(left.Equals(right));
		}

		[Theory]
		[InlineData("", "", true)]
		[InlineData("A", "A", true)]
		[InlineData("A", "a", true)]
		[InlineData("A", "B", false)]
		public void Equals_WithIgnoreCaseString_ShouldReturnExpectedResult(string one, string two, bool expectedResult)
		{
			var left = new StringValue("7", one);
			var right = new StringValue("7", two);
			Assert.Equal(expectedResult, left.Equals(right));

			left = new StringValue("7", one);
			right = new StringValue("-7", two);
			Assert.False(left.Equals(right));

			left = new StringValue(one, "7");
			right = new StringValue(two, "-7");
			Assert.False(left.Equals(right));
		}

		[Theory]
		[InlineData("", "", true)]
		[InlineData("A", "A", true)]
		[InlineData("A", "a", false)] // Note that the collection elements define their own Equals() and do not care about the parent ValueObject's StringComparison value, by design
		[InlineData("A", "B", false)]
		public void Equals_WithImmutableArray_ShouldReturnExpectedResult(string one, string two, bool expectedResult)
		{
			var left = new ImmutableArrayValueObject(new[] { one });
			var right = new ImmutableArrayValueObject(new[] { two });
			Assert.Equal(expectedResult, left.Equals(right));
			Assert.Equal(expectedResult, right.Equals(left));
		}

		[Theory]
		[InlineData(null, null, true)] // Implementation should still handle null left operand as expected
		[InlineData(null, "", false)] // Implementation should still handle null left operand as expected
		[InlineData("", null, true)] // Custom collection's equality always returns true
		[InlineData("A", "A", true)] // Custom collection's equality always returns true
		[InlineData("A", "B", true)] // Custom collection's equality always returns true
		public void Equals_WithCustomEquatableCollection_ShouldHonorItsOverride(string? one, string? two, bool expectedResult)
		{
			var left = new CustomCollectionValueObject() { Values = one is null ? null : new CustomCollectionValueObject.CustomCollection(one) };
			var right = new CustomCollectionValueObject() { Values = two is null ? null : new CustomCollectionValueObject.CustomCollection(two) };
			Assert.Equal(expectedResult, left.Equals(right));
		}

		[Theory]
		[InlineData(0, 0)]
		[InlineData(0, 1)]
		[InlineData(1, -1)]
		public void EqualityOperator_Regularly_ShouldMatchEquals(int one, int two)
		{
			var left = new IntValue(7, one);
			var right = new IntValue(7, two);
			Assert.Equal(left.Equals(right), left == right);

			left = new IntValue(7, one);
			right = new IntValue(-7, two);
			Assert.Equal(left.Equals(right), left == right);

			left = new IntValue(one, 7);
			right = new IntValue(two, -7);
			Assert.Equal(left.Equals(right), left == right);
		}

		[Theory]
		[InlineData("", "")]
		[InlineData("A", "A")]
		[InlineData("A", "a")]
		[InlineData("A", "B")]
		public void EqualityOperator_WithIgnoreCaseString_ShouldMatchEquals(string one, string two)
		{
			var left = new StringValue("7", one);
			var right = new StringValue("7", two);
			Assert.Equal(left.Equals(right), left == right);

			left = new StringValue("7", one);
			right = new StringValue("-7", two);
			Assert.Equal(left.Equals(right), left == right);

			left = new StringValue(one, "7");
			right = new StringValue(two, "-7");
			Assert.Equal(left.Equals(right), left == right);
		}

		[Fact]
		public void CompareTo_WithNullValueVsNull_ShouldReturnExpectedResult()
		{
			var nullValued = new DefaultComparingStringValue(value: null);

			Assert.Equal(+1, nullValued.CompareTo(null));
		}

		[Theory]
		[InlineData("", "")]
		[InlineData("A", "A")]
		[InlineData("A", "a")]
		[InlineData("A", "B")]
		public void CompareTo_WithEqualValuesAndIgnoreCaseString_ShouldHaveEqualityMatchingEquals(string one, string two)
		{
			var left = new StringValue("7", one);
			var right = new StringValue("7", two);
			Assert.Equal(left.Equals(right), left.CompareTo(right) == 0);
		}

		[Fact]
		public void CompareTo_WithoutExplicitInterface_ShouldNotBeImplemented()
		{
			var array = new[] { new IntValue(1, 1), new IntValue(2, 2) };

			Assert.Throws<InvalidOperationException>(() => Array.Sort(array));
		}

		[Fact]
		public void CompareTo_WithExplicitInterface_ShouldBeImplementedCorrectly()
		{
			var array = new[] { new StringValue("a", "3"), new StringValue("A", "4"), new StringValue("0", "2"), new StringValue("0", "1"), };

			array = array.OrderBy(x => x).ToArray(); // Stable sort

			Assert.Equal("1", array[0].Two); // { 0, 1 } before { 0, 2 }
			Assert.Equal("2", array[1].Two);
			Assert.Equal("3", array[2].Two); // Stable sort combined with ignore-case should have kept "a" before "A"
			Assert.Equal("4", array[3].Two);
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
		public void CompareTo_WithIgnoreCaseString_ShouldReturnExpectedResult(string? one, string? two, int expectedResult)
		{
			var left = one is null ? null : new StringValue(one, "7");
			var right = two is null ? null : new StringValue(two, "7");

			Assert.Equal(expectedResult, Comparer<StringValue>.Default.Compare(left, right));
			Assert.Equal(-expectedResult, Comparer<StringValue>.Default.Compare(right, left));

			left = one is null ? null : new StringValue("7", one);
			right = two is null ? null : new StringValue("7", two);

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
		public void GreaterThan_WithIgnoreCaseString_ShouldReturnExpectedResult(string? one, string? two, int expectedResult)
		{
			var left = one is null ? null : new StringValue(one, "7");
			var right = two is null ? null : new StringValue(two, "7");

			Assert.Equal(expectedResult > 0, left > right);
			Assert.Equal(expectedResult <= 0, left <= right);

			left = one is null ? null : new StringValue("7", one);
			right = two is null ? null : new StringValue("7", two);

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
		public void LessThan_WithIgnoreCaseString_ShouldReturnExpectedResult(string? one, string? two, int expectedResult)
		{
			var left = one is null ? null : new StringValue(one, "7");
			var right = two is null ? null : new StringValue(two, "7");

			Assert.Equal(expectedResult < 0, left < right);
			Assert.Equal(expectedResult >= 0, left >= right);

			left = one is null ? null : new StringValue("7", one);
			right = two is null ? null : new StringValue("7", two);

			Assert.Equal(expectedResult < 0, left < right);
			Assert.Equal(expectedResult >= 0, left >= right);
		}

		[Fact]
		public void ComparisonOperators_WithNullValueVsNull_ShouldReturnExpectedResult()
		{
			var nullValued = new DefaultComparingStringValue(value: null);

#pragma warning disable xUnit2024 // Do not use boolean asserts for simple equality tests -- We are testing overloaded operators
			Assert.False(null == nullValued);
			Assert.True(null != nullValued);
			Assert.False(nullValued == null);
			Assert.True(nullValued != null);
			Assert.True(null < nullValued);
			Assert.True(null <= nullValued);
			Assert.False(null >= nullValued);
			Assert.False(null >= nullValued);
			Assert.False(nullValued < null);
			Assert.False(nullValued <= null);
			Assert.True(nullValued > null);
			Assert.True(nullValued >= null);
#pragma warning restore xUnit2024 // Do not use boolean asserts for simple equality tests
		}

		[Theory]
		[InlineData(null)]
		[InlineData(0)]
		[InlineData(1)]
		public void SerializeWithSystemTextJson_Regularly_ShouldReturnExpectedResult(int? value)
		{
			// Regrettably we do not yet have an easy, reliable way of omitting calculated properties from the serialization
			var intInstance = value is null ? null : new IntValue(value.Value, value.Value);
			Assert.Equal(value is null ? "null" : $@"{{""One"":{value},""Two"":{value},""CalculatedProperty"":""{value}-{value}""}}", System.Text.Json.JsonSerializer.Serialize(intInstance));

			var stringInstance = value is null ? null : new StringValue(value.ToString()!, value.ToString()!);
			Assert.Equal(value is null ? "null" : $@"{{""One"":""{value}"",""Two"":""{value}""}}", System.Text.Json.JsonSerializer.Serialize(stringInstance));
		}

		[Theory]
		[InlineData(null)]
		[InlineData(0)]
		[InlineData(1)]
		public void SerializeWithNewtonsoftJson_Regularly_ShouldReturnExpectedResult(int? value)
		{
			// Regrettably we do not yet have an easy, reliable way of omitting calculated properties from the serialization
			var intInstance = value is null ? null : new IntValue(value.Value, value.Value);
			Assert.Equal(value is null ? "null" : $@"{{""One"":{value},""Two"":{value},""CalculatedProperty"":""{value}-{value}""}}", Newtonsoft.Json.JsonConvert.SerializeObject(intInstance));

			var stringInstance = value is null ? null : new StringValue(value.ToString()!, value.ToString()!);
			Assert.Equal(value is null ? "null" : $@"{{""One"":""{value}"",""Two"":""{value}""}}", Newtonsoft.Json.JsonConvert.SerializeObject(stringInstance));
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

			var instance = value is null ? null : new DecimalValue(value.Value, value.Value);

			Assert.Equal(value is null ? "null" : $@"{{""One"":{value},""Two"":{value}}}", System.Text.Json.JsonSerializer.Serialize(instance));
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

			var instance = value is null ? null : new DecimalValue(value.Value, value.Value);

			// Newtonsoft appends ".0" for some reason
			Assert.Equal(value is null ? "null" : $@"{{""One"":{value}.0,""Two"":{value}.0}}", Newtonsoft.Json.JsonConvert.SerializeObject(instance));
		}

		[Theory]
		[InlineData("null", null)]
		[InlineData(@"{""One"":0,""Two"":0}", 0)]
		[InlineData(@"{""One"":1,""Two"":1}", 1)]
		public void DeserializeWithSystemTextJson_Regularly_ShouldReturnExpectedResult(string json, int? value)
		{
			var result = System.Text.Json.JsonSerializer.Deserialize<IntValue>(json);

			if (value is null)
				Assert.Null(result);
			else
			{
				Assert.NotNull(result);
				Assert.Equal(value, result.One);
				Assert.Equal(value, result.Two);
			}

			json = json == "null" ? json : json.Replace(value.ToString()!, $@"""{value}""");

			var stringResult = System.Text.Json.JsonSerializer.Deserialize<StringValue>(json);

			if (value is null)
				Assert.Null(stringResult);
			else
			{
				Assert.NotNull(stringResult);
				Assert.Equal(value.ToString(), stringResult.One);
				Assert.Equal(value.ToString(), stringResult.Two);
			}
		}

		[Theory]
		[InlineData("null", null)]
		[InlineData(@"{""One"":0,""Two"":0}", 0)]
		[InlineData(@"{""One"":1,""Two"":1}", 1)]
		public void DeserializeWithNewtonsoftJson_Regularly_ShouldReturnExpectedResult(string json, int? value)
		{
			var result = Newtonsoft.Json.JsonConvert.DeserializeObject<IntValue>(json);

			if (value is null)
				Assert.Null(result);
			else
			{
				Assert.NotNull(result);
				Assert.Equal(value, result.One);
				Assert.Equal(value, result.Two);
			}

			json = json == "null" ? json : json.Replace(value.ToString()!, $@"""{value}""");

			var stringResult = Newtonsoft.Json.JsonConvert.DeserializeObject<StringValue>(json);

			if (value is null)
				Assert.Null(stringResult);
			else
			{
				Assert.NotNull(stringResult);
				Assert.Equal(value.ToString(), stringResult.One);
				Assert.Equal(value.ToString(), stringResult.Two);
			}
		}

		/// <summary>
		/// ValueObjects should (de)serialize decimals normally, which is the most expected behavior.
		/// Some special-casing exists only for generated identities.
		/// </summary>
		[Theory]
		[InlineData(@"null", null)]
		[InlineData(@"{""One"":0,""Two"":0}", 0)]
		[InlineData(@"{""One"":1,""Two"":1.0}", 1)]
		public void DeserializeWithSystemTextJson_WithDecimal_ShouldReturnExpectedResult(string json, int? value)
		{
			// Attempt to mess with the deserialization, which should have no effect
			CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("nl-NL");

			var result = System.Text.Json.JsonSerializer.Deserialize<DecimalValue>(json);

			if (value is null)
				Assert.Null(result);
			else
			{
				Assert.NotNull(result);
				Assert.Equal(value.Value, result.One);
				Assert.Equal(value.Value, result.Two);
			}
		}

		/// <summary>
		/// ValueObjects should (de)serialize decimals normally, which is the most expected behavior.
		/// Some special-casing exists only for generated identities.
		/// </summary>
		[Theory]
		[InlineData("null", null)]
		[InlineData(@"{""One"":0,""Two"":0}", 0)]
		[InlineData(@"{""One"":1,""Two"":1.0}", 1)]
		public void DeserializeWithNewtonsoftJson_WithDecimal_ShouldReturnExpectedResult(string json, int? value)
		{
			// Attempt to mess with the deserialization, which should have no effect
			CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("nl-NL");

			var result = Newtonsoft.Json.JsonConvert.DeserializeObject<DecimalValue>(json);

			if (value is null)
				Assert.Null(result);
			else
			{
				Assert.NotNull(result);
				Assert.Equal(value.Value, result.One);
				Assert.Equal(value.Value, result.Two);
			}
		}

		private sealed class ManualValueObject : ValueObject
		{
			public override string ToString() => this.Id.ToString();

			public int Id { get; }

			public ManualValueObject(int id)
			{
				this.Id = id;
			}

			public static new bool ContainsNonAlphanumericCharacters(ReadOnlySpan<char> text)
			{
				return ValueObject.ContainsNonAlphanumericCharacters(text);
			}

			public static new bool ContainsNonWordCharacters(ReadOnlySpan<char> text)
			{
				return ValueObject.ContainsNonWordCharacters(text);
			}

			public static new bool ContainsNonAsciiCharacters(ReadOnlySpan<char> text)
			{
				return ValueObject.ContainsNonAsciiCharacters(text);
			}

			public static new bool ContainsNonAsciiOrNonPrintableCharacters(ReadOnlySpan<char> text, bool flagNewLinesAndTabs = true)
			{
				return ValueObject.ContainsNonAsciiOrNonPrintableCharacters(text, flagNewLinesAndTabs);
			}

			public static new bool ContainsNonAsciiOrNonPrintableOrWhitespaceCharacters(ReadOnlySpan<char> text)
			{
				return ValueObject.ContainsNonAsciiOrNonPrintableOrWhitespaceCharacters(text);
			}

			public static new bool ContainsNonPrintableCharacters(ReadOnlySpan<char> text, bool flagNewLinesAndTabs)
			{
				return ValueObject.ContainsNonPrintableCharacters(text, flagNewLinesAndTabs);
			}

			public static new bool ContainsNonPrintableCharactersOrDoubleQuotes(ReadOnlySpan<char> text, bool flagNewLinesAndTabs)
			{
				return ValueObject.ContainsNonPrintableCharactersOrDoubleQuotes(text, flagNewLinesAndTabs);
			}

			public static new bool ContainsWhitespaceOrNonPrintableCharacters(ReadOnlySpan<char> text)
			{
				return ValueObject.ContainsWhitespaceOrNonPrintableCharacters(text);
			}
		}
	}

	// Use a namespace, since our source generators dislike nested types
	namespace ValueObjectTestTypes
	{
		[ValueObject]
		public sealed partial class ValueObjectWithIIdentity : IIdentity<int>
		{
		}

		/// <summary>
		/// This once caused build errors, before a bug fix handled properties of fully source-generated types.
		/// </summary>
		[ValueObject]
		public sealed partial class ValueObjectWithGeneratedIdentity // Unfortunately we cannot get IComparable<T>, since the source generator will only implement it if all properties are KNOWN to be IComparable<T> to themselves
		{
			/// <summary>
			/// This type is only fleshed out AFTER source generators have run.
			/// During source generation, its properties are unknown, and thus our own source generator cannot know whether it is a value type or a reference type.
			/// </summary>
			public FullyGeneratedId SomeValue { get; private init; }

			public ValueObjectWithGeneratedIdentity(FullyGeneratedId someValue)
			{
				this.SomeValue = someValue;
			}

			public sealed class Entity : Entity<FullyGeneratedId, ulong>
			{
				public Entity()
					: base(default)
				{
				}
			}
		}

		[ValueObject]
		public sealed partial class IntValue
		{
			[JsonInclude, JsonPropertyName("One"), Newtonsoft.Json.JsonProperty]
			public int One { get; private init; }
			[JsonInclude, JsonPropertyName("Two"), Newtonsoft.Json.JsonProperty]
			public int Two { get; private init; }

			public string CalculatedProperty => $"{this.One}-{this.Two}";

			public IntValue(int one, int two, object? _ = null)
			{
				this.One = one;
				this.Two = two;
			}

			public StringComparison GetStringComparison() => this.StringComparison;
		}

		[ValueObject]
		public sealed partial class StringValue : IComparable<StringValue>
		{
			protected override StringComparison StringComparison => StringComparison.OrdinalIgnoreCase;

			[JsonInclude, JsonPropertyName("One"), Newtonsoft.Json.JsonProperty]
			public string One { get; private init; }
			[JsonInclude, JsonPropertyName("Two"), Newtonsoft.Json.JsonProperty]
			public string Two { get; private init; }

			public StringValue(string one, string two, object? _ = null)
			{
				this.One = one ?? throw new ArgumentNullException(nameof(one));
				this.Two = two ?? throw new ArgumentNullException(nameof(two));
			}

			public StringComparison GetStringComparison() => this.StringComparison;
		}

		[ValueObject]
		public sealed partial class DecimalValue : ValueObject
		{
			[JsonInclude, JsonPropertyName("One"), Newtonsoft.Json.JsonProperty]
			public decimal One { get; private init; }
			[JsonInclude, JsonPropertyName("Two"), Newtonsoft.Json.JsonProperty]
			public decimal Two { get; private init; }

			public DecimalValue(decimal one, decimal two, object? _ = null)
			{
				this.One = one;
				this.Two = two;
			}
		}

		[ValueObject]
		public sealed partial class DefaultComparingStringValue : IComparable<DefaultComparingStringValue>
		{
			public string? Value { get; private init; }

			public DefaultComparingStringValue(string? value)
			{
				this.Value = value;
			}

			public StringComparison GetStringComparison() => this.StringComparison;
		}

		[ValueObject]
		public sealed partial class ImmutableArrayValueObject
		{
			protected override StringComparison StringComparison => StringComparison.OrdinalIgnoreCase;

			public ImmutableArray<string> Values { get; private init; }
			public ImmutableArray<string>? ValuesNullable { get; private init; }

			public ImmutableArrayValueObject(IEnumerable<string> values)
			{
				this.Values = values.ToImmutableArray();
				this.ValuesNullable = this.Values;
			}
		}

		/// <summary>
		/// Should merely compile.
		/// </summary>
		[Obsolete("Should merely compile.", error: true)]
		[ValueObject]
		public sealed partial class ArrayValueObject
		{
			protected override StringComparison StringComparison => StringComparison.OrdinalIgnoreCase;

			public string?[]? StringValues { get; private init; }
			public int?[] IntValues { get; private init; }

			public ArrayValueObject(string?[]? stringValues, int?[] intValues)
			{
				this.StringValues = stringValues;
				this.IntValues = intValues;
			}
		}

		[ValueObject]
		public sealed partial class CustomCollectionValueObject
		{
			public CustomCollection? Values { get; set; }

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
		[Obsolete("Should merely compile.", error: true)]
		[ValueObject]
		internal sealed partial class EmptyValueObject
		{
			public override string ToString() => throw new NotSupportedException();
		}

		/// <summary>
		/// Should merely compile.
		/// </summary>
		[Obsolete("Should merely compile.", error: true)]
		[ValueObject]
		[Serializable]
		[Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.Fields)]
		internal sealed partial class FullySelfImplementedValueObject : ValueObject, IComparable<FullySelfImplementedValueObject>
		{
			protected sealed override StringComparison StringComparison => throw new NotSupportedException("This operation applies to string-based value objects only.");

			public sealed override string ToString()
			{
				return $"{{FullySelfImplementedValueObject}}";
			}

			public sealed override int GetHashCode()
			{
				return typeof(FullySelfImplementedValueObject).GetHashCode();
			}

			public sealed override bool Equals(object? other)
			{
				return other is FullySelfImplementedValueObject otherValue && this.Equals(otherValue);
			}

			public bool Equals(FullySelfImplementedValueObject? other)
			{
				if (other is null) return false;

				return true; ;
			}

			// This method is generated only if the ValueObject implements IComparable<T> against its own type and each data member implements IComparable<T> against its own type
			public int CompareTo(FullySelfImplementedValueObject? other)
			{
				if (other is null) return +1;

				return 0;
			}

			public static bool operator ==(FullySelfImplementedValueObject? left, FullySelfImplementedValueObject? right) => left is null ? right is null : left.Equals(right);
			public static bool operator !=(FullySelfImplementedValueObject? left, FullySelfImplementedValueObject? right) => !(left == right);

			public static bool operator >(FullySelfImplementedValueObject? left, FullySelfImplementedValueObject? right) => left is not null && left.CompareTo(right) > 0;
			public static bool operator <(FullySelfImplementedValueObject? left, FullySelfImplementedValueObject? right) => left is null ? right is not null : left.CompareTo(right) < 0;
			public static bool operator >=(FullySelfImplementedValueObject? left, FullySelfImplementedValueObject? right) => !(left < right);
			public static bool operator <=(FullySelfImplementedValueObject? left, FullySelfImplementedValueObject? right) => !(left > right);
		}
	}
}
