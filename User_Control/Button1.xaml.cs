using System.Windows;
using System.Windows.Controls;

namespace MadininApp.User_Control
{
    /// <summary>
    /// Logique d'interaction pour Button1.xaml
    /// </summary>
    public partial class Button1 : UserControl
    {
        public static readonly DependencyProperty Button1ContentProperty = DependencyProperty.Register(
    "Button1Content", typeof(string), typeof(Button1), new PropertyMetadata(default(string)));

        public string Button1Content
        {
            get { return (string)GetValue(Button1ContentProperty); }
            set { SetValue(Button1ContentProperty, value); }
        }

        // Événement routé
        public static readonly RoutedEvent ButtonClickEvent = EventManager.RegisterRoutedEvent(
            "ButtonClick", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Button1));

        public event RoutedEventHandler ButtonClick
        {
            add { AddHandler(ButtonClickEvent, value); }
            remove { RemoveHandler(ButtonClickEvent, value); }
        }


        public Button1()
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