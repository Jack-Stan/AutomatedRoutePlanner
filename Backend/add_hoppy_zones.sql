-- Hoppy Zones Toevoegen (zonder bestaande data te verwijderen)
-- Voeg echte Hoppy locaties toe

-- Eerst nieuwe regions toevoegen
INSERT INTO Regions (Name, Country, CountryCode, Description, Latitude, Longitude, IsActive, CreatedAt) VALUES
('Zeeland', 'Nederland', 'NL', 'Zeeland provincie Nederland', 51.4988, 3.6109, 1, GETUTCDATE()),
('Antwerpen-Prov', 'België', 'BE', 'Provincie Antwerpen', 51.2194, 4.4025, 1, GETUTCDATE()),
('Oost-Vlaanderen-Prov', 'België', 'BE', 'Oost-Vlaanderen provincie', 51.0259, 3.7174, 1, GETUTCDATE()),
('Limburg-BE-Prov', 'België', 'BE', 'Limburg provincie België', 50.9300, 5.3378, 1, GETUTCDATE()),
('West-Vlaanderen-Kust', 'België', 'BE', 'West-Vlaanderen kust', 51.2093, 3.2247, 1, GETUTCDATE()),
('Valencia-Spanje', 'Spanje', 'ES', 'Comunidad Valenciana', 39.4699, -0.3763, 1, GETUTCDATE()),
('Canarias-Spanje', 'Spanje', 'ES', 'Islas Canarias', 28.2916, -16.6291, 1, GETUTCDATE()),
('Dodecanese-GR', 'Griekenland', 'GR', 'Dodecanese eilanden', 36.4341, 28.2176, 1, GETUTCDATE()),
('Ionian-GR', 'Griekenland', 'GR', 'Ionische eilanden', 38.9637, 20.6230, 1, GETUTCDATE()),
('Crete-GR', 'Griekenland', 'GR', 'Kreta eiland', 35.2401, 24.8093, 1, GETUTCDATE()),
('Gibraltar-Territory', 'Gibraltar', 'GI', 'British Overseas Territory', 36.1408, -5.3536, 1, GETUTCDATE());

-- Nu zones toevoegen
INSERT INTO Zones (Name, CountryCode, GeoJsonBoundary, RegionId) VALUES
-- Nederland zones
('Cadzand', 'NL', '{"type":"Polygon","coordinates":[[[3.37,51.37],[3.42,51.37],[3.42,51.40],[3.37,51.40],[3.37,51.37]]]}', 
 (SELECT Id FROM Regions WHERE Name = 'Zeeland')),

-- België zones  
('Kempen', 'BE', '{"type":"Polygon","coordinates":[[[4.95,51.30],[5.05,51.30],[5.05,51.40],[4.95,51.40],[4.95,51.30]]]}', 
 (SELECT Id FROM Regions WHERE Name = 'Antwerpen-Prov')),
('Sint-Niklaas', 'BE', '{"type":"Polygon","coordinates":[[[4.13,51.15],[4.18,51.15],[4.18,51.18],[4.13,51.18],[4.13,51.15]]]}', 
 (SELECT Id FROM Regions WHERE Name = 'Oost-Vlaanderen-Prov')),
('Genk', 'BE', '{"type":"Polygon","coordinates":[[[5.48,50.95],[5.53,50.95],[5.53,51.00],[5.48,51.00],[5.48,50.95]]]}', 
 (SELECT Id FROM Regions WHERE Name = 'Limburg-BE-Prov')),
('Mechelen', 'BE', '{"type":"Polygon","coordinates":[[[4.46,51.01],[4.51,51.01],[4.51,51.06],[4.46,51.06],[4.46,51.01]]]}', 
 (SELECT Id FROM Regions WHERE Name = 'Antwerpen-Prov')),
('Blankenberge', 'BE', '{"type":"Polygon","coordinates":[[[3.12,51.30],[3.17,51.30],[3.17,51.33],[3.12,51.33],[3.12,51.30]]]}', 
 (SELECT Id FROM Regions WHERE Name = 'West-Vlaanderen-Kust')),
('Dender en Vlaamse Ardennen', 'BE', '{"type":"Polygon","coordinates":[[[3.85,50.75],[3.95,50.75],[3.95,50.85],[3.85,50.85],[3.85,50.75]]]}', 
 (SELECT Id FROM Regions WHERE Name = 'Oost-Vlaanderen-Prov')),

