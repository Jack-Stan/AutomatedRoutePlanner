-- SQL Script om 150 E-Scooters toe te voegen in zone Kortrijk (ID = 2)
-- Verspreid over 10 verschillende locaties in Kortrijk
-- BELANGRIJK: Run eerst database_setup.sql om de tabellen aan te maken!

USE [Hoppydb]
GO

-- Controleer of Vehicles tabel bestaat
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Vehicles' AND xtype='U')
BEGIN
    PRINT 'ERROR: Vehicles tabel bestaat niet! Run eerst database_setup.sql'
    RETURN
END

-- Controleer of Zone Kortrijk (ID=2) bestaat
IF NOT EXISTS (SELECT * FROM Zones WHERE Id = 2)
BEGIN
    PRINT 'ERROR: Zone Kortrijk (ID=2) bestaat niet! Run eerst database_setup.sql'
    RETURN
END

PRINT 'Database setup OK. Vehicles data invoegen...'

-- Insert 150 E-Scooters in Kortrijk verspreid over 10 locaties
INSERT INTO Vehicles (ExternalId, RegistrationNumber, VehicleType, ZoneId, CurrentParkingZoneId, Latitude, Longitude, BatteryLevel, NeedsBatteryReplacement, IsAvailable, LastUpdated)
VALUES
-- Locatie 1: Grote Markt (centrum Kortrijk) - 15 scooters
('SCOOTER_KOR_001', 'KOR-001', 'E-Scooter', 2, NULL, 50.8297, 3.2649, 15, 1, 1, GETDATE()),
('SCOOTER_KOR_002', 'KOR-002', 'E-Scooter', 2, NULL, 50.8295, 3.2651, 22, 1, 1, GETDATE()),
('SCOOTER_KOR_003', 'KOR-003', 'E-Scooter', 2, NULL, 50.8299, 3.2647, 18, 1, 1, GETDATE()),
('SCOOTER_KOR_004', 'KOR-004', 'E-Scooter', 2, NULL, 50.8293, 3.2653, 8, 1, 1, GETDATE()),
('SCOOTER_KOR_005', 'KOR-005', 'E-Scooter', 2, NULL, 50.8301, 3.2645, 91, 0, 1, GETDATE()),
('SCOOTER_KOR_006', 'KOR-006', 'E-Scooter', 2, NULL, 50.8291, 3.2655, 14, 1, 1, GETDATE()),
('SCOOTER_KOR_007', 'KOR-007', 'E-Scooter', 2, NULL, 50.8303, 3.2643, 67, 0, 1, GETDATE()),
('SCOOTER_KOR_008', 'KOR-008', 'E-Scooter', 2, NULL, 50.8289, 3.2657, 21, 1, 1, GETDATE()),
('SCOOTER_KOR_009', 'KOR-009', 'E-Scooter', 2, NULL, 50.8305, 3.2641, 88, 0, 1, GETDATE()),
('SCOOTER_KOR_010', 'KOR-010', 'E-Scooter', 2, NULL, 50.8287, 3.2659, 12, 1, 1, GETDATE()),
('SCOOTER_KOR_011', 'KOR-011', 'E-Scooter', 2, NULL, 50.8307, 3.2639, 76, 0, 1, GETDATE()),
('SCOOTER_KOR_012', 'KOR-012', 'E-Scooter', 2, NULL, 50.8285, 3.2661, 19, 1, 1, GETDATE()),
('SCOOTER_KOR_013', 'KOR-013', 'E-Scooter', 2, NULL, 50.8309, 3.2637, 84, 0, 1, GETDATE()),
('SCOOTER_KOR_014', 'KOR-014', 'E-Scooter', 2, NULL, 50.8283, 3.2663, 25, 1, 1, GETDATE()),
('SCOOTER_KOR_015', 'KOR-015', 'E-Scooter', 2, NULL, 50.8311, 3.2635, 93, 0, 1, GETDATE()),

