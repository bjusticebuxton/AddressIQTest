
// Copyright (c) 2021 Firstlogic Solutions, LLC
// This software is the confidential and proprietary information
// of Firstlogic Solutions, LLC and its affiliates. You shall not
// disclose such Confidential Information and shall use it only
// in accordance with the terms of the license agreement you
// entered into with Firstlogic Solutions, LLC.
// All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AddressIQNET;


namespace examples
{
    class single_record_json_example
    {
        public static int ProcessTestRecordJSON(String configFilePath)
        {
            int nReturn = AddressIQDefines.ADDRESS_IQ_OK;

            // first we need to read the config data from the file
            String configData = System.IO.File.ReadAllText(configFilePath);

            if (0 == configData.Length)
            {
                return AddressIQDefines.ADDRESS_IQ_ERROR;
            }


            String errorMessage = "";
            AddressIQInstance inst = null;
            AddressIQThread processingThread = null;

            // first step in the process is to create a configured instance of the API
            inst = AddressIQFactory.CreateInstance(configData, ref errorMessage);

            if (null == inst)
            {
                // there is nothing we can do further since we need a configured instance
                DisplayJSONError(errorMessage);
                return AddressIQDefines.ADDRESS_IQ_ERROR;
            }

            // now we need that instance to own a thread that we will use to process records
            processingThread = inst.CreateThread();

            if (null != processingThread)
            {
                String inputData = CreateDiscreteJSONInputData("1234", "Firstlogic Solutions LLC", "3235 Satellite Blvd", "Suite 300", "Duluth", "GA", "30096");
                String outputData = "";

                if (AddressIQDefines.ADDRESS_IQ_OK == processingThread.ProcessRecord(inputData, ref outputData))
                {
                    DisplayJSONOutputData(outputData);
                }
                else
                {
                    errorMessage = processingThread.GetErrorMessage();
                    DisplayJSONError(errorMessage);
                    nReturn = AddressIQDefines.ADDRESS_IQ_ERROR;
                }

                // at times during processing, the instance should be checked for a DPV or LACS false positive lock
                // this can be done prior to terminating the instance, but if you wait until then, you might be unaware
                // that either of these features has become disabled due to the lock
                AddressIQDefines.FalsePositiveLock lockType = inst.GetFalsePositiveLock();
                if (AddressIQDefines.FalsePositiveLock.FalsePositiveLockNone != lockType)
                {
                    if (AddressIQDefines.FalsePositiveLock.FalsePositiveLockDPV == lockType)
                    {
                        Console.WriteLine("A false positive record caused DPV to lock");
                        nReturn = AddressIQDefines.ADDRESS_IQ_ERROR;
                    }
                    else
                    {
                        Console.WriteLine("A false positive record caused LACSLink to lock");
                        nReturn = AddressIQDefines.ADDRESS_IQ_ERROR;
                    }

                }

                // terminate the thread
                // NOTE - we could keep this thread alive and process more records, but this is a sample meant to show
                // how to use the API

                inst.TerminateThread(ref processingThread);

                // when processing is completed, we can retrieve any statistics that may have been captured during processing
                String statsData = "";
                if (AddressIQDefines.ADDRESS_IQ_ERROR == inst.GetStatisticsData(ref statsData))
                {
                    errorMessage = inst.GetErrorMessage();
                    DisplayJSONError(errorMessage);
                    nReturn = AddressIQDefines.ADDRESS_IQ_ERROR;
                }

                //retrieve CASS report data
                String sCASSReportData = "";
                if (AddressIQDefines.ADDRESS_IQ_ERROR == inst.GetCASSReportData(ref sCASSReportData))
                {
                    errorMessage = inst.GetErrorMessage();
                    DisplayJSONError(errorMessage);
                    nReturn = AddressIQDefines.ADDRESS_IQ_ERROR;
                }
            }
            else
            {
                nReturn = AddressIQDefines.ADDRESS_IQ_ERROR;
            }

            if (null != inst)
            {
                if (AddressIQDefines.ADDRESS_IQ_ERROR == AddressIQFactory.TerminateInstance(inst, ref errorMessage))
                {
                    DisplayJSONError(errorMessage);
                    nReturn = AddressIQDefines.ADDRESS_IQ_ERROR;
                }
            }

            return nReturn;
        }

