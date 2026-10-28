# ROW 1.06 · macOS 原生移植预览

目标：macOS 13+，Apple Silicon 与 Intel 通用应用。Swift / SwiftUI / AppKit，无第三方代码依赖。

## 当前实现范围

- 菜单栏下方40点单行悬浮条，普通窗口上方，适配显示器；浅深色跟随系统或手动设置。
- 五项关注，自由混搭 A股、美股/指数、币种、NBA 与足球；搜索、添加、满额替换、删除补位、拖放/上移排序、即时保存。
- 股票代码查询、更多币种目录、球队跟随近三日赛程、比赛阶段、节比分、分差、足球事件、采样走势图。
- 菜单栏入口；Option+Command+B 全局隐藏/显示；源报价与本机接收时间区分。
- 60秒轮询，失败保留上次快照；重连/旧基线不生成比分变化提示。

这是原生移植，不是将 Windows EXE 转换。已在 GitHub macOS 15.7.9 / Xcode 16.4 环境通过编译、四项核心测试、arm64/x86_64 通用程序检查和 ad-hoc 签名完整性验证；尚未完成真实 Mac 桌面交互验收，不宣称已与 Windows 完全一致。

## 与 Windows 的区别

普通置顶不申请辅助功能、录屏或管理员权限。悬浮条位于系统菜单栏下方，使用屏幕可见区域避开刘海与 Dock。不修改系统菜单栏。

macOS 没有在此实现 Windows AppBar 式的全局工作区预留。本版会覆盖其他窗口顶部40点区域；可以隐藏，也可手动将工作窗口下移。本版不自动移动其他应用或桌面图标。跨桌面/全屏辅助显示已设置系统行为，但 Stage Manager、全屏应用及多显示器需要实机验证，不能保证覆盖所有界面。

设置保存在 ~/Library/Application Support/ROW/settings.json。与 Windows 配置分别存储，不自动迁移或修改 Windows 设置。

## 下载与安装

[下载 Mac 原生预览 ZIP](https://github.com/johncao3/row-desktop/raw/refs/heads/main/downloads/ROW-1.06-macOS-preview.zip) · [校验值](../downloads/SHA256SUMS.txt) · [成功构建记录](https://github.com/johncao3/row-desktop/actions/runs/40628998339)

在 Mac 上解压，将 ROW.app 拖入“应用程序”（或个人的 Applications 目录），打开后从系统菜单栏 ROW 图标进入设置。Option+Command+B 隐藏/显示顶部栏。第一次试用请检查窗口位置、五项关注与休眠唤醒。

这是未获 Developer ID 签名及苹果公证的测试应用，仅有 ad-hoc 签名。若你确认下载源和校验值可信，可按苹果指引，在尝试打开后进入“系统设置 → 隐私与安全性 → 仍要打开”，仅对该应用作出选择；组织管理策略可能不允许。不要关闭整个 Gatekeeper 防护。[苹果说明](https://support.apple.com/en-gb/102445)

卸载前从菜单栏退出，然后把 ROW.app 移到废纸篓；个人设置留在应用支持目录中，需清除时再自行处理。

## 在 Mac 构建

安装 Xcode Command Line Tools 后，在终端进入本目录执行：

```sh
bash build-macos.sh
```

脚本先运行合成数据测试，再编译 arm64 与 x86_64、合并通用程序，生成 ROW.app 和 ZIP。只做本机 ad-hoc 签名，不代表 Apple Developer ID 签名或苹果公证。不会上传、发布或申请任何付费服务。

开发运行可用 `swift run ROW`。正式发行应使用 Developer ID 签名并公证；不上 Mac App Store 也可以通过 GitHub 分发。尚未公证的测试应用可能被 Gatekeeper 拦截，可信应用的逐项放行按苹果支持文档处理，不建议关闭整个安全防护。

## 实机验收

检查：首次启动、联网、五项增删与重排、保存失败提示、退至菜单栏、快捷键、原生设置与详情、刘海/外接显示器、切换桌面与全屏、休眠唤醒、退出与重新启动。核心逻辑测试不请求网络、不操控桌面。

数据与队徽仍使用 Windows 原型相同提供方；现有公开测试版不代表已获第三方许可或官方背书。代码许可沿用主仓库，不另行授予第三方素材权利。
