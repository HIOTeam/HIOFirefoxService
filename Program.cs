using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using System.IO;
using Newtonsoft.Json;
using System.Diagnostics;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using System.Security.AccessControl;
using Microsoft.Win32;
using System.IO.MemoryMappedFiles;
using System.Messaging;

namespace Firefox
{

   
    // sends to a queue and receives from a queue.
    public class SendData
    {
        public string label;
        public string data;
    };

    /// <summary>
    /// Creates the app service connection
    /// </summary>
    public class Firefox
    {
        public static string PATHMSGREC = @".\Private$\MSGHIO1FF"+ System.Security.Principal.WindowsIdentity.GetCurrent().Name.Split('\\').Last();
        public static  string PATHMSGSEND = @".\Private$\MSGHIO2"+ System.Security.Principal.WindowsIdentity.GetCurrent().Name.Split('\\').Last();
        public static string BROWSER = "Firefox";
        

        private static AutoResetEvent _signal = new AutoResetEvent(false);
        private static AutoResetEvent _signalRec = new AutoResetEvent(false);
        static bool checkRec = false;
        public static  bool killProcess(string processName,int ownerId)
        {

            Process[] processInstances = Process.GetProcessesByName(processName);

            foreach (Process p in processInstances)
                if (p.Id != ownerId)
                    p.Kill();
            return true;
        }
        public static void Main()
        {

            try
            {
               

                Process currentProcess = Process.GetCurrentProcess();
                killProcess("HIOFirefox", currentProcess.Id);

                Thread thread2 = new Thread(() => threadReadData());
                thread2.SetApartmentState(ApartmentState.STA);
                thread2.IsBackground = true;
                thread2.Start();


                Thread thread = new Thread(() => threadWriteData());
                thread.SetApartmentState(ApartmentState.STA);
                thread.IsBackground = true;
                thread.Start();

                _signal.WaitOne();
            }catch(Exception ex){
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

                System.IO.StreamWriter file = new System.IO.StreamWriter(Path.GetTempPath() + "\\log_HIO" + BROWSER + ".log", true);
                file.WriteLine(DateTime.Now + " "+BROWSER+":   " + fileName + "   " + methodName + "      " + ex.Message + line + col);
                file.Close();
            }
        }

        private static void threadReadData()
        {

            // Receive a message from a queue.
            ReceiveMessage();
        }
        private static bool threadWriteData()
        {
            // Send a message to a queue.
            SendMessage();
            return false;
        }


        //**************************************************
        // Sends an Order to a queue.
        //**************************************************

