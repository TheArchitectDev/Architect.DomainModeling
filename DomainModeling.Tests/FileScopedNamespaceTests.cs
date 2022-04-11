namespace Architect.DomainModeling.Tests;

// This file tests source generation in combination with C# 10's FileScopedNamespaces, which initially was wrongfully flagged as nested types

[SourceGenerated]
public partial class FileScopedNamespaceValueObject : ValueObject
{
	public override string ToString() => throw new NotSupportedException();
}

[SourceGenerated]
public partial class FileScopedNamespaceWrapperValueObject : WrapperValueObject<int>
{
}

[SourceGenerated]
public partial class FileScopedDummyBuilder : DummyBuilder<FileScopedNamespaceValueObject, FileScopedDummyBuilder>
{
}

[SourceGenerated]
public partial struct FileScopedId : IIdentity<ulong>
{
}

public partial class FileScopedNamespaceEntity : Entity<FileScopedNamespaceEntityId, ulong>
{
	public FileScopedNamespaceEntity()
		: base(default)
	{
	}
}
