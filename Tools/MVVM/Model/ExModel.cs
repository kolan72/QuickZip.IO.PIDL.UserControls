using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cinch;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;
using System.IO.Tools;

namespace QuickZip.IO.PIDL.UserControls.Model
{

    public abstract class ExModel : Cinch.ValidatingObject
    {
        #region Constructor       
        protected ExModel(FileSystemInfoEx entry)
        {
            if (entry == null) 
                return;
            //TODO Добавить длину файла
            Name = entry.Name;
            Label = entry.Label;
            FullName = entry.FullName;
            //0.2
            LastAccessTime = entry.LastAccessTime;
            LastWriteTime = entry.LastWriteTime;
            CreationTime = entry.CreationTime;
            EntryType = entry.IsFolder ? entry.FullName.EndsWith(":\\") ? "Drive" : "Directory" : FileTypeInfoProvider.GetFileTypeInfo(PathEx.GetExtension(Name)).FileType;
            //if (FullName.StartsWith("\\\\") ||  //Network directory
            //    (FullName.IndexOf('\\') == -1 && !FullName.StartsWith("::")) //Directory without path and not guid.
            //    || entry.IsVirtual) //Virtual directory                                
            IsEditable = (entry.Attributes | System.IO.FileAttributes.ReadOnly) != 0;
            _embeddedEntry = entry;
            //+AK
            Attribs = (entry.Attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly ? "r" : "-";
            Attribs += (entry.Attributes & FileAttributes.Archive) == FileAttributes.Archive ? "a" : "";
        }

        static ExModel()
        {

        }

        //0.3
        public static ExModel FromExEntry(FileSystemInfoEx entry)
        {
            if (entry is DirectoryInfoEx)
                return FromExEntry(entry as DirectoryInfoEx);
            else return FromExEntry(entry as FileInfoEx);
        }


        public static ExModel FromExEntry(FileInfo entry)
        {
            return new FileModel(entry as FileInfo);
        }


        public static FileModel FromExEntry(FileInfoEx entry)
        {
            return new FileModel(entry as FileInfoEx);
        }

        public static DirectoryModel FromExEntry(DirectoryInfoEx entry)
        {
            if (entry.FullName.EndsWith(":\\"))
                return new DriveModel(entry);
            else
                return new DirectoryModel(entry as DirectoryInfoEx);
        }


        public static ExModel FromExEntry()
        {
            return null;
        }

        #endregion


        #region Methods

        public virtual void Refresh()
        {

        }

        public override bool Equals(object obj)
        {
            if (obj is ExModel)
            {
                if (obj is FileModel)
                {
                    return ((obj as ExModel).FullName.Equals(FullName)
                        && (obj as ExModel).LastWriteTime.Equals(LastWriteTime)
                        && (obj as ExModel).EntryType.Equals(EntryType)
                        && (obj as ExModel).IsEditable.Equals(IsEditable)
                        && (obj as ExModel).CreationTime.Equals(CreationTime)
                        //LastAccessTime может поменяться как при просмотре свойств файла, так и при добавлении нового, поэтому для экономии времени не обновляем
                        ////&& (obj as ExModel).LastAccessTime.Equals(LastAccessTime)
                        //TODO Возможно продолжить
                    );
                }
                else
                { 
                    if(obj is DirectoryModel)   
                        return ((obj as ExModel).FullName.Equals(FullName));
                    else
                        throw new NotImplementedException("сравнение не реализовано!");
                }
            }

            return EmbeddedEntry.Equals(obj);
        }

        public override int GetHashCode()
        {
            return FullName.GetHashCode();
        }
        #endregion

        #region Data
        private string _name;
        private string _fullName;        
        private string _label;
        private string _entryType;
        private bool _isEditable;
        private DateTime _creationTime, _lastWriteTime, _lastAccessTime;
        private string _attribs;
        private FileSystemInfoEx _embeddedEntry = null;
        #endregion

        #region Public Properties

        /// <summary>
        /// DirectoryName
        /// </summary>        
        static PropertyChangedEventArgs nameChangeArgs =
            ObservableHelper.CreateArgs<ExModel>(x => x.Name);
        public string Name
        {
            get { return _name; }
            set
            {
                if (!String.IsNullOrEmpty(_name) && _name != value)
                {
                    string newName = PathEx.Combine(PathEx.GetDirectoryName(FullName), value);
                    FileSystemInfoEx entry = EmbeddedEntry;
                    string origName = _name;
                    string origFullName = _fullName;                    
                    _name = value;
                    _fullName = newName;
                    
                    try
                    {
                        IOTools.Rename(entry.FullName, PathEx.GetFileName(_fullName));
                        FullName = newName;

                        Label = EmbeddedEntry.Label;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Rename failed");
                        _name = origName;
                        _fullName = origFullName;
                        return;
                    }
                }
                else _name = value;  
                              

                NotifyPropertyChanged(nameChangeArgs);
            }
        }


        /// <summary>
        /// FullName
        /// </summary>
        static PropertyChangedEventArgs parseNameChangeArgs =
            ObservableHelper.CreateArgs<ExModel>(x => x.FullName);

        public string FullName
        {
            get { return _fullName; }
            set
            {
                _fullName = value;
                NotifyPropertyChanged(parseNameChangeArgs);
            }
        }


        /// <summary>
        /// Label
        /// </summary>
        static PropertyChangedEventArgs labelChangeArgs =
            ObservableHelper.CreateArgs<ExModel>(x => x.Label);

        public string Label
        {
            get { return _label; }
            set
            {
                _label = value;
                NotifyPropertyChanged(labelChangeArgs);
            }
        }

        /// <summary>
        /// Type
        /// </summary>
        static PropertyChangedEventArgs entryTypeChangeArgs =
            ObservableHelper.CreateArgs<ExModel>(x => x.EntryType);

        public string EntryType
        {
            get { return _entryType; }
            set
            {
                _entryType = value;
                NotifyPropertyChanged(entryTypeChangeArgs);
            }
        }

        /// <summary>
        /// CreationTime
        /// </summary>
        static PropertyChangedEventArgs CreationTimeChangeArgs =
            ObservableHelper.CreateArgs<ExModel>(x => x.CreationTime);

        public DateTime CreationTime
        {
            get { return _creationTime; }
            set
            {
                _creationTime = value;
                NotifyPropertyChanged(CreationTimeChangeArgs);
            }
        }

        /// <summary>
        /// LastWriteTime
        /// </summary>
        static PropertyChangedEventArgs lastWriteTimeChangeArgs =
            ObservableHelper.CreateArgs<ExModel>(x => x.LastWriteTime);

        public DateTime LastWriteTime
        {
            get { return _lastWriteTime; }
            set
            {
                _lastWriteTime = value;
                NotifyPropertyChanged(lastWriteTimeChangeArgs);
            }
        }

        /// <summary>
        /// LastAccessTime
        /// </summary>
        static PropertyChangedEventArgs lastAccessTimeChangeArgs =
            ObservableHelper.CreateArgs<ExModel>(x => x.LastAccessTime);

        public DateTime LastAccessTime
        {
            get { return _lastAccessTime; }
            set
            {
                _lastAccessTime = value;
                NotifyPropertyChanged(lastAccessTimeChangeArgs);
            }
        }

        //+AK
        static PropertyChangedEventArgs attribsArgs =
           ObservableHelper.CreateArgs<ExModel>(x => x.Attribs);
        public string Attribs
        {
            get { return _attribs; }
            set
            {
                _attribs = value;
                NotifyPropertyChanged(attribsArgs);
            }
        }

        /// <summary>
        /// IsEditable
        /// </summary>
        static PropertyChangedEventArgs isEditableChangeArgs =
            ObservableHelper.CreateArgs<ExModel>(x => x.IsEditable);

        public bool IsEditable
        {
            get { return _isEditable; }
            set
            {
                _isEditable = value;                
                NotifyPropertyChanged(isEditableChangeArgs);
            }
        }

        /// <summary>
        /// EmbeddedEntry
        /// </summary>
        static PropertyChangedEventArgs embeddedEntryChangeArgs =
            ObservableHelper.CreateArgs<ExModel>(x => x.EmbeddedEntry);

        public FileSystemInfoEx EmbeddedEntry
        {
            get
            {
               if (_embeddedEntry == null)
                    _embeddedEntry = FileSystemInfoEx.FromString(FullName);
                return _embeddedEntry; }
        }

        #endregion

    }



}
