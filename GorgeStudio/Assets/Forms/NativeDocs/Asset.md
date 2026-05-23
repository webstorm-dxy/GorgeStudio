# Native/Framework/Asset 目录文档

## 概述
`Native/Framework/Asset` 目录包含了 Gorge Chart Studio 中的资源管理组件，这些组件用于加载和管理各种类型的资源，如音频、图像和视频。

## 类和文件详解

### Asset.g
资源基类，所有资源类型的基类。

#### 字段
- `name`: 资源名称

#### 构造函数
- `Asset()`: 创建一个新的资源实例

#### 方法
- `LoadAsset()`: 加载资源，返回布尔值表示是否加载成功

### Audio.g
音频资源类，表示一个音频资源。

### AudioAsset.g
音频资源管理类，继承自 [Asset](#assetg)。

#### 构造函数
- `AudioAsset()`: 创建一个新的音频资源管理实例

#### 方法
- `GetAsset()`: 获取音频资源
- `LoadAsset()`: 加载音频资源，返回布尔值表示是否加载成功

### Graph.g
图资源类，表示一个图像资源。

#### 字段
- `width`: 图像宽度（像素数）
- `height`: 图像高度（像素数）

### GraphAsset.g
图资源管理类，继承自 [Asset](#assetg)。

#### 构造函数
- `GraphAsset()`: 创建一个新的图资源管理实例

#### 方法
- `GetAsset()`: 获取图像资源

### ImageAsset.g
图像资源管理类，继承自 [GraphAsset](#graphassetg)。

#### 字段
- `texture`: 图像纹理，类型为 [Graph](#graphg)

#### 构造函数
- `ImageAsset()`: 创建一个新的图像资源管理实例

#### 方法
- `DescriptorDisplayString()`: 获取资源描述显示字符串
- `GetAsset()`: 获取图像资源
- `LoadAsset()`: 加载图像资源，返回布尔值表示是否加载成功

### NativeAudioAsset.g
原生音频资源管理类，继承自 [AudioAsset](#audioassetg)。

#### 字段
- `audio`: 音频资源，类型为 [Audio](#audiog)

#### 构造函数
- `NativeAudioAsset()`: 创建一个新的原生音频资源管理实例

#### 方法
- `GetAsset()`: 获取音频资源
- `LoadAsset()`: 加载音频资源，返回布尔值表示是否加载成功

### NativeVideoAsset.g
原生视频资源管理类，继承自 [VideoAsset](#videoassetg)。

#### 字段
- `video`: 视频资源，类型为 [Video](#videog)

#### 构造函数
- `NativeVideoAsset()`: 创建一个新的原生视频资源管理实例

#### 方法
- `GetAsset()`: 获取视频资源
- `LoadAsset()`: 加载视频资源，返回布尔值表示是否加载成功

### Video.g
视频资源类，表示一个视频资源。

### VideoAsset.g
视频资源管理类，继承自 [Asset](#assetg)。

#### 构造函数
- `VideoAsset()`: 创建一个新的视频资源管理实例

#### 方法
- `GetAsset()`: 获取视频资源
- `LoadAsset()`: 加载视频资源，返回布尔值表示是否加载成功

### WavAudioAsset.g
WAV 音频资源管理类，继承自 [AudioAsset](#audioassetg)。

#### 字段
- `wavFilePath`: WAV 文件路径

#### 构造函数
- `WavAudioAsset()`: 创建一个新的 WAV 音频资源管理实例（注意：代码中显示构造函数名为 NativeAudioAsset，这可能是一个笔误）

#### 方法
- `GetAsset()`: 获取音频资源
- `LoadAsset()`: 加载音频资源，返回布尔值表示是否加载成功