using HtmlAgilityPack;
using MadininApp.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;

namespace MadininApp.Services
{

    internal static class MadinScrapper
    {
        private const string URL = "https://www.madinin-art.net/";
        private static readonly string _technoImgSrc = "https://www.madinin-art.net/wp-content/uploads/2014/06/messageries.jpg";
        private static readonly string _technoCategory = "Technologies";
        private static readonly string _technoTitle = "Comment mettre La Lettre de Madinin'Art en liste blanche";
        private static readonly string _technoHtmlContent =
            @"<p>
            Même si vous êtes abonné à La Lettre de Madinin’Art, elle peut souvent arriver par erreur dans le dossier spam de votre messagerie ! Afin d’éviter cela, vous devez placer l’adresse de l’expéditeur dans votre liste blanche. La procédure diffère légèrement selon le programme ou service de messagerie que vous utilisez.
                <p><a class=""read-more"" href=""https://www.madinin-art.net/comment-mettre-la-lettre-de-madininart-en-liste-blanche/"">Lire Plus</a></p>
            </p>";
        /// <summary>
        /// Extraction des catégories et de leurs urls d'un document html
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        private static Dictionary<string, string> CategoriesExtract(HtmlDocument document)
        {
            var categories = new Dictionary<string, string>();

            // Selection des nodes
            var categoryNodes = document.DocumentNode.SelectNodes("//*[contains(@class, 'post-category')]//a");

            if (categoryNodes != null)
            {
                foreach (var node in categoryNodes)
                {
                    string categoryName = node.InnerText.Trim();
                    string categoryUrl = node.GetAttributeValue("href", string.Empty);

                    if (!string.IsNullOrEmpty(categoryName) && !string.IsNullOrEmpty(categoryUrl))
                    {
                        categories[categoryName] = categoryUrl;
                    }
                }
            }

            return categories;
        }
        /// <summary>
        /// Représente l'objet resultant du scrapping
        /// ie : HtmlNodes des articles et dicionaires des Urls des catégories
        /// </summary>
        private class ScrappingResult
        {
            public HtmlNodeCollection Articles;
            public Dictionary<string, string> CategoryUrlByName;
        }

        /// <summary>
        /// Emplissage d'un HtmlDocument avec la réponse de la WebRequest
        /// </summary>
        /// <param name="htmlDoc"></param>
        /// <returns></returns>
        private static async Task<bool> FromHttpRequestToHtmlDocumentLoad(HtmlDocument htmlDoc)
        {
            string responseBody = "";
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync(URL);
                    response.EnsureSuccessStatusCode();

                    byte[] responseBytes = await response.Content.ReadAsByteArrayAsync();
                    Encoding encoding = Encoding.UTF8;
                    responseBody = encoding.GetString(responseBytes);
                }
            }
            catch (HttpRequestException)
            {
                return false;
            }

