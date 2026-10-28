import Foundation
import CoreFoundation

public enum FeedParser {
    static func dict(_ x: Any?) -> [String:Any] { x as? [String:Any] ?? [:] }
    static func array(_ x: Any?) -> [Any] { x as? [Any] ?? [] }
    static func string(_ x: Any?) -> String { guard let x=x, !(x is NSNull) else { return "" }; return String(describing:x) }
    static func number(_ x: Any?) -> Double? { Double(string(x)) }
    static func json(_ data: Data) throws -> Any { try JSONSerialization.jsonObject(with:data) }
    public static func stock(_ data: Data, code: String) throws -> [RowItem] {
        let encoding=String.Encoding(rawValue:CFStringConvertEncodingToNSStringEncoding(CFStringEncoding(CFStringEncodings.GB_18030_2000.rawValue)))
        guard let text=String(data:data,encoding:encoding) ?? String(data:data,encoding:.utf8),let first=text.firstIndex(of:"\""),let last=text.lastIndex(of:"\""),first<last else { throw RowError.invalid("行情响应无效") }
        let fields=text[text.index(after:first)..<last].components(separatedBy:"~")
        guard fields.count>32,!fields[1].isEmpty,let price=Double(fields[3]),price>0,let change=Double(fields[32]) else { throw RowError.invalid("行情字段缺失") }
        let us=code.hasPrefix("us"), index=code.hasPrefix("us.") || ["sh000001","sz399001","sz399006"].contains(code)
        var item=RowItem("stock:"+code.lowercased(),fields[1],us ? "美股":"A股",(index ? "":us ? "$":"¥")+String(format:"%.2f",price),"腾讯行情")
        item.price=price; item.change=change; item.quoteTime=fields[30]; item.details=(index ? "指数点数":code.uppercased())+" · 较前收盘"; return [item]
    }
    public static func coins(_ data: Data, ids: [String]) throws -> [RowItem] {
        guard let values=try json(data) as? [[String:Any]] else { throw RowError.invalid("币价响应无效") }
        var result=[RowItem]()
        for value in values where ids.contains(string(value["id"])) {
            guard let price=number(value["price_usd"]),price>0,let change=number(value["percent_change_24h"]) else { throw RowError.invalid("币价字段缺失") }
            var item=RowItem("coin:"+string(value["id"]),string(value["symbol"]),"加密","$"+(price<1 ? String(format:"%.8g",price):String(format:"%.2f",price)),"CoinLore")
            item.price=price; item.change=change; item.quoteTime="未提供源报价时刻"; item.details="USD · 过去24小时"; result.append(item)
        }
        guard Set(result.map{$0.id}).count==Set(ids).count else { throw RowError.invalid("币价数据不完整") }; return result
    }
    public static func coinDirectory(_ data: Data) throws -> [Watch] {
        let root=dict(try json(data)); let list=array(root["data"])
        let result=list.compactMap { row -> Watch? in let d=dict(row), id=string(d["id"]); guard !id.isEmpty,id.allSatisfy(\.isNumber) else { return nil }; return Watch("coin:"+id,string(d["symbol"])+" · "+string(d["name"]),"加密") }
        guard !result.isEmpty else { throw RowError.invalid("币种目录为空或响应无效") }; return result
    }
    static func teamName(_ competitor: [String:Any]) -> String {
        let name=string(dict(competitor["team"])["displayName"]); return TeamNames.chinese[name] ?? name
    }
    public static func sports(_ data: Data, group: String, league: String) throws -> [RowItem] {
        let root=dict(try json(data)); guard root["events"] is [Any] else { throw RowError.invalid("比分响应缺少 events") }
        return array(root["events"]).compactMap { raw -> RowItem? in
            let event=dict(raw), competition=dict(array(event["competitions"]).first), teams=array(competition["competitors"]).map(dict)
            guard teams.count==2 else { return nil }
            let h=teams.first{string($0["homeAway"])=="home"} ?? teams[0], a=teams.first{string($0["homeAway"])=="away"} ?? teams[1]
            let status=dict(event["status"]), type=dict(status["type"]), state=string(type["state"])
            var item=RowItem("game:\(group):\(string(event["id"]))",teamName(h)+" · "+teamName(a),group,state=="pre" ? "VS":string(h["score"])+" : "+string(a["score"]),"ESPN")
            item.home=teamName(h); item.away=teamName(a); item.homeID=string(dict(h["team"])["id"]); item.awayID=string(dict(a["team"])["id"])
            item.homeLogo=string(dict(h["team"])["logo"]); item.awayLogo=string(dict(a["team"])["logo"])
            item.league=league; item.rank=state=="in" ? 0:state=="pre" ? 1:2
            if let date=ISO8601DateFormatter().date(from:string(event["date"])) { item.start=date; item.day=RowLogic.day(date) }
            let halftime=string(type["name"]).contains("HALFTIME")
            item.status=state=="in" ? (halftime ? "中场":(group=="NBA" ? "Q"+string(status["period"])+" ":"")+string(status["displayClock"])) : state=="post" ? "完场":item.start==Date.distantFuture ? "待定":RowLogic.format(item.start,"MM-dd HH:mm")
            item.phase=string(type["shortDetail"]); item.quoteTime=item.start==Date.distantFuture ? "开赛时间待定":"开赛 "+RowLogic.format(item.start,"MM-dd HH:mm")+" 北京时间"
            if group=="NBA" {
                let hp=array(h["linescores"]).map(dict),ap=array(a["linescores"]).map(dict)
                let periods=Set((hp+ap).compactMap{Int(string($0["period"]))}).sorted()
                func score(_ list: [[String:Any]],_ n: Int) -> String { guard let d=list.first(where:{Int(string($0["period"]))==n}) else {return "—"}; return string(d["displayValue"] ?? d["value"]) }
                let margin: String
                if state=="pre" { margin="尚未开赛" } else if let hs=Int(string(h["score"])),let ascore=Int(string(a["score"])) { margin=hs==ascore ? "双方战平":(hs>ascore ? "主队领先 ":"客队领先 ")+String(abs(hs-ascore))+" 分" } else { margin="分差暂无数据" }
                item.details=margin+"\n\n"+(periods.isEmpty ? "各节比分：接口暂无数据":periods.map{($0<=4 ? "Q\($0)":"OT\($0-4)")+"  \(score(hp,$0)) : \(score(ap,$0))"}.joined(separator:"    "))
            } else if competition["details"] == nil { item.details="进球 / 红牌：接口暂无事件数据" }
            else {
                let events=array(competition["details"]).compactMap { raw -> String? in
                    let e=dict(raw),red=e["redCard"] as? Bool ?? false,goal=e["scoringPlay"] as? Bool ?? false
                    guard red || goal else { return nil }
                    let teamID=string(dict(e["team"])["id"]),team=teamID==item.homeID ? item.home:teamID==item.awayID ? item.away:"球队未注明"
                    let kind=red ? "红牌":e["ownGoal"] as? Bool==true ? "乌龙球":e["penaltyKick"] as? Bool==true ? "点球进球":"进球"
                    let player=array(e["athletesInvolved"]).map{string(dict($0)["displayName"])}.joined(separator:" / ")
                    return string(dict(e["clock"])["displayValue"])+"  "+kind+" · "+team+" "+player
                }
                item.details=events.isEmpty ? "接口当前未返回进球或红牌事件":events.joined(separator:"\n")
            }
            return item
        }.sorted { $0.rank==$1.rank ? $0.start<$1.start:$0.rank<$1.rank }
    }
}

public enum FeedClient {
    public static func data(_ url: String) async throws -> Data {
        guard let url=URL(string:url) else { throw RowError.invalid("无效地址") }
        var request=URLRequest(url:url); request.timeoutInterval=16; request.setValue("ROW/1.06 macOS (desktop ticker)",forHTTPHeaderField:"User-Agent")
        let (data,response)=try await URLSession.shared.data(for:request)
        guard let response=response as? HTTPURLResponse,(200..<300).contains(response.statusCode) else { throw RowError.invalid("数据源请求失败") }; return data
    }
}
