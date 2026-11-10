# API contract: frontend calls vs backend routes

Source of truth for what `Frontend/src/services/api.ts` may call. Every call in that file (52 methods) is listed with the screens that use it, the matching backend route and a verdict. Nothing else in `Frontend/` talks HTTP (`axios` is only imported in `api.ts`).

Base URL `/api`. Every route needs `Authorization: Bearer <jwt>` unless it says **public**. Roles come from the token's `role` claim: `Admin`, `FleetManager`, `BatterySwapper`. "Manager" = Admin or FleetManager. "Own" = a BatterySwapper only on a route assigned to them (403 for someone else's, 404 if it does not exist). "In scope" = a FleetManager only on routes and stops in their own zone or region (section 7); an Admin is always in scope. Out of scope is 403, and an unknown id is 404.

Verdicts:

- **ok**: matches the backend and a screen uses it. Nothing to do.
- **path-mismatch**: the backend has it under another path; the frontend changes, the backend stays.
- **build**: a screen really uses it; the backend endpoint was added (this change).
- **dead**: no screen calls it (the only other callers are the Redux thunks, which no screen dispatches), or it is a leftover. Remove it from the frontend. "backend route kept" means the backend route exists and stays, so removing the frontend call loses nothing.

Screens: Login, ResetPw (ResetPasswordScreen), FirstLogin, Dashboard, Routes, RouteDetails, RouteGen (RouteGenerationScreen), Vehicles, VehicleDetails, UserMgmt (UserManagementScreen), Auth (AuthContext), Redux (`store/slices/*`, never dispatched).

## 1. Calls

### Auth

| api.ts method | HTTP | Screens | Backend | Verdict |
|---|---|---|---|---|
| login | POST /auth/login | Auth | POST /auth/login, **public** | ok |
| forgotPassword | POST /auth/forgot-password | Login | same, **public** | ok |
| resetPassword | POST /auth/reset-password | ResetPw | same, **public** | ok |
| changePassword | POST /auth/users/change-password | Auth (FirstLogin) | same, any role, own account; wrong current password is 400 | ok |
| getCurrentUser | GET /auth/users/me | none | same, any role | dead (backend route kept) |
| getUsers | GET /auth/users | UserMgmt | same, Admin | ok |
| getSwappers | GET /auth/users?role=2[&zoneId=] | RouteGen | same, **Admin, or FleetManager when role=2** | ok (backend widened for FleetManager, see 4) |
| createUser | POST /auth/users | UserMgmt | same, Admin; role Admin is 403 | ok |
| updateUser | PUT /auth/users/{id} | UserMgmt | same, Admin | ok |
| deleteUser | DELETE /auth/users/{id}/permanent | UserMgmt | same, Admin | ok |
| resetUserPassword | POST /auth/users/{id}/reset-password | UserMgmt | same, Admin | ok |
| getActiveUsersCount | GET /auth/users/count | Dashboard (Admin branch) | same, Admin | ok |

### Routes

| api.ts method | HTTP | Screens | Backend | Verdict |
|---|---|---|---|---|
| generateRoute | POST /routes/suggest | RouteGen | same, Admin or FleetManager with the body's `zoneId` in scope | ok |
| suggestRoute | POST /routes/suggest | none (duplicate of generateRoute) | same | dead (backend route kept) |
| getRouteSuggestions | GET /routes/suggestions?zoneId= | Dashboard, Routes | same, any role (swapper: own routes only; FleetManager: a zone out of scope is an **empty list**, not a 403) | ok |
| getAllRoutes | GET /routes/all | Routes (Manager branch) | same, Admin: all routes; FleetManager: only routes in scope (empty list when unassigned) | ok |
| getRoute | GET /routes/{id} | RouteDetails, Redux | same, Admin; FleetManager in scope; swapper own | ok |
| confirmRoute | POST /routes/{id}/confirm | Routes | same, Admin; FleetManager in scope; swapper own | ok |
| getTodaysRoute | GET /routes/today?swapperId= | none | same, Admin; FleetManager when that route is in scope; swapper own id | dead (backend route kept) |
| approveRoute | POST /routes/{id}/approve | none | same, Admin or FleetManager in scope; body is `{notes?}` only, approver = token user | dead (backend route kept) |
| rejectRoute | POST /routes/{id}/reject | none | same, same rules | dead (backend route kept) |
| getRoutesPendingApproval | GET /routes/pending-approval[?zoneId=] | none | same, Admin: all; FleetManager: only routes in scope | dead (backend route kept) |
| getRoutes | GET /routes | Redux | none | dead |
| createRoute | POST /routes | Redux | none | dead |
| updateRoute | PUT /routes/{id} | Redux | none | dead |
| deleteRoute | DELETE /routes/{id} | Redux | none | dead |
| optimizeRoute | POST /routes/{id}/optimize | Redux | none | dead |

