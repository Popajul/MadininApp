using MadininApp.Objects;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MadininApp.User_Control
{
    /// <summary>
    /// Logique d'interaction pour Tuile.xaml
    /// </summary>
    public partial class Tuile : UserControl
    {
       
        // DependencyProperty pour Data
        public static readonly DependencyProperty ArticleProperty =
            DependencyProperty.Register("Article", typeof(MadinArticle), typeof(Tuile), new PropertyMetadata(null));
        public MadinArticle Article
        {
            get { return (MadinArticle)GetValue(ArticleProperty); }
            set { SetValue(ArticleProperty, value); }
        }

        public Tuile()
        {
            InitializeComponent();

        }
    }
}
