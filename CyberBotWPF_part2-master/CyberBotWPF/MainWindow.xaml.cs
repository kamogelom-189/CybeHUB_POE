using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace CyberBotWPF;

public partial class MainWindow : Window
{
    private readonly string _username;
    private readonly ResponseEngine _engine;
    private readonly ChatMemory _memory;

    private static readonly SolidColorBrush BrushBotBubble = new(Color.FromRgb(0x16, 0x1B, 0x22));
    private static readonly SolidColorBrush BrushUserBubble = new(Color.FromRgb(0x1C, 0x43, 0x6B));
    private static readonly SolidColorBrush BrushCyan = new(Color.FromRgb(0x00, 0xE5, 0xFF));
    private static readonly SolidColorBrush BrushYellow = new(Color.FromRgb(0xE3, 0xB3, 0x41));
    private static readonly SolidColorBrush BrushMuted = new(Color.FromRgb(0x8B, 0x94, 0x9E));
    private static readonly SolidColorBrush BrushBorder = new(Color.FromRgb(0x30, 0x36, 0x3D));

    private DispatcherTimer? _typeTimer;
    private string _pendingText = "";
    private int _typeIndex;
    private TextBlock? _typeTarget;

    // Required parameterless constructor fallback preventing WPF runtime launch crashes
    public MainWindow() : this("Guest")
    {
    }

    public MainWindow(string username)
    {
        InitializeComponent();

        _username = username;
        _memory = new ChatMemory(username);
        _engine = new ResponseEngine(_memory);

        TitleLabel.Text = $"CyberBot  ·  Hi, {username}!";

        TopicButtons.ItemsSource = new[]
        {
            "🔐 Passwords", "🎣 Phishing", "🦠 Malware",
            "🌐 VPN", "🔑 2FA", "💾 Backups",
            "📋 Tips", "❓ Help"
        };

        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        AddBotMessage($"Welcome back, {_username}! 🛡 I'm CyberBot, your cybersecurity companion.");
        AddBotMessage("Ask me about passwords, phishing, malware, VPNs, 2FA, backups — or type 'help'.");
        UpdateMemoryPanel();
        InputBox.Focus();
    }

