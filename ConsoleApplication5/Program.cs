using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using LiteDB;
using System.IO;
using System.Xml;


namespace ConsoleApplication5
{
    #region жути жуткие
    /// <summary>
    /// Пока не знаю как решить вопрос с более квадриллионом значений
    /// Пока решение кроме как создать многомерные списки не приходит 
    /// при достижении предела значения по длинне списка :)
    /// 
    /// на 3к собраных данных начинаются тормоза
    /// как то нужно группами последними или более вероятными вытягивать
    /// 
    /// можно сделать финт ушами и группировать значения с последующим восстановлением или как то .... но пока не до этого
    /// 
    /// </summary>

    #region статики для передачи параметров
    static class p {
        public static string[] pakage = new string[] { };
        public static Queue<Curr> curr = new Queue<Curr>();
        public static List<Moda> LModa = new List<Moda>();
        public static Average linqAver = new Average();
        public static Calculationmed linqMedian = new Calculationmed();

        public static char[] charDb = new char[]{'a','b', 'c', 'd', 'e', 'f', 'g' };


        //блокировка потока доступа к бд
        public static object locker = new object();

        //блокировка потока доступа к бд
        public static object lockerMod = new object();

        //блокировка вывода на консоль
        public static object lokerConsole = new object();

        public static object lokerEnqueue = new object();

        public static int delay = 1000;

        public static string ip = "239.0.0.222";
        public static int port = 2222;

    }
    #endregion

    #region курсы
    public class Curr {
        public Guid Id { get; set; }
        public double curr { get; set; }
        public uint pak { get; set; }
        public uint razr { get; set; }
        public DateTime dat { get; set; }
    }
    #endregion

    #region мода
    public class Moda : ICloneable {
        public Moda() {
            this.curr = 1;
            this.count = 1;
        }
        public Guid Id { get; set; }
        public double curr { get; set; }
        public int count { get; set; }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
    #endregion

    #region вход
    class Program
    {
        
        static void Main(string[] args)
        {
            xmlRead xmr = new xmlRead();
            xmr.read();


            //Очистка базы
            string sourceDbCur = @"MyData.db";
            if (File.Exists(sourceDbCur)) File.Delete(sourceDbCur);
            string sourceDbMod = @"MyModa.db";
            if (File.Exists(sourceDbMod)) File.Delete(sourceDbMod);

            
            ConsoleKeyInfo cki;
            Fixed fix;

            var recive = new UdpRecive();
            Thread myThread = new Thread(new ThreadStart(recive.UDPThread));
            myThread.Start(); // чтения пакетов UDP

            var mt = new MatchTread();
            Thread mtThread = new Thread(new ThreadStart(mt.matchthread));
            mtThread.Start(); // поток расчетов

            lock (p.lokerConsole)
            {
                Console.SetCursorPosition(0, 4);
                Console.WriteLine("Для фиксации текущих значений ");
                Console.SetCursorPosition(0, 5);
                Console.WriteLine("расчета 'Среднеквадратического отклонения' и 'Моды' нажмите Enter");
            }
            do
            {
                cki = Console.ReadKey();
                if (ConsoleKey.Enter != 0) {
                    fix = new Fixed();
                    fix.FixedThread();
                }
            } while (cki.Key != ConsoleKey.Escape);
        }
    }
    #endregion

    #region расширение для клонирования
    static class Extensions
    {
        public static IList<T> Clone<T>(this IList<T> listToClone) where T : ICloneable
        {
            return listToClone.Select(Moda => (T)Moda.Clone()).ToList();
        }
    }
    #endregion