        public static String CreateDiscreteJSONInputData(String recordID, String nameOrFirm, String primaryAddress, String seondaryAddress, String locality, String region, String postcode)
        {
            String inputData = "";

            JObject inpRecRoot = new JObject();
            JObject inpRec = new JObject();
            JArray inpFields = new JArray();

            JObject oneField = new JObject();
            oneField.Add(new JProperty("Input_Field_Name", "Record_ID"));
            oneField.Add(new JProperty("Input_Field_Value", recordID));
            inpFields.Add(oneField);

            oneField = new JObject();
            oneField.Add(new JProperty("Input_Field_Name", "Name_Firm"));
            oneField.Add(new JProperty("Input_Field_Value", nameOrFirm));
            inpFields.Add(oneField);

            oneField = new JObject();
            oneField.Add(new JProperty("Input_Field_Name", "Primary_Address"));
            oneField.Add(new JProperty("Input_Field_Value", primaryAddress));
            inpFields.Add(oneField);

            oneField = new JObject();
            oneField.Add(new JProperty("Input_Field_Name", "Secondary_Address"));
            oneField.Add(new JProperty("Input_Field_Value", seondaryAddress));
            inpFields.Add(oneField);

            oneField = new JObject();
            oneField.Add(new JProperty("Input_Field_Name", "Locality"));
            oneField.Add(new JProperty("Input_Field_Value", locality));
            inpFields.Add(oneField);

            oneField = new JObject();
            oneField.Add(new JProperty("Input_Field_Name", "Region"));
            oneField.Add(new JProperty("Input_Field_Value", region));
            inpFields.Add(oneField);

            oneField = new JObject();
            oneField.Add(new JProperty("Input_Field_Name", "Postcode"));
            oneField.Add(new JProperty("Input_Field_Value", postcode));
            inpFields.Add(oneField);

            inpRec.Add("Input_Fields", inpFields);
            inpRecRoot.Add("Input_Record", inpRec);

            inputData = inpRecRoot.ToString();

            return inputData;
        }
        public static void ParseJSONError(String errorData, ref int errorNumber, ref int errorType, ref String briefMessage, ref String detailMessage)
        {
            try
            {

                JObject rootNode = JObject.Parse(errorData);
                if (null != rootNode)
                {
                    JObject errorObj = (JObject)rootNode["Error_Information"];
                    errorType = (int)errorObj.GetValue("Error_Type");
                    errorNumber = (int)errorObj.GetValue("Error_Number");
                    briefMessage = (String)errorObj.GetValue("Brief_Message");
                    detailMessage = (String)errorObj.GetValue("Detail_Message");
                }
            }
            catch(JsonReaderException /*e*/)
            {
                //if there is something wrong with the config json and the API can't determine whether it is JSON
                //or XML, then it doesn't know what format in which to create the error text.  In this case, it's
                //just a string, not an object.
                errorType = -1;
                errorNumber = -1;
                briefMessage = "Undefined error";
                detailMessage = errorData;
            }
        }



