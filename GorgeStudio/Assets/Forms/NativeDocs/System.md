# Native/System 目录文档

## 概述
`Native/System` 目录包含了 Gorge Chart Studio 的系统级数据结构组件，这些组件为游戏开发提供了基础的数据结构实现。

## 类和文件详解

### BoolArray.g
布尔数组类。

#### 字段
- `length`: 数组长度

#### 构造函数
- `BoolArray()`: 创建一个新的布尔数组实例

#### 方法
- `Get(int index)`: 获取指定索引处的值
- `Set(int index, bool value)`: 设置指定索引处的值

### BoolList.g
布尔列表类。

#### 字段
- `length`: 列表长度

#### 构造函数
- `BoolList()`: 创建一个新的布尔列表实例

#### 方法
- `Get(int index)`: 获取指定索引处的值
- `Set(int index, bool value)`: 设置指定索引处的值
- `Add(bool value)`: 添加值到列表末尾
- `RemoveAt(int index)`: 移除指定索引处的值

### FloatArray.g
浮点数组类。

#### 字段
- `length`: 数组长度

#### 构造函数
- `FloatArray()`: 创建一个新的浮点数组实例

#### 方法
- `Get(int index)`: 获取指定索引处的值
- `Set(int index, float value)`: 设置指定索引处的值

### FloatList.g
浮点列表类。

#### 字段
- `length`: 列表长度

#### 构造函数
- `FloatList()`: 创建一个新的浮点列表实例

#### 方法
- `Get(int index)`: 获取指定索引处的值
- `Set(int index, float value)`: 设置指定索引处的值
- `Add(float value)`: 添加值到列表末尾
- `RemoveAt(int index)`: 移除指定索引处的值

### Injector.g
注入器类，用于依赖注入。

#### 泛型参数
- `TBaseObject`: 基对象类型

### IntArray.g
整数数组类。

#### 字段
- `length`: 数组长度

#### 构造函数
- `IntArray()`: 创建一个新的整数数组实例

#### 方法
- `Get(int index)`: 获取指定索引处的值
- `Set(int index, int value)`: 设置指定索引处的值

### IntList.g
整数列表类。

#### 字段
- `length`: 列表长度

#### 构造函数
- `IntList()`: 创建一个新的整数列表实例

#### 方法
- `Get(int index)`: 获取指定索引处的值
- `Set(int index, int value)`: 设置指定索引处的值
- `Add(int value)`: 添加值到列表末尾
- `RemoveAt(int index)`: 移除指定索引处的值

### ObjectArray.g
对象数组类。

#### 泛型参数
- `TItem`: 对象类型

#### 字段
- `length`: 数组长度

#### 构造函数
- `ObjectArray()`: 创建一个新的对象数组实例

#### 方法
- `Get(int index)`: 获取指定索引处的对象
- `Set(int index, TItem value)`: 设置指定索引处的对象

### ObjectList.g
对象列表类。

#### 泛型参数
- `TItem`: 对象类型

#### 字段
- `length`: 列表长度

#### 构造函数
- `ObjectList()`: 创建一个新的对象列表实例

#### 方法
- `Get(int index)`: 获取指定索引处的对象
- `Set(int index, TItem value)`: 设置指定索引处的对象
- `Add(TItem value)`: 添加对象到列表末尾
- `RemoveAt(int index)`: 移除指定索引处的对象

### StringArray.g
字符串数组类。

#### 字段
- `length`: 数组长度

#### 构造函数
- `StringArray()`: 创建一个新的字符串数组实例

#### 方法
- `Get(int index)`: 获取指定索引处的字符串
- `Set(int index, string value)`: 设置指定索引处的字符串

### StringList.g
字符串列表类。

#### 字段
- `length`: 列表长度

#### 构造函数
- `StringList()`: 创建一个新的字符串列表实例

#### 方法
- `Get(int index)`: 获取指定索引处的字符串
- `Set(int index, string value)`: 设置指定索引处的字符串
- `Add(string value)`: 添加字符串到列表末尾
- `RemoveAt(int index)`: 移除指定索引处的字符串