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
using QuickZip.IO.PIDL.UserControls.Model;

namespace QuickZip.IO.PIDL.UserControls
{
    //+AK
    class FolderToIconConverter : FolderToIconConverterBase, IValueConverter, IMultiValueConverter
    {

        #region Defines

        public enum IconSize
        {
            small = 1, large, extraLarge, jumbo, thumbnail
        }
        //private static string moduleName = null;
        private int defaultsize;

        public int DefaultSize { get { return defaultsize; } set { defaultsize = value; } }

        private class thumbnailInfo
        {
            public IconSize iconsize;
            public System.Drawing.Size outputSize;
            public WriteableBitmap bitmap;
            public string key;
            public int roll;
            public thumbnailInfo(WriteableBitmap b, string k, IconSize size, System.Drawing.Size outSize, int rol)
            {
                bitmap = b;
                key = k;
                iconsize = size;
                outputSize = outSize;
                roll = rol;
            }
        }

      

        #endregion

        #region Environment Tools
        public static bool isVistaUp()
        {
            return (Environment.OSVersion.Version.Major >= 6);
        }

        #endregion

        #region Win32api
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        protected static extern bool DeleteObject(IntPtr hObject);

        [StructLayout(LayoutKind.Sequential)]
        protected struct SHFILEINFO
        {
            public IntPtr hIcon;
            public IntPtr iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        };

        protected const uint SHGFI_ICON = 0x100;
        protected const uint SHGFI_TYPENAME = 0x400;
        protected const uint SHGFI_PIDL = 0x000000008;
        protected const uint SHGFI_LARGEICON = 0x0; // 'Large icon
        protected const uint SHGFI_SMALLICON = 0x1; // 'Small icon
        protected const uint SHGFI_SYSICONINDEX = 16384;
        protected const uint SHGFI_USEFILEATTRIBUTES = 16;

        // <summary>
        /// Get Icons that are associated with files.
        /// To use it, use (System.Drawing.Icon myIcon = System.Drawing.Icon.FromHandle(shinfo.hIcon));
        /// hImgSmall = SHGetFileInfo(fName, 0, ref shinfo,(uint)Marshal.SizeOf(shinfo),Win32.SHGFI_ICON |Win32.SHGFI_SMALLICON);
        /// </summary>
        [DllImport("shell32.dll")]
        protected static extern IntPtr SHGetFileInfo(IntPtr pszPath, uint dwFileAttributes,
                                                  ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);
        [DllImport("shell32.dll")]
        protected static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes,
                                                  ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

        public static Bitmap GetFileIcon(IntPtr fileName, IconSize size)
        {
            SHFILEINFO shinfo = new SHFILEINFO();

            uint flags = SHGFI_SYSICONINDEX | SHGFI_USEFILEATTRIBUTES | SHGFI_PIDL;
            if (size == IconSize.small)
                flags = flags | SHGFI_ICON | SHGFI_SMALLICON;
            else flags = flags | SHGFI_ICON;

            SHGetFileInfo(fileName, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), flags);
            return shinfo.hIcon != IntPtr.Zero ? Icon.FromHandle(shinfo.hIcon).ToBitmap() : new Bitmap(1, 1);
        }

