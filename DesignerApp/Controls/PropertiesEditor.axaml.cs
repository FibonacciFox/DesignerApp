using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Input;
using Avalonia.Media;

namespace DesignerApp.Controls
{
    public partial class PropertiesEditor : UserControl
    {
        private Control _selectedControl;
        private Dictionary<string, List<AvaloniaProperty>> _categories;

        public PropertiesEditor()
        {
            InitializeComponent();
            InitializeCategories();
        }

        /// <summary>
        /// Инициализация категорий свойств.
        /// </summary>
        private void InitializeCategories()
        {
            _categories = new Dictionary<string, List<AvaloniaProperty>>
            {
                { "Layout", new List<AvaloniaProperty> { Canvas.LeftProperty, Canvas.TopProperty, Layoutable.WidthProperty, Layoutable.HeightProperty, Layoutable.MarginProperty } },
                { "Appearance", new List<AvaloniaProperty> { Visual.OpacityProperty, Visual.IsVisibleProperty } },
                { "Behavior", new List<AvaloniaProperty> { InputElement.IsEnabledProperty, InputElement.IsHitTestVisibleProperty } }
            };
        }

        /// <summary>
        /// Устанавливает выбранный контрол и обновляет список свойств.
        /// </summary>
        /// <param name="control">Выбранный контрол.</param>
        public void SetSelectedControl(Control control)
        {
            _selectedControl = control;
            UpdateProperties();
        }

        /// <summary>
        /// Обновляет список свойств выбранного контрола.
        /// </summary>
        private void UpdateProperties()
        {
            if (_selectedControl == null)
                return;

            PropertiesList.ItemsSource = null;

            var propertyItems = new List<Control>();
            var otherProperties = new List<AvaloniaProperty>();

            foreach (var category in _categories)
            {
                var expander = new Expander
                {
                    Header = category.Key,
                    Content = new StackPanel(),
                    Width = 350,
                    IsExpanded = true
                };

                foreach (var property in category.Value)
                {
                    if (AvaloniaPropertyRegistry.Instance.IsRegistered(_selectedControl, property) && !property.IsReadOnly)
                    {
                        var panel = CreatePropertyPanel(property);
                        if (panel != null)
                        {
                            (expander.Content as StackPanel).Children.Add(panel);
                        }
                    }
                }

                propertyItems.Add(expander);
            }

            // Обработка свойств, которые не попали в категории
            foreach (var property in AvaloniaPropertyRegistry.Instance.GetRegistered(_selectedControl).Where(p => !p.IsReadOnly))
            {
                if (!_categories.Values.Any(c => c.Contains(property)))
                {
                    otherProperties.Add(property);
                }
            }

            if (otherProperties.Any())
            {
                var otherExpander = new Expander
                {
                    Header = "Other",
                    Content = new StackPanel(),
                    Width = 350,
                    IsExpanded = true
                };

                foreach (var property in otherProperties)
                {
                    var panel = CreatePropertyPanel(property);
                    if (panel != null)
                    {
                        (otherExpander.Content as StackPanel).Children.Add(panel);
                    }
                }
                
                propertyItems.Add(otherExpander);
            }

            PropertiesList.ItemsSource = propertyItems;
        }

        /// <summary>
        /// Создает панель для отображения свойства.
        /// </summary>
        /// <param name="property">Свойство для отображения.</param>
        /// <returns>Панель для отображения свойства.</returns>
        private StackPanel CreatePropertyPanel(AvaloniaProperty property)
        {
            var value = _selectedControl.GetValue(property);
            var editor = CreateEditor(property, value);
            return editor;
        }

        /// <summary>
        /// Создает редактор для данного свойства.
        /// </summary>
        /// <param name="property">Свойство для редактирования.</param>
        /// <param name="value">Текущее значение свойства.</param>
        /// <returns>Контрол-редактор для свойства.</returns>
        private StackPanel CreateEditor(AvaloniaProperty property, object value)
        {
            if (property.PropertyType == typeof(string))
            {
                return CreateTextBox(value, property);
            }

            if (property.PropertyType == typeof(bool))
            {
                return CreateCheckBox(value, property);
            }

            if (property.PropertyType == typeof(int))
            {
                return CreateNumericUpDown(value, property);
            }

            if (property.PropertyType == typeof(double))
            {
                return CreateDoubleTextBox(value, property);
            }

            if (property.PropertyType.IsEnum)
            {
                return CreateComboBox(value, property);
            }

            return null;
        }

        /// <summary>
        /// Создает панель с текстовым редактором.
        /// </summary>
        /// <param name="value">Текущее значение свойства.</param>
        /// <param name="property">Свойство для редактирования.</param>
        /// <returns>Панель с текстовым редактором.</returns>
        private StackPanel CreateTextBox(object value, AvaloniaProperty property)
        {
            var textBox = new TextBox { Text = value?.ToString() };
            textBox.PropertyChanged += (sender, e) =>
            {
                if (e.Property == TextBox.TextProperty)
                {
                    _selectedControl.SetValue(property, textBox.Text);
                }
            };

            return new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(5),
                Children =
                {
                    new TextBlock { Text = property.Name, Width = 150, VerticalAlignment = VerticalAlignment.Center },
                    textBox
                }
            };
        }

