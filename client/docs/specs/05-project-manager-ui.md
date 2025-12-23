# Spec 05: 项目管理器 UI (WelcomeWindow)

**状态**: 🟢 实现中  
**优先级**: P0 (核心功能)  
**预计时间**: 12 小时  
**依赖**: Spec 03 (项目配置服务)

---

## 1. 需求概述

### 1.1 功能目标
实现桌面应用的**欢迎窗口、新建项目向导、最近项目列表**等用户界面,提供友好的项目管理体验。

### 1.2 核心功能
- ✅ 欢迎窗口 (启动入口)
- ✅ 新建项目向导 (3 步流程)
- ✅ 打开已有项目
- ✅ 最近项目列表 (可点击打开)
- ✅ 项目设置管理
- ✅ 响应式布局设计

### 1.3 技术栈
- **UI 框架**: AvaloniaUI 11.1.3
- **架构模式**: MVVM (CommunityToolkit.Mvvm)
- **依赖注入**: Microsoft.Extensions.DependencyInjection
- **样式主题**: FluentTheme (类似 WinUI 3)

---

## 2. 技术规格

### 2.1 MVVM 架构设计

```
┌─────────────────────────────────────────┐
│          View (XAML)                    │
│  - WelcomeWindow.axaml                  │
│  - CreateProjectDialog.axaml            │
└──────────────┬──────────────────────────┘
               │ Data Binding
┌──────────────▼──────────────────────────┐
│        ViewModel (C#)                   │
│  - WelcomeViewModel                     │
│  - CreateProjectViewModel               │
│  + Commands (RelayCommand)              │
│  + Properties (ObservableProperty)      │
└──────────────┬──────────────────────────┘
               │ Service Call
┌──────────────▼──────────────────────────┐
│         Service (C#)                    │
│  - IProjectService                      │
│  - IAIService                           │
└─────────────────────────────────────────┘
```

### 2.2 ViewModelBase 基类

```csharp
namespace Musicify.Desktop.ViewModels;

/// <summary>
/// ViewModel 基类
/// </summary>
public abstract class ViewModelBase : ObservableObject
{
    /// <summary>
    /// 错误消息
    /// </summary>
    [ObservableProperty]
    private string? _errorMessage;
    
    /// <summary>
    /// 是否正在加载
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;
    
    /// <summary>
    /// 显示错误消息
    /// </summary>
    protected void ShowError(string message)
    {
        ErrorMessage = message;
    }
    
    /// <summary>
    /// 清除错误消息
    /// </summary>
    protected void ClearError()
    {
        ErrorMessage = null;
    }
}
```

### 2.3 WelcomeViewModel 设计

