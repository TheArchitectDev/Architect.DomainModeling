using System.Collections.Concurrent;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Architect.DomainModeling.Generator
{
	/// <summary>
	/// The base class for <see cref="ISourceGenerator"/>s implemented in this package.
	/// </summary>
	public abstract class SourceGenerator : BaseSourceGenerator
	{
		/// <summary>
		/// Helps avoid errors caused by duplicate type names.
		/// </summary>
		private static ConcurrentDictionary<string, bool> GeneratedNames { get; } = new ConcurrentDictionary<string, bool>();

		static SourceGenerator()
		{
#if DEBUG
			// Uncomment the following to debug the source generators
			//if (!System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Launch();
#endif
		}

		// Note: If we ever want to know the .NET version being compiled for, one way could be Execute()'s GeneratorExecutionContext.Compilation.ReferencedAssemblyNames.FirstOrDefault(name => name.Name == "System.Runtime")?.Version.Major

		public new abstract void Initialize(GeneratorInitializationContext context);
		public new abstract void Execute(GeneratorExecutionContext context);

		protected sealed override void WillInitialize(GeneratorInitializationContext context)
		{
			// Allow new compilations to use all names again
			if (GeneratedNames.Count > 0) GeneratedNames.Clear();
		}

		protected sealed override void WillExecute(GeneratorExecutionContext context)
		{
		}

		protected static void AddSource(GeneratorExecutionContext context, string sourceText, ITypeSymbol type)
		{
			var name = $"{type.Name}.Generated";

			if (!GeneratedNames.TryAdd(name, value: default))
				name = $"{type.Name}-{type.ContainingNamespace.ToString().GetStableStringHashCode32()}";

			sourceText = sourceText.NormalizeWhitespace();

			context.AddSource(name, SourceText.From(sourceText, Encoding.UTF8));
		}
	}

	/// <summary>
	/// Split off into a deeper base class so that <see cref="SourceGenerator"/> can hide certain methods behind new ones.
	/// </summary>
	public abstract class BaseSourceGenerator : ISourceGenerator
	{
		public void Initialize(GeneratorInitializationContext context)
		{
			this.WillInitialize(context);
			this.InitializeCore(context);
		}

		protected abstract void WillInitialize(GeneratorInitializationContext context);

		private void InitializeCore(GeneratorInitializationContext context)
		{
			((SourceGenerator)this).Initialize(context);
		}

		public void Execute(GeneratorExecutionContext context)
		{
			this.WillExecute(context);
			this.ExecuteCore(context);
		}

		protected abstract void WillExecute(GeneratorExecutionContext context);

		private void ExecuteCore(GeneratorExecutionContext context)
		{
			((SourceGenerator)this).Execute(context);
		}
	}
}
