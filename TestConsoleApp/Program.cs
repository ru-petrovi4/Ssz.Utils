using Microsoft.Extensions.Configuration;
using Ssz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace TestConsoleApp
{
    class Program
    {
        //static string _s0 = "+WOBA94T4gOaA9zo+Ygda+9GcZecFC1fGy1TdfXCpRw=";
        //static string _s1 = "R7/hRrRroSQ+MnWAhoLl2THHTWfZfmXsq3+PuVTRL6k=";
        //static string _s2 = "m9zyvlcodIYY+B1j2FK21mmvchyFfylNjO/jjtTU9Cg=";
        static void Main(string[] args)
        {
            var q = new Queue<int>();
            q.Enqueue(1);
            q.Enqueue(2);
            q.Enqueue(3);
            Console.WriteLine(q.Count);
            foreach (var i in q)
            {
                Console.WriteLine(i);
            }
            Console.WriteLine(q.Count);
            //Console.WriteLine(q.Dequeue());
            //Console.WriteLine(q.Dequeue());
            //Console.WriteLine(q.Dequeue());
            //Console.WriteLine(q.Dequeue());
            //Console.WriteLine(q.Count);
            //var cr = new ConfigurationBuilder().AddJsonFile(@"appSettings.json", optional: true, reloadOnChange: true).Build();
            //ConfigurationHelper.Initialize(cr);

            //var ar = new byte[2];
            //ar[1] = 1;
            //Memory<byte> memory = ar;
            //var memory2 = memory.Slice(1);
            //Console.WriteLine(memory2.Span[0]);
            //ar[1] = 2;
            //Console.WriteLine(memory2.Span[0]);

            //var r = new TaskCompletionSource<int>();

            //Task.Run(async () =>
            //{
            //    try
            //    {
            //        var result = await r.Task;
            //        Console.WriteLine(result);
            //    }
            //    catch (InvalidOperationException ex)
            //    {
            //        Console.WriteLine("InvalidOperationException " + ex.GetType());
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine("Exception " + ex.GetType());
            //    }
            //}
            //);

            ////r.SetException(new InvalidOperationException());
            //r.TrySetCanceled();
            //r.SetResult(5);

            //Console.ReadKey();


            //
            //var bytes0 = System.Convert.FromBase64String(_s0);
            //var bytes1 = System.Convert.FromBase64String(_s1);
            //var bytes2 = System.Convert.FromBase64String(_s2);

            //Console.WriteLine(String.Join(',', bytes0.Select(b => b.ToString(""))));
            //Console.WriteLine(String.Join(',', bytes1.Select(b => b.ToString(""))));
            //Console.WriteLine(String.Join(',', bytes2.Select(b => b.ToString(""))));

            //Console.WriteLine();
            //Console.WriteLine();
            //Console.WriteLine("sadad".Substring(6, 0) == "");
        }       


   }
}


//var buffer = new ConsoleBuffer(width: 6);
//buffer.DrawHorizontalLine(x: 0, y: 0, width: 6, color: White);
//buffer.DrawHorizontalLine(x: 0, y: 2, width: 6, color: White);
//buffer.DrawVerticalLine(x: 0, y: 0, height: 3, color: White);
//buffer.DrawVerticalLine(x: 5, y: 0, height: 3, color: White);
//buffer.DrawString(x: 1, y: 1, color: White, text: "1337");
//new ConsoleRenderTarget().Render(buffer);

//var sr0 = String.Concat(_s0.Reverse().ToArray()) + "=";
//var bytes0 = System.Convert.FromBase64String(sr0);
//var bytes1 = System.Convert.FromBase64String(String.Concat(_s1.Reverse()));
//var bytes2 = System.Convert.FromBase64String(String.Concat(_s2.Reverse()));

//Console.WriteLine(String.Join(',', bytes0));
//Console.WriteLine(String.Join(',', bytes1));
//Console.WriteLine(String.Join(',', bytes2));

//Console.WriteLine(Encoding.ASCII.GetString(bytes0.Select(b => (byte)(b)).ToArray()));
//Console.WriteLine(Encoding.ASCII.GetString(bytes1.Select(b => (byte)(b)).ToArray()));
//Console.WriteLine(Encoding.ASCII.GetString(bytes2.Select(b => (byte)(b)).ToArray()));

//Console.WriteLine();
//Console.WriteLine();

//var c = Encoding.GetEncoding(437);
//Console.WriteLine(c.GetString(bytes0.Select(b => (byte)(b)).ToArray()));
//Console.WriteLine(Encoding.GetEncoding(437).GetString(bytes1.Select(b => (byte)(b)).ToArray()));
//Console.WriteLine(Encoding.GetEncoding(437).GetString(bytes2.Select(b => (byte)(b)).ToArray()));





