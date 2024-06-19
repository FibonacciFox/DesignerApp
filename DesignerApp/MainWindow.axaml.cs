using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using System.Linq;
using System.Text;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;

namespace DesignerApp
{
    public partial class MainWindow : Window
    {
        private StringBuilder _axamlBuilder;
        private Control _selectedControl;

        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            _axamlBuilder = new StringBuilder();

            UpdateControlsTree();
        }

        private void AddControl(string controlType)
        {
            Control? control = controlType switch
            {
                "Button" => new Button { Content = "New Button", Name = "button1" },
                "TextBox" => new TextBox { Text = "New TextBox" },
                "Label" => new TextBlock { Text = "New Label" },
                "StackPanel" => new StackPanel(),
                "DockPanel" => new DockPanel(),
                "Grid" => new Grid(),
                "ComboBox" => new ComboBox { ItemsSource = new[] { "Item 1", "Item 2", "Item 3" } },
                "CheckBox" => new CheckBox { Content = "New CheckBox" },
                "RadioButton" => new RadioButton { Content = "New RadioButton" },
                "Slider" => new Slider { Minimum = 0, Maximum = 100, Value = 50 },
                "ProgressBar" => new ProgressBar { Minimum = 0, Maximum = 100, Value = 50 },
                "Image" => new Image { Source = new Avalonia.Media.Imaging.Bitmap(AssetLoader.Open(new Uri("avares://DesignerApp/Assets/image.png"))), Width = 100, Height = 100 },
                _ => null,
            };

            if (control != null)
            {
                control.PointerPressed += Control_PointerPressed;

                if (_selectedControl is Panel selectedPanel)
                {
                    selectedPanel.Children.Add(control);
                }
                else
                {
                    DesignerCanvas.Children.Add(control);
                }

                AppendToAxaml(control);
                UpdateControlsTree();
            }
        }

        private void ControlsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ControlsListView.SelectedItem is TextBlock selectedItem)
            {
                AddControl(selectedItem.Tag.ToString());
                ControlsListView.SelectedItem = null;
            }
        }

        private void Control_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (sender is Control control)
            {
                _selectedControl = control;
                PropertiesEditor.SetSelectedControl(control);
            }
        }

        public void AppendToAxaml(Control? control)
        {
            var axaml = GenerateControlAxaml(control);
            _axamlBuilder.AppendLine(axaml);
            AxamlTextBox.Text = GenerateAxaml();
        }

        private string GenerateControlAxaml(Control? control)
        {
            var sb = new StringBuilder();
            var type = control.GetType();

            sb.Append($"<{type.Name}");

            if (!string.IsNullOrEmpty(control.Name))
            {
                sb.Append($" Name=\"{control.Name}\"");
            }

            foreach (var property in AvaloniaPropertyRegistry.Instance.GetRegistered(control)
                .Where(p => !p.IsReadOnly && p.Name != "Content"))
            {
                var value = control.GetValue(property);

                if (value is double doubleValue)
                {
                    if (double.IsInfinity(doubleValue))
                    {
                        continue;
                    }
                    else if (double.IsNaN(doubleValue))
                    {
                        value = 0;
                    }
                }

                sb.Append($" {property.Name}=\"{value}\"");
            }

            sb.Append(" />");
            return sb.ToString();
        }

        private string GenerateAxaml()
        {
            var axaml = new StringBuilder();
            axaml.AppendLine("<StackPanel xmlns=\"https://github.com/avaloniaui\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">");
            axaml.AppendLine(_axamlBuilder.ToString());
            axaml.AppendLine("</StackPanel>");
            return axaml.ToString();
        }

        private void UpdateControlsTree()
        {
            var root = new TreeViewItem { Header = "DesignerCanvas", IsExpanded = true };
            foreach (var child in DesignerCanvas.Children)
            {
                root.Items.Add(CreateTreeViewItem(child));
            }
            ControlsTree.ItemsSource = new[] { root };
        }

        private TreeViewItem CreateTreeViewItem(Control control)
        {
            var item = new TreeViewItem { Header = control.GetType().Name, Tag = control, IsExpanded = true };
            item.PointerPressed += TreeViewItem_PointerPressed;
            if (control is Panel panel)
            {
                foreach (var child in panel.Children)
                {
                    item.Items.Add(CreateTreeViewItem(child));
                }
            }
            return item;
        }

        private void TreeViewItem_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (sender is TreeViewItem item && item.Tag is Control control)
            {
                _selectedControl = control;
                PropertiesEditor.SetSelectedControl(control);
            }
        }

        private void ControlsTree_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ControlsTree.SelectedItem is TreeViewItem item && item.Tag is Control control)
            {
                _selectedControl = control;
                PropertiesEditor.SetSelectedControl(control);
            }
        }
    }
}
