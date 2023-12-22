using System.Diagnostics.CodeAnalysis;

namespace Architect.DomainModeling.Configuration;

/// <summary>
/// An instance of this abstraction configures a miscellaneous component when it comes to <see cref="IEntity"/> types.
/// One example is a convention configurator for Entity Framework.
/// </summary>
public interface IEntityConfigurator
{
	/// <summary>
	/// A callback to configure an entity of type <typeparamref name="TEntity"/>.
	/// </summary>
	void ConfigureEntity<
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] TEntity>(
			in Args args)
		where TEntity : IEntity;

	public readonly struct Args
	{
		public bool HasDefaultConstructor { get; init; }
	}
}