-- Locatie 2: Station Kortrijk - 15 scooters  
('SCOOTER_KOR_016', 'KOR-016', 'E-Scooter', 2, NULL, 50.8246, 3.2564, 17, 1, 1, GETDATE()),
('SCOOTER_KOR_017', 'KOR-017', 'E-Scooter', 2, NULL, 50.8244, 3.2566, 89, 0, 1, GETDATE()),
('SCOOTER_KOR_018', 'KOR-018', 'E-Scooter', 2, NULL, 50.8248, 3.2562, 23, 1, 1, GETDATE()),
('SCOOTER_KOR_019', 'KOR-019', 'E-Scooter', 2, NULL, 50.8242, 3.2568, 11, 1, 1, GETDATE()),
('SCOOTER_KOR_020', 'KOR-020', 'E-Scooter', 2, NULL, 50.8250, 3.2560, 77, 0, 1, GETDATE()),
('SCOOTER_KOR_021', 'KOR-021', 'E-Scooter', 2, NULL, 50.8240, 3.2570, 20, 1, 1, GETDATE()),
('SCOOTER_KOR_022', 'KOR-022', 'E-Scooter', 2, NULL, 50.8252, 3.2558, 95, 0, 1, GETDATE()),
('SCOOTER_KOR_023', 'KOR-023', 'E-Scooter', 2, NULL, 50.8238, 3.2572, 16, 1, 1, GETDATE()),
('SCOOTER_KOR_024', 'KOR-024', 'E-Scooter', 2, NULL, 50.8254, 3.2556, 82, 0, 1, GETDATE()),
('SCOOTER_KOR_025', 'KOR-025', 'E-Scooter', 2, NULL, 50.8236, 3.2574, 24, 1, 1, GETDATE()),
('SCOOTER_KOR_026', 'KOR-026', 'E-Scooter', 2, NULL, 50.8256, 3.2554, 90, 0, 1, GETDATE()),
('SCOOTER_KOR_027', 'KOR-027', 'E-Scooter', 2, NULL, 50.8234, 3.2576, 13, 1, 1, GETDATE()),
('SCOOTER_KOR_028', 'KOR-028', 'E-Scooter', 2, NULL, 50.8258, 3.2552, 75, 0, 1, GETDATE()),
('SCOOTER_KOR_029', 'KOR-029', 'E-Scooter', 2, NULL, 50.8232, 3.2578, 21, 1, 1, GETDATE()),
('SCOOTER_KOR_030', 'KOR-030', 'E-Scooter', 2, NULL, 50.8260, 3.2550, 86, 0, 1, GETDATE()),

-- Locatie 3: K Shopping Center - 15 scooters
('SCOOTER_KOR_031', 'KOR-031', 'E-Scooter', 2, NULL, 50.8187, 3.2443, 19, 1, 1, GETDATE()),
('SCOOTER_KOR_032', 'KOR-032', 'E-Scooter', 2, NULL, 50.8185, 3.2445, 73, 0, 1, GETDATE()),
('SCOOTER_KOR_033', 'KOR-033', 'E-Scooter', 2, NULL, 50.8189, 3.2441, 22, 1, 1, GETDATE()),
('SCOOTER_KOR_034', 'KOR-034', 'E-Scooter', 2, NULL, 50.8183, 3.2447, 9, 1, 1, GETDATE()),
('SCOOTER_KOR_035', 'KOR-035', 'E-Scooter', 2, NULL, 50.8191, 3.2439, 94, 0, 1, GETDATE()),
('SCOOTER_KOR_036', 'KOR-036', 'E-Scooter', 2, NULL, 50.8181, 3.2449, 17, 1, 1, GETDATE()),
('SCOOTER_KOR_037', 'KOR-037', 'E-Scooter', 2, NULL, 50.8193, 3.2437, 81, 0, 1, GETDATE()),
('SCOOTER_KOR_038', 'KOR-038', 'E-Scooter', 2, NULL, 50.8179, 3.2451, 25, 1, 1, GETDATE()),
('SCOOTER_KOR_039', 'KOR-039', 'E-Scooter', 2, NULL, 50.8195, 3.2435, 68, 0, 1, GETDATE()),
('SCOOTER_KOR_040', 'KOR-040', 'E-Scooter', 2, NULL, 50.8177, 3.2453, 15, 1, 1, GETDATE()),
('SCOOTER_KOR_041', 'KOR-041', 'E-Scooter', 2, NULL, 50.8197, 3.2433, 92, 0, 1, GETDATE()),
('SCOOTER_KOR_042', 'KOR-042', 'E-Scooter', 2, NULL, 50.8175, 3.2455, 12, 1, 1, GETDATE()),
('SCOOTER_KOR_043', 'KOR-043', 'E-Scooter', 2, NULL, 50.8199, 3.2431, 79, 0, 1, GETDATE()),
('SCOOTER_KOR_044', 'KOR-044', 'E-Scooter', 2, NULL, 50.8173, 3.2457, 23, 1, 1, GETDATE()),
('SCOOTER_KOR_045', 'KOR-045', 'E-Scooter', 2, NULL, 50.8201, 3.2429, 87, 0, 1, GETDATE()),

