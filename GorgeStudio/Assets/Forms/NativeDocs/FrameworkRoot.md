# Native/Framework 根目录文件文档

## 概述
`Native/Framework` 根目录包含了 Gorge Chart Studio 的核心框架组件，这些组件为游戏开发提供了基础功能和抽象。

## 类和接口详解

### Element.g
元素基类，是所有游戏元素的基类。

#### 字段
- `simulator`: 元素模拟器，类型为 [ElementSimulator](#elementsimulatorg)
- `lateIndependentSimulator`: 延迟独立元素模拟器，类型为 [ElementSimulator](#elementsimulatorg)
- `nodes`: 节点数组，类型为 [Node](Graphics.md#nodeg)[]
- `derivedElements`: 派生元素数组，类型为 [Element](#elementg)[]

#### 构造函数
- `Element()`: 创建一个新的元素实例

### ElementSimulator.g
元素模拟器类。

#### 字段
- `transformers`: 变换器数组，类型为 [ITransformer](#itransformerg)[]

#### 构造函数
- `ElementSimulator(ITransformer[] transformers)`: 使用指定变换器数组创建一个新的元素模拟器实例

### ITransformer.g
变换器接口。

#### 方法
- `Transform(float now)`: 执行变换，返回自动机命令数组

### Note.g
音符基类，继承自 [Element](#elementg)。

#### 字段
- `automaton`: 信号自动机，类型为 [SignalTsiga](Tsiga.md#signaltsigag)

#### 构造函数
- `Note()`: 创建一个新的音符实例

#### 方法
- `DoRespond(string respondMode, float respondChartTime)`: 执行响应，返回自动机指令表

### VariableFloat.g
可变浮点数类。

#### 字段
- `baseValue`: 基础值
- `variationCurve`: 变化曲线，类型为 [FunctionCurve](Curve.md#functioncurvag)

#### 构造函数
- `VariableFloat()`: 创建一个新的可变浮点数实例

#### 方法
- `EvaluateAdd(float curveTime)`: 计算曲线在指定时间的值并加到基础值上
- `EvaluateDoubleLerp(float curveTime, float min, float max)`: 计算曲线在指定时间的值，并将其在指定范围内进行双重线性插值