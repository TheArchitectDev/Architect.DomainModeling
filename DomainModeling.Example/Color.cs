namespace Architect.DomainModeling.Example;

// Use "Go To Definition" on the type to view the source-generated partial
// Uncomment the IComparable interface to see how the generated code changes
[ValueObject]
public partial class Color //: IComparable<Color>
{
	public static Color RedColor { get; } = new Color(red: UInt16.MaxValue, green: 0, blue: 0);
	public static Color GreenColor { get; } = new Color(red: 0, green: UInt16.MaxValue, blue: 0);
	public static Color BlueColor { get; } = new Color(red: 0, green: 0, blue: UInt16.MaxValue);

	public ushort Red { get; private init; }
	public ushort Green { get; private init; }
	public ushort Blue { get; private init; }

	public Color(ushort red, ushort green, ushort blue)
	{
		this.Red = red;
		this.Green = green;
		this.Blue = blue;
	}
}
