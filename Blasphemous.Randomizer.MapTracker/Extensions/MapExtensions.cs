using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Blasphemous.Randomizer.MapTracker.Extensions;

internal static class MapExtensions
{
    public static bool IsReverseOf<T>(this List<T> mainList, List<T> otherList)
    {
        if (mainList.Count != otherList.Count)
            return false;

        for (int i = 0; i < mainList.Count; i++)
        {
            if (!mainList[i].Equals(otherList[mainList.Count - 1 - i]))
                return false;
        }

        return true;
    }

    // Recursive method that returns the entire hierarchy of an object
    public static string DisplayHierarchy(this Transform transform, int maxLevel, bool includeComponents)
    {
        return transform.DisplayHierarchy_INTERNAL(new StringBuilder(), 0, maxLevel, includeComponents).ToString();
    }

    public static StringBuilder DisplayHierarchy_INTERNAL(this Transform transform, StringBuilder currentHierarchy, int currentLevel, int maxLevel, bool includeComponents)
    {
        // Indent
        for (int i = 0; i < currentLevel; i++)
            currentHierarchy.Append("\t");

        // Add this object
        currentHierarchy.Append(transform.name);

        // Add components
        if (includeComponents)
        {
            currentHierarchy.Append(" - ");
            foreach (Component c in transform.GetComponents<Component>())
                currentHierarchy.Append(c.ToString() + ", ");
        }
        currentHierarchy.AppendLine();

        // Add children
        if (currentLevel < maxLevel)
        {
            for (int i = 0; i < transform.childCount; i++)
                currentHierarchy = transform.GetChild(i).DisplayHierarchy_INTERNAL(currentHierarchy, currentLevel + 1, maxLevel, includeComponents);
        }

        // Return output
        return currentHierarchy;
    }
}