    private void InputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) ProcessInput();
    }

    private void Send_Click(object sender, RoutedEventArgs e) => ProcessInput();

    private void TopicButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
        {
            string raw = btn.Content?.ToString() ?? "";
            int spaceIdx = raw.IndexOf(' ');
            InputBox.Text = spaceIdx >= 0 ? raw.Substring(spaceIdx + 1) : raw;
            ProcessInput();
        }
    }

    private void ProcessInput()
    {
        string text = InputBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(text)) return;

        InputBox.Clear();
        AddUserMessage(text);

        var result = _engine.GetResponse(text);

        if (result.IsExit)
        {
            AddBotMessage($"Stay safe out there, {_username}! 🛡 CyberBot signing off.");
            SendButton.IsEnabled = false;
            InputBox.IsEnabled = false;
            return;
        }

        if (result.IsHelp)
        {
            ShowHelp();
            return;
        }

        if (result.SentimentAck is not null)
            AddBotMessage(result.SentimentAck);

        AddBotMessageAnimated(result.Response);

        if (result.Tip is not null)
        {
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                AddBotMessage(result.Tip);
            };
            timer.Start();
        }

        UpdateMemoryPanel();
        ScrollToBottom();
    }

    private void AddUserMessage(string text)
    {
        var bubble = MakeBubble(text, BrushUserBubble, BrushCyan, $"{_username}", isUser: true);
        ChatPanel.Children.Add(bubble);
        ScrollToBottom();
    }

    private void AddBotMessage(string text)
    {
        var bubble = MakeBubble(text, BrushBotBubble, BrushCyan, "🛡 CyberBot", isUser: false);
        ChatPanel.Children.Add(bubble);
        ScrollToBottom();
    }

    private void AddBotMessageAnimated(string text)
    {
        var (container, tb) = MakeBubbleAnimated(BrushBotBubble, "🛡 CyberBot");
        ChatPanel.Children.Add(container);
        ScrollToBottom();
        StartTyping(tb, text);
    }

    private static Border MakeBubble(string text, SolidColorBrush bg, SolidColorBrush labelColor, string label, bool isUser)
    {
        var tb = new TextBlock
        {
            Text = text,
            Foreground = new SolidColorBrush(Color.FromRgb(0xE6, 0xED, 0xF3)),
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 13.5,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 3, 0, 0)
        };

        var labelTb = new TextBlock
        {
            Text = label,
            Foreground = labelColor,
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            FontFamily = new FontFamily("Segoe UI")
        };

        var stack = new StackPanel();
        stack.Children.Add(labelTb);
        stack.Children.Add(tb);

        return new Border
        {
            Background = bg,
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14, 10, 14, 10),
            Margin = isUser ? new Thickness(80, 4, 0, 4) : new Thickness(0, 4, 80, 4),
            BorderBrush = BrushBorder,
            BorderThickness = new Thickness(1),
            Child = stack
        };
    }

    private static (Border container, TextBlock textBlock) MakeBubbleAnimated(SolidColorBrush bg, string label)
    {
        var tb = new TextBlock
        {
            Foreground = new SolidColorBrush(Color.FromRgb(0xE6, 0xED, 0xF3)),
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 13.5,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 3, 0, 0)
        };

        var labelTb = new TextBlock
        {
            Text = label,
            Foreground = BrushCyan,
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            FontFamily = new FontFamily("Segoe UI")
        };

        var stack = new StackPanel();
        stack.Children.Add(labelTb);
        stack.Children.Add(tb);

        var border = new Border
        {
            Background = bg,
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14, 10, 14, 10),
            Margin = new Thickness(0, 4, 80, 4),
            BorderBrush = BrushBorder,
            BorderThickness = new Thickness(1),
            Child = stack
        };

        return (border, tb);
    }

    private void StartTyping(TextBlock target, string text)
    {
        _typeTimer?.Stop();
        _pendingText = text;
        _typeIndex = 0;
        _typeTarget = target;

        _typeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(14) };
        _typeTimer.Tick += TypeTick;
        _typeTimer.Start();
    }

    private void TypeTick(object? sender, EventArgs e)
    {
        if (_typeTarget is null || _typeIndex >= _pendingText.Length)
        {
            _typeTimer?.Stop();
            ScrollToBottom();
            return;
        }

        int chunk = Math.Min(3, _pendingText.Length - _typeIndex);
        _typeTarget.Text += _pendingText.Substring(_typeIndex, chunk);
        _typeIndex += chunk;
        ScrollToBottom();
    }

    private void ShowHelp()
    {
        var topics = new (string kw, string desc)[]
        {
            ("password",      "Password best practices"),
            ("phishing",      "Spot & avoid phishing attacks"),
            ("malware",       "Malware explained"),
            ("vpn",           "Why a VPN matters"),
            ("2fa",           "Two-factor authentication"),
            ("backup",        "The 3-2-1 backup strategy"),
            ("tips",          "Quick daily security checklist"),
            ("exit",          "Say goodbye")
        };

        var sp = new StackPanel { Margin = new Thickness(0, 6, 0, 0) };
        sp.Children.Add(new TextBlock
        {
            Text = "📚  Available Topics",
            Foreground = BrushYellow,
            FontWeight = FontWeights.Bold,
            FontSize = 13,
            Margin = new Thickness(0, 0, 0, 8)
        });

        foreach (var (kw, desc) in topics)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 2) };
            row.Children.Add(new TextBlock
            {
                Text = kw.PadRight(15),
                Foreground = BrushCyan,
                FontFamily = new FontFamily("Consolas"),
                FontSize = 12,
                Width = 120
            });
            row.Children.Add(new TextBlock
            {
                Text = $"→  {desc}",
                Foreground = BrushMuted,
                FontSize = 12
            });
            sp.Children.Add(row);
        }

        var border = new Border
        {
            Background = BrushBotBubble,
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(14, 10, 14, 10),
            Margin = new Thickness(0, 4, 80, 4),
            BorderBrush = BrushBorder,
            BorderThickness = new Thickness(1),
            Child = sp
        };

        ChatPanel.Children.Add(border);
        ScrollToBottom();
    }

    private void UpdateMemoryPanel()
    {
        MemoryUserLabel.Text = $"👤 Name: {_memory.Username}";
        MemoryTopicLabel.Text = $"⭐ Interest: {(_memory.FavouriteTopic ?? "–")}";
        MemorySentimentLabel.Text = $"💬 Mood: {(_memory.LastSentiment ?? "–")}";
    }

    private void ScrollToBottom()
    {
        Dispatcher.BeginInvoke(DispatcherPriority.Background, () => ChatScroll.ScrollToEnd());
    }

    private void ClearChat_Click(object sender, RoutedEventArgs e)
    {
        ChatPanel.Children.Clear();
        AddBotMessage("Chat cleared! How can I help you stay safe online?");
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}