using MadininApp.Objects;
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;

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

        // Ajoutez un delegate pour l'événement
        public delegate void TuileDroppedHandler(Tuile source, Tuile target);

        // Définissez l'événement basé sur ce delegate
        public event TuileDroppedHandler TuileDropped;
        private void Tuile_Drop(object sender, DragEventArgs e)
        {
            // Exemple de récupération de l'objet déplacé
            var sourceTuile = e.Data.GetData(typeof(Tuile)) as Tuile;
            if (sourceTuile != null)
            {
                TuileDropped?.Invoke(sourceTuile, sender as Tuile);
            }

        }

        private void Control_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }
            
                // Préparation des données à déplacer
                var data = new DataObject(typeof(Tuile), this);

                // Démarrage du drag
                DragDrop.DoDragDrop((DependencyObject)sender, data, DragDropEffects.Move);
            
        }

    }
}