### Route stops

| api.ts method | HTTP (old) | Screens | Backend (new) | Verdict |
|---|---|---|---|---|
| completeRouteStop | POST /routestops/{routeId}/stops/{stopId}/complete | RouteDetails | **POST /routestops/{stopId}/complete**, any role: Admin; FleetManager in scope; swapper own | path-mismatch (frontend: done) |
| updateRouteStopStatus | PUT /routestops/{routeId}/stops/{stopId}/status | RouteDetails ("Probleem melden") | **PUT /routestops/{stopId}/status**, any role: Admin; FleetManager in scope; swapper own. Body `{ status, reason? }` | build (frontend: done) |
| updateRouteStopInstructions | PUT /routes/{routeId}/stops/{stopId}/instructions | RouteGen (edit modal) | **PUT /routestops/{stopId}/instructions**, Admin or FleetManager in scope | build (frontend: done) |

### Vehicles

| api.ts method | HTTP | Screens | Backend | Verdict |
|---|---|---|---|---|
| getAllVehicles | GET /vehicles/all | Vehicles | same, any role | ok |
| getAllLowBatteryVehicles | GET /vehicles/lowbattery/all?batteryThreshold= | Vehicles | same, any role | ok |
| getVehiclesByZone | GET /vehicles/zone/{zoneId} | Dashboard, VehicleDetails | same, any role | ok |
| getLowBatteryVehicles | GET /vehicles/lowbattery?zoneId=&batteryThreshold= | none | same | dead (backend route kept) |
| getVehicleStats | GET /vehicles/stats/zone/{zoneId} | none | same | dead (backend route kept) |
| getVehicle | GET /vehicles/{id} | Redux | same | dead (backend route kept) |
| getVehicles | GET /vehicles | Redux | none (the list is `/vehicles/all`) | dead |
| createVehicle | POST /vehicles | Redux | none | dead |
| updateVehicle | PUT /vehicles/{id} | Redux | none | dead |
| deleteVehicle | DELETE /vehicles/{id} | Redux | none | dead |

### Zones and regions

| api.ts method | HTTP | Screens | Backend | Verdict |
|---|---|---|---|---|
| getZones | GET /zones | UserMgmt, Redux | same, any role | ok |
| getZonesCount | GET /zones/count | Dashboard | same, any role | ok |
| getCountries | GET /regions/countries | RouteGen | same, any role | ok |
| getRegionsByCountry | GET /regions/country/{code} | RouteGen | same, any role | ok |
| getRegions | GET /regions | UserMgmt | same, any role | ok |
| getZone | GET /zones/{id} | Redux | same | dead (backend route kept) |
| getZonesByRegion | GET /regions/{id}/zones | none | same | dead (backend route kept) |
| getZonesByCountry | GET /zones/country/{code} | none | none | dead |
| createZone | POST /zones | Redux | none | dead |
| updateZone | PUT /zones/{id} | Redux | none | dead |
| deleteZone | DELETE /zones/{id} | Redux | none | dead |

### Other

| api.ts method | HTTP | Screens | Backend | Verdict |
|---|---|---|---|---|
| healthCheck | GET /health | none | none | dead |

