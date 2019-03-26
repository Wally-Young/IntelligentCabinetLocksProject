using System;
using System.Collections.Generic;
using System.Text;

namespace SerialTestNetCore
{
   public class Logical
    {
        

      

        public string CmddData { get; set; }

        public int  Result { get; set; }

        public string Error { get; set; }

        public string Success { get; set; }
        public  int NextState { get; set; }

        public Logical(string cmd,int result,string error,string success,int nextState)
        {
            this.CmddData = cmd;
            this.Result = result;
            this.Error = error;
            this.Success = success;
            this.NextState = nextState;
        }
    }
}