-- Locatie 4: Begijnhof park - 15 scooters
('SCOOTER_KOR_046', 'KOR-046', 'E-Scooter', 2, NULL, 50.8312, 3.2698, 20, 1, 1, GETDATE()),
('SCOOTER_KOR_047', 'KOR-047', 'E-Scooter', 2, NULL, 50.8310, 3.2700, 74, 0, 1, GETDATE()),
('SCOOTER_KOR_048', 'KOR-048', 'E-Scooter', 2, NULL, 50.8314, 3.2696, 18, 1, 1, GETDATE()),
('SCOOTER_KOR_049', 'KOR-049', 'E-Scooter', 2, NULL, 50.8308, 3.2702, 10, 1, 1, GETDATE()),
('SCOOTER_KOR_050', 'KOR-050', 'E-Scooter', 2, NULL, 50.8316, 3.2694, 96, 0, 1, GETDATE()),
('SCOOTER_KOR_051', 'KOR-051', 'E-Scooter', 2, NULL, 50.8306, 3.2704, 14, 1, 1, GETDATE()),
('SCOOTER_KOR_052', 'KOR-052', 'E-Scooter', 2, NULL, 50.8318, 3.2692, 83, 0, 1, GETDATE()),
('SCOOTER_KOR_053', 'KOR-053', 'E-Scooter', 2, NULL, 50.8304, 3.2706, 24, 1, 1, GETDATE()),
('SCOOTER_KOR_054', 'KOR-054', 'E-Scooter', 2, NULL, 50.8320, 3.2690, 71, 0, 1, GETDATE()),
('SCOOTER_KOR_055', 'KOR-055', 'E-Scooter', 2, NULL, 50.8302, 3.2708, 16, 1, 1, GETDATE()),
('SCOOTER_KOR_056', 'KOR-056', 'E-Scooter', 2, NULL, 50.8322, 3.2688, 89, 0, 1, GETDATE()),
('SCOOTER_KOR_057', 'KOR-057', 'E-Scooter', 2, NULL, 50.8300, 3.2710, 11, 1, 1, GETDATE()),
('SCOOTER_KOR_058', 'KOR-058', 'E-Scooter', 2, NULL, 50.8324, 3.2686, 78, 0, 1, GETDATE()),
('SCOOTER_KOR_059', 'KOR-059', 'E-Scooter', 2, NULL, 50.8298, 3.2712, 25, 1, 1, GETDATE()),
('SCOOTER_KOR_060', 'KOR-060', 'E-Scooter', 2, NULL, 50.8326, 3.2684, 95, 0, 1, GETDATE()),

