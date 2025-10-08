namespace Architect.DomainModeling.Tests;

// Test custom subtypes of the attributes allow testing that even subclasses are recognized, provided that the naming is recognizable

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class TestEntityAttribute : EntityAttribute
{
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class TestEntityAttribute<
	TId,
	TIdUnderlying> : EntityAttribute<TId, TIdUnderlying>
	where TId : IEquatable<TId>?, IComparable<TId>?
	where TIdUnderlying : IEquatable<TIdUnderlying>?, IComparable<TIdUnderlying>?
{
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class TestDomainEventAttribute : DomainEventAttribute
{
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class TestValueObjectAttribute : ValueObjectAttribute
{
}

[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class TestIdentityAttribute<T> : IdentityValueObjectAttribute<T>
	where T : notnull, IEquatable<T>, IComparable<T>
{
}

[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class TestWrapperAttribute<TValue> : WrapperValueObjectAttribute<TValue>
	where TValue : notnull
{
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
public sealed class TestBuilderAttribute<TModel> : DummyBuilderAttribute<TModel>
	where TModel : notnull
{
}