    #region поток после запроса вычилений, фиксирующий расчитанные значения
    //Расчеты квадратического и моды
    //Надо создавать несколько коллекций для пред обработки
    class Fixed {
        public void FixedThread() {

            
            //так так так максимальная длинна ограничена 2^32
            //!!!НАДО ВЫТАСКИВАТЬ ЧАСТЯМИ со смещением
            //либо сменить место хранения
            //нУЖНО ОБРАБАТЫВАТЬ ЧАСТЯМИ ИНАЧЕ ПАМЯТИ ПРИДЕТ КИРДЫК

            List<Curr> d;
            lock (p.locker)
            {
                using (var db = new LiteDatabase(@"MyData.db"))
                {
                    ILiteCollection<Curr> currDat = db.GetCollection<Curr>("currency");
                    d = currDat.FindAll().ToList();
                    
                    currDat.DeleteAll();

                }
            }
            if (d.Count == 0) {
                
                return;
            }

            //снимаем скрин значений
            //среднее значение схваченное налету
            double av = p.linqAver.aver;

            //количество прилетевших пакетов
            int co = p.linqAver.counter;

            //медиана
            double me = p.linqMedian.median;

            //так копированный или просто ссылка на кучу?
            List<Moda> lm = new List<Moda>(p.LModa);

            //формирование пакета для передачи в поток
            
            object tro = new object[] {av,d};
            d.Sort((x, y) => x.pak.CompareTo(y.pak));
            uint pak = d.Last().pak;
            Int64 pakCount = d.Count();

            lock (p.lokerConsole)
            {
                Console.SetCursorPosition(0, 4);
                Console.WriteLine("                              ");
                Console.SetCursorPosition(0, 4);
                Console.WriteLine("Фиксация ");

                Console.SetCursorPosition(9, 4);
                Console.WriteLine("{0}", DateTime.Now);


                Console.SetCursorPosition(0, 5);
                Console.WriteLine("                                                                 ");
                Console.SetCursorPosition(0, 5);
                Console.WriteLine("Получено пакетов: ");
                Console.SetCursorPosition(20, 5);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("{0}", co);
                Console.ResetColor();

                Console.WriteLine("Потеряно пакетов: ");
                Console.SetCursorPosition(20, 6);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("{0}", pak -co);

                Console.ResetColor();


                Console.SetCursorPosition(0, 7);
                Console.WriteLine("                                                                 ");
                Console.SetCursorPosition(0, 7);
                Console.WriteLine("Среднее значение: ");
                Console.SetCursorPosition(20, 7);
                Console.WriteLine("{0:f5}", av);
            
                Console.SetCursorPosition(0, 8);
                Console.WriteLine("Медиана: ");
                Console.SetCursorPosition(20, 8);
                Console.WriteLine("{0}", me);
            }

            //Среднеквадратическое
            RootMeanSquare rms = new RootMeanSquare();
            Thread RMSThread = new Thread(new ParameterizedThreadStart(rms.RMS));
            RMSThread.Start(tro); // вычисление среднеквадратического отклонения

            
            //Поменяем стратегию все данные для этого есть
            CalculationModa clm = new CalculationModa(d);
            Thread ModThread = new Thread(new ThreadStart(clm.modaOne));
            ModThread.Start(); // вычисление среднеквадратического отклонения
        }

    
    }
    #endregion

    #region Вычиление моды

    class CalculationModa {
        public CalculationModa(List<Curr> curr) {
            this.curr = curr;
            this.Lmoda = new List<Moda>();
        }
        List<Curr> curr { get; set; }
        List<Moda> Lmoda { get; set; }

        List<Moda> OneModa { get; set; }

