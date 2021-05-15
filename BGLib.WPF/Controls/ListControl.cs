using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BGLib.WPF.Controls
{
    /// <summary>
    /// 按照步骤 1a 或 1b 操作，然后执行步骤 2 以在 XAML 文件中使用此自定义控件。
    ///
    /// 步骤 1a) 在当前项目中存在的 XAML 文件中使用该自定义控件。
    /// 将此 XmlNamespace 特性添加到要使用该特性的标记文件的根
    /// 元素中:
    ///
    ///     xmlns:MyNamespace="clr-namespace:BGLib.LegacyWPF"
    ///
    ///
    /// 步骤 1b) 在其他项目中存在的 XAML 文件中使用该自定义控件。
    /// 将此 XmlNamespace 特性添加到要使用该特性的标记文件的根
    /// 元素中:
    ///
    ///     xmlns:MyNamespace="clr-namespace:BGLib.LegacyWPF;assembly=BGLib.LegacyWPF"
    ///
    /// 您还需要添加一个从 XAML 文件所在的项目到此项目的项目引用，
    /// 并重新生成以避免编译错误:
    ///
    ///     在解决方案资源管理器中右击目标项目，然后依次单击
    ///     “添加引用”->“项目”->[浏览查找并选择此项目]
    ///
    ///
    /// 步骤 2)
    /// 继续操作并在 XAML 文件中使用控件。
    ///
    ///     <MyNamespace:ListControl/>
    ///
    /// </summary>
    [StyleTypedProperty(Property = "ItemContainerStyle", StyleTargetType = typeof(ListItemControl))]
    public class ListControl : ItemsControl
    {
        static ListControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ListControl), new FrameworkPropertyMetadata(typeof(ListControl)));
            var template = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(VirtualizingStackPanel)));
            template.Seal();
            ItemsPanelProperty.OverrideMetadata(typeof(ListControl), new FrameworkPropertyMetadata(template));
        }

        private ListItemControl _item;

        public event EventHandler<ItemClickedEventArgs> ItemClicked
        {
            add => AddHandler(ItemClickedEvent, value);
            remove => RemoveHandler(ItemClickedEvent, value);
        }

        // Add a RoutedEvent Registration for ItemClicked. 
        public static readonly RoutedEvent ItemClickedEvent =
            EventManager.RegisterRoutedEvent(
                nameof(ItemClicked),
                RoutingStrategy.Bubble,
                typeof(EventHandler<ItemClickedEventArgs>),
                typeof(ListControl));

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is ListItemControl;
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new ListItemControl();
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            _item = ContainerFromElement(e.OriginalSource as DependencyObject) as ListItemControl;
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);

            var item = ContainerFromElement(e.OriginalSource as DependencyObject) as ListItemControl;
            if (item == null || item != _item)
                return;
            var obj = item.DataContext ?? item;
            var eventArgs = new ItemClickedEventArgs(ItemClickedEvent, obj);
            RaiseEvent(eventArgs);
        }
    }
}
