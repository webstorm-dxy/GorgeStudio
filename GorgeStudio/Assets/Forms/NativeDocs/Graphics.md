# Native/Framework/Graphics 目录文档

## 概述
`Native/Framework/Graphics` 目录包含了 Gorge Chart Studio 中的图形相关组件，这些组件用于处理游戏中的视觉元素，包括节点、精灵、变换器等。

## 类和接口详解

### IMeshTransformer.g
网格变换器接口，定义了网格顶点变换的方法。

#### 方法
- `Transform(Vector3 vertex)`: 对给定的顶点进行变换并返回变换后的顶点

### AnnulusMeshTransformer.g
环形网格变换器类，实现 [IMeshTransformer](#imeshtransformerg) 接口，将方形网格变换为扇环。

#### 字段
- `xAngle`: 将x坐标映射到角度（弧度制）的函数，类型为 [FunctionCurve](Curve.md#functioncurvag)
- `yRadius`: 将y坐标映射到半径的函数，类型为 [FunctionCurve](Curve.md#functioncurvag)

#### 构造函数
- `AnnulusMeshTransformer()`: 创建一个新的环形网格变换器实例

#### 方法
- `Transform(Vector3 vertex)`: 对给定的顶点进行环形变换并返回变换后的顶点

### Axis.g
轴向枚举，定义了三维空间中的轴向。

#### 枚举值
- `X`: X轴
- `Y`: Y轴
- `Z`: Z轴

### CurveMeshTransformer.g
曲线网格变换器类，实现 [IMeshTransformer](#imeshtransformerg) 接口，在纵向或横向上生效，在指定方向上按曲线扭曲，另一方向不变形。

#### 字段
- `curve`: 形状曲线，类型为 [FunctionCurve](Curve.md#functioncurvag)
- `isHorizontal`: 是否在横向上变形

#### 构造函数
- `CurveMeshTransformer()`: 创建一个新的曲线网格变换器实例

#### 方法
- `Transform(Vector3 vertex)`: 对给定的顶点进行曲线变换并返回变换后的顶点

### CurveSprite.g
曲线精灵类，继承自 [Node](#nodeg)。

#### 字段
- `points`: 曲线点数组，类型为 Vector2[]
- `color`: 颜色，类型为 ColorArgb
- `width`: 宽度

#### 构造函数
- `Curve2D(Vector2[] points)`: 使用指定点数组创建一个新的曲线精灵实例

### CurveWarpTransformer.g
曲线扭曲变换器类，实现 [IMeshTransformer](#imeshtransformerg) 接口，将x轴扭曲为目标曲线，同时保持法线做纵向映射。

#### 字段
- `curve`: 形状曲线，类型为 [FunctionCurve](Curve.md#functioncurvag)
- `preserveProportions`: 是否关闭曲率畸变
- `curvatureInfluence`: 曲率畸变强度
- `transformedAxis`: 待变换轴向，对应曲线x轴，类型为 [Axis](#axisg)
- `curveValueAxis`: 曲线值轴向，对应曲线y轴，类型为 [Axis](#axisg)

#### 构造函数
- `CurveWarpTransformer()`: 创建一个新的曲线扭曲变换器实例

#### 方法
- `Transform(Vector3 vertex)`: 对给定的顶点进行曲线扭曲变换并返回变换后的顶点

### CurvedSideSprite.g
曲边四边形类，继承自 [Node](#nodeg)。

#### 字段
- `graph`: 图像，类型为 [Graph](Asset.md#graphg)
- `vertexLeftBottom`: 左下顶点位置
- `vertexRightBottom`: 右下顶点位置
- `vertexLeftTop`: 左上顶点位置
- `vertexRightTop`: 右上顶点位置
- `leftCurve`: 左边曲线，类型为 [FunctionCurve](Curve.md#functioncurvag)
- `topCurve`: 上边曲线，类型为 [FunctionCurve](Curve.md#functioncurvag)
- `rightCurve`: 右边曲线，类型为 [FunctionCurve](Curve.md#functioncurvag)
- `bottomCurve`: 下边曲线，类型为 [FunctionCurve](Curve.md#functioncurvag)
- `color`: 颜色，类型为 ColorArgb
- `horizontalSegments`: 水平网格段数
- `verticalSegments`: 垂直网格段数

#### 构造函数
- `CurvedSideSprite(Graph graph)`: 使用指定图像创建一个新的曲边四边形实例

### MeshedSprite.g
网格化精灵类，继承自 [Node](#nodeg)。

#### 字段
- `graph`: 图像，类型为 [Graph](Asset.md#graphg)
- `centerX`: 中心点x坐标
- `centerY`: 中心点y坐标
- `width`: 宽度
- `height`: 高度
- `color`: 颜色，类型为 ColorArgb
- `horizontalSegments`: 水平网格段数
- `verticalSegments`: 垂直网格段数

#### 构造函数
- `MeshedSprite(Graph graph)`: 使用指定图像创建一个新的网格化精灵实例

#### 方法
- `AddMeshTransformer(IMeshTransformer transformer)`: 添加网格变换器
- `ForceUpdate()`: 强制更新

### NineSliceSprite.g
九宫格精灵类，继承自 [Node](#nodeg)。

#### 字段
- `graph`: 图像，类型为 [Graph](Asset.md#graphg)
- `sliceLeftTop`: 左上切分点(像素数)
- `sliceRightBottom`: 右下切分点(像素数)
- `baseSize`: 基本尺寸(不做任何9slice拉伸的情况下的原始大小)
- `color`: 颜色，类型为 ColorArgb
- `hsl`: 颜色偏移

#### 构造函数
- `NineSliceSprite(Graph graph, Vector2 sliceLeftTop, Vector2 sliceRightBottom, Vector2 baseSize)`: 使用指定参数创建一个新的九宫格精灵实例

### Node.g
图形节点基类。

#### 字段
- `alive`: 是否存活
- `existenceReference`: 存在性参考节点，类型为 [Node](#nodeg)
- `position`: 相对位置
- `positionReference`: 相对位置参考节点，类型为 [Node](#nodeg)
- `rotation`: 相对角度（暂时使用欧拉表示）
- `rotationReference`: 相对角度参考节点，类型为 [Node](#nodeg)
- `size`: 相对尺寸
- `sizeReference`: 相对尺寸参考节点，类型为 [Node](#nodeg)

#### 构造函数
- `Node()`: 创建一个新的节点实例

#### 方法
- `GlobalPosition()`: 获取全局位置
- `LocalPositionToGlobalPosition(Vector3 position)`: 将局部位置转换为全局位置
- `GlobalPositionToLocalPosition(Vector3 position)`: 将全局位置转换为局部位置

### Sprite.g
精灵类，继承自 [Node](#nodeg)。

#### 字段
- `graph`: 图像，类型为 [Graph](Asset.md#graphg)
- `color`: 颜色，类型为 ColorArgb

#### 构造函数
- `Sprite(Graph graph)`: 使用指定图像创建一个新的精灵实例

### VideoSprite.g
视频精灵类，继承自 [Node](#nodeg)。

#### 字段
- `video`: 视频，类型为 [Video](Asset.md#videog)
- `color`: 颜色，类型为 ColorArgb
- `videoWidth`: 视频宽度
- `videoHeight`: 视频高度

#### 构造函数
- `VideoSprite(Video video)`: 使用指定视频创建一个新的视频精灵实例

#### 方法
- `SetTime(float time)`: 设置视频播放时间