        public static void SendMessage()
        {
            try
            {
                // Create a new order and set values.
                SendData sentOrder = new SendData();
                sentOrder.label = BROWSER; //set header

                while (true)
                {
                    char[] data = null;
                    while ((data = Read()) != null)
                    {
                        try
                        {

                            sentOrder.data = new string(data);
                            if (sentOrder.data == "") continue;
                            JObject datatest = (JObject)JsonConvert.DeserializeObject<JObject>(sentOrder.data);
                            if (datatest["CMD"].Value<string>() == "exit")
                            {
                                Environment.Exit(0);
                            }

                            // Connect to a queue on the local computer.
                            if (!MessageQueue.Exists(PATHMSGSEND))
                                MessageQueue.Create(PATHMSGSEND);
                            MessageQueue myQueue = new MessageQueue(PATHMSGSEND);

                            // Send the Order to the queue.
                            myQueue.Send(sentOrder);
                            myQueue.Purge();

                            Thread threadTimeout = new Thread(() => threadTimeoutResponse(3000));
                            threadTimeout.SetApartmentState(ApartmentState.STA);
                            threadTimeout.Start();
                            _signalRec.WaitOne();

                            Thread.Sleep(200);
                            if (checkRec == false)
                            {
                                if (datatest["CMD"].Value<string>() == "INIT")
                                {
                                    Write("{\"CMD\":\"CONNCETION\",\"data\":\"false\"}");

                                }

                                System.IO.StreamWriter file = new System.IO.StreamWriter(Path.GetTempPath() + "\\log_HIO" + BROWSER + ".log", true);
                                file.WriteLine(DateTime.Now + " " + BROWSER + ": Timeout recieve data!");
                                file.Close();
                                _signalRec.Reset();
                            }
                            else
                            {
                                checkRec = false;
                                _signalRec.Reset();
                            }
                        }
                        catch
                        {
                            continue;

                        }

                    }
                }
            }
            catch (Exception ex)
            {

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

                System.IO.StreamWriter file = new System.IO.StreamWriter(Path.GetTempPath() + "\\log_HIO" + BROWSER + ".log", true);
                file.WriteLine(DateTime.Now + " " + BROWSER + ":   " + fileName + "   " + methodName + "      " + ex.Message + line + col);
                file.Close();
            }
        }
        private static void threadTimeoutResponse(int miliSec)
        {
            Thread.Sleep(miliSec);
            _signalRec.Set();

        }
        public static char[] Read()
        {
            try
            {

                var stdin = Console.OpenStandardInput();
                var length = 0;

                var lengthBytes = new byte[4];
                stdin.Read(lengthBytes, 0, 4);
                length = BitConverter.ToInt32(lengthBytes, 0);
                if (length == 0)
                    Environment.Exit(0);
                var buffer = new char[length];
                using (var reader = new StreamReader(stdin))
                {
                    while (reader.Peek() >= 0)
                    {
                        reader.Read(buffer, 0, buffer.Length);

                    }
                }
                return buffer;

            }
            catch (Exception ex)
            {
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

                System.IO.StreamWriter file = new System.IO.StreamWriter(Path.GetTempPath() + "\\log_HIO" + BROWSER + ".log", true);
                file.WriteLine(DateTime.Now + " " + BROWSER + ":   " + fileName + "   " + methodName + "      " + ex.Message + line + col);
                file.Close();
                return null;
            }
        }
        public static void Write(JToken data)
        {
            try
            {

                var json = new JObject();
                json["data"] = data;
               // System.Windows.Forms.MessageBox.Show(json.ToString(Formatting.None),"write chrome");
                var bytes = System.Text.Encoding.UTF8.GetBytes(json.ToString(Formatting.None));

                var stdout = Console.OpenStandardOutput();
                stdout.WriteByte((byte)((bytes.Length >> 0) & 0xFF));
                stdout.WriteByte((byte)((bytes.Length >> 8) & 0xFF));
                stdout.WriteByte((byte)((bytes.Length >> 16) & 0xFF));
                stdout.WriteByte((byte)((bytes.Length >> 24) & 0xFF));
                stdout.Write(bytes, 0, bytes.Length);
                stdout.Flush();
            }
            catch (Exception ex)
            {
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

                System.IO.StreamWriter file = new System.IO.StreamWriter(Path.GetTempPath() + "\\log_HIO" + BROWSER + ".log", true);
                file.WriteLine(DateTime.Now + " " + BROWSER + ":   " + fileName + "   " + methodName + "      " + ex.Message + line + col);
                file.Close();
            }

        }
        //**************************************************
        // Receives a message containing an Order.
        //**************************************************

        public static void ReceiveMessage()
        {
            try
            {
                // Connect to the a queue on the local computer.
                if (!MessageQueue.Exists(PATHMSGREC))
                    MessageQueue.Create(PATHMSGREC);
                if (!MessageQueue.Exists(PATHMSGSEND))
                    MessageQueue.Create(PATHMSGSEND);
                MessageQueue myQueueRec = new MessageQueue(PATHMSGREC);
                MessageQueue myQueueSend = new MessageQueue(PATHMSGSEND);

                // Set the formatter to indicate body contains an Order.
                myQueueRec.Formatter = new XmlMessageFormatter(new Type[] { typeof(SendData) });
                SendData sentOrder = new SendData();
                sentOrder.label = BROWSER; //set header
                sentOrder.data = "true";
                while (true)
                {

                    try
                    {

                        // Receive and format the message. 
                        Message myMessage = myQueueRec.Receive();

                        myQueueRec.Purge();

                        SendData dataPack = (SendData)myMessage.Body;
                        if (dataPack.data == "true")
                        {
                            checkRec = true;
                            _signalRec.Set();
                            continue;
                        }
                        else
                            myQueueSend.Send(sentOrder);
                        // Display message information.

                        JObject datatest = (JObject)JsonConvert.DeserializeObject<JObject>(dataPack.data.Trim().Replace("\0", ""));
                        if (datatest["CMD"].Value<string>() == "exit")
                        {
                            Environment.Exit(0);
                        }
                        Write(dataPack.data.Trim().Replace("\0", ""));


                    }
                    // Catch other exceptions as necessary.
                    catch (Exception ex)
                    {
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

                        System.IO.StreamWriter file = new System.IO.StreamWriter(Path.GetTempPath() + "\\log_HIO"+BROWSER+".log", true);
                        file.WriteLine(DateTime.Now + " " + BROWSER + ":   " + fileName + "   " + methodName + "      " + ex.Message + line + col);
                        file.Close();
                    }


                }
            }catch(Exception ex){
          
            }
      
        }
    }
}