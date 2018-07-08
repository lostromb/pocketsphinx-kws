using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SphinxPortManaged
{
    public static class err
    {
        public static void E_ERROR(string message)
        {
            Console.Write("ERROR: " + message);
        }

        public static void E_WARN(string message)
        {
            Console.Write("WARN: " + message);
        }

        public static void E_INFO(string message)
        {
            Console.Write("INFO: " + message);
        }

        public static void E_INFO_NOFN(string message)
        {
            Console.Write(message);
        }

        public static void E_DEBUG(string message)
        {
            Console.Write("DEBUG: " + message);
        }

        public static void E_INFOCONT(string message)
        {
            Console.Write(message);
        }

        public static void E_FATAL(string message)
        {
            Console.Write("FATAL: " + message);
        }

        public static void E_ERROR_SYSTEM(string message)
        {
            Console.Write("ERROR_SYSTEM: " + message);
        }

        public static void E_FATAL_SYSTEM(string message)
        {
            Console.Write("FATAL_SYSTEM: " + message);
        }
    }
}
