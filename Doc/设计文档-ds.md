# Redis 客户端类库 — 跨语言设计文档

> 涵盖 C# (.NET)、Go、Python 三个语言版本，三套实现共享同一套架构设计。

---

## 1. 整体架构总览

```
┌─────────────────────────────────────────────────┐
│                 应用层 (User API)                  │
│  StringCommands / HashCommands / ListCommands    │
│  SetCommands / SortedSetCommands / KeyCommands   │
│  ServerCommands / TransactionCommands            │
├─────────────────────────────────────────────────┤
│               命令层 (Command Layer)               │
│  RedisCommand — 命令编排、参数校验、调用 SendCommand │
├─────────────────────────────────────────────────┤
│              协议层 (Protocol Layer)               │
│  RESPEncoder — 命令 → RESP 协议格式               │
│  RESPDecoder — RESP 响应 → 强类型结果              │
├─────────────────────────────────────────────────┤
│             连接层 (Connection Layer)              │
│  RedisConnection — TCP连接、心跳、重连机制          │
│  RedisClient — 客户端入口、连接池管理              │
└─────────────────────────────────────────────────┘
```

### 1.1 分层职责

| 层级 | 职责 | 不负责 |
|------|------|--------|
| 连接层 | TCP 套接字管理、连接生命周期、心跳保活、重连、连接池 | 协议解析、业务逻辑 |
| 协议层 | RESP 协议的编码（命令）与解码（响应） | 网络通信、命令语义 |
| 命令层 | 封装各类 Redis 命令，参数校验，调用协议层发送 | 网络细节、协议细节 |
| 应用层 | 用户直接调用的 API，按数据类型组织 | — |

### 1.2 三语言文件映射总表

| 模块 | C# (.NET) | Go | Python |
|------|-----------|-----|--------|
| 核心入口 | `RedisClient.cs` | `redisclient.go` | `client.py` |
| TCP连接管理 | `RedisConnection.cs` | `connection.go` | `connection.py` |
| RESP编码器 | `RESPEncoder.cs` | `resp_encoder.go` | `resp_encoder.py` |
| RESP解码器 | `RESPDecoder.cs` | `resp_decoder.go` | `resp_decoder.py` |
| 命令基类 | `RedisCommand.cs` | `command.go` | `command.py` |
| 字符串命令 | `StringCommands.cs` | `string_cmds.go` | `string_cmds.py` |
| 哈希命令 | `HashCommands.cs` | `hash_cmds.go` | `hash_cmds.py` |
| 列表命令 | `ListCommands.cs` | `list_cmds.go` | `list_cmds.py` |
| 集合命令 | `SetCommands.cs` | `set_cmds.go` | `set_cmds.py` |
| 有序集合命令 | `SortedSetCommands.cs` | `zset_cmds.go` | `zset_cmds.py` |
| 键命令 | `KeyCommands.cs` | `key_cmds.go` | `key_cmds.py` |
| 服务器命令 | `ServerCommands.cs` | `server_cmds.go` | `server_cmds.py` |
| 事务命令 | `TransactionCommands.cs` | `tx_cmds.go` | `tx_cmds.py` |
| 连接状态枚举 | `TcpConnectStatusEnum.cs` | 内联常量 | 枚举/常量 |
| 认证状态枚举 | `AuthenticateStatusEnum.cs` | 内联常量 | 枚举/常量 |
| 异常类型 | `Exceptions/*.cs` | `errors.go` | `exceptions.py` |
| 网络工具 | `Helper/NetworkHelper.cs` | 内联 | 内联 |

---

## 2. 连接层 (Connection Layer)

### 2.1 RedisConnection — TCP 连接管理

管理一条到 Redis 服务器的 TCP 连接，负责连接的建立、关闭、心跳保活和断线重连。

#### 字段 / 属性