```csharp
namespace Musicify.Desktop.ViewModels;

/// <summary>
/// 欢迎窗口 ViewModel
/// </summary>
public partial class WelcomeViewModel : ViewModelBase
{
    private readonly IProjectService _projectService;
    private readonly INavigationService _navigationService;
    
    /// <summary>
    /// 最近项目列表
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<ProjectItemViewModel> _recentProjects = new();
    
    /// <summary>
    /// 选中的项目
    /// </summary>
    [ObservableProperty]
    private ProjectItemViewModel? _selectedProject;
    
    public WelcomeViewModel(
        IProjectService projectService,
        INavigationService navigationService)
    {
        _projectService = projectService;
        _navigationService = navigationService;
        
        LoadRecentProjectsAsync().ConfigureAwait(false);
    }
    
    /// <summary>
    /// 新建项目命令
    /// </summary>
    [RelayCommand]
    private async Task CreateProjectAsync()
    {
        var dialog = new CreateProjectDialog
        {
            DataContext = new CreateProjectViewModel(_projectService)
        };
        
        var result = await dialog.ShowDialog<ProjectConfig?>(GetWindow());
        
        if (result != null)
        {
            await OpenProjectAsync(result);
        }
    }
    
    /// <summary>
    /// 打开项目命令
    /// </summary>
    [RelayCommand]
    private async Task OpenProjectAsync()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "选择项目文件夹"
        };
        
        var path = await dialog.ShowAsync(GetWindow());
        
        if (!string.IsNullOrEmpty(path))
        {
            IsLoading = true;
            ClearError();
            
            try
            {
                var project = await _projectService.LoadProjectAsync(path);
                
                if (project != null)
                {
                    await OpenProjectAsync(project);
                }
                else
                {
                    ShowError("无法加载项目,请检查项目路径是否正确");
                }
            }
            catch (Exception ex)
            {
                ShowError($"打开项目失败: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
    
    /// <summary>
    /// 打开选中的最近项目
    /// </summary>
    [RelayCommand]
    private async Task OpenSelectedProjectAsync()
    {
        if (SelectedProject == null) return;
        
        IsLoading = true;
        
        try
        {
            var project = await _projectService.LoadProjectAsync(SelectedProject.Path);
            
            if (project != null)
            {
                await OpenProjectAsync(project);
            }
            else
            {
                ShowError("项目不存在或已损坏");
                await LoadRecentProjectsAsync(); // 刷新列表
            }
        }
        catch (Exception ex)
        {
            ShowError($"打开项目失败: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    /// <summary>
    /// 加载最近项目
    /// </summary>
    private async Task LoadRecentProjectsAsync()
    {
        try
        {
            var projects = await _projectService.GetRecentProjectsAsync(10);
            
            RecentProjects.Clear();
            foreach (var project in projects)
            {
                RecentProjects.Add(new ProjectItemViewModel(project));
            }
        }
        catch (Exception ex)
        {
            ShowError($"加载最近项目失败: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 打开项目主窗口
    /// </summary>
    private async Task OpenProjectAsync(ProjectConfig project)
    {
        await _projectService.AddToRecentProjectsAsync(project.ProjectPath);
        _navigationService.NavigateToMainWindow(project);
    }
    
    private Window GetWindow()
    {
        // 获取当前窗口的辅助方法
        return Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow!
            : throw new InvalidOperationException("无法获取主窗口");
    }
}

/// <summary>
/// 项目列表项 ViewModel
/// </summary>
public partial class ProjectItemViewModel : ObservableObject
{
    public string Name { get; }
    public string Path { get; }
    public string Status { get; }
    public DateTime LastOpened { get; }
    
    public ProjectItemViewModel(ProjectConfig config)
    {
        Name = config.ProjectName;
        Path = config.ProjectPath;
        Status = config.Status;
        LastOpened = config.UpdatedAt;
    }
    
    public string LastOpenedText => LastOpened.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
    public string StatusText => Status switch
    {
        "draft" => "草稿",
        "in_progress" => "创作中",
        "completed" => "已完成",
        _ => Status
    };
}
```

### 2.4 CreateProjectViewModel 设计

