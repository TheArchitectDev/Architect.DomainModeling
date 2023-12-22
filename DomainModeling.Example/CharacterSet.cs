namespace Architect.DomainModeling.Example;

/// <summary>
/// Demonstrates structural equality with collections.
/// </summary>
[ValueObject]
public partial class CharacterSet
{
	public override string ToString() => $"[{String.Join(", ", this.Characters)}]";

	public IReadOnlySet<char> Characters { get; private init; }

	public CharacterSet(IEnumerable<char> characters)
	{
		this.Characters = characters.Distinct().ToHashSet();
	}

	public bool ContainsCharacter(char character)
	{
		return this.Characters.Contains(character);
	}
}