| 成员 | 语言 | 类型 | 作用 |
|------|------|------|------|
| `Host` | C# 属性 / Go 字段 / Python 属性 | `string` | Redis 服务器主机名或 IP 地址 |
| `Port` | C# 属性 / Go 字段 / Python 属性 | `int` | Redis 服务器端口号 |
| `ConnectionTimeout` | C# 属性 / Go 字段 / Python 属性 | `int` (ms) | 连接超时时间，默认 5000ms |
| `ReceiveTimeout` | C# 属性 / Go 字段 / Python 属性 | `int` (ms) | 接收超时时间，默认 5000ms |
| `SendTimeout` | C# 属性 / Go 字段 / Python 属性 | `int` (ms) | 发送超时时间，默认 5000ms |
| `_tcpSocket` (`_conn`) | C# `TcpClient` / Go `net.Conn` / Python `socket` | — | 底层 TCP 套接字 |
| `_stream` | C# `NetworkStream` / Go 直接使用 conn / Python 封装 | — | 网络数据流 |
| `_connectStatus` | 语言对应枚举 | `int`/枚举 | 当前连接状态（未连接/连接中/已连接/连接失败） |
| `_isDisposed` | C# / Python `bool` | `bool` | 是否已释放资源 |
| `_lastActivityTime` | `DateTime` / `time.Time` / `datetime` | 时间戳 | 最后一次通信时间，用于心跳判断 |
| `_heartbeatInterval` | `TimeSpan` / `time.Duration` / `timedelta` | 时间间隔 | 心跳间隔，默认 30s |
| `_lock` | 语言对应的锁类型 | 锁 | 连接操作的并发安全锁 |

#### 方法

| 方法 | 语言差异 | 作用 |
|------|----------|------|
| `ConnectAsync()` | C# `async Task` / Go 同步 / Python `async def` | 建立 TCP 连接。Go 版本使用 `net.DialTimeout`，C#/Python 使用各自的异步 connect |
| `DisconnectAsync()` | 同上 | 主动关闭连接，发送 QUIT 命令，释放套接字资源 |
| `ReconnectAsync()` | 同上 | 断线重连：关闭旧连接 → 等待 → 创建新连接 → 重新认证（如有密码） |
| `SendAsync(data)` | 同上 | 发送原始字节数据到 TCP 流。加锁保证并发安全 |
| `ReceiveAsync()` | 同上 | 从 TCP 流读取原始字节响应。根据 `ReceiveTimeout` 超时 |
| `HeartbeatAsync()` | 同上 | 心跳检测：超过 `_heartbeatInterval` 未通信时发送 PING。若检测到连接断开则触发重连 |
| `GetConnectStatus()` | 三语言 | 返回当前连接状态 |

#### 设计要点

- **状态机驱动**：连接状态使用 CAS（C# `Interlocked.CompareExchange` / Go `atomic.CompareAndSwap` / Python `threading.Lock` + 条件判断）保证并发安全，防止多次同时连接
- **心跳与惰性检测结合**：定时心跳 + 每次命令发送前检测套接字状态
- **资源安全**：实现 `Dispose` / `Close` 模式，标记 `_isDisposed` 防止重复释放

---

### 2.2 RedisClient — 客户端入口与连接池

该类是用户使用类库的入口，管理到多个 Redis 服务器的连接池，采用懒加载 + 单例模式。

#### 字段 / 属性

| 成员 | 类型 | 作用 |
|------|------|------|
| `_connection` | `RedisConnection` | 当前客户端使用的 TCP 连接实例 |
| `_username` | `string` | Redis 认证用户名（Redis 6.0+ ACL 模式） |
| `_password` | `string` | Redis 认证密码 |
| `_db` | `int` | 当前选中的数据库编号（0-15） |
| `_authenticateStatus` | 枚举值 | 认证状态（未认证/认证中/已认证/认证失败） |
| `_defaultDb` | `int` | 构造函数传入的默认数据库编号 |
| `_connectionPool` | 各语言并发字典 | 连接池字典，键为 `host:port:username`，值为 `Lazy<RedisClient>` |
| `Db` (`get/set`) | `int` | 公开属性，获取/设置当前数据库（set 时内部调用 SELECT） |

#### 方法

| 方法 | 作用 |
|------|------|
| `CreateClient(host, port, username, password, dbNum)` 静态/类方法 | 工厂方法：从连接池获取或创建 RedisClient 实例。使用双重检查锁定+CAS保证单例 |
| `ConnectAsync()` | 调用 `_connection.ConnectAsync()` 建立连接。连接成功后执行认证流程 |
| `AuthenticateAsync()` | 发送 AUTH 命令进行身份验证。使用 CAS 确保仅一个协程/线程执行认证 |
| `SelectDatabaseAsync(dbNum)` | 切换数据库，发送 SELECT 命令。成功后更新 `_db` |
| `SendCommandAsync(respCommand)` | 命令发送的最终入口：调用 `_connection.SendAsync` + `ReceiveAsync` |
| `CloseAsync()` | 关闭连接并从连接池中移除自身 |
| `Dispose()` | 释放资源，从连接池移除 |

#### 连接池设计

```python
# 伪代码 — 三语言通用逻辑
_pool = ConcurrentDictionary<key, Lazy<RedisClient>>()

def CreateClient(host, port, username, password, dbNum=0):
    key = f"{host}:{port}:{username}"
    return _pool.GetOrAdd(key, lambda: Lazy(RedisClient(host, port, username, password, dbNum))).Value
```

