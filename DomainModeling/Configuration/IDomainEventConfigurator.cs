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

#pragma warning disable IDE0040 // Remove accessibility modifiers -- We always want explicit accessibility for types
	public readonly struct Args
#pragma warning restore IDE0040 // Remove accessibility modifiers
	{
		public readonly bool HasDefaultConstructor { get; init; }
	}
}
