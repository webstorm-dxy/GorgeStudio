# Native/Framework/Curve 目录文档

## 概述
`Native/Framework/Curve` 目录包含了 Gorge Chart Studio 中的曲线相关组件，这些组件用于定义和计算各种类型的数学曲线，包括函数曲线和颜色曲线。

## 子目录

### ColorCurve
颜色曲线相关类。

#### ColorCurve.g
颜色曲线基类。

##### 构造函数
- `ColorCurve()`: 创建一个新的颜色曲线实例

##### 方法
- `Evaluate(float x)`: 根据给定的 x 值计算并返回对应的颜色值

#### LerpColorCurve.g
线性插值颜色曲线类，继承自 [ColorCurve](#colorcurvag)。

##### 字段
- `colorPoints`: 颜色点数组，类型为 ColorArgb[]
- `progressCurve`: 进度曲线，类型为 [FunctionCurve](#functioncurvag)，用于定义在颜色点之间的插值进度

##### 构造函数
- `LerpColorCurve()`: 创建一个新的线性插值颜色曲线实例

##### 方法
- `Evaluate(float x)`: 根据给定的 x 值计算并返回通过线性插值得到的颜色值

### FunctionCurve
函数曲线相关类。

#### FunctionCurve.g
函数曲线基类。

##### 构造函数
- `FunctionCurve()`: 创建一个新的函数曲线实例

##### 方法
- `Evaluate(float x)`: 根据给定的 x 值计算并返回对应的函数值

#### AdditionFunctionCurve.g
加法函数曲线类，继承自 [FunctionCurve](#functioncurvag)。将两个函数曲线的值相加。

##### 字段
- `firstFunctionCurve`: 第一个函数曲线，类型为 [FunctionCurve](#functioncurvag)
- `secondFunctionCurve`: 第二个函数曲线，类型为 [FunctionCurve](#functioncurvag)

##### 构造函数
- `AdditionFunctionCurve()`: 创建一个新的加法函数曲线实例

##### 方法
- `Evaluate(float x)`: 计算两个函数曲线在给定点的值之和

#### ArcFunctionCurve.g
弧函数曲线类，继承自 [FunctionCurve](#functioncurvag)。按圆心角在位于x轴的指定弦上生成一个弧。

##### 字段
- `chordStart`: 弦起点坐标
- `chordEnd`: 弦终点坐标
- `angle`: 圆心角，使用弧度制

##### 构造函数
- `ArcFunctionCurve()`: 创建一个新的弧函数曲线实例
- `ArcFunctionCurve(float chordStart, float chordEnd, float angle)`: 使用指定参数创建一个新的弧函数曲线实例

##### 方法
- `Evaluate(float x)`: 根据给定的 x 值计算并返回对应的弧函数值

#### AxialSymmetricFunctionCurve.g
轴对称函数曲线类，继承自 [FunctionCurve](#functioncurvag)。

##### 字段
- `functionCurve`: 原始函数曲线，类型为 [FunctionCurve](#functioncurvag)
- `axis`: 对称轴位置
- `keepLeft`: 是否保留左侧而对称到右侧

##### 构造函数
- `AxialSymmetricFunctionCurve()`: 创建一个新的轴对称函数曲线实例

##### 方法
- `Evaluate(float x)`: 根据给定的 x 值计算并返回对应的轴对称函数值

#### CompositeFunctionCurve.g
复合函数曲线类，继承自 [FunctionCurve](#functioncurvag)。

##### 字段
- `outerFunctionCurve`: 外层函数曲线，类型为 [FunctionCurve](#functioncurvag)
- `innerFunctionCurve`: 内层函数曲线，类型为 [FunctionCurve](#functioncurvag)

##### 构造函数
- `CompositeFunctionCurve()`: 创建一个新的复合函数曲线实例
- `CompositeFunctionCurve(FunctionCurve outerFunctionCurve, FunctionCurve innerFunctionCurve)`: 使用指定的外层和内层函数曲线创建一个新的复合函数曲线实例

##### 方法
- `Evaluate(float x)`: 计算复合函数在给定点的值，即 outerFunctionCurve(innerFunctionCurve(x))

#### ConstantFunctionCurve.g
常量函数曲线类，继承自 [FunctionCurve](#functioncurvag)。

##### 字段
- `value`: 常量值

##### 构造函数
- `ConstantFunctionCurve()`: 创建一个新的常量函数曲线实例
- `ConstantFunctionCurve(float value)`: 使用指定值创建一个新的常量函数曲线实例

##### 方法
- `Evaluate(float x)`: 对于任意 x 值都返回相同的常量值

#### CubicHermiteSpline.g
加权三次埃尔米特样条曲线类，继承自 [FunctionCurve](#functioncurvag)。

##### 字段
- `startPoint`: 起点，类型为 Vector2
- `startTangent`: 起点切线
- `startWeight`: 起点权重
- `endPoint`: 终点，类型为 Vector2
- `endTangent`: 终点切线
- `endWeight`: 终点权重

##### 构造函数
- `CubicHermiteSpline()`: 创建一个新的三次埃尔米特样条曲线实例

##### 方法
- `Evaluate(float x)`: 根据给定的 x 值计算并返回对应的三次埃尔米特样条函数值

#### FunctionPiece.g
函数片段类，表示函数曲线的一部分。

##### 字段
- `functionCurve`: 函数曲线，类型为 [FunctionCurve](#functioncurvag)
- `startX`: 起始 x 值
- `endX`: 结束 x 值
- `leftClosed`: 左端点是否闭合
- `rightClosed`: 右端点是否闭合

##### 构造函数
- `FunctionPiece()`: 创建一个新的函数片段实例

#### LinearCurve.g
线性曲线类，继承自 [FunctionCurve](#functioncurvag)。

##### 字段
- `timeStart`: 起始时间
- `valueStart`: 起始值
- `timeEnd`: 结束时间
- `valueEnd`: 结束值

##### 构造函数
- `LinearCurve()`: 创建一个新的线性曲线实例
- `LinearCurve(float timeStart, float valueStart, float timeEnd, float valueEnd)`: 使用指定参数创建一个新的线性曲线实例

##### 方法
- `Evaluate(float time)`: 根据给定的时间计算并返回对应的线性插值函数值

#### LinearFunctionCurve.g
线性函数曲线类（y = kx + b），继承自 [FunctionCurve](#functioncurvag)。

##### 字段
- `k`: 斜率
- `b`: y轴截距

##### 构造函数
- `LinearFunctionCurve()`: 创建一个新的线性函数曲线实例
- `LinearFunctionCurve(float k, float b)`: 使用指定斜率和截距创建一个新的线性函数曲线实例

##### 方法
- `Evaluate(float x)`: 根据给定的 x 值计算并返回对应的线性函数值

#### MultiplicationFunctionCurve.g
乘法函数曲线类，继承自 [FunctionCurve](#functioncurvag)。将两个函数曲线的值相乘。

##### 字段
- `firstFunctionCurve`: 第一个函数曲线，类型为 [FunctionCurve](#functioncurvag)
- `secondFunctionCurve`: 第二个函数曲线，类型为 [FunctionCurve](#functioncurvag)

##### 构造函数
- `MultiplicationFunctionCurve()`: 创建一个新的乘法函数曲线实例
- `MultiplicationFunctionCurve(FunctionCurve firstFunctionCurve, FunctionCurve secondFunctionCurve)`: 使用指定的两个函数曲线创建一个新的乘法函数曲线实例

##### 方法
- `Evaluate(float x)`: 计算两个函数曲线在给定点的值之积

#### PeriodicFunctionCurve.g
周期函数曲线类，继承自 [FunctionCurve](#functioncurvag)。

##### 字段
- `functionCurve`: 基础函数曲线，类型为 [FunctionCurve](#functioncurvag)
- `startX`: 起始 x 值
- `endX`: 结束 x 值
- `leftClosed`: 左包含，否则为右包含

##### 构造函数
- `PeriodicFunction()`: 创建一个新的周期函数曲线实例

##### 方法
- `Evaluate(float x)`: 根据给定的 x 值计算并返回对应的周期函数值

#### PiecewiseFunctionCurve.g
分段函数曲线类，继承自 [FunctionCurve](#functioncurvag)。

##### 字段
- `functionPieces`: 函数片段数组，类型为 [FunctionPiece](#functionpieceg)[]

##### 构造函数
- `PiecewiseFunctionCurve()`: 创建一个新的分段函数曲线实例

##### 方法
- `Evaluate(float x)`: 根据给定的 x 值在对应的函数片段上计算并返回函数值

#### QuadraticFunctionCurve.g
二次函数曲线类（y = ax² + bx + c），继承自 [FunctionCurve](#functioncurvag)。

##### 字段
- `a`: 二次项系数
- `b`: 一次项系数
- `c`: 常数项

##### 构造函数
- `QuadraticFunctionCurve()`: 创建一个新的二次函数曲线实例
- `QuadraticFunctionCurve(float a, float b, float c)`: 使用指定系数创建一个新的二次函数曲线实例

##### 方法
- `Evaluate(float x)`: 根据给定的 x 值计算并返回对应的二次函数值