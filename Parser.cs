using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using System.Net;

namespace gomeltrans_net_parser
{
    delegate void delegostanovkaParsed(Ostanovka ostanovka);
    delegate void delegBusParsed(Marshrut bus);
    delegate void delegEnd();
    delegate void delegEndFile();
    internal sealed class Parser
    {
        public event delegostanovkaParsed ostanovkaParsed;
        public event delegBusParsed BusParsed;
        public event delegEnd End;
        public event delegEndFile EndFile;

        List<Marshrut> marshruts = new List<Marshrut>();
            int count = 0;
        public void Parse(object o)
        {

                // Парсинг автобусов
            string site = "http://gomeltrans.net/routes/bus/";
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(site);
            HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
            Stream stream = resp.GetResponseStream();
            StreamReader reader = new StreamReader(stream, Encoding.GetEncoding(1251));
            string response = reader.ReadToEnd();
            string[] buses = response.Split(new string[] { "<a href=\"/routes/bus/" }, StringSplitOptions.RemoveEmptyEntries);
            Parse1(buses, Type.Bus);

                // парсинг троллейбусов
            site = "http://gomeltrans.net/routes/trolleybus/";
            req = (HttpWebRequest)WebRequest.Create(site);
            resp = (HttpWebResponse)req.GetResponse();
            stream = resp.GetResponseStream();
            reader = new StreamReader(stream, Encoding.GetEncoding(1251));
            response = reader.ReadToEnd();
            string[] trolleyBuses = response.Split(new string[] { "<a href=\"/routes/trolleybus/" }, StringSplitOptions.RemoveEmptyEntries);
            Parse1(trolleyBuses, Type.trolley);

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("start;stop;town;vid;marsh;typeday;napr;ostan;num_ostan;time;go1;go2;go3;go4;go5;go6;go7;go8;enabled;info;unik;depo1;depo2;depo3;depo4;depo5;depo6;depo7");

            foreach (Marshrut marshrut in marshruts)
            {
                foreach (Ostanovka ostanovka in marshrut.ostanovki)
                {
                    builder.AppendLine(";;Гомель;" + marshrut.type.ToString() + ";" + marshrut.marsh + ";0;" + marshrut.napr + ";" + ostanovka.name + ";" + ostanovka.num_ost + ";2;"+ ostanovka.time[Day.mon]+";"+
                        ostanovka.time[Day.tue] + ";" + ostanovka.time[Day.wed] + ";" + ostanovka.time[Day.thr] + ";" + ostanovka.time[Day.fr] + ";" + 
                        ostanovka.time[Day.sut] + ";" + ostanovka.time[Day.sun] + ";" + ostanovka.time[Day.holyday] + ";" +"1");
                }
            }
            End();
            File.AppendAllText("xyz.csv", builder.ToString());
            EndFile();

        }

