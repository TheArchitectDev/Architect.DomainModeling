namespace Architect.DomainModeling;

/// <summary>
/// <para>
/// A <see cref="ValueObject"/> wrapping a single value, i.e. an immutable data model representing a single value.
/// </para>
/// <para>
/// Value objects are identified and compared by their values.
/// </para>
/// <para>
/// This type offers protected methods to help perform common validations on its underlying data.
/// </para>
/// </summary>
[Serializable]
public abstract class WrapperValueObject<TValue> : ValueObject, IWrapperValueObject<TValue>
	where TValue : notnull
{
	/// <summary>
	/// <para>
	/// The <see cref="WrapperValueObject{TValue}"/> may use this to decide how to compare its string value.
	/// </para>
	/// <para>
	/// This property is only relevant where <typeparamref name="TValue"/> is string, and may throw a <see cref="NotSupportedException"/> otherwise.
	/// </para>
	/// <para>
	/// String-based subclasses should usually override this with one of the following:
	/// </para>
	/// <para>
	/// <code>=> StringComparison.Ordinal;</code>
	/// <code>=> StringComparison.OrdinalIgnoreCase;</code>
	/// </para>
	/// </summary>
	protected abstract override StringComparison StringComparison { get; }
}
