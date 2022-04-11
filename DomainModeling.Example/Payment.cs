namespace Architect.DomainModeling.Example;

// Use "Go To Definition" on the PaymentId type to view its source-generated implementation
public class Payment : Entity<PaymentId, string> // Entity<PaymentId, string>: An Entity identified by a PaymentId, which is a source-generated struct wrapping a string
{
	// A default ToString() property based on the type and the Id value is provided by the base class
	// Hash code and equality implementations based on the Id value are provided by the base class

	// The Id property is provided by the base class

	public string Currency { get; } // Note that Currency deserves its own value object in practice
	public decimal Amount { get; }

	public Payment(string currency, decimal amount)
		: base(new PaymentId(Guid.NewGuid().ToString("N"))) // ID generated on construction (see also: https://github.com/TheArchitectDev/Architect.Identities#distributed-ids)
	{
		this.Currency = currency ?? throw new ArgumentNullException(nameof(currency));
		this.Amount = amount;
	}
}