- **键设计**：`{host}:{port}:{username}` — 同一用户到同一 Redis 实例共享一个连接
- **懒加载**：使用 `Lazy<T>` / `sync.Once` / 延迟初始化，确保首次使用时才创建
- **并发安全**：C# 用 `ConcurrentDictionary`，Go 用 `sync.Map`，Python 用 `threading.Lock` 保护

---

## 3. 协议层 (Protocol Layer)

### 3.1 RESPEncoder — 命令编码

将字符串命令编码为 RESP (REdis Serialization Protocol) 协议格式。

#### 方法

| 方法 | 输入 | 输出 | 作用 |
|------|------|------|------|
| `Encode(command)` | `string`（如 `"SET key value"`） | `string` / `byte[]` | 将空格分隔的命令转为 RESP 数组格式 |
| `EncodeArgs(args)` | `string[]` / `[]string` / `list[str]` | `string` / `byte[]` | 将参数数组直接转为 RESP 格式（避免空格分割歧义） |
| `EncodeNull()` | — | `string` / `byte[]` | 编码 RESP Null 批量字符串 `$-1\r\n` |
| `EncodeInteger(n)` | `int` / `int64` | `string` / `byte[]` | 编码 RESP 整数 |

#### RESP 协议格式速记

```
*N\r\n          — 数组，N 个元素
$L\r\n...\r\n   — 批量字符串，L 字节长度
+OK\r\n         — 简单字符串
-ERR msg\r\n    — 错误
:42\r\n         — 整数
```

#### 示例

| 输入 | 输出 |
|------|------|
| `"SET name Tom"` | `*3\r\n$3\r\nSET\r\n$4\r\nname\r\n$3\r\nTom\r\n` |
| `"AUTH user pass"` | `*3\r\n$4\r\nAUTH\r\n$4\r\nuser\r\n$4\r\npass\r\n` |

**注意**：Go 版本中现有实现 `strings.Split(key, "")` 是 bug，应改为 `strings.Split(key, " ")`。

### 3.2 RESPDecoder — 响应解码

解析 Redis 服务器返回的 RESP 协议数据，将其转换为强类型结果。

#### 方法

| 方法 | 输入 | 输出 | 作用 |
|------|------|------|------|
| `Decode(bytes)` | `byte[]` | `RedisResult` | 入口方法，根据首字节分派到具体解析器 |
| `DecodeSimpleString(bytes)` | `byte[]` | `string` | 解析 `+` 开头的简单字符串（去掉首字节和 `\r\n`） |
| `DecodeError(bytes)` | `byte[]` | `RedisError` | 解析 `-` 开头的错误信息 |
| `DecodeInteger(bytes)` | `byte[]` | `long` | 解析 `:` 开头的整数 |
| `DecodeBulkString(bytes)` | `byte[]` | `string` / `None` | 解析 `$` 开头的批量字符串，`$-1` 返回 null |
| `DecodeArray(bytes)` | `byte[]` | `RedisResult[]` | 解析 `*` 开头的数组（递归调用 Decode） |
| `DecodeMap(bytes)` | `byte[]` | `Dictionary<K,V>` | 解析 `%` 开头的 Map（Redis 7.0+ RESP3） |
| `DecodeSet(bytes)` | `byte[]` | `RedisResult[]` | 解析 `~` 开头的集合（RESP3） |
| `DecodeBool(bytes)` | `byte[]` | `bool` | 解析 `#` 开头的布尔值（RESP3） |
| `DecodeNull(bytes)` | `byte[]` | `null` | 解析 `_` 开头的 Null（RESP3） |
| `DecodeDouble(bytes)` | `byte[]` | `double` | 解析 `,` 开头的浮点数（RESP3） |
| `DecodePush(bytes)` | `byte[]` | `RedisPush` | 解析 `>` 开头的 Push 消息（RESP3，用于 Pub/Sub） |

#### RESP 首字节映射表

| 首字节 | 数据类型 | RESP 版本 |
|--------|----------|-----------|
| `+` | 简单字符串 (Simple String) | RESP2 |
| `-` | 错误 (Error) | RESP2 |
| `:` | 整数 (Integer) | RESP2 |
| `$` | 批量字符串 (Bulk String) | RESP2 |
| `*` | 数组 (Array) | RESP2 |
| `%` | Map | RESP3 |
| `~` | 集合 (Set) | RESP3 |
| `#` | 布尔值 (Boolean) | RESP3 |
| `_` | Null | RESP3 |
| `,` | 浮点数 (Double) | RESP3 |
| `>` | Push 消息 | RESP3 |