            if (String.IsNullOrEmpty(responseBody)) return false;
            htmlDoc.LoadHtml(responseBody);
            return true;
        }

        /// <summary>
        /// Récupération de l'HtmlDocument et Extraction du ScrappingResult
        /// </summary>
        /// <returns></returns>
        private static async Task<ScrappingResult> GetScrappingResult()
        {

            HtmlDocument htmlDoc = new HtmlDocument();
            bool completionOk = await FromHttpRequestToHtmlDocumentLoad(htmlDoc);
            if (!completionOk) return null;

            var categoryUrlByName = CategoriesExtract(htmlDoc);
            var articles = htmlDoc.DocumentNode.SelectNodes("//article");

            if (articles != null)
            {
                var result = new ScrappingResult { Articles = articles, CategoryUrlByName = categoryUrlByName };
                return result;
            }
            return null;
        }

        internal static async Task<List<MadinArticle>> GetMadinArticlesFromScrap()
        {
            ScrappingResult scrappingResult = await GetScrappingResult();

            List<MadinArticle> madinArticles = new List<MadinArticle>();
            if (scrappingResult == null && scrappingResult.Articles == null)
            {
                return madinArticles;
            }

            HtmlNodeCollection articlesNodes = scrappingResult.Articles;
            Dictionary<string, string> urlCategoryByNames = scrappingResult.CategoryUrlByName;

            foreach (var node in articlesNodes)
            {
                // Category et category Url
                var categoryNode = node.SelectSingleNode($"(.//*[contains(concat(' ', normalize-space(@class), ' '), ' post-category ')])");
                var category = categoryNode == null ? "" : HtmlEntity.DeEntitize(categoryNode.InnerText).Trim();

                var cats = category.Split(',');
                var categories = new List<Category>();
                foreach (var catName in cats)
                {
                    urlCategoryByNames.TryGetValue(catName.Trim(), out string url);
                    categories.Add(
                        new Category()
                        {
                            Name = catName,
                            Url = url
                        }
                        );
                }

                // Title
                var titleNode = node.SelectSingleNode($"(.//*[contains(concat(' ', normalize-space(@class), ' '), ' entry-title ')])");
                var title = titleNode == null ? "" : HtmlEntity.DeEntitize(titleNode.InnerText).Trim();
                if(title.Contains("retraite"))
                {
                  
                }
                // Date
                var dateNode = node.SelectSingleNode($"(.//*[contains(concat(' ', normalize-space(@class), ' '), ' published updated ')])");
                var date = dateNode == null ? "" : HtmlEntity.DeEntitize(dateNode.InnerText).Trim();
                // Content
                var contentNode = node.SelectSingleNode($"(.//*[contains(concat(' ', normalize-space(@class), ' '), ' entry-content ')])");
                var content = contentNode == null ? "" : HtmlEntity.DeEntitize(contentNode.InnerText).Trim();

                // Subtitle premier span de entry-content
                var firstSpan = contentNode?.SelectSingleNode(".//span");
                var subtitle = firstSpan == null ? "" : HtmlEntity.DeEntitize(firstSpan.InnerText).Trim();


                // Image
                var imgNode = contentNode?.SelectSingleNode($"(.//div[@class='excerpt']//img)");
                var src = imgNode?.GetAttributeValue("src", string.Empty) ?? "";
                // Author
                var author = "";
                if (string.IsNullOrEmpty(content))
                    continue;

                int startDashIndex = content.IndexOf("—");
                int endDashIndex = content.LastIndexOf("—");

                if (startDashIndex != -1 && endDashIndex != -1 && endDashIndex > startDashIndex)
                {
                    author = content.Substring(startDashIndex + 1, endDashIndex - startDashIndex - 1).Trim();
                }
                // Décoder les entités HTML dans le contenu
                Regex htmlEntityRegex = new Regex("&##[^;]+;|&#[^;]+;|&[A-Za-z]+;");
                MatchCollection matches = htmlEntityRegex.Matches(author);

                // Parcourir toutes les correspondances et les décoder
                foreach (Match match in matches)
                {
                    string encodedEntity = match.Value;
                    encodedEntity = encodedEntity.Replace("##", "#");
                    string decodedEntity = HttpUtility.HtmlDecode(encodedEntity);

                    // Remplacez l'entité encodée par sa forme décodée dans le contenu
                    author = author.Replace(encodedEntity, decodedEntity);
                }
                var madinUrl = contentNode.SelectSingleNode($"(.//*[contains(concat(' ', normalize-space(@class), ' '), ' read-more ')])")?.GetAttributeValue("href", string.Empty) ?? "";
                // Nettoyage
                var cleanedContentNode = NewNodeWithoutAuthorAndImg(contentNode, author);
                bool isActualité = title.Contains("Actualités");
                if (isActualité)
                {
                    RemoveStylesRecursively(cleanedContentNode);
                    RemoveEmptyLinks(cleanedContentNode);
                }
                var htmlContent = cleanedContentNode == null ? "" : HtmlEntity.DeEntitize(cleanedContentNode.InnerHtml);

                content = cleanedContentNode == null ? "" : HtmlEntity.DeEntitize(cleanedContentNode.InnerText);

                madinArticles.Add(
                    new MadinArticle
                    {
                        Content = content,
                        HtmlContent = htmlContent,
                        Node = node,
                        Category = category,
                        Categories = categories,
                        Date = date,
                        Title = title,
                        ImageUrl = src,
                        Author = author,
                        MadinUrl = madinUrl,
                        IsTopArticle = false,
                        IsChecked = false,
                        IsGeneratedArticle = false,
                        IsPlaceHolder = false,
                        Subtitle = subtitle
                    });
            }
            var topArticle = madinArticles.First();
            madinArticles.Remove(topArticle);
            topArticle.IsTopArticle = true;
            topArticle.IsChecked = true;

            var generatedArticles = GenerateMadinArticle(madinArticles);
            madinArticles.AddRange(generatedArticles);
            // On ne garde pas les articles de category Yekri
            // On supprime les doublons basé sur le titre
            // On ordonne par catégory
            var articlesSansCategory = madinArticles.Where(a => string.IsNullOrWhiteSpace(a.Category)).ToList();
            var articleAvecCategory = madinArticles.Where(a => !string.IsNullOrWhiteSpace(a.Category)).ToList();
            var filteredArticles = articleAvecCategory.Where(a => !a.Category.Contains("Yékri")).GroupBy(art => art.Title).Select(g => g.First()).Where(a => a.Title != topArticle.Title).GroupBy(a => a.Category).SelectMany(g => g).ToList();

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
            result.Insert(0, topArticle);

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
            result.Add(technoArticle);
            if (result.Count % 2 == 0)
            {
                result.Add(MadinArticle.GetPlaceHolderArticle());
            }
            return result;
        }
        /// <summary>
        /// Retourne une copie du Content Node débarrassé de l'auteur et de l'image
        /// </summary>
        /// <param name="originalNode"></param>
        /// <param name="author"></param>
        /// <returns></returns>
        private static HtmlNode NewNodeWithoutAuthorAndImg(HtmlNode originalNode, string author)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml("<div></div>"); // Créer un nouveau nœud racine
            HtmlNode newNode = doc.DocumentNode.FirstChild;

            var isAuthor = !String.IsNullOrEmpty(author);

            var nodes = originalNode.SelectNodes(".//p | .//ul");
            if (nodes != null)
            {

                foreach (HtmlNode p in nodes)
                {
                    if (String.IsNullOrWhiteSpace(p.InnerText)) continue;
                    if (!isAuthor || !p.InnerText.Contains(author))
                    {
                        // Cloner le nœud p pour éviter de modifier le nœud original
                        HtmlNode newP = p.CloneNode(true);

                        // Supprimer tous les nœuds img du nouveau nœud p
                        foreach (var img in newP.SelectNodes(".//img")?.ToList() ?? new List<HtmlNode>())
                        {
                            img.Remove();
                        }

                        // Ajouter le nœud p modifié au nouveau nœud
                        newNode.AppendChild(newP);
                    }
                }
            }

            return newNode;
        }
        private static void RemoveStylesRecursively(HtmlNode node)
        {
            foreach (HtmlNode child in node.ChildNodes)
            {
                RemoveStyles(child);
                RemoveStylesRecursively(child);
            }
        }

        private static void RemoveStyles(HtmlNode node)
        {
            if (node.Attributes["style"] != null)
            {
                node.Attributes.Remove("style");
            }
        }
        private static void RemoveEmptyLinks(HtmlNode node)
        {
            // Sélectionner tous les éléments 'a' dans le nœud donné
            var links = node.SelectNodes(".//a").ToList();

            // Parcourir chaque lien pour vérifier s'il est vide
            foreach (var link in links)
            {
                // Vérifier si le lien est vide (pas de contenu textuel et pas d'éléments enfants)
                if (string.IsNullOrWhiteSpace(link.InnerText) && !link.HasChildNodes)
                {
                    // Supprimer le lien vide du document
                    link.Remove();
                }
            }
        }

        private class GeneratedArticleBuilder
        {
            public string Title { get; set; }
            public string ImageUrl { get; set; }
            public string MadinUrl { get; set; }
            public Predicate<MadinArticle> SelectorPredicate { get; set; }
        }
        private static List<MadinArticle> GenerateMadinArticle(List<MadinArticle> unfilteredArticle)
        {

            List<MadinArticle> madinArticlesGenerated = new List<MadinArticle>();
            var contentTemplate = "<p class=\"generated-line\"><span><a class=\"discret-link\" href=\"[MadinUrl]\">[Title]</a></span></p>";


            List<GeneratedArticleBuilder> builders = new List<GeneratedArticleBuilder>()
            {
                    new GeneratedArticleBuilder()
                    {
                        Title = "Nos belles éphémérides",
                        ImageUrl = "https://www.madinin-art.net/images/les_ephemerides.jpg",
                        MadinUrl = "https://www.madinin-art.net/cat/yekri/",
                        SelectorPredicate = a => (a.Category.Contains("Yékri") && a.Title.Contains("éphéméride"))
                    },
                    new GeneratedArticleBuilder()
                    {
                        Title="Les chroniques de J-M Nol",
                        ImageUrl = "https://www.madinin-art.net/wp-content/uploads/2024/03/cat_chroniques_de_J-M_Nol.jpg",
                        MadinUrl = "https://www.madinin-art.net/cat/sciences_sociales/economie/les-chroniques-de-jean-marie-nol/",
                        SelectorPredicate = a=> a.Category.Contains("Les chroniques de Jean-Marie Nol")
                    },
                    new GeneratedArticleBuilder()
                    {
                        Title="Politiques",
                        ImageUrl = "https://www.madinin-art.net/wp-content/uploads/2018/10/debattre.jpg",
                        MadinUrl = "https://www.madinin-art.net/cat/politiques/",
                        SelectorPredicate = a=> a.Category == "Politiques"
                    }



            };


            foreach (var builder in builders)
            {
                var title = builder.Title;
                var imageUrl = builder.ImageUrl;
                var madinUrl = builder.MadinUrl;
                var predicate = builder.SelectorPredicate;
                var articles = unfilteredArticle.Where(a => predicate(a));

                var titles = new List<(string, string, string)>();
                if (title == "Nos belles éphémérides")
                {
                    titles = articles.Select(a => (a.MadinUrl, Title: a.Subtitle, a.Author)).Where(a => !string.IsNullOrWhiteSpace(a.Title)).ToList();
                }
                else
                {
                    titles = articles.Select(a => (a.MadinUrl, a.Title, a.Author)).Where(s => !string.IsNullOrWhiteSpace(s.Title)).ToList();
                }

                StringBuilder stringBuilder = new StringBuilder();
                foreach (var t in titles)
                {
                    var ligne = String.IsNullOrWhiteSpace(t.Item3) ? t.Item2 : t.Item2 + " - " + (t.Item3.Contains("Par") ? "" : builder.Title == "Les chroniques de J-M Nol" ? "" : "Par") + t.Item3;
                    stringBuilder.AppendLine(contentTemplate.Replace("[MadinUrl]", t.Item1).Replace("[Title]", ligne));

                }
                var htmlContent = stringBuilder.ToString();
                var htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(htmlContent);

                var content = htmlDocument.DocumentNode.InnerText;

                var generatedArticle = new MadinArticle()
                {
                    Title = title,
                    Category = "",
                    MadinUrl = madinUrl,
                    ImageUrl = imageUrl,
                    IsTopArticle = false,
                    IsPlaceHolder = false,
                    IsGeneratedArticle = true,
                    HtmlContent = stringBuilder.ToString(),
                    Content = content
                };

                madinArticlesGenerated.Add(generatedArticle);
            }
            return madinArticlesGenerated;
        }
    }
}