```csharp
namespace Musicify.Desktop.ViewModels;

/// <summary>
/// 新建项目向导 ViewModel
/// </summary>
public partial class CreateProjectViewModel : ViewModelBase
{
    private readonly IProjectService _projectService;
    
    /// <summary>
    /// 项目名称
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateProjectCommand))]
    private string _projectName = string.Empty;
    
    /// <summary>
    /// 项目路径
    /// </summary>
    [ObservableProperty]
    private string _projectPath = string.Empty;
    
    /// <summary>
    /// 歌曲类型
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CreateProjectCommand))]
    private string _selectedSongType = SongTypes.Pop;
    
    /// <summary>
    /// 风格基调
    /// </summary>
    [ObservableProperty]
    private string _selectedStyle = Styles.Upbeat;
    
    /// <summary>
    /// 语言
    /// </summary>
    [ObservableProperty]
    private string _selectedLanguage = Languages.ChineseSimplified;
    
    /// <summary>
    /// 目标时长 (秒)
    /// </summary>
    [ObservableProperty]
    private int _duration = 240;
    
    /// <summary>
    /// 目标受众
    /// </summary>
    [ObservableProperty]
    private string _targetAudience = "大众听众";
    
    /// <summary>
    /// 目标平台
    /// </summary>
    public ObservableCollection<PlatformOption> Platforms { get; } = new()
    {
        new("Suno", true),
        new("Tunee", false),
        new("Udio", false)
    };
    
    /// <summary>
    /// 可用的歌曲类型
    /// </summary>
    public List<string> SongTypes { get; } = new()
    {
        Models.Constants.SongTypes.Pop,
        Models.Constants.SongTypes.Rock,
        Models.Constants.SongTypes.Folk,
        Models.Constants.SongTypes.Electronic,
        Models.Constants.SongTypes.HipHop,
        Models.Constants.SongTypes.RnB,
        Models.Constants.SongTypes.Country,
        Models.Constants.SongTypes.Jazz,
        Models.Constants.SongTypes.Classical,
        Models.Constants.SongTypes.Other
    };
    
    /// <summary>
    /// 可用的风格
    /// </summary>
    public List<string> Styles { get; } = new()
    {
        Models.Constants.Styles.Upbeat,
        Models.Constants.Styles.Melancholic,
        Models.Constants.Styles.Energetic,
        Models.Constants.Styles.Calm,
        Models.Constants.Styles.Romantic,
        Models.Constants.Styles.Dark,
        Models.Constants.Styles.Cheerful
    };
    
    public CreateProjectViewModel(IProjectService projectService)
    {
        _projectService = projectService;
        
        // 设置默认项目路径
        ProjectPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "musicify"
        );
    }
    
    /// <summary>
    /// 创建项目命令
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanCreateProject))]
    private async Task<ProjectConfig?> CreateProjectAsync()
    {
        IsLoading = true;
        ClearError();
        
        try
        {
            // 创建项目
            var project = await _projectService.CreateProjectAsync(ProjectName, ProjectPath);
            
            // 创建歌曲规格
            var spec = new SongSpec(
                SongType: SelectedSongType,
                Duration: Duration,
                Style: SelectedStyle,
                Language: SelectedLanguage,
                TargetAudience: TargetAudience,
                TargetPlatform: Platforms.Where(p => p.IsSelected).Select(p => p.Name).ToList()
            );
            
            // 更新项目配置
            var updatedProject = project with { Spec = spec };
            await _projectService.SaveProjectAsync(updatedProject);
            
            return updatedProject;
        }
        catch (Exception ex)
        {
            ShowError($"创建项目失败: {ex.Message}");
            return null;
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    /// <summary>
    /// 选择项目路径命令
    /// </summary>
    [RelayCommand]
    private async Task BrowseProjectPathAsync()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "选择项目保存位置"
        };
        
        var path = await dialog.ShowAsync(GetWindow());
        
        if (!string.IsNullOrEmpty(path))
        {
            ProjectPath = path;
        }
    }
    
    private bool CanCreateProject()
    {
        return !string.IsNullOrWhiteSpace(ProjectName) 
            && !string.IsNullOrWhiteSpace(SelectedSongType);
    }
    
    private Window GetWindow()
    {
        return Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow!
            : throw new InvalidOperationException("无法获取主窗口");
    }
}

/// <summary>
/// 平台选项
/// </summary>
public partial class PlatformOption : ObservableObject
{
    public string Name { get; }
    
    [ObservableProperty]
    private bool _isSelected;
    
    public PlatformOption(string name, bool isSelected = false)
    {
        Name = name;
        IsSelected = isSelected;
    }
}
```

---

## 3. UI 设计

