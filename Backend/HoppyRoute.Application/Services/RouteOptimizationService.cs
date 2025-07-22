using Google.OrTools.ConstraintSolver;
using HoppyRoute.Application.DTOs;
using HoppyRoute.Application.Interfaces;
using HoppyRoute.Domain.Entities;

namespace HoppyRoute.Application.Services
{
    public class RouteOptimizationService : IRouteOptimizationService
    {
        public async Task<RouteOptimizationResult> OptimizeRouteAsync(List<Vehicle> vehicles, int targetDurationMinutes)
        {
            try
            {
                if (vehicles.Count == 0)
                {
                    return new RouteOptimizationResult
                    {
                        Success = false,
                        ErrorMessage = "No vehicles provided for optimization"
                    };
                }

                // Create distance matrix
                var distanceMatrix = await CreateDistanceMatrixAsync(vehicles);
                
                // Set up OR-Tools
                var manager = new RoutingIndexManager(distanceMatrix.GetLength(0), 1, 0);
                var routing = new RoutingModel(manager);

                // Add distance constraint
                int transitCallbackIndex = routing.RegisterTransitCallback((long fromIndex, long toIndex) =>
                {
                    var fromNode = manager.IndexToNode(fromIndex);
                    var toNode = manager.IndexToNode(toIndex);
                    return distanceMatrix[fromNode, toNode];
                });

                routing.SetArcCostEvaluatorOfAllVehicles(transitCallbackIndex);

                // Add time constraint (convert minutes to seconds for OR-Tools)
                routing.AddDimension(transitCallbackIndex, 0, targetDurationMinutes * 60, true, "Time");

                // Set search parameters
                var searchParameters = operations_research_constraint_solver.DefaultRoutingSearchParameters();
                searchParameters.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.PathCheapestArc;
                searchParameters.LocalSearchMetaheuristic = LocalSearchMetaheuristic.Types.Value.GuidedLocalSearch;
                searchParameters.TimeLimit = Google.Protobuf.WellKnownTypes.Duration.FromTimeSpan(TimeSpan.FromSeconds(30));

                // Solve
                var solution = routing.SolveWithParameters(searchParameters);

                if (solution == null)
                {
                    return new RouteOptimizationResult
                    {
                        Success = false,
                        ErrorMessage = "No solution found within time limit"
                    };
                }

                // Extract solution
                var optimalOrder = new List<int>();
                var index = routing.Start(0);
                var totalDistance = 0.0;
                var totalTime = 0;

                while (!routing.IsEnd(index))
                {
                    var nodeIndex = manager.IndexToNode(index);
                    if (nodeIndex > 0) // Skip depot (index 0)
                    {
                        optimalOrder.Add(nodeIndex - 1); // Adjust for depot offset
                    }
                    
                    var previousIndex = index;
                    index = solution.Value(routing.NextVar(index));
                    
                    if (!routing.IsEnd(index))
                    {
                        var fromNode = manager.IndexToNode(previousIndex);
                        var toNode = manager.IndexToNode(index);
                        totalDistance += await CalculateDistanceAsync(
                            vehicles[Math.Max(0, fromNode - 1)].Latitude,
                            vehicles[Math.Max(0, fromNode - 1)].Longitude,
                            vehicles[Math.Max(0, toNode - 1)].Latitude,
                            vehicles[Math.Max(0, toNode - 1)].Longitude);
                        totalTime += distanceMatrix[fromNode, toNode];
                    }
                }

                return new RouteOptimizationResult
                {
                    OptimalOrder = optimalOrder,
                    TotalDurationMinutes = totalTime / 60, // Convert back to minutes
                    TotalDistanceKm = totalDistance,
                    Success = true
                };
            }
            catch (Exception ex)
            {
                return new RouteOptimizationResult
                {
                    Success = false,
                    ErrorMessage = $"Optimization failed: {ex.Message}"
                };
            }
        }

        public async Task<List<int>> OptimizeRouteAsync(CreateRouteRequest request)
        {
            // For now, return the vehicle IDs in the order they were provided
            // In a real implementation, you would use Google OR-Tools for optimization
            return await Task.FromResult(request.VehicleIds);
        }

        public async Task<double> CalculateDistanceAsync(double lat1, double lon1, double lat2, double lon2)
        {
            // Haversine formula for calculating distance between two points
            const double R = 6371; // Earth's radius in kilometers

            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var distance = R * c;

            return await Task.FromResult(distance);
        }

        public async Task<int> CalculateTravelTimeAsync(double lat1, double lon1, double lat2, double lon2)
        {
            // Simple estimation: 30 km/h average speed in urban areas + 5 minutes per stop
            var distance = await CalculateDistanceAsync(lat1, lon1, lat2, lon2);
            var travelTimeMinutes = (distance / 30.0) * 60; // Convert to minutes
            var stopTime = 5; // 5 minutes per stop for battery swap
            
            return (int)Math.Ceiling(travelTimeMinutes + stopTime);
        }

        private async Task<int[,]> CreateDistanceMatrixAsync(List<Vehicle> vehicles)
        {
            var n = vehicles.Count + 1; // +1 for depot (starting point)
            var matrix = new int[n, n];

            // Depot coordinates (center of zone for simplicity)
            var depotLat = vehicles.Average(v => v.Latitude);
            var depotLon = vehicles.Average(v => v.Longitude);

            // Fill matrix
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (i == j)
                    {
                        matrix[i, j] = 0;
                        continue;
                    }

                    double lat1, lon1, lat2, lon2;

                    // Determine coordinates for i
                    if (i == 0)
                    {
                        lat1 = depotLat;
                        lon1 = depotLon;
                    }
                    else
                    {
                        lat1 = vehicles[i - 1].Latitude;
                        lon1 = vehicles[i - 1].Longitude;
                    }

                    // Determine coordinates for j
                    if (j == 0)
                    {
                        lat2 = depotLat;
                        lon2 = depotLon;
                    }
                    else
                    {
                        lat2 = vehicles[j - 1].Latitude;
                        lon2 = vehicles[j - 1].Longitude;
                    }

                    var travelTime = await CalculateTravelTimeAsync(lat1, lon1, lat2, lon2);
                    matrix[i, j] = travelTime * 60; // Convert to seconds for OR-Tools
                }
            }

            return matrix;
        }

        private static double ToRadians(double angle)
        {
            return Math.PI * angle / 180.0;
        }

        public async Task<RouteOptimizationResult> OptimizeRouteWithParkingZonesAsync(List<Vehicle> vehicles, List<ParkingZone> parkingZones, int targetDurationMinutes)
        {
            try
            {
                if (vehicles.Count == 0 || parkingZones.Count == 0)
                {
                    return new RouteOptimizationResult
                    {
                        Success = false,
                        ErrorMessage = "No vehicles or parking zones provided for optimization"
                    };
                }

                // For now, use the simple optimization and add parking zone logic later
                var simpleResult = await OptimizeRouteAsync(vehicles, targetDurationMinutes);
                
                // TODO: Implement parking zone specific optimization
                // This would involve:
                // 1. Finding optimal pickup points (parking zones with vehicles needing battery replacement)
                // 2. Finding optimal dropoff points (parking zones with available spaces)
                // 3. Optimizing the route considering both pickup and dropoff locations
                
                return simpleResult;
            }
            catch (Exception ex)
            {
                return new RouteOptimizationResult
                {
                    Success = false,
                    ErrorMessage = $"Error optimizing route with parking zones: {ex.Message}"
                };
            }
        }
    }
}