        public void modaOne() {

            //всеравно много
            //надо как то оптимизировать

            curr.Sort((x, y) => x.curr.CompareTo(y.curr));

            #region загашник
            ////Получаем предрасчитанные значения
            //lock (p.locker)
            //{
            //    using (var db = new LiteDatabase(@"MyModa.db"))
            //    {
            //        ILiteCollection<Moda> ModDat = db.GetCollection<Moda>("ModaOne");
            //        Lmoda = ModDat.FindAll().ToList();
            //        ModDat.DeleteAll();

            //    }
            //}
            #endregion

            
            ///тут надо сделать разбиение по частям имеющие приоритет данные
            ///и не актуальные
            ///за один проход мало что изменится при большом объеме информации
            ///и паралельно вычислять обновляя записи
            ///

            //Получаем предрасчитанные значения
            lock (p.lockerMod)
            {
                using (var db = new LiteDatabase(@"MyModa.db"))
                {
                    ILiteCollection<Moda> ModDat = db.GetCollection<Moda>("ModaOne");
                    ModDat.EnsureIndex(x => x.curr);
                    foreach (Curr c in curr) {
                        
                        var results = ModDat.FindOne(x => x.curr==c.curr);
                        //var results = from n in ModDat select n where 
                        if (results == null)
                        {
                            ModDat.Insert(new Moda() { curr = c.curr, count = 1 });
                        }
                        else {
                            //Moda m = (Moda)results;
                            //m.curr++;
                            results.curr++;
                            ModDat.Update(results);
                        }
                        
                    }
                    Lmoda = ModDat.FindAll().ToList();
                }
            }

            lock (p.lokerConsole)
            {
                Console.SetCursorPosition(0, 10);
                Console.WriteLine("Записей БД:");
                Console.SetCursorPosition(20, 10);
                Console.WriteLine("{0}", Lmoda.Count());
            }


            Lmoda.Sort((x, y) => x.curr.CompareTo(y.curr));


            #region загашник
            //foreach (Curr c in curr) {

            //    //Отсортировали значения
            //    if (!Lmoda.Exists(x => x.curr == c.curr))
            //    {
            //        Lmoda.Add(new Moda() { curr = c.curr,count=1});
            //        Lmoda.Sort((x, y) => x.curr.CompareTo(y.curr));
            //    }
            //    else
            //    {
            //        //поиск элемента и изменение его в списке
            //        Lmoda.FindAll(x => x.curr == c.curr).ForEach(s => s.count++);
            //    }

            //}

            //lock (p.locker)
            //{
            //    using (var db = new LiteDatabase(@"MyModa.db"))
            //    {
            //        ILiteCollection<Moda> ModDat = db.GetCollection<Moda>("ModaOne");

            //        ModDat.Insert(Lmoda);

            //    }
            //}
            #endregion

            Calculationmed calculationmed = new Calculationmed();
            calculationmed.arrayModaList((List<Moda>)Lmoda.Clone());

            //теперь проверка и поиск доступных мод по разрядам, нужно сделать разные обьекты
            for (int i = 0; i < 5; i++) {
                
                ModaRound mr = new ModaRound((List<Moda>)Lmoda.Clone(), i,5);
                //mr.GetModa();
                Thread mrThread = new Thread(new ThreadStart(mr.GetModa));
                mrThread.Start(); // вычисление моды
            }
        }

    }

    class ModaRound {
        public ModaRound(List<Moda> Lmoda,int round, int index)
        {
            this.Lmoda = Lmoda;
            this.round = round;
            this.index = index;
            this.Rmoda = new List<Moda>();

        }
        List<Moda> Lmoda { get; set; }
        List<Moda> Rmoda { get; set; }
        int round { get; set; }
        int index { get; set; }
        public void GetModa() {


            index -= round;

            #region  загашник
            //string dbName = string.Format("MyModa" + p.charDb[round]);

            // lock (p.lockerMod)
            //  {
            //using (var db = new LiteDatabase(@dbName + ".db"))
            //{
            //    ILiteCollection<Moda> ModDat = db.GetCollection<Moda>(dbName);
            //    ModDat.EnsureIndex(x => x.curr);
            //    foreach (Moda c in Lmoda)
            //    {
            //        string str = string.Format("{0:f5}", c.curr);
            //        str = str.Substring(0, str.Length - round);
            //        c.curr = Convert.ToDouble(str);

            //        var results = ModDat.FindOne(x => x.curr == c.curr);
            //        //var results = from n in ModDat select n where 
            //        if (results == null)
            //        {
            //            ModDat.Insert(new Moda() { curr = c.curr, count = 1 });
            //        }
            //        else
            //        {
            //            //Moda m = (Moda)results;
            //            //m.curr++;
            //            results.curr++;
            //            ModDat.Update(results);
            //        }

            //    }
            //    Rmoda = ModDat.FindAll().ToList();
            //    //ModDat.DeleteAll();

            //    lock (p.lokerConsole)
            //    {

            //        Console.SetCursorPosition(0, 25+index);
            //        Console.WriteLine("Стрельбу окончил");
            //    }
            //}
            //   }
            #endregion

            foreach (Moda c in Lmoda)
            {

                string str = string.Format("{0:f5}", c.curr);
                str = str.Substring(0, str.Length - round);
                c.curr = Convert.ToDouble(str);

                //c.Curr = Math.Round(c.Curr, round);

                if (!Rmoda.Exists(x => x.curr == c.curr))
                {
                    Rmoda.Add(new Moda() { curr = c.curr });
                    Rmoda.Sort((x, y) => x.curr.CompareTo(y.curr));
                }
                else
                {
                    //поиск элемента и изменение его в списке
                    Rmoda.FindAll(x => x.curr == c.curr).ForEach(s => s.count++);
                }
            }



            Rmoda.Sort((x, y) => x.count.CompareTo(y.count));

            //index -= round;
            lock (p.lokerConsole)
            {
                Console.SetCursorPosition(0, 10 + index);
                Console.WriteLine("                                                                            ");
                Console.SetCursorPosition(0, 10+index);
                Console.WriteLine("Мода Round({0}): ", round);
            }
            int lastCount = Rmoda.Last().count;
                int ind = Rmoda.Count - 1;
                int sm = 20;
                int cheker = 0;
                bool wOff = true;

            lock (p.lokerConsole)
            {
                Console.SetCursorPosition(sm, 10 + index);
                Console.WriteLine("{0:f5}", Rmoda.Last().curr);
            }
            sm += 10;
            do
                 {
                if (cheker != 3) {
                    if (ind != 0)
                    {


                        ind -= 1;

                        if (Rmoda[ind].count == lastCount)
                        {
                            lock (p.lokerConsole)
                            {
                                Console.SetCursorPosition(sm, 10 + index);
                                Console.WriteLine("{0:f5}", Rmoda[ind].curr);
                            }
                            sm += 10;



                            cheker++;

                        }
                        else { wOff = false; }
                        
                    }
                }

                else {
                    wOff = false;
                }

            }
                while (wOff);

                
            
        }
    }

    #endregion

    #region Среднее квадратическое отклонение
    class RootMeanSquare {
        public void RMS(object trop) {
            object[] tro = (object[])trop;
            double av = (double)tro[0];
            List<Curr> currDat = (List<Curr>)tro[1];

            double s = 0;
            int count = currDat.Count();

            foreach (var d in currDat) {
                s += Math.Pow(av - d.curr,2);            
            }
            
            double rms = Math.Sqrt(s / count);
            lock (p.lokerConsole)
            {
                Console.SetCursorPosition(0, 9);
                Console.WriteLine("Ср. кв. откл: ");
                Console.SetCursorPosition(20, 9);
                Console.WriteLine("{0:f5}", rms);
            }
        }
    }
    #endregion

    #region поток вычислений на лету
    class MatchTread {
        public void matchthread()
        {
            
            Average av = new Average();
            p.linqAver = av;

            //Calculationmed calculationmed = new Calculationmed();
            //p.linqMedian = calculationmed;

            while (true)
            {
                lock (p.lokerEnqueue)
                {
                    if (p.curr.Any())
                    {
                        Curr c = p.curr.Dequeue();

                        lock (p.locker)
                        {
                            using (var db = new LiteDatabase(@"MyData.db"))
                            {
                                var col = db.GetCollection<Curr>("currency");
                                col.Insert(c);
                            }
                        }

                        //Разбить по потокам
                        av.ThreadSumm(c.curr);
                        //calculationmed.arrayModaList(new Moda() { curr = c.curr,count=1 });
                    }
                    
                }
            }
        }
    }
    #region мода и медиана в получающем потоке (старое)
    class Calculationmed
    {
        public double median { get; set; }

        static object locker = new object();

        public void arrayModaList(List<Moda> moda)
        {
            lock (locker)
            {
                //if (!p.LModa.Exists(x => x.curr == moda.curr))
                //{
                //    p.LModa.Add(moda);
                //    p.LModa.Sort((x, y) => x.curr.CompareTo(y.curr));
                //}
                //else
                //{
                //    //поиск элемента и изменение его в списке
                //    p.LModa.FindAll(x => x.curr == moda.curr).ForEach(s => s.count++);
                //}

                //Работа с медианой
                if (moda.Count() > 3)
                {
                    int mediana = moda.Count() / 2;
                    mediana -= 1;

                    if (moda.Count() % 2 != 0 && moda.Count() != 1)
                    {
                        median = moda[mediana + 1].curr;

                        lock (p.lokerConsole)
                        {
                            Console.SetCursorPosition(0, 8);
                            Console.WriteLine("Медиана: ");
                            Console.SetCursorPosition(20, 8);
                            Console.WriteLine("{0}", median);
                        }
                    }
                    else
                    {
                        median = (moda[mediana].curr + moda[mediana + 1].curr) / 2;

                        lock (p.lokerConsole)
                        {
                            Console.SetCursorPosition(0, 8);
                            Console.WriteLine("Медиана: ");
                            Console.SetCursorPosition(20, 8);
                            Console.WriteLine("{0}", median);
                        }
                    }
                }
            }
        }

    }
    #endregion

