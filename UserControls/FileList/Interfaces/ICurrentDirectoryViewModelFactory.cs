using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using QuickZip.IO.PIDL.UserControls.ViewModel;

namespace QuickZip.IO.PIDL.UserControls 
{
    public interface ICurrentDirectoryViewModelFactory
    {
        CurrentDirectoryViewModel Create(FileListViewModel rootModel, Model.DirectoryModel model);

    }
}
