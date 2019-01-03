using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace PhoneBackup
{
    internal class FTPclient
    {
        //поля
        //поле для хранения имени фтп-сервера
        private string _Host;

        //поле для хранения логина
        private string _UserName;

        //поле для хранения пароля
        private string _Password;

        //объект для запроса данных
        public FtpWebRequest ftpRequest;

        //объект для получения данных
        public FtpWebResponse ftpResponse;

        //флаг использования SSL
        private bool _UseSSL = false;

        //фтп-сервер
        public string Host
        {
            get => _Host;
            set => _Host = value;
        }
        //логин
        public string UserName
        {
            get => _UserName;
            set => _UserName = value;
        }
        //пароль
        public string Password
        {
            get => _Password;
            set => _Password = value;
        }
        //Для установки SSL-чтобы данные нельзя было перехватить
        public bool UseSSL
        {
            get => _UseSSL;
            set => _UseSSL = value;
        }
        //Реализеум команду LIST для получения подробного списока файлов на FTP-сервере
        public FileStruct[] ListDirectory(string path)
        {
            if (path == null || path == "")
            {
                path = "/";
            }
            //Создаем объект запроса
            ftpRequest = (FtpWebRequest)WebRequest.Create("ftp://" + _Host + path);
            //логин и пароль
            ftpRequest.Credentials = new NetworkCredential(_UserName, _Password);
            //команда фтп LIST
            ftpRequest.Method = WebRequestMethods.Ftp.ListDirectory;

            ftpRequest.EnableSsl = _UseSSL;
            //Получаем входящий поток
            ftpRequest.Timeout = 3000;
            ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();

            //переменная для хранения всей полученной информации
            string content = "";

            StreamReader sr = new StreamReader(ftpResponse.GetResponseStream(), Encoding.UTF8);
            content = sr.ReadToEnd();
            sr.Close();
            ftpResponse.Close();

            DirectoryListParser parser = new DirectoryListParser(content);
            return parser.FullListing;
        }

        public long DownloadFile(string path, string fileName)
        {
            // string path = currentPath;
            // string fileName = currentFileName;
            ftpRequest = (FtpWebRequest)WebRequest.Create("ftp://" + _Host + path);

            ftpRequest.Credentials = new NetworkCredential(_UserName, _Password);
            //команда фтп RETR
            ftpRequest.Method = WebRequestMethods.Ftp.DownloadFile;

            ftpRequest.EnableSsl = _UseSSL;
            FileStream downloadedFile = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite);
            ftpRequest.Timeout = 3000;
            ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
            //Получаем входящий поток
            Stream responseStream = ftpResponse.GetResponseStream();

            int blockSize = 1024;
            //Буфер для считываемых данных
            byte[] buffer = new byte[blockSize];
            long length = 0;

            int size = 0;
            size = responseStream.Read(buffer, 0, blockSize);
            length += size;
            while (size > 0)
            {
                downloadedFile.Write(buffer, 0, size);
                length += size;
                size = responseStream.Read(buffer, 0, blockSize);
            }
            ftpResponse.Close();
            downloadedFile.Close();
            responseStream.Close();
            return length;
        }
        //метод протокола FTP STOR для загрузки файла на FTP-сервер
        public void UploadFile(string path, string fileName)
        {
            //для имени файла
            string shortName = fileName.Remove(0, fileName.LastIndexOf(@"\") + 1);


            FileStream uploadedFile = new FileStream(fileName, FileMode.Open, FileAccess.Read);

            ftpRequest = (FtpWebRequest)WebRequest.Create("ftp://" + _Host + path + shortName);
            ftpRequest.Credentials = new NetworkCredential(_UserName, _Password);
            ftpRequest.EnableSsl = _UseSSL;
            ftpRequest.Method = WebRequestMethods.Ftp.UploadFile;

            //Буфер для загружаемых данных
            byte[] file_to_bytes = new byte[uploadedFile.Length];
            //Считываем данные в буфер
            uploadedFile.Read(file_to_bytes, 0, file_to_bytes.Length);

            uploadedFile.Close();

            //Поток для загрузки файла 
            Stream writer = ftpRequest.GetRequestStream();

            writer.Write(file_to_bytes, 0, file_to_bytes.Length);
            writer.Close();
        }
        //метод протокола FTP DELE для удаления файла с FTP-сервера 
        public void DeleteFile(string path)
        {
            ftpRequest = (FtpWebRequest)WebRequest.Create("ftp://" + _Host + path);
            ftpRequest.Credentials = new NetworkCredential(_UserName, _Password);
            ftpRequest.EnableSsl = _UseSSL;
            ftpRequest.Method = WebRequestMethods.Ftp.DeleteFile;

            FtpWebResponse ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
            ftpResponse.Close();
        }

        //метод протокола FTP MKD для создания каталога на FTP-сервере 
        public void CreateDirectory(string path, string folderName)
        {
            FtpWebRequest ftpRequest = (FtpWebRequest)WebRequest.Create("ftp://" + _Host + path + folderName);

            ftpRequest.Credentials = new NetworkCredential(_UserName, _Password);
            ftpRequest.EnableSsl = _UseSSL;
            ftpRequest.Method = WebRequestMethods.Ftp.MakeDirectory;

            FtpWebResponse ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
            ftpResponse.Close();
        }
        //метод протокола FTP RMD для удаления каталога с FTP-сервера 
        public void RemoveDirectory(string path)
        {
            string filename = path;
            FtpWebRequest ftpRequest = (FtpWebRequest)WebRequest.Create("ftp://" + _Host + path);
            ftpRequest.Credentials = new NetworkCredential(_UserName, _Password);
            ftpRequest.EnableSsl = _UseSSL;
            ftpRequest.Method = WebRequestMethods.Ftp.RemoveDirectory;

            FtpWebResponse ftpResponse = (FtpWebResponse)ftpRequest.GetResponse();
            ftpResponse.Close();
        }
    }
    //Для парсинга полученного детального списка каталогов фтп-сервера
    //Структура для хранения детальной информации о файде или каталоге
    public struct FileStruct
    {
        public string Flags;
        public string Owner;
        public bool IsDirectory;
        public string CreateTime;
        public string Name;
        public string path;
    }

    public enum FileListStyle
    {
        UnixStyle,
        WindowsStyle,
        Unknown
    }
    //Класс для парсинга
    public class DirectoryListParser
    {
        private List<FileStruct> _myListArray;

        public FileStruct[] FullListing => _myListArray.ToArray();

        public FileStruct[] FileList
        {
            get
            {
                List<FileStruct> _fileList = new List<FileStruct>();
                foreach (FileStruct thisstruct in _myListArray)
                {
                    if (!thisstruct.IsDirectory)
                    {
                        _fileList.Add(thisstruct);
                    }
                }
                return _fileList.ToArray();
            }
        }

        public FileStruct[] DirectoryList
        {
            get
            {
                List<FileStruct> _dirList = new List<FileStruct>();
                foreach (FileStruct thisstruct in _myListArray)
                {
                    if (thisstruct.IsDirectory)
                    {
                        _dirList.Add(thisstruct);
                    }
                }
                return _dirList.ToArray();
            }
        }

        public DirectoryListParser(string responseString)
        {
            _myListArray = GetList(responseString);
        }

        private List<FileStruct> GetList(string datastring)
        {
            List<FileStruct> myListArray = new List<FileStruct>();
            string[] dataRecords = datastring.Split('\n');
            //Получаем стиль записей на сервере
            FileListStyle _directoryListStyle = GuessFileListStyle(dataRecords);
            foreach (string s in dataRecords)
            {
                if (s != "")
                {
                    FileStruct f = new FileStruct();
                    f.Name = "..";
                    switch (_directoryListStyle)
                    {
                        case FileListStyle.UnixStyle:
                            f = ParseFileStructFromUnixStyleRecord(s);
                            break;
                        case FileListStyle.WindowsStyle:
                            f = ParseFileStructFromWindowsStyleRecord(s);
                            break;
                        case FileListStyle.Unknown:
                            f = ParseFileStructFromUnknownStyleRecord(s);
                            break;
                    }
                    if (f.Name != "" && f.Name != "." && f.Name != ".." && f.Name != null)
                    {
                        myListArray.Add(f);
                    }
                }
            }
            return myListArray;
        }
        FileStruct ParseFileStructFromUnknownStyleRecord(string Record)
        {
            FileStruct f = new FileStruct();
            string processstr = Record.Trim();
            //Получаем дату
            string dateStr = "";
            //Получаем время
            string timeStr = "";
            //Это папка или нет
            if (processstr.Contains("."))
            {
                f.IsDirectory = false;
            }
            else
            {
                f.IsDirectory = true;
            }
            //Остальное содержмое строки представляет имя каталога/файла
            f.Name = processstr;
            return f;
        }




        //Парсинг, если фтп сервера работает на Windows
        private FileStruct ParseFileStructFromWindowsStyleRecord(string Record)
        {
            //Предположим стиль записи 02-03-04  07:46PM       <DIR>     Append
            FileStruct f = new FileStruct();
            string processstr = Record.Trim();
            //Получаем дату
            string dateStr = processstr.Substring(0, 8);
            processstr = (processstr.Substring(8, processstr.Length - 8)).Trim();
            //Получаем время
            string timeStr = processstr.Substring(0, 7);
            processstr = (processstr.Substring(7, processstr.Length - 7)).Trim();
            f.CreateTime = dateStr + " " + timeStr;
            //Это папка или нет
            if (processstr.Substring(0, 5) == "<DIR>")
            {
                f.IsDirectory = true;
                processstr = (processstr.Substring(5, processstr.Length - 5)).Trim();
            }
            else
            {
                string[] strs = processstr.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                processstr = strs[1];
                f.IsDirectory = false;
            }
            //Остальное содержмое строки представляет имя каталога/файла
            f.Name = processstr;
            return f;
        }
        //Получаем на какой ОС работает фтп-сервер - от этого будет зависеть дальнейший парсинг
        public FileListStyle GuessFileListStyle(string[] recordList)
        {
            foreach (string s in recordList)
            {
                //Если соблюдено условие, то используется стиль Unix
                if (s.Length > 10
                    && Regex.IsMatch(s.Substring(0, 10), "(-|d)((-|r)(-|w)(-|x)){3}"))
                {
                    return FileListStyle.UnixStyle;
                }
                //Иначе стиль Windows
                else if (s.Length > 8
                    && Regex.IsMatch(s.Substring(0, 8), "[0-9]{2}-[0-9]{2}-[0-9]{2}"))
                {
                    return FileListStyle.WindowsStyle;
                }
            }
            return FileListStyle.Unknown;
        }
        //Если сервер работает на nix-ах
        private FileStruct ParseFileStructFromUnixStyleRecord(string record)
        {
            //Предположим. тчо запись имеет формат dr-xr-xr-x   1 owner    group    0 Nov 25  2002 bussys
            FileStruct f = new FileStruct();
            if (record[0] == '-' || record[0] == 'd')
            {// правильная запись файла
                string processstr = record.Trim();
                f.Flags = processstr.Substring(0, 9);
                f.IsDirectory = (f.Flags[0] == 'd');
                processstr = (processstr.Substring(11)).Trim();
                //отсекаем часть строки
                _cutSubstringFromStringWithTrim(ref processstr, ' ', 0);
                f.Owner = _cutSubstringFromStringWithTrim(ref processstr, ' ', 0);
                f.CreateTime = getCreateTimeString(record);
                //Индекс начала имени файла
                int fileNameIndex = record.IndexOf(f.CreateTime) + f.CreateTime.Length;
                //Само имя файла

                var substrings = record.Split(' ');
                if (record.IndexOf(':') != -1)
                {
                    for (int i = 0; i < substrings.Length; i++)
                    {
                        if (substrings[i].IndexOf(':') != -1)
                        {
                            for (int j = i + 1; j < substrings.Length; j++)
                                if (j > i + 1)
                                    f.Name += " " + substrings[j].Replace("\r", "");
                                else
                                    f.Name += substrings[j].Replace("\r", "");
                            break;
                        }
                    }
                    if (f.Name == null)
                    {
                        f.Name = f.Name;
                    }
                }
                else
                {
                    for (int i = 0; i < substrings.Length; i++)
                    {
                        for (int y = 0; y < 30; y++)
                        {
                            if (substrings[i].IndexOf("20" + y.ToString()) != -1)
                            {
                                for (int j = i + 1; j < substrings.Length; j++)
                                    if (j > i + 1)
                                        f.Name += " " + substrings[j].Replace("\r", "");
                                    else
                                        f.Name += substrings[j].Replace("\r", "");
                                break;
                            }
                        }
                    }
                    for (int i = 0; i < substrings.Length; i++)
                    {

                        if (substrings[i].IndexOf("1970") != -1)
                        {
                            for (int j = i + 1; j < substrings.Length; j++)
                                if (j > i + 1)
                                    f.Name += " " + substrings[j].Replace("\r", "");
                                else
                                    f.Name += substrings[j].Replace("\r", "");
                            break;
                        }

                    }
                    if (f.Name == null)
                    {
                        f.Name = f.Name;
                    }
                }

            }
            else
            {
                f.Name = "";
            }

            return f;
        }

        private string getCreateTimeString(string record)
        {
            //Получаем время
            /*  string month = "(jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec)";
              string space = @"(\040)+";
              string day = "([0-9]|[1-3][0-9])";
              string year = "[1-2][0-9]{3}";
              string time = "[0-9]{1,2}:[0-9]{2}";
              Regex dateTimeRegex = new Regex(month + space + day + space + "(" + year + "|" + time + ")", RegexOptions.IgnoreCase);
              Match match = dateTimeRegex.Match(record);
              return match.Value;*/
            return "";
            //return record.Split(' ')[record.Split(' ').Length - 3]+" "+record.Split(' ')[record.Split(' ').Length - 2];
        }

        private string _cutSubstringFromStringWithTrim(ref string s, char c, int startIndex)
        {
            int pos1 = s.IndexOf(c, startIndex);
            string retString = s.Substring(0, pos1);
            s = (s.Substring(pos1)).Trim();
            return retString;
        }
    }
}
