using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace Architect.DomainModeling.Example;

public static class Program
{
	[UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Merely demoing JSON serialization.")]
	public static void Main()
	{
		// ValueObject
		{
			Console.WriteLine("Demonstrating ValueObject:");

			var green = new Color(red: 0, green: UInt16.MaxValue, blue: 0);

			Console.WriteLine($"{Color.RedColor == Color.GreenColor}: {Color.RedColor} == {Color.GreenColor} (different values)");
			Console.WriteLine($"{Color.GreenColor == green}: {Color.GreenColor} == {green} (different instances, same values)"); // ValueObjects have structural equality

			Console.WriteLine();
		}

		// WrapperValueObject
		{
			Console.WriteLine("Demonstrating WrapperValueObject:");

			var constructedCurrency = new Currency("EUR");
			var castCurrency = (Currency)"USD";

			Console.WriteLine($"Constructed from string: {constructedCurrency}");
			Console.WriteLine($"Cast from string: {castCurrency}");
			Console.WriteLine($"Currency object cast to string: {(string)constructedCurrency}");

			var upper = new Currency("EUR");
			var lower = new Currency("eur");

			Console.WriteLine($"{constructedCurrency == castCurrency}: {constructedCurrency} == {castCurrency} (different values)");
			Console.WriteLine($"{upper == lower}: {upper} == {lower} (different only in casing, with ignore-case value object)"); // ValueObjects have structural equality, and this one ignores casing

			var serialized = JsonConvert.SerializeObject(new Currency("USD"));
			var deserialized = JsonConvert.DeserializeObject<Currency>(serialized);

			Console.WriteLine($"JSON-serialized: {serialized}"); // Generated serializers for System.Text.Json and Newtonsoft provide serialization as if there was no wrapper object
			Console.WriteLine($"JSON-deserialized: {deserialized}");

			Console.WriteLine();
		}

		// Entity
		{
			Console.WriteLine("Demonstrating Entity:");

			var payment = new Payment((Currency)"EUR", 1.00m);
			var similarPayment = new Payment((Currency)"EUR", 1.00m);

			Console.WriteLine($"Default ToString() implementation: {payment}");
			Console.WriteLine($"{payment.Equals(payment)}: {payment}.Equals({payment}) (same obj)");
			Console.WriteLine($"{payment.Equals(similarPayment)}: {payment}.Equals({similarPayment}) (other obj)"); // Different entities, even though they look similar

			// Entities have reference equality, or even ID-based equality if the Entity base classes are used
			// This library aims to avoid forced base classes, and reference equality tends to suffice when entities are used with Entity Framework
			// However, the base classes can be used to upgrade to ID-based equality, which allows even to separately loaded instances to be considered equal

			Console.WriteLine();
		}

		// DummyBuilder
		{
			Console.WriteLine("Demonstrating DummyBuilder:");

			// The builder pattern prevents tight coupling between test methods and constructor signatures, permitting constructor changes without breaking dozens of tests
			var defaultPayment = new PaymentDummyBuilder().Build();
			var usdPayment = new PaymentDummyBuilder().WithCurrency((Currency)"USD").Build();

			Console.WriteLine($"Default Payment from builder: {defaultPayment}, {defaultPayment.Currency}, {defaultPayment.Amount}");
			Console.WriteLine($"Customized Payment from builder: {usdPayment}, {usdPayment.Currency}, {usdPayment.Amount}");

			Console.WriteLine();
		}

		// Collection equality
		{
			Console.WriteLine("Demonstrating structural equality for collections:");

			var abc = new CharacterSet(['a', 'b', 'c',]);
			var abcd = new CharacterSet(['a', 'b', 'c', 'd',]);
			var abcClone = new CharacterSet(['a', 'b', 'c',]);

			Console.WriteLine($"{abc == abcd}: {abc} == {abcd} (different values)");
			Console.WriteLine($"{abc == abcClone}: {abc} == {abcClone} (different instances, same values in collection)"); // ValueObjects have structural equality

			Console.WriteLine();
		}
	}
}
