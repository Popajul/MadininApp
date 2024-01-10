using MadininApp.Services;
using MadininApp.ViewModel;
using MadininApp.Windows;
using Microsoft.Win32;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

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
                if(_model == null)
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
                if(_editor == null)
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

            
        }

        private void OnValiderClicked(object sender, RoutedEventArgs e)
        {
            var selectedItems = _model.DataCollection.Where(item => item.IsChecked).ToList();

            var lettreHtml =  HtmlGenerator.CreateHtmlFile(selectedItems, LeMotDuChef);
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


    }

}