-- Spanje zones
('Torrevieja', 'ES', '{"type":"Polygon","coordinates":[[[−0.70,37.95],[−0.65,37.95],[−0.65,38.00],[−0.70,38.00],[−0.70,37.95]]]}', 
 (SELECT Id FROM Regions WHERE Name = 'Valencia-Spanje')),
('Orihuela', 'ES', '{"type":"Polygon","coordinates":[[[−0.95,37.95],[−0.90,37.95],[−0.90,38.05],[−0.95,38.05],[−0.95,37.95]]]}', 
 (SELECT Id FROM Regions WHERE Name = 'Valencia-Spanje')),
('Albir', 'ES', '{"type":"Polygon","coordinates":[[[−0.08,38.58],[−0.03,38.58],[−0.03,38.63],[−0.08,38.63],[−0.08,38.58]]]}', 
 (SELECT Id FROM Regions WHERE Name = 'Valencia-Spanje')),
('Lanzarote', 'ES', '{"type":"Polygon","coordinates":[[[−13.70,28.90],[−13.40,28.90],[−13.40,29.25],[−13.70,29.25],[−13.70,28.90]]]}', 
 (SELECT Id FROM Regions WHERE Name = 'Canarias-Spanje')),
('Altea', 'ES', '{"type":"Polygon","coordinates":[[[−0.05,38.58],[0.00,38.58],[0.00,38.63],[−0.05,38.63],[−0.05,38.58]]]}', 
 (SELECT Id FROM Regions WHERE Name = 'Valencia-Spanje')),
('La Nucia', 'ES', '{"type":"Polygon","coordinates":[[[−0.12,38.60],[−0.07,38.60],[−0.07,38.65],[−0.12,38.65],[−0.12,38.60]]]}', 
 (SELECT Id FROM Regions WHERE Name = 'Valencia-Spanje')),
('Fuerteventura', 'ES', '{"type":"Polygon","coordinates":[[[−14.30,28.05],[−13.85,28.05],[−13.85,28.75],[−14.30,28.75],[−14.30,28.05]]]}', 
 (SELECT Id FROM Regions WHERE Name = 'Canarias-Spanje')),
('Tenerife', 'ES', '{"type":"Polygon","coordinates":[[[−16.90,28.05],[−16.10,28.05],[−16.10,28.60],[−16.90,28.60],[−16.90,28.05]]]}', 
 (SELECT Id FROM Regions WHERE Name = 'Canarias-Spanje')),

-- Griekenland zones
('Rhodos', 'GR', '{"type":"Polygon","coordinates":[[[27.95,36.15],[28.25,36.15],[28.25,36.50],[27.95,36.50],[27.95,36.15]]]}', 
 (SELECT Id FROM Regions WHERE Name = 'Dodecanese-GR')),
('Kos', 'GR', '{"type":"Polygon","coordinates":[[[27.05,36.85],[27.35,36.85],[27.35,36.95],[27.05,36.95],[27.05,36.85]]]}', 
 (SELECT Id FROM Regions WHERE Name = 'Dodecanese-GR')),
('Corfu', 'GR', '{"type":"Polygon","coordinates":[[[19.85,39.35],[19.95,39.35],[19.95,39.80],[19.85,39.80],[19.85,39.35]]]}', 
 (SELECT Id FROM Regions WHERE Name = 'Ionian-GR')),
('Rethymnon', 'GR', '{"type":"Polygon","coordinates":[[[24.45,35.35],[24.55,35.35],[24.55,35.40],[24.45,35.40],[24.45,35.35]]]}', 
 (SELECT Id FROM Regions WHERE Name = 'Crete-GR')),

-- Gibraltar zone
('Gibraltar', 'GI', '{"type":"Polygon","coordinates":[[[−5.36,36.13],[−5.34,36.13],[−5.34,36.15],[−5.36,36.15],[−5.36,36.13]]]}', 
 (SELECT Id FROM Regions WHERE Name = 'Gibraltar-Territory'));

-- Update zone 2 naam naar Kortrijk
UPDATE Zones SET Name = 'Kortrijk' WHERE Id = 2;

-- Toon alle zones gegroepeerd per land
SELECT 
    r.Country,
    z.Name as ZoneName,
    z.CountryCode
FROM Regions r
JOIN Zones z ON r.Id = z.RegionId
WHERE z.Name IS NOT NULL
ORDER BY r.Country, z.Name;