        private void Parse1(string[] transport, Type type)
        {

            string[] hrefs = new string[transport.Length - 1];
            for (int i = 1; i < transport.Length; i++)
            {
                if (type==Type.Bus)
                    hrefs[i - 1] = "http://gomeltrans.net/routes/bus/" + transport[i].Split(new char[] { '/' })[0];
                if (type == Type.trolley)
                    hrefs[i - 1] = "http://gomeltrans.net/routes/trolleybus/" + transport[i].Split(new char[] { '/' })[0];
            }

            for (int i = 0; i < hrefs.Length; i++)
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(hrefs[i]);
                //HttpWebRequest req = (HttpWebRequest)WebRequest.Create("http://gomeltrans.net/routes/trolleybus/6/");
                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
                Stream stream = resp.GetResponseStream();
                StreamReader reader = new StreamReader(stream, Encoding.GetEncoding(1251));
                string response = reader.ReadToEnd();
                string info;

                    // left
                string left = ParseBetweenStrings(response, "<td class=\"t-left\">", "<td class=\"t-right\">");
                if (left == "")
                    left = ParseBetweenStrings(response, "<td class=\"t-left\">", "<hr class=\"timetable-divider\">");
                Marshrut marshrutLeft = new Marshrut();
                info = ParseBetweenStrings(left, "<div class=\"t-elem\">", "</table>");  
                marshrutLeft.marsh = ParseBetweenStrings(left, "<div class=\"route-number\">", "</div>");
                marshrutLeft.napr = ParseBetweenStrings(left, "<td class=\"route-stop1\">", "<tr>").Trim() + " - " + ParseBetweenStrings(left, "<td class=\"route-stop2\">", "</table>").Trim();
                marshrutLeft.type = type;
                marshrutLeft.ostanovki = GetOstanovki(left);
                marshruts.Add(marshrutLeft);
                BusParsed(marshrutLeft);

                    // right
                string right = ParseBetweenStrings(response, "<td class=\"t-right\">", "<hr class=\"timetable-divider\">");
                if (right != "")
                {
                    Marshrut marshrutRight = new Marshrut();
                    info = ParseBetweenStrings(right, "<div class=\"t-elem\">", "</table>");          
                    marshrutRight.marsh = ParseBetweenStrings(right, "<div class=\"route-number\">", "</div>");
                    marshrutRight.napr = ParseBetweenStrings(right, "<td class=\"route-stop1\">", "<tr>").Trim() + " - " + ParseBetweenStrings(right, "<td class=\"route-stop2\">", "</table>").Trim();
                    marshrutRight.type = type;
                    marshrutRight.ostanovki = GetOstanovki(right);
                    marshruts.Add(marshrutRight);
                    BusParsed(marshrutRight);
                }
                count++;
                if (count > 1)
                    return;
            }
        }

        private List<Ostanovka> GetOstanovki(string marshrut)
        {
            List<Ostanovka> ostanovki = new List<Ostanovka>();
            string[] ost = marshrut.Split(new string[] { "<div class=\"t-elem t-elem-stop\">" }, StringSplitOptions.RemoveEmptyEntries);     // получили htmlы с остановками
            for (int i = 1; i < ost.Length; i++)
            {
                Ostanovka ostanovka = new Ostanovka();
                ostanovka.num_ost = ParseBetweenStrings(ost[i], "<td class=\"stop-number\">", "<td class=\"stop-name\">").TrimEnd();
                string href = ParseBetweenStrings(ost[i], "<a href=\"", "\"");
                //string href = ParseBetweenStrings(ost[i], "<a href=\"", "\">");
                ostanovka.name = ParseBetweenStrings(ost[i], href + "\">", "</a>");
                href = "http://gomeltrans.net" + href;
                ostanovka.time = GetTime(href);

                ostanovki.Add(ostanovka);
                ostanovkaParsed(ostanovka);
            }
            return ostanovki;
        }

        private Dictionary<Day, string> GetTime(string URL)        // словарь: день недели - расписание
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(URL);
            HttpWebResponse resp = (HttpWebResponse)req.GetResponse();
            Stream stream = resp.GetResponseStream();
            StreamReader reader = new StreamReader(stream, Encoding.GetEncoding(1251));
            string response = reader.ReadToEnd();

