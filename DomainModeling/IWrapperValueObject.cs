namespace Architect.DomainModeling;

/// <summary>
/// <para>
/// An <see cref="IValueObject"/> wrapping a single value, i.e. an immutable data model representing a single value.
/// </para>
/// <para>
/// Value objects are identified and compared by their values.
/// </para>
/// <para>
/// Struct value objects should implement this interface, as they cannot inherit from <see cref="WrapperValueObject{TValue}"/>.
/// </para>
/// </summary>
public interface IWrapperValueObject<TValue> : IValueObject
	where TValue : notnull
{
}
