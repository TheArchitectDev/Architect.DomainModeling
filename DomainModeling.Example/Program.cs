using System.Reflection;
using Newtonsoft.Json;

namespace Architect.DomainModeling.Example;

public static class Program
{
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

			var constructedDescription = new Description("Constructed");
			var castDescription = (Description)"Cast";

			Console.WriteLine($"Constructed from string: {constructedDescription}");
			Console.WriteLine($"Cast from string: {castDescription}");
			Console.WriteLine($"Description object cast to string: {(string)constructedDescription}");

			var upper = new Description("CASING");
			var lower = new Description("casing");

			Console.WriteLine($"{constructedDescription == castDescription}: {constructedDescription} == {castDescription} (different values)");
			Console.WriteLine($"{upper == lower}: {upper} == {lower} (different only in casing, with ignore-case value object)"); // ValueObjects have structural equality, and this one ignores casing

			var serialized = JsonConvert.SerializeObject(new Description("PrettySerializable"));
			var deserialized = JsonConvert.DeserializeObject<Description>(serialized);

			Console.WriteLine($"JSON-serialized: {serialized}"); // Generated serializers for System.Text.Json and Newtonsoft provide serialization as if there was no wrapper object
			Console.WriteLine($"JSON-deserialized: {deserialized}");

			Console.WriteLine();
		}

		// Entity
		{
			Console.WriteLine("Demonstrating Entity:");

			var payment = new Payment("EUR", 1.00m);
			var similarPayment = new Payment("EUR", 1.00m);

			Console.WriteLine($"Default ToString() implementation: {payment}");
			Console.WriteLine($"{payment.Equals(payment)}: {payment}.Equals({payment}) (same obj)");
			Console.WriteLine($"{payment.Equals(similarPayment)}: {payment}.Equals({similarPayment}) (other obj)"); // Entities have ID-based equality

			// Demonstrate two different instances with the same ID, to simulate the entity being loaded from a database twice
			typeof(Entity<PaymentId>).GetField("<Id>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(similarPayment, payment.Id);

			Console.WriteLine($"{payment.Equals(similarPayment)}: {payment}.Equals({similarPayment}) (same ID)"); // Entities have ID-based equality

			Console.WriteLine();
		}

		// DummyBuilder
		{
			Console.WriteLine("Demonstrating DummyBuilder:");

			// The builder pattern prevents tight coupling between test methods and constructor signatures, permitting constructor changes without breaking dozens of tests
			var defaultPayment = new PaymentDummyBuilder().Build();
			var usdPayment = new PaymentDummyBuilder().WithCurrency("USD").Build();

			Console.WriteLine($"Default Payment from builder: {defaultPayment}, {defaultPayment.Currency}, {defaultPayment.Amount}");
			Console.WriteLine($"Customized Payment from builder: {usdPayment}, {usdPayment.Currency}, {usdPayment.Amount}");

			Console.WriteLine();
		}

		// Collection equality
		{
			Console.WriteLine("Demonstrating structural equality for collections:");

			var abc = new CharacterSet(new[] { 'a', 'b', 'c', });
			var abcd = new CharacterSet(new[] { 'a', 'b', 'c', 'd', });
			var abcClone = new CharacterSet(new[] { 'a', 'b', 'c', });

			Console.WriteLine($"{abc == abcd}: {abc} == {abcd} (different values)");
			Console.WriteLine($"{abc == abcClone}: {abc} == {abcClone} (different instances, same values in collection)"); // ValueObjects have structural equality

			Console.WriteLine();
		}
	}
}
