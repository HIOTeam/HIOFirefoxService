using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace HIOChrome
{
    class ErrorHandle
    {
        public void ErrorFunc(Exception ex){
        
         StackTrace st = new StackTrace(ex, true);
               //Get the first stack frame
               StackFrame frame = st.GetFrame(0);

               //Get the file name
               string fileName = frame.GetFileName();

               //Get the method name
               string methodName = frame.GetMethod().Name;

               //Get the line number from the stack frame
               int line = frame.GetFileLineNumber();

               //Get the column number
               int col = frame.GetFileColumnNumber();

               System.IO.StreamWriter file = new System.IO.StreamWriter(Path.GetTempPath() + "\\log_HIOChrome.log", true);
               file.WriteLine(DateTime.Now+ " Chrome:   "+ fileName + "   " + methodName + "      " + ex.Message + line + col);
               file.Close();
        }
        public void ErrorFunc(string err)
        {



            System.IO.StreamWriter file = new System.IO.StreamWriter(Path.GetTempPath() + "\\log_HIOChrome.log", true);
            file.WriteLine(DateTime.Now+ " Error: "+err);
            file.Close();
        }
    }
}
