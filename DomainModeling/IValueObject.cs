namespace Architect.DomainModeling;

/// <summary>
/// <para>
/// An immutable data model representing one or more values.
/// </para>
/// <para>
/// Value objects are identified and compared by their values.
/// </para>
/// <para>
/// Struct value objects should implement this interface, as they cannot inherit from <see cref="ValueObject"/>.
/// </para>
/// </summary>
public interface IValueObject : IDomainObject
{
}
