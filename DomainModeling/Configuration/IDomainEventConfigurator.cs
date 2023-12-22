using System.Diagnostics.CodeAnalysis;

namespace Architect.DomainModeling.Configuration;

/// <summary>
/// An instance of this abstraction configures a miscellaneous component when it comes to domain event types.
/// One example is a convention configurator for Entity Framework.
/// </summary>
public interface IDomainEventConfigurator
{
	/// <summary>
	/// A callback to configure a domain event of type <typeparamref name="TDomainEvent"/>.
	/// </summary>
	void ConfigureDomainEvent<
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] TDomainEvent>(
			in Args args)
		where TDomainEvent : IDomainObject;

	public readonly struct Args
	{
		public bool HasDefaultConstructor { get; init; }
	}
}
