namespace Architect.DomainModeling.Example;

// Use "Go To Definition" on the type to view the source-generated partial
// Uncomment the IComparable interface to see how the generated code changes
[WrapperValueObject<string>]
public partial class Description //: IComparable<Description>
{
	// For string wrappers, we must define how they are compared
	protected override StringComparison StringComparison => StringComparison.OrdinalIgnoreCase;

	// Any component that we define manually is omitted by the generated code
	// For example, we can explicitly define the Value property to have greater clarity, since it is quintessential
	public string Value { get; private init; }

	// An explicitly defined constructor allows us to enforce the domain rules and invariants
	public Description(string value)
	{
		this.Value = value ?? throw new ArgumentNullException(nameof(value));

		if (this.Value.Length > 255) throw new ArgumentException("Too long.");

		if (ContainsNonWordCharacters(this.Value)) throw new ArgumentException("Nonsense.");
	}
}
