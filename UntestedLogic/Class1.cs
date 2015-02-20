using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
namespace SandboxTest
{
    public class SandboxTest
    {
        static public int add(int a, int b)
        {
            // Something unintended happens
            File.ReadAllText("C:\\Temp\\file.txt");
            return a + b;
        }

        static public int minus(int a, int b)
        {
            // And exception
            throw new ArgumentException();
            return a - b;
        }
    }
}
