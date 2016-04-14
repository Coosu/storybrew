﻿using System;
using System.IO;

namespace StorybrewEditor.Util
{
    public static class PathHelper
    {
        /// <summary>
        /// Replaces directory separator by Path.DirectorySeparatorChar
        /// </summary>
        public static string WithPlatformSeparators(string path)
        {
            if (Path.DirectorySeparatorChar != '/')
                path = path.Replace('/', Path.DirectorySeparatorChar);
            if (Path.DirectorySeparatorChar != '\\')
                path = path.Replace('\\', Path.DirectorySeparatorChar);
            return path;
        }

        /// <summary>
        /// Replaces directory separator by a StandardDirectorySeparator
        /// </summary>
        public const char StandardDirectorySeparator = '/';
        public static string WithStandardSeparators(string path)
        {
            if (Path.DirectorySeparatorChar != StandardDirectorySeparator)
                path = path.Replace(Path.DirectorySeparatorChar, StandardDirectorySeparator);
            path = path.Replace('\\', StandardDirectorySeparator);
            return path;
        }

        public static bool FolderContainsPath(string folder, string path)
        {
            folder = WithStandardSeparators(Path.GetFullPath(folder)).TrimEnd('/');
            path = WithStandardSeparators(Path.GetFullPath(path)).TrimEnd('/');

            return path.Length >= folder.Length + 1 && path[folder.Length] == '/' && path.StartsWith(folder);
        }

        public static string GetRelativePath(string folder, string path)
        {
            folder = WithStandardSeparators(Path.GetFullPath(folder)).TrimEnd('/');
            path = WithStandardSeparators(Path.GetFullPath(path)).TrimEnd('/');

            if (path.Length < folder.Length + 1 || path[folder.Length] != '/' || !path.StartsWith(folder))
                throw new ArgumentException(path + " isn't contained in " + folder);

            return path.Substring(folder.Length + 1);
        }
    }
}
