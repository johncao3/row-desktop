import Foundation

public struct Watch: Codable, Identifiable, Equatable, Hashable {
    public var id: String
    public var title: String
    public var group: String
    public init(_ id: String, _ title: String, _ group: String) { self.id=id; self.title=title; self.group=group }
}
public struct Preferences: Codable, Equatable {
    public var watches: [Watch] = [Watch("stock:sh000001","上证指数","A股"),Watch("stock:us.inx","标普500","美股"),Watch("coin:90","BTC","加密"),Watch("auto:NBA","NBA 轮播","NBA"),Watch("auto:足球","足球轮播","足球")]
    public var leagues = ["eng.1","esp.1","uefa.champions"]
    public var theme = "auto"
    public var screenID = ""
    public var rotate = true
    public var quiet = false
    public init() {}
    public func validated() throws -> Preferences {
        guard watches.count <= 5, Set(watches.map { $0.id.lowercased() }).count == watches.count,
              watches.allSatisfy({ !$0.id.isEmpty && !$0.group.isEmpty }), leagues.count <= 8,
              leagues.allSatisfy({ $0.range(of: "^[a-z0-9.-]+$", options: .regularExpression) != nil }),
              ["auto","light","dark"].contains(theme) else { throw RowError.invalid("设置内容无效") }
        return self
    }
}
public enum RowError: LocalizedError {
    case invalid(String)
    public var errorDescription: String? { if case .invalid(let message)=self { return message }; return nil }
}
public struct RowItem: Identifiable, Equatable {
    public var id: String, title: String, group: String, value: String, source: String
    public var quoteTime="", status="", phase="", details="", league="", day=""
    public var home="", away="", homeID="", awayID="", homeLogo="", awayLogo=""
    public var rank=1
    public var start=Date.distantFuture
    public var price: Double?, change: Double?
    public init(_ id: String,_ title: String,_ group: String,_ value: String,_ source: String="") {
        self.id=id; self.title=title; self.group=group; self.value=value; self.source=source
    }
    public var isSport: Bool { !home.isEmpty }
    public var watch: Watch { Watch(id,title,group) }
    public func teamWatch(home isHome: Bool) -> Watch { Watch("team:\(group):\(league):\(isHome ? homeID : awayID)",isHome ? home : away,group) }
}
public struct QuotePoint: Identifiable {
    public let id=UUID()
    public var date: Date, price: Double, sourceTime: String
    public init(_ date: Date,_ price: Double,_ sourceTime: String) { self.date=date; self.price=price; self.sourceTime=sourceTime }
}
public enum RowLogic {
    public static func stockCode(_ input: String) throws -> String {
        let s=input.trimmingCharacters(in:.whitespacesAndNewlines).lowercased()
        if s.range(of:"^(sh|sz|bj)[0-9]{6}$",options:.regularExpression) != nil { return s }
        if s.range(of:"^[0-9]{6}$",options:.regularExpression) != nil { return (s.hasPrefix("6") || s.hasPrefix("5") || s.hasPrefix("9") ? "sh" : s.hasPrefix("4") || s.hasPrefix("8") ? "bj" : "sz")+s }
        let ticker=s.hasPrefix("us") ? String(s.dropFirst(2)) : s
        if ticker.range(of:"^[a-z.][a-z0-9.]{0,14}$",options:.regularExpression) != nil { return "us"+ticker }
        throw RowError.invalid("输入 A 股代码或美股代码，例如 600519、sz000001、AAPL")
    }
    public static func followed(_ watch: Watch, items: [RowItem], now: Date=Date()) -> RowItem? {
        let games=items.filter { $0.isSport && ($0.teamWatch(home:true).id==watch.id || $0.teamWatch(home:false).id==watch.id) }
        return games.filter{$0.rank==0}.min{$0.start<$1.start}
            ?? games.filter{$0.rank==2 && $0.day==day(now)}.max{$0.start<$1.start}
            ?? games.filter{$0.rank==1}.min{$0.start<$1.start}
    }
    public static func scoreChanged(_ old: RowItem?,_ item: RowItem, previous: Date?, received: Date, previousFailed: Bool) -> Bool {
        guard let old=old, let previous=previous else { return false }
        return !previousFailed && old.id==item.id && old.isSport && old.rank==0 && (item.rank==0 || item.rank==2) && received>=previous && received.timeIntervalSince(previous)<=150 && old.value != item.value
    }
    public static func day(_ date: Date) -> String { format(date,"yyyy-MM-dd") }
    public static func format(_ date: Date,_ pattern: String) -> String {
        let f=DateFormatter(); f.locale=Locale(identifier:"en_US_POSIX"); f.timeZone=TimeZone(identifier:"Asia/Shanghai"); f.dateFormat=pattern; return f.string(from:date)
    }
    public static func quoteAge(_ item: RowItem, now: Date=Date()) -> String {
        guard item.group=="A股" || item.group=="美股" else { return item.isSport ? "接口比分快照，时间不会在本机模拟前进。" : "接口未提供源报价时刻，无法判断源数据延迟。" }
        let f=DateFormatter(); f.locale=Locale(identifier:"en_US_POSIX"); f.timeZone=TimeZone(identifier:item.group=="美股" ? "America/New_York":"Asia/Shanghai")
        var stamp: Date?
        for pattern in ["yyyyMMddHHmmss","yyyy-MM-dd HH:mm:ss"] { f.dateFormat=pattern; if let date=f.date(from:item.quoteTime) { stamp=date; break } }
        guard let stamp=stamp else { return "无法解析源报价时间，不标记为实时。" }
        let seconds=now.timeIntervalSince(stamp)
        if seconds < -60 { return "源时间晚于本机时间，请检查时钟。" }
        return "报价距今 \(max(0,Int(seconds/60))) 分钟（含休市时间，不等同于接口延迟）。"
    }
    public static var seeds: [Watch] {
        [Watch("stock:sh000001","上证指数","A股"),Watch("stock:sz399001","深证成指","A股"),Watch("stock:sz399006","创业板指","A股"),Watch("stock:sh600519","贵州茅台","A股"),Watch("stock:sz300750","宁德时代","A股"),Watch("stock:us.inx","标普500","美股"),Watch("stock:us.ixic","纳斯达克","美股"),Watch("stock:us.dji","道琼斯","美股"),Watch("stock:usaapl","苹果","美股"),Watch("stock:usmsft","微软","美股"),Watch("stock:usnvda","英伟达","美股"),Watch("stock:ustsla","特斯拉","美股"),Watch("coin:90","BTC · Bitcoin","加密"),Watch("coin:80","ETH · Ethereum","加密")]
    }
    public static let leagues=["eng.1":"英超","esp.1":"西甲","uefa.champions":"欧冠","ita.1":"意甲","ger.1":"德甲","fra.1":"法甲","chn.1":"中超"]
}
