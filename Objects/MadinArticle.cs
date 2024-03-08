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
        public MadinArticle()
        {
            IsPlaceHolder = false;
        }

        public static MadinArticle GetPlaceHolderArticle()
        {
            var article = new MadinArticle()
            {
                IsPlaceHolder = true,
                Title = "",
                Content = "",
                Date = "",
                Author = "",
                ImageUrl = "https://www.madinin-art.net/images/logo_la_lettre_blanc.jpg",
                HtmlContent = "",
                MadinUrl = "",
                Node = null,
                IsTopArticle = false,
                IsChecked = true,
                Category = "",
                Categories = new List<Category>()

            };


            return article;
        }
        public string Content { get; set; }
        public string Category { get; set; }
        public IEnumerable<Category> Categories { get; set; }
        public string Date { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string ImageUrl { get; set; }
        public string HtmlContent { get; set; }
        public bool IsActualite { get => Title?.Contains("Actualités") ?? false; }
        private bool _isTopArticle;
        public bool IsTopArticle
        {
            get { return _isTopArticle; }
            set
            {
                if (_isTopArticle != value)
                {
                    _isTopArticle = value;
                    OnPropertyChanged(nameof(IsTopArticle));
                }
            }
        }
        public string MadinUrl { get; set; }
        public HtmlNode Node { get; set; }
        public bool IsPlaceHolder { get; set; } = false;
        public bool IsNotPlaceHolder { get => !IsPlaceHolder; }


        /// <summary>
        /// On veut une mesure comparative de la taille des articles
        /// </summary>
        public int HeigthRelativeMeasure { get => Content?.Length ?? 0; }

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