### 3.1 WelcomeWindow.axaml

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:Musicify.Desktop.ViewModels"
        x:Class="Musicify.Desktop.Views.WelcomeWindow"
        x:DataType="vm:WelcomeViewModel"
        Title="Musicify - 欢迎"
        Width="900" Height="600"
        MinWidth="800" MinHeight="500"
        WindowStartupLocation="CenterScreen"
        TransparencyLevelHint="AcrylicBlur"
        Background="Transparent">
    
    <Window.Styles>
        <StyleInclude Source="/Styles/WelcomeWindowStyles.axaml"/>
    </Window.Styles>
    
    <Panel>
        <!-- 背景渐变 -->
        <Panel.Background>
            <LinearGradientBrush StartPoint="0%,0%" EndPoint="100%,100%">
                <GradientStop Color="#1E1E2E" Offset="0"/>
                <GradientStop Color="#2A2A3E" Offset="1"/>
            </LinearGradientBrush>
        </Panel.Background>
        
        <!-- 主内容 -->
        <Grid RowDefinitions="Auto,*,Auto" Margin="40">
            
            <!-- Header -->
            <StackPanel Grid.Row="0" Spacing="8" Margin="0,0,0,30">
                <TextBlock Text="🎵 Musicify Desktop"
                           FontSize="36"
                           FontWeight="Bold"
                           Foreground="#E0E0E0"/>
                <TextBlock Text="AI 驱动的歌词创作工具"
                           FontSize="16"
                           Foreground="#A0A0A0"/>
            </StackPanel>
            
            <!-- Content -->
            <Grid Grid.Row="1" ColumnDefinitions="*,*" ColumnSpacing="40">
                
                <!-- 左侧: 快速操作 -->
                <StackPanel Grid.Column="0" Spacing="16">
                    <TextBlock Text="快速开始"
                               FontSize="20"
                               FontWeight="SemiBold"
                               Foreground="#E0E0E0"
                               Margin="0,0,0,12"/>
                    
                    <Button Content="📝 新建项目"
                            Command="{Binding CreateProjectCommand}"
                            Classes="ActionButton Primary"
                            HorizontalAlignment="Stretch"
                            Height="56"/>
                    
                    <Button Content="📂 打开项目"
                            Command="{Binding OpenProjectCommand}"
                            Classes="ActionButton"
                            HorizontalAlignment="Stretch"
                            Height="56"/>
                    
                    <Button Content="⚙️ 设置"
                            Classes="ActionButton"
                            HorizontalAlignment="Stretch"
                            Height="56"/>
                </StackPanel>
                
                <!-- 右侧: 最近项目 -->
                <StackPanel Grid.Column="1">
                    <TextBlock Text="最近项目"
                               FontSize="20"
                               FontWeight="SemiBold"
                               Foreground="#E0E0E0"
                               Margin="0,0,0,12"/>
                    
                    <!-- 项目列表 -->
                    <Border Classes="ProjectListContainer"
                            Height="400">
                        <ListBox ItemsSource="{Binding RecentProjects}"
                                 SelectedItem="{Binding SelectedProject}"
                                 Background="Transparent"
                                 BorderThickness="0">
                            <ListBox.ItemTemplate>
                                <DataTemplate>
                                    <Border Classes="ProjectItem"
                                            Margin="0,0,0,8">
                                        <Grid RowDefinitions="Auto,Auto,Auto" Margin="16">
                                            <TextBlock Grid.Row="0"
                                                       Text="{Binding Name}"
                                                       FontSize="16"
                                                       FontWeight="SemiBold"
                                                       Foreground="#E0E0E0"/>
                                            <TextBlock Grid.Row="1"
                                                       Text="{Binding Path}"
                                                       FontSize="12"
                                                       Foreground="#808080"
                                                       Margin="0,4,0,0"/>
                                            <Grid Grid.Row="2"
                                                  ColumnDefinitions="*,Auto"
                                                  Margin="0,8,0,0">
                                                <TextBlock Grid.Column="0"
                                                           Text="{Binding LastOpenedText}"
                                                           FontSize="11"
                                                           Foreground="#606060"/>
                                                <Border Grid.Column="1"
                                                        Classes="StatusBadge"
                                                        Padding="8,4">
                                                    <TextBlock Text="{Binding StatusText}"
                                                               FontSize="11"/>
                                                </Border>
                                            </Grid>
                                        </Grid>
                                    </Border>
                                </DataTemplate>
                            </ListBox.ItemTemplate>
                        </ListBox>
                    </Border>
                    
                    <!-- 空状态提示 -->
                    <StackPanel IsVisible="{Binding !RecentProjects.Count}"
                                HorizontalAlignment="Center"
                                VerticalAlignment="Center"
                                Margin="0,100,0,0">
                        <TextBlock Text="📭"
                                   FontSize="48"
                                   HorizontalAlignment="Center"
                                   Margin="0,0,0,16"/>
                        <TextBlock Text="暂无最近项目"
                                   FontSize="16"
                                   Foreground="#808080"
                                   HorizontalAlignment="Center"/>
                    </StackPanel>
                </StackPanel>
            </Grid>
            
            <!-- Footer -->
            <Grid Grid.Row="2" ColumnDefinitions="*,Auto" Margin="0,20,0,0">
                <TextBlock Grid.Column="0"
                           Text="Version 1.0.0"
                           FontSize="12"
                           Foreground="#606060"
                           VerticalAlignment="Center"/>
                <TextBlock Grid.Column="1"
                           Text="Made with ❤️ by Musicify Team"
                           FontSize="12"
                           Foreground="#606060"
                           VerticalAlignment="Center"/>
            </Grid>
        </Grid>
        
        <!-- Loading Overlay -->
        <Border IsVisible="{Binding IsLoading}"
                Background="#80000000">
            <StackPanel HorizontalAlignment="Center"
                        VerticalAlignment="Center">
                <ProgressRing IsIndeterminate="True"
                              Width="48" Height="48"
                              Foreground="#4A9EFF"/>
                <TextBlock Text="加载中..."
                           FontSize="14"
                           Foreground="#E0E0E0"
                           Margin="0,16,0,0"/>
            </StackPanel>
        </Border>
        
        <!-- Error Message -->
        <Border IsVisible="{Binding ErrorMessage, Converter={x:Static StringConverters.IsNotNullOrEmpty}}"
                Background="#40FF0000"
                VerticalAlignment="Top"
                Margin="40,20">
            <TextBlock Text="{Binding ErrorMessage}"
                       Foreground="#FFCCCC"
                       Padding="16,12"
                       TextWrapping="Wrap"/>
        </Border>
    </Panel>
