using MadininApp.Objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace MadininApp.Services
{
    public class FontStyledText
    {
        public int FontWeight;
        public double FontSize;
        public string FontColor;
        public string Text;
        public static FontStyledText GetFontStyledTextFromRun(Run run)
        {
            // recupération text,font-size,font-weight,color
            TextRange textRange = new TextRange(run.ContentStart, run.ContentEnd);
            string text = textRange.Text;
            // Récupérer la taille de police
            double fontSize = run.FontSize;
            int fontWeight = run.FontWeight == FontWeights.Bold ? 700 : 100;
            // Récupérer la couleur (si elle est définie)
            string textColor = "black";
            if (run.Foreground is SolidColorBrush brush)
            {
                if (brush.Color != Colors.Black)
                {
                    textColor = brush.Color == Colors.Red ? "red" : "green";
                }
            }

            return new FontStyledText()
            {
                FontWeight = fontWeight,
                FontSize = fontSize,
                FontColor = textColor,
                Text = text.Replace("\n", "").Replace("\r", "")
            };
        }
        public static bool StyleChangeDetected(FontStyledText txt1, FontStyledText txt2)
        {
            return txt1.FontWeight != txt2.FontWeight || txt1.FontSize != txt2.FontSize || txt1.FontColor != txt2.FontColor;
        }
    }

    internal static class HtmlGenerator
    {
        private static readonly string _path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LaLettre.html");
        private static readonly string _outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"LaLettreFinale.html");
        private static readonly string _technoImgSrc = "https://www.madinin-art.net/wp-content/uploads/2014/06/messageries.jpg";
        private static readonly string _technoCategory = "Technologies";
        private static readonly string _technoTitle = "Comment mettre La Lettre de Madinin'Art en liste blanche";
        private static readonly string _technoHtmlContent =
            @"<p>
            Même si vous êtes abonné à La Lettre de Madinin’Art, elle peut souvent arriver par erreur dans le dossier spam de votre messagerie ! Afin d’éviter cela, vous devez placer l’adresse de l’expéditeur dans votre liste blanche. La procédure diffère légèrement selon le programme ou service de messagerie que vous utilisez.
                <p><a class=""read-more"" href=""https://www.madinin-art.net/comment-mettre-la-lettre-de-madininart-en-liste-blanche/"">Lire Plus</a></p>
            </p>";

        /// <summary>
        /// L'objectif de cette méthode est de réordonner la selection d'article
        /// afin que l'ordre d'affichage sur le rendu html soit cohérent avec l'ordre d'apparition des articles sur la page d'accueil
        /// </summary>
        /// <param name="articles"></param>
        /// <returns></returns>
        private static List<MadinArticle> OrderSelectedArticle(IEnumerable<MadinArticle> articles, out int indexCoupure)
        {
            List<MadinArticle> orderedList = new List<MadinArticle>();
            var leftColulmnArticles = articles.Where((art, i) => i % 2 == 0);
            var rightColulmnArticles = articles.Where((art, i) => i % 2 == 1);
            orderedList.AddRange(leftColulmnArticles);
            orderedList.AddRange(rightColulmnArticles);

            indexCoupure = orderedList.IndexOf(rightColulmnArticles.First());
            return orderedList;
        }

        public static string CreateHtmlFile(List<MadinArticle> articles, BlockCollection leMotDuChef)
        {
            var articlesSelected = OrderSelectedArticle(articles.Where(a => !a.IsTopArticle), out int indexCoupure);
            List<MadinArticle> lastArticles = new List<MadinArticle>();
            var topArticle = articles.SingleOrDefault(a => a.IsTopArticle);
            var technoArticle = new MadinArticle()
            {
                Author = "",
                Category = _technoCategory,
                Content = "",
                Date = "",
                HtmlContent = _technoHtmlContent,
                ImageUrl = _technoImgSrc,
                IsChecked = true,
                Node = null,
                Title = _technoTitle,
                IsTopArticle = false
            };
            articlesSelected.Add(technoArticle);
            if (leMotDuChef.Any())
            {
                var articleDuChef = CreateMadinArticleFromBlockCollection(leMotDuChef);
                lastArticles.Add(articleDuChef);
            }

            // Charger le template HTML
            string htmlTemplate = File.ReadAllText(_path);

            // Construire les articles HTML
            StringBuilder articlesHtml = new StringBuilder();

            double articlesHeight = articlesSelected.Sum(a => a.HeigthRelativeMeasure);
            double columnHeight = articlesHeight / 2;
            // on cherche l'article qui entraine le depassement de capacité de la premiere colonne
            MadinArticle overflowArticle = new MadinArticle();
           // double heightAccumulator = 0;
            /*foreach(var article in articlesSelected)
            {
                heightAccumulator+= article.HeigthRelativeMeasure;
                if(heightAccumulator > columnHeight)
                {
                    overflowArticle = article;
                    articlesSelected.Remove(overflowArticle);
                    break;
                }
            }*/


            foreach (var article in articlesSelected)
            {
                string classe = article.IsActualite ? " article actualite" : "article";
                var categoryLinks = "";
                if (article.Categories != null)
                {
                    categoryLinks = String.Join(",", article.Categories.Select(cat => $"<span><a href = \"{cat.Url}\">{cat.Name}</a></span>"));
                }
                string html = $" <div class=\"{classe}\"><h4>{categoryLinks}</h4><h2>{article.Title}</h2><h3>{article.Author}</h3><img src=\"{article.ImageUrl}\"><p>{article.HtmlContent}</p></div>";
                articlesHtml.Append(html);
            }

            StringBuilder lastArticlesHtml = new StringBuilder();

            foreach (var article in lastArticles)
            {
                string classe = "article";
                lastArticlesHtml.Append($"<div class=\"{classe}\"><h4>{article.Category}</h4><h2>{article.Title}</h2><h3>{article.Author}</h3><img src=\"{article.ImageUrl}\"><p>{article.HtmlContent}</p></div>");
            }

            // Insérer les articles dans le template
            htmlTemplate = htmlTemplate.Replace("<!-- Articles Here -->", articlesHtml.ToString());
            htmlTemplate = htmlTemplate.Replace(" <!-- Two Articles Here -->", lastArticlesHtml.ToString());


            string topArticlesHtml = ($"<h3>{topArticle.Category}</h3><h1>{topArticle.Title}</h1><h4>{topArticle.Author}</h4><img src=\"{topArticle.ImageUrl}\"><p>{topArticle.HtmlContent}</p>");
            htmlTemplate = htmlTemplate.Replace("<!-- Top Article Here -->", topArticlesHtml.ToString());
            // Sauvegarder le fichier HTML résultant
            File.WriteAllText(_outputPath, htmlTemplate);

            if (!String.IsNullOrEmpty(overflowArticle.Title))
            {
                string classe = overflowArticle.IsActualite ? " article actualite overflow" : "article overflow";
                var categoryLinks = "";
                if (overflowArticle.Categories != null)
                {
                    categoryLinks = String.Join(",", overflowArticle.Categories.Select(cat => $"<span><a href = \"{cat.Url}\">{cat.Name}</a></span>"));
                }
                string overflowHtml = $" <div class=\"{classe}\"><h4>{categoryLinks}</h4><h2>{overflowArticle.Title}</h2><h3>{overflowArticle.Author}</h3><img src=\"{overflowArticle.ImageUrl}\"><p>{overflowArticle.HtmlContent}</p></div>";
                htmlTemplate = htmlTemplate.Replace("<!--overflow article here-->", overflowHtml.ToString());
            }
          /*  Window w = new Window();
            w.WindowState = WindowState.Maximized;
            WebView2 b = new WebView2();

            await b.EnsureCoreWebView2Async(null);
            
            b.CoreWebView2.NavigateToString(htmlTemplate);
            w.Content = b;
            w.Show();*/

            return htmlTemplate;
        }
        private static MadinArticle CreateMadinArticleFromBlockCollection(BlockCollection leMotDuChef)
        {
            MadinArticle article = new MadinArticle()
            {
                Author = "",
                Category = "",
                Date = "",
                Content = "",
                ImageUrl = "",
                Node = null,
                Title = ""
            };
            StringBuilder sb = new StringBuilder();
            foreach (var block in leMotDuChef)
            {

                if (block is Paragraph paragraph)
                {
                    string textALign;
                    switch (paragraph.TextAlignment)
                    {
                        case System.Windows.TextAlignment.Left:
                            textALign = "left";
                            break;
                        case System.Windows.TextAlignment.Right:
                            textALign = "right";
                            break;
                        case System.Windows.TextAlignment.Center:
                            textALign = "center";
                            break;
                        default:
                            textALign = "left";
                            break;
                    }
                    sb.AppendLine($"<p style=\"text-align:{textALign};\">");
                    sb.AppendLine($"<span style=\"[autreStyle]\">");

                    // Parcourir chaque Inline (Run) dans le paragraphe
                    Run firstRun = paragraph.Inlines.OfType<Run>().FirstOrDefault();
                    if (firstRun == null) continue;

                    FontStyledText previousContent = FontStyledText.GetFontStyledTextFromRun(firstRun);
                    bool lineBreak = false;
                    foreach (Inline inline in paragraph.Inlines)
                    {
                        if (inline is LineBreak)
                        {
                            sb.Append("</span><br>");
                            lineBreak = true;
                            continue;
                        }
                        if (inline is Run run)
                        {
                            FontStyledText content = FontStyledText.GetFontStyledTextFromRun(run);
                            bool styleBreakDetected = FontStyledText.StyleChangeDetected(previousContent, content);
                            previousContent = content;
                            // si on detecte un changement de style , on doit fermer le span et en ouvrir un nouveau
                            // dans les deux cas on met à jour le style du dernier span non fermé
                            if (styleBreakDetected)
                            {
                                if (!lineBreak)
                                {
                                    sb.Append("</span>");
                                    lineBreak = false;
                                }

                                sb.AppendLine("<span style=\"[autreStyle]\">");
                            }
                            string style = $"font-weight:{content.FontWeight};font-size:{content.FontSize}px; color:{content.FontColor};";
                            sb.Replace("[autreStyle]", style);
                            sb.Append(content.Text);

                        }


                    }
                }
                sb.AppendLine("</p>");
            }
            article.HtmlContent = sb.ToString();
            return article;
        }

    }

}

