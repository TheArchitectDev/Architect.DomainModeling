using System.Diagnostics.CodeAnalysis;

namespace Architect.DomainModeling.Configuration;

/// <summary>
/// An instance of this abstraction configures a miscellaneous component when it comes to <see cref="IIdentity{T}"/> types.
/// One example is a convention configurator for Entity Framework.
/// </summary>
public interface IIdentityConfigurator
{
	/// <summary>
	/// A callback to configure an identity of type <typeparamref name="TIdentity"/>.
	/// </summary>
	void ConfigureIdentity<
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] TIdentity,
		TUnderlying>(
			in Args args)
		where TIdentity : IIdentity<TUnderlying>, ISerializableDomainObject<TIdentity, TUnderlying>
		where TUnderlying : notnull, IEquatable<TUnderlying>, IComparable<TUnderlying>;

	public readonly struct Args
	{
	}
}
