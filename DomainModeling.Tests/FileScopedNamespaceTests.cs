namespace Architect.DomainModeling.Tests;

// This file tests source generation in combination with C# 10's FileScopedNamespaces, which initially was wrongfully flagged as nested types

[ValueObject]
public partial class FileScopedNamespaceValueObject
{
	public override string ToString() => throw new NotSupportedException();
}

[WrapperValueObject<int>]
public partial class FileScopedNamespaceWrapperValueObject
{
}

[DummyBuilder<FileScopedNamespaceValueObject>]
public partial class FileScopedDummyBuilder
{
}

[IdentityValueObject<ulong>]
public partial struct FileScopedId
{
}

public partial class FileScopedNamespaceEntity : Entity<FileScopedNamespaceEntityId, ulong>
{
	public FileScopedNamespaceEntity()
		: base(default)
	{
	}
}
