using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace gomeltrans_net_parser
{
    public partial class gomeltrans_net_Parser : Form
    {


        public gomeltrans_net_Parser()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            Parser parser = new Parser();
            parser.ostanovkaParsed += Parser_ostanovkaParsed;
            parser.BusParsed += Parser_BusParsed;
            parser.End += Parser_End;
            parser.EndFile += Parser_EndFile;
            Thread thread = new Thread(new ParameterizedThreadStart(parser.Parse));
            thread.Start(new object());


        }

        private void Parser_ostanovkaParsed(Ostanovka ostanovka)
        {
            Invoke(new Action(
                    () =>
                        {
                            textBoxResult.AppendText("   " + ostanovka.num_ost+": "+ostanovka.name + "; ");
                        }
                    )
                 );
        }

        private void Parser_EndFile()
        {
            Invoke(new Action(
                    () =>
                        {
                            textBoxResult.AppendText("Файл сформирован, находится в каталоге с программой" + Environment.NewLine);
                            button1.Enabled = true;
                        }
                    )
                 );
        }

        private void Parser_End()
        {
            Invoke(new Action(
                                () =>
                                    {
                                        textBoxResult.AppendText("Парсинг завершён, формируем файл .CSV"+ Environment.NewLine);
                                    }
                                )
                             );
        }

        private void Parser_BusParsed(Marshrut bus)
        {
            Invoke(new Action(
                                () =>
                                    {
                                        textBoxResult.AppendText(Environment.NewLine  +"Маршрут №" + bus.marsh +": "+ bus.napr+ " " + Environment.NewLine+ Environment.NewLine + Environment.NewLine);
                                    }
                                )
                             );
        }
    }

    class Marshrut
    {
        public Type type;
        public string marsh;       // номер маршрута
        public string napr;        //направлние (от конечной до конечной) 
        public List<Ostanovka> ostanovki = new List<Ostanovka>();     // Список остановки 
    }

    class Ostanovka
    {
        public string name;
        public string num_ost;    //Номер основки по порядку (нумерация индивидуально для каждого направления начиная с 1) 
        public Dictionary<Day, string> time;       // в эти столбики пишем время в формате 06:15, 06:25, ....... – это расписание Начинаем поле с пробела.  
    }

    public enum Type {Bus, trolley}
    public enum Day { mon, tue, wed, thr, fr, sut, sun, holyday}
}
