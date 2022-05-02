# Architect.DomainModeling

A complete Domain-Driven Design (DDD) toolset for implementing domain models, including base types and source generators.

- Base types, including: `ValueObject`, `WrapperValueObject`, `Entity`, `IIdentity`, `IApplicationService`, `IDomainService`.
- Source generators, for types including: `ValueObject`, `WrapperValueObject`, `DummyBuilder`, `IIdentity`.
- Structural implementations for hash codes and equality on collections (also used automatically by source-generated value objects containing collections).

## Source Generators

This package uses source generators (introduced in .NET 5). Source generators write additional C# code as part of the compilation process.

Among other advantages, source generators enable IntelliSense on generated code. They are primarily used here to generate boilerplate code, such as overrides of `ToString()`, `GetHashCode()`, and `Equals()`, as well as operator overloads.

## ValueObject

A value object is an an immutable data model representing one or more values. Such an object is identified and compared by its values. Value objects cannot be mutated, but new ones with different values can be created. Built-in examples from .NET itself are `string` and `DateTime`.

Firstly, the base type offers a number `protected static` methods to help perform common validations, such as `ContainsNonWordCharacters()`.

More importantly, consider the following type:

```cs
public class Color : ValueObject
{
	public ushort Red { get; }
	public ushort Green { get; }
	public ushort Blue { get; }

	public Color(ushort red, ushort green, ushort blue)
	{
		this.Red = red;
		this.Green = green;
		this.Blue = blue;
	}
}
```

This is the non-boilerplate portion of the value object, i.e. everything that we would like to define by hand. However, the type is missing the following:

- A `ToString()` override.
- A `GetHashCode()` override.
- An `Equals()` override.
- The `IEquatable<Color>` interface implementation.
- Operator overloads for `==` and `!=` based on `Equals()`, since a value object only ever cares about its contents, never its reference identity.
- Potentially the `IComparable<Color>` interface implementation.
- Correctly configured nullable reference types (`?` vs. no `?`) on all mentioned boilerplate code.
- Unit tests on any _hand-written_ boilerplate code.

Change the type as follows to have source generators tackle all of the above:

```cs
[SourceGenerated]
public partial class Color : ValueObject
{
	// Snip
}
```

The `IComparable<Color>` interface can optionally be added, if the type is considered to have a natural order. In such case, the type's properties are compared in the order in which they are defined. When adding the interface, make sure that the properties are defined in the intended order for comparison.

Note that if we inherit from `ValueObject` but omit the `SourceGeneratedAttribute`, we get partial benefits:

- Overriding `ToString()` is made mandatory before the type will build.
- `GetHashCode()` and `Equals()` are overridden to throw a `NotSupportedException`. Value objects should use structural equality, and an exception is better than unintentional reference equality (i.e. bugs).
- Operators `==` and `!=` are implemented to delegate to `Equals()`, to avoid unintentional reference equality.

## WrapperValueObject

A wrapper value object is a value object that represents and wraps a single value. For example, a domain model may define a `Description` value object, a string with certain restrictions on its length and permitted characters.

The wrapper value object is just another value object. Its existence is merely a technical detail to make it easier to implement value objects that represent a single value.

Consider the following type:

```cs
public class Description : WrapperValueObject<string>
{
	protected override StringComparison StringComparison => StringComparison.Ordinal;

	public string Value { get; }

	public Description(string value)
	{
		this.Value = value ?? throw new ArgumentNullException(nameof(value));

		if (this.Value.Length > 255) throw new ArgumentException("Too long.");

		if (ContainsNonWordCharacters(this.Value)) throw new ArgumentException("Nonsense.");
	}
}
```

Besides all the things that the value object in the previous section was missing, this type is missing the following:

- An implementation of the `ContainsNonWordCharacters()` method.
- An explicit conversion from `string` (explicit since not every string is a `Description`).
- An implicit conversion to `string` (implicit since every `Description` is a valid `string`).
- If the underlying type had been a value type (e.g. `int`), conversions from and to its nullable counterpart (e.g. `int?`).
- Ideally, JSON converters that convert instances to and from `"MyDescription"` rather than `{"Value":"MyDescription"}`.

Change the type as follows to have source generators tackle all of the above:

```cs
[SourceGenerated]
public partial class Description : WrapperValueObject<string>
{
	// Snip
}
```

The `IComparable<Description>` interface can optionally be added, if the type is considered to have a natural order.

## Entity

An entity is a data model that is defined by its identity and a thread of continuity. It may be mutated during its life cycle. Entities are often stored in a database.

For entities themselves, the package offers base types, with no source generation required. However, it is often desirable to have a custom type for an entity's ID. For example, `PaymentId` tends to be a more expressive type than `ulong`. Unfortunately, such custom ID types tend to consist of boilerplate code that gets in the way, is a hassle to write, and is easy to make mistakes in.

Consider the following type:

```cs
public class Payment : Entity<PaymentId>
{
	public string Currency { get; }
	public decimal Amount { get; }

	public Payment(string currency, decimal amount)
		: base(new PaymentId())
	{
		this.Currency = currency ?? throw new ArgumentNullException(nameof(currency));
		this.Amount = amount;
	}
}
```

