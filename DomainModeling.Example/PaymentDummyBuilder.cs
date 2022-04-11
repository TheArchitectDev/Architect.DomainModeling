namespace Architect.DomainModeling.Example;

// The source-generated partial provides an appropriate type summary
[SourceGenerated]
public sealed partial class PaymentDummyBuilder : DummyBuilder<Payment, PaymentDummyBuilder>
{
	// The source-generated partial defines a default value for each property, along with a fluent method to change it

	private string Currency { get; set; } = "EUR"; // Since the source generator cannot guess a decent default currency, we specify it manually

	// The source-generated partial defines a Build() method that invokes the most visible, simplest parameterized constructor
}
