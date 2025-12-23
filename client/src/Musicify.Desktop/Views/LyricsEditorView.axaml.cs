using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using AvaloniaEdit;
using Microsoft.Extensions.DependencyInjection;
using Musicify.Core.ViewModels;
using Musicify.Desktop;
using System.ComponentModel;

namespace Musicify.Desktop.Views;

public partial class LyricsEditorView : UserControl
{
    private TextEditor? _textEditor;
    private TextEditor? _textEditorSplit;

    public LyricsEditorView()
    {
        InitializeComponent();
        
        // 从 DI 容器获取 ViewModel
        var app = Application.Current as App;
        if (app?.Services != null)
        {
            var viewModel = app.Services.GetService<LyricsEditorViewModel>();
            if (viewModel != null)
            {
                DataContext = viewModel;
                
                // 监听 ViewModel 属性变化
                viewModel.PropertyChanged += OnViewModelPropertyChanged;
            }
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        
        // 获取 TextEditor 引用
        _textEditor = this.FindControl<TextEditor>("LyricsTextEditor");
        _textEditorSplit = this.FindControl<TextEditor>("LyricsTextEditorSplit");
        
        // 配置编辑器
        ConfigureTextEditor(_textEditor);
        ConfigureTextEditor(_textEditorSplit);
        
        // 应用语法高亮
        ApplySyntaxHighlighting();
    }

    private void ConfigureTextEditor(TextEditor? editor)
    {
        if (editor == null) return;
        
        // 配置编辑器选项
        editor.Options = new AvaloniaEdit.TextEditorOptions
        {
            ShowBoxForControlCharacters = false,
            EnableHyperlinks = false,
            EnableEmailHyperlinks = false,
            EnableRectangularSelection = true,
            EnableVirtualSpace = false,
            AllowScrollBelowDocument = false,
            IndentationSize = 4,
            ConvertTabsToSpaces = false
        };
        
        // 绑定文本内容
        if (DataContext is LyricsEditorViewModel viewModel)
        {
            editor.Text = viewModel.LyricsText;
            
            // 双向绑定文本
            editor.TextChanged += (s, e) =>
            {
                if (s is TextEditor te && DataContext is LyricsEditorViewModel vm)
                {
                    vm.LyricsText = te.Text;
                }
            };
        }
    }

    private void ApplySyntaxHighlighting()
    {
        // 注意: AvaloniaEdit 0.10.12 与 Avalonia 11.1.3 存在版本兼容性问题
        // 暂时禁用语法高亮，等待兼容版本或使用其他方案
        // TODO: 当有兼容的 AvaloniaEdit 版本时，重新启用语法高亮功能
        
        // 可以尝试使用简单的文本样式来实现段落标记高亮
        // 或者等待 AvaloniaEdit 更新到兼容版本
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is LyricsEditorViewModel viewModel)
        {
            // 当歌词文本变化时,更新编辑器内容
            if (e.PropertyName == nameof(LyricsEditorViewModel.LyricsText))
            {
                if (_textEditor != null && _textEditor.Text != viewModel.LyricsText)
                {
                    _textEditor.Text = viewModel.LyricsText;
                }
                
                if (_textEditorSplit != null && _textEditorSplit.Text != viewModel.LyricsText)
                {
                    _textEditorSplit.Text = viewModel.LyricsText;
                }
            }
        }
    }
}


