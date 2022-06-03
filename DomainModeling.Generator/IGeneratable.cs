using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;

namespace Architect.DomainModeling.Generator
{
	/// <summary>
	/// <para>
	/// Interface intended for record types used to store the transformation data of source generators.
	/// </para>
	/// <para>
	/// Extension methods on this type allow additional data (such as an <see cref="INamedTypeSymbol"/>) to be associated, without that data becoming part of the record's equality implementation.
	/// </para>
	/// </summary>
	internal interface IGeneratable
	{
	}

	internal static class GeneratableExtensions
	{
		public static readonly ConditionalWeakTable<object, object> AdditionalDataPerGeneratable = new ConditionalWeakTable<object, object>();

		public static void SetAssociatedData(this IGeneratable generatable, object data)
		{
			AdditionalDataPerGeneratable.Remove(generatable);
			AdditionalDataPerGeneratable.Add(generatable, data);
		}

		public static TData GetAssociatedData<TData>(this IGeneratable generatable)
		{
			if (!AdditionalDataPerGeneratable.TryGetValue(generatable, out var result))
				throw new KeyNotFoundException("Attemped to retrieve data for the generatable object, but no data was stored.");

			return (TData)result;
		}
	}
}
