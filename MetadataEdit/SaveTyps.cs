using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Metadataviewer
{
    public class SaveTyps : ViewModelBase
    {

        private string savetyp;
        public string SaveType
        {
            get => savetyp;
            set=> SetProperty(ref savetyp, value);  
        }

        private string savetypvalue;
        public string SaveTypeValue
        {
            get => savetypvalue;   
            set => SetProperty(ref savetypvalue, value);   
        }

    }
}
