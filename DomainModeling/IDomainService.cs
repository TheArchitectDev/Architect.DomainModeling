namespace Architect.DomainModeling
{
	/// <summary>
	/// <para>
	/// A domain service encapsulates behavior that does not belong to any specific <see cref="ValueObject"/> or <see cref="Entity"/>.
	/// </para>
	/// <para>
	/// Domain services must be stateless.
	/// </para>
	/// </summary>
	public interface IDomainService : IDomainObject
    {
    }
}
