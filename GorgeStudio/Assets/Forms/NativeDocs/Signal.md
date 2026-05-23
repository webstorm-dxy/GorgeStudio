# Native/Framework/Signal 目录文档

## 概述
`Native/Framework/Signal` 目录包含了 Gorge Chart Studio 中的信号相关组件，这些组件用于处理游戏中的各种信号，包括布尔信号、浮点信号和触摸信号等。

## 类和接口详解

### ISignal.g
信号接口，所有信号类型的基接口。

### BoolSignal.g
布尔信号类，实现 [ISignal](#isignalg) 接口。

#### 字段
- `value`: 布尔值

#### 构造函数
- `BoolSignal(bool value)`: 使用指定布尔值创建一个新的布尔信号实例

### FloatSignal.g
浮点信号类，实现 [ISignal](#isignalg) 接口。

#### 字段
- `value`: 浮点值

#### 构造函数
- `FloatSignal(float value)`: 使用指定浮点值创建一个新的浮点信号实例

### FloatSignalConditionType.g
浮点信号条件类型枚举。

#### 枚举值
- `Keep`: 保持
- `In`: 进入
- `Out`: 离开

### SignalFilter.g
信号过滤器类。

#### 字段
- `priority`: 优先级，类型为 delegate<Priority[]> 
- `conditionTypes`: 过滤情况集合，类型为 int[]
- `endTime`: 结束时间，类型为 delegate<float>
- `timeMode`: 时间类型，类型为 [TimeMode](Tsiga.md#timemodeg)
- `acceptConsume`: 接收时是否消耗
- `denyConsume`: 拒绝时是否消耗（危险逻辑，使用时注意）

#### 构造函数
- `InputSignalFilter(delegate<Priority[]> priority, int[] conditionTypes, delegate<float> endTime, TimeMode timeMode, bool acceptConsume, bool denyConsume)`: 使用指定参数创建一个新的信号过滤器实例

#### 方法
- `CanDetect(string channelName)`: 根据信道名判断是否是否进入检测
- `Detect(string channelName, int signalId, int conditionType, ISignal signalValue, ISignal lastSignalValue)`: 执行检测，返回值为是否可接受

### TouchSignal.g
触摸信号类，实现 [ISignal](#isignalg) 接口。

#### 字段
- `isTouching`: 是否正在触摸
- `position`: 触摸位置

#### 构造函数
- `TouchSignal(bool isTouching, Vector2 position)`: 使用指定参数创建一个新的触摸信号实例

### TouchType.g
触摸类型枚举。

#### 枚举值
- `Begin`: 开始触摸
- `Keep`: 保持触摸
- `End`: 结束触摸