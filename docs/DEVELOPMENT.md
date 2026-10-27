# 开发与验证

此目录按明确的源文件/文档白名单从 1.05 原型整理，未复制原型运行状态。源码仍有未完成授权核查的数据适配器；开发构建不等于发行许可。

## 编译

```powershell
.\build.ps1
.\scripts\Test-Local.ps1
```

`build.ps1` 调用 Windows 自带 .NET Framework C# 编译器，引用系统程序集，不下载 NuGet 包。系统需要可用的 .NET Framework 4.x 和 Windows 桌面环境。程序未签名。

## 测试分级

- `--self-test`、`--selection-test`：本地合成数据，测试解析、错误保留、五位置选择与 JSON 保存顺序；不访问接口、不调整桌面。
- `--interaction-test`、`--sports-layout-test`：本地界面回归和三位数比分布局；会短暂显示测试窗口，不请求网络、不移动桌面图标。
- `--detail-test <fixture目录>`：需另行准备 NBA / 足球响应 JSON，不随仓库分发真实响应；检查详情渲染及模拟价格采样。
- `--preview`：演示数据绘图，会短暂显示本程序窗口，生成图片，仅留本地。
- `--catalog-check`、`--check`、`--snapshot`：会请求第三方网络接口，仅在使用范围获准后人工运行；响应和行情截图不入库。
- `--dock-test`：短暂预留桌面工作区并调整被覆盖的桌面图标，测试恢复，再退出。应在无未完成桌面操作时人工运行。输出可能反映环境，仅留本地。

## 结构

- source/CornerBar.cs：设置模型、行情解析/请求、入口与离线检查。
- source/Bar.cs、RowUI.cs、SelectionUI.cs：主栏、面板与自选。
- source/DesktopDock.cs：AppBar 工作区。
- source/DesktopIcons.cs：桌面图标位置的条件避让与恢复。
- source/Brand.cs、assets/IconBuilder.cs：项目图标和控件样式。

桌面恢复功能会保存 Shell 项目标识。不要以此文件制作测试样本、提交 Issue 或提供给他人。

## 发布

当前没有自动上传/Release/Actions 工作流。`scripts/Prepare-Release.ps1` 读取 `release-readiness.json`，未通过时会在构建和压缩前退出。以后更新记录必须依据真实授权或已完成的替换/移除，不能只为跳过检查而改状态。
