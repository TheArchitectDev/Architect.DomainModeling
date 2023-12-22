namespace Architect.DomainModeling;

/// <summary>
/// <para>
/// Marks a type as a dummy builder that can produce instances of <typeparamref name="TModel"/>, such as for testing.
/// </para>
/// <para>
/// Dummy builders make it easy to produce non-empty instances.
/// </para>
/// <para>
/// Specific default values can be customized by manually declaring the corresponding properties or fields.
/// These can simply be copied from the source-generated implementation and then changed.
/// </para>
/// <para>
/// Additional With*() methods can be added by imitating the source-generated implementation, either delegating to other With*() methods or assigning the properties or fields.
/// </para>
/// <para>
/// This attribute should only be applied to concrete types.
/// For example, if PaymentDummyBuilder is a concrete dummy builder type inheriting from abstract type FinancialDummyBuilder, then only PaymentDummyBuilder should have the attribute.
/// </para>
/// </summary>
/// <typeparam name="TModel">The model type produced by the annotated dummy builder.</typeparam>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public class DummyBuilderAttribute<TModel> : Attribute
	where TModel : notnull
{
}
