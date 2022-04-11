namespace Architect.DomainModeling
{
	/// <summary>
	/// <para>
	/// Indicates that additional source code is generated for the type at compile time.
	/// </para>
	/// <para>
	/// This attribute only takes effect is the type is marked as partial and has an interface or base class that supports source generation.
	/// </para>
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
	public class SourceGeneratedAttribute : Attribute
	{
	}
}