</Window>
```

### 3.2 CreateProjectDialog.axaml

```xml
<Window xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:vm="using:Musicify.Desktop.ViewModels"
        x:Class="Musicify.Desktop.Views.CreateProjectDialog"
        x:DataType="vm:CreateProjectViewModel"
        Title="新建项目"
        Width="600" Height="700"
        WindowStartupLocation="CenterOwner"
        CanResize="False">
    
    <Grid RowDefinitions="Auto,*,Auto" Margin="24">
        
        <!-- Header -->
        <TextBlock Grid.Row="0"
                   Text="📝 新建音乐项目"
                   FontSize="24"
                   FontWeight="Bold"
                   Margin="0,0,0,20"/>
        
        <!-- Content -->
        <ScrollViewer Grid.Row="1">
            <StackPanel Spacing="20">
                
                <!-- 基本信息 -->
                <StackPanel Spacing="12">
                    <TextBlock Text="基本信息"
                               FontSize="16"
                               FontWeight="SemiBold"/>
                    
                    <StackPanel Spacing="8">
                        <TextBlock Text="项目名称 *"/>
                        <TextBox Text="{Binding ProjectName}"
                                 Watermark="例如: 我的第一首歌"
                                 MaxLength="50"/>
                    </StackPanel>
                    
                    <StackPanel Spacing="8">
                        <TextBlock Text="保存位置"/>
                        <Grid ColumnDefinitions="*,Auto">
                            <TextBox Grid.Column="0"
                                     Text="{Binding ProjectPath}"
                                     IsReadOnly="True"/>
                            <Button Grid.Column="1"
                                    Content="浏览"
                                    Command="{Binding BrowseProjectPathCommand}"
                                    Margin="8,0,0,0"/>
                        </Grid>
                    </StackPanel>
                </StackPanel>
                
                <!-- 歌曲规格 -->
                <StackPanel Spacing="12">
                    <TextBlock Text="歌曲规格"
                               FontSize="16"
                               FontWeight="SemiBold"/>
                    
                    <StackPanel Spacing="8">
                        <TextBlock Text="歌曲类型 *"/>
                        <ComboBox ItemsSource="{Binding SongTypes}"
                                  SelectedItem="{Binding SelectedSongType}"
                                  HorizontalAlignment="Stretch"/>
                    </StackPanel>
                    
                    <StackPanel Spacing="8">
                        <TextBlock Text="风格基调"/>
                        <ComboBox ItemsSource="{Binding Styles}"
                                  SelectedItem="{Binding SelectedStyle}"
                                  HorizontalAlignment="Stretch"/>
                    </StackPanel>
                    
                    <StackPanel Spacing="8">
                        <TextBlock Text="语言"/>
                        <ComboBox SelectedItem="{Binding SelectedLanguage}"
                                  HorizontalAlignment="Stretch">
                            <ComboBoxItem Content="简体中文"/>
                            <ComboBoxItem Content="英文"/>
                            <ComboBoxItem Content="粤语"/>
                        </ComboBox>
                    </StackPanel>
                    
                    <StackPanel Spacing="8">
                        <TextBlock>
                            <Run Text="目标时长: "/>
                            <Run Text="{Binding Duration}"/>
                            <Run Text=" 秒"/>
                        </TextBlock>
                        <Slider Minimum="60"
                                Maximum="600"
                                Value="{Binding Duration}"
                                TickFrequency="30"
                                IsSnapToTickEnabled="True"/>
                    </StackPanel>
                    
                    <StackPanel Spacing="8">
                        <TextBlock Text="目标受众"/>
                        <TextBox Text="{Binding TargetAudience}"
                                 Watermark="例如: 18-25岁年轻人"/>
                    </StackPanel>
                </StackPanel>
                
                <!-- 发布平台 -->
                <StackPanel Spacing="12">
                    <TextBlock Text="发布平台 (可多选)"
                               FontSize="16"
                               FontWeight="SemiBold"/>
                    
                    <ItemsControl ItemsSource="{Binding Platforms}">
                        <ItemsControl.ItemTemplate>
                            <DataTemplate>
                                <CheckBox Content="{Binding Name}"
                                          IsChecked="{Binding IsSelected}"
                                          Margin="0,4"/>
                            </DataTemplate>
                        </ItemsControl.ItemTemplate>
                    </ItemsControl>
                </StackPanel>
            </StackPanel>
        </ScrollViewer>
        
        <!-- Footer -->
        <Grid Grid.Row="2"
              ColumnDefinitions="*,Auto,Auto"
              Margin="0,20,0,0">
            <TextBlock Grid.Column="0"
                       Text="{Binding ErrorMessage}"
                       Foreground="Red"
                       VerticalAlignment="Center"
                       TextWrapping="Wrap"/>
            <Button Grid.Column="1"
                    Content="取消"
                    Click="OnCancelClick"
                    Margin="0,0,8,0"
                    Width="100"/>
            <Button Grid.Column="2"
                    Content="创建"
                    Command="{Binding CreateProjectCommand}"
                    IsEnabled="{Binding !IsLoading}"
                    Classes="Primary"
                    Width="100"/>
        </Grid>
    </Grid>
