using MadininApp.Objects;
using MadininApp.Services;
using MadininApp.User_Control;
using MadininApp.ViewModel;
using MadininApp.Windows;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
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
            set
            {
                _model = value;
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
            myStackItemsControl.Loaded += MyItemsControl_Loaded;
            myWrapItemsControl.Loaded += MyItemsControl_Loaded;


        }

        private void UpdateTuileDroppedEventSubscription()
        {
            myStackItemsControl.UpdateLayout();
            myWrapItemsControl.UpdateLayout();
            foreach (var item in myStackItemsControl.Items)
            {
                // Obtenir le conteneur d'éléments pour l'item actuel
                if (myStackItemsControl.ItemContainerGenerator.ContainerFromItem(item) is FrameworkElement container)
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
            foreach (var item in myWrapItemsControl.Items)
            {
                // Obtenir le conteneur d'éléments pour l'item actuel
                if (myWrapItemsControl.ItemContainerGenerator.ContainerFromItem(item) is FrameworkElement container)
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
            Model.OtherArticles = new ObservableCollection<MadinArticle>(Model.OtherArticles.Where(a => a?.IsChecked ?? false));
            if(Model.OtherArticles.Count % 2 != 0)
            {
                Model.OtherArticles.Add(MadinArticle.GetPlaceHolderArticle());
            }
            RefreshDataContextCategories();
            UpdateTuileDroppedEventSubscription();
        }
        //Generation de la lettre Html
        private void OnValiderClicked(object sender, RoutedEventArgs e)
        {
            var selectedItems = _model.OtherArticles.Where(a=>a.IsChecked).ToList();
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
            var dataCollection = Model.OtherArticles;
            int targetIndex = dataCollection.IndexOf(target.Article);
            int sourceIndex = dataCollection.IndexOf(source.Article);
            var articleSource = source.Article;
            var articleTarget = target.Article;
            if (sourceIndex < 0 || targetIndex < 0 || sourceIndex == targetIndex || articleSource.IsPlaceHolder) return; // No valid drop, or dropped on itself

            // Deux cas à gérer selon que l'article cible et l'article source appartiennent à la même colonne ou non
            // Les index de la premiere colonne sont pairs et ceux de la deuxième colonne sont impairs
            bool moveFromSameColumn = sourceIndex % 2 == targetIndex % 2;
            var targetColumn = new ObservableCollection<MadinArticle>(dataCollection.Where((a, i) => i % 2 == targetIndex % 2));
            var sourceColumn = new ObservableCollection<MadinArticle>(dataCollection.Where((a, i) => i % 2 == sourceIndex % 2));
            if (moveFromSameColumn)
            {
                // TargetColumn == SourceColumn
                var secondColumn = dataCollection.Where((a, i) => i % 2 != sourceIndex % 2).ToList();
                var sourceIndexInTargetColumn = targetColumn.IndexOf(articleSource);
                var targetIndexInTargetColumn = targetColumn.IndexOf(articleTarget);
                sourceColumn.Move(sourceIndexInTargetColumn, targetIndexInTargetColumn - 1 < 0 ? 0 : targetIndexInTargetColumn - 1);
                //On reconstruit la liste de tous les articles


                for (int i = 0; i < dataCollection.Count; i++)
                {
                    if (i % 2 == sourceIndex % 2)
                    {
                        dataCollection[i] = sourceColumn[i / 2];
                    }
                    else
                    {
                        continue;
                    }
                }

            }
            else
            {
                // Suppression des Fake articles ils seront ajouté après le déplacement pour rééquilibrer les colonnes avant de reconstruire la collection complète
                var fakeArticleInSourceColumn = sourceColumn.Where(a => a?.IsPlaceHolder??true).ToList();
                foreach (var fakeArticle in fakeArticleInSourceColumn)
                {
                    sourceColumn.Remove(fakeArticle);
                }
               
                var FakeArticleInTargetColumn = targetColumn.Where(a => a?.IsPlaceHolder ?? true).ToList();
                foreach (var fakeArticle in FakeArticleInTargetColumn)
                {
                    targetColumn.Remove(fakeArticle);
                }

                // On Supprime l'article source de sa colonne d'origine
                if(!articleSource.IsPlaceHolder)
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
                int fakeArticleInserted = 0;
                var sourceColumnCount = sourceColumn.Count;
                var targetColumnCount = targetColumn.Count;
                if (sourceColumnCount != targetColumnCount)
                {
                    
                    var column = sourceColumnCount < targetColumnCount ? sourceColumn : targetColumn;
                    while(sourceColumn.Count != targetColumn.Count)
                    {
                        var addedfakeArticle = MadinArticle.GetPlaceHolderArticle();
                        column.Add(addedfakeArticle);
                        fakeArticleInserted += 1;
                    }
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
                
                var counter = dataCollection.Where(a=>a.IsNotPlaceHolder).Count() + fakeArticleInserted;
                dataCollection = new ObservableCollection<MadinArticle>(Enumerable.Repeat<MadinArticle>(null, counter));
                for (int i = 0; i < counter; i++)
                {

                    if (i % 2 == 0)
                    {
                        var art = firstCollection.First();
                        firstCollection.Remove(art);
                        dataCollection[i] = art;
                    }
                    else
                    {
                        var art = secondCollection.FirstOrDefault();
                        secondCollection.Remove(art);
                        dataCollection[i] = art;
                    }
                }
            }
            Model.OtherArticles = dataCollection;
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

            var articleGroupByCategory = articleAvecCategory.GroupBy(a => a.Category);
            List<IGrouping<string, MadinArticle>> orderedGroupByCategory = new List<IGrouping<string, MadinArticle>>();

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
            result.AddRange(articlesSansCategory);



            for (int i = 0; i < result.Count; i++)
            {
                Model.OtherArticles[i] = result[i];
            }

            UpdateTuileDroppedEventSubscription();
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (var art in Model.OtherArticles)
            {
                art.ImageVisibility = Visibility.Visible;
            }
            SwitchPanel(sender, e);

        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var art in Model.OtherArticles)
            {
                art.ImageVisibility = Visibility.Collapsed;
            }
            SwitchPanel(sender, e);
        }

        private void SwitchPanel(object sender, RoutedEventArgs e)
        {
            // Basculer entre StackPanel et WrapPanel
            if (myWrapPanel == null || myStackPanel == null)
                return;
            if (myWrapPanel.Visibility == Visibility.Visible)
            {
                myWrapPanel.Visibility = Visibility.Collapsed;
                myStackPanel.Visibility = Visibility.Visible;
            }
            else
            {
                myWrapPanel.Visibility = Visibility.Visible;
                myStackPanel.Visibility = Visibility.Collapsed;
            }
            UpdateLayout();
        }

        private void OnReinitialiserClicked(object sender, RoutedEventArgs e)
        {
            _model = new MainViewModel();
            DataContext = Model;
            InitializeComponent();
            myStackItemsControl.Loaded += MyItemsControl_Loaded;
            myWrapItemsControl.Loaded += MyItemsControl_Loaded;
            ToutSelectionnerCheckBox.IsChecked = false;
            RefreshDataContextCategories();
            UpdateTuileDroppedEventSubscription();
        }

        private void OnSauvegarderClicked(object sender, RoutedEventArgs e)
        {
            var json = Model.SaveData();
            SaveFileDialog openFileDialog = new SaveFileDialog
            {
                Filter = "MADININAPP File (*.mdn)|*.mdn|Tous les fichiers (*.*)|*.*",
                DefaultExt = "mdn", // Extension par défaut
                AddExtension = true, // Ajouter l'extension si l'utilisateur ne le fait pas
                Title = "Sauvegarder",
                FileName = $"{DateTime.Now:dd_MM_yyyy_HH_mm_ss}.mdn",
            };

            if(openFileDialog.ShowDialog()??false)
                File.WriteAllText(openFileDialog.FileName, json,Encoding.UTF8);
        
        }

        private void OnLoadDataClicked(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "MADININAPP File (*.mdn)|*.mdn|Tous les fichiers (*.*)|*.*",
                DefaultExt = "mdn", // Extension par défaut
                AddExtension = true, // Ajouter l'extension si l'utilisateur ne le fait pas
          
            };
            // Afficher le dialogue
            bool? result = openFileDialog.ShowDialog();

            // Traitement du résultat
            if (result == true)
            {
                // L'utilisateur a sélectionné un fichier
                string cheminFichier = openFileDialog.FileName;
                Model = Model.LoadData(cheminFichier);
                DataContext = Model;
                RefreshDataContextCategories();
                UpdateTuileDroppedEventSubscription();
            }
        }
        private void ShowHelpWindow(object sender, RoutedEventArgs e)
        {
            HelpWindow helpWindow = new HelpWindow();
            helpWindow.Owner = this;
            helpWindow.ShowDialog(); // Affiche la fenêtre en mode modale
        }
    }
    


}