//static void Main(string[] args)
//{
//    for (int i0 = 0; i0 < 10; i0++)
//    {
//        for (int i1 = 0; i1 < 10; i1++)
//        {
//            for (int i2 = 0; i2 < 10; i2++)
//            {
//                for (int i3 = 0; i3 < 10; i3++)
//                {
//                    bool succeeded = Check(i0, i1, i2, i3);
//                    if (succeeded)
//                    {
//                        Console.WriteLine("Succeeded: {0}{1}{2}{3}", i0, i1, i2, i3);
//                    }
//                }
//            }
//        }
//    }
//    Console.WriteLine("Completed.");
//    Console.ReadKey();
//}

//private static bool Check(int i0, int i1, int i2, int i3)
//{
//    if (i0 == i1 || i0 == i2 || i0 == i3 ||
//        i1 == i2 || i1 == i3 || i2 == i3) return false;

//    var list0 = new List<int>();
//    var list1 = new List<int>();
//    var list2 = new List<int>();
//    var list3 = new List<int>();
//    for (int i = 0; i < 6; i++)
//    {
//        if (_data[i, 0] == i0)
//        {
//            list0.Add(i);
//        }
//        if (_data[i, 1] == i1)
//        {
//            list1.Add(i);
//        }
//        if (_data[i, 2] == i2)
//        {
//            list2.Add(i);
//        }
//        if (_data[i, 3] == i3)
//        {
//            list3.Add(i);
//        }
//    }
//    if (list0.Intersect(list1).Any() || list0.Intersect(list2).Any() || list0.Intersect(list3).Any() ||
//        list1.Intersect(list2).Any() || list1.Intersect(list3).Any() || list2.Intersect(list3).Any()) return false;

//    if (list0.Count == 0 || list1.Count == 0 || list2.Count == 0 || list3.Count == 0) return false;

//    if ((list0.Count + list1.Count + list2.Count + list3.Count) == 6)
//    {
//        return true;
//    }

//    return false;
//}

//private static char[,] _data = new char[,]
//                {
//                    { 'c', 'b', 'p', ' ', ' ', ' ', 'b', ' ', ' ' },
//                    { ' ', 'b', 't', 'h', ' ', ' ', 'b', 'c', ' ' },
//                    { ' ', 't', 't', 't', ' ', 't', 'c', ' ', 't' },
//                    { ' ', ' ', 'c', ' ', ' ', ' ', 't', ' ', 't' },
//                    { ' ', 't', ' ', ' ', ' ', ' ', 'c', ' ', 't' },
//                    { ' ', 't', ' ', ' ', ' ', 'p', 'c', ' ', ' ' },
//                    { ' ', ' ', ' ', ' ', ' ', ' ', ' ', 'c', ' ' },
//                    { ' ', 'p', 't', ' ', 'h', ' ', ' ', 'p', ' ' },
//                    { ' ', ' ', ' ', ' ', ' ', 'p', 't', 't', 't' }
//                };


//    _arthur.Positions.Add(new Point(2, 1));
//            _arthur.Positions.Add(new Point(4, 1));
//            _arthur.Positions.Add(new Point(2, 2));
//            _arthur.Positions.Add(new Point(1, 3));
//            _arthur.Positions.Add(new Point(5, 3));
//            _arthur.Positions.Add(new Point(8, 3));
//            _arthur.Positions.Add(new Point(1, 4));
//            _arthur.Positions.Add(new Point(2, 4));
//            _arthur.Positions.Add(new Point(5, 4));
//            _arthur.Positions.Add(new Point(6, 4));
//            _arthur.Positions.Add(new Point(8, 4));
//            _arthur.Positions.Add(new Point(1, 5));
//            _arthur.Positions.Add(new Point(8, 5));
//            _arthur.Positions.Add(new Point(1, 6));
//            _arthur.Positions.Add(new Point(3, 6));
//            _arthur.Positions.Add(new Point(8, 6));
//            _arthur.Positions.Add(new Point(9, 6));
//            _arthur.Positions.Add(new Point(2, 7));
//            _arthur.Positions.Add(new Point(3, 7));
//            _arthur.Positions.Add(new Point(4, 7));
//            _arthur.Positions.Add(new Point(4, 8));
//            _arthur.Positions.Add(new Point(7, 8));
//            _arthur.Positions.Add(new Point(9, 8));
//            _arthur.Positions.Add(new Point(2, 9));
//            _arthur.Positions.Add(new Point(3, 9));
//            _arthur.Positions.Add(new Point(4, 9));