### 3.3 RedisResult — 响应结果封装

RESPDecoder 解码后的统一结果类型，提供便捷的类型转换方法。

| 成员 | 作用 |
|------|------|
| `Type` (`ResultType` 枚举) | 结果类型标识：SimpleString / Error / Integer / BulkString / Array / Map / Set / Boolean / Null / Double / Push |
| `RawValue` | 原始值（`object` / `interface{}` / `Any`） |
| `Error` | 如果类型是 Error，存储错误信息 |
| `AsString()` | 转为字符串（如果是 BulkString 或 SimpleString） |
| `AsInt64()` | 转为 64 位整数 |
| `AsDouble()` | 转为浮点数 |
| `AsBool()` | 转为布尔值 |
| `AsArray()` | 转为数组 |
| `AsMap()` | 转为字典 |
| `AsBytes()` | 转为原始字节 |
| `IsNull()` | 判断是否为 Null |
| `ThrowIfError()` | 如果是 Error 类型则抛出异常 |

---

## 4. 命令层 (Command Layer)

### 4.1 设计原则

- **按 Redis 数据类型分文件**，每个文件封装一类操作
- **命令方法命名**：`CommandName[Async]`，如 `SetAsync`、`GetAsync`
- **每个命令方法内部**：构造参数 → 调用 RESPEncoder → 调用 `SendCommandAsync` → RESPDecoder 解析结果 → 类型转换返回
- **参数校验**：在发送前对必填参数、参数类型进行校验，抛出明确异常

### 4.2 命令类的基类

三语言共用的命令基类（或接口）：

| 成员 | 作用 |
|------|------|
| `_client` / `client` | 持有 `RedisClient` 引用，用于发送命令 |
| `_encoder` | 持有 `RESPEncoder` 引用（也可通过 `_client` 间接获取） |
| `SendAsync(respCommand)` | 底层发送方法，委托给 `_client.SendCommandAsync` |

### 4.3 StringCommands — 字符串命令

对应 Redis `SET` / `GET` / `INCR` / `DECR` / `APPEND` / `STRLEN` / `MGET` / `MSET` / `GETSET` 等。

| 方法 | Redis 命令 | 作用 |
|------|-----------|------|
| `SetAsync(key, value, expiration?)` | SET | 设置键值对，可选过期时间（秒/毫秒） |
| `GetAsync(key)` | GET | 获取键对应的值，不存在返回 null |
| `GetSetAsync(key, value)` | GETSET | 设置新值并返回旧值 |
| `SetNxAsync(key, value)` | SETNX | 键不存在时设置 |
| `SetExAsync(key, seconds, value)` | SETEX | 设置键值对并指定过期秒数 |
| `PSetExAsync(key, ms, value)` | PSETEX | 设置键值对并指定过期毫秒数 |
| `MSetAsync(kvPairs)` | MSET | 批量设置多个键值对 |
| `MGetAsync(keys)` | MGET | 批量获取多个键的值 |
| `IncrAsync(key)` | INCR | 自增 1 |
| `IncrByAsync(key, increment)` | INCRBY | 自增指定数值 |
| `IncrByFloatAsync(key, increment)` | INCRBYFLOAT | 自增指定浮点数值 |
| `DecrAsync(key)` | DECR | 自减 1 |
| `DecrByAsync(key, decrement)` | DECRBY | 自减指定数值 |
| `AppendAsync(key, value)` | APPEND | 追加字符串 |
| `StrLenAsync(key)` | STRLEN | 获取字符串长度 |

### 4.4 HashCommands — 哈希命令

| 方法 | Redis 命令 | 作用 |
|------|-----------|------|
| `HSetAsync(key, field, value)` | HSET | 设置哈希表字段值 |
| `HSetNxAsync(key, field, value)` | HSETNX | 字段不存在时设置 |
| `HGetAsync(key, field)` | HGET | 获取哈希表字段值 |
| `HGetAllAsync(key)` | HGETALL | 获取所有字段和值 |
| `HDelAsync(key, fields...)` | HDEL | 删除一个或多个字段 |
| `HExistsAsync(key, field)` | HEXISTS | 判断字段是否存在 |
| `HKeysAsync(key)` | HKEYS | 获取所有字段名 |
| `HValsAsync(key)` | HVALS | 获取所有值 |
| `HLenAsync(key)` | HLEN | 获取字段数量 |
| `HStrLenAsync(key, field)` | HSTRLEN | 获取字段值的字符串长度 |
| `HIncrByAsync(key, field, increment)` | HINCRBY | 自增字段数值 |
| `HIncrByFloatAsync(key, field, increment)` | HINCRBYFLOAT | 自增字段浮点数值 |
| `HMGetAsync(key, fields...)` | HMGET | 获取多个字段的值 |
| `HRandFieldAsync(key, count?)` | HRANDFIELD | 随机获取字段 |

