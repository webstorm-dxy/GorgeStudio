---
name: gorge-charting
description: >
  Domain knowledge for Gorge language charting and Dremu form rhythm game
  authoring. Use when working with .g chart files, creating or editing Gorge
  charts, understanding injector syntax, scene/coordinate system, chart
  structure (Score/Staff/Period hierarchy), or Dremu form mechanics (notes,
  lanes, parameters). Covers Gorge injector format, scene coordinates, chart
  structure conventions, and the Dremu touchscreen rhythm game form.
---

# Gorge Charting

Reference for the Gorge rhythm game framework's charting system and the
Dremu form.

## When to consult references

| Topic | File |
|---|---|
| Injector syntax, `@Inject` annotation, arrays, nesting | [injector-format.md](references/injector-format.md) |
| Camera, z-layering, screen adaptation, coordinate ranges | [scene-coordinates.md](references/scene-coordinates.md) |
| Score/Staff/Period hierarchy, `@ElementStaff`, `@Chart`, `PeriodConfig` | [chart-structure.md](references/chart-structure.md) |
| Dremu note types, lanes, parameters, curve types | [dremu-form.md](references/dremu-form.md) |

## Quick reference: Chart file anatomy

```g
[
    string form = "Dremu",           // Form name
    string displayName = "My Staff"  // Editor display name
]
@ElementStaff                        // Marks as element staff
class MyStaff
{
    [
        GorgeFramework.PeriodConfig^ config = GorgeFramework.PeriodConfig : {
            timeOffset : 0.0,        // Period start time
            minLength : 10.0,        // Minimum duration
        }
    ]
    @Chart                            // Marks as chart entry point
    static GorgeFramework.Element^[] Period()
    {
        return new GorgeFramework.Element^[2]{
            // Elements go here as injector literals
            Dremu.DremuTap : {
                laneName : "Main1",
                hitTime : 1.0,
                // ...
            },
            Dremu.DremuHold : {
                laneName : "Main1",
                hitTime : 2.0,
                holdTime : 0.5,
                // ...
            },
        };
    }
}
```

## Key conventions

- **Object construction order**: super inject → super constructor →
  subclass inject → subclass constructor
- **Default values**: prefer defaults unless the field must be locked at
  chart design time (e.g. note position, hit time)
- **Gameplay elements**: z < 0, within x(-8,8) × y(-5,5)
- **Performance elements**: z > 0, can extend beyond safe rectangle
- **Period time offset** is added to note hitTime for effective timing
- **Chart structure** (Score/Staff/Period) is an editor concept — runtime
  sees only a flat element list