//            _bolo.Positions.Add(new Point(1, 1));
//            _bolo.Positions.Add(new Point(8, 2));
//            _bolo.Positions.Add(new Point(7, 3));
//            _bolo.Positions.Add(new Point(3, 4));
//            _bolo.Positions.Add(new Point(7, 5));
//            _bolo.Positions.Add(new Point(7, 6));
//            _bolo.Positions.Add(new Point(8, 7));

//            _corky.Positions.Add(new Point(4, 4));

//            _demzo.Positions.Add(new Point(2, 1));
//            _demzo.Positions.Add(new Point(4, 1));
//            _demzo.Positions.Add(new Point(2, 2));
//            _demzo.Positions.Add(new Point(5, 5));
//            _demzo.Positions.Add(new Point(6, 5));
//            _demzo.Positions.Add(new Point(5, 6));
//            _demzo.Positions.Add(new Point(1, 7));
//            _demzo.Positions.Add(new Point(2, 7));
//            _demzo.Positions.Add(new Point(3, 7));
//            _demzo.Positions.Add(new Point(7, 7));
//            _demzo.Positions.Add(new Point(9, 7));
//            _demzo.Positions.Add(new Point(1, 8));
//            _demzo.Positions.Add(new Point(6, 8));
//            _demzo.Positions.Add(new Point(7, 8));
//            _demzo.Positions.Add(new Point(9, 8));
//            _demzo.Positions.Add(new Point(1, 9));
//            _demzo.Positions.Add(new Point(2, 9));
//            _demzo.Positions.Add(new Point(3, 9));
//            _demzo.Positions.Add(new Point(5, 9));

//            _elko.Positions.Add(new Point(1, 3));
//            _elko.Positions.Add(new Point(1, 6));
//            _elko.Positions.Add(new Point(9, 6));

//            _farukh.Positions.Add(new Point(2, 1));
//            _farukh.Positions.Add(new Point(7, 1));
//            _farukh.Positions.Add(new Point(2, 2));
//            _farukh.Positions.Add(new Point(7, 2));

//            _geva.Positions.Add(new Point(6, 1));
//            _geva.Positions.Add(new Point(8, 1));
//            _geva.Positions.Add(new Point(1, 2));
//            _geva.Positions.Add(new Point(6, 2));

//            _henry.Positions.Add(new Point(1, 2));
//            _henry.Positions.Add(new Point(5, 2));
//            _henry.Positions.Add(new Point(1, 8));
//            _henry.Positions.Add(new Point(4, 8));
//            _henry.Positions.Add(new Point(6, 8));

//            foreach (var a in _arthur.Positions)
//                foreach (var b in _bolo.Positions)
//                    foreach (var c in _corky.Positions)
//                        foreach (var d in _demzo.Positions)
//                            foreach (var e in _elko.Positions)
//                                foreach (var f in _farukh.Positions)
//                                    foreach (var g in _geva.Positions)
//                                        foreach (var h in _henry.Positions)

//                                            for (int gunarX = 1; gunarX< 10; gunarX++)
//                                                for (int gunarY = 1; gunarY< 10; gunarY++)
//                                                {
//                                                    bool succeeded = Test(a, b, c, d, e, f, g, h, gunarX, gunarY);
//                                                    if (succeeded)
//                                                    {
//                                                        Console.WriteLine("a={0} b={1} c={2} d={3} e={4} f={5} g={6} h={7} Gunar=({8}, {9})", a, b, c, d, e, f, g, h, gunarX, gunarY);
//                                                    }
//                                                }

//            //_corky.Positions.Add(new Point(, ));
//            //var any = new Any(5000);
//            //Console.WriteLine(any.ValueAs<byte>(false));            
//        }

//        private static bool Test(Point a, Point b, Point c, Point d, Point e, Point f, Point g, Point h, int gunarX, int gunarY)
//{
//    if (a.Equals(b) || a.Equals(c) || a.Equals(d) || a.Equals(e) || a.Equals(f) || a.Equals(g) || a.Equals(h))
//        }

//static Person _arthur = new Person();
//static Person _bolo = new Person();
//static Person _corky = new Person();
//static Person _demzo = new Person();
//static Person _elko = new Person();
//static Person _farukh = new Person();
//static Person _geva = new Person();
//static Person _henry = new Person();
//static Person _gunar = new Person();

//class Person
//{
//    public List<Point> Positions = new List<Point>();

//    //public int X;

//    //public int Y;
//}

//struct Point
//{
//    public Point(int x, int y)
//    {
//        X = x;
//        Y = y;


//    }
//    public int X;

//    public int Y;

//    public override string ToString()
//    {
//        return "(" + X.ToString() + "," + Y.ToString() + ")";
//    }

//    public override bool Equals(object? obj)
//    {
//        if (obj == null) return false;
//        return X == ((Point)obj).X && Y == ((Point)obj).Y;
//    }

//    public override int GetHashCode()
//    {
//        return 0;
//    }
//}