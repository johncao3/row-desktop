import SwiftUI
import UniformTypeIdentifiers
import RowCore

struct BrandView: View {
    var body: some View { HStack(spacing:3){Circle().fill(.orange).frame(width:5,height:5);Rectangle().fill(.primary).frame(width:17,height:2)}.padding(7) }
}
struct TickerView: View {
    @EnvironmentObject var store: Store
    var settings: () -> Void
    var body: some View {
        GeometryReader { geometry in
            HStack(spacing:0) {
                Button(action:settings){BrandView()}.buttonStyle(.plain).frame(width:42).help("ROW · 我的关注")
                ForEach(0..<5) { index in
                    Rectangle().fill(Color.primary.opacity(0.2)).frame(width:1,height:19)
                    TickerSlot(item:store.slot(index),width:max(70,(geometry.size.width-76)/5),settings:settings)
                        .frame(maxWidth:.infinity,maxHeight:.infinity)
                }
                Button(action:settings){Image(systemName:"ellipsis")}.buttonStyle(.plain).frame(width:28).help("我的关注")
            }.frame(height:40).background(Color(nsColor:.windowBackgroundColor))
        }.frame(height:40)
    }
}
struct TickerSlot: View {
    @EnvironmentObject var store: Store
    let item: RowItem
    var width: CGFloat
    var settings: () -> Void
    @State private var details=false
    private var color: Color {store.settings.quiet ? .primary:(item.change ?? 0)>=0 ? .red:.green}
    var body: some View {
        Button {if item.id.hasPrefix("empty:"){settings()}else{details.toggle()}} label:{
            HStack(spacing:6) {
                if item.isSport {
                    crest(item.homeLogo,item.homeID).help(item.home)
                    Text(item.value).font(.system(size:width<210 ? 13:15,weight:.semibold,design:.monospaced)).fixedSize()
                    crest(item.awayLogo,item.awayID).help(item.away)
                    if width>190 {Text(item.status).font(.system(size:11)).foregroundStyle(.secondary).lineLimit(1).minimumScaleFactor(0.75)}
                } else {
                    Text(item.group=="A股" ? "A":item.group=="美股" ? "US":item.group=="加密" ? "₿":"＋")
                        .font(.system(size:9,weight:.bold)).padding(4).background(Color.primary.opacity(0.06),in:RoundedRectangle(cornerRadius:4))
                    Text(item.title).font(.system(size:12)).lineLimit(1).minimumScaleFactor(0.7)
                    Text(item.value).font(.system(size:13,weight:.medium,design:.monospaced)).lineLimit(1).minimumScaleFactor(0.65)
                    if let change=item.change {Text(String(format:"%+.2f%%",change)).font(.system(size:11,design:.monospaced)).foregroundStyle(color).lineLimit(1).minimumScaleFactor(0.7)}
                }
                if store.failed(item) {Circle().fill(.orange).frame(width:4,height:4)}
            }.padding(.horizontal,7).frame(maxWidth:.infinity,maxHeight:.infinity).contentShape(Rectangle())
                .overlay(alignment:.bottom){if let date=store.changedAt[item.id],store.now.timeIntervalSince(date)<12{Rectangle().fill(.orange).frame(height:2).padding(.horizontal,15)}}
        }.buttonStyle(.plain).help(item.title+" · "+item.value+" · "+item.status)
            .popover(isPresented:$details){DetailView(original:item).environmentObject(store)}
    }
    @ViewBuilder private func crest(_ url: String,_ id: String) -> some View {
        let link=url.isEmpty ? "https://a.espncdn.com/i/teamlogos/\(item.group=="NBA" ? "nba":"soccer")/500/\(id).png":url
        AsyncImage(url:URL(string:link)){phase in
            if let image=phase.image {image.resizable().scaledToFit()}else{Image(systemName:item.group=="NBA" ? "basketball":"soccerball").resizable().scaledToFit().foregroundStyle(.secondary)}
        }.frame(width:20,height:20)
    }
}
struct WatchlistView: View {
    @EnvironmentObject var store: Store
    var preferences: () -> Void
    @State private var query=""
    @State private var filter="全部"
    @State private var replacement: Watch?
    @State private var busy=false
    private var results: [Watch] {store.catalog.filter{watch in
        let category=filter=="全部" || (filter=="行情" && ["A股","美股","加密"].contains(watch.group)) || (filter=="篮球" && watch.group=="NBA") || (filter=="足球" && watch.group=="足球")
        return category && (query.isEmpty || "\(watch.title) \(watch.id) \(watch.group)".localizedCaseInsensitiveContains(query))
    }}
    var body: some View {
        VStack(alignment:.leading,spacing:14) {
            HStack{BrandView();Text("我的关注").font(.title2.bold());Spacer();Button("偏好设置",action:preferences)}
            Text("列表顺序对应顶部五项 · 修改立即保存").foregroundStyle(.secondary)
            HStack{Text("我的关注").font(.headline);Spacer();Text("\(store.settings.watches.count) / 5").foregroundStyle(.secondary)}
            VStack(spacing:4) {
                if store.settings.watches.isEmpty {Text("从下方搜索并点击 ＋，添加第一项。").foregroundStyle(.secondary).frame(maxWidth:.infinity,minHeight:90)}
                ForEach(Array(store.settings.watches.enumerated()),id:\.element.id) { index,watch in
                    HStack{Text("≡").foregroundStyle(.secondary);Text(watch.title+(watch.id.hasPrefix("team:") ? " · 跟随球队":""));Spacer();Button("↑"){store.reorder(watch.id,to:index-1)}.disabled(index==0);Button("−"){store.remove(watch)}}
                        .padding(8).background(Color.primary.opacity(0.04),in:RoundedRectangle(cornerRadius:6))
                        .onDrag{NSItemProvider(object:("ROWWatch:"+watch.id) as NSString)}
                        .onDrop(of:[UTType.text],isTargeted:nil){providers in
                            guard let provider=providers.first else{return false}
                            _=provider.loadObject(ofClass:NSString.self){object,_ in if let text=object as? String,text.hasPrefix("ROWWatch:"){DispatchQueue.main.async{store.reorder(String(text.dropFirst(9)),to:index)}}};return true
                        }
                }
            }.frame(minHeight:150,alignment:.top)
            HStack{TextField("搜索股票、币种、球队或比赛",text:$query).textFieldStyle(.roundedBorder);Button("查询代码"){run{await store.query(query)}}.disabled(busy)}
            HStack{Picker("分类",selection:$filter){ForEach(["全部","行情","篮球","足球"],id:\.self){Text($0)}}.pickerStyle(.segmented);Button("刷新比赛"){run{await store.refresh()}}.disabled(store.refreshing || busy);Button("更多币种"){run{await store.moreCoins()}}.disabled(busy)}
            ScrollView {
                LazyVStack(spacing:4) {
                    ForEach(Array(results.prefix(80))) { watch in
                        HStack {
                            VStack(alignment:.leading,spacing:3){Text(watch.title);Text(watch.group+(watch.id.hasPrefix("team:") ? " · 跟随球队":" · "+watch.id)).font(.caption).foregroundStyle(.secondary)}
                            Spacer();let selected=store.settings.watches.contains(where:{$0.id==watch.id})
                            Button(selected ? "✓":"＋"){if store.settings.watches.count==5{replacement=watch}else{store.add(watch)}}.disabled(selected)
                        }.padding(9).background(Color.primary.opacity(0.04),in:RoundedRectangle(cornerRadius:6))
                    }
                    if results.isEmpty {Text("暂无结果。股票可查询代码；币种可加载更多；球队来自启用联赛的近三日赛程。").foregroundStyle(.secondary).padding()}
                    if results.count>80 {Text("已显示前80项，请输入名称缩小范围").foregroundStyle(.secondary)}
                }
            }.frame(minHeight:160)
            Text(store.message).font(.caption).foregroundStyle(.secondary).lineLimit(2)
        }.padding(24).frame(minWidth:680,minHeight:650)
            .confirmationDialog("已满5项，替换哪一项？",isPresented:Binding(get:{replacement != nil},set:{if !$0{replacement=nil}}),titleVisibility:.visible){
                ForEach(Array(store.settings.watches.enumerated()),id:\.element.id){index,watch in Button(watch.title){if let pending=replacement{store.add(pending,replacing:index)};replacement=nil}}
                Button("取消",role:.cancel){replacement=nil}
            }
    }
    private func run(_ operation: @escaping () async -> Void) {busy=true;Task{await operation();busy=false}}
}
struct PreferencesView: View {
    @EnvironmentObject var store: Store
    private func binding<T>(_ path: WritableKeyPath<Preferences,T>) -> Binding<T> {Binding(get:{store.settings[keyPath:path]},set:{value in store.change{$0[keyPath:path]=value}})}
    var body: some View {
        VStack(alignment:.leading,spacing:18) {
            HStack{BrandView();Text("偏好设置").font(.title2.bold())}
            Picker("外观",selection:binding(\.theme)){Text("跟随系统").tag("auto");Text("浅色").tag("light");Text("深色").tag("dark")}
            Picker("显示屏幕",selection:binding(\.screenID)){Text("主屏幕").tag("");ForEach(store.screenNames.keys.sorted(),id:\.self){id in Text(store.screenNames[id] ?? id).tag(id)}}
            Toggle("分类自动轮播",isOn:binding(\.rotate));Toggle("单色涨跌",isOn:binding(\.quiet))
            Text("足球联赛").font(.headline)
            LazyVGrid(columns:[GridItem(.flexible()),GridItem(.flexible()),GridItem(.flexible())]) {
                ForEach(RowLogic.leagues.keys.sorted(),id:\.self){key in Toggle(RowLogic.leagues[key] ?? key,isOn:Binding(get:{store.settings.leagues.contains(key)},set:{enabled in store.change{if enabled{$0.leagues.append(key)}else{$0.leagues.removeAll{$0==key}}};Task{await store.refresh()}}))}
            }
            Divider();Text("置顶条位于系统菜单栏下方，不占用刘海区。\n本版不自动移动其他软件窗口，也不申请辅助功能或录屏权限。").font(.callout).foregroundStyle(.secondary)
            Text("每60秒轮询 · 原生 Mac 预览版 · 第三方数据使用范围仍在核对").font(.caption).foregroundStyle(.secondary)
        }.padding(24).frame(width:540)
    }
}
struct DetailView: View {
    @EnvironmentObject var store: Store
    let original: RowItem
    private var current: RowItem {store.items.first{$0.id==original.id} ?? original}
    var body: some View {
        VStack(alignment:.leading,spacing:12) {
            HStack{Text(current.title).font(.headline);Spacer();Text(current.group).foregroundStyle(.secondary)}
            Text(current.value+(current.change.map{String(format:"  %+.2f%%",$0)} ?? "")).font(.system(size:25,weight:.semibold,design:.monospaced))
            Text(current.phase.isEmpty ? current.details:current.phase).foregroundStyle(.secondary)
            if current.isSport {ScrollView{Text(current.details).frame(maxWidth:.infinity,alignment:.leading)}.frame(maxHeight:140);Text(store.scoreChange[current.id] ?? "本次运行尚未观察到比分变化").font(.caption)}
            else {Text("源报价时间 · "+current.quoteTime).font(.caption);PriceChart(points:store.history[current.id] ?? []).frame(height:100);Text("本次运行采样，最多180点 · 非完整日内分时图").font(.caption).foregroundStyle(.secondary)}
            Divider();Text(store.received(current)).font(.caption);Text(RowLogic.quoteAge(current)).font(.caption).foregroundStyle(.secondary);Text("来源 · "+current.source).font(.caption).foregroundStyle(.secondary)
        }.padding(20).frame(width:440)
    }
}
struct PriceChart: View {
    let points: [QuotePoint]
    var body: some View {
        Canvas {context,size in
            guard points.count>1,let first=points.first,let last=points.last,let low=points.map(\.price).min(),let high=points.map(\.price).max() else{return}
            let span=max(0.00000001,high-low),duration=max(1,last.date.timeIntervalSince(first.date))
            func position(_ p: QuotePoint) -> CGPoint {CGPoint(x:8+p.date.timeIntervalSince(first.date)/duration*(size.width-16),y:8+(high-p.price)/span*(size.height-16))}
            for index in 1..<points.count where points[index].date.timeIntervalSince(points[index-1].date)<=150 {var path=Path();path.move(to:position(points[index-1]));path.addLine(to:position(points[index]));context.stroke(path,with:.color(.orange),lineWidth:1.5)}
        }.background(Color.primary.opacity(0.035),in:RoundedRectangle(cornerRadius:6))
            .overlay{if points.count<2{Text("至少两次有效报价后显示走势").font(.caption).foregroundStyle(.secondary)}}
    }
}
