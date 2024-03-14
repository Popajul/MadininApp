using MadininApp.Objects;
using MadininApp.Services;
using MadininApp.User_Control;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;

namespace MadininApp.ViewModel
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private void UpdateTopArticle()
        {
            if (!OtherArticles.Any(a => a.Title == _topArticle.Title))
            {
                _topArticle.IsTopArticle = false;
                _topArticle.IsChecked = true;
                var newTopArticle = DataCollection.FirstOrDefault(a => a.IsTopArticle) ?? MadinArticle.GetPlaceHolderArticle();
                newTopArticle.IsChecked = true;
                var topArticleIsRemoveFromOther = OtherArticles.Remove(newTopArticle);
                if (topArticleIsRemoveFromOther)
                {
                    OtherArticles.Insert(0, _topArticle);
                    OnPropertyChanged(nameof(OtherArticles));
                    TopArticle = newTopArticle;
                }

            }

        }
        private ObservableCollection<MadinArticle> _dataCollection;

        public ObservableCollection<MadinArticle> DataCollection
        {
            get { return _dataCollection; }
            set
            {
                _dataCollection = value;
                OnPropertyChanged(nameof(DataCollection));
            }
        }

        private MadinArticle _topArticle;
        public MadinArticle TopArticle
        {
            get
            {
                if (_topArticle == null)
                {
                    _topArticle = DataCollection.FirstOrDefault(a => a.IsTopArticle) ?? MadinArticle.GetPlaceHolderArticle();
                }
                return _topArticle;
            }
            set
            {
                _topArticle = value;
                OnPropertyChanged(nameof(TopArticle));
            }
        }

        private ObservableCollection<MadinArticle> _otherArticles;
        public ObservableCollection<MadinArticle> OtherArticles
        {
            get
            {
                if (_otherArticles == null)
                {
                    _otherArticles = new ObservableCollection<MadinArticle>(DataCollection.Where(a => !a.IsTopArticle));
                }
                return _otherArticles;
            }
            set
            {
                _otherArticles = value;
                OnPropertyChanged(nameof(OtherArticles));
            }
        }

    
        private ObservableCollection<string> _categories;
        public ObservableCollection<string> Categories
        {
            get
            {
                if (_categories == null)
                {
                    _categories = new ObservableCollection<string>(DataCollection.Select(a=>a.Category).Distinct());
                }
                return _categories;
            }
            set
            {
                _categories = value;
                OnPropertyChanged(nameof(Categories));
            }
        }

        private decimal _maxWidth => (decimal)SystemParameters.PrimaryScreenWidth;
        public decimal MaxWidth 
        {
            get
            {
                return _maxWidth;
            }
            set 
            { 
                OnPropertyChanged(nameof(MaxWidth));
            }
        }

        private decimal _maxHeight => (decimal)SystemParameters.PrimaryScreenHeight - 20;
        public decimal MaxHeight
        {
            get
            {
                return _maxHeight;
            }
            set
            {
                OnPropertyChanged(nameof(MaxHeight));
            }
        }

        private decimal _maxArticlesScrollHeight => (decimal)SystemParameters.PrimaryScreenHeight - 90;
        public decimal MaxArticlesScrollHeight
        {
            get
            {
                return _maxArticlesScrollHeight;
            }
            set
            {
                OnPropertyChanged(nameof(MaxArticlesScrollHeight));
            }
        }

        private decimal _maxCatHeight => (decimal)SystemParameters.PrimaryScreenHeight - 400;
        public decimal MaxCatHeight
        {
            get
            {
                return _maxCatHeight;
            }
            set
            {
                OnPropertyChanged(nameof(MaxCatHeight));
            }
        }

        public MainViewModel()
        {
            DataCollection = new ObservableCollection<MadinArticle>();

            var task = Task.Run(() => LoadDataAsync());

            task.Wait();
            foreach (var article in DataCollection)
            {
                article.PropertyChanged += Article_PropertyChanged;
            }
        }

        public static async Task<MainViewModel> CreateModel()
        {
            var mvm = new MainViewModel();
            await mvm.LoadDataAsync();
            return mvm;
        }

        private async Task LoadDataAsync()
        {
            var data = await MadinScrapper.GetMadinArticlesFromScrap();

            foreach (var item in data)
            {
                DataCollection.Add(item);
            }
        }
        private void Article_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MadinArticle.IsTopArticle))
            {
                UpdateTopArticle();
            }
        }
        // Implémentation de INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (propertyName == "TopArticle")
            {

            }
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string SaveData()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            return JsonSerializer.Serialize(DataCollection, options);
            
        }

        public MainViewModel LoadData(string cheminFichier)
        {
            if (!string.IsNullOrEmpty(cheminFichier))
            {
                string jsonString = File.ReadAllText(cheminFichier);
                var data = JsonSerializer.Deserialize<ObservableCollection<MadinArticle>>(jsonString);
                if(data.Count%2 == 0)
                {
                    data.Add(MadinArticle.GetPlaceHolderArticle());
                }
                return new MainViewModel { DataCollection =  data};
            }
            return this;
        }
    }

}
