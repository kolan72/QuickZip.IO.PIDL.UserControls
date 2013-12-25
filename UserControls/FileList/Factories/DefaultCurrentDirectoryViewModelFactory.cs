using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using QuickZip.IO.PIDL.UserControls.ViewModel;


namespace QuickZip.IO.PIDL.UserControls
{
    public class DefaultCurrentDirectoryViewModelFactory :ICurrentDirectoryViewModelFactory
    {
        public CurrentDirectoryViewModel Create(FileListViewModel rootModel, Model.DirectoryModel model)
        { return new CurrentDirectoryViewModel(rootModel, model); ; }
    }
}
