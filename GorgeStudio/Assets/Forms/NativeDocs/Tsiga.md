# Native/Framework/Tsiga 目录文档

## 概述
`Native/Framework/Tsiga` 目录包含了 Gorge Chart Studio 中的自动机和输入处理相关组件，这些组件用于处理音符的输入、响应和状态管理。

## 类和接口详解

### IAutomatonCommand.g
自动机命令接口，所有自动机命令类型的基接口。

### AppendSignalCommand.g
追加信号边沿的指令类，实现 [IAutomatonCommand](#iautomatoncommandg) 接口。

#### 字段
- `channelName`: 信道名称
- `id`: 信号ID
- `delaySimulateTime`: 延迟模拟时间
- `value`: 信号值，类型为 [ISignal](Signal.md#isignalg)

#### 构造函数
- `AppendSignalCommand(string channelName, int id, float delaySimulateTime, ISignal value)`: 使用指定参数创建一个新的追加信号命令实例

### DeriveElementCommand.g
由Note向自动机发出的，派生element的指令类，实现 [IAutomatonCommand](#iautomatoncommandg) 接口。

#### 字段
- `element`: 元素，类型为 [Element](../Element.g)
- `changeAutomaton`: 是否改变自动机

#### 构造函数
- `DeriveElementCommand(Element element, bool changeAutomaton)`: 使用指定参数创建一个新的派生元素命令实例

### DestroyElementCommand.g
由Note向自动机发出的，销毁element的指令类，实现 [IAutomatonCommand](#iautomatoncommandg) 接口。

#### 字段
- `element`: 元素，类型为 [Element](../Element.g)
- `changeAutomaton`: 是否改变自动机

#### 构造函数
- `DestroyElementCommand(Element element, bool changeAutomaton)`: 使用指定参数创建一个新的销毁元素命令实例

### FloatSignalFilter.g
浮点信号过滤器类，继承自 [SignalFilter](Signal.md#signalfilterg)。

#### 字段
- `filterRange`: 触控区，类型为 delegate<bool:FloatSignal>
- `channelName`: 过滤的频道名

#### 构造函数
- `FloatSignalFilter(string channelName, delegate<Priority[]> priority, int[] conditionType, delegate<bool:FloatSignal> filterRange, delegate<float> endTime, TimeMode timeMode, bool acceptConsume, bool denyConsume)`: 使用指定参数创建一个新的浮点信号过滤器实例

#### 方法
- `CanDetect(string channelName)`: 判断是否可以检测指定信道
- `Detect(string channelName, int signalId, int conditionType, ISignal signalValue, ISignal lastSignalValue)`: 执行检测，返回值为是否可接受

### HistoryStack.g
历史栈类。

#### 构造函数
- `HistoryStack()`: 创建一个新的历史栈实例

### InputGraph.g
输入图类。

#### 构造函数
- `InputGraph(InputGraphState[] states, bool accept, bool stackRespond, int inputPointer, string exportState)`: 使用指定参数创建一个新的输入图实例

#### 方法
- `GoAcceptEdge(float chartTime, HistoryStack historyStack)`: 进入接收边，只执行状态修改，不执行动作，返回值为接受边对象
- `GoDenyEdge(float chartTime, HistoryStack historyStack)`: 进入拒绝边，只执行状态修改，不执行动作，返回值为接受边对象

### InputGraphEdge.g
输入图的一条边。

#### 字段
- `deny`: 是否直接进入失败停机
- `jump`: 跳转步数（deny为true则不跳转）
- `stackAction`: 跳转时栈操作，类型为 delegate<void:Note,TimeStack,float,HistoryStack>
- `accept`: 是否进入接收模式
- `stackRespond`: 是否进入弹栈响应模式
- `edgeRespond`: 是否在跳转时尝试响应，具体发生的响应内容由时间栈确定
- `exportState`: 进入的导出状态，如果为null则保持原状

#### 构造函数
- `InputGraphEdge(bool deny, int jump, delegate<void:Note,TimeStack,float,HistoryStack> stackAction, bool accept, bool stackRespond, bool edgeRespond, string exportState)`: 使用指定参数创建一个新的输入图边实例

### InputGraphState.g
输入图的一个状态。

#### 字段
- `filter`: 输入信号过滤器，类型为 [SignalFilter](Signal.md#signalfilterg)
- `acceptedEdge`: 过滤成功出边，类型为 [InputGraphEdge](#inputgraphedgeg)
- `deniedEdge`: 过滤失败出边，类型为 [InputGraphEdge](#inputgraphedgeg)

#### 构造函数
- `InputGraphState(SignalFilter filter, InputGraphEdge acceptedEdge, InputGraphEdge deniedEdge)`: 使用指定参数创建一个新的输入图状态实例

### InputSignalFilter.g
输入信号过滤器类，继承自 [SignalFilter](Signal.md#signalfilterg)。

#### 字段
- `touchArea`: 触控区，类型为 delegate<bool:TouchSignal>
- `signalIdFilter`: 信号编号过滤器，类型为 delegate<bool:int>
- `onDetected`: 信号检查调用，无论过滤是否成功，都会调用，类型为 delegate<void:int,TouchSignal>

#### 构造函数
- `InputSignalFilter(delegate<Priority[]> priority, delegate<void:int,TouchSignal> onDetected, int[] touchType, delegate<bool:int> signalIdFilter, delegate<bool:TouchSignal> touchArea, delegate<float> endTime, TimeMode timeMode, bool acceptConsume, bool denyConsume)`: 使用指定参数创建一个新的输入信号过滤器实例

#### 方法
- `CanDetect(string channelName)`: 判断是否可以检测指定信道
- `Detect(string channelName, int signalId, int conditionType, ISignal signalValue, ISignal lastSignalValue)`: 执行检测，返回值为是否可接受

### Priority.g
优先级类。

#### 字段
- `getPriority`: 获取优先级的委托，类型为 delegate<float:ISignal>

#### 构造函数
- `Priority(delegate<float:ISignal> getPriority)`: 使用指定参数创建一个新的优先级实例

### SignalTsiga.g
信号 Tsiga 类。

#### 构造函数
- `SignalTsiga(Note note, TimeStack timeStack, InputGraph inputGraph, HistoryStack historyStack)`: 使用指定参数创建一个新的信号 Tsiga 实例

#### 方法
- `GetState()`: 获取当前状态

### TimeItem.g
时间项类。

#### 字段
- `accept`: 是否接收
- `respondMode`: 响应结果，为null代表不响应
- `time`: 出栈时间，类型为 delegate<float>

#### 构造函数
- `TimeItem(delegate<float> time, bool accept, string respondMode)`: 使用指定参数创建一个新的时间项实例

### TimeMode.g
输入项的时间类型枚举。

#### 枚举值
- `CatchBefore`: 在结束时间前捕获一次，捕获成功则跳转
- `KeepUntil`: 在结束时间前始终存在，时间结束时跳转

### TimeStack.g
时间栈类。

#### 构造函数
- `TimeStack(bool accept, string respondMode)`: 使用指定参数创建一个新的时间栈实例

#### 方法
- `InitPush(TimeItem timeItem)`: 初始化推送时间项
- `Pop(float chartTime, HistoryStack historyStack)`: 弹出时间项
- `Push(float chartTime, TimeItem timeItem, HistoryStack historyStack)`: 推送时间项