import AppKit
import SwiftUI
import RowCore

@MainActor final class Store: ObservableObject {
    @Published private(set) var settings=Preferences()
    @Published private(set) var items=[RowItem]()
    @Published var message="每60秒更新 · 行情与赛程可能延迟"
    @Published var refreshing=false
    @Published var page=0
    @Published var now=Date()
    @Published var history=[String:[QuotePoint]]()
    @Published var scoreChange=[String:String]()
    @Published var changedAt=[String:Date]()
    @Published var extraCatalog=[Watch]()
    @Published var screenNames=[String:String]()
    var onSettingsChanged: (() -> Void)?
    private struct Batch { var items: [RowItem]; var received: Date?; var error: String? }
    private var batches=[String:Batch]()
    private let settingsURL: URL
    init() {
        let directory=FileManager.default.urls(for:.applicationSupportDirectory,in:.userDomainMask)[0].appendingPathComponent("ROW",isDirectory:true)
        settingsURL=directory.appendingPathComponent("settings.json")
        if let data=try? Data(contentsOf:settingsURL) {
            do { settings=try JSONDecoder().decode(Preferences.self,from:data).validated() }
            catch { message="设置文件无效，使用默认关注；原文件未删除。" }
        }
    }
    @discardableResult func commit(_ next: Preferences) -> Bool {
        do {
            let validated=try next.validated()
            try FileManager.default.createDirectory(at:settingsURL.deletingLastPathComponent(),withIntermediateDirectories:true)
            try JSONEncoder().encode(validated).write(to:settingsURL,options:.atomic)
            settings=validated; onSettingsChanged?(); return true
        } catch { message="设置未保存，已保留原设置：\(error.localizedDescription)"; return false }
    }
    func change(_ edit: (inout Preferences) -> Void) { var next=settings; edit(&next); _=commit(next) }
    func add(_ watch: Watch, replacing index: Int?=nil) {
        guard !settings.watches.contains(where:{$0.id==watch.id}) else {return}
        var next=settings
        if let index=index,next.watches.indices.contains(index) { next.watches[index]=watch }
        else if next.watches.count<5 { next.watches.append(watch) } else { return }
        if commit(next) { message="已关注 \(watch.title) · 已保存"; Task{await refresh()} }
    }
    func remove(_ watch: Watch) { change{$0.watches.removeAll{$0.id==watch.id}} }
    func reorder(_ id: String, to index: Int) {
        guard let from=settings.watches.firstIndex(where:{$0.id==id}),settings.watches.indices.contains(index),from != index else {return}
        change { let watch=$0.watches.remove(at:from); $0.watches.insert(watch,at:index) }
    }
    var catalog: [Watch] {
        var list=RowLogic.seeds+extraCatalog+settings.watches+items.map(\.watch)
        for item in items where item.isSport && !item.homeID.isEmpty && !item.awayID.isEmpty { list += [item.teamWatch(home:true),item.teamWatch(home:false)] }
        list += ["A股","美股","加密","NBA","足球"].map{Watch("auto:"+$0,$0+"轮播",$0)}
        return Array(Dictionary(list.map{($0.id,$0)},uniquingKeysWith:{_,new in new}).values).sorted { left,right in
            func rank(_ w: Watch) -> Int { w.id.hasPrefix("team:") ? 0:w.id.hasPrefix("game:") ? 1:2 }
            return rank(left)==rank(right) ? left.title.localizedStandardCompare(right.title) == .orderedAscending:rank(left)<rank(right)
        }
    }
    func slot(_ index: Int) -> RowItem {
        guard index<settings.watches.count else {return RowItem("empty:\(index)","添加关注","","＋")}
        let watch=settings.watches[index]
        if watch.id.hasPrefix("team:"),let match=RowLogic.followed(watch,items:items) {return match}
        if watch.id.hasPrefix("auto:") {
            let rows=items.filter{$0.group==watch.group}.sorted{$0.isSport && $1.isSport ? ($0.rank==$1.rank ? $0.start<$1.start:$0.rank<$1.rank):$0.title<$1.title}
            if !rows.isEmpty {return rows[((page % rows.count)+rows.count)%rows.count]}
        }
        if let item=items.first(where:{$0.id==watch.id}) {return item}
        return RowItem(watch.id,watch.title,watch.group,watch.id.hasPrefix("team:") ? "暂无近场":watch.id.hasPrefix("game:") ? "赛程已过期":"等待数据")
    }
    func received(_ item: RowItem) -> String {
        guard let batch=batches.values.first(where:{$0.items.contains(where:{$0.id==item.id})}),let date=batch.received else {return "尚未接收到数据"}
        return (batch.error==nil ? "本机接收 ":"连接异常 · 最近接收 ")+RowLogic.format(date,"MM-dd HH:mm:ss")+" 北京时间"
    }
    func failed(_ item: RowItem) -> Bool { batches.values.contains{$0.error != nil && $0.items.contains(where:{$0.id==item.id})} }
    private func apply(_ key: String,_ incoming: [RowItem]?,_ error: String?) {
        let old=batches[key],date=Date()
        if let incoming=incoming {
            for item in incoming {
                let previous=old?.items.first{$0.id==item.id}
                if RowLogic.scoreChanged(previous,item,previous:old?.received,received:date,previousFailed:old?.error != nil) {scoreChange[item.id]="\(previous!.value) → \(item.value) · \(RowLogic.format(date,"HH:mm:ss")) 观察到（含修正）"; changedAt[item.id]=date}
                if let price=item.price {
                    var samples=history[item.id] ?? []
                    if item.group=="加密" || samples.last?.sourceTime != item.quoteTime {samples.append(QuotePoint(date,price,item.quoteTime)); history[item.id]=Array(samples.suffix(180))}
                }
            }
            batches[key]=Batch(items:incoming,received:date,error:nil)
        } else { batches[key]=Batch(items:old?.items ?? [],received:old?.received,error:error) }
        let flattened=batches.values.sorted{($0.received ?? .distantPast)<($1.received ?? .distantPast)}.flatMap(\.items)
        items=Array(Dictionary(flattened.map{($0.id,$0)},uniquingKeysWith:{_,new in new}).values)
        if history.count>200 {for key in history.keys.sorted().prefix(history.count-200) {history.removeValue(forKey:key)}}
    }
    struct Job { var id: String; var url: String; var parse: (Data) throws -> [RowItem] }
    private func jobs() -> [Job] {
        let watches=settings.watches
        var codes=Set(watches.filter{$0.id.hasPrefix("stock:")}.map{String($0.id.dropFirst(6))})
        for group in ["A股","美股"] where watches.contains(where:{$0.id=="auto:"+group}) {codes.formUnion(RowLogic.seeds.filter{$0.group==group}.map{String($0.id.dropFirst(6))})}
        var result=codes.sorted().map { code in Job(id:"stock:"+code,url:"https://qt.gtimg.cn/q="+code,parse:{try FeedParser.stock($0,code:code)}) }
        var coins=Set(watches.filter{$0.id.hasPrefix("coin:")}.map{String($0.id.dropFirst(5))})
        if watches.contains(where:{$0.id=="auto:加密"}) {coins.formUnion(["90","80"])}
        let ids=Array(coins).sorted(); if !ids.isEmpty {result.append(Job(id:"coins",url:"https://api.coinlore.net/api/ticker/?id="+ids.joined(separator:","),parse:{try FeedParser.coins($0,ids:ids)}))}
        var sports=[("NBA","basketball/nba","NBA")]
        sports += settings.leagues.map{("足球","soccer/"+$0,RowLogic.leagues[$0] ?? $0)}
        for (group,path,league) in sports {
            for offset in -1...1 {
                let date=Date().addingTimeInterval(Double(offset)*86400), key="\(path):\(offset)"
                result.append(Job(id:key,url:"https://site.api.espn.com/apis/site/v2/sports/\(path)/scoreboard?dates=\(RowLogic.format(date,"yyyyMMdd"))",parse:{try FeedParser.sports($0,group:group,league:league)}))
            }
        }
        return result
    }
    func refresh() async {
        guard !refreshing else {return}; refreshing=true; defer {refreshing=false}
        let jobs=jobs(),active=Set(jobs.map(\.id))
        for key in Array(batches.keys) where !active.contains(key) {batches.removeValue(forKey:key)}
        await withTaskGroup(of:(String,[RowItem]?,String?).self) { group in
            for job in jobs { group.addTask {do {return (job.id,try job.parse(await FeedClient.data(job.url)),nil)} catch {return (job.id,nil,error.localizedDescription)} } }
            for await (id,rows,error) in group {apply(id,rows,error)}
        }
        message=batches.values.contains{$0.error != nil} ? "部分数据源连接异常，保留上次快照":"已更新 · "+RowLogic.format(Date(),"HH:mm:ss")+" 北京时间"
    }
    func query(_ code: String) async {
        do {let code=try RowLogic.stockCode(code); let rows=try FeedParser.stock(await FeedClient.data("https://qt.gtimg.cn/q="+code),code:code); extraCatalog += rows.map(\.watch); apply("query:"+code,rows,nil); message="找到 \(rows.first!.title)，点击 ＋ 关注"}
        catch {message=error.localizedDescription}
    }
    func moreCoins() async {
        do {extraCatalog.removeAll{$0.group=="加密"}; let rows=try FeedParser.coinDirectory(await FeedClient.data("https://api.coinlore.net/api/assets/")); extraCatalog += rows; message="已加载 \(rows.count) 个币种，输入名称或代码搜索"}
        catch {message=error.localizedDescription}
    }
}
