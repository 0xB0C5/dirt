using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace dirt
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.Error.WriteLine("No source file specified.");
                return;
            }

            string programPath = args[0];

            bool verbose = false;
            byte[] input = null;

            for (int i = 1; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "-v":
                        verbose = true;
                        break;

                    case "-i":
                        i++;
                        if (i < args.Length)
                        {
                            input = args[i].Select(c => (byte)c).ToArray();
                        }
                        else
                        {
                            input = new byte[0];
                        }
                        break;

                    default:
                        Console.Error.WriteLine("Unrecognized option " + args[i]);
                        return;
                        
                }
            }

            var code = File.ReadAllBytes(programPath);

            var tree = new Parser(code).Parse();
            var transducer = Transducer.FromSyntaxTree(tree);

            if (input == null)
            {
                var inStream = Console.OpenStandardInput();

                var buffer = new byte[1024];

                var inputList = new List<byte>();

                while (true)
                {
                    int readCount = inStream.Read(buffer, 0, buffer.Length);
                    if (readCount == 0) break;

                    inputList.AddRange(buffer.Take(readCount));
                }

                input = inputList.ToArray();
            }

            var outStream = Console.OpenStandardOutput();
            while (true)
            {
                var output = transducer.GetMinimumOutput(input);

                if (output == null || verbose)
                {
                    outStream.Write(input, 0, input.Length);
                    outStream.Write(new byte[] { (byte)'\n', (byte)'\n' });
                }

                if (output == null)
                {
                    break;
                }

                input = output;
            }
        }
    }
}
