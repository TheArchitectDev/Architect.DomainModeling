namespace Architect.DomainModeling;

/// <summary>
/// <para>
/// Marks a type as a DDD wrapper value object in the domain model, i.e. a value object wrapping a single value of type <typeparamref name="TValue"/>.
/// For example, consider a ProperName type wrapping a <see cref="String"/>.
/// </para>
/// <para>
/// If the annotated type is also a partial class, the source generator kicks in to complete it.
/// </para>
/// <para>
/// This attribute should only be applied to concrete types.
/// For example, if ProperName is a concrete wrapper value object type inheriting from abstract type Text, then only ProperName should have the attribute.
/// </para>
/// </summary>
/// <typeparam name="TValue">The underlying type wrapped by the annotated wrapper value object type.</typeparam>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public class WrapperValueObjectAttribute<TValue> : ValueObjectAttribute
	where TValue : notnull
{
}