### 4.5 ListCommands — 列表命令

| 方法 | Redis 命令 | 作用 |
|------|-----------|------|
| `LPushAsync(key, elements...)` | LPUSH | 从左侧推入一个或多个元素 |
| `RPushAsync(key, elements...)` | RPUSH | 从右侧推入一个或多个元素 |
| `LPushXAsync(key, element)` | LPUSHX | 列表存在时从左侧推入 |
| `RPushXAsync(key, element)` | RPUSHX | 列表存在时从右侧推入 |
| `LPopAsync(key)` | LPOP | 从左侧弹出元素 |
| `RPopAsync(key)` | RPOP | 从右侧弹出元素 |
| `LRemAsync(key, count, element)` | LREM | 移除指定数量的元素 |
| `LLenAsync(key)` | LLEN | 获取列表长度 |
| `LIndexAsync(key, index)` | LINDEX | 获取指定索引的元素 |
| `LSetAsync(key, index, element)` | LSET | 设置指定索引的元素值 |
| `LInsertAsync(key, before/after, pivot, element)` | LINSERT | 在指定元素前后插入 |
| `LRangeAsync(key, start, stop)` | LRANGE | 获取指定范围的元素 |
| `RPopLPushAsync(source, destination)` | RPOPLPUSH | 弹出右侧元素并推入左侧 |
| `BLPopAsync(keys, timeout)` | BLPOP | 阻塞式左侧弹出 |
| `BRPopAsync(keys, timeout)` | BRPOP | 阻塞式右侧弹出 |
| `BRPopLPushAsync(source, dest, timeout)` | BRPOPLPUSH | 阻塞式 RPOPLPUSH |

### 4.6 SetCommands — 集合命令

| 方法 | Redis 命令 | 作用 |
|------|-----------|------|
| `SAddAsync(key, members...)` | SADD | 添加一个或多个成员 |
| `SRemAsync(key, members...)` | SREM | 移除一个或多个成员 |
| `SMembersAsync(key)` | SMEMBERS | 获取所有成员 |
| `SIsMemberAsync(key, member)` | SISMEMBER | 判断成员是否存在 |
| `SCardAsync(key)` | SCARD | 获取成员数量 |
| `SPopAsync(key, count?)` | SPOP | 随机移除并返回一个或多个成员 |
| `SRandMemberAsync(key, count?)` | SRANDMEMBER | 随机获取一个或多个成员 |
| `SMoveAsync(source, dest, member)` | SMOVE | 移动成员到另一个集合 |
| `SDiffAsync(keys...)` | SDIFF | 差集 |
| `SInterAsync(keys...)` | SINTER | 交集 |
| `SUnionAsync(keys...)` | SUNION | 并集 |
| `SDiffStoreAsync(dest, keys...)` | SDIFFSTORE | 差集并存储 |
| `SInterStoreAsync(dest, keys...)` | SINTERSTORE | 交集并存储 |
| `SUnionStoreAsync(dest, keys...)` | SUNIONSTORE | 并集并存储 |

### 4.7 SortedSetCommands — 有序集合命令

| 方法 | Redis 命令 | 作用 |
|------|-----------|------|
| `ZAddAsync(key, scoreMembers...)` | ZADD | 添加一个或多个成员（带分数） |
| `ZRemAsync(key, members...)` | ZREM | 移除一个或多个成员 |
| `ZScoreAsync(key, member)` | ZSCORE | 获取成员分数 |
| `ZIncrByAsync(key, increment, member)` | ZINCRBY | 自增成员分数 |
| `ZCardAsync(key)` | ZCARD | 获取成员数量 |
| `ZCountAsync(key, min, max)` | ZCOUNT | 统计分数区间内的成员数 |
| `ZRangeAsync(key, start, stop, withScores?)` | ZRANGE | 按索引范围获取成员 |
| `ZRangeByScoreAsync(key, min, max)` | ZRANGEBYSCORE | 按分数范围获取成员 |
| `ZRankAsync(key, member)` | ZRANK | 获取成员排名（分数从低到高） |
| `ZRevRankAsync(key, member)` | ZREVRANK | 获取成员排名（分数从高到低） |
| `ZRemRangeByRankAsync(key, start, stop)` | ZREMRANGEBYRANK | 按排名范围移除 |
| `ZRemRangeByScoreAsync(key, min, max)` | ZREMRANGEBYSCORE | 按分数范围移除 |
| `ZInterStoreAsync(dest, keys, weights?)` | ZINTERSTORE | 交集并存储 |
| `ZUnionStoreAsync(dest, keys, weights?)` | ZUNIONSTORE | 并集并存储 |