-- Locatie 5: Sportcomplex Lange Munte - 15 scooters
('SCOOTER_KOR_061', 'KOR-061', 'E-Scooter', 2, NULL, 50.8354, 3.2587, 13, 1, 1, GETDATE()),
('SCOOTER_KOR_062', 'KOR-062', 'E-Scooter', 2, NULL, 50.8352, 3.2589, 80, 0, 1, GETDATE()),
('SCOOTER_KOR_063', 'KOR-063', 'E-Scooter', 2, NULL, 50.8356, 3.2585, 21, 1, 1, GETDATE()),
('SCOOTER_KOR_064', 'KOR-064', 'E-Scooter', 2, NULL, 50.8350, 3.2591, 7, 1, 1, GETDATE()),
('SCOOTER_KOR_065', 'KOR-065', 'E-Scooter', 2, NULL, 50.8358, 3.2583, 87, 0, 1, GETDATE()),
('SCOOTER_KOR_066', 'KOR-066', 'E-Scooter', 2, NULL, 50.8348, 3.2593, 18, 1, 1, GETDATE()),
('SCOOTER_KOR_067', 'KOR-067', 'E-Scooter', 2, NULL, 50.8360, 3.2581, 72, 0, 1, GETDATE()),
('SCOOTER_KOR_068', 'KOR-068', 'E-Scooter', 2, NULL, 50.8346, 3.2595, 23, 1, 1, GETDATE()),
('SCOOTER_KOR_069', 'KOR-069', 'E-Scooter', 2, NULL, 50.8362, 3.2579, 91, 0, 1, GETDATE()),
('SCOOTER_KOR_070', 'KOR-070', 'E-Scooter', 2, NULL, 50.8344, 3.2597, 15, 1, 1, GETDATE()),
('SCOOTER_KOR_071', 'KOR-071', 'E-Scooter', 2, NULL, 50.8364, 3.2577, 85, 0, 1, GETDATE()),
('SCOOTER_KOR_072', 'KOR-072', 'E-Scooter', 2, NULL, 50.8342, 3.2599, 19, 1, 1, GETDATE()),
('SCOOTER_KOR_073', 'KOR-073', 'E-Scooter', 2, NULL, 50.8366, 3.2575, 76, 0, 1, GETDATE()),
('SCOOTER_KOR_074', 'KOR-074', 'E-Scooter', 2, NULL, 50.8340, 3.2601, 17, 1, 1, GETDATE()),
('SCOOTER_KOR_075', 'KOR-075', 'E-Scooter', 2, NULL, 50.8290, 3.2675, 22, 1, 1, GETDATE()),

-- Locatie 6: Kortrijk Xpo - 10 scooters (binnen zone) + 5 OUT OF ZONE
('SCOOTER_KOR_076', 'KOR-076', 'E-Scooter', 2, NULL, 50.8288, 3.2456, 85, 0, 1, GETDATE()),
('SCOOTER_KOR_077', 'KOR-077', 'E-Scooter', 2, NULL, 50.8286, 3.2458, 16, 1, 1, GETDATE()),
('SCOOTER_KOR_078', 'KOR-078', 'E-Scooter', 2, NULL, 50.8290, 3.2454, 71, 0, 1, GETDATE()),
('SCOOTER_KOR_079', 'KOR-079', 'E-Scooter', 2, NULL, 50.8284, 3.2460, 12, 1, 1, GETDATE()),
('SCOOTER_KOR_080', 'KOR-080', 'E-Scooter', 2, NULL, 50.8292, 3.2452, 88, 0, 1, GETDATE()),
('SCOOTER_KOR_081', 'KOR-081', 'E-Scooter', 2, NULL, 50.8282, 3.2462, 19, 1, 1, GETDATE()),
('SCOOTER_KOR_082', 'KOR-082', 'E-Scooter', 2, NULL, 50.8294, 3.2450, 67, 0, 1, GETDATE()),
('SCOOTER_KOR_083', 'KOR-083', 'E-Scooter', 2, NULL, 50.8280, 3.2464, 14, 1, 1, GETDATE()),
('SCOOTER_KOR_084', 'KOR-084', 'E-Scooter', 2, NULL, 50.8296, 3.2448, 76, 0, 1, GETDATE()),
('SCOOTER_KOR_085', 'KOR-085', 'E-Scooter', 2, NULL, 50.8278, 3.2466, 23, 1, 1, GETDATE()),

