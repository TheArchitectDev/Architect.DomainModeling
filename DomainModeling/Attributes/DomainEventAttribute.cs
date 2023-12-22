namespace Architect.DomainModeling;

/// <summary>
/// <para>
/// Marks a type as a DDD domain event in the domain model.
/// </para>
/// <para>
/// Although the package currently offers no direction for how to work with domain events, this attribute allows them to be marked, and possibly included in source generators that are based on domain object types.
/// </para>
/// <para>
/// This attribute should only be applied to concrete types.
/// For example, if TransactionSettledEvent is a concrete type inheriting from abstract type FinancialEvent, then only TransactionSettledEvent should have the attribute.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class DomainEventAttribute : Attribute
{
}