### 4.8 KeyCommands — 键命令

| 方法 | Redis 命令 | 作用 |
|------|-----------|------|
| `DelAsync(keys...)` | DEL | 删除一个或多个键 |
| `ExistsAsync(keys...)` | EXISTS | 判断键是否存在 |
| `ExpireAsync(key, seconds)` | EXPIRE | 设置过期时间（秒） |
| `ExpireAtAsync(key, timestamp)` | EXPIREAT | 设置过期时间戳（秒级） |
| `TTLAsync(key)` | TTL | 获取剩余过期时间（秒） |
| `PTTLAsync(key)` | PTTL | 获取剩余过期时间（毫秒） |
| `PersistAsync(key)` | PERSIST | 移除过期时间 |
| `TypeAsync(key)` | TYPE | 获取键的类型 |
| `RenameAsync(key, newKey)` | RENAME | 重命名键 |
| `RenameNxAsync(key, newKey)` | RENAMENX | 新键不存在时重命名 |
| `SortAsync(key, options?)` | SORT | 排序 |
| `CopyAsync(source, dest, replace?)` | COPY | 复制键 |
| `ScanAsync(cursor, pattern?, count?)` | SCAN | 增量迭代键空间 |

### 4.9 ServerCommands — 服务器命令

| 方法 | Redis 命令 | 作用 |
|------|-----------|------|
| `PingAsync()` | PING | 检测连接是否存活 |
| `SelectAsync(dbNum)` | SELECT | 切换数据库 |
| `FlushDbAsync()` | FLUSHDB | 清空当前数据库 |
| `FlushAllAsync()` | FLUSHALL | 清空所有数据库 |
| `InfoAsync(section?)` | INFO | 获取服务器信息 |
| `ConfigGetAsync(parameter)` | CONFIG GET | 获取配置参数 |
| `ConfigSetAsync(parameter, value)` | CONFIG SET | 设置配置参数 |
| `ClientListAsync()` | CLIENT LIST | 获取客户端列表 |
| `ClientKillAsync(addr/id)` | CLIENT KILL | 关闭客户端连接 |
| `DBSizeAsync()` | DBSIZE | 获取当前数据库键数量 |
| `TimeAsync()` | TIME | 获取服务器时间 |
| `CommandAsync()` | COMMAND | 获取所有命令信息 |
| `SlowLogGetAsync(n?)` | SLOWLOG GET | 获取慢查询日志 |
| `RoleAsync()` | ROLE | 获取主从角色信息 |

### 4.10 TransactionCommands — 事务与管道命令

| 方法 | Redis 命令 | 作用 |
|------|-----------|------|
| `MultiAsync()` | MULTI | 开启事务 |
| `ExecAsync()` | EXEC | 执行事务 |
| `DiscardAsync()` | DISCARD | 取消事务 |
| `WatchAsync(keys...)` | WATCH | 监视一个或多个键 |
| `UnwatchAsync()` | UNWATCH | 取消监视 |
| `PipelineAsync(commands)` | 管道（非原生命令） | 批量发送命令，减少网络往返 |

---

## 5. 枚举与常量

### 5.1 TcpConnectStatus（连接状态枚举）

| 值 | 说明 |
|----|------|
| `Disconnected = 0` | 未连接 |
| `Connecting = 1` | 连接中 |
| `Connected = 2` | 连接成功 |
| `ConnectionFailed = 3` | 连接失败 |

### 5.2 AuthenticateStatus（认证状态枚举）

| 值 | 说明 |
|----|------|
| `Unauthenticated = 0` | 未认证 |
| `Authenticating = 1` | 认证中 |
| `Authenticated = 2` | 已认证 |
| `AuthenticationFailed = 3` | 认证失败 |

### 5.3 ResultType（响应类型枚举 — RESP3 完整覆盖）

| 值 | 对应 RESP 首字节 |
|----|----------------|
| `SimpleString` | `+` |
| `Error` | `-` |
| `Integer` | `:` |
| `BulkString` | `$` |
| `Array` | `*` |
| `Map` | `%` |
| `Set` | `~` |
| `Boolean` | `#` |
| `Null` | `_` |
| `Double` | `,` |
| `Push` | `>` |

