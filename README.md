<p align="center"><img src="assets/ROW.png" width="96" alt="ROW logo"></p>

# ROW · 时刻在场

**工作在手，动态在眼前。**

一条放在 Windows 桌面顶部的信息栏，让股票、币种和比赛留在视线里。五项内容自由搭配，查看价格、涨跌与比分；点击后展开详情。

## 下载 1.06 公开测试版

**[下载 Windows 安装包](https://github.com/johncao3/row-desktop/raw/refs/heads/main/downloads/ROW-1.06-Setup.exe)** · [SHA256 校验值](downloads/SHA256SUMS.txt) · [安装与使用](docs/USER_GUIDE.md) · [版本说明](docs/RELEASE_1.06.md)

Windows 10 / 11，需系统中可用的 .NET Framework 4.5 或更新版本。当前在 Windows 11 开发环境验证，其他系统尚未全面验证。安装到当前用户，无需管理员权限；未签名，系统或组织策略可能限制运行。不要为运行本软件关闭安全防护。

这是公开测试版本。第三方数据与队徽的公众客户端使用范围仍在核对，未宣称已获官方授权、合作或背书。向 CoinLore 与腾讯自选股的许可询问已经发送，尚无本项目记录中的授权确认。接口可能延迟、限流、变更或停止服务；当前按 60 秒轮询，不是逐笔或逐球推送。

## 能做什么

- A 股、美股及大盘指数、BTC/ETH 等币种、NBA 与足球，可在顶部五项中自由搭配。
- “我的关注”中搜索后点 ＋ 添加，满额才选择替换；点 − 删除，拖动 ≡ 或点 ↑ 排序，立即保存。
- 篮球各节/加时比分、分差和最近观察到的变化；足球比赛阶段及源返回的进球/红牌。
- 行情价格、涨跌、报价时间和本次启动后积累的采样走势。
- 40 DIP 单行、日夜主题、托盘控制、Ctrl+Alt+B 显示/隐藏。
- 工作区预留顶部空间，桌面图标避让与条件恢复。

[软件介绍](docs/INTRODUCTION.md) · [1.06 更新](docs/UPDATE_1.06.md) · [隐私](PRIVACY.md) · [软件使用范围](LICENSE-STATUS.md) · [数据使用范围核对](docs/DATA_RIGHTS.md)

## 安装与卸载

先从托盘退出旧版，再运行安装包。安装创建桌面和开始菜单快捷方式，并提供 Windows 卸载入口；不自动开机启动。旧便携版的自选不自动迁移，详情见使用说明。

卸载保留自选、桌面恢复备份、日志和当前卸载器。先正常退出以恢复桌面位置，确认无需恢复后可手动清理剩余目录。

## 开发与验证

```powershell
.\build.ps1
.\scripts\Test-Local.ps1
.\scripts\Build-Candidate.ps1 -PublicPreview
```

公开下载文件由 GitHub 仓库直接托管，当前未建立 GitHub Releases 条目。构建与测试不上传任何个人状态。反馈问题时请删除日志中的本地路径等个人信息，不要提交 settings.json 或 desktop-layout-backup.json。