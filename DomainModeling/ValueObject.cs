namespace Architect.DomainModeling
{
	/// <summary>
	/// <para>
	/// An immutable data model representing one or more values.
	/// </para>
	/// <para>
	/// Value objects are identified and compared by their values.
	/// </para>
	/// <para>
	/// This type offers protected methods to help perform common validations on its underlying data.
	/// </para>
	/// </summary>
	[Serializable]
	public abstract partial class ValueObject : DomainObject, IValueObject
	{
		public override abstract string? ToString();
		public override int GetHashCode() => throw new NotSupportedException();
		public override bool Equals(object? obj) => throw new NotSupportedException();

		/// <summary>
		/// <para>
		/// The <see cref="ValueObject"/> may use this to decide how to compare its <em>immediate</em> string properties.
		/// </para>
		/// <para>
		/// This setting does not affect non-string properties, properties of other types, or strings in collections.
		/// </para>
		/// <para>
		/// Generally, each string should be wrapped in its own <see cref="WrapperValueObject{TValue}"/>, which handles its comparisons appropriately.
		/// That way, a multi-valued <see cref="ValueObject"/> need not concern itself with this setting.
		/// </para>
		/// </summary>
		protected virtual StringComparison StringComparison => StringComparison.Ordinal;

		public static bool operator ==(ValueObject? left, ValueObject? right) => left is null ? right is null : left.Equals(right);
		public static bool operator !=(ValueObject? left, ValueObject? right) => !(left == right);
	}
}
