# Native/Framework/System 目录文档

## 概述
`Native/Framework/System` 目录包含了 Gorge Chart Studio 中的系统级工具类，这些组件提供了常用的系统功能，如数学计算、日志记录、随机数生成等。

## 类和文件详解

### ColorArgb.g
ARGB 颜色类。

#### 字段
- `a`: Alpha 通道值（透明度）
- `r`: Red 通道值（红色）
- `g`: Green 通道值（绿色）
- `b`: Blue 通道值（蓝色）

#### 构造函数
- `ColorArgb()`: 创建一个新的默认颜色实例（默认为黑色不透明）
- `ColorArgb(float a, float r, float g, float b)`: 使用指定通道值创建一个新的颜色实例

### Logger.g
日志记录器类。

#### 方法
- `Log(string info)`: 记录指定信息到日志

### Math.g
数学工具类，提供各种数学计算方法。

#### 方法
- `Abs(float f)`: 计算浮点数的绝对值
- `Sqrt(float f)`: 计算浮点数的平方根
- `Max(float f1, float f2)`: 返回两个浮点数中的最大值
- `Max(float f1, float f2, float f3, float f4)`: 返回四个浮点数中的最大值
- `Min(float f1, float f2)`: 返回两个浮点数中的最小值
- `Atan(float f)`: 计算浮点数的反正切值
- `Sin(float f)`: 计算浮点数的正弦值（弧度制）
- `Cos(float f)`: 计算浮点数的余弦值（弧度制）
- `CosDeg(float f)`: 计算浮点数的余弦值（角度制）
- `SinDeg(float f)`: 计算浮点数的正弦值（角度制）
- `Pi()`: 获取圆周率π的值
- `FloatPositiveInfinity()`: 获取正无穷大值
- `FloatNegativeInfinity()`: 获取负无穷大值
- `Floor(float f)`: 对浮点数向下取整
- `Ceil(float f)`: 对浮点数向上取整
- `ClampInt(int a, int b, int value)`: 将整数值限制在指定范围内
- `Lerp(float a, float b, float t)`: 在两个浮点数之间进行线性插值
- `InverseLerp(float a, float b, float v)`: 计算指定值在两个浮点数之间的插值比例

### Random.g
随机数生成器类。

#### 方法
- `RandomNormalized()`: 生成归一化的随机向量
- `RandomFloat(float a, float b)`: 生成指定范围内的随机浮点数

### Vector2.g
二维向量类。

#### 字段
- `x`: X 坐标值
- `y`: Y 坐标值

#### 构造函数
- `Vector2()`: 创建一个新的默认二维向量实例（默认为零向量）
- `Vector2(float x, float y)`: 使用指定坐标值创建一个新的二维向量实例

#### 方法
- `ToVector3()`: 将二维向量转换为三维向量
- `Scale(Vector2 v1, Vector2 v2)`: 对两个二维向量进行缩放（逐分量相乘）
- `Distance(Vector2 v1, Vector2 v2)`: 计算两个二维向量之间的距离
- `Normalize(Vector2 v)`: 对二维向量进行归一化
- `Angle(Vector2 v)`: 计算二维向量的角度（角度制）
- `Lerp(Vector2 a, Vector2 b, float t)`: 在两个二维向量之间进行线性插值

### Vector3.g
三维向量类。

#### 字段
- `x`: X 坐标值
- `y`: Y 坐标值
- `z`: Z 坐标值

#### 构造函数
- `Vector3()`: 创建一个新的默认三维向量实例（默认为零向量）
- `Vector3(float x, float y, float z)`: 使用指定坐标值创建一个新的三维向量实例

#### 方法
- `ToVector2()`: 将三维向量转换为二维向量（忽略 Z 分量）