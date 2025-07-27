using System.Diagnostics.CodeAnalysis;

namespace Architect.DomainModeling;

/// <summary>
/// An <see cref="IDomainObject"/> of type <typeparamref name="TModel"/> that can be serialized and deserialized to underlying type <typeparamref name="TUnderlying"/>.
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
