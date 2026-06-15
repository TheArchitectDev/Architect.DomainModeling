#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Architect.DomainModeling;

/// <summary>
/// <para>
/// Marks a type as a DDD entity in the domain model.
/// </para>
/// <para>
/// This attribute should only be applied to concrete types.
/// For example, if Banana and Strawberry are two concrete entity types inheriting from type Fruit, then only Banana and Strawberry should have the attribute.
/// </para>
/// <para>
/// Subclasses of this attribute are also honored, provided that they contain "Entity" in their name.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class EntityAttribute : Attribute
{
}

/// <summary>
/// <para>
/// Marks a type as a DDD entity in the domain model, with generated custom ID type <typeparamref name="TId"/>, which wraps <typeparamref name="TIdUnderlying"/>.
/// </para>
/// <para>
/// This attribute should only be applied to concrete types.
/// For example, if Banana and Strawberry are two concrete entity types inheriting from type Fruit, then only Banana and Strawberry should have the attribute.
/// If they all need to use a FruitId, then use the non-generic <see cref="EntityAttribute"/>, and manually define FruitId with the <see cref="IdentityValueObjectAttribute{T}"/>.
/// </para>
/// <para>
/// Subclasses of this attribute are also honored, provided that they contain "Entity" in their name.
/// </para>
/// </summary>
/// <typeparam name="TId">The custom ID type for this entity. The type is source-generated if a nonexistent type is specified.</typeparam>
/// <typeparam name="TIdUnderlying">The underlying type used by the custom ID type.</typeparam>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class EntityAttribute<
	TId,
	TIdUnderlying> : EntityAttribute
	where TId : IEquatable<TId>?, IComparable<TId>?
	where TIdUnderlying : IEquatable<TIdUnderlying>?, IComparable<TIdUnderlying>?
{
}
