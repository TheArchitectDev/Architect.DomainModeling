using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Architect.DomainModeling.Generator.Common;

/// <summary>
/// Represents a <see cref="Location"/> as a simple, serializable structure.
/// </summary>
internal sealed record class SimpleLocation
{
	public string FilePath { get; }
	public TextSpan TextSpan { get; }
	public LinePositionSpan LineSpan { get; }

	public SimpleLocation(Location location)
	{
		var lineSpan = location.GetLineSpan();
		this.FilePath = lineSpan.Path;
		this.TextSpan = location.SourceSpan;
		this.LineSpan = lineSpan.Span;
	}

	public SimpleLocation(string filePath, TextSpan textSpan, LinePositionSpan lineSpan)
	{
		this.FilePath = filePath;
		this.TextSpan = textSpan;
		this.LineSpan = lineSpan;
	}

#nullable disable
	public static implicit operator SimpleLocation(Location location) => location is null ? null : new SimpleLocation(location);
	public static implicit operator Location(SimpleLocation location) => location is null ? null : Location.Create(location.FilePath, location.TextSpan, location.LineSpan);
#nullable enable
}