    #region расчет среднего значения
    //Расчет среднего значения из потока входящих значений + первоначальный вывод 
    class Average
    {
        public Average()
        {
            this.summ = 0;
            this.counter = 0;
            this.aver = 0;
        }
        public double summ { get; set; }
        public int counter { get; set; }
        public double aver { get; set; }

        static object locker = new object();

        public void ThreadSumm(double value)
        {
            lock (locker)
            {
                this.counter++;
                summ += value;
                aver = summ / counter;

                lock (p.lokerConsole)
                {
                    Console.SetCursorPosition(0, 0);
                    Console.WriteLine("Всего значений: ");
                    Console.SetCursorPosition(20, 0);
                    Console.WriteLine("{0}", this.counter);


                    Console.SetCursorPosition(0, 1);
                    Console.WriteLine("Среднее значение: ");
                    Console.SetCursorPosition(20, 1);
                    Console.WriteLine("{0:f5}", aver);
                }
            }

        }
    }
    #endregion
    #endregion

    #region прием и запись UDP
    class UdpRecive {
        public void UDPThread() {
            while (true) {
                UDPRecive();
                Thread.Sleep(p.delay);
            }
        }
        public void UDPRecive() {

            var smsg = new ReadMsg();
            UdpClient client = new UdpClient();

            client.ExclusiveAddressUse = false;
            IPEndPoint localEp = new IPEndPoint(IPAddress.Any, p.port);

            client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            client.ExclusiveAddressUse = false;

            client.Client.Bind(localEp);

            IPAddress multicastaddress = IPAddress.Parse(p.ip);
            client.JoinMulticastGroup(multicastaddress);

            Byte[] data = client.Receive(ref localEp);
            string strData = Encoding.Unicode.GetString(data);

            string[] cc = strData.Split(new char[] { '#' });
            lock (p.lokerEnqueue) {
                p.curr.Enqueue(new Curr() { curr = Convert.ToDouble(cc[0]), pak = Convert.ToUInt32(cc[1]), razr = Convert.ToUInt32(cc[2]), dat = DateTime.UtcNow });
            }
            
            smsg.MsgConsole(cc);

            client.Close();
        }

        class ReadMsg
        {
            public void MsgConsole(string[] msg)
            {
                lock (p.lokerConsole)
                {
                    Console.SetCursorPosition(0, 16);
                    Console.WriteLine(msg[0] + " " + msg[1] + " " + msg[2]);
                }
            }
        }
    }


    #endregion
    #endregion

    #region конфиг
    /// <summary>
    /// Не делал нее какие проверки на соответсвие так как вот так :)
    /// </summary>
    class xmlRead
    {
        public void read()
        {

            string[] ss = Environment.CurrentDirectory.Split(new char[] { '\\' });
            string pach = "";

            for (int i = 0; i < ss.Length - 4; i++)
            {
                pach += ss[i] + "\\";
            }
            pach += "config.xml";


            XmlDocument xDoc = new XmlDocument();
            xDoc.Load(pach);
            // получим корневой элемент
            XmlElement xRoot = xDoc.DocumentElement;
            // обход всех узлов в корневом элементе

            string delay = xRoot.SelectSingleNode("delay").InnerText;

            string ip = xRoot.SelectSingleNode("udp").SelectSingleNode("ip").InnerText;
            string port = xRoot.SelectSingleNode("udp").SelectSingleNode("port").InnerText;


            p.ip = ip;
            p.port = Convert.ToInt32(port);

            p.delay = Convert.ToInt32(delay);
            }
        }
    
    #endregion
}
