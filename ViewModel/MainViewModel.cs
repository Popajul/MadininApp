using MadininApp.Objects;
using MadininApp.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace MadininApp.ViewModel
{
    public class MainViewModel : INotifyPropertyChanged
    {
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

        // Constructeur
        public MainViewModel()
        {
            DataCollection = new ObservableCollection<MadinArticle>();
            LoadDataAsync();
        }

        private async void LoadDataAsync()
        {
            var data = await MadinScrapper.GetMadinArticlesFromScrap();

            foreach (var item in data)
            {
                DataCollection.Add(item);
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
