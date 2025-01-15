using AddressIQNET;
using examples;
using System;

namespace AddressIQExample
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var configFilePath = args[0];
            Console.WriteLine("configPath: {0}", configFilePath);

            try
            {
                var result = single_record_json_example.ProcessTestRecordJSON(configFilePath);

                if (result == AddressIQDefines.ADDRESS_IQ_OK)
                {
                    Console.WriteLine("completed.");
                }
                else
                {
                    Console.WriteLine("Error.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: {0}", ex.Message);
            }
        }
    }
}