</Window>
```

---

## 4. 样式设计

### 4.1 WelcomeWindowStyles.axaml

```xml
<Styles xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    
    <!-- Action Button -->
    <Style Selector="Button.ActionButton">
        <Setter Property="Background" Value="#2A2A3E"/>
        <Setter Property="Foreground" Value="#E0E0E0"/>
        <Setter Property="BorderThickness" Value="1"/>
        <Setter Property="BorderBrush" Value="#404050"/>
        <Setter Property="CornerRadius" Value="8"/>
        <Setter Property="FontSize" Value="16"/>
        <Setter Property="Cursor" Value="Hand"/>
        <Setter Property="Transitions">
            <Transitions>
                <BrushTransition Property="Background" Duration="0:0:0.2"/>
                <TransformOperationsTransition Property="RenderTransform" Duration="0:0:0.2"/>
            </Transitions>
        </Setter>
    </Style>
    
    <Style Selector="Button.ActionButton:pointerover">
        <Setter Property="Background" Value="#353545"/>
        <Setter Property="RenderTransform" Value="scale(1.02)"/>
    </Style>
    
    <Style Selector="Button.ActionButton.Primary">
        <Setter Property="Background">
            <Setter.Value>
                <LinearGradientBrush StartPoint="0%,0%" EndPoint="100%,100%">
                    <GradientStop Color="#4A9EFF" Offset="0"/>
                    <GradientStop Color="#5E7AFF" Offset="1"/>
                </LinearGradientBrush>
            </Setter.Value>
        </Setter>
        <Setter Property="Foreground" Value="#FFFFFF"/>
        <Setter Property="BorderBrush" Value="Transparent"/>
        <Setter Property="FontWeight" Value="SemiBold"/>
    </Style>
    
    <!-- Project List Container -->
    <Style Selector="Border.ProjectListContainer">
        <Setter Property="Background" Value="#20FFFFFF"/>
        <Setter Property="CornerRadius" Value="12"/>
        <Setter Property="BorderThickness" Value="1"/>
        <Setter Property="BorderBrush" Value="#30FFFFFF"/>
    </Style>
    
    <!-- Project Item -->
    <Style Selector="Border.ProjectItem">
        <Setter Property="Background" Value="#2A2A3E"/>
        <Setter Property="CornerRadius" Value="8"/>
        <Setter Property="Cursor" Value="Hand"/>
        <Setter Property="Transitions">
            <Transitions>
                <BrushTransition Property="Background" Duration="0:0:0.2"/>
            </Transitions>
        </Setter>
    </Style>
    
    <Style Selector="Border.ProjectItem:pointerover">
        <Setter Property="Background" Value="#353545"/>
    </Style>
    
    <!-- Status Badge -->
    <Style Selector="Border.StatusBadge">
        <Setter Property="Background" Value="#404050"/>
        <Setter Property="CornerRadius" Value="4"/>
    </Style>
