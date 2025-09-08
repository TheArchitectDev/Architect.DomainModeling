namespace Architect.DomainModeling.Configuration;

/// <summary>
/// Base options for value wrappers, used by both <see cref="IdentityConfigurationOptions"/> and <see cref="WrapperValueObjectConfigurationOptions"/>.
/// </summary>
public abstract record class ValueWrapperConfigurationOptions
{
	/// <summary>
	/// <para>
	/// If specified, this collation is set on configured <see langword="string"/> <see cref="IIdentity{T}"/> types that use <see cref="StringComparison.Ordinal"/>.
	/// </para>
	/// <para>
	/// This helps the database column match the model's behavior.
	/// </para>
	/// </summary>
	public string? CaseSensitiveCollation { get; init; }

	/// <summary>
	/// <para>
	/// If specified, this collation is set on configured <see langword="string"/> <see cref="IIdentity{T}"/> types that use <see cref="StringComparison.OrdinalIgnoreCase"/>.
	/// </para>
	/// <para>
	/// This helps the database column match the model's behavior.
	/// </para>
	/// </summary>
	public string? IgnoreCaseCollation { get; init; }
}
