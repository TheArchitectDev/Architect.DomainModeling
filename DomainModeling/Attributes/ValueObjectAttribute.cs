namespace Architect.DomainModeling;

/// <summary>
/// <para>
/// Marks a type as a DDD value object in the domain model.
/// </para>
/// <para>
/// If the annotated type is also a partial class, the source generator kicks in to complete it.
/// </para>
/// <para>
/// This attribute should only be applied to concrete types.
/// For example, if Address is a concrete value object type inheriting from abstract type PersonalDetail, then only Address should have the attribute.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public class ValueObjectAttribute : Attribute
{
}