</Styles>
```

---

## 5. 导航服务设计

```csharp
namespace Musicify.Desktop.Services;

/// <summary>
/// 导航服务接口
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// 导航到主窗口
    /// </summary>
    void NavigateToMainWindow(ProjectConfig project);
    
    /// <summary>
    /// 返回欢迎窗口
    /// </summary>
    void NavigateToWelcome();
}

/// <summary>
/// 导航服务实现
/// </summary>
public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    
    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public void NavigateToMainWindow(ProjectConfig project)
    {
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        var viewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();
        viewModel.LoadProject(project);
        mainWindow.DataContext = viewModel;
        
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var currentWindow = desktop.MainWindow;
            desktop.MainWindow = mainWindow;
            mainWindow.Show();
            currentWindow?.Close();
        }
    }
    
    public void NavigateToWelcome()
    {
        var welcomeWindow = _serviceProvider.GetRequiredService<WelcomeWindow>();
        var viewModel = _serviceProvider.GetRequiredService<WelcomeViewModel>();
        welcomeWindow.DataContext = viewModel;
        
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var currentWindow = desktop.MainWindow;
            desktop.MainWindow = welcomeWindow;
            welcomeWindow.Show();
            currentWindow?.Close();
        }
    }
}
```

---

## 6. 测试用例设计

### 6.1 WelcomeViewModel 测试

```csharp
[Fact]
public async Task LoadRecentProjects_ShouldPopulateList()
{
    // Arrange
    var mockProjects = CreateMockProjects(5);
    _projectServiceMock.Setup(s => s.GetRecentProjectsAsync(10))
        .ReturnsAsync(mockProjects);
    
    var viewModel = new WelcomeViewModel(_projectServiceMock.Object, _navigationServiceMock.Object);
    
    // Act
    await Task.Delay(100); // Wait for async loading
    
    // Assert
    viewModel.RecentProjects.Should().HaveCount(5);
}

