namespace Architect.DomainModeling
{
	/// <summary>
	/// A base class used to implement the Builder pattern, specifically for use in test methods.
	/// </summary>
	/// <typeparam name="TModel">The type constructed by the builder.</typeparam>
	/// <typeparam name="TModelBuilder">The type of the concrete builder itself.</typeparam>
	public abstract class DummyBuilder<TModel, TModelBuilder>
		where TModel : class
		where TModelBuilder : DummyBuilder<TModel, TModelBuilder>
	{
		protected DummyBuilder()
		{
			if (this.GetType() != typeof(TModelBuilder))
				throw new Exception($"Builder class {this.GetType().Name} must specify its own type as the {nameof(TModelBuilder)} type parameter.");
		}

		protected virtual TModelBuilder With(Action<TModelBuilder> assignment)
		{
			assignment((TModelBuilder)this);
			return (TModelBuilder)this;
		}

		public abstract TModel Build();
	}
}
