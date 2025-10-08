using Architect.DomainModeling.Comparisons;

namespace Architect.DomainModeling.Example;

// Use "Go To Definition" on the type to view the source-generated partial
// Outcomment the IComparable interface to see how the generated code changes
[WrapperValueObject<string>]
public partial record struct Currency : IComparable<Currency>
{
	// For string wrappers, we must define how they are compared
	private StringComparison StringComparison => StringComparison.OrdinalIgnoreCase;

	// Any component that we define manually is omitted by the generated code
	// For example, we can explicitly define the Value property to have greater clarity, since it is quintessential
	public string Value { get; private init; }

	// An explicitly defined constructor allows us to enforce the domain rules and invariants
	public Currency(string value)
	{
		// Note: We could even choose to do ToUpperInvariant() on the input value, for a more consistent internal representation
		this.Value = value ?? throw new ArgumentNullException(nameof(value));

		if (this.Value.Length != 3)
			throw new ArgumentException($"A {nameof(Currency)} must be exactly 3 chars long.");

		if (ValueObjectStringValidator.ContainsNonAsciiOrNonPrintableOrWhitespaceCharacters(this.Value))
			throw new ArgumentException($"A {nameof(Currency)} must consist of simple characters.");
	}
}
