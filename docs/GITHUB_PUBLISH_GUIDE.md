# johncao3 的 GitHub 发布步骤

当前决定：先整理，等待数据授权；**现在不发布下载、不创建公开 Release**。建议先建立 Private 仓库保管源码与材料。

## 第一步：建立私有仓库

1. 在浏览器手动登录 GitHub 的 johncao3 账号，不把密码、验证码或访问令牌发到聊天。
2. 打开 https://github.com/new 。
3. Owner 选择 johncao3；Repository name 填 row-desktop。
4. Description 可填：Windows desktop ticker for markets and sports — release preparation。
5. 选择 **Private**。
6. 暂不添加 GitHub 生成的 README、.gitignore 或 license，这些已整理在本地；软件许可证尚未决定，不随便选 MIT。
7. 点击 Create repository。

计划地址是 `https://github.com/johncao3/row-desktop`；此文档不表示它已经创建，若重名可改名。

官方说明：https://docs.github.com/en/repositories/creating-and-managing-repositories/creating-a-new-repository

## 第二步：通过网页上传材料

本次不要求安装 GitHub CLI 或购买服务器。使用准备好的 **ROW-GitHub-preparation.zip**，解压得到 row-desktop 文件夹。

1. 打开新建仓库，选择“uploading an existing file”，或 Add file → Upload files。
2. 拖入 row-desktop **内部的文件和子目录**。不要把外层 row-desktop 文件夹整体作为仓库中的第二层根目录，也不要只上传 ZIP 文件。
3. 确认 README.md、build.ps1、source/、assets/、docs/、scripts/、.github/、release-readiness.json、.gitignore、.gitattributes 都在正确位置。Windows 隐藏文件显示可能影响点号文件的选择，可在资源管理器开启“隐藏的项目”。
4. 不上传本项目根目录、outputs/、work/、历史发布 ZIP 或自己的运行目录。网页上传**不会替你执行 .gitignore 过滤**。
5. Commit message 填 `Prepare ROW repository and data-rights review`，提交到 main。
6. 打开 README 确认显示“发布筹备中”；页面保留 Private 标记，Releases 为空。

官方上传说明：https://docs.github.com/en/repositories/working-with-files/managing-files/adding-a-file-to-a-repository

如果只想先展示介绍页而不公开源码：需要另建一个仅含介绍材料的公开仓库；当前本源码筹备仓库先保持 Private，避免在尚未决定代码许可时公开。

## 第三步：等待授权或调整数据源

逐项处理 docs/DATA_RIGHTS.md 与 docs/PERMISSION_REQUESTS.md。草稿没有发送。收到明确答复后保存在本机 private/ 目录；不要直接公开邮件、合同、联系人信息或报价。

你也可以选择移除或替换未获准的来源，再重新验证。免费软件、用户直连接口或提示“仅供学习”均不能自动替代使用许可。

## 第四步：以后准备发行包

1. 决定软件许可；更新 LICENSE-STATUS.md 与实际分发条款。
2. 确认获准来源的接口、地域、用户规模、署名、图标和收费限制。
3. 完成代码替换/移除及回归检查，更新 release-readiness.json 的证据与状态。
4. 由你确认发布后将 publicReleaseApproved 改为 true；这些记录应有事实依据。
5. 在本地运行 scripts/Prepare-Release.ps1。脚本按白名单生成 ZIP 与 SHA256SUMS.txt，不上传。

## 第五步：正式发布（目前先不做）

1. 需要向所有人提供下载时，确认最终源码可见范围和许可，再决定是否将本仓库转为 Public。若保持源码私有，可另建公开的下载/介绍仓库，不能依赖私有 Releases 让所有人下载。
2. 仓库页面 → Releases → Draft a new release。
3. Choose a tag → 新建 `v1.02`（若版本已更新用真实版本），目标为经过验证的提交。
4. 标题 `ROW 1.02 for Windows`，正文根据 docs/RELEASE_NOTES_DRAFT.md 改成最终实际功能。
5. 附件上传最终 Windows ZIP 和 SHA256SUMS.txt，**不要上传个人状态或整份本地工作目录**。
6. 先 Save draft，核对文件、哈希、授权和说明。首次测试发行可勾选 pre-release。
7. 确认后 Publish release。发布页面地址可作为下载入口，不需要单独网站或域名。

官方 Release 说明：https://docs.github.com/en/repositories/releasing-projects-on-github/managing-releases-in-a-repository

当前本机没有配置 GitHub CLI、远程地址或登录授权；本次整理未进行任何上传、在线建仓或下载发布。
