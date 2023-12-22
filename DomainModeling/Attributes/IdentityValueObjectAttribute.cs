namespace Architect.DomainModeling;

/// <summary>
/// <para>
/// Marks a type as a DDD identity value object in the domain model, i.e. a value object containing an ID, with underlying type <typeparamref name="T"/>.
/// </para>
/// <para>
/// If the annotated type is also a partial struct, the source generator kicks in to complete it.
/// </para>
/// <para>
/// Note that identity types tend to have no validation.
/// For example, even though no entity might exist for IDs 0 and 999999999999, they are still valid ID values for which such a question could be asked.
/// If validation <em>is</em> desirable for an ID type, such as for a third-party <see cref="String"/> ID that is expected to fit within given length, then a wrapper value object is worth considering.
/// </para>
/// </summary>
/// <typeparam name="T">The underlying type wrapped by the annotated identity type.</typeparam>
[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class IdentityValueObjectAttribute<T> : ValueObjectAttribute
	where T : notnull, IEquatable<T>, IComparable<T>
{
}