        protected static void DisplayJSONError(String errorData)
        {
	        int errorType = 0;
            int errorNumber = 0;
            String briefMessage = "";
            String detailMessage = "";


            ParseJSONError(errorData, ref errorNumber, ref errorType, ref briefMessage, ref detailMessage);

            Console.WriteLine("Error type: " + errorType);
            Console.WriteLine("Error number: " + errorNumber);
            Console.WriteLine(briefMessage);
            Console.WriteLine(detailMessage);
        }
        protected static void DisplayJSONOutputData(String outputData)
        {
            List<String> fieldNames = new List<String>();
            List<String> fieldValues = new List<String>();

            fieldNames.Add("Primary_Number");
	        fieldNames.Add("Primary_Name");
            fieldNames.Add("Suffix");
            fieldNames.Add("Secondary_Number");
	        fieldNames.Add("Secondary_Description");
	        fieldNames.Add("City");
	        fieldNames.Add("State");
	        fieldNames.Add("Zip5");
	        fieldNames.Add("Zip4");
	        fieldNames.Add("DPV_Status");
	        fieldNames.Add("ErrStat");
            fieldNames.Add("NCOA_Return_Code");
            fieldNames.Add("Move_Found");

            Console.WriteLine("Record ID: " + GetJSONRecordID(outputData));

	        Console.WriteLine("\n\nCASS Output");

            GetJSONCASSOutputFields(outputData, fieldNames, ref fieldValues);

            for (int nFldIdx = 0; nFldIdx < fieldNames.Count; ++nFldIdx)
            {
                Console.WriteLine(fieldNames[nFldIdx] + ": " + fieldValues[nFldIdx]);
	        }

            fieldValues.Clear();
            GetJSONNCOAOutputFields(outputData, fieldNames, ref fieldValues);
            if (fieldValues.Count > 0)
            {
                Console.WriteLine("\n\nNCOA Output");
                for (int nFldIdx = 0; nFldIdx < fieldNames.Count; ++nFldIdx)
                {
                    Console.WriteLine(fieldNames[nFldIdx] + ": " + fieldValues[nFldIdx]);
                }
            }
        }

        protected static String GetJSONRecordID(String data)
        {
            String sRecID = "";
            JObject rootNode = JObject.Parse(data);
            if (null != rootNode)
            {
                JObject outRec = (JObject)rootNode["Output_Record"];
                if (null != outRec)
                {
                    sRecID = (String)outRec.GetValue("Record_ID");
                }
            }

	        return sRecID;
        }

        protected static void GetJSONCASSOutputFields(String data, List<String> fieldNames, ref List<String> fieldValues)
        {
            JObject rootNode = JObject.Parse(data);

            if (null != rootNode)
            {
                JObject outRec = (JObject)rootNode["Output_Record"];
                if (null != outRec)
                {
                    JObject cassOutput = (JObject)outRec["CASS_Output"];
                    if (null != cassOutput)
                    {
                        GetJSONOutputFields(cassOutput, fieldNames, ref fieldValues);
                    }
                }
            }
        }

        protected static void GetJSONNCOAOutputFields(String data, List<String> fieldNames, ref List<String> fieldValues)
        {
            JObject rootNode = JObject.Parse(data);

            if (null != rootNode)
            {
                JObject outRec = (JObject)rootNode["Output_Record"];
                if (null != outRec)
                {
                    JObject ncoaOutput = (JObject)outRec["NCOA_Output"];
                    if (null != ncoaOutput)
                    {
                        GetJSONOutputFields(ncoaOutput, fieldNames, ref fieldValues);
                    }
                }
            }
        }


        protected static void GetJSONOutputFields(JObject outputFields, List<String> fieldNames, ref List<String> fieldValues)
        {
            JArray outFieldArray = (JArray)outputFields["Output_Fields"];
            if (null != outFieldArray)
            {
                int numFields = fieldNames.Count;
                string sVal = "";
                fieldValues.Clear();
                for (int fldIdx = 0; fldIdx < numFields; fldIdx++)
                {
                    sVal = "";
                    GetOneJSONOutputField(fieldNames[fldIdx], outFieldArray, ref sVal);
                    fieldValues.Add(sVal);
                }
            }

        }
        protected static void GetOneJSONOutputField(String fieldName, JArray outputFields, ref String fieldValue)
        {
            fieldValue = "";
            foreach (JObject oneOutput in outputFields)
            {
                JProperty prop = oneOutput.Property("Output_Field_Name");
                String sFieldName = (String)prop.Value;
                if (true == sFieldName.Equals(fieldName, StringComparison.OrdinalIgnoreCase))
                {
                    prop = oneOutput.Property("Output_Field_Value");
                    fieldValue = (String)prop.Value;
                    break;
                }
            }
        }

    }
}
