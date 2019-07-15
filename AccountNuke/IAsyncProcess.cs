using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AccountNuke
{
    public interface IAsyncProcess
    {
         Task Execute(string RoleARN);
    }
}
