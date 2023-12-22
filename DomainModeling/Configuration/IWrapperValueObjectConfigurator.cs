using System.Diagnostics.CodeAnalysis;

namespace Architect.DomainModeling.Configuration;

/// <summary>
/// An instance of this abstraction configures a miscellaneous component when it comes to <see cref="IWrapperValueObject{TValue}"/> types.
/// One example is a convention configurator for Entity Framework.
/// </summary>
public interface IWrapperValueObjectConfigurator
{
	/// <summary>
	/// A callback to configure a wrapper value object of type <typeparamref name="TWrapper"/>.
	/// </summary>
	void ConfigureWrapperValueObject<
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] TWrapper,
	TValue>(
			in Args args)
		where TWrapper : IWrapperValueObject<TValue>, ISerializableDomainObject<TWrapper, TValue>
		where TValue : notnull;

	public readonly struct Args
	{
	}
}
