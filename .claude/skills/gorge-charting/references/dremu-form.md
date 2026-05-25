# Dremu Form

## Overview

Dremu is a touchscreen rhythm game form with non-fixed lanes. It resembles
a "falling-notes" rhythm game where notes move toward a judgment line,
and the player performs actions at judgment points.

## Lanes (Tracks)

### MainLine (判定线)

The primary judgment line. Usually placed at lower screen (e.g. y = -3)
so hands don't block the main display area.

### GuideLine (引导线)

A secondary lane that MUST be bound to a MainLine. Notes on a GuideLine
fall along the guide line's direction rather than perpendicular to the
judgment line. Judgment still uses the bound MainLine.

## Note Types

| Note | Action Required | Start | Hold | Release |
|---|---|---|---|---|
| **Tap (点击)** | Tap in judgment zone | Touch down | No | Not required |
| **Taplik (划动)** | Tap + flick in any direction | Touch down | No | Not required |
| **Drag (拖动)** | Maintain touch in zone | Not required | Yes (brief) | Not required |
| **Hold (长按)** | Touch down + hold | Touch down | Yes (until holdTime) | Not required |

### Note Behavior Details

- **Tap**: Requires touch-down from non-touching state. Release not required.
- **Taplik**: Requires touch-down + quick flick in any direction. Release not required.
- **Drag**: Maintain touch contact within judgment zone. Neither start nor end
  of touch is required. Dense Drag notes constrain finger trajectory, creating
  a natural "dragging" feel.
- **Hold**: Requires touch-down + sustained contact until `holdTime`. Release
  not required.

## Key Parameters

Most parameters have sensible defaults suited to traditional rhythm game
conventions. Parameters recommended for explicit charting:

| Parameter | Description |
|---|---|
| `hitTime` | Judgment time (seconds). Should align with musical rhythm. |
| `holdTime` | Hold duration (Hold notes). Release point should align with rhythm. |
| `position` / `laneName` | Note position / which lane the note belongs to |
| `laneLines` | Lane trajectory (function curve) |
| `leadTime` | Early judgment window (seconds before hitTime) |
| `lagTime` | Late judgment window (seconds after hitTime) |
| `distance` | Visual approach distance curve |
| `color` | Visual color (LerpColorCurve) |
| `drawStartX` / `drawEndX` | Lane visual width animation curves |
| `generateTime` | When the element appears (seconds) |
| `keepTime` | How long the element persists (seconds) |
| `positionY` / `rotationZ` | Visual transform overrides |

**Period time offset is added to hitTime.** The effective hit time is
`period.timeOffset + note.hitTime`.

## Common Curves & Types

- `GorgeFramework.VariableFloat`: `baseValue` + `variationCurve`
- `GorgeFramework.CubicHermiteSpline`: Hermite interpolation with
  `startPoint`, `startTangent`, `startWeight`, `endPoint`, `endTangent`, `endWeight`
- `GorgeFramework.PiecewiseFunctionCurve`: Array of `FunctionPiece`
  segments, each with `functionCurve`, `startX`, `endX`
- `GorgeFramework.LinearCurve`: `timeStart`, `valueStart`, `timeEnd`, `valueEnd`
- `GorgeFramework.LinearFunctionCurve`: `k` (slope), `b` (intercept)
- `GorgeFramework.CompositeFunctionCurve`: `outerFunctionCurve` composed
  with `innerFunctionCurve`
- `GorgeFramework.PeriodicFunctionCurve`: Repeats a curve over `[startX, endX]`
- `GorgeFramework.AxialSymmetricFunctionCurve`: Mirrors around an `axis`
- `GorgeFramework.LerpColorCurve`: Color interpolation with `colorPoints`
  (array of `ColorArgb`) and optional `progressCurve`

## Designing for the Editor

When defining Element classes with `@Inject` fields, use metadata to
make them editor-friendly:

```
[
    auto defaultValue = 0.0,
    string type = "basic",
    int order = 1,
    string displayName = "hit time",
    string information = "unit: seconds, >= 0",
    delegate<bool:float> check = bool:(float hitTime) -> {
        return hitTime >= 0;
    }
]
@Inject
float hitTime = ^hitTime;
```

## Performance vs Gameplay Elements

- Gameplay notes: z < 0, within safe rectangle x(-8,8) × y(-5,5)
- Art/performance elements (like `DremuMainLane` art lines): z > 0,
  can extend beyond safe rectangle for wide screens