            Dictionary<Day, string> dic = new Dictionary<Day, string>();
            dic.Add(Day.mon, "");
            dic.Add(Day.tue, "");
            dic.Add(Day.wed, "");
            dic.Add(Day.thr, "");
            dic.Add(Day.fr, "");
            dic.Add(Day.sut, "");
            dic.Add(Day.sun, "");
            dic.Add(Day.holyday, "");
            List<string> days = SplitBetweenTags(response, "<div class=\"schedule-graphic\">", new string[] { "<div" }, "</div>");
            for (int i = 0; i < days.Count; i++)        // проходимся по каждому дню
            {
                int endIndex = 0;
                string full = ParseBetweenTags(days[i], "<div class=\"schedule-full\">", new string[] { "<div" }, "</div>", 0, out endIndex);
                string dayName = ParseBetweenTags(days[i], "<h2 class=\"schedule-graphic-name", new string[] { }, "</h2>", 0, out endIndex);

                List<string> listHours = SplitBetweenTags(full, "<div class=\"sch-hour\">", new string[] { "<div" }, "</div>");
                string shedule = " ";   // пробел в начале по требованию заказчика
                                        // формируем часы и минуты по дню
                foreach (string hours in listHours)
                {
                    string hour = ParseBetweenStrings(hours, "<div class=\"sch-h\">", "</div>");
                    if (hour == "")
                        hour = ParseBetweenStrings(hours, "<div class=\"sch-h sch-h-1h\">", "</div>");
                    List<string> minutesList = new List<string>();
                    minutesList = SplitBetweenTags(hours, "class=\"sch-m\">", new string[] { }, "</div>");

                    // class="sch-m sch-v sch-v2 sch-next">

                    List<string> next = SplitBetweenTags(hours, "sch-next\">", new string[] { }, "</div>");   // один из автобусов будет следующим на данный момент времени. у него другой тег                   
                    List<string> underLinedGreen = SplitBetweenTags(hours, "class=\"sch-m sch-v sch-v1\">", new string[] { }, "</div>");   // некоторые автобусы подчёркнуты зеленым
                    List<string> underLinedRed = SplitBetweenTags(hours, "class=\"sch-m sch-v sch-v2\">", new string[] { }, "</div>");   // некоторые автобусы подчёркнуты красным
                    List<string> underLinedBlue = SplitBetweenTags(hours, "class=\"sch-m sch-v sch-v3\">", new string[] { }, "</div>");   // некоторые автобусы подчёркнуты синим
                    List<string> underLinedOrange = SplitBetweenTags(hours, "class=\"sch-m sch-v sch-v4\">", new string[] { }, "</div>");   // некоторые автобусы подчёркнуты оранжевым
                    minutesList.AddRange(next);
                    minutesList.AddRange(underLinedGreen);
                    minutesList.AddRange(underLinedRed);
                    minutesList.AddRange(underLinedBlue);
                    minutesList.AddRange(underLinedOrange);
                    minutesList.Sort();         // отсортируем чтоб все автобусы были на своих местах

                    foreach (string minute in minutesList)
                    {
                        shedule += (hour + ":" + minute + ",");
                    }
                }
                shedule = shedule.Substring(0, shedule.Length - 1);
                    // узнаём что за день
                if (dayName.ToLower().Contains("будни"))
                {
                    dic[Day.mon]= shedule;
                    dic[Day.tue]= shedule;
                    dic[Day.wed] = shedule;
                    dic[Day.thr] = shedule;
                    dic[Day.fr] = shedule;
                }

                if (dayName.ToLower().Contains("выходные"))
                {
                    dic[Day.sut]=shedule;
                    dic[Day.sun]= shedule;
                    dic[Day.holyday]= shedule;
                }

                if (dayName.ToLower().Contains("понедельник"))
                {
                    dic[Day.mon] = shedule;
                }

                if (dayName.ToLower().Contains("вторник"))
                {
                    dic[Day.tue] = shedule;
                }

                if (dayName.ToLower().Contains("среда"))
                {
                    dic[Day.wed] = shedule;
                }

                if (dayName.ToLower().Contains("четверг"))
                {
                    dic[Day.thr] = shedule;
                }

                if (dayName.ToLower().Contains("пятница"))
                {
                    dic[Day.fr] = shedule;
                }
            }

