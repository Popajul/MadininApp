using HtmlAgilityPack;
using MadininApp.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace MadininApp.Services
{

    internal static class MadinScrapper
    {
        private const string URL = "https://www.madinin-art.net/";
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
                // Date
                var dateNode = node.SelectSingleNode($"(.//*[contains(concat(' ', normalize-space(@class), ' '), ' published updated ')])");
                var date = dateNode == null ? "" : HtmlEntity.DeEntitize(dateNode.InnerText).Trim();
                // Content
                var contentNode = node.SelectSingleNode($"(.//*[contains(concat(' ', normalize-space(@class), ' '), ' entry-content ')])");
                var content = contentNode == null ? "" : HtmlEntity.DeEntitize(contentNode.InnerText).Trim();
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
                        Author = author
                    });
                var topArticle = madinArticles.First();
                topArticle.IsTopArticle = true;
                topArticle.IsChecked = true;
            }
            return madinArticles.GroupBy(art => art.Title).Select(g => g.First()).ToList();
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

            var nodes = originalNode.SelectNodes(".//p");
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
    }
}
