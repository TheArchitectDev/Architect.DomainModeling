namespace Architect.DomainModeling;

/// <summary>
/// <para>
/// Marks a type as a DDD entity in the domain model.
/// </para>
/// <para>
/// If the annotated type is also partial, the source generator kicks in to complete it.
/// </para>
/// <para>
/// This attribute should only be applied to concrete types.
/// For example, if Banana and Strawberry are two concrete entity types inheriting from type Fruit, then only Banana and Strawberry should have the attribute.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class EntityAttribute : Attribute
{
}