-- OUT OF ZONE - Marke (noord van Kortrijk zone) - 5 scooters  
('SCOOTER_KOR_086', 'KOR-086', 'E-Scooter', 2, NULL, 50.8650, 3.2598, 83, 0, 1, GETDATE()), -- Te ver noord
('SCOOTER_KOR_087', 'KOR-087', 'E-Scooter', 2, NULL, 50.8648, 3.2600, 8, 1, 1, GETDATE()),
('SCOOTER_KOR_088', 'KOR-088', 'E-Scooter', 2, NULL, 50.8652, 3.2596, 59, 0, 1, GETDATE()),
('SCOOTER_KOR_089', 'KOR-089', 'E-Scooter', 2, NULL, 50.8646, 3.2602, 21, 1, 1, GETDATE()),
('SCOOTER_KOR_090', 'KOR-090', 'E-Scooter', 2, NULL, 50.8654, 3.2594, 17, 1, 1, GETDATE()),

-- Locatie 7: Kortrijk Weide (park) - 15 scooters
('SCOOTER_KOR_091', 'KOR-091', 'E-Scooter', 2, NULL, 50.8332, 3.2712, 24, 1, 1, GETDATE()),
('SCOOTER_KOR_092', 'KOR-092', 'E-Scooter', 2, NULL, 50.8330, 3.2714, 69, 0, 1, GETDATE()),
('SCOOTER_KOR_093', 'KOR-093', 'E-Scooter', 2, NULL, 50.8334, 3.2710, 11, 1, 1, GETDATE()),
('SCOOTER_KOR_094', 'KOR-094', 'E-Scooter', 2, NULL, 50.8328, 3.2716, 86, 0, 1, GETDATE()),
('SCOOTER_KOR_095', 'KOR-095', 'E-Scooter', 2, NULL, 50.8336, 3.2708, 20, 1, 1, GETDATE()),
('SCOOTER_KOR_096', 'KOR-096', 'E-Scooter', 2, NULL, 50.8326, 3.2718, 73, 0, 1, GETDATE()),
('SCOOTER_KOR_097', 'KOR-097', 'E-Scooter', 2, NULL, 50.8338, 3.2706, 16, 1, 1, GETDATE()),
('SCOOTER_KOR_098', 'KOR-098', 'E-Scooter', 2, NULL, 50.8324, 3.2720, 89, 0, 1, GETDATE()),
('SCOOTER_KOR_099', 'KOR-099', 'E-Scooter', 2, NULL, 50.8340, 3.2704, 13, 1, 1, GETDATE()),
('SCOOTER_KOR_100', 'KOR-100', 'E-Scooter', 2, NULL, 50.8322, 3.2722, 77, 0, 1, GETDATE()),
('SCOOTER_KOR_101', 'KOR-101', 'E-Scooter', 2, NULL, 50.8342, 3.2702, 25, 1, 1, GETDATE()),
('SCOOTER_KOR_102', 'KOR-102', 'E-Scooter', 2, NULL, 50.8320, 3.2724, 91, 0, 1, GETDATE()),
('SCOOTER_KOR_103', 'KOR-103', 'E-Scooter', 2, NULL, 50.8344, 3.2700, 18, 1, 1, GETDATE()),
('SCOOTER_KOR_104', 'KOR-104', 'E-Scooter', 2, NULL, 50.8318, 3.2726, 84, 0, 1, GETDATE()),
('SCOOTER_KOR_105', 'KOR-105', 'E-Scooter', 2, NULL, 50.8346, 3.2698, 22, 1, 1, GETDATE()),

