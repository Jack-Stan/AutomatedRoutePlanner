-- Hoppy Zones en Regions Update Script (Veilige versie)
-- Verwijder oude testdata en voeg echte Hoppy locaties toe

-- Stap 1: Alle foreign key dependencies opschonen
DELETE FROM ParkingZones; -- Verwijder alle parkingzones eerst
DELETE FROM Routes WHERE Id NOT IN (SELECT DISTINCT RouteId FROM RouteStops WHERE RouteId IS NOT NULL); -- Bewaar routes met stops

-- Stap 2: Zones opschonen behalve Kortrijk (zone 2)
DELETE FROM Zones WHERE Id NOT IN (2);

-- Stap 3: Bewaar Kortrijk region ID en schoon andere regions op
DECLARE @KortrijkRegionId INT = (SELECT TOP 1 RegionId FROM Zones WHERE Id = 2);
DELETE FROM Regions WHERE Id != @KortrijkRegionId;

-- Stap 4: Update Kortrijk region
UPDATE Regions 
SET Name = 'West-Vlaanderen',
    Country = 'België', 
    CountryCode = 'BE',
    Description = 'West-Vlaanderen provincie',
    Latitude = 50.8476,
    Longitude = 3.2797,
    IsActive = 1,
    CreatedAt = GETUTCDATE()
WHERE Id = @KortrijkRegionId;

-- Stap 5: Update zone 2 naar Kortrijk
UPDATE Zones 
SET Name = 'Kortrijk', 
    CountryCode = 'BE',
    GeoJsonBoundary = '{"type":"Polygon","coordinates":[[[3.25,50.82],[3.30,50.82],[3.30,50.87],[3.25,50.87],[3.25,50.82]]]}',
    RegionId = @KortrijkRegionId
WHERE Id = 2;

-- Stap 6: Nieuwe regions toevoegen
INSERT INTO Regions (Name, Country, CountryCode, Description, Latitude, Longitude, IsActive, CreatedAt) VALUES
('Zeeland', 'Nederland', 'NL', 'Zeeland provincie Nederland', 51.4988, 3.6109, 1, GETUTCDATE()),
('Antwerpen', 'België', 'BE', 'Provincie Antwerpen', 51.2194, 4.4025, 1, GETUTCDATE()),
('Oost-Vlaanderen', 'België', 'BE', 'Oost-Vlaanderen provincie', 51.0259, 3.7174, 1, GETUTCDATE()),
('Limburg-BE', 'België', 'BE', 'Limburg provincie België', 50.9300, 5.3378, 1, GETUTCDATE()),
('West-Vlaanderen-Kust', 'België', 'BE', 'West-Vlaanderen kust', 51.2093, 3.2247, 1, GETUTCDATE()),
('Valencia', 'Spanje', 'ES', 'Comunidad Valenciana', 39.4699, -0.3763, 1, GETUTCDATE()),
('Canarias', 'Spanje', 'ES', 'Islas Canarias', 28.2916, -16.6291, 1, GETUTCDATE()),
('Dodecanese', 'Griekenland', 'GR', 'Dodecanese eilanden', 36.4341, 28.2176, 1, GETUTCDATE()),
('Ionian Islands', 'Griekenland', 'GR', 'Ionische eilanden', 38.9637, 20.6230, 1, GETUTCDATE()),
('Crete', 'Griekenland', 'GR', 'Kreta eiland', 35.2401, 24.8093, 1, GETUTCDATE()),
('Gibraltar', 'Gibraltar', 'GI', 'British Overseas Territory', 36.1408, -5.3536, 1, GETUTCDATE());

-- Stap 7: Zones toevoegen met juiste region IDs
INSERT INTO Zones (Name, CountryCode, GeoJsonBoundary, RegionId) VALUES
-- Nederland zones
('Cadzand', 'NL', '{"type":"Polygon","coordinates":[[[3.37,51.37],[3.42,51.37],[3.42,51.40],[3.37,51.40],[3.37,51.37]]]}', 
 (SELECT Id FROM Regions WHERE Name = 'Zeeland')),

-- België zones
('Kempen', 'BE', '{"type":"Polygon","coordinates":[[[4.95,51.30],[5.05,51.30],[5.05,51.40],[4.95,51.40],[4.95,51.30]]]}', 
 (SELECT Id FROM Regions WHERE Name = 'Antwerpen')),
('Sint-Niklaas', 'BE', '{"type":"Polygon","coordinates":[[[4.13,51.15],[4.18,51.15],[4.18,51.18],[4.13,51.18],[4.13,51.15]]]}', 
 (SELECT Id FROM Regions WHERE Name = 'Oost-Vlaanderen')),
('Genk', 'BE', '{"type":"Polygon","coordinates":[[[5.48,50.95],[5.53,50.95],[5.53,51.00],[5.48,51.00],[5.48,50.95]]]}', 
 (SELECT Id FROM Regions WHERE Name = 'Limburg-BE')),
('Mechelen', 'BE', '{"type":"Polygon","coordinates":[[[4.46,51.01],[4.51,51.01],[4.51,51.06],[4.46,51.06],[4.46,51.01]]]}', 
 (SELECT Id FROM Regions WHERE Name = 'Antwerpen')),
