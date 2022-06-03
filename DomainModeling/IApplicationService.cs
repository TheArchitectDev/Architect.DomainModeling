namespace Architect.DomainModeling;

/// <summary>
/// <para>
/// An application service exists on top of the domain model. It orchestrates use cases by calling methods on domain objects.
/// </para>
/// <para>
/// Application services must be stateless.
/// </para>
/// <para>
/// An application service does not make business decisions, which are part of the domain model itself.
/// </para>
/// <para>
/// Often there are non-domain concerns, such as security, caching, or logging, that are added by the application service.
/// </para>
/// </summary>
public interface IApplicationService
{
}