-- Locatie 8: Texture museum omgeving - 15 scooters
('SCOOTER_KOR_106', 'KOR-106', 'E-Scooter', 2, NULL, 50.8267, 3.2634, 19, 1, 1, GETDATE()),
('SCOOTER_KOR_107', 'KOR-107', 'E-Scooter', 2, NULL, 50.8265, 3.2636, 75, 0, 1, GETDATE()),
('SCOOTER_KOR_108', 'KOR-108', 'E-Scooter', 2, NULL, 50.8269, 3.2632, 14, 1, 1, GETDATE()),
('SCOOTER_KOR_109', 'KOR-109', 'E-Scooter', 2, NULL, 50.8263, 3.2638, 87, 0, 1, GETDATE()),
('SCOOTER_KOR_110', 'KOR-110', 'E-Scooter', 2, NULL, 50.8271, 3.2630, 23, 1, 1, GETDATE()),
('SCOOTER_KOR_111', 'KOR-111', 'E-Scooter', 2, NULL, 50.8261, 3.2640, 68, 0, 1, GETDATE()),
('SCOOTER_KOR_112', 'KOR-112', 'E-Scooter', 2, NULL, 50.8273, 3.2628, 12, 1, 1, GETDATE()),
('SCOOTER_KOR_113', 'KOR-113', 'E-Scooter', 2, NULL, 50.8259, 3.2642, 93, 0, 1, GETDATE()),
('SCOOTER_KOR_114', 'KOR-114', 'E-Scooter', 2, NULL, 50.8275, 3.2626, 17, 1, 1, GETDATE()),
('SCOOTER_KOR_115', 'KOR-115', 'E-Scooter', 2, NULL, 50.8257, 3.2644, 80, 0, 1, GETDATE()),
('SCOOTER_KOR_116', 'KOR-116', 'E-Scooter', 2, NULL, 50.8277, 3.2624, 21, 1, 1, GETDATE()),
('SCOOTER_KOR_117', 'KOR-117', 'E-Scooter', 2, NULL, 50.8255, 3.2646, 74, 0, 1, GETDATE()),
('SCOOTER_KOR_118', 'KOR-118', 'E-Scooter', 2, NULL, 50.8279, 3.2622, 15, 1, 1, GETDATE()),
('SCOOTER_KOR_119', 'KOR-119', 'E-Scooter', 2, NULL, 50.8253, 3.2648, 82, 0, 1, GETDATE()),
('SCOOTER_KOR_120', 'KOR-120', 'E-Scooter', 2, NULL, 50.8281, 3.2620, 24, 1, 1, GETDATE()),

-- Locatie 9: Broelkaai (Leie rivier) - 15 scooters
('SCOOTER_KOR_121', 'KOR-121', 'E-Scooter', 2, NULL, 50.8221, 3.2561, 16, 1, 1, GETDATE()),
('SCOOTER_KOR_122', 'KOR-122', 'E-Scooter', 2, NULL, 50.8219, 3.2563, 78, 0, 1, GETDATE()),
('SCOOTER_KOR_123', 'KOR-123', 'E-Scooter', 2, NULL, 50.8223, 3.2559, 20, 1, 1, GETDATE()),
('SCOOTER_KOR_124', 'KOR-124', 'E-Scooter', 2, NULL, 50.8217, 3.2565, 9, 1, 1, GETDATE()),
('SCOOTER_KOR_125', 'KOR-125', 'E-Scooter', 2, NULL, 50.8225, 3.2557, 85, 0, 1, GETDATE()),
('SCOOTER_KOR_126', 'KOR-126', 'E-Scooter', 2, NULL, 50.8215, 3.2567, 22, 1, 1, GETDATE()),
('SCOOTER_KOR_127', 'KOR-127', 'E-Scooter', 2, NULL, 50.8227, 3.2555, 71, 0, 1, GETDATE()),
('SCOOTER_KOR_128', 'KOR-128', 'E-Scooter', 2, NULL, 50.8213, 3.2569, 18, 1, 1, GETDATE()),
('SCOOTER_KOR_129', 'KOR-129', 'E-Scooter', 2, NULL, 50.8229, 3.2553, 92, 0, 1, GETDATE()),
('SCOOTER_KOR_130', 'KOR-130', 'E-Scooter', 2, NULL, 50.8211, 3.2571, 14, 1, 1, GETDATE()),
('SCOOTER_KOR_131', 'KOR-131', 'E-Scooter', 2, NULL, 50.8231, 3.2551, 76, 0, 1, GETDATE()),
('SCOOTER_KOR_132', 'KOR-132', 'E-Scooter', 2, NULL, 50.8209, 3.2573, 25, 1, 1, GETDATE()),
('SCOOTER_KOR_133', 'KOR-133', 'E-Scooter', 2, NULL, 50.8233, 3.2549, 88, 0, 1, GETDATE()),
('SCOOTER_KOR_134', 'KOR-134', 'E-Scooter', 2, NULL, 50.8207, 3.2575, 19, 1, 1, GETDATE()),
('SCOOTER_KOR_135', 'KOR-135', 'E-Scooter', 2, NULL, 50.8235, 3.2547, 83, 0, 1, GETDATE()),

