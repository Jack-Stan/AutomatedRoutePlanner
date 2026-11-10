using HoppyRoute.Application.DTOs;
using HoppyRoute.Domain.Entities;

namespace HoppyRoute.Application.Mapping
{
    /// <summary>
    /// The one place a Vehicle becomes a VehicleDto, so route stops, route-stop endpoints and the
    /// vehicle endpoints all return the same fields. Navigation names (ZoneName, CurrentParkingZoneName)
    /// are only filled when the query Included them; there is no lazy loading.
    /// </summary>
    public static class VehicleDtoMapper
    {
        public static VehicleDto ToDto(Vehicle vehicle) => new()
        {
            Id = vehicle.Id,
            ExternalId = vehicle.ExternalId,
            RegistrationNumber = vehicle.RegistrationNumber,
            VehicleType = vehicle.VehicleType,
            ZoneId = vehicle.ZoneId,
            ZoneName = vehicle.Zone?.Name,
            CurrentParkingZoneId = vehicle.CurrentParkingZoneId,
            CurrentParkingZoneName = vehicle.CurrentParkingZone?.Name,
            Latitude = vehicle.Latitude,
            Longitude = vehicle.Longitude,
            BatteryLevel = vehicle.BatteryLevel,
            NeedsBatteryReplacement = vehicle.NeedsBatteryReplacement,
            IsAvailable = vehicle.IsAvailable,
            LastUpdated = vehicle.LastUpdated
        };
    }
}