The entity needs a `PaymentId` type. This type could be a full-fledged `WrapperValueObject<ulong>` or `WrapperValueObject<string>`, with `IComparable<PaymentId>`. In fact, it might also be desirable for such a type to be a struct.

Change the type as follows to get a source-generated ID type for the entity:

```cs
public class Payment : Entity<PaymentId, string>
{
	// Snip
}
```

The `Entity<TId, TIdPrimitive>` base class is what triggers source generation of the `TId`, if no such type exists. The `TIdPrimitive` type parameter specifies the underlying primitive to use. Note that the generated ID type is itself a value object, as is customary in DDD.

The entity could then be modified as follows to create a new, unique ID on construction:

```cs
	public Payment(string currency, decimal amount)
		: base(new PaymentId(Guid.NewGuid().ToString("N")))
	{
		// Snip
	}
```

For a more database-friendly alternative to GUIDs, see [Distributed IDs](https://github.com/TheArchitectDev/Architect.Identities#distributed-ids).

## DummyBuilder

Domain objects have parameterized constructors, so that they can guarantee a valid state. For many value objects, such constructors are unlikely to ever change: `new PaymentId(1)`, `new Description("Example")`, `new Color(1, 1, 1)`.

However, entities have constructors that tend to change. The same applies for value objects that exist simply to group clusters of an entity's properties, e.g. `PaymentConfiguration`. When one of these constructors changes, such as when the `Payment` gets a new property (one that should be passed to the constructor), then all the callers need to change accordingly.

Usually, production code has just a handful of callers of an entity's constructor. However, _test code_ can easily have dozens of callers of that constructor.

The simple act of adding one property would require dozens of additional changes instead of a handful, only because of the existence of test code. The changes are "dumb" changes, as the test methods do not care about the new property, which never existed when they were written.

The Builder pattern fixes this problem:

```cs
public class PaymentDummyBuilder : DummyBuilder<Payment, PaymentDummyBuilder>
{
	// Have a default value for each property, along with a fluent method to change it

	private string Currency { get; set; } = "EUR";
	public PaymentDummyBuilder WithCurrency(string value) => this.With(b => b.Currency = value);

	private decimal Amount { get; set; } = 1.00m;
	public PaymentDummyBuilder WithAmount(decimal value) => this.With(b => b.Amount = value);

	// Have a Build() method to invoke the most usual constructor with the configured values

	public override Payment Build()
	{
		var result = new Payment(
			currency: this.Currency,
			amount: this.Amount);
		return result;
	}
}
```

Test methods avoid constructor invocations, e.g. `new Payment("EUR", 1.00m)`, and instead use the following:

```cs
new PaymentBuilder().Build(); // Completely default instance
new PaymentBuilder().WithCurrency("USD").Build(); // Partially modified instance for a specific test
```

For example, to test that the constructor throws the appropriate exception when given a null currency:

```cs
Assert.Throws<ArgumentNullException>(() => new PaymentBuilder().WithCurrency(null!).Build());
```

This way, whenever a constructor is changed, the only test code that breaks is the dummy builder. Instead of dozens of additional changes, we need only make a handful.

As the builder is repaired to account for the changed constructor, all tests work again. If a new constructor parameter was added, existing tests tend to work perfectly fine as long as the builder provides a sensible default value for the parameter.

Unfortunately, the dummy builders tend to consist of boilerplate code and can be tedious to write and maintain.

Change the type as follows to get source generation for it:

```cs
[SourceGenerated]
public partial class PaymentDummyBuilder : DummyBuilder<Payment, PaymentDummyBuilder>
{
	// Anything defined manually is omitted by the generated source code, i.e. manual code always takes precedence

	// The source generator is fairly good at instantiating default values, but not everything it generates is sensible to the domain model
	// Since the source generator cannot guess what an example currency value might look like, we define that property and its initializer manually
	// Everything else we let the source generator provide

	private string Currency { get; set; } = "EUR";
}
```

The generated `Build()` method opts for the most visible, simplest parameterized constructor, since it tends to represent the most "regular" way of constructing the domain object. Specifically, it picks by { greatest visibility, parameterized over default, fewest parameters }. The builder's properties and fluent methods are based on that same constructor. We can deviate by manually implementing the `Build()` method and manually adding properties and fluent methods. To remove generated fluent methods, we can obscure them by manually implementing them as private, protected, or internal.

## Testing

### Generated Files

While "Go To Definition" works for inspecting source-generated code, sometimes you may want to have the generated code in files.
To have source generators write a copy to a file for each generated piece of code, add the following to the project file containing your `[SourceGenerated]` types and find the files under the `obj` directory:

```xml
<PropertyGroup>
	<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
	<CompilerGeneratedFilesOutputPath>$(BaseIntermediateOutputPath)/GeneratedFiles</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

### Debugging

Source generators can be debugged by enabling the following (outcommented) line in the `DomainModeling.Generator` project. To start debugging, rebuild and choose the current Visual Studio instance in the dialog that appears.

```cs
if (!System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Launch();
```
