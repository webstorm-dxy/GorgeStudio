# Native/Framework/GorgeEditor 目录文档

## 概述
`Native/Framework/GorgeEditor` 目录包含了 Gorge Chart Studio 编辑器相关的组件，这些组件用于在编辑器中可视化显示元素信息。

## 类和文件详解

### ElementLine.g
元素线类，用于在编辑器中表示元素的时间线。

#### 字段
- `color`: 线的颜色，类型为 ColorArgb
- `points`: 线的各点，类型为 [ElementLinePoint](#elementlinepointg)[]

#### 构造函数
- `ElementLine(ColorArgb color, ElementLinePoint[] points)`: 使用指定颜色和点数组创建一个新的元素线实例

### ElementLinePoint.g
元素线点类，表示元素线上的一个点。

#### 字段
- `time`: 该点所在时间
- `position`: 该点绘制的纵向位置，从下向上，范围为0-1
- `width`: 该点绘制的纵向宽度，范围为0-1

#### 构造函数
- `ElementLinePoint(float time, float position, float width)`: 使用指定时间、位置和宽度创建一个新的元素线点实例