using System.Windows;
using System.Windows.Media;

namespace MadininApp
{


    public static class VisualTreeHelperExtensions
    {
        /// <summary>
        /// Finds the first parent of the specified type in the visual tree.
        /// </summary>
        /// <typeparam name="T">The type of the parent to find.</typeparam>
        /// <param name="child">The starting element for the search.</param>
        /// <returns>The first parent element of the specified type, or null if no parent of the type is found.</returns>
        public static T FindParentOfType<T>(DependencyObject child) where T : DependencyObject
        {
            // Get the current parent of the element
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            // Loop up the visual tree until reaching the root
            while (parentObject != null)
            {
                // Check if the current parent matches the type we're looking for
                if (parentObject is T parent)
                {
                    return parent;
                }

                // Move up the tree
                parentObject = VisualTreeHelper.GetParent(parentObject);
            }

            // No matching parent was found
            return null;
        }


        public static T FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child is T t)
                {
                    return t;
                }
                else
                {
                    T childOfChild = FindVisualChild<T>(child);
                    if (childOfChild != null)
                    {
                        return childOfChild;
                    }
                }
            }
            return null;
        }
    }

}

