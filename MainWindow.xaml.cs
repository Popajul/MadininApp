using MadininApp.Objects;
using MadininApp.Services;
using MadininApp.User_Control;
using MadininApp.ViewModel;
using MadininApp.Windows;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace MadininApp
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel _model;
        public MainViewModel Model
        {
            get
            {
                if (_model == null)
                {
                    _model = new MainViewModel();
                }
                return _model;
            }
        }
        private TextEditorWindow Editor
        {
            get
            {
                if (_editor == null)
                    _editor = new TextEditorWindow();
                return _editor;
            }
        }
        private TextEditorWindow _editor;
        public BlockCollection LeMotDuChef => Editor.txtEditor?.Document?.Blocks;
        public MainWindow()
        {
            DataContext = Model;
            InitializeComponent();
            myItemsControl.Loaded += MyItemsControl_Loaded;

        }

        private void UpdateTuileDroppedEventSubscription()
        {
            myItemsControl.UpdateLayout();

            foreach (var item in myItemsControl.Items)
            {
                // Obtenir le conteneur d'éléments pour l'item actuel
                var container = myItemsControl.ItemContainerGenerator.ContainerFromItem(item) as FrameworkElement;
                if (container != null)
                {
                    // Trouver l'instance de Tuile à l'intérieur du conteneur d'éléments
                    Tuile tuile = VisualTreeHelperExtensions.FindVisualChild<Tuile>(container);
                    if (tuile != null)
                    {

                        tuile.TuileDropped -= Tuile_TuileDropped;
                        tuile.TuileDropped += Tuile_TuileDropped;
                    }
                }
            }
        }

        private void RefreshDataContextCategories()
        {
            Model.Categories = new ObservableCollection<string>(Model.DataCollection.Select(a => a.Category).Distinct());
        }
        private void MyItemsControl_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateTuileDroppedEventSubscription();
            RefreshDataContextCategories();
        }
        private void OnSelectionValidate(object sender, RoutedEventArgs e)
        {
            // suppression des article non selectionné de la liste des articles
            Model.DataCollection = new ObservableCollection<MadinArticle>(Model.DataCollection.Where(a => a.IsChecked));
            Model.OtherArticles = new ObservableCollection<MadinArticle>(Model.OtherArticles.Where(a => a.IsChecked));
            UpdateTuileDroppedEventSubscription();
        }
        //Generation de la lettre Html
        private void OnValiderClicked(object sender, RoutedEventArgs e)
        {
            var selectedItems = _model.OtherArticles.ToList();
            string positionBasLettre = null;
            foreach (var child in PositionLettreStackPanel.Children)
            {
                if (child is RadioButton radioButton && (bool)radioButton.IsChecked)
                {
                    positionBasLettre = radioButton.Content.ToString();
                    break;
                }
            }
            
            var lettreHtml = HtmlGenerator.CreateHtmlFile(selectedItems, _model.TopArticle, LeMotDuChef, positionBasLettre);
            SaveFile(lettreHtml);
        }
        private void OnSelectAllChecked(object sender, RoutedEventArgs e)
        {
            if (!(sender is CheckBox cb) || !cb.IsChecked.HasValue) return;

            foreach (var item in _model.DataCollection)
            {
                item.IsChecked = cb.IsChecked.Value;
            }
        }

        private void OnOpenTextEditor(object sender, RoutedEventArgs e)
        {
            OpenTextEditor();
        }
        private void SaveFile(string content)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "HTML File (*.html)|*.html|All files (*.*)|*.*", // Filtre pour les fichiers HTML
                DefaultExt = "html", // Extension par défaut
                AddExtension = true // Ajouter l'extension si l'utilisateur ne le fait pas
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                File.WriteAllText(saveFileDialog.FileName, content);
            }
        }

        private void OpenTextEditor()
        {
            Editor.Show();
        }


        private void Tuile_TuileDropped(Tuile source, Tuile target)
        {
            var DataCollection = Model.OtherArticles;
            int targetIndex = DataCollection.IndexOf(target.Article);
            int sourceIndex = DataCollection.IndexOf(source.Article);
            var articleSource = source.Article;
            var articleTarget = target.Article;
            if (sourceIndex < 0 || targetIndex < 0 || sourceIndex == targetIndex || articleSource.IsPlaceHolder) return; // No valid drop, or dropped on itself

            // Deux cas à gérer selon que l'article cible et l'article source appartiennent à la même colonne ou non
            // Les index de la premiere colonne sont pairs et ceux de la deuxième colonne sont impairs
            bool moveFromSameColumn = sourceIndex % 2 == targetIndex % 2;
            var targetColumn = new ObservableCollection<MadinArticle>(DataCollection.Where((a, i) => i % 2 == targetIndex % 2));
            var sourceColumn = new ObservableCollection<MadinArticle>(DataCollection.Where((a, i) => i % 2 == sourceIndex % 2));
            if (moveFromSameColumn)
            {
                // TargetColumn == SourceColumn
                var secondColumn = DataCollection.Where((a, i) => i % 2 != sourceIndex % 2).ToList();
                var sourceIndexInTargetColumn = targetColumn.IndexOf(articleSource);
                var targetIndexInTargetColumn = targetColumn.IndexOf(articleTarget);
                
                sourceColumn.Move(sourceIndexInTargetColumn, targetIndexInTargetColumn - 1 < 0? 0 : targetIndexInTargetColumn - 1);
                //On reconstruit la liste de tous les articles


                for (int i = 0; i < DataCollection.Count; i++)
                {
                    if (i % 2 == sourceIndex % 2)
                    {
                        DataCollection[i] = sourceColumn[i / 2];
                    }
                    else
                    {
                        continue;
                    }
                }

            }
            else
            {
                var fakeArticleInSourceColumn = sourceColumn.FirstOrDefault(a => a.IsPlaceHolder);
                if (fakeArticleInSourceColumn != null)
                {
                    sourceColumn.Remove(fakeArticleInSourceColumn);
                }

                var FakeArticleInTargetColumn = targetColumn.FirstOrDefault(a => a.IsPlaceHolder);
                if (FakeArticleInTargetColumn != null)
                {
                    targetColumn.Remove(FakeArticleInTargetColumn);
                }
                // On Supprime l'article source de sa colonne d'origine
                sourceColumn.Remove(articleSource);
                // On l'insert dans la colonne cible
                var targetIndexInTargetColumn = targetColumn.IndexOf(articleTarget);
                if (targetIndexInTargetColumn < 0)
                {
                    targetColumn.Add(articleSource);
                }
                else
                {
                    targetColumn.Insert(targetIndexInTargetColumn, articleSource);
                }

                // si le nombre d'articles sur la colonne source est inférieur à celui de la colonne cible et que la colonne source est la colonne de gauche 
                // on ajoute un article vide à la fin de la colonne source
                bool fakeArticleInserted = false;
                if (sourceColumn.Count < targetColumn.Count && sourceIndex % 2 == 0)
                {
                    var addedfakeArticle = MadinArticle.GetPlaceHolderArticle();
                    sourceColumn.Add(addedfakeArticle);
                    fakeArticleInserted = true;
                }
                // Si la colonne cible est la colonne de gauche et que après le déplacement elle contient plus d'articles que la colonne de droite
                // On supprime les éventuelle articles vides
                if (targetColumn.Count > sourceColumn.Count && targetIndex % 2 == 0)
                {
                    var previousAddedfakeArticle = targetColumn.FirstOrDefault(a => a.IsPlaceHolder);
                    if(previousAddedfakeArticle != null)
                        targetColumn.Remove(previousAddedfakeArticle);
                }

                // On reconstruit la liste de tous les articles
                ObservableCollection<MadinArticle> firstCollection = null;
                ObservableCollection<MadinArticle> secondCollection = null;
                if (sourceIndex % 2 == 0)
                {
                    firstCollection = sourceColumn;
                    secondCollection = targetColumn;
                }
                else
                {
                    firstCollection = targetColumn;
                    secondCollection = sourceColumn;
                }
                var fakeArticle = DataCollection.FirstOrDefault(a => a.IsPlaceHolder);
                if (fakeArticle != null) 
                {
                    DataCollection.Remove(fakeArticle);
                }

                
                for (int i = 0; i < DataCollection.Count; i++)
                {

                    if (i % 2 == 0)
                    {
                        var art = firstCollection.First();
                        firstCollection.Remove(art);
                        DataCollection[i] = art;
                    }
                    else
                    {
                        var art = secondCollection.FirstOrDefault();
                        secondCollection.Remove(art);
                        DataCollection[i] = art;
                    }
                }
                if (fakeArticleInserted)
                {
                    DataCollection.Add(secondCollection.Last());
                }
            }

            UpdateTuileDroppedEventSubscription();

        }

        private void ScrollViewer_PreviewDragOver(object sender, DragEventArgs e)
        {
            const double tolerance = 60; // La distance en pixels depuis le bord pour déclencher le défilement
            const double offset = 60; // Combien défiler à chaque intervalle

            var scrollViewer = sender as ScrollViewer;
            var mousePosition = e.GetPosition(scrollViewer);

            // Défilement vers le haut
            if (mousePosition.Y < tolerance && scrollViewer.VerticalOffset > 0)
            {
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - offset);
            }
            // Défilement vers le bas
            else if (mousePosition.Y > scrollViewer.ActualHeight - tolerance && scrollViewer.VerticalOffset < scrollViewer.ScrollableHeight)
            {
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + offset);
            }

            // Ajoutez ici la logique pour le défilement horizontal si nécessaire
        }


        private void CategoriesListBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var listBox = sender as ListBox;
            var viewModel = DataContext as MainViewModel;

            if (listBox.SelectedItem is string selectedCategory)
            {
                int currentIndex = viewModel.Categories.IndexOf(selectedCategory);
                int newIndex = currentIndex;

                switch (e.Key)
                {
                    case Key.Up:
                        newIndex = currentIndex - 1;
                        break;
                    case Key.Down:
                        newIndex = currentIndex + 1;
                        break;
                }

                if (newIndex >= 0 && newIndex < viewModel.Categories.Count)
                {
                    viewModel.Categories.Move(currentIndex, newIndex);
                    listBox.SelectedIndex = newIndex; // Gardez l'élément déplacé sélectionné
                    e.Handled = true; // Marquez l'événement comme géré
                }
            }
        }


        private void OnOrderCategoryChanged(object sender, RoutedEventArgs e)
        {
           var orderedCategories = CategoriesListBox.Items.Cast<string>().ToList();
            OrderDataCollectionByCategory(orderedCategories);
        }

        private void OrderDataCollectionByCategory(List<string> orderedCategories)
        {
            var madinArticles = Model.OtherArticles;
          

            var articlesSansCategory = madinArticles.Where(a => string.IsNullOrWhiteSpace(a.Category)).ToList();
            var articleAvecCategory = madinArticles.Where(a => !string.IsNullOrWhiteSpace(a.Category)).ToList();

            var articleGroupByCategory = madinArticles.GroupBy(a => a.Category);
            List<IGrouping<string,MadinArticle>> orderedGroupByCategory = new List<IGrouping<string, MadinArticle>>();

            foreach (var category in orderedCategories)
            {
                var group = articleGroupByCategory.FirstOrDefault(g => g.Key == category);
                if (group != null)
                {
                    orderedGroupByCategory.Add(group);
                }
            }
            var filteredArticles = orderedGroupByCategory.SelectMany(g => g).ToList();
            var result = new List<MadinArticle>();
            var firstArticle = filteredArticles.First();
            filteredArticles.Remove(firstArticle);
            result.Add(firstArticle);

            while (filteredArticles.Count != 0)
            {
                var lastArticle = result.Last();
                var article = filteredArticles.FirstOrDefault(a => a.Category != lastArticle.Category);
                if (article == null)
                {
                    break;
                }
                filteredArticles.Remove(article);
                result.Add(article);
            }

            while (filteredArticles.Count != 0)
            {
                var article = filteredArticles.First();
                filteredArticles.Remove(article);
                var articleSansCategory = articlesSansCategory.FirstOrDefault(a => a.Title == article.Title);
                if (articleSansCategory != null)
                {
                    result.Add(articleSansCategory);
                    articlesSansCategory.Remove(articleSansCategory);
                }
                result.Add(article);
            }
           

            
            for (int i = 0; i < result.Count; i++)
            {
                Model.OtherArticles[i] = result[i];
            }
            
            UpdateTuileDroppedEventSubscription();
        }
    }

}


