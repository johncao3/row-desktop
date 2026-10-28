# 授权询问草稿（未发送）

此处仅准备文字；尚未代表维护者联系任何公司。发信前请按实际范围、预估人数和官方接收渠道补全，保留书面回复到本机 private/ 中，不把往来邮件公开提交。

## CoinLore

官方公开联系地址：contact@coinlore.com（来源：其 API 文档和条款页）。

Subject: Permission clarification for a free Windows desktop ticker using CoinLore API

Hello CoinLore team,

I maintain ROW, a Windows desktop ticker currently a publicly visible source prototype with no released binary. I plan to distribute the application free of charge through GitHub. There are no ads, paid subscriptions or server-side data resale in the proposed initial release.

Each user's device would query your `/api/assets/` catalog when requested and `/api/ticker/?id=...` as a batch of up to 30 configured IDs about once per minute. Startup and user-initiated refreshes can also trigger requests; this is a description of the current prototype, not a hard per-minute rate cap. Data would appear in a small desktop bar. We do not plan to proxy the API, provide bulk data downloads, or distribute snapshots of your data with the software. Public user count is currently unknown; the initial rollout would be small.

Your API documentation describes developer use in applications, while section 6 of the general terms restricts redistribution without written consent. Could you confirm whether this free client-side display and public distribution of the application are permitted? Please clarify attribution, permitted regions, request limits per device and across users, caching, and whether later donations or sponsorship would change the permission. We can omit your logos unless separately permitted.

Please let us know whether there are API-specific terms or an approval process. We will wait for clarification before enabling CoinLore in a public download.

Thank you,
johncao3 / ROW

## 腾讯财经 / 自选股

主题：咨询免费 Windows 桌面行情工具对腾讯行情接口的使用许可

您好，我正在开发 ROW 桌面信息栏，计划通过 GitHub 免费向公众分发。目前源码仓库已公开，实时客户端仅本人测试，尚未发布下载。

计划由用户电脑直接请求 qt.gtimg.cn 的 A股、美股、指数及部分 ETF 报价，约每60秒更新配置中的标的，每个代码分别请求，启动、自选变更或手动操作也可能触发请求；顶部显示最多五项，但配置会保留曾加入的股票代码，实际请求代码数可能超过五个。每位用户可选择关注标的，不提供交易、不集中转发行情、不打包历史数据或出售数据，初期无广告和收费。

请问此接口是否允许第三方免费客户端公开发行并展示行情？是否需要专项 API 合作、交易所终端展示许可或其他授权？请告知许可地域、用户规模、请求限制、署名、延迟标识、缓存要求，以及以后收费/赞助对许可的影响。如果该接口不适用，是否有正式可申请的替代接口？

在取得明确答复前，我们不会发布含该接口的下载。感谢指引至正确的支持/商务部门。

johncao3 / ROW

## ESPN 比分和队徽

Subject: Licensing inquiry: NBA / football scores in a free Windows desktop application

Hello ESPN team,

I maintain ROW, a Windows desktop ticker in private development. Before distributing it free of charge on GitHub, I would like to confirm the permitted data integration route.

The prototype requests NBA and football scoreboards from `site.api.espn.com/apis/site/v2/sports/.../scoreboard` on a 60-second refresh timer, plus startup or user-initiated refreshes. Each refresh makes three date requests (yesterday, today, tomorrow) per configured league. With NBA and three default football leagues this is 12 scoreboard requests per refresh; with eight configured football leagues it is 27. Team crest URLs returned by the feed may be fetched and cached in memory, with retries after failures. It displays fixtures, team names, scores and game clocks. It currently also tests team logo URLs returned by the feed. There is no video, wagering, API resale or central data proxy.

Is automated access and client-side display through this endpoint permitted for a publicly distributed free desktop app? Is a separate data license required, and do you offer a free authorized route? Please clarify attribution, regions, user-count/rate limits, caching and whether team logo display requires separate rights. We can remove logos and use text labels if necessary.

No public download has been released. We will wait for a documented permitted route before public integration.

Thank you,
johncao3 / ROW

腾讯拟收件地址：zxg@tencent.com，来自财付通官方自选股协议公告，是产品联系线索，不代表数据授权专用邮箱；公告为 2022 年资料，若未回复使用腾讯官网客服转接。ESPN 专用许可邮箱仍未核实，通过官方 Fan Support 询问转接。详情见 AUTHORIZATION_NEXT_STEPS.md。
