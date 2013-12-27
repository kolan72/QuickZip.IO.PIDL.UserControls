using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Media;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Threading;
using System.Windows;
using System.Threading;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.IO;
using vbAccelerator.Components.ImageList;
using System.Diagnostics;
using System.Windows.Resources;
using vbAccelerator.Components.Shell;
using QuickZip.UserControls;
using QuickZip.Converters;
using ShellDll;
using QuickZip.IO.PIDL.UserControls.ViewModel;
using System.IO.Tools;


namespace QuickZip.IO.PIDL.UserControls
{
    //+AK
    class FolderToIconConverterBase
    {
        protected static string imageFilter = ".jpg,.jpeg,.png,.gif,.bmp,.tiff";
        protected static string tempPath = System.IO.Path.GetTempPath();
        protected static string specialExtFilter = ".exe,.lnk";

        protected virtual void ValueToKey(object value, out string key, out string fastKey, out bool delayLoading)
        {
            delayLoading = false;
            key = "";
            fastKey = "";

            if (value is FileSystemInfoEx)
            {
                FileSystemInfoEx entry = value as FileSystemInfoEx;

                if (value is FileInfoEx)
                {
                    fastKey = PathEx.GetExtension(entry.Name);
                    if (imageFilter.IndexOf(fastKey, StringComparison.InvariantCultureIgnoreCase) != -1 ||
                        specialExtFilter.Split(',').Contains(fastKey))
                        key = entry.FullName;
                    else key = fastKey;
                    delayLoading = key != fastKey;

                }
                else //DirectoryInfoEx
                {
                    DirectoryInfoEx dirEntry = entry as DirectoryInfoEx;
                    fastKey = tempPath;
                    key = fastKey;
                    if (IsSpecialFolder(entry.FullName))
                    {
                        key = entry.FullName;
                        delayLoading = true;
                    }

                }

            }

            //////key = imageKey = ""; delayLoading = false;

            //////if (value is string)
            //////{
            //////    string path = value as string;
            //////    if (Path.GetExtension(path) != "")
            //////    {
            //////        key = imageKey = Path.GetExtension(path);

            //////        if (imageFilter.IndexOf(key) != -1)
            //////        {
            //////            imageKey = path;
            //////            delayLoading = true;
            //////        }
            //////    }
            //////    else
            //////        if (File.Exists(path)) //File without extension
            //////            key = imageKey = ".AaAaA";
            //////        else //Directory
            //////            key = imageKey = tempPath;

            //////}
        }

        public static bool IsSpecialFolder(string path)
        {
            return path.EndsWith(":\\") || path.EndsWith(":") ||
                (path.StartsWith("::") && path.Split('\\').Count() <= 2) ||
                (path.StartsWith(DirectoryInfoEx.CurrentUserDirectory.FullName) && PathEx.FullNameToGuidName(path).Split('\\').Count() <= 2);
        }


    }
}
