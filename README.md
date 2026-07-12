# Qomicex.Core

![License](https://img.shields.io/github/license/Qomicex-Public/Qomicex.Core?style=for-the-badge)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet)
![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS-blue?style=for-the-badge)

Qomicex 启动器的跨平台核心库，用 C# 编写，封装了 Minecraft 启动、版本安装、资源管理、账户认证、日志分析等全部底层能力。以 .NET 类库形式提供，供 Qomicex 各前端（Avalonia 桌面版、Tauri 桌面版、独立 Windows 启动器等）复用。

> 本仓库是 [Qomicex-Public](https://github.com/Qomicex-Public) 组织下的独立仓库，通常以 Git 子模块的形式被其他 Qomicex 项目引用。

## 多平台支持

| 平台 | 状态 |
| -------- | ------------------ |
| Windows  | :white_check_mark: |
| macOS    | :white_check_mark: |
| Linux    | :white_check_mark: |

路径、进程、原生库（`.dll` / `.so` / `.dylib`）处理均做平台适配，平台判断统一使用 `OperatingSystem.IsWindows()` / `IsLinux()` / `IsMacOS()`。

## 安装前须知

+ Qomicex.Core 仅支持最新的 LTS .NET 10.0。
+ 使用 SkiaSharp 时，`dotnet publish` 需加 `-p:IncludeNativeLibrariesForSelfExtract=true`。

## 安装

作为源码引用（当前 Qomicex 项目采用的方式）：

* 将 Qomicex.Core 作为 Git 子模块检出，然后在你的项目中添加对 `Qomicex.Core.csproj` 的引用。

```bash
dotnet build Qomicex.Core.csproj -c Release
```

## 功能路线图

| 功能 | 状态 |
| ------------------------------------------------------------- | ------------------ |
| 离线账户模型                                                   | :white_check_mark: |
| 在线账户模型（Yggdrasil / authlib-injector）                   | :white_check_mark: |
| 在线账户模型（Microsoft OAuth）                                | :white_check_mark: |
| 在线账户模型（统一通行证 / Tongyi）                            | :white_check_mark: |
| 版本隔离                                                       | :white_check_mark: |
| 版本继承（inheritsFrom）合并                                   | :white_check_mark: |
| 旧版 Forge 安装模型                                            | :white_check_mark: |
| 新版 Forge 安装模型                                            | :white_check_mark: |
| NeoForge 安装模型                                             | :white_check_mark: |
| Fabric 安装模型                                               | :white_check_mark: |
| Quilt 安装模型                                                | :white_check_mark: |
| LiteLoader 安装模型                                           | :white_check_mark: |
| OptiFine 安装模型                                             | :white_check_mark: |
| 整合包安装（CurseForge / Modrinth）                            | :white_check_mark: |
| 资源自动补全（缺失库 / 资产 / 主 jar 检测）                     | :white_check_mark: |
| 资源中心聚合（Modrinth / CurseForge / FeedTheBeast）           | :white_check_mark: |
| Java 扫描、兼容性判断与在线下载                                | :white_check_mark: |
| 游戏配置读写（options.txt / servers.dat）                     | :white_check_mark: |
| 局域网服务器发现                                              | :white_check_mark: |
| 皮肤渲染（SkiaSharp）                                         | :white_check_mark: |
| 游戏日志与崩溃报告分析                                         | :white_check_mark: |

## 技术栈

| 项目 | 说明 |
|------|------|
| 目标框架 | `net10.0` |
| 语言特性 | 可空引用类型、隐式 using 启用 |
| JSON | `System.Text.Json`（内置） |
| TOML | `Tomlyn` 0.17.0 |
| 图像 | `SkiaSharp` 2.88.7 |
| DNS（服务器发现 / SRV） | `DnsClient` 1.8.0 |


## 核心组件

Qomicex.Core 通过若干职责单一的模块组合成完整核心框架。

| 类 | 命名空间 | 职责 |
| ---------------------------- | ---------------------- | -------------------------------------------------- |
| `Launcher`                   | `Modules.Launcher`     | 启动参数构建器：ClassPath、JVM/游戏参数拼接、Natives 解压 |
| `GeneralHelper`              | `Modules.Helpers`      | 版本搜索、加载器识别、SHA1 校验、解压等通用工具        |
| `JavaHelper`                 | `Modules.Helpers`      | 本地 Java 扫描与版本兼容性判断                        |
| `LocalResourceHelper`        | `Modules.Helpers.Resources` | 库 / 资产 / 主 jar 缺失检测，Maven 路径解析      |
| `MinecraftLogAnalyzer`       | `Modules.Helpers.LogAnalysis` | 日志与崩溃报告分析引擎                          |

各类安装器实现统一的 `IInstaller` 接口：

| 类 | 父接口 | 职责 |
| ------------------------ | ------------------ | ----------------------------------------------------- |
| `ForgeInstaller`         | `IInstaller`       | 新旧版 Forge 安装 |
| `NeoForgeInstaller`      | `IInstaller`       | NeoForge 安装 |
| `FabricInstaller`        | `IInstaller`       | Fabric 安装 |
| `QuiltInstaller`         | `IInstaller`       | Quilt 安装 |
| `LiteloaderInstaller`    | `IInstaller`       | LiteLoader 安装 |
| `OptiFineInstaller`      | `IInstaller`       | OptiFine 安装 |

## 快速开始

### Java 检测

```csharp
using Qomicex.Core.Modules.Helpers;

// 扫描系统中所有可用的 Java 安装
List<JavaHelper.JavaInfoExtended> javaList = JavaHelper.SearchJava();

// 按 Minecraft 版本获取所需 Java 大版本号
int required = JavaHelper.GetRequiredJavaMajor("1.20.1", gameDir);
```

### 版本扫描

```csharp
using Qomicex.Core.Modules.Helpers;

var helper = new GeneralHelper();
List<string> versions = helper.SearchVersionsFast(gameDir);
```

### 资源补全（缺失文件检测）

```csharp
using Qomicex.Core.Modules.Helpers.Resources;

var resource = new LocalResourceHelper();
resource.SetDownloadSource(1);

// 一次性检测某版本缺失的库、资产与主 jar
List<LocalResourceHelper.MissFileData> missing =
    await resource.GetAllMissFilesAsync("1.20.1-Forge-47.1.0", gameDir);
```

### 启动配置

```csharp
using Qomicex.Core.Modules.Launcher;
using static Qomicex.Core.DataModules;

var launcher = new Launcher();
var param = new Launcher.LauncherParam
{
    GameDir       = @"D:\.minecraft",           // .minecraft 根目录
    Version       = "1.20.1-Forge-47.1.0",      // 要启动的版本名
    MaxMemory     = "4096",                     // 最大内存 (MB)
    Width         = "854",
    Height        = "480",
    DevideVersion = true,                       // 版本隔离
    Java    = new DataDetails.Java { Path = @"C:\jdk-17\bin\javaw.exe", VersionID = 17 },
    Account = new DataDetails.Account { Name = "Steve", LoginMethod = "Legacy" },
};
```

### 定义账户模型

离线：

```csharp
param.Account = new DataDetails.Account
{
    Name        = "Steve",
    LoginMethod = "Legacy",
};
```

在线（Microsoft OAuth）：

```csharp
using Qomicex.Core.Modules.Helpers.Account;

var ms = new Microsoft(/* clientId 等 */);
var oauth   = await ms.OAuthLogin();
DataDetails.Account account = await ms.GetUserInfo(oauth.AccessToken, oauth.RefreshToken);
```

在线（Yggdrasil / 外置登录）：

```csharp
using Qomicex.Core.Modules.Helpers.Account;

var yggdrasil = new Yggdrasil(/* server, username, password */);
List<Yggdrasil.YggdrasilAccount> accounts = await yggdrasil.AuthenticateAsync();
```

### 启动！

```csharp
// 先解压 Natives，再构建启动参数
launcher.UnzipNatives(jsonPath, param.GameDir, versionPath);
string args = launcher.SelectParam(param, "Qomicex");
// args 即完整的 Java 启动参数字符串，交由进程启动即可
```

### 崩溃报告分析

```csharp
using Qomicex.Core.Modules.Helpers.LogAnalysis;

var analyzer = new MinecraftLogAnalyzer();
string? latest = MinecraftLogAnalyzer.GetLatestCrashReport(gameDir);

var result = await analyzer.AnalyzeAsync(latest!);
if (result.IsSuccess)
{
    Console.WriteLine(MinecraftLogAnalyzer.GenerateSummary(result.Value));
    var solution = MinecraftLogAnalyzer.GetPrimarySolution(result.Value);
}
```

## 核心概念

### Result 结果类型

`Common/Result.cs` 提供函数式错误处理（Railway-Oriented Programming），用于替代预期错误场景下的异常，支持 `Map` / `Bind` / `Match` / `Tap` / `GetValueOr` 等组合子：

```csharp
using Qomicex.Core.Common;

string summary = (await analyzer.AnalyzeAsync(path)).Match(
    onSuccess: r   => MinecraftLogAnalyzer.GenerateSummary(r),
    onFailure: err => $"分析失败: {err.Message}");
```

### 路径体系与版本隔离

- **GameDir**：`.minecraft` 根目录，存放 `versions`、`assets`、`libraries`、`logs` 等共享目录。
- **VersionDir**：`GameDir/versions/{版本名}/`，存放该版本的 JSON、jar、原生库。
- 开启版本隔离（`DevideVersion = true`）时，`mods`、`saves`、`resourcepacks` 等目录落在 `VersionDir` 之下；否则位于 GameDir 根。

启动器始终以 `GameDir` 作为路径构造的基准。

## 目录结构

```
Qomicex.Core/
├─ Common/Result.cs                 # Result<T> / Result<T,E> 结果类型
├─ DataModules.cs                   # 核心数据模型（Account / Java / Launcher / Version …）
├─ Modules/
│  ├─ Launcher/Launcher.cs          # 启动参数构建器
│  └─ Helpers/
│     ├─ GeneralHelper.cs           # 通用工具
│     ├─ JavaHelper.cs              # Java 扫描与兼容性
│     ├─ GameVersion.cs             # 从 jar 识别游戏版本
│     ├─ AccountHelper.cs           # 离线 UUID 生成
│     ├─ SystemInfoHelper.cs        # 系统信息 / ClassPath 分隔符
│     ├─ Account/                   # Microsoft / Yggdrasil / Tongyi 认证
│     ├─ GameSettings/              # options.txt 与 servers.dat 读写
│     ├─ Installers/                # 加载器与整合包安装器
│     ├─ LogAnalysis/               # 日志与崩溃报告分析引擎
│     ├─ MultiPlatforms/            # 平台相关能力（内存等）
│     └─ Resources/                 # 资源解析与在线资源（Modrinth/CurseForge/FTB/Local）
└─ Resources/                       # 嵌入资源（启动包装器、日志规则库）
```

## 许可证

本项目基于 [GNU General Public License v3.0](./LICENSE) 授权。

## 免责声明

Qomicex.Core 与 Mojang 及其任何软件无关联。
