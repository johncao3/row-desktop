import AppKit
import SwiftUI
import RowCore
import Carbon

final class FloatingPanel: NSPanel {
    override var canBecomeKey: Bool {false}
    override var canBecomeMain: Bool {false}
}
@MainActor final class AppDelegate: NSObject, NSApplicationDelegate {
    let store=Store()
    var panel: FloatingPanel!
    var status: NSStatusItem!
    var settingsWindow: NSWindow?,preferencesWindow: NSWindow?
    var refreshTimer: Timer?,clockTimer: Timer?,rotateTimer: Timer?
    var hidden=false
    var hotKey: EventHotKeyRef?
    var hotKeyHandler: EventHandlerRef?
    func applicationDidFinishLaunching(_ notification: Notification) {
        NSApp.setActivationPolicy(.accessory)
        panel=FloatingPanel(contentRect:NSRect(x:0,y:0,width:1000,height:40),styleMask:[.borderless,.nonactivatingPanel],backing:.buffered,defer:false)
        panel.level = .floating; panel.hidesOnDeactivate=false; panel.isFloatingPanel=true
        panel.collectionBehavior=[.canJoinAllSpaces,.fullScreenAuxiliary,.stationary,.ignoresCycle]
        panel.isOpaque=false;panel.hasShadow=true;panel.backgroundColor = .clear
        panel.contentView=NSHostingView(rootView:TickerView(settings:{[weak self] in self?.showSettings()}).environmentObject(store))
        store.onSettingsChanged={[weak self] in self?.applyAppearance();self?.place()}
        status=NSStatusBar.system.statusItem(withLength:NSStatusItem.squareLength)
        status.button?.image=brandImage();status.button?.toolTip="ROW · 时刻在场"
        let menu=NSMenu()
        for (title,selector,key) in [("显示 / 隐藏",#selector(toggle),""),("我的关注",#selector(showSettings),","),("偏好设置",#selector(showPreferences),""),("刷新数据",#selector(refresh),""),("退出 ROW",#selector(quit),"q")] {let item=NSMenuItem(title:title,action:selector,keyEquivalent:key);item.target=self;menu.addItem(item)}
        status.menu=menu
        NotificationCenter.default.addObserver(self,selector:#selector(place),name:NSApplication.didChangeScreenParametersNotification,object:nil)
        NSWorkspace.shared.notificationCenter.addObserver(self,selector:#selector(spaceChanged),name:NSWorkspace.activeSpaceDidChangeNotification,object:nil)
        var event=EventTypeSpec(eventClass:OSType(kEventClassKeyboard),eventKind:UInt32(kEventHotKeyPressed))
        InstallEventHandler(GetApplicationEventTarget(),{_,_,context in
            if let context=context {let app=Unmanaged<AppDelegate>.fromOpaque(context).takeUnretainedValue();Task{@MainActor in app.toggle()}}
            return noErr
        },1,&event,Unmanaged.passUnretained(self).toOpaque(),&hotKeyHandler)
        RegisterEventHotKey(UInt32(kVK_ANSI_B),UInt32(cmdKey|optionKey),EventHotKeyID(signature:0x524F5731,id:1),GetApplicationEventTarget(),0,&hotKey)
        applyAppearance();place();panel.orderFrontRegardless()
        refreshTimer=Timer.scheduledTimer(withTimeInterval:60,repeats:true){[weak self] _ in Task{@MainActor in await self?.store.refresh()}}
        clockTimer=Timer.scheduledTimer(withTimeInterval:1,repeats:true){[weak self] _ in Task{@MainActor in self?.store.now=Date()}}
        rotateTimer=Timer.scheduledTimer(withTimeInterval:12,repeats:true){[weak self] _ in Task{@MainActor in if self?.store.settings.rotate==true{self?.store.page+=1}}}
        Task{await store.refresh()}
    }
    func applyAppearance() {NSApp.appearance=store.settings.theme=="light" ? NSAppearance(named:.aqua):store.settings.theme=="dark" ? NSAppearance(named:.darkAqua):nil}
    func screenID(_ screen: NSScreen) -> String {(screen.deviceDescription[NSDeviceDescriptionKey("NSScreenNumber")] as? NSNumber)?.stringValue ?? ""}
    @objc func place() {
        store.screenNames=Dictionary(NSScreen.screens.map{(screenID($0),$0.localizedName)},uniquingKeysWith:{first,_ in first})
        guard let screen=NSScreen.screens.first(where:{screenID($0)==store.settings.screenID}) ?? NSScreen.screens.first else{return}
        let frame=screen.visibleFrame
        panel.setFrame(NSRect(x:frame.minX,y:frame.maxY-40,width:frame.width,height:40),display:true)
    }
    @objc func spaceChanged(){if !hidden{place();panel.orderFrontRegardless()}}
    @objc func toggle(){hidden.toggle();if hidden{panel.orderOut(nil)}else{place();panel.orderFrontRegardless()}}
    @objc func refresh(){Task{await store.refresh()}}
    @objc func quit(){NSApp.terminate(nil)}
    @objc func showSettings(){
        if settingsWindow==nil {settingsWindow=window("ROW · 我的关注",root:WatchlistView(preferences:{[weak self] in self?.showPreferences()}).environmentObject(store),size:NSSize(width:730,height:690))}
        NSApp.activate(ignoringOtherApps:true);settingsWindow?.makeKeyAndOrderFront(nil)
    }
    @objc func showPreferences(){
        if preferencesWindow==nil {preferencesWindow=window("ROW · 偏好设置",root:PreferencesView().environmentObject(store),size:NSSize(width:590,height:460))}
        NSApp.activate(ignoringOtherApps:true);preferencesWindow?.makeKeyAndOrderFront(nil)
    }
    func window<Content: View>(_ title: String,root: Content,size: NSSize) -> NSWindow {
        let window=NSWindow(contentRect:NSRect(origin:.zero,size:size),styleMask:[.titled,.closable,.miniaturizable],backing:.buffered,defer:false)
        window.title=title;window.isReleasedWhenClosed=false;window.contentView=NSHostingView(rootView:root);window.center();return window
    }
    func brandImage() -> NSImage {
        let image=NSImage(size:NSSize(width:22,height:22),flipped:false){_ in
            NSColor.systemOrange.setFill();NSBezierPath(ovalIn:NSRect(x:2,y:9,width:5,height:5)).fill()
            NSColor.labelColor.setStroke();let line=NSBezierPath();line.lineWidth=2;line.move(to:NSPoint(x:9,y:11.5));line.line(to:NSPoint(x:21,y:11.5));line.stroke();return true
        };return image
    }
    func applicationWillTerminate(_ notification: Notification){refreshTimer?.invalidate();clockTimer?.invalidate();rotateTimer?.invalidate();if let hotKey=hotKey{UnregisterEventHotKey(hotKey)};if let handler=hotKeyHandler{RemoveEventHandler(handler)}}
}
let application=NSApplication.shared
let delegate=AppDelegate()
application.delegate=delegate
application.run()
