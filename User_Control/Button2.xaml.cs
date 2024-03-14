using System.Windows;
using System.Windows.Controls;

namespace MadininApp.User_Control
{
    /// <summary>
    /// Logique d'interaction pour Button2.xaml
    /// </summary>
    public partial class Button2 : UserControl
    {
        public static readonly DependencyProperty ButtonImageSourceProperty = DependencyProperty.Register(
    "Button2ImageSource", typeof(string), typeof(Button2), new PropertyMetadata(default(string)));

        public string Button2ImageSource
        {
            get { return (string)GetValue(ButtonImageSourceProperty); }
            set { SetValue(ButtonImageSourceProperty, value); }
        }

        // Événement routé
        public static readonly RoutedEvent ButtonClickEvent = EventManager.RegisterRoutedEvent(
            "ButtonClick", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Button2));

        public event RoutedEventHandler ButtonClick
        {
            add { AddHandler(ButtonClickEvent, value); }
            remove { RemoveHandler(ButtonClickEvent, value); }
        }


        public Button2()
        {
            DataContext = this;
            InitializeComponent();
        }

        private void OnButtonClicked(object sender, RoutedEventArgs e)
        {
            // Déclencher l'événement
            RaiseEvent(new RoutedEventArgs(ButtonClickEvent));
        }
    }
}