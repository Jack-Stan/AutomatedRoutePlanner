namespace HoppyRoute.Domain.Enums
{
    public enum UserRole
    {
        Admin = 0,           // Hoogste niveau - beheert het hele systeem
        FleetManager = 1,    // Beheert een specifieke zone en zijn swappers
        BatterySwapper = 2   // Dagelijkse operationele taken
    }
}
