using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Architect.DomainModeling;

/// <summary>
/// <para>
/// An entity is a data model that is defined by its identity and a thread of continuity. It may be mutated during its life cycle.
/// </para>
/// <para>
/// <see cref="Entity{TId, TIdPrimitive}"/> automatically declares an ID property of type <typeparamref name="TId"/>, as well as overriding certain behavior to make use of it.
/// </para>
/// <para>
/// <see cref="Entity{TId, TIdPrimitive}"/> automatically generates source code for type <typeparamref name="TId"/> if it does not exist.
/// The source-generated type wraps a value of type <typeparamref name="TIdPrimitive"/>.
/// </para>
/// </summary>
/// <typeparam name="TId">The custom ID type for this entity. The type is source-generated if a nonexistent type is specified.</typeparam>
/// <typeparam name="TIdPrimitive">The underlying primitive type used by the custom ID type.</typeparam>
[Serializable]
public abstract class Entity<TId, TIdPrimitive> : Entity<TId>
	where TId : IEquatable<TId>?, IComparable<TId>?
	where TIdPrimitive : IEquatable<TIdPrimitive>?, IComparable<TIdPrimitive>?
{
	protected Entity(TId id)
		: base(id)
	{
	}
}

/// <summary>
/// <para>
/// An entity is a data model that is defined by its identity and a thread of continuity. It may be mutated during its life cycle.
/// </para>
/// <para>
/// <see cref="Entity{TId}"/> automatically declares an ID property of type <typeparamref name="TId"/>, as well as overriding certain behavior to make use of it.
/// </para>
/// </summary>
[Serializable]
public abstract class Entity<TId> : Entity, IEquatable<Entity<TId>?>
	where TId : IEquatable<TId>?
{
	public override string ToString() => $"{{{this.GetType().Name} Id={this.Id}}}";

	/// <summary>
	/// <para>
	/// An instance of <typeparamref name="TId"/> has a meaningful value if it is both non-null (for reference types) and <em>not equal to this value</em>.
	/// </para>
	/// <para>
	/// For example, when <typeparamref name="TId"/> is a custom class wrapping an (auto-increment) <see cref="UInt64"/>, comparing against default/null is insufficient to determine if an instance has a meaningful value.
	/// An instance containing a value of 0 should also be considered an uninitialized ID.
	/// </para>
	/// <para>
	/// This property contains an empty instance of <typeparamref name="TId"/>, with all its fields set to their default values.
	/// </para>
	/// </summary>
	protected static TId? DefaultId { get; } = typeof(TId).IsValueType || typeof(TId) == typeof(string) || typeof(TId).IsAbstract || typeof(TId).IsInterface /*|| typeof(TId).IsGenericTypeDefinition || typeof(TId).IsArray*/
		? default
		: (TId)FormatterServices.GetUninitializedObject(typeof(TId));

	/// <summary>
	/// The entity's unique identity.
	/// </summary>
	public TId Id { get; }

	/// <param name="id">The unique identity for the entity.</param>
	protected Entity(TId id)
	{
		this.Id = id;
	}

	public override int GetHashCode()
	{
		// With a null or default-valued ID, use a reference-based hash code, to match Equals()
		return this.Id is null || this.Id.Equals(DefaultId)
			? RuntimeHelpers.GetHashCode(this)
			: this.Id.GetHashCode();
	}

	public override bool Equals(object? other)
	{
		return other is Entity<TId> otherId && this.Equals(otherId);
	}

	public virtual bool Equals(Entity<TId>? other)
	{
		if (other is null) return false;

		// Either we must be the same reference
		// Or we must have non-null, non-default, equal IDs (i.e. two entities with a default ID are not automatically considered the same entity)
		return ReferenceEquals(this, other) ||
			(this.Id is not null && !this.Id.Equals(DefaultId) && this.Id.Equals(other.Id));
	}
}

/// <summary>
/// <para>
/// An entity is a data model that is defined by its identity and a thread of continuity. It may be mutated during its life cycle.
/// </para>
/// </summary>
[Serializable]
public abstract class Entity : DomainObject
{
}
