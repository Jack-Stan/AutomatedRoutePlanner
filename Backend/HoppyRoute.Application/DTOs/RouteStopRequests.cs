using System.ComponentModel.DataAnnotations;
using HoppyRoute.Domain.Enums;

namespace HoppyRoute.Application.DTOs
{
    /// <summary>
    /// Body for PUT routestops/{id}/status. Only InProgress and Skipped are accepted (see the controller).
    /// <see cref="Reason"/> is optional and only valid together with Skipped.
    /// </summary>
    public class UpdateRouteStopStatusRequest
    {
        public RouteStopStatus Status { get; set; }

        [StringLength(500)] // matches RouteStop.SkipReason
        public string? Reason { get; set; }
    }

    /// <summary>Body for PUT routestops/{id}/instructions. Replaces the stop's note; null or empty clears it.</summary>
    public class UpdateRouteStopInstructionsRequest
    {
        [StringLength(500)] // matches RouteStop.Notes
        public string? Notes { get; set; }
    }
}
