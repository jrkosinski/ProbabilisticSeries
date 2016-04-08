using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Trading.Utilities.Data
{
    public interface IDataReader
    {
        DataSet Read(string source);
    }
}
