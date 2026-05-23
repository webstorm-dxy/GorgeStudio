# Native/Framework/Deenty 目录文档

## 概述
`Native/Framework/Deenty` 目录包含了 Gorge Chart Studio 中与音符相关的组件，这些组件用于处理音符之间的关联和响应结果。

## 类和文件详解

### NoteLinkage.g
音符关联类，用于定义音符之间的关联关系。

#### 字段
- `json`: JSON 格式的关联数据

#### 构造函数
- `NoteLinkage()`: 创建一个新的音符关联实例

### RespondResult.g
响应结果枚举，定义了音符响应的不同结果类型。

#### 枚举值
- `Miss`: 未命中
- `Good`: 良好
- `Perfect`: 完美
- `BestPerfect`: 最佳完美