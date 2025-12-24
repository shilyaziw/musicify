using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Musicify.Core.ViewModels;

namespace Musicify.Desktop.Views;

public partial class LyricsEditorView : UserControl
{
    private TextBox? _textEditor;
    private TextBox? _textEditorSplit;

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

        // 获取 TextBox 引用
        _textEditor = this.FindControl<TextBox>("LyricsTextEditor");
        _textEditorSplit = this.FindControl<TextBox>("LyricsTextEditorSplit");

        // 配置编辑器
        ConfigureTextBox(_textEditor);
        ConfigureTextBox(_textEditorSplit);
    }

    private void ConfigureTextBox(TextBox? textBox)
    {
        if (textBox == null)
        {
            return;
        }

        // 绑定文本内容
        if (DataContext is LyricsEditorViewModel viewModel)
        {
            textBox.Text = viewModel.LyricsText;

            // 双向绑定文本
            textBox.TextChanged += (s, e) =>
            {
                if (s is TextBox tb && DataContext is LyricsEditorViewModel vm)
                {
                    vm.LyricsText = tb.Text ?? string.Empty;
                }
            };
        }
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
