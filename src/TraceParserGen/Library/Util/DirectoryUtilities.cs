/****************************************************************************/
/*                           DirectoryUtilities.cs                          */
/****************************************************************************/

/* AUTHOR: Vance Morrison
 * Date  : 11/3/2005  */
/****************************************************************************/
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Reflection;
using System.CodeDom.Compiler;
using System.Diagnostics;           // for StackTrace; Process

/******************************************************************************/
/// <summary>
/// General purpose utilities dealing with archiveFile system directories. 
/// </summary>
static public class DirectoryUtilities
{
    public static string GetRelativePath(string fileName, string directory)
    {
        Debug.Assert(fileName.StartsWith(directory), "directory not a prefix");

        int directoryEnd = directory.Length;
        if (directoryEnd == 0)
            return fileName;
        while (directoryEnd < fileName.Length && fileName[directoryEnd] == '\\')
            directoryEnd++;
        string relativePath = fileName.Substring(directoryEnd);
        return relativePath;
    }

    /// <summary>
    /// SafeCopy sourceDirectory to directoryToVersion recursively. The target directory does
    /// no need to exist
    /// </summary>
    public static void Copy(string sourceDirectory, string targetDirectory)
    {
        Copy(sourceDirectory, targetDirectory, SearchOption.AllDirectories);
    }

    /// <summary>
    /// SafeCopy all files from sourceDirectory to directoryToVersion.  If searchOptions == AllDirectories
    /// then the copy is recursive, otherwise it is just one level.  The target directory does not
    /// need to exist. 
    /// </summary>
    public static void Copy(string sourceDirectory, string targetDirectory, SearchOption searchOptions)
    {
        if (!Directory.Exists(targetDirectory))
            Directory.CreateDirectory(targetDirectory);

        foreach (string sourceFile in Directory.GetFiles(sourceDirectory))
        {
            string targetFile = Path.Combine(targetDirectory, Path.GetFileName(sourceFile));
            FileUtilities.ForceCopy(sourceFile, targetFile);
        }
        if (searchOptions == SearchOption.AllDirectories)
        {
            foreach (string sourceDir in Directory.GetDirectories(sourceDirectory))
            {
                string targetDir = Path.Combine(targetDirectory, Path.GetFileName(sourceDir));
                Copy(sourceDir, targetDir, searchOptions);
            }
        }
    }

    /// <summary>
    /// Clean is sort of a 'safe' recursive delete of a directory.  It either deletes the
    /// files or moves them to '*.deleting' names.  It deletes directories that are completely
    /// empty.  Thus it will do a recursive delete when that is possible.  There will only 
    /// be *.deleting files after this returns.  It returns the number of files and directories
    /// that could not be deleted.  
    /// </summary>
    public static int Clean(string directory)
    {
        if (!Directory.Exists(directory))
            return 0;

        int ret = 0;
        foreach (string file in Directory.GetFiles(directory))
            if (!FileUtilities.ForceDelete(file))
                ret++;

        foreach (string subDir in Directory.GetDirectories(directory))
            ret += Clean(subDir);

        if (ret == 0)
        {
            try
            {
                Directory.Delete(directory, true);
            }
            catch
            {
                ret++;
            }
        }
        else
            ret++;
        return ret;
    }

    /// <summary>
    /// Removes the oldest directories directly under 'directoryPath' so that 
    /// only 'numberToKeep' are left. 
    /// </summary>
    /// <param variable="directoryPath">Directory to removed old files from.</param>
    /// <param variable="numberToKeep">The number of files to keep.</param>
    /// <returns> true if there were no errors deleting files</returns>
    public static bool DeleteOldest(string directoryPath, int numberToKeep)
    {
        if (!Directory.Exists(directoryPath))
            return true;

        string[] dirs = Directory.GetDirectories(directoryPath);
        int numToDelete = dirs.Length - numberToKeep;
        if (numToDelete <= 0)
            return true;

        Array.Sort<string>(dirs, delegate(string x, string y)
        {
            return File.GetLastWriteTimeUtc(x).CompareTo(File.GetLastWriteTimeUtc(y));
        });

        bool ret = true;
        for (int i = 0; i < numToDelete; i++)
        {
            try
            {
                Directory.Delete(dirs[i]);
            }
            catch (Exception)
            {
                // TODO trace message;
                ret = false;
            }
        }
        return ret;
    }

    /// <summary>
    /// DirectoryUtilities.GetFiles is basicaly the same as Directory.GetFiles 
    /// however it returns IEnumerator, which means that it lazy.  This is very important 
    /// for large directory trees.  A searchPattern can be specified (Windows wildcard conventions)
    /// that can be used to filter the set of archiveFile names returned. 
    /// 
    /// Suggested Usage
    /// 
    ///     foreach(string fileName in DirectoryUtilities.GetFiles("c:\", "*.txt")){
    ///         Console.WriteLine(fileName);
    ///     }
    ///
    /// </summary>
    /// <param variable="directoryPath">The base directory to enumerate</param>
    /// <param variable="searchPattern">A pattern to filter the names (windows filename wildcards * ?)</param>
    /// <param variable="searchOptions">Indicate if the search is recursive or not.  </param>
    /// <returns>The enumerator for all archiveFile names in the directory (recursively). </returns>
    public static IEnumerable<string> GetFiles(string directoryPath, string searchPattern, SearchOption searchOptions)
    {

        string[] fileNames = Directory.GetFiles(directoryPath, searchPattern, SearchOption.TopDirectoryOnly);
        Array.Sort<string>(fileNames, StringComparer.OrdinalIgnoreCase);
        foreach (string fileName in fileNames)
        {
            yield return fileName;
        }

        if (searchOptions == SearchOption.AllDirectories)
        {
            string[] subDirNames = Directory.GetDirectories(directoryPath);
            Array.Sort<string>(subDirNames);
            foreach (string subDir in subDirNames)
            {
                foreach (string fileName in DirectoryUtilities.GetFiles(subDir, searchPattern, searchOptions))
                {
                    yield return fileName;
                }
            }
        }
    }
    public static IEnumerable<string> GetFiles(string directoryName, string searchPattern)
    {
        return GetFiles(directoryName, searchPattern, SearchOption.TopDirectoryOnly);
    }
    public static IEnumerable<string> GetFiles(string directoryName)
    {
        return GetFiles(directoryName, "*");
    }
}