-- Locatie 10: Leie oevers (waterkant noord) - 10 scooters (binnen zone)
('SCOOTER_KOR_136', 'KOR-136', 'E-Scooter', 2, NULL, 50.8267, 3.2723, 71, 0, 1, GETDATE()),
('SCOOTER_KOR_137', 'KOR-137', 'E-Scooter', 2, NULL, 50.8265, 3.2725, 16, 1, 1, GETDATE()),
('SCOOTER_KOR_138', 'KOR-138', 'E-Scooter', 2, NULL, 50.8269, 3.2721, 85, 0, 1, GETDATE()),
('SCOOTER_KOR_139', 'KOR-139', 'E-Scooter', 2, NULL, 50.8263, 3.2727, 11, 1, 1, GETDATE()),
('SCOOTER_KOR_140', 'KOR-140', 'E-Scooter', 2, NULL, 50.8271, 3.2719, 64, 0, 1, GETDATE()),
('SCOOTER_KOR_141', 'KOR-141', 'E-Scooter', 2, NULL, 50.8261, 3.2729, 24, 1, 1, GETDATE()),
('SCOOTER_KOR_142', 'KOR-142', 'E-Scooter', 2, NULL, 50.8273, 3.2717, 90, 0, 1, GETDATE()),
('SCOOTER_KOR_143', 'KOR-143', 'E-Scooter', 2, NULL, 50.8259, 3.2731, 18, 1, 1, GETDATE()),
('SCOOTER_KOR_144', 'KOR-144', 'E-Scooter', 2, NULL, 50.8275, 3.2715, 77, 0, 1, GETDATE()),
('SCOOTER_KOR_145', 'KOR-145', 'E-Scooter', 2, NULL, 50.8257, 3.2733, 20, 1, 1, GETDATE()),

-- Locatie 11: OUT OF ZONE - Wevelgem (buiten Kortrijk zone) - 5 scooters
('SCOOTER_KOR_146', 'KOR-146', 'E-Scooter', 2, NULL, 50.8050, 3.1850, 83, 0, 1, GETDATE()), -- Te ver zuid/west
('SCOOTER_KOR_147', 'KOR-147', 'E-Scooter', 2, NULL, 50.8048, 3.1852, 15, 1, 1, GETDATE()),
('SCOOTER_KOR_148', 'KOR-148', 'E-Scooter', 2, NULL, 50.8052, 3.1848, 62, 0, 1, GETDATE()),
('SCOOTER_KOR_149', 'KOR-149', 'E-Scooter', 2, NULL, 50.8046, 3.1854, 22, 1, 1, GETDATE()),
('SCOOTER_KOR_150', 'KOR-150', 'E-Scooter', 2, NULL, 50.8054, 3.1846, 9, 1, 1, GETDATE());

-- Controleer het resultaat
SELECT COUNT(*) as 'Totaal E-Scooters in Kortrijk' FROM Vehicles WHERE ZoneId = 2 AND VehicleType = 'E-Scooter';

-- Bekijk vehicles met lage batterij (< 25%) voor route planning
SELECT COUNT(*) as 'E-Scooters met lage batterij (<25%)' FROM Vehicles WHERE ZoneId = 2 AND VehicleType = 'E-Scooter' AND BatteryLevel < 25;

PRINT 'Vehicle data succesvol toegevoegd aan Kortrijk zone!'
