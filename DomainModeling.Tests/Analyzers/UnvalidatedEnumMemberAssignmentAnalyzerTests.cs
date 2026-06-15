using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace Architect.DomainModeling.Tests.Analyzers;

public class UnvalidatedEnumMemberAssignmentAnalyzerTests
{
	// Unfortunately, we always get "unnecessary suppression" even when the warning is successfully suppressed
	// All we can do is manually outcomment the suppression temporarily to check that each statement in this file still warns

	private const HttpStatusCode NonexistentStatus = (HttpStatusCode)1;
	private const LazyThreadSafetyMode NonexistentThreadSafetyMode = (LazyThreadSafetyMode)999;

	[SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "False positive.")]
	[SuppressMessage("Usage", "UnvalidatedEnumAssignmentToDomainobject:Unvalidated enum assignment to domain object member", Justification = "Testing presence of warning.")]
	public static void AssignUnvalidatedValueToDomainObjectEnumMember_Always_ShouldWarn(
		HttpStatusCode statusCode, LazyThreadSafetyMode lazyThreadSafetyMode,
		HttpStatusCode? nullableStatusCode, LazyThreadSafetyMode? nullableLazyThreadSafetyMode)
	{
#pragma warning disable IDE0034 // Simplify 'default' expression -- Testing presence of warning with this syntax too

		var entity = new TestEntity();
		var valueObject = new TestValueObject();

		entity.Status = default;
		entity.Status = default(HttpStatusCode);
		entity.Status = NonexistentStatus;
		entity.Status = (HttpStatusCode)1;
		entity.Status = statusCode;
		entity.Status = (HttpStatusCode)nullableStatusCode!;
		entity.Status = nullableStatusCode!.Value;

		//valueObject.LazyThreadSafetyMode = default; // Matches a defined value
		//valueObject.LazyThreadSafetyMode = default(LazyThreadSafetyMode); // Matches a defined value
		valueObject.LazyThreadSafetyMode = NonexistentThreadSafetyMode;
		valueObject.LazyThreadSafetyMode = (LazyThreadSafetyMode)999;
		valueObject.LazyThreadSafetyMode = lazyThreadSafetyMode;
		valueObject.LazyThreadSafetyMode = (LazyThreadSafetyMode)nullableLazyThreadSafetyMode!;
		valueObject.LazyThreadSafetyMode = nullableLazyThreadSafetyMode!.Value;

		//entity.NullableStatus = default; // Null is permitted
		//entity.NullableStatus = default(HttpStatusCode?); // Null is permitted
		entity.NullableStatus = default(HttpStatusCode);
		entity.NullableStatus = NonexistentStatus;
		entity.NullableStatus = (HttpStatusCode?)NonexistentStatus;
		entity.NullableStatus = (HttpStatusCode)(HttpStatusCode?)NonexistentStatus;
		entity.NullableStatus = (HttpStatusCode)1;
		entity.NullableStatus = (HttpStatusCode?)(HttpStatusCode)1;
		entity.NullableStatus = statusCode;
		entity.NullableStatus = (HttpStatusCode?)statusCode;
		entity.NullableStatus = (HttpStatusCode)(HttpStatusCode?)statusCode;
		entity.NullableStatus = nullableStatusCode;
		entity.NullableStatus = (HttpStatusCode)nullableStatusCode!;
		entity.NullableStatus = nullableStatusCode!.Value;

		//valueObject.NullableLazyThreadSafetyMode = default; // Null is permitted
		//valueObject.NullableLazyThreadSafetyMode = default(LazyThreadSafetyMode?); // Null is permitted
		//valueObject.NullableLazyThreadSafetyMode = default(LazyThreadSafetyMode); // Matches a defined value
		valueObject.NullableLazyThreadSafetyMode = NonexistentThreadSafetyMode;
		valueObject.NullableLazyThreadSafetyMode = (LazyThreadSafetyMode?)NonexistentThreadSafetyMode;
		valueObject.NullableLazyThreadSafetyMode = (LazyThreadSafetyMode)(LazyThreadSafetyMode?)NonexistentThreadSafetyMode;
		valueObject.NullableLazyThreadSafetyMode = (LazyThreadSafetyMode)999;
		valueObject.NullableLazyThreadSafetyMode = (LazyThreadSafetyMode?)(LazyThreadSafetyMode)999;
		valueObject.NullableLazyThreadSafetyMode = lazyThreadSafetyMode;
		valueObject.NullableLazyThreadSafetyMode = (LazyThreadSafetyMode?)lazyThreadSafetyMode;
		valueObject.NullableLazyThreadSafetyMode = (LazyThreadSafetyMode)(LazyThreadSafetyMode?)lazyThreadSafetyMode;
		valueObject.NullableLazyThreadSafetyMode = nullableLazyThreadSafetyMode;
		valueObject.NullableLazyThreadSafetyMode = (LazyThreadSafetyMode)nullableLazyThreadSafetyMode!;
		valueObject.NullableLazyThreadSafetyMode = nullableLazyThreadSafetyMode!.Value;

		// Problematic arms in (nested) ternaries and switches are detected and fixable
		entity.NullableStatus = new Random().NextDouble() switch
		{
			< 0.1 => HttpStatusCode.OK,
			< 0.2 => new Random().NextDouble() < 0.5
				? statusCode
				: default(HttpStatusCode?),
			_ => null,
		};
		entity.NullableStatus = new Random().NextDouble() < 0.5
			? HttpStatusCode.OK
			: new Random().NextDouble() < 0.5
				? (new Random().NextDouble() < 0.5
					? new Random().NextDouble() switch
					{
						< 0.1 => HttpStatusCode.OK,
						< 0.2 => statusCode,
						_ => null,
					}
					: throw new NotSupportedException("Example exception"))
				: statusCode;

#pragma warning restore IDE0034 // Simplify 'default' expression
	}

	private sealed class TestEntity : IDomainObject
	{
		public HttpStatusCode Status { get; set; }
		public HttpStatusCode? NullableStatus { get; set; }
	}

	private sealed class TestValueObject : IValueObject
	{
		public LazyThreadSafetyMode LazyThreadSafetyMode { get; set; }
		public LazyThreadSafetyMode? NullableLazyThreadSafetyMode { get; set; }
	}
}
