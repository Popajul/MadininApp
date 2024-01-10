using HtmlAgilityPack;
using System.Collections.Generic;
using System.ComponentModel;

namespace MadininApp.Objects
{
    public class Category
    {
        public string Url;
        public string Name;
        public Category() { }
    }
    public class MadinArticle : INotifyPropertyChanged
    {
        public string Content { get; set; }
        public string Category { get; set; }
        public IEnumerable<Category> Categories { get; set; }
        public string Date { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string ImageUrl { get; set; }
        public string HtmlContent { get; set; }
        public bool IsActualite { get => Title.Contains("Actualités"); }
        public bool IsTopArticle { get; set; }
        public HtmlNode Node { get; set; }
        /// <summary>
        /// On veut une mesure comparative de la taille des articles
        /// </summary>
        public int HeigthRelativeMeasure { get => Content.Length; }

        private bool _isChecked;
        public bool IsChecked
        {
            get { return _isChecked; }
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    OnPropertyChanged(nameof(IsChecked));
                }
            }
        }

        // Implémentation de INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
