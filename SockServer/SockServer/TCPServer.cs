using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Runtime.Serialization.Formatters.Binary;


namespace SockServer
{
    class TCPServer
    {
        static string newstr(string sstr = "")
        {
            string ostr = "";
            int i = 0;
            do
            {
                ostr += sstr[i];
                i++;
            } while (!sstr[i].Equals('\0'));
            return ostr;
        }

        static int main_interface()
        {
            Console.Clear();
            Console.WriteLine("Введите номер порта: \n");
            return Convert.ToInt16(Console.ReadLine());
        }

        static string smd5(string filename)
        {
            using (MD5 md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

                }
            }
        }

        static int port = main_interface();     //порт для приема входящих запросов
        static void Main(string[] args)
        {
            string path = "C:\\users\\sokol\\source\\repos\\Socket-Server\\";

            try
            {
                TcpListener clientListener = new TcpListener(port);
                clientListener.Start();
                Console.WriteLine("Сервер запущен. Ожидание подключений...");
                TcpClient client = clientListener.AcceptTcpClient();
                NetworkStream readerStream = client.GetStream();
                BinaryFormatter outformat = new BinaryFormatter();
                FileStream fs = new FileStream(path + Convert.ToString(port) + "_rc.txt", FileMode.Create);

                BinaryWriter bw = new BinaryWriter(fs);

                int count;
                count = int.Parse(outformat.Deserialize(readerStream).ToString());//Получаем размер файла
                int i = 0;
                for (; i < count; i += 1024)//Цикл пока не дойдём до конца файла
                {

                    byte[] buf = (byte[])(outformat.Deserialize(readerStream));//Собственно читаем из потока и записываем в файл
                    bw.Write(buf);
                }
                outformat = null;
                readerStream.Flush();
                readerStream.Close();
                bw.Close();
                fs.Close();
                clientListener.Stop();
                client.Close();

                Console.WriteLine("Готовится ответ");
                //Записываем контрольные суммы в файл
                using (StreamWriter ostream = new StreamWriter(path + Convert.ToString(port) + ".md5s", false, System.Text.Encoding.Default))
                {
                    using (StreamReader sr = new StreamReader(path + Convert.ToString(port) + "_rc.txt", System.Text.Encoding.Default))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            if (line.EndsWith("\0"))
                                line = newstr(line);
                            Console.WriteLine(line);
                            string outs = line + ":  " + smd5(line) + "\n";
                            ostream.WriteLine(outs);
                        }
                        sr.Close();
                    }
                    ostream.Close();
                }

                //-------------------------------------------------------------
                Console.WriteLine("Сервер отвечает");
                TcpClient eclient = new TcpClient("localhost", 7000);
                NetworkStream writerStream = eclient.GetStream();
                BinaryFormatter format = new BinaryFormatter();
                byte[] buff = new byte[1024];
                fs = new FileStream(path + Convert.ToString(port) + ".md5s", FileMode.Open);
                BinaryReader br = new BinaryReader(fs);
                long k = fs.Length;//Размер файла.
                format.Serialize(writerStream, k.ToString());//Вначале передаём размер
                while ((count = br.Read(buff, 0, 1024)) > 0)
                {
                    format.Serialize(writerStream, buff);//А теперь в цикле по 1024 байта передаём файл
                }
                buff = null;
                Console.WriteLine("Данные успешно переданы");
                fs.Close();
                long totalMemory = GC.GetTotalMemory(false);

                GC.Collect();
                GC.WaitForPendingFinalizers();
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.ReadLine();
            }
        }
    }
}