---

## 6. 异常体系 (Exceptions / Errors)

| 异常类型 | 触发场景 |
|----------|----------|
| `RedisConnectionException` | 连接失败、连接超时、连接意外断开 |
| `RedisAuthenticationException` | AUTH 认证失败、用户名或密码错误 |
| `RedisCommandException` | 发送命令时参数校验失败 |
| `RedisProtocolException` | RESP 协议解析异常、格式错误 |
| `RedisTimeoutException` | 操作超时（发送/接收超时） |
| `RedisServerException` | 服务器返回错误响应（`-ERR` 类型） |
| `RedisBusyException` | 服务器正忙（如 `-BUSY` 响应） |
| `RedisIOException` | 底层网络 IO 异常 |

---

## 7. 三语言实现对照说明

### 7.1 C# (.NET) 实现

```
SimpleRedis/
├── RedisClient.cs              — 客户端入口，连接池管理
├── RedisConnection.cs          — TCP 连接管理，心跳，重连
├── RESPEncoder.cs              — RESP 协议编码
├── RESPDecoder.cs              — RESP 协议解码
├── RedisResult.cs              — 响应结果封装
├── RedisCommand.cs             — 命令基类
├── Commands/
│   ├── StringCommands.cs
│   ├── HashCommands.cs
│   ├── ListCommands.cs
│   ├── SetCommands.cs
│   ├── SortedSetCommands.cs
│   ├── KeyCommands.cs
│   ├── ServerCommands.cs
│   └── TransactionCommands.cs
├── Enum/
│   ├── TcpConnectStatusEnum.cs
│   ├── AuthenticateStatusEnum.cs
│   └── ResultTypeEnum.cs
├── Exceptions/
│   ├── RedisConnectionException.cs
│   ├── RedisAuthenticationException.cs
│   ├── RedisCommandException.cs
│   ├── RedisProtocolException.cs
│   ├── RedisTimeoutException.cs
│   ├── RedisServerException.cs
│   └── RedisIOException.cs
└── Helper/
    └── NetworkHelper.cs        — 网络等待工具（Socket.Poll / Socket.Select）
```

**语言特性利用**：
- `async Task` / `await` 异步编程
- `ConcurrentDictionary<string, Lazy<>>` 连接池
- `Interlocked.CompareExchange` / `Volatile.Read` CAS 并发控制
- `IDisposable` 资源释放模式
- `CancellationTokenSource` 超时控制
- 泛型方法 `GetAsync<T>` 支持反序列化

### 7.2 Go 实现

```
simpleredis/
├── redisclient.go              — 客户端入口，连接池管理
├── connection.go               — TCP 连接管理，心跳，重连
├── resp_encoder.go             — RESP 协议编码
├── resp_decoder.go             — RESP 协议解码
├── result.go                   — 响应结果封装
├── command.go                  — 命令基类
├── string_cmds.go
├── hash_cmds.go
├── list_cmds.go
├── set_cmds.go
├── zset_cmds.go
├── key_cmds.go
├── server_cmds.go
├── tx_cmds.go
└── errors.go                   — 异常/错误定义
```

**语言特性利用**：
- `struct` 而非全局变量（当前版本 `redisclient.go` 使用全局变量是错误的，需要改为 struct 封装）
- `net.DialTimeout` TCP 连接
- `sync.Map` 连接池
- `sync.Once` / `sync.Mutex` / `atomic` 并发控制
- `context.Context` 超时控制
- `io.Reader` / `io.Writer` 接口抽象流操作
- `bufio.Reader` / `bufio.Writer` 缓冲读写

**当前 Go 代码存在的问题**（需修复）：
1. `redisclient.go` 使用全局变量而非 struct — 无法创建多个客户端实例
2. `TransitionCommand` 中 `strings.Split(key, "")` 错误，应改为 `strings.Split(key, " ")`
3. 缺少连接池管理、心跳、重连机制
4. `AnalysisRequest` 不支持 Bulk String 和 Array 解析

### 7.3 Python 实现

```
simpleredis/
├── __init__.py                 — 包初始化，导出公共 API
├── client.py                   — 客户端入口，连接池管理
├── connection.py               — TCP 连接管理，心跳，重连
├── resp_encoder.py             — RESP 协议编码
├── resp_decoder.py             — RESP 协议解码
├── result.py                   — 响应结果封装
├── command.py                  — 命令基类
├── string_cmds.py
├── hash_cmds.py
├── list_cmds.py
├── set_cmds.py
├── zset_cmds.py
├── key_cmds.py
├── server_cmds.py
├── tx_cmds.py
└── exceptions.py               — 自定义异常
```

