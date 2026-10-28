using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Collections.Generic;
[assembly: AssemblyTitle("ROW Installer")]
[assembly: AssemblyVersion("1.6.0.0")]
static class Installer {
    const string Identity="ROW.Desktop.1";
    const string RegistryPath=@"Software\Microsoft\Windows\CurrentVersion\Uninstall\ROW.Desktop";
    static readonly string[] Files={"ROW.exe","README.md","PRIVACY.md","THIRD_PARTY_NOTICES.md","LICENSE-STATUS.md","USER_GUIDE.md","RELEASE_STATUS.md"};
    static string DefaultRoot {get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Programs","ROW");}}
    [STAThread] static int Main(string[] args){
        Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
        try{
            if(args.Length==2&&args[0]=="/preview"){using(var f=BuildWindow()){f.Show();Application.DoEvents();using(var image=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(image,new Rectangle(0,0,f.Width,f.Height));image.Save(args[1]);}}return 0;}
            if(args.Length==2&&args[0]=="/test-install"){Install(Path.GetFullPath(args[1]),false);return 0;}
            if(args.Length==2&&args[0]=="/test-uninstall"){Uninstall(Path.GetFullPath(args[1]),false);return 0;}
            if(args.Length==1&&args[0]=="/uninstall"){
                string root=AppDomain.CurrentDomain.BaseDirectory;
                if(MessageBox.Show("卸载 ROW 1.06？请先从托盘退出 ROW。\n自选、桌面恢复备份和日志将保留。","ROW",MessageBoxButtons.OKCancel)!=DialogResult.OK)return 0;
                Uninstall(root,true);MessageBox.Show("ROW 已卸载。为保留用户数据，安装目录及当前卸载器会保留。","ROW");return 0;
            }
            Application.Run(BuildWindow());return 0;
        }catch(Exception e){if(args.Length>0)Console.Error.WriteLine(e);else MessageBox.Show("操作未完成："+e.Message,"ROW");return 1;}
    }
    static Form BuildWindow(){
        var form=new Form{Text="ROW 1.06 · 安装",ClientSize=new Size(560,330),StartPosition=FormStartPosition.CenterScreen,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,BackColor=Color.White,Font=new Font("Microsoft YaHei UI",10)};
        using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("ROW.ico"))form.Icon=new Icon(stream);
        form.Controls.Add(new Label{Text="ROW · 时刻在场",Font=new Font(form.Font.FontFamily,21),AutoSize=true,Location=new Point(28,25)});
        form.Controls.Add(new Label{Text="1.06 实时版 · 待发布安装包\n仅用于维护者验证；第三方数据与队徽公开使用范围待确认。",Bounds=new Rectangle(30,83,505,55),ForeColor=Color.DimGray});
        form.Controls.Add(new Label{Text="安装到当前用户，无需管理员权限",AutoSize=true,Location=new Point(30,151)});
        form.Controls.Add(new TextBox{Text=DefaultRoot,ReadOnly=true,Bounds=new Rectangle(30,181,500,29)});
        var launch=new CheckBox{Text="安装完成后启动 ROW",Checked=false,Bounds=new Rectangle(30,224,300,30)};form.Controls.Add(launch);
        var button=new Button{Text="安装 / 更新",Bounds=new Rectangle(366,265,164,38),BackColor=Color.FromArgb(245,246,248),FlatStyle=FlatStyle.Flat};form.Controls.Add(button);
        button.Click+=(o,e)=>{button.Enabled=false;try{Install(DefaultRoot,true);if(launch.Checked)Process.Start(Path.Combine(DefaultRoot,"ROW.exe"));MessageBox.Show("安装完成。可从桌面或开始菜单打开 ROW。","ROW");form.Close();}catch(Exception ex){MessageBox.Show("安装未完成："+ex.Message,"ROW");button.Enabled=true;}};
        return form;
    }
    static void CheckRunning(string root){
        string exe=Path.Combine(root,"ROW.exe");
        foreach(var p in Process.GetProcessesByName("ROW")){using(p){string path=null;try{path=p.MainModule.FileName;}catch{}if(path!=null&&string.Equals(path,exe,StringComparison.OrdinalIgnoreCase))throw new IOException("请先从右下角托盘退出此目录中的 ROW，再重试。");}}
    }
    static void CheckOwned(string root){
        if(!File.Exists(Path.Combine(root,"row-install.id"))||File.ReadAllText(Path.Combine(root,"row-install.id"))!=Identity)throw new IOException("安装标识不匹配，未更改此目录。");
    }
    static void Install(string root,bool integrate){
        CheckRunning(root);
        if(Directory.Exists(root)&&Directory.EnumerateFileSystemEntries(root).Any())CheckOwned(root);
        Directory.CreateDirectory(root);
        var originals=new Dictionary<string,byte[]>();var written=new List<string>();
        try{
            using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("payload.zip"))using(var zip=new ZipArchive(stream,ZipArchiveMode.Read)){
                if(zip.Entries.Count!=Files.Length||Files.Any(n=>zip.Entries.Count(e=>e.FullName==n)!=1))throw new InvalidDataException("安装包文件清单不匹配。");
                foreach(var entry in zip.Entries){string path=Path.Combine(root,entry.FullName);originals[path]=File.Exists(path)?File.ReadAllBytes(path):null;written.Add(path);using(var input=entry.Open())using(var output=File.Create(path))input.CopyTo(output);}
            }
            string marker=Path.Combine(root,"row-install.id");originals[marker]=File.Exists(marker)?File.ReadAllBytes(marker):null;written.Add(marker);File.WriteAllText(marker,Identity);
            string uninstall=Path.Combine(root,"Uninstall.exe");originals[uninstall]=File.Exists(uninstall)?File.ReadAllBytes(uninstall):null;written.Add(uninstall);File.Copy(Assembly.GetExecutingAssembly().Location,uninstall,true);
        }catch{foreach(string path in written.AsEnumerable().Reverse()){if(originals[path]==null){if(File.Exists(path))File.Delete(path);}else File.WriteAllBytes(path,originals[path]);}throw;}
        if(integrate){
            MakeShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),"ROW.lnk"),root);
            string menu=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs),"ROW");Directory.CreateDirectory(menu);MakeShortcut(Path.Combine(menu,"ROW.lnk"),root);
            using(var key=Registry.CurrentUser.CreateSubKey(RegistryPath)){key.SetValue("DisplayName","ROW 1.06");key.SetValue("DisplayVersion","1.06");key.SetValue("Publisher","johncao3");key.SetValue("InstallLocation",root);key.SetValue("DisplayIcon",Path.Combine(root,"ROW.exe")+",0");key.SetValue("UninstallString","\""+Path.Combine(root,"Uninstall.exe")+"\" /uninstall");key.SetValue("NoModify",1,RegistryValueKind.DWord);key.SetValue("NoRepair",1,RegistryValueKind.DWord);}
        }
    }
    static void MakeShortcut(string path,string root){
        dynamic shell=Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));dynamic shortcut=shell.CreateShortcut(path);shortcut.TargetPath=Path.Combine(root,"ROW.exe");shortcut.WorkingDirectory=root;shortcut.IconLocation=Path.Combine(root,"ROW.exe")+",0";shortcut.Save();
    }
    static void RemoveShortcut(string path,string root){
        if(!File.Exists(path))return;dynamic shell=Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));dynamic shortcut=shell.CreateShortcut(path);if(string.Equals((string)shortcut.TargetPath,Path.Combine(root,"ROW.exe"),StringComparison.OrdinalIgnoreCase))File.Delete(path);
    }
    static void Uninstall(string root,bool integrate){
        CheckOwned(root);CheckRunning(root);
        if(integrate){RemoveShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),"ROW.lnk"),root);RemoveShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs),"ROW","ROW.lnk"),root);
            using(var key=Registry.CurrentUser.OpenSubKey(RegistryPath)){if(key!=null&&!string.Equals((string)key.GetValue("InstallLocation"),root.TrimEnd(Path.DirectorySeparatorChar),StringComparison.OrdinalIgnoreCase))throw new IOException("卸载注册路径不匹配。");}
            Registry.CurrentUser.DeleteSubKey(RegistryPath,false);
        }
        foreach(string name in Files){string path=Path.Combine(root,name);if(File.Exists(path))File.Delete(path);}
        // Personal settings, layout recovery data, logs and this running uninstaller are retained.
    }
}
