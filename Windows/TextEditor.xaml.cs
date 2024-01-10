using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace MadininApp.Windows
{
    /// <summary>
    /// Logique d'interaction pour TextEditor.xaml
    /// </summary>
    public partial class TextEditorWindow : Window
    {
        private readonly FontFamily _defaultFontFamily = new FontFamily("Verdana");
        private readonly double _defaultFontSize = 16.0;
        private readonly FontWeight _defaultFontWeight = FontWeights.Normal;
        private readonly SolidColorBrush _defaultFontColor = Brushes.Black;
        private readonly HorizontalAlignment _defaultHorizontalAlignement = HorizontalAlignment.Left;
        public TextEditorWindow()
        {
            InitializeComponent();
            //Gestion de la fermeture de la fenêtre
            this.Closing += new System.ComponentModel.CancelEventHandler(Window_Closing);

            //Nota Bene
            notabene.Text =
                "Passer à la ligne : shift + entrée\nNouveau paragraphe: entrée\nles traits horizontaux délimitent les paragraphes\nils n'apparaissent pas dans le rendu html";
            notabene.Background = Brushes.WhiteSmoke;

            // On force les dimensions de la fenetre pour correspondre à la largeur d'un article
            // On empêche le redimensionnement
            this.Width = 400;
            this.MinWidth = 400;
            this.MaxWidth = 400;
            this.ResizeMode = ResizeMode.NoResize;

            // Initialisation de l'éditeur 

            txtEditor.FontFamily = _defaultFontFamily;
            txtEditor.FontSize = _defaultFontSize;
            txtEditor.FontWeight = _defaultFontWeight;
            txtEditor.Foreground = _defaultFontColor;
            txtEditor.HorizontalContentAlignment = _defaultHorizontalAlignement;
            txtEditor.Document.Blocks.Clear();

            var paragraph = new Paragraph
            {
                FontFamily = _defaultFontFamily,
                FontSize = _defaultFontSize,
                FontWeight = _defaultFontWeight,
                Foreground = _defaultFontColor
            };

            txtEditor.Document.Blocks.Add(paragraph);


            // Initialisation des controle de font de l'éditeur
            fontSizeComboBox.Items.Add(16);
            fontSizeComboBox.Items.Add(24);
            fontSizeComboBox.SelectedIndex = 0;
            radioBlack.IsChecked = true;
            alignLeft.IsChecked = true;
        }
        #region "Event Handler"
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true; // Annule la fermeture de la fenêtre

            this.Hide();
        }


        private async void TxtEditor_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {


            // Vous devrez peut-être retarder l'exécution jusqu'à ce que le nouveau caret position soit établi
            await Task.Delay(100);
            UpdateFontControlSelection();

        }

        private void OnAlignmentChanged(object sender, RoutedEventArgs e)
        {
            if (!(sender is RadioButton radioButton) || txtEditor.Selection == null) return;

            HorizontalAlignment alignment = HorizontalAlignment.Left;
            TextAlignment textAlignment = TextAlignment.Left;
            if (radioButton == alignCenter)
            {
                alignment = HorizontalAlignment.Center;
                textAlignment = TextAlignment.Center;
            }

            else if (radioButton == alignRight)
            {
                alignment = HorizontalAlignment.Right;
                textAlignment = TextAlignment.Right;
            }


            txtEditor.Selection.ApplyPropertyValue(Paragraph.TextAlignmentProperty, textAlignment);
            txtEditor.HorizontalContentAlignment = alignment;
        }

        private void OnToggleBold(object sender, RoutedEventArgs e)
        {
            if (txtEditor.Selection == null) return;

            var fontWeight = (sender as CheckBox).IsChecked == true ? FontWeights.Bold : FontWeights.Normal;
            txtEditor.Selection.ApplyPropertyValue(TextElement.FontWeightProperty, fontWeight);
            txtEditor.FontWeight = fontWeight;
        }

        private void OnNewParagraph(object sender, RoutedEventArgs e)
        {
            Paragraph newParagraph = new Paragraph();
            txtEditor.Document.Blocks.Add(newParagraph);
            txtEditor.CaretPosition = newParagraph.ContentStart;
        }

        private void FontSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (fontSizeComboBox.SelectedItem != null)
            {
                double fontSize = double.Parse(fontSizeComboBox.SelectedItem.ToString());
                txtEditor.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, fontSize);
                txtEditor.FontSize = fontSize;
            }
        }
        private void OnColorChecked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton)
            {
                SolidColorBrush color = Brushes.Black;

                if (radioButton == radioRed)
                {
                    color = Brushes.Red;
                }
                else if (radioButton == radioGreen)
                {
                    color = Brushes.Green;
                }

                txtEditor.Selection.ApplyPropertyValue(TextElement.ForegroundProperty, color);
                txtEditor.Foreground = color;
            }
        }

        private void OnValiderClick(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void OnAnnulerClick(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }
        #endregion
        #region "Functions"
        private void UpdateFontControlSelection()
        {
            if (txtEditor.Selection != null)
            {
                object fontSize = txtEditor.Selection.GetPropertyValue(TextElement.FontSizeProperty);
                object fontWeight = txtEditor.Selection.GetPropertyValue(TextElement.FontWeightProperty);
                object color = txtEditor.Selection.GetPropertyValue(TextElement.ForegroundProperty);
                object txtAlign = txtEditor.Selection.GetPropertyValue(Paragraph.TextAlignmentProperty);
                if (fontSize is double fs && !double.IsNaN((double)fontSize))
                {
                    fontSizeComboBox.SelectedIndex = fs == 16 ? 0 : 1;
                }
                if (fontWeight is FontWeight fw)
                {
                    toggleBold.IsChecked = fw == FontWeights.Bold;
                }
                if (color is SolidColorBrush scb)
                {
                    Color brushColor = scb.Color;

                    if (brushColor == Colors.Red)
                    {
                        radioRed.IsChecked = true;
                    }
                    else if (brushColor == Colors.Green)
                    {
                        radioGreen.IsChecked = true;
                    }
                    else
                    {
                        radioBlack.IsChecked = true;
                    }


                }
                if (txtAlign is TextAlignment ta)
                {
                    switch (ta)
                    {
                        case TextAlignment.Center:
                            alignCenter.IsChecked = true;
                            break;
                        case TextAlignment.Right:
                            alignRight.IsChecked = true;
                            break;
                        default:
                            alignLeft.IsChecked = true;
                            break;
                    }
                }

            }
        }
        #endregion


    }
}
