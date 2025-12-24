# 图标转换指南

## 问题
应用程序需要 ICO 格式的图标文件，但当前只有 PNG 格式。

## 解决方案

### 方法 1：在线转换
1. 访问 https://convertio.co/png-ico/ 或 https://favicon.io/favicon-converter/
2. 上传 `client/src/Musicify.Desktop/Assets/app-icon.png`
3. 下载转换后的 ICO 文件
4. 将其保存为 `client/src/Musicify.Desktop/Assets/app-icon.ico`

### 方法 2：使用 ImageMagick（如果已安装）
```bash
# 在 macOS 上安装 ImageMagick
brew install imagemagick

# 转换图标
convert client/src/Musicify.Desktop/Assets/app-icon.png client/src/Musicify.Desktop/Assets/app-icon.ico
```

### 方法 3：使用 GIMP
1. 在 GIMP 中打开 PNG 文件
2. 导出为 ICO 格式
3. 保存到 Assets 目录

## 完成后
在项目文件中添加图标引用：
```xml
<ApplicationIcon>Assets/app-icon.ico</ApplicationIcon>
```
