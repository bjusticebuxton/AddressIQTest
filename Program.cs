using System;
using System.IO;
using AddressIQNET;
using examples; 

namespace AddressIQExample
{
    class Program
    {
        static void Main(string[] args)
        {

            string configFilePath = args[0];

            int result = single_record_json_example.ProcessTestRecordJSON(configFilePath);

            if (result == AddressIQDefines.ADDRESS_IQ_OK)
            {
                Console.WriteLine("completed.");
            }
            else
            {
                Console.WriteLine("Error.");
            }
        }
    }
}
