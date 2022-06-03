namespace Architect.DomainModeling;

/// <summary>
/// <para>
/// A specific <see cref="IValueObject"/> used as an object's identity.
/// </para>
/// <para>
/// This interface marks an identity type that wraps underlying type <typeparamref name="T"/>.
/// </para>
/// </summary>
public interface IIdentity<T> : IValueObject
	where T : IEquatable<T>, IComparable<T>
{
}
