using System.Diagnostics.CodeAnalysis;

namespace Architect.DomainModeling;

/// <summary>
/// A domain object of type <typeparamref name="TModel"/> that can be serialized to and deserialized from underlying type <typeparamref name="TUnderlying"/>.
/// </summary>
public interface ISerializableDomainObject<
	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] TModel,
	TUnderlying>
{
	/// <summary>
	/// Serializes a <typeparamref name="TModel"/> as a <typeparamref name="TUnderlying"/>.
	/// </summary>
	TUnderlying? Serialize();

	/// <summary>
	/// Deserializes a <typeparamref name="TModel"/> from a <typeparamref name="TUnderlying"/>.
	/// </summary>
	abstract static TModel Deserialize(TUnderlying value);
}