        // <summary>
        /// Return large file icon of the specified file.
        /// </summary>
        public static Bitmap GetFileIcon(string fileName, IconSize size)
        {
            if (fileName.StartsWith("."))
                fileName = "AAA" + fileName;

            SHFILEINFO shinfo = new SHFILEINFO();

            uint flags = SHGFI_SYSICONINDEX;
            if (fileName.IndexOf(":") == -1)
                flags = flags | SHGFI_USEFILEATTRIBUTES;

            if (size == IconSize.small)
                flags = flags | SHGFI_ICON | SHGFI_SMALLICON;
            else flags = flags | SHGFI_ICON;

            SHGetFileInfo(fileName, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), flags);
            Bitmap retVal = shinfo.hIcon != IntPtr.Zero ? Icon.FromHandle(shinfo.hIcon).ToBitmap() : new Bitmap(1, 1);
            if (!retVal.Size.Equals(IconSizeToSize(size)))
                return resizeImage(retVal, IconSizeToSize(size), 0);
            return retVal;
        }

        #endregion

        #region Image Tools
        public static BitmapSource loadBitmap(Bitmap source)
        {
            IntPtr hBitmap = source.GetHbitmap();
            //Memory Leak fixes, for more info : http://social.msdn.microsoft.com/forums/en-US/wpf/thread/edcf2482-b931-4939-9415-15b3515ddac6/
            try
            {
                BitmapSource bmSource = Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty,
                   BitmapSizeOptions.FromEmptyOptions());
                bmSource.Freeze();
                return bmSource;
            }
            catch
            {
                BitmapSource bmSource = new BitmapImage();
                bmSource.Freeze();
                return bmSource;
            }
            finally
            {
                DeleteObject(hBitmap);
            }

        }

        private static void copyBitmap(BitmapSource source, WriteableBitmap target, bool dispatcher, int spacing, bool freezeBitmap)
        {
            int width = source.PixelWidth;
            int height = source.PixelHeight;
            int stride = width * ((source.Format.BitsPerPixel + 7) / 8);

            byte[] bits = new byte[height * stride];
            source.CopyPixels(bits, stride, 0);
            source = null;

            //original code.
            //writeBitmap.Dispatcher.Invoke(DispatcherPriority.Background,
            //    new ThreadStart(delegate
            //    {
            //        //UI Thread
            //        Int32Rect outRect = new Int32Rect(0, (int)(writeBitmap.Height - height) / 2, width, height);                    
            //        writeBitmap.WritePixels(outRect, bits, stride, 0);                                        
            //    }));

            //Bugfixes by h32

            if (dispatcher)
            {
                target.Dispatcher.BeginInvoke(DispatcherPriority.Background,
                new ThreadStart(delegate
                {
                    //UI Thread
                    var delta = target.Height - height;
                    var newWidth = width > target.Width ? (int)target.Width : width;
                    var newHeight = height > target.Height ? (int)target.Height : height;
                    Int32Rect outRect = new Int32Rect((int)((target.Width - newWidth) / 2), (int)(delta >= 0 ? delta : 0) / 2 + spacing, newWidth - (spacing * 2), newWidth - (spacing * 2));
                    try
                    {
                        target.WritePixels(outRect, bits, stride, 0);
                        if (freezeBitmap)
                        {
                            target.Freeze();
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.Message);
                        System.Diagnostics.Debugger.Break();
                    }
                }));
            }
            else
            {
                var delta = target.Height - height;
                var newWidth = width > target.Width ? (int)target.Width : width;
                var newHeight = height > target.Height ? (int)target.Height : height;
                Int32Rect outRect = new Int32Rect(spacing, (int)(delta >= 0 ? delta : 0) / 2 + spacing, newWidth - (spacing * 2), newWidth - (spacing * 2));
                try
                {
                    target.WritePixels(outRect, bits, stride, 0);
                    if (freezeBitmap)
                        target.Freeze();
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                    System.Diagnostics.Debugger.Break();
                }
            }
        }

        //http://blog.paranoidferret.com/?p=11 , modified a little.
        public static Bitmap resizeImage(Bitmap imgToResize, System.Drawing.Size size, int spacing)
        {
            int sourceWidth = imgToResize.Width;
            int sourceHeight = imgToResize.Height;

            float nPercent = 0;
            float nPercentW = 0;
            float nPercentH = 0;

            nPercentW = ((float)size.Width / (float)sourceWidth);
            nPercentH = ((float)size.Height / (float)sourceHeight);

            if (nPercentH < nPercentW)
                nPercent = nPercentH;
            else
                nPercent = nPercentW;

            int destWidth = (int)((sourceWidth * nPercent) - spacing * 4);
            int destHeight = (int)((sourceHeight * nPercent) - spacing * 4);

            int leftOffset = (int)((size.Width - destWidth) / 2);
            int topOffset = (int)((size.Height - destHeight) / 2);


            Bitmap b = new Bitmap(size.Width, size.Height);
            Graphics g = Graphics.FromImage((Image)b);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
            if (spacing > 0)
            {
                g.DrawLines(System.Drawing.Pens.Silver, new PointF[] {
                new PointF(leftOffset - spacing, topOffset + destHeight + spacing), //BottomLeft
                new PointF(leftOffset - spacing, topOffset -spacing),                 //TopLeft
                new PointF(leftOffset + destWidth + spacing, topOffset - spacing)});//TopRight

                g.DrawLines(System.Drawing.Pens.Gray, new PointF[] {
                new PointF(leftOffset + destWidth + spacing, topOffset - spacing),  //TopRight
                new PointF(leftOffset + destWidth + spacing, topOffset + destHeight + spacing), //BottomRight
               new PointF(leftOffset - spacing, topOffset + destHeight + spacing),}); //BottomLeft
            }
            g.DrawImage(imgToResize, leftOffset, topOffset, destWidth, destHeight);
            g.Dispose();

            return b;
        }
        public static Bitmap resizeJumbo(Bitmap imgToResize, System.Drawing.Size size, int spacing)
        {
            int sourceWidth = imgToResize.Width;
            int sourceHeight = imgToResize.Height;

            float nPercent = 0;
            float nPercentW = 0;
            float nPercentH = 0;

            nPercentW = ((float)size.Width / (float)sourceWidth);
            nPercentH = ((float)size.Height / (float)sourceHeight);

            if (nPercentH < nPercentW)
                nPercent = nPercentH;
            else
                nPercent = nPercentW;

            int destWidth = 80;
            int destHeight = 80;

            int leftOffset = (int)((size.Width - destWidth) / 2);
            int topOffset = (int)((size.Height - destHeight) / 2);


            Bitmap b = new Bitmap(size.Width, size.Height);
            Graphics g = Graphics.FromImage((Image)b);
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
            g.DrawLines(System.Drawing.Pens.Silver, new PointF[] {
                new PointF(0 + spacing, size.Height - spacing), //BottomLeft
                new PointF(0 + spacing, 0 + spacing),                 //TopLeft
                new PointF(size.Width - spacing, 0 + spacing)});//TopRight

            g.DrawLines(System.Drawing.Pens.Gray, new PointF[] {
                new PointF(size.Width - spacing, 0 + spacing),  //TopRight
                new PointF(size.Width - spacing, size.Height - spacing), //BottomRight
                new PointF(0 + spacing, size.Height - spacing)}); //BottomLeft

            g.DrawImage(imgToResize, leftOffset, topOffset, destWidth, destHeight);
            g.Dispose();

            return b;
        }
        #endregion

        #region Instance Cache
        private static Dictionary<string, BitmapSource> iconDic = new Dictionary<string, BitmapSource>();
        private static int currentRoll = 1;
        //ReaderWriterLockSlim getPathLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        public void ClearInstanceCache()
        {
            iconDic.Clear();
            currentRoll += 1;
        }

        public int IconCount { get { return iconDic.Count; } }

        public ImageSource GetIconImage(string fastKey, string key, IconSize size, bool delayLoading)
        {
            string extKey = KeyToExtKey(key, size);
            string ext = Path.GetExtension(key);
            string fastExtKey = KeyToExtKey(fastKey, size);

            bool isImage = !String.IsNullOrEmpty(ext) &&
                imageFilter.IndexOf(ext, StringComparison.InvariantCultureIgnoreCase) != -1;
            delayLoading = delayLoading && ((isImage && (int)size >= (int)IconSize.extraLarge) || !isImage);

            //!!! +AK Поставлено принудительно, т.к. иначе при первом запуске возвращает одинаковую иконку(папку) для всех файлов
            //Видимо, значение в iconDic не успевает подмениться
            delayLoading = false;//
            //0.4
            //Fixed Small Image Icon not shared by all instance
            if (isImage && size == IconSize.small)
                extKey = ext.ToLower();

            if (!iconDic.ContainsKey(extKey))
            {
                if (delayLoading)
                {
                    WriteableBitmap bitmap = new WriteableBitmap(
                        iconDic.ContainsKey(fastExtKey) ? iconDic[fastExtKey] : loadBitmap(KeyToBitmap(fastKey, size)));
                    thumbnailInfo info = new thumbnailInfo(bitmap, key, size, new System.Drawing.Size(bitmap.PixelWidth, bitmap.PixelHeight), currentRoll);

                    if (isImage)
                        ThreadPool.QueueUserWorkItem(new WaitCallback(PollThumbnailCallback), info);
                    else
                    ////Не работает PollIconCallback(info);
                    {
                        ThreadPool.QueueUserWorkItem(new WaitCallback(PollIconCallback), info);
                        _ev.WaitOne();
                    }
                    iconDic.Add(extKey, bitmap);
                }
                else
                    iconDic.Add(extKey, loadBitmap(KeyToBitmap(isImage ? fastKey : key, size)));
            }

            return iconDic[extKey];
        }

        //+AK
        private AutoResetEvent _ev = new AutoResetEvent(false);
        #region PollCallback
        //Icon (e.g. exe)
        private void PollIconCallback(object state)
        {
            thumbnailInfo input = state as thumbnailInfo;

            WriteableBitmap writeBitmap = input.bitmap;
            IconSize size = input.iconsize;

            Bitmap origBitmap = KeyToBitmap(input.key, size);
            Bitmap inputBitmap = origBitmap;
            System.Drawing.Size outputSize = input.outputSize;
            if (size == IconSize.jumbo || size == IconSize.thumbnail)
                inputBitmap = resizeJumbo(origBitmap, outputSize, 5);
            else inputBitmap = resizeImage(origBitmap, outputSize, 0);

            BitmapSource inputBitmapSource = loadBitmap(inputBitmap);
            origBitmap.Dispose();
            inputBitmap.Dispose();

            //Update back to source.
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
                new EventHandler(delegate
                {
                    iconDic[input.key] = inputBitmapSource;
                }));

            copyBitmap(inputBitmapSource, writeBitmap, true, 0, true);//false
            _ev.Set();
        }

        //Thumbnail (for images)
        private void PollThumbnailCallback(object state)
        {
            //Non UIThread
            thumbnailInfo input = state as thumbnailInfo;

            WriteableBitmap writeBitmap = input.bitmap;
            IconSize size = input.iconsize;

            try
            {
                string thumbPath = input.key;
                Bitmap origBitmap;
                origBitmap = new Bitmap(KeyToBitmap(input.key, IconSize.thumbnail));
                Bitmap inputBitmap = resizeImage(origBitmap, IconSizeToSize(size), 5);
                BitmapSource inputBitmapSource = loadBitmap(inputBitmap);
                origBitmap.Dispose();
                inputBitmap.Dispose();
                inputBitmapSource.Freeze();

                //Update back to source.
                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
                    new EventHandler(delegate
                    {
                        iconDic[input.key] = inputBitmapSource;
                    }));

                copyBitmap(inputBitmapSource, writeBitmap, true, 0, true);//false
            }
            catch (Exception ex) { Debug.WriteLine("PollThumbnailCallback " + ex.Message + "(" + input.key + ")"); }

        }
        #endregion


        #endregion

        #region Unused

        ////http://igorshare.wordpress.com/2009/01/07/wpf-extracting-bitmapimage-from-an-attached-resource-in-referenced-assemblylibrary/
        //private static BitmapImage GetResourceImage(string resourcePath)
        //{
        //    var image = new BitmapImage();            
        //    string resourceLocation =
        //        string.Format("pack://application:,,,/{0};component/{1}", moduleName,
        //                      resourcePath);
        //    try
        //    {
        //        image.BeginInit();
        //        image.CacheOption = BitmapCacheOption.OnLoad;
        //        image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
        //        image.UriSource = new Uri(resourceLocation);
        //        image.EndInit();
        //        image.Freeze();
        //    }
        //    catch (Exception e)
        //    {
        //        System.Diagnostics.Trace.WriteLine(e.ToString());
        //    }

        //    return image;
        //}
        //private static Bitmap GetResourceBitmap(string resourcePath)
        //{            
        //    string resourceLocation =
        //        string.Format("pack://application:,,,/{0};component/{1}", moduleName,
        //                      resourcePath);

        //    StreamResourceInfo info = Application.GetResourceStream(new Uri(resourceLocation));
        //    Bitmap bitmap = new Bitmap(info.Stream);
        //    return bitmap;
        //}

        //private static Bitmap GetFolderBitmap(string lookup, IconSize size)
        //{

        //    switch (size)
        //    {
        //        case IconSize.thumbnail:
        //        case IconSize.jumbo:
        //            return loadJumbo(lookup, isDiskFolder(lookup));
        //        case IconSize.extraLarge:
        //            _imgList.ImageListSize = SysImageListSize.extraLargeIcons;
        //            return _imgList.Icon(_imgList.IconIndex(lookup, isDiskFolder(lookup))).ToBitmap();
        //        //case IconSize.large :
        //        //    _imgList.ImageListSize = SysImageListSize.largeIcons;
        //        //    return _imgList.Icon(_imgList.IconIndex(lookup, isDiskFolder(lookup))).ToBitmap();
        //        //case IconSize.small :
        //        //    _imgList.ImageListSize = SysImageListSize.smallIcons;
        //        //    return _imgList.Icon(_imgList.IconIndex(lookup, isDiskFolder(lookup))).ToBitmap();
        //        default:
        //            try
        //            {
        //                return GetFileIcon(lookup, size).ToBitmap();
        //            }
        //            catch { return GetFolderBitmap(UCUtils.GetProgramPath(), size); }
        //    }
        //}



        #endregion


        #region Static Methods
        private static string KeyToExtKey(string key, IconSize size)
        {
            switch (size)
            {
                case IconSize.jumbo: return key + "J";
                case IconSize.thumbnail: return key + "T";
                case IconSize.extraLarge: return key + "E";
                case IconSize.large: return key + "L";
                default: return key;
            }
        }

        private static string ExtKeyToKey(string extKey, IconSize size)
        {
            if (extKey.Length > 2 && extKey[extKey.Length - 2] == '+')
                return extKey.Substring(0, extKey.Length - 2);
            return extKey;
        }

        private static System.Drawing.Size IconSizeToSize(IconSize size)
        {
            switch (size)
            {
                case IconSize.thumbnail: return new System.Drawing.Size(96, 96);
                case IconSize.jumbo: return new System.Drawing.Size(64, 64);
                case IconSize.extraLarge: return new System.Drawing.Size(48, 48);
                case IconSize.large: return new System.Drawing.Size(32, 32);
                default: return new System.Drawing.Size(16, 16);
            }
        }

        private static IconSize SizeToIconSize(int size)
        {
            if (size <= 16) return IconSize.small;
            else if (size <= 32) return IconSize.large;
            else if (size <= 47) return IconSize.extraLarge;
            //else if (iconSize <= 72) return IconSize.jumbo;
            else return IconSize.thumbnail;
        }
        #endregion


        #region IValueConverter Members
       

        /// <summary>
        /// Return the key to be stored for the input value.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="key">Key used for non-thumbnail.</param>
        /// <param name="imageKey">Key used for thumbnail</param>
        /// <param name="delayLoading">Load icon in separate thread.</param>
        protected void ValueToKey(object value, out string key, out string fastKey, out bool delayLoading)
        {
            key = ""; fastKey = ""; delayLoading = false;

            ExModel model = null;
            if (value is ExViewModel)
                model = (value as ExViewModel).EmbeddedModel;
            else if (value is ExModel)
                model = value as ExModel;

            FileSystemInfoEx entry = null;
            if (model != null)
            {
                if (model is FileModel)
                {
                    key = fastKey = PathEx.GetExtension(model.Name);
                    if (key == "")
                        key = fastKey = ".AaAaA";
                    if (imageFilter.IndexOf(key, StringComparison.InvariantCultureIgnoreCase) != -1 ||
                        specialExtFilter.Split(',').Contains(key))
                    {
                        entry = model.EmbeddedEntry;
                        delayLoading = true;
                    }
                }
                else
                {

                    if (IsSpecialFolder(model.FullName))
                    {
                        entry = model.EmbeddedEntry;
                        delayLoading = true;
                    }
                    else
                    {
                        key = fastKey = tempPath;
                    }
                }

            }

            if (entry != null)
                base.ValueToKey(entry, out key, out fastKey, out delayLoading);
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



        /// <summary>
        /// Return the Bitmap that represent the specified key and size.
        /// </summary>
        protected  Bitmap KeyToBitmap(string key, IconSize size)
        {
            try
            {
                bool isDiskFolder = key.EndsWith(":\\");
                bool isTemp = key.Equals(tempPath);

                string ext = PathEx.GetExtension(key).ToLower();

                if (!ext.StartsWith("."))
                {
                    if (specialExtFilter.Split(',').Contains(key))
                    {
                        switch (ext)
                        {
                            case ".exe":
                                ShellDll.PIDL pidlLookup = FileSystemInfoEx.FromString(key).PIDL;
                                try
                                {
                                    return GetFileIcon(pidlLookup.Ptr, size);
                                }
                                finally { if (pidlLookup != null) pidlLookup.Free(); }

                            case ".lnk":
                                using (ShellLink sl = new ShellLink(key))
                                    switch (size)
                                    {
                                        case IconSize.small:
                                            return sl.SmallIcon.ToBitmap();
                                        default: return sl.LargeIcon.ToBitmap();
                                    }
                        }
                    }
                }



                if (fileBasedFSFilter.Split(',').Contains(key))
                    return GetFileBasedFSBitmap(key, size);
                else
                    if (key == "" || key.StartsWith(".")) //Extension 
                    {
                        return GetFileIcon(key, size);
                    }
                    else
                        if (IsSpecialFolder(key))
                        {
                            ShellDll.PIDL pidlLookup = FileSystemInfoEx.FromString(key).PIDL;
                            try
                            {
                                Bitmap bm = GetFileIcon(pidlLookup.Ptr, size);
                                //BitmapImage bm2 =Imaging.CreateBitmapSourceFromHBitmap(
                                return bm;
                            }
                            finally { if (pidlLookup != null) pidlLookup.Free(); }
                        }
                        else
                            switch (size)
                            {
                                case IconSize.thumbnail:
                                    return loadThumbnail(key, isDiskFolder || isTemp);
                                //case IconSize.jumbo:
                                //case IconSize.extraLarge:
                                //    return loadJumbo(key, isDiskFolder || isTemp, size);
                                //case IconSize.large :
                                //    _imgList.ImageListSize = SysImageListSize.largeIcons;
                                //    return _imgList.Icon(_imgList.IconIndex(key, isDiskFolder)).ToBitmap();
                                //case IconSize.small :
                                //    _imgList.ImageListSize = SysImageListSize.smallIcons;
                                //    return _imgList.Icon(_imgList.IconIndex(key, isDiskFolder)).ToBitmap();
                                default:
                                    try
                                    {
                                        return GetFileIcon(key, size);
                                    }
                                    catch { return KeyToBitmap(UCUtils.GetProgramPath(), size); }
                            }
            }
            catch { return new Bitmap(1, 1); }
            ////bool isDiskFolder = key.EndsWith(":\\");

            ////string ext = PathEx.GetExtension(key);
            ////if ((ext != "" && ext != key && imageFilter.IndexOf(ext) != -1))
            ////{
            ////    return GetFileIcon(FileSystemInfoEx.FromString(key).PIDL.Ptr, size);
            ////}
            ////else

            ////    if (key == "" || key.StartsWith(".")) //Extension 
            ////    {
            ////        return GetFileIcon(key, size);
            ////    }
            ////    else
            ////        switch (size)
            ////        {
            ////            case IconSize.thumbnail:
            ////            case IconSize.jumbo:
            ////                return loadJumbo(key, isDiskFolder);
            ////            case IconSize.extraLarge:
            ////                _imgList.ImageListSize = SysImageListSize.extraLargeIcons;
            ////                return _imgList.Icon(_imgList.IconIndex(key, isDiskFolder)).ToBitmap();
            ////            //case IconSize.large :
            ////            //    _imgList.ImageListSize = SysImageListSize.largeIcons;
            ////            //    return _imgList.Icon(_imgList.IconIndex(key, isDiskFolder)).ToBitmap();
            ////            //case IconSize.small :
            ////            //    _imgList.ImageListSize = SysImageListSize.smallIcons;
            ////            //    return _imgList.Icon(_imgList.IconIndex(key, isDiskFolder)).ToBitmap();
            ////            default:
            ////                try
            ////                {
            ////                    return GetFileIcon(key, size);
            ////                }
            ////                catch { return KeyToBitmap(UCUtils.GetProgramPath(), size); }
            ////        }
        }

        protected Bitmap GetFileBasedFSBitmap(string ext, IconSize size)
        {
            string lookup = tempPath;
            Bitmap folderBitmap = KeyToBitmap(lookup, size);
            if (ext != "")
            {
                ext = ext.Substring(0, 1).ToUpper() + ext.Substring(1).ToLower();

                using (Graphics g = Graphics.FromImage(folderBitmap))
                {
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                    Font font = new Font("Comic Sans MS", folderBitmap.Width / 5, System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic);
                    float height = g.MeasureString(ext, font).Height;
                    float rightOffset = folderBitmap.Width / 5;

                    if (size == IconSize.small)
                    {
                        font = new Font("Arial", 5, System.Drawing.FontStyle.Bold);
                        height = g.MeasureString(ext, font).Height;
                        rightOffset = 0;
                    }


                    g.DrawString(ext, font,
                                System.Drawing.Brushes.Black,
                                new RectangleF(0, folderBitmap.Height - height, folderBitmap.Width - rightOffset, height),
                                new StringFormat(StringFormatFlags.DirectionRightToLeft));

                }
            }

            return folderBitmap;
        }

        private static Bitmap loadThumbnail(string lookup, bool forceLoadFromDisk)
        {
            string ext = PathEx.GetExtension(lookup).ToLower();
            if (!String.IsNullOrEmpty(ext) && imageFilter.IndexOf(ext) != -1)
                try
                {
                    return new Bitmap(FileSystemInfoEx.FromString(lookup).FullName);
                }
                catch { return new Bitmap(1, 1); }
            else return GetFileIcon(lookup, IconSize.large);
        }

        protected static string fileBasedFSFilter = ".zip,.7z,.lha,.lzh,.sqx,.cab,.ace";
        private static SysImageList _imgList = new SysImageList(SysImageListSize.jumbo);

        private static Bitmap loadJumbo(string lookup, bool forceLoadFromDisk)
        {
            _imgList.ImageListSize = isVistaUp() ? SysImageListSize.jumbo : SysImageListSize.extraLargeIcons;

            Icon icon = _imgList.Icon(_imgList.IconIndex(lookup, forceLoadFromDisk));
            Bitmap bitmap = icon.ToBitmap();
            icon.Dispose();

            System.Drawing.Color empty = System.Drawing.Color.FromArgb(0, 0, 0, 0);

            if (bitmap.Width < 256)
                bitmap = resizeImage(bitmap, new System.Drawing.Size(256, 256), 0);
            else if (bitmap.GetPixel(100, 100) == empty && bitmap.GetPixel(200, 200) == empty && bitmap.GetPixel(200, 200) == empty)
            {
                _imgList.ImageListSize = SysImageListSize.largeIcons;
                bitmap = resizeJumbo(_imgList.Icon(_imgList.IconIndex(lookup)).ToBitmap(), new System.Drawing.Size(200, 200), 5);
            }

            return bitmap;
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string key, fastKey;
            bool delayLoading;
            if (value is object[] && ((object[])value).Length > 0)
                value = ((object[])value)[0];

            ValueToKey(value, out key, out fastKey, out delayLoading);

            IconSize iconSize = SizeToIconSize(DefaultSize);
            if (parameter != null)
            {
                if (parameter is int)
                    iconSize = SizeToIconSize((int)parameter);
                if (parameter is string)
                {
                    int size;
                    if (Int32.TryParse(parameter as string, out size))
                        iconSize = SizeToIconSize(size);
                }
            }

            return GetIconImage(fastKey, key, iconSize, delayLoading);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IMultiValueConverter Members

        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            switch (values.Length)
            {
                case 1:
                    return Convert(values[0], targetType, parameter, culture);
                case 2:
                    if (values[1] is int)
                        return Convert(values[0], targetType, values[1], culture);
                    break;
            }
            return null;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}