**语言特性利用**：
- `socket` 模块 TCP 连接
- `asyncio` 异步 I/O（`async def` / `await`）
- `threading.Lock` 并发控制
- `functools.lru_cache` / 字典 + 锁 实现连接池
- `struct` 模块处理字节数据
- `typing` 类型注解

---

## 8. 数据流示例：一次完整的 SET 命令

以 `SET name Tom` 为例，展示数据在三层之间的流转：

```
用户调用:
  await stringCmds.SetAsync("name", "Tom")

命令层 (StringCommands.SetAsync):
  1. 参数校验: key="name" 不为空, value="Tom" 不为空
  2. 调用 _encoder.EncodeArgs(["SET", "name", "Tom"])
     返回: "*3\r\n$3\r\nSET\r\n$4\r\nname\r\n$3\r\nTom\r\n"
  3. 调用 await _client.SendCommandAsync(encodedCommand)

客户端 (RedisClient.SendCommandAsync):
  4. 检查 _connection 连接状态，断线则自动重连
  5. 调用 await _connection.SendAsync(respBytes)
  6. 调用 await _connection.ReceiveAsync()
  7. 返回原始字节: [43, 79, 75, 13, 10] 即 "+OK\r\n"

解码层 (RESPDecoder.Decode):
  8. 首字节 43 = '+', 进入 SimpleString 解析
  9. 去掉首字节和尾部 \r\n, 返回 "OK"
 10. 封装为 RedisResult { Type=SimpleString, Value="OK" }

用户得到:
  "OK" (string)
```

---

## 9. 设计要点与约束

### 9.1 并发安全

| 场景 | C# | Go | Python |
|------|-----|----|--------|
| 连接池写入 | `ConcurrentDictionary.GetOrAdd` | `sync.Map.LoadOrStore` | `Lock` + 双重检查 |
| 认证状态切换 | `Interlocked.CompareExchange` | `atomic.CompareAndSwapInt32` | `Lock` + 条件判断 |
| 连接状态切换 | 同上 | 同上 | 同上 |
| 套接字写入 | `lock` (`NetworkStream` 线程安全) | `sync.Mutex` | `Lock` |
| 心跳定时器 | `System.Threading.Timer` | `time.Ticker` | `asyncio.ensure_future` |

### 9.2 资源释放

- 所有语言版本都实现 `Dispose` / `Close` 模式
- 释放顺序：停止心跳定时器 → 关闭连接池引用 → 关闭流 → 关闭套接字
- 释放后标记 `_isDisposed = true`，后续调用直接抛出 `ObjectDisposedException`

### 9.3 超时控制

- **连接超时**：TCP 连接建立的最长等待时间（默认 5000ms）
- **发送超时**：命令写入 TCP 缓冲区的等待时间（默认 5000ms）
- **接收超时**：等待服务器响应的最长等待时间（默认 5000ms）
- **空闲超时**：超过此时间无通信则发送心跳（默认 30000ms）
- C# 使用 `CancellationTokenSource`，Go 使用 `context.WithTimeout`，Python 使用 `asyncio.wait_for`

### 9.4 可扩展性

- 添加新命令类型只需在 `Commands/` 目录下新增文件
- `RESPDecoder` 按首字节分发，新增 RESP3 类型只需添加新的 case 分支
- 连接层、协议层、命令层通过接口/抽象类解耦，任意层可独立替换

---

## 10. 当前版本问题清单

| 问题 | 文件 | 说明 |
|------|------|------|
| Go 全局变量 | `Go/simpleredis/redisclient.go` | `var conn net.Conn` 等应改为 struct 字段 |
| Go Split bug | `Go/simpleredis/redisclient.go:68` | `strings.Split(key, "")` 应改为 `strings.Split(key, " ")` |
| Go 缺少连接池 | `Go/simpleredis/redisclient.go` | 未实现客户端实例管理 |
| C# 冗余命令类 | `SimpleStringCommand.cs` | 与 `RedisCommand.cs` 功能重叠，应合并 |
| C# 未使用的字段 | `RedisClient.cs:_connectStatus` | 字段定义了但未在逻辑中使用 |
| C# 内部访问 | `RedisCommand.cs:81` | `_clien._db` 访问内部字段，应改为公开属性 |
| 认证失败状态 bug | `RedisClient.cs:146` | 认证失败时设置的是 `Authenticated` 而非 `AuthenticationFailed` |
| 缺少 Python 实现 | — | Python 版本尚未创建 |