        /// <summary>
        /// Создает панель с редактором для свойства типа double.
        /// </summary>
        /// <param name="value">Текущее значение свойства.</param>
        /// <param name="property">Свойство для редактирования.</param>
        /// <returns>Панель с редактором для свойства типа double.</returns>
        private StackPanel CreateDoubleTextBox(object value, AvaloniaProperty property)
        {
            var textBox = new TextBox
            {
                Text = IsValidDouble(value?.ToString()) ? value?.ToString() : string.Empty
            };
            
            textBox.PropertyChanged += (sender, e) =>
            {
                if (e.Property == TextBox.TextProperty)
                {
                    if (IsValidDouble(textBox.Text, out var result))
                    {
                        // Проверка допустимого диапазона для свойства Opacity
                        if (property.Name == "Opacity" && (result < 0 || result > 1))
                        {
                            textBox.Text = _selectedControl.GetValue(property).ToString();
                            return;
                        }

                        _selectedControl.SetValue(property, result);
                    }
                }
            };

            return new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(5),
                Children =
                {
                    new TextBlock { Text = property.Name, Width = 150, VerticalAlignment = VerticalAlignment.Center },
                    textBox
                }
            };
        }

        /// <summary>
        /// Создает панель с чекбоксом для редактирования булевого свойства.
        /// </summary>
        /// <param name="value">Текущее значение свойства.</param>
        /// <param name="property">Свойство для редактирования.</param>
        /// <returns>Панель с чекбоксом.</returns>
        private StackPanel CreateCheckBox(object value, AvaloniaProperty property)
        {
            var checkBox = new CheckBox { IsChecked = (bool?)value };
            checkBox.PropertyChanged += (sender, e) =>
            {
                if (e.Property == CheckBox.IsCheckedProperty)
                {
                    _selectedControl.SetValue(property, checkBox.IsChecked);
                }
            };

            return new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(5),
                Children =
                {
                    new TextBlock { Text = property.Name, Width = 150, VerticalAlignment = VerticalAlignment.Center },
                    checkBox
                }
            };
        }

        /// <summary>
        /// Создает панель с редактором для числового свойства.
        /// </summary>
        /// <param name="value">Текущее значение свойства.</param>
        /// <param name="property">Свойство для редактирования.</param>
        /// <returns>Панель с редактором для числового свойства.</returns>
        private StackPanel CreateNumericUpDown(object value, AvaloniaProperty property)
        {
            var numericUpDown = new NumericUpDown { Value = Convert.ToDecimal(value) };
            numericUpDown.PropertyChanged += (sender, e) =>
            {
                if (e.Property == NumericUpDown.ValueProperty)
                {
                    _selectedControl.SetValue(property, (int)numericUpDown.Value);
                }
            };

            return new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(5),
                Children =
                {
                    new TextBlock { Text = property.Name, Width = 150, VerticalAlignment = VerticalAlignment.Center },
                    numericUpDown
                }
            };
        }

        /// <summary>
        /// Создает панель с выпадающим списком для редактирования свойства-перечисления.
        /// </summary>
        /// <param name="value">Текущее значение свойства.</param>
        /// <param name="property">Свойство для редактирования.</param>
        /// <returns>Панель с выпадающим списком.</returns>
        private StackPanel CreateComboBox(object value, AvaloniaProperty property)
        {
            var comboBox = new ComboBox
            {
                ItemsSource = Enum.GetValues(property.PropertyType),
                SelectedItem = value
            };
            comboBox.PropertyChanged += (sender, e) =>
            {
                if (e.Property == ComboBox.SelectedItemProperty)
                {
                    _selectedControl.SetValue(property, comboBox.SelectedItem);
                }
            };

            return new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(5),
                Children =
                {
                    new TextBlock { Text = property.Name, Width = 150, VerticalAlignment = VerticalAlignment.Center },
                    comboBox
                }
            };
        }

        /// <summary>
        /// Создает панель для отображения нескольких свойств.
        /// </summary>
        /// <param name="properties">Список свойств для отображения.</param>
        /// <returns>Панель для отображения нескольких свойств.</returns>
        private Expander CreateMultiPropertyPanel(IEnumerable<AvaloniaProperty> properties)
        {
            var panel = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(5) };

            foreach (var property in properties)
            {
                var value = _selectedControl.GetValue(property);
                var editor = CreateEditor(property, value);

                if (editor != null)
                {
                    panel.Children.Add(editor);
                }
            }

            var expander = new Expander
            {
                Header = "Layout",
                Content = panel,
                Width = 350,
                IsExpanded = true,
                Background = Brushes.DarkSlateGray,
                Foreground = Brushes.Brown
            };

            return expander;
        }

        /// <summary>
        /// Преобразует строку в объект Thickness.
        /// </summary>
        /// <param name="s">Строка для преобразования.</param>
        /// <param name="thickness">Результирующий объект Thickness.</param>
        /// <returns>Возвращает true, если преобразование прошло успешно; иначе false.</returns>
        private bool TryParseThickness(string s, out Thickness thickness)
        {
            thickness = default;

            var parts = s.Split(',');
            if (parts.Length != 4)
                return false;

            if (double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var left) &&
                double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var top) &&
                double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var right) &&
                double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var bottom))
            {
                thickness = new Thickness(left, top, right, bottom);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Проверяет, является ли строка допустимым числом с плавающей точкой.
        /// </summary>
        /// <param name="value">Строка для проверки.</param>
        /// <param name="result">Результат преобразования строки в double.</param>
        /// <returns>Возвращает true, если строка является допустимым числом с плавающей точкой; иначе false.</returns>
        private bool IsValidDouble(string value, out double result)
        {
            if (double.TryParse(value, out result))
            {
                return !double.IsInfinity(result) && !double.IsNaN(result);
            }
            return false;
        }

        /// <summary>
        /// Проверяет, является ли строка допустимым числом с плавающей точкой.
        /// </summary>
        /// <param name="value">Строка для проверки.</param>
        /// <returns>Возвращает true, если строка является допустимым числом с плавающей точкой; иначе false.</returns>
        private bool IsValidDouble(string value)
        {
            return IsValidDouble(value, out _);
        }
    }
}
