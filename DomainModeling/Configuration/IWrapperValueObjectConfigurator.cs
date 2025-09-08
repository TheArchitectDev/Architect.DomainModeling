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
	TValue,
	TCore>(
			in Args args)
		where TWrapper : IWrapperValueObject<TValue>, IDirectValueWrapper<TWrapper, TValue>, ICoreValueWrapper<TWrapper, TCore>
		where TValue : notnull;

	[SuppressMessage("Style", "IDE0040:Remove accessibility modifiers", Justification = "We always want explicit accessibility for types")]
	public readonly struct Args
	{
	}
}
