namespace Architect.DomainModeling.Example;

/// <summary>
/// Demonstrates structural equality with collections.
/// </summary>
[SourceGenerated]
public partial class CharacterSet : ValueObject
{
	public override string ToString() => $"[{String.Join(", ", this.Characters)}]";

	public IReadOnlySet<char> Characters { get; }

	public CharacterSet(IEnumerable<char> characters)
	{
		this.Characters = characters.Distinct().ToHashSet();
	}

	public bool ContainsCharacter(char character)
	{
		return this.Characters.Contains(character);
	}
}
