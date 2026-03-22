namespace Architect.DomainModeling;

/// <inheritdoc/>
public interface IDummyBuilder<TModel> : IDummyBuilder
	where TModel : notnull
{
}

/// <summary>
/// <para>
/// A dummy builder that can produce instances of a certain type, such as for testing.
/// </para>
/// <para>
/// Dummy builders make it easy to produce non-empty instances.
/// </para>
/// <para>
/// See <see cref="DummyBuilderAttribute{TModel}"/>.
/// </para>
/// </summary>
public interface IDummyBuilder
{
}
