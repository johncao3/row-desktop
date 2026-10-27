# 授权询问草稿（未发送）

此处仅准备文字；尚未代表维护者联系任何公司。发信前请按实际范围、预估人数和官方接收渠道补全，保留书面回复到本机 private/ 中，不把往来邮件公开提交。

## CoinLore

官方公开联系地址：contact@coinlore.com（来源：其 API 文档和条款页）。

Subject: Permission clarification for a free Windows desktop ticker using CoinLore API

Hello CoinLore team,

I maintain ROW, a Windows desktop ticker currently in private prototyping. I plan to distribute the application free of charge through GitHub. There are no ads, paid subscriptions or server-side data resale in the proposed initial release.

Each user's device would query your `/api/assets/` catalog when requested and `/api/ticker/?id=...` about once per minute for selected assets. Data would appear in a small desktop bar. We do not plan to proxy the API, provide bulk data downloads, or distribute snapshots of your data with the software. Public user count is currently unknown; the initial rollout would be small.

Your API documentation describes developer use in applications, while section 6 of the general terms restricts redistribution without written consent. Could you confirm whether this free client-side display and public distribution of the application are permitted? Please clarify attribution, permitted regions, request limits per device and across users, caching, and whether later donations or sponsorship would change the permission. We can omit your logos unless separately permitted.

Please let us know whether there are API-specific terms or an approval process. We will wait for clarification before enabling CoinLore in a public download.

Thank you,
johncao3 / ROW

## 腾讯财经 / 自选股

主题：咨询免费 Windows 桌面行情工具对腾讯行情接口的使用许可

您好，我正在开发 ROW 桌面信息栏，计划通过 GitHub 免费向公众分发。目前仅为本人测试，尚未发布下载。

计划由用户电脑直接请求 qt.gtimg.cn 的 A股、美股、指数及部分 ETF 报价，约每60秒更新；每位用户选择少量标的，不提供交易、不集中转发行情、不打包历史数据或出售数据，初期无广告和收费。

请问此接口是否允许第三方免费客户端公开发行并展示行情？是否需要专项 API 合作、交易所终端展示许可或其他授权？请告知许可地域、用户规模、请求限制、署名、延迟标识、缓存要求，以及以后收费/赞助对许可的影响。如果该接口不适用，是否有正式可申请的替代接口？

在取得明确答复前，我们不会发布含该接口的下载。感谢指引至正确的支持/商务部门。

johncao3 / ROW

## ESPN 比分和队徽

Subject: Licensing inquiry: NBA / football scores in a free Windows desktop application

Hello ESPN team,

I maintain ROW, a Windows desktop ticker in private development. Before distributing it free of charge on GitHub, I would like to confirm the permitted data integration route.

The prototype requests NBA and football scoreboards from `site.api.espn.com/apis/site/v2/sports/.../scoreboard` about once per minute per client, for a small date range. It displays fixtures, team names, scores and game clocks. It currently also tests team logo URLs returned by the feed. There is no video, wagering, API resale or central data proxy.

Is automated access and client-side display through this endpoint permitted for a publicly distributed free desktop app? Is a separate data license required, and do you offer a free authorized route? Please clarify attribution, regions, user-count/rate limits, caching and whether team logo display requires separate rights. We can remove logos and use text labels if necessary.

No public download has been released. We will wait for a documented permitted route before public integration.

Thank you,
johncao3 / ROW

ESPN 与腾讯的接收地址尚未核实，不使用猜测邮箱；通过各自官方支持页面询问转接数据授权部门。
