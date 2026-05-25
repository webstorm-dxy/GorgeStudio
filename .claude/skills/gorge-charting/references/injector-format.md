# Gorge Injector Format

## Overview

Injectors are compile-time constant data structures in Gorge for storing
object initialization data. Object construction in Gorge involves two phases:
injector injection and constructor invocation.

## Construction Order

```
superclass injection -> superclass constructor -> subclass injection ->
subclass constructor -> ...
```

- Subclass injectors contain all superclass injector data, allowing implicit
  conversion from subclass injector to superclass injector.
- Gorge allows calling a constructor directly on an injector object rather
  than a class name — this invokes the constructor of the injector's
  declared type (not a superclass), which must be an "injector constructor".

## Design Purpose

Injectors store static object information. Gorge (a rhythm game framework)
typically uses injectors to store note/element data in charts. A standard
Gorge chart is an array of element injectors.

Injectors do NOT influence constructor selection — only call parameters
determine which constructor is used. Thus injectors are ideal for semantic
data, while constructor parameters convey "dynamic conditions" (e.g.
normal play vs. auto-play).

## Compile-Time Nature

- All injector field values must be compile-time constants (or defaults).
- Compiler constructs injector objects at compile time, stores them as
  immediates in intermediate code.
- Enables serialization and moves online cost to offline precompute.

## Syntax

### Type Injector

Resembles JSON but starts with the class name:

```
ClassName : {
    field1 : value1,
    field2 : value2,
}
```

Example:
```
GorgeFramework.Vector2 : {
    x : 1.052632,
    y : -1.6,
}
```

### Nested Injectors

```
GorgeFramework.CubicHermiteSpline : {
    startPoint : GorgeFramework.Vector2 : {
        x : 0.0,
        y : 1.6,
    },
    startTangent : -80.0,
    startWeight : 0.035,
    endPoint : GorgeFramework.Vector2 : {
        x : 1.052632,
        y : -1.6,
    },
    endTangent : 0.0,
    endWeight : 0.0,
}
```

### Array Injector

Type name suffixed with `^` denotes the injector type for that class.
Array injectors use curly braces (NOT square brackets like JSON):

```
Dremu.DremuHoldInnerNote^ : {
    Dremu.DremuHoldInnerNote : {
        hitTime : 0.3157895,
    },
    Dremu.DremuHoldInnerNote : {
        hitTime : 0.5263158,
    },
}
```

`Dremu.DremuHoldInnerNote^` means the injector type of `DremuHoldInnerNote`.
The above is an injector of an injector-array (`DremuHoldInnerNote^[]^`).

### Empty Injectors

- Empty type injector: `{ : }` — all fields use defaults
- Empty array injector: `{ , }` — zero-length array

```
GorgeFramework.Vector2 : { : }   // type injector, all defaults
GorgeFramework.Vector2 : { , }   // array injector, length 0
```

### Optional Fields

Type injectors don't need to fill every field — missing fields get defaults
defined by the class implementation. If no default exists, the field is
required in the injector literal (error at construction time otherwise).

Prefer defaults unless the field must be locked at chart design time
(e.g. note position, hit time).

## Injector Fields vs Object Fields

Prefix field names with `^` to reference injector fields:

```
injectorA.^injectorFieldB
```

In field initialization code within a class:
```
@Inject
float hitTime = ^hitTime;
```

This copies the injector field value to the object field.

## The `@Inject` Annotation

Defines an injector field on a class with optional metadata:

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

Metadata recognized by the Gorge chart editor:
- `defaultValue` — default value for the injector field
- `type` — category for UI grouping
- `order` — display order in editor
- `displayName` — human-readable label
- `information` — tooltip / help text
- `check` — validation delegate

If the injector field type differs from the object field, use angle brackets
on `@Inject`: `@Inject<float>`