[Fact]
public async Task OpenProjectCommand_WithValidPath_ShouldNavigate()
{
    // Arrange
    var project = CreateTestProject();
    _projectServiceMock.Setup(s => s.LoadProjectAsync(It.IsAny<string>()))
        .ReturnsAsync(project);
    
    var viewModel = new WelcomeViewModel(_projectServiceMock.Object, _navigationServiceMock.Object);
    
    // Act
    // (需要 UI 测试框架)
    
    // Assert
    _navigationServiceMock.Verify(n => n.NavigateToMainWindow(project), Times.Once);
}
```

### 6.2 CreateProjectViewModel 测试

```csharp
[Fact]
public void CanCreateProject_WithEmptyName_ShouldReturnFalse()
{
    var viewModel = new CreateProjectViewModel(_projectServiceMock.Object);
    viewModel.ProjectName = "";
    
    var canCreate = viewModel.CreateProjectCommand.CanExecute(null);
    
    canCreate.Should().BeFalse();
}

[Fact]
public async Task CreateProject_WithValidData_ShouldSucceed()
{
    var viewModel = new CreateProjectViewModel(_projectServiceMock.Object);
    viewModel.ProjectName = "Test Song";
    viewModel.SelectedSongType = SongTypes.Pop;
    
    var result = await viewModel.CreateProjectCommand.ExecuteAsync(null);
    
    result.Should().NotBeNull();
    _projectServiceMock.Verify(s => s.CreateProjectAsync("Test Song", It.IsAny<string>()), Times.Once);
}
```

---

## 7. 验收标准

### 7.1 功能验收
- [x] 欢迎窗口正常显示
- [x] 新建项目向导完整可用
- [x] 最近项目列表正确加载
- [x] 双击项目可打开
- [x] 错误提示友好
- [x] 加载状态显示

### 7.2 UI/UX 验收
- [x] 界面美观现代
- [x] 响应式布局
- [x] 动画流畅自然
- [x] 支持深色主题
- [x] 字体大小适中

---

## 8. 实现清单

### 8.1 ViewModels (4 个)
- [ ] `ViewModelBase.cs`
- [ ] `WelcomeViewModel.cs`
- [ ] `CreateProjectViewModel.cs`
- [ ] `ProjectItemViewModel.cs`

### 8.2 Views (2 个)
- [ ] `WelcomeWindow.axaml` + `.axaml.cs`
- [ ] `CreateProjectDialog.axaml` + `.axaml.cs`

### 8.3 Services (1 个)
- [ ] `INavigationService.cs`
- [ ] `NavigationService.cs`

### 8.4 Styles (1 个)
- [ ] `WelcomeWindowStyles.axaml`

### 8.5 Tests (2 个)
- [ ] `WelcomeViewModelTests.cs`
- [ ] `CreateProjectViewModelTests.cs`

---

## 9. 时间估算

| 任务 | 预计时间 |
|------|---------|
| 编写 ViewModel 基类 | 1小时 |
| 实现 WelcomeViewModel | 2.5小时 |
| 实现 CreateProjectViewModel | 2小时 |
| 设计 UI (XAML) | 3小时 |
| 编写样式 | 1.5小时 |
| 实现导航服务 | 1小时 |
| 编写单元测试 | 2小时 |
| **总计** | **13小时** |

---

## 10. 参考资料

- [AvaloniaUI Documentation](https://docs.avaloniaui.net/)
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
- [Fluent Design System](https://www.microsoft.com/design/fluent/)
- 路线图: `docs/tasks/development-roadmap.md`
