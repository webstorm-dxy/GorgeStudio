# Native/Framework/Gameplay 目录文档

## 概述
`Native/Framework/Gameplay` 目录包含了 Gorge Chart Studio 中与游戏玩法相关的组件，这些组件用于处理游戏环境、编辑更新模式和周期配置等。

## 类和文件详解

### EditUpdateMode.g
编辑更新模式枚举，定义了在编辑模式下元素的更新方式。

#### 枚举值
- `Static`: 不做任何动态更新
- `ReInject`: 如果元素已经生成，则调用@EditReInject注解标记的方法进行更新；如果元素没有生成，则调用@EditTryGenerate注解标记的方法判断是否生成，如是则生成。处理后使用零推进刷新状态
- `ReGenerate`: 如果元素已生成，则销毁该元素；调用@EditTryGenerate注解标记的方法判断是否生成，如是则生成。处理后使用零推进刷新状态
- `RePlay`: 重新创建仿真环境，并推进到当前谱面时间

### Environment.g
环境配置类，提供了访问游戏环境相关信息和功能的静态方法。

#### 方法
- `GetAssetByName(string assetName)`: 根据资源名称获取资源
- `FindAliveLane(string typeName, string laneName)`: 根据类型名称和轨道名称查找存活的轨道
- `FindAliveLane(string typeName, int laneId)`: 根据类型名称和轨道ID查找存活的轨道
- `ScreenToWorldPoint(Vector3 position)`: 将屏幕坐标转换为世界坐标
- `Scoring(RespondResult result)`: 根据响应结果进行计分
- `PlayRespondEffect(string name)`: 播放指定名称的响应音效
- `ViewportSize()`: 获取视口尺寸

### PeriodConfig.g
周期配置类，用于配置时间周期相关的参数。

#### 字段
- `timeOffset`: 时间偏移量
- `minLength`: 最小长度
- `active`: 是否激活

#### 构造函数
- `PeriodConfig()`: 创建一个新的周期配置实例