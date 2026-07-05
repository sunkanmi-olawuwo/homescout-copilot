# Area Evidence Map

This page defines the implementation direction for HomeScout's area evidence map: a 2D, evidence-backed view of nearby places that helps buyers and renters understand daily-life fit around a property or postcode.

The map is not a property portal, a Google Maps clone, or a simplistic area score. It is a decision-support surface that makes nearby schools, transport, shops, parks, and services visible with distance, source, freshness, and caveats.

## Product Goal

When a user adds a listing or postcode, HomeScout should answer practical questions like:

- What useful places are nearby?
- How far are the nearest supermarket, school, station, bus stop, park, pharmacy, or GP?
- Which homes are better for my commute, school needs, or lifestyle?
- What area facts are missing or uncertain?

This supports both buying and renting. The same map can sit inside a buyer decision pack or renter decision pack.

## MVP Shape

Start with a restrained 2D "decision map".

- One property or postcode centre pin.
- Nearby places grouped by category.
- Named places where the source provides names.
- Distance labels such as `0.2 mi` and `4 min walk estimate`.
- Filter controls for Schools, Transport, Shops, Parks, Health, and Other.
- A companion list ranked by distance.
- Source and provenance badges on both map pins and list rows.
- A clear caveat when distance is straight-line rather than real walking route.

The first version should prefer clarity over spectacle. The map should help a user compare areas quickly, not dominate the whole workspace.

## Why 2D First

2D is the right first slice because it is cheaper, faster, easier to read, easier to make accessible, and easier to test.

3D can be explored later as a premium/demo mode with tilted buildings and floating labels, but it adds complexity: heavier rendering, harder label placement, more device variation, more data work, and harder accessibility. It should not block the MVP.

## User Experience

The map appears as a tool/detail surface inside the comparison workspace and evidence panel.

Primary states:

- Empty: no postcode or listing location yet.
- Loading: area lookup in progress.
- Ready: map plus ranked nearby-place list.
- Partial: some categories unavailable; visible missing-data state.
- Degraded: source failed and cached/fallback data is being shown.
- Unsupported: postcode cannot be geocoded or sits outside the supported data area.

The map should never label an area as safe or unsafe. Crime, school, and commute context must stay descriptive and source-linked.

## Data Categories

MVP categories:

| Category | Examples | First Source Direction |
| --- | --- | --- |
| Shops | supermarkets, convenience stores, retail clusters | OpenStreetMap / Overpass |
| Transport | railway stations, tube/DLR/tram stops, bus stops | NaPTAN, TfL for London, OpenStreetMap fallback |
| Schools | primary, secondary, nursery where available | GOV.UK school datasets |
| Parks | parks, playgrounds, open spaces | OpenStreetMap / Overpass |
| Health | pharmacies, GP surgeries, dentists | OpenStreetMap first; official sources later where needed |
| Other | libraries, gyms, places of worship, cafes | OpenStreetMap / Overpass |

Each place should carry a source name, source URL or reference, last checked timestamp, and provenance: `Live`, `Cache`, or `Fallback`.

## API Contract

The frontend calls the HomeScout API. It must not call Overpass, TfL, school datasets, geocoders, or routing providers directly.

Planned endpoint:

```http
GET /api/area/nearby?postcode=SE10%209NF&radiusMetres=1200
```

Alternative input once listings have structured location facts:

```http
GET /api/area/nearby?latitude=51.4826&longitude=-0.0077&radiusMetres=1200
```

Response shape:

```json
{
  "centre": {
    "label": "SE10 9NF",
    "latitude": 51.4826,
    "longitude": -0.0077
  },
  "radiusMetres": 1200,
  "distanceMode": "StraightLine",
  "places": [
    {
      "id": "osm:node:123",
      "name": "Example Supermarket",
      "category": "Shops",
      "subcategory": "supermarket",
      "latitude": 51.4812,
      "longitude": -0.0091,
      "distanceMetres": 210,
      "estimatedWalkMinutes": 3,
      "source": "OpenStreetMap",
      "sourceReference": "osm:node:123",
      "provenance": "Live",
      "lastCheckedUtc": "2026-07-05T10:00:00Z"
    }
  ],
  "assumptions": [
    "Distances are straight-line estimates until walking-route support is enabled.",
    "OpenStreetMap coverage varies by area."
  ],
  "missing": [
    {
      "category": "Schools",
      "reason": "Official school dataset lookup is not configured yet."
    }
  ]
}
```

Use shared DTO names consistently when implementation starts:

- `AreaNearbyRequest`
- `AreaNearbyResponse`
- `AreaCentre`
- `NearbyPlace`
- `NearbyPlaceCategory`
- `AreaMissingData`
- `DistanceMode`
- `DataProvenance`

Expected domain failures should use FluentResults in `.API.Service` and map to ProblemDetails at the API boundary.

## Backend Implementation Direction

Recommended service shape:

- `IAreaEvidenceService` orchestrates geocoding, nearby-place lookup, distance calculation, and response assembly.
- `IPostcodeGeocoder` resolves postcode to latitude/longitude.
- `INearbyPlaceProvider` returns source-specific places.
- `IDistanceEstimator` calculates straight-line distance first.
- Source adapters sit behind provider interfaces and include cache-aware provenance.

First implementation can use:

- Postcode geocoding from an approved postcode source.
- OpenStreetMap / Overpass for shops, parks, and general amenities.
- NaPTAN or TfL for London transport stops.
- Official school datasets once the source and licence path is confirmed.

Cache external results to reduce rate-limit pressure and keep the app usable during source outages. Production should log fallback paths and expose provenance so users know whether data is live or cached.

## Frontend Implementation Direction

Use MapLibre GL JS for the map surface when the feature is built.

Frontend components:

- `AreaEvidenceMap`
- `AreaMapPin`
- `AreaCategoryFilter`
- `NearbyPlacesList`
- `NearbyPlaceRow`
- `AreaSourceBadge`
- `AreaMissingDataNotice`

The map view should:

- Render category-specific pins with accessible labels.
- Keep filters as compact controls, not explanatory text blocks.
- Keep the ranked list usable without the map for accessibility and mobile.
- Show loading, partial, degraded, and missing-data states.
- Avoid decorative map styling that makes evidence harder to read.

Mobile should prioritise the list first, with the map available above or behind a tab depending on available space.

## Distance And Routing

MVP:

- Use straight-line distance.
- Estimate walk time using a conservative walking-speed assumption.
- Label this clearly as an approximation.

Later:

- Add walking-route distance and travel time with OSRM, Valhalla, GraphHopper, Mapbox Directions, or TfL where appropriate.
- Preserve the `distanceMode` field so old clients can tell whether values are straight-line or route-based.

## Safety And Trust Rules

- Do not rank an area as safe or unsafe.
- Do not imply school quality from distance alone.
- Do not hide missing data.
- Do not present OpenStreetMap coverage as complete.
- Always show source and freshness for area facts.
- Keep area evidence separate from mortgage or tenancy advice.

## Testing

Backend tests:

- Unit tests for distance calculation and category mapping.
- Contract tests for `GET /api/area/nearby`.
- Offline fake provider tests for partial and degraded responses.
- External `[Category("External")]` tests for real provider adapters once implemented.

Frontend tests:

- Component tests for category filters, list rendering, source badges, and missing-data states.
- Accessibility checks for map/list labels and keyboard navigation.
- E2E smoke test for a sample postcode once the endpoint exists.

Quality gate remains `scripts/quality-gate.sh` before a PR is considered ready.

## Acceptance Criteria For The First Slice

- A user can enter or open a listing/postcode and see a 2D map plus nearby-place list.
- Places are grouped by category and show names where available.
- Each visible fact has distance, source, freshness, and provenance.
- Missing categories are shown calmly.
- Straight-line distance is labelled as approximate.
- The frontend only calls the HomeScout API.
- No safe/unsafe area verdicts are shown.

## Review notes (reconcile at implementation)

Lead review, 2026-07-05 — the direction is sound and consistent with HomeScout's standards
(API-first, FluentResults → ProblemDetails, provenance + source + freshness, no safe/unsafe
verdicts, seam-first providers with offline fakes + `[Category("External")]` live tests,
cache-with-fallback-logging, accessibility). A few things to reconcile when work starts:

- **Reuse the existing provenance type.** The codebase already models provenance as `Live`/`Cache`/
  `Fallback` (`EvidenceItem.Provenance` on the backend, `Provenance` in the frontend). Use that same
  type rather than introducing a parallel `DataProvenance`, so the evidence panel stays uniform.
- **MapLibre needs a tile/style source, and OSM requires attribution.** MapLibre GL JS renders vector
  tiles from a style URL — pick the tile/style provider (self-hosted or a free tier) as part of the
  slice. OpenStreetMap data/tiles are ODbL: show **"© OpenStreetMap contributors"** on the map (the
  per-place source badge is not sufficient on its own). Confirm the licence path for the school
  datasets before shipping them.
- **Promote to an indexed plan when built.** This is a design-direction reference in `wiki/`. When
  implementation starts, add owning plan file(s) under `wiki/__plans/` (backend + frontend) and index
  them in `wiki/__plans/README.md`, so `scripts/check-plan-drift.sh` tracks the endpoint/DTO names
  (`AreaNearbyRequest`, `/api/area/nearby`, etc.) against the code.
- Depends on the **Listing-model spine** for the lat/long input path (see the backend backlog in
  [[Log]]); the postcode path can land independently.

## Future Extensions

- Walking-route distance and commute-aware weighting.
- Comparison mode across two or three listings.
- Preference-aware highlights, such as "nearer to primary schools" or "better for rail commute".
- Shareable area-evidence section in the decision pack.
- Optional 3D visualisation for demos after the 2D map is proven.

See [[Feature Coverage]], [[Endpoint Summary]], [[RAG Architecture]], and [[Frontend Design Guidelines]].