### Tally

52 calls: **ok 24** (one of them, getSwappers, needed a backend role change), **path-mismatch 1**, **build 2**, **dead 25** (14 with no backend route, 11 whose backend route stays).

No admin screen has vehicle, zone or route create/edit/delete buttons, so none of that CRUD was built. The only callers were Redux thunks that nothing dispatches (`useAppDispatch` and `useAppSelector` are never used outside `store/`), which makes the whole store dead.

Backend routes with no frontend caller (kept): `GET /vehicles/available/zone/{id}`, `GET /vehicles/needing-battery-replacement/zone/{id}`, `GET /regions/{id}`, `GET /auth/users/{id}`, `DELETE /auth/users/{id}` (deactivate), `POST /auth/validate-token` (**public**), plus the dead-with-backend rows above.

## 2. The new endpoints

Both are keyed by the **stop id** only (the route is derived server-side), like the existing `complete`.

### PUT /api/routestops/{id}/status

Body `{ "status": <number>, "reason"?: "<string, max 500>" }`. Returns the stop (`RouteStopDto`, status numeric, with `skipReason`).

- Roles: Admin (any stop); FleetManager (stops in their own zone or region); BatterySwapper (stops on their own route only).
- Accepted values: **2 (InProgress)** and **4 (Skipped)**. Starting a stop stamps `actualArrivalTime`. Completing is not accepted here (400): use `POST /routestops/{id}/complete`, which stamps the times.
- **`reason`** is stored as `skipReason`, trimmed, and only together with Skipped. An empty or whitespace reason stores nothing. A non-empty reason with any other status is a **400** (rather than being silently dropped), and so is a reason over 500 characters. `skipReason` comes back on the stop here and on every stop inside route DTOs.
- 400 for any other value, 409 when the stop is already Completed or Skipped, 404 unknown stop, 403 out of scope or someone else's route, 401 no token.

### PUT /api/routestops/{id}/instructions

Body `{ "notes": "<string, max 500>" }`. Replaces the stop's note; `""` or `null` clears it. Returns the stop (`RouteStopDto`, with `notes`).

- Role: Admin, or FleetManager for a stop in their own zone or region (403 for a swapper, even on their own stop, and for a FleetManager out of scope).
- 400 over 500 characters, 404 unknown stop, 401 no token.
- Swappers read it as `notes` on each stop of `GET /routes/{id}` (the route mapper now includes it).
- One field, not two: the database has a single `RouteStop.Notes` column (500 chars). The old `specialInstructions` and `batterySwapNotes` do not exist on the backend. Two fields would need two new columns plus a startup step like the one for `SkipReason` (section 8). That is a follow-up if wanted.

### Also changed

- `POST /routes/{id}/approve` and `/reject`: the approver is the token user. The body is `{ "notes"?: string }`, optional, and a sent `approvedBy` is ignored.
- `GET /auth/users`: a FleetManager may call it with `role=2` (swappers) only; `role=0`, `role=1` or no role is 403. Admin unchanged.

## 3. Enums on the wire

All enums are serialised as **numbers**. Current backend values:

| Enum | Values |
|---|---|
| RouteStopStatus | Pending 1, InProgress 2, Completed 3, Skipped 4 |
| RouteStatus | Suggested 0, Confirmed 1, InProgress 2, Completed 3, Cancelled 4 |
| RouteApprovalStatus | PendingApproval 0, Approved 1, Rejected 2, RequiresChanges 3 |
| UserRole | Admin 0, FleetManager 1, BatterySwapper 2 |

The frontend disagrees in four places (frontend work list, section 5).

## 4. Role changes made in this contract work

- Stop status and instructions endpoints (new), rules above.
- Zone scoping for FleetManagers (section 7) on the route and stop endpoints above.
- `GET /auth/users?role=2` opened to FleetManager. Before that, RouteGenerationScreen (Admin and FleetManager) got a 403 for a FleetManager, and `api.ts` then cleared the stored session on a 403 (fixed since, see section 5 item 7). This is in its own commit so it can be reverted on its own.

## 5. Frontend work list (AOIFE)

Status: **all six items done** on branch `fix/frontend-contract`. Verified with `npx tsc --noEmit` (no new errors; the 2 existing `react-native-vector-icons` type errors in `DashboardScreen.tsx` remain) and grep. Not yet run on a device, see the last paragraph of this section.

1. **Done.** `completeRouteStop`: `POST /routestops/${stopId}/complete`. Signature `(stopId)`; RouteDetailsScreen passes `stop.id`. No body. Response is `{ message: string }`, not a `RouteStopDto` (the screen ignores it; type it as `Promise<void>`).
2. **Done.** `updateRouteStopStatus`: `PUT /routestops/${stopId}/status`, body `{ status }`, response `RouteStopDto`. RouteDetailsScreen sends Skipped, which must be **4**.
3. **Done.** `updateRouteStopInstructions`: `PUT /routestops/${stopId}/instructions`, body `{ notes }`, response `RouteStopDto`. Signature `(stopId, notes)`. In RouteGenerationScreen merge the two modal inputs into one note (max 500 chars), show `stop.notes` instead of `specialInstructions` and `batterySwapNotes`, and drop those two fields from the `RouteStopDto` interface. Add `notes?: string` there. Showing `stop.notes` on RouteDetailsScreen is how the swapper sees it.
4. **Done.** Fix the enums to the table in section 3 (now one copy each in `Frontend/src/types/index.ts`, imported by the screens; `RouteApprovalStatus` lives there too and nothing in the frontend branches on it yet):
   - `RouteStopStatus` (in `RouteDetailsScreen.tsx` and `types/index.ts`): 1 to 4, **remove `Failed`**. Today `Pending = 0` never matches the backend's 1, so the swapper's action buttons never render.
   - `RouteStatus` in `RouteDetailsScreen.tsx` and `types/index.ts`: 0 to 4 with `Suggested` and `Confirmed` (RoutesScreen is already right).
   - `RouteApprovalStatus` in `RouteGenerationScreen.tsx`: 3 is `RequiresChanges`, not `AutoApproved`.
5. **Done.** Delete the 25 dead methods from `api.ts`: suggestRoute, getTodaysRoute, getRoutes, createRoute, updateRoute, deleteRoute, optimizeRoute, approveRoute, rejectRoute, getRoutesPendingApproval, getLowBatteryVehicles, getVehicleStats, getVehicle, getVehicles, createVehicle, updateVehicle, deleteVehicle, getZonesByCountry, getZone, getZonesByRegion, createZone, updateZone, deleteZone, healthCheck, getCurrentUser. Then the Redux slices that call them (`routesSlice`, `vehiclesSlice`, `zonesSlice`) have no callers left; the store, `store/hooks.ts`, the `Provider` in `App.tsx` and the `@reduxjs/toolkit` and `react-redux` dependencies are removed as well (nothing dispatched or selected).
6. **Done.** `RouteDto` interface: the backend field is `assignedSwapperId`, not `swapperId` (no screen reads it; `swapperName` still works through a compatibility property).
7. **Done.** A **403 no longer logs the user out.** The response interceptor in `api.ts` clears the stored token and user and calls the logout callback on a **401 only** (`AuthContext` registers that callback, so the navigator goes to login). A 403 leaves the session alone and rejects with the backend's `message`, or "Je hebt geen toegang tot deze actie" when it sent none. Screens read it through `getApiErrorMessage` in `Frontend/src/utils/apiError.ts` and show it in their existing Alert or error state: RouteDetails (load, complete, skip), Routes (load, confirm) and RouteGeneration (countries, swappers, zones, generate, save note). There is no approve or reject UI in the frontend.

Still needs a real device or emulator (no Expo here): the swapper's "Swap Voltooid" and "Probleem Melden" buttons now rendering on Pending (1) stops, complete and skip round trips, the merged notes modal (500 character limit, counter) and `notes` showing on RouteDetails and in the RouteGen preview, and a fresh `npm install` without the Redux packages. For item 7: a FleetManager opening a route outside their zone should see the message and stay logged in; an expired token should land on the login screen.

