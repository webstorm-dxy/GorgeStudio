# Gorge Chart Structure

## Core Concepts

- **Gorge Framework**: Rhythm game development framework using the Gorge
  language for gameplay logic and chart authoring.
- **Form (模态)**: A complete gameplay definition — a set of Element and
  Note class definitions. Analogous to a "game mode".
- **Element**: A gameplay entity. Notes are a subclass of Element with
  additional input-handling capability.
- **Note**: An Element subclass with a Time-Stack Input-Graph Automaton
  (TSIGA) for recognizing and responding to player input signals.
- **Chart (谱面)**: A playable level composed of elements.
- **Simulation (仿真)**: Gorge's term for playing back a chart.

## Chart Hierarchy

```
Score (总谱)
 └─ Staff (谱表) × N
     └─ Period (乐段) × N
         └─ Note/Element × N
```

### Score (总谱)

One playable chart = one Score. The editor edits one Score at a time
(analogous to a .psd file in Photoshop).

### Staff (谱表)

Each Staff carries exactly one Form. By assigning different Forms to
Staffs within a Score, you achieve **Polymorphous Charting (多模态混合制谱)**.

Two staff types:
- **Element Staff** (`@ElementStaff`): Carries Elements of one Form
- **Audio Staff**: Carries timed audio playback only

### Period (乐段)

A Period has a **time offset**. Elements within the Period typically
use this offset to adjust their actual timeline. Before playback, Gorge
calls element-defined methods to adjust injector field values based on
Period parameters.

This lets element groups be copied and placed at different time points
without modifying individual element time parameters.

### Audio Period

Only within Audio Staffs. Has a time offset but carries audio instead
of elements. Audio plays when the timeline reaches the offset position.

## How Charts Are Compiled

Gorge framework searches compiled code for static classes marked `@Chart`
that return `Element^[]` (injector array). It then simulates all elements.

The Score/Staff/Period structure is a **Gorge Editor concept** — the
runtime only sees a flat list of elements. Chart structure design should
focus on the charter's editing experience: maintainability, reusability,
extensibility.

## Staff Organization Patterns

- Split one Form across multiple Staffs (e.g. lanes in one, notes in another,
  performance elements in a third)
- Structure Periods by musical sections: intro, verse, chorus, bridge, outro
- Isolate reusable note/element patterns into dedicated Periods for copying

## Annotations

- `@ElementStaff`: Marks a class as an element staff
- `@Chart`: Marks a static method that returns `Element^[]` as a chart period
- `@Inject`: Defines an injector field on a class
- `GorgeFramework.PeriodConfig^ config`: Period-level configuration
  (timeOffset, minLength, etc.)
