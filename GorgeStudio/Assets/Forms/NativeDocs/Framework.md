# Native/Framework 目录文档

## 概述
`Native/Framework` 目录包含了 Gorge Chart Studio 的核心框架组件，这些组件为游戏开发提供了基础功能和抽象。

## 子目录和文件

### Asset
- `Asset.g`: 资源基类
- `Audio.g`: 音频资源
- `AudioAsset.g`: 音频资源管理
- `Graph.g`: 图资源
- `GraphAsset.g`: 图资源管理
- `ImageAsset.g`: 图像资源管理
- `NativeAudioAsset.g`: 原生音频资源
- `NativeVideoAsset.g`: 原生视频资源
- `Video.g`: 视频资源
- `VideoAsset.g`: 视频资源管理
- `WavAudioAsset.g`: WAV 音频资源

### Curve
#### ColorCurve
- `ColorCurve.g`: 颜色曲线基类
- `LerpColorCurve.g`: 线性插值颜色曲线

#### FunctionCurve
- `AdditionFunctionCurve.g`: 加法函数曲线
- `ArcFunctionCurve.g`: 弧线函数曲线
- `AxialSymmetricFunctionCurve.g`: 轴对称函数曲线
- `CompositeFunctionCurve.g`: 复合函数曲线
- `ConstantFunctionCurve.g`: 常量函数曲线
- `CubicHermiteSpline.g`: 三次 Hermite 样条
- `FunctionCurve.g`: 函数曲线基类
- `FunctionPiece.g`: 函数片段
- `LinearCurve.g`: 线性曲线
- `LinearFunctionCurve.g`: 线性函数曲线
- `MultiplicationFunctionCurve.g`: 乘法函数曲线
- `PeriodicFunctionCurve.g`: 周期函数曲线
- `PiecewiseFunctionCurve.g`: 分段函数曲线
- `QuadraticFunctionCurve.g`: 二次函数曲线

### Deenty
- `NoteLinkage.g`: 音符关联
- `RespondResult.g`: 响应结果

### Gameplay
- `EditUpdateMode.g`: 编辑更新模式
- `Environment.g`: 环境配置
- `PeriodConfig.g`: 周期配置

### GorgeEditor
- `ElementLine.g`: 元素线
- `ElementLinePoint.g`: 元素线点

### Graphics
- `AnnulusMeshTransformer.g`: 环形网格变换器
- `Axis.g`: 轴
- `CurvedSideSprite.g`: 弯曲边精灵
- `CurveMeshTransformer.g`: 曲线网格变换器
- `CurveSprite.g`: 曲线精灵
- `CurveWarpTransformer.g`: 曲线扭曲变换器
- `IMeshTransformer.g`: 网格变换器接口
- `MeshedSprite.g`: 网格精灵
- `NineSliceSprite.g`: 九宫格精灵
- `Node.g`: 节点
- `Sprite.g`: 精灵
- `VideoSprite.g`: 视频精灵

### Signal
- `BoolSignal.g`: 布尔信号
- `FloatSignal.g`: 浮点信号
- `FloatSignalConditionType.g`: 浮点信号条件类型
- `ISignal.g`: 信号接口
- `SignalFilter.g`: 信号过滤器
- `TouchSignal.g`: 触摸信号
- `TouchType.g`: 触摸类型

### System
- `ColorArgb.g`: ARGB 颜色
- `Logger.g`: 日志记录器
- `Math.g`: 数学工具
- `Random.g`: 随机数生成器
- `Vector2.g`: 二维向量
- `Vector3.g`: 三维向量

### Tsiga
- `AppendSignalCommand.g`: 追加信号命令
- `DeriveElementCommand.g`: 派生元素命令
- `DestroyElementCommand.g`: 销毁元素命令
- `FloatSignalFilter.g`: 浮点信号过滤器
- `HistoryStack.g`: 历史栈
- `IAutomatonCommand.g`: 自动机命令接口
- `InputGraph.g`: 输入图
- `InputGraphEdge.g`: 输入图边
- `InputGraphState.g`: 输入图状态
- `InputSignalFilter.g`: 输入信号过滤器
- `Priority.g`: 优先级
- `SignalTsiga.g`: 信号 Tsiga
- `TimeItem.g`: 时间项
- `TimeMode.g`: 时间模式
- `TimeStack.g`: 时间栈

## 根目录文件
- `Element.g`: 元素基类
- `ElementSimulator.g`: 元素模拟器
- `ITransformer.g`: 变换器接口
- `Note.g`: 音符基类
- `VariableFloat.g`: 可变浮点数