# Gorge Scene & Coordinate System

## Current State

Gorge currently targets 2D rhythm games. Camera and coordinate system are
fixed (no form-accessible camera control API yet). 3D and camera control
are roadmap goals — these conventions may evolve.

## Camera

- Orthographic projection, shooting along z-axis (negative → positive)
- Center of view at xy(0, 0)
- Render range: z-axis (-10, 10)

## Screen Adaptation

"Fit-inside" mode ensures the rectangle x(-8, 8) × y(-5, 5) is fully
visible on any aspect ratio, centered on screen.

Portions beyond this range are still rendered and can receive input
(touch, etc.).

## Z-Axis Layering Convention

| z range | Purpose |
|---|---|
| z < 0 | Gameplay elements (interactive notes, guiding visual effects) |
| z = 0 | Black mask overlay (player-adjustable opacity) for focusing during high-score attempts |
| z > 0 | Artistic/performance elements (non-gameplay visuals) |

## Placement Guidelines

- **Gameplay elements**: within x(-8,8) × y(-5,5), z < 0
- **Performance elements**: can extend beyond the safe rectangle to fill
  ultrawide screens and avoid "black bars", z > 0
- Some judgment about target device aspect ratios is expected