('Blankenberge', 'BE', '{"type":"Polygon","coordinates":[[[3.12,51.30],[3.17,51.30],[3.17,51.33],[3.12,51.33],[3.12,51.30]]]}', 
 (SELECT Id FROM Regions WHERE Name = 'West-Vlaanderen-Kust')),
('Dender en Vlaamse Ardennen', 'BE', '{"type":"Polygon","coordinates":[[[3.85,50.75],[3.95,50.75],[3.95,50.85],[3.85,50.85],[3.85,50.75]]]}', 
 (SELECT Id FROM Regions WHERE Name = 'Oost-Vlaanderen')),

-- Spanje zones
('Torrevieja', 'ES', '{"type":"Polygon","coordinates":[[[−0.70,37.95],[−0.65,37.95],[−0.65,38.00],[−0.70,38.00],[−0.70,37.95]]]}', 
 (SELECT Id FROM Regions WHERE Name = 'Valencia')),
('Orihuela', 'ES', '{"type":"Polygon","coordinates":[[[−0.95,37.95],[−0.90,37.95],[−0.90,38.05],[−0.95,38.05],[−0.95,37.95]]]}', 
 (SELECT Id FROM Regions WHERE Name = 'Valencia')),
('Albir', 'ES', '{"type":"Polygon","coordinates":[[[−0.08,38.58],[−0.03,38.58],[−0.03,38.63],[−0.08,38.63],[−0.08,38.58]]]}', 
 (SELECT Id FROM Regions WHERE Name = 'Valencia')),
('Lanzarote', 'ES', '{"type":"Polygon","coordinates":[[[−13.70,28.90],[−13.40,28.90],[−13.40,29.25],[−13.70,29.25],[−13.70,28.90]]]}', 
 (SELECT Id FROM Regions WHERE Name = 'Canarias')),
('Altea', 'ES', '{"type":"Polygon","coordinates":[[[−0.05,38.58],[0.00,38.58],[0.00,38.63],[−0.05,38.63],[−0.05,38.58]]]}', 
 (SELECT Id FROM Regions WHERE Name = 'Valencia')),
('La Nucia', 'ES', '{"type":"Polygon","coordinates":[[[−0.12,38.60],[−0.07,38.60],[−0.07,38.65],[−0.12,38.65],[−0.12,38.60]]]}', 
 (SELECT Id FROM Regions WHERE Name = 'Valencia')),
('Fuerteventura', 'ES', '{"type":"Polygon","coordinates":[[[−14.30,28.05],[−13.85,28.05],[−13.85,28.75],[−14.30,28.75],[−14.30,28.05]]]}', 
 (SELECT Id FROM Regions WHERE Name = 'Canarias')),
('Tenerife', 'ES', '{"type":"Polygon","coordinates":[[[−16.90,28.05],[−16.10,28.05],[−16.10,28.60],[−16.90,28.60],[−16.90,28.05]]]}', 
 (SELECT Id FROM Regions WHERE Name = 'Canarias')),

-- Griekenland zones
('Rhodos', 'GR', '{"type":"Polygon","coordinates":[[[27.95,36.15],[28.25,36.15],[28.25,36.50],[27.95,36.50],[27.95,36.15]]]}', 
 (SELECT Id FROM Regions WHERE Name = 'Dodecanese')),
('Kos', 'GR', '{"type":"Polygon","coordinates":[[[27.05,36.85],[27.35,36.85],[27.35,36.95],[27.05,36.95],[27.05,36.85]]]}', 
 (SELECT Id FROM Regions WHERE Name = 'Dodecanese')),
('Corfu', 'GR', '{"type":"Polygon","coordinates":[[[19.85,39.35],[19.95,39.35],[19.95,39.80],[19.85,39.80],[19.85,39.35]]]}', 
 (SELECT Id FROM Regions WHERE Name = 'Ionian Islands')),
('Rethymnon', 'GR', '{"type":"Polygon","coordinates":[[[24.45,35.35],[24.55,35.35],[24.55,35.40],[24.45,35.40],[24.45,35.35]]]}', 
 (SELECT Id FROM Regions WHERE Name = 'Crete')),

-- Gibraltar zone
('Gibraltar', 'GI', '{"type":"Polygon","coordinates":[[[−5.36,36.13],[−5.34,36.13],[−5.34,36.15],[−5.36,36.15],[−5.36,36.13]]]}', 
 (SELECT Id FROM Regions WHERE Name = 'Gibraltar'));

-- Stap 8: Controleer resultaat
SELECT 
    r.Name as RegionName,
    r.Country,
    z.Name as ZoneName,
    z.CountryCode,
    COUNT(v.Id) as VehicleCount
FROM Regions r
LEFT JOIN Zones z ON r.Id = z.RegionId
LEFT JOIN Vehicles v ON z.Id = v.ZoneId
GROUP BY r.Name, r.Country, z.Name, z.CountryCode
ORDER BY r.Country, r.Name, z.Name;
