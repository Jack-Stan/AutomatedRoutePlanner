namespace HoppyRoute.Application.DTOs
{
    /// <summary>What the controllers need to decide who may act on a route (or on one of its stops).</summary>
    public record RouteAccessInfo(int AssignedSwapperId, int ZoneId);
}
