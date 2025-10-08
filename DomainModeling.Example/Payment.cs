namespace Architect.DomainModeling.Example;

// An Entity identified by a PaymentId, the latter being a source-generated struct wrapping a string
// Use "Go To Definition" on the PaymentId type to view its source-generated implementation
[Entity<PaymentId, string>]
public sealed class Payment : Entity<PaymentId> // Base class is optional, but offers ID-based equality and a decent ToString() override
{
	// Property Id is declared by base class

	public Currency Currency { get; }
	public decimal Amount { get; }

	public Payment(
		Currency currency,
		decimal amount)
		: base(new PaymentId(Guid.CreateVersion7().ToString("N"))) // ID generated on construction (see also: https://github.com/TheArchitectDev/Architect.Identities#distributed-ids)
	{
		// Note how, thanks to the chosen types, it is hard to pass an invalid value
		// (The use of the "default" keyword for struct WrapperValueObjects is prevented by an analyzer)
		this.Currency = currency;
		this.Amount = amount;
	}
}
