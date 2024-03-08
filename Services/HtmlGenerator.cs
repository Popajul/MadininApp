using HtmlAgilityPack;
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
        private static string GenerateHTMLTable(List<string> dataList)
        {
            // Créer un objet StringBuilder pour construire le code HTML de la table
            StringBuilder htmlBuilder = new StringBuilder();

            // Ouvrir la balise de début de la table
            htmlBuilder.AppendLine("<table>");
            int length = dataList.Count;
            /*int l = length%2 == 0 ? length / 2 : length / 2 + 1;*/
            // Itérer sur la liste de données
            for (int i = 0; i < length; i+=2)
            {
                // Ouvrir une nouvelle ligne de tableau
                htmlBuilder.AppendLine("<tr>");

                // Ajouter les balises de cellules de données avec les identifiants correspondants aux index
                htmlBuilder.AppendLine($"<td id=\"{i}\"></td>");
                htmlBuilder.AppendLine($"<td id=\"{i + 1}\"></td>");

                // Fermer la ligne de tableau
                htmlBuilder.AppendLine("</tr>");
            }

            // Fermer la balise de fin de la table
            htmlBuilder.AppendLine("</table>");

            // Retourner le code HTML de la table
            return htmlBuilder.ToString();
        }

        private static string FillHTMLTable(List<string> articlesHtml, string htmltable)
        {
            int length = articlesHtml.Count;
            int l = length == 0 ? length / 2 : length / 2 + 1;
            // Itérer sur la liste de nœuds HTML
            for (int i = 0; i < articlesHtml.Count; i++)
            {
                // Ajouter le contenu des nœuds aux balises de cellules de données correspondantes
                htmltable = htmltable.Replace($"<td id=\"{i}\"></td>", $"<td id=\"{i}\">{articlesHtml[i]}</td>");
            }

            return htmltable;
        }

        public static string CreateHtmlFile(List<MadinArticle> articles,MadinArticle topArticle, BlockCollection leMotDuChef, string positionBasLettre)
        {
            var articlesSelected = articles.Where(a => !a.IsTopArticle).ToList();
            List<MadinArticle> lastArticles = new List<MadinArticle>();
            
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
            
            if (leMotDuChef.Any())
            {
                var articleDuChef = CreateMadinArticleFromBlockCollection(leMotDuChef,positionBasLettre);
                lastArticles.Add(articleDuChef);
            }

            // On génere un nouvel article contenant les messages de fin de lettre et on l'ajoute à la liste des derniers articles
            var articleFinLettre1 = new MadinArticle()
            {
                Author = "",
                Category = "",
                Content = "",
                Date = "",
                HtmlContent = "<span style=\"color: red;\">La lettre de Madinin'Art est publié le 1er, le 10 et le 20 de chaque mois</span>",
                ImageUrl = "",
                IsChecked = true,
                Node = null,
                Title = "",
                IsTopArticle = false
            };
            var articleFinLettre2 = new MadinArticle()
            {
                Author = "",
                Category = "",
                Content = "",
                Date = "",
                HtmlContent = "<span style=\"color:black;\">Le site de Madinin'Art</span>\r\n<span style=\"color:red;\">est mis à jour</span>\r\n<span style=\"color:green;\">continûment</span>",
                ImageUrl = "",
                IsChecked = true,
                Node = null,
                Title = "",
                IsTopArticle = false
            };
            lastArticles.Add(articleFinLettre1);
            lastArticles.Add(articleFinLettre2);

            // Charger le template HTML
            string htmlTemplate = File.ReadAllText(_path);

            
           /* MadinArticle overflowArticle = new MadinArticle();*/

            StringBuilder lastArticlesHtml = new StringBuilder();
            foreach (var article in lastArticles)
            {
                var htmlImage = string.IsNullOrEmpty(article.ImageUrl) ? "" : $"<img src=\"{article.ImageUrl}\">";
                string classe = positionBasLettre == "en dessous" ? "footer-element" : "article";
                
                lastArticlesHtml.Append($"<div class=\"{classe}\"><h4>{article.Category}</h4><h2><a href={article.MadinUrl}>{article.Title}</a></h2><h3>{article.Author}</h3>{htmlImage}<p>{article.HtmlContent}</p></div>");
            }

            
            if (positionBasLettre == "à gauche")
            {
                // On place le techno article et les lastArticles à gauche
                var fakeArticle = articlesSelected.FirstOrDefault(a=>a.IsPlaceHolder);
                if (fakeArticle == null)
                {
                    positionBasLettre = "en dessous";
                }
                else
                {
                    // On réuni le html content 
                    var htmlContent = lastArticlesHtml.ToString();

                    technoArticle.HtmlContent += htmlContent;
                    var fakeIndex = articlesSelected.IndexOf(fakeArticle);
                    articlesSelected[fakeIndex] =technoArticle;
                }
            }
            if (positionBasLettre == "à droite")
            {
                // On réuni le html content 
                var htmlContent = lastArticlesHtml.ToString();

                technoArticle.HtmlContent += htmlContent;
                articlesSelected.Add(technoArticle);
            }
            if(positionBasLettre == "à droite séparé de l'article techno")
            {
                articlesSelected.Add(technoArticle);
                // On réuni le html content 
                var htmlContent = lastArticlesHtml.ToString();
                var nextArticle = MadinArticle.GetPlaceHolderArticle();
                nextArticle.HtmlContent = htmlContent;
                nextArticle.ImageUrl = "";
                articlesSelected.Add(nextArticle);

            }
            if (positionBasLettre == "en dessous")
            {
                articlesSelected.Add(technoArticle);
            }

            // Construire les articles HTML
            List<string> articlesHtml = new List<string>();
            foreach (var article in articlesSelected)
            {
                string classe = article.IsActualite ? " article actualite" : "article";
                var categoryLinks = "";
                if (article.Categories != null)
                {
                    categoryLinks = String.Join(",", article.Categories.Select(cat => $"<span><a href = \"{cat.Url}\">{cat.Name}</a></span>"));
                }
                var htmlImage = string.IsNullOrEmpty(article.ImageUrl) ? "" : $"<img src=\"{article.ImageUrl}\">";
                string html = $" <div class=\"{classe}\"><h4>{categoryLinks}</h4><h2><a href={article.MadinUrl}>{article.Title}</a></h2><h3>{article.Author}</h3>{htmlImage}<p>{article.HtmlContent}</p></div>";
                articlesHtml.Add(html);
            }


            // Remplir le tableau HTML avec les articles
            string htmlTable = GenerateHTMLTable(articlesHtml);
            string articlesHtmlTable = FillHTMLTable(articlesHtml, htmlTable);
            

           

            // Insérer les articles dans le template
            htmlTemplate = htmlTemplate.Replace("<!-- Articles Here -->", articlesHtmlTable);
            htmlTemplate = htmlTemplate.Replace(" <!-- Two Articles Here -->", lastArticlesHtml.ToString());


            string topArticlesHtml = ($"<h3>{topArticle.Category}</h3><h1><a href={topArticle.MadinUrl}>{topArticle.Title}</a></h1><h4>{topArticle.Author}</h4><img src=\"{topArticle.ImageUrl}\"><p>{topArticle.HtmlContent}</p>");
            htmlTemplate = htmlTemplate.Replace("<!-- Top Article Here -->", topArticlesHtml.ToString());

            if(positionBasLettre == "en dessous")
            {
                htmlTemplate = htmlTemplate.Replace("<!--footer element here-->", lastArticlesHtml.ToString());
            }
            // Sauvegarder le fichier HTML résultant
            File.WriteAllText(_outputPath, htmlTemplate);


            return htmlTemplate;
        }
        private static MadinArticle CreateMadinArticleFromBlockCollection(BlockCollection leMotDuChef, string positionBasLettre)
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
                    if(positionBasLettre == "en dessous")
                        textALign = "center";

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
                            sb.Append("</span><br><span style=\"[autreStyle]\">");
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
                                    sb.AppendLine("<span style=\"[autreStyle]\">");
                                }

                                
                            }
                            string style = $"font-weight:{content.FontWeight};font-size:{content.FontSize}pt; color:{content.FontColor};";
                            sb.Replace("[autreStyle]", style);
                            sb.Append(content.Text);

                        }


                    }
                }
                sb.AppendLine("</span></p>");
            }
            article.HtmlContent = sb.ToString();
            return article;
        }

    }

}

