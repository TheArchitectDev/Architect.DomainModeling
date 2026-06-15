namespace Architect.DomainModeling;

/// <summary>
/// <para>
/// An <see cref="IValueObject"/> wrapping a single value, i.e. an immutable data model representing a single value.
/// </para>
/// <para>
/// Value objects are identified and compared by their values.
/// </para>
/// </summary>
public interface IWrapperValueObject<TValue> : IWrapperValueObject
	where TValue : notnull
{
}

/// <summary>
/// <para>
/// An <see cref="IValueObject"/> wrapping a single value, i.e. an immutable data model representing a single value.
/// </para>
/// <para>
/// Value objects are identified and compared by their values.
/// </para>
/// </summary>
public interface IWrapperValueObject : IValueObject
{
}