## 6. Done since (leftovers)

- Stop vehicles on every route DTO now carry `registrationNumber`, `vehicleType`, `zoneName`, `needsBatteryReplacement` and `isAvailable` (one shared `VehicleDtoMapper`; the zone name comes from an `Include`, in the same query).
- The skip comment is stored (`skipReason`, section 2), sent by RouteDetailsScreen as `reason`, and shown on the stop.
- FleetManager zone scoping (section 7).

## 7. Zone scoping for FleetManagers

A FleetManager's scope comes from their user record, read from the database on every request (not from the token, so an admin changing an assignment or deactivating the account applies at once):

1. `assignedZoneId` set: that zone only. The narrowest assignment wins, so if a region is also set it is ignored.
2. else `assignedRegionId` set: every zone of that region.
3. else **no scope**: an unassigned FleetManager manages nothing, lists nothing and gets 403 on every action. An admin must assign a zone or region first.

Admins are global. Inactive, unknown and non-FleetManager users have no scope.

Enforced (403 out of scope, 404 unknown id): `POST /routes/suggest` (body `zoneId`), `POST /routes/{id}/approve|reject|confirm`, `POST /routestops/{id}/complete`, `PUT /routestops/{id}/status|instructions`, `GET /routes/{id}`, `GET /routes/today`.

Lists (empty 200, never a 403): `GET /routes/all`, `GET /routes/pending-approval` (a `zoneId` filter outside the scope is also empty) and `GET /routes/suggestions?zoneId=` for a zone outside the scope. The empty list matters because `DashboardScreen` and `RoutesScreen` call `suggestions` with a hard-coded zone (2). A 403 no longer logs the user out (section 5 item 7), but an empty list is still friendlier than an error on those screens.

Not scoped: `GET /auth/users?role=2` (the swapper list) and the vehicle, zone and region reads, because swappers are not required to have an assignment and a scoped list would leave a FleetManager unable to assign anyone.

## 8. Schema change: `RouteStops.SkipReason`

The database is created with `EnsureCreated`, which never alters an existing database, and EF selects every mapped column, so a missing column would break every route and stop query on an existing database. `SchemaUpgrades.Apply` therefore runs at startup right after `EnsureCreated` and executes, on SQL Server only:

```sql
IF OBJECT_ID(N'[dbo].[RouteStops]', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.RouteStops', N'SkipReason') IS NULL
    ALTER TABLE [dbo].[RouteStops] ADD [SkipReason] nvarchar(500) NULL
```

It is idempotent (a fresh database already has the column) and needs `ALTER` permission on `RouteStops` for the app's database user. On an existing database `EnsureCreated` does nothing, so that user may not have it today. If the permission is withheld, the step fails: the error is logged by the existing "Database initialization failed" handler, the app keeps starting (as for any other startup database error), and route and stop queries then fail with "Invalid column name 'SkipReason'". In that case run the statement above once as a DBA.

## 9. Not done, listed for Stan

- **Unassigned FleetManagers are locked out** of route management until an admin gives them a zone or region (section 7). Existing FleetManagers created without an assignment are affected.
- ~~`api.ts` clears the stored session on any 403.~~ Done, see section 5 item 7: only a 401 clears the session now.
- The swapper list `GET /auth/users?role=2` is not zone-scoped (see section 7).
- Zone and region assignment is not validated against each other: a FleetManager can have a zone outside their region; the zone wins.
- VehiclesScreen and VehicleDetailsScreen have a "Swap voltooid" button that only shows an alert (`// In a real app, this would update the vehicle's battery level`). There is no API call, so no endpoint was built. Whether a swap outside a route should exist, and who may do it, is a design question.
- RouteGenerationScreen "Route doorsturen" and RoutesScreen "Route genereren" are stubs with no API call. The backend already creates generated routes as PendingApproval.
