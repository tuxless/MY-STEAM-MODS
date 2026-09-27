# MY-STEAM-MODS
我所制作的steam创意工坊mods
从 Mega Crit 官方发布页 下载 ModUploader-win-x64.zip，解压，找到 ModUploader.exe。GitHub
下载我更新的 workshop.json，覆盖你最后一次构建所用项目里的 dist\SpireDraft-beta\workshop.json。这份说明标明了 Beta v0.111.0 的测试状态，默认可见性是 private。你也可以先在 dist\SpireDraft-beta\content\SpireDraft.json 中把 author 改成自己的名字。
保持 Steam 登录，双击该项目根目录的 Upload.cmd。输入 beta；随后粘贴 ModUploader.exe 的完整路径，例如 D:\ModUploader\ModUploader.exe。不用先双击上传器创建 NewModWorkspace，我们的 dist\SpireDraft-beta 已是工作区。官方命令使用的是工作区目录 upload -w <目录>。megacrit/sts2-mod-uploader · GitHub
成功后，检查 dist\SpireDraft-beta\mod_id.txt 是否出现，保留整个工作区和这个文件；下次运行 Upload.cmd 会更新同一个工坊项目。若提示接受 Steam 创意工坊协议，按上传器给出的链接接受后重试。megacrit/sts2-mod-uploader · GitHub
先查看私密工坊页面的标题、封面和 RitsuLib 依赖。准备公开时，把同一工作区  的 "visibility": "private" 改为 "public"，再运行一次 Upload.cmd、输入 beta。