            return dic;
        }

        private string ParseBetweenStrings(string inputString, string startStr, string EndString, ref int start, ref int end)
        {
            start = inputString.IndexOf(startStr, end);
            if (start == -1)
                return "";
            end = inputString.IndexOf(EndString, start);
            return inputString.Substring(start + startStr.Length, end - start - startStr.Length);
        }

        private string ParseBetweenStrings(string inputString, string startStr, string EndString)
        {
            int start = inputString.IndexOf(startStr, 0);
            if (start == -1)
                return "";
            int end = inputString.IndexOf(EndString, start+ startStr.Length);
            if (end == -1)
                return "";
            return inputString.Substring(start + startStr.Length, end - start - startStr.Length);
        }

        private List<string> SplitBetweenTags(string inputString, string mainOpenTag, string[] openTegsArr, string closeTag)
        {
            List<string> list = new List<string>();
            int startIndex = 0;
            int endIndex;
            while (true)
            {
                string s = ParseBetweenTags(inputString, mainOpenTag, openTegsArr, closeTag, startIndex, out endIndex);
                if (s != "")
                {
                    list.Add(s);
                    startIndex = endIndex;
                }
                else
                    return list;
            }
        }



        /// <summary>
        /// Вырезает текст между открывающим и закрывающим тегом, учитывая, что внутри могут быть другие теги с таким же закрывающим
        /// </summary>
        /// <param name="inputString">обрабатываемая строка</param>
        /// <param name="mainOpenTag">Главный открывающий тег - тот, который нас интересует</param>
        /// <param name="openTegsArr">массив других открывающих тегов, которые могут быть вложены в главный открывающий тег mainOpenTeg, закрывающим тегом которых служит closeTeg</param>
        /// <param name="closeTag">закрывающий тег</param>
        /// <param name="start">индекс с которого начинаем парсить</param>
        /// <param name="indexEnd">индекс закрывающего тега, out-параметр</param>
        /// <returns></returns>
        private string ParseBetweenTags(string inputString, string mainOpenTag, string[] openTegsArr, string closeTag, int start, out int indexEnd)
        {                // ищем OpenTegs, увеличиваем counter++, далее в зависимости от того нашли OpenTeg или closeTeg делаем ++ или --. Когда counter==0 - заканчиваем поиск
            int counter = 0;    // счётчик уровней вхождения тега  
            indexEnd = 0;       // инициализируем out-параметр
            string[] openTegs = new string[openTegsArr.Length + 1];     // массив со всеми возможными открывающими тегами (главным, который нас интересует (его может быть несколько) и другими)
            openTegsArr.CopyTo(openTegs, 1);
            openTegs[0] = mainOpenTag;
            int startIndex = inputString.IndexOf(mainOpenTag, start);      // начинаем парсинг с первого вхождения главного окрывающего тега

            if (startIndex != -1)
                counter++;
            else
                return "";
            int indexStart = startIndex + mainOpenTag.Length;

            while (counter != 0)
            {
                indexEnd = inputString.IndexOf(closeTag, indexStart);
                indexStart = IndexOfAny(inputString, openTegs, indexStart);
                if (indexStart == -1)
                    indexStart = int.MaxValue;
                if (indexEnd != -1)
                {
                    if (indexEnd < indexStart)
                    {
                        if (--counter == 0)     // нашли закрывающий тег
                            return inputString.Substring(startIndex + mainOpenTag.Length, indexEnd - startIndex - mainOpenTag.Length);
                        else
                            indexStart = indexEnd + closeTag.Length;
                    }
                    else
                    {
                        counter++;
                        indexStart = indexStart + 1;        // добавляем единицу, чтоб не начинать поиск с той же позиции
                    }
                }
                else
                {
                    MessageBox.Show("Не найден закрывающий тег");
                    return "";
                }
            }
            return "";
        }

        /// <summary>
        /// Ищет индекс первого вхождения любой строки из массива arr в строке str
        /// </summary>
        /// <param name="str"></param>
        /// <param name="arr"></param>
        /// <param name="startIndex"></param>
        /// <returns>индекс первого вхождения</returns>
        private int IndexOfAny(string str, string[] arr, int startIndex)
        {
            int start = int.MaxValue;
            foreach (string s in arr)
            {
                int n = str.IndexOf(s, startIndex);
                if (n < start && n != -1)
                    start = n;
            }
            if (start == int.MaxValue)
                start = -1;
            return start;
        }
    }
}
