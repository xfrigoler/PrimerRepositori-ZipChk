using System;
using System.Configuration;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace CLadisPB_XML_Checker
{
    class Program
    {
        //private static string _XMLFolder = @"\\edeubreapp004\Ladis-IExport\XMLS\";
        //private static string _XMLFile = @"PB2XML.TXT";
        //private static string _ZIPFile = @"AnalysisReportsCore_K#####.zip";
        //
        private static string _XMLFolder = @ConfigurationManager.AppSettings["SourceXMLFolder"];
        private static string _XMLFile = @ConfigurationManager.AppSettings["SourceTXTDetailFile"];
        private static string _ZIPFile = @ConfigurationManager.AppSettings["TargetZIPFileName"];
        public static List<string> missingFilesInZip;
        static void Main(string[] args)
        {
            Console.WriteLine("");
            Console.WriteLine("*--------------------------------------------*");
            Console.WriteLine("* ZIP file of ANALYSIS REPORT in XML checker *");
            Console.WriteLine("*--------------------------------------------*");
            // Debugging....
            bool areWeDebugging = false;
            //
            if (args.Length == 0 && !areWeDebugging)
            {
                showValidParams();
                return;
            }
            string currentCustomer = @ConfigurationManager.AppSettings["CurrentCustomer"];
            int validOptions = 0;
            missingFilesInZip = new List<string>();
            List<string> allParams = new List<string>();
            foreach (string s in args)
            {
                allParams.Add(s.ToLower());
            }
            //
            // Debugging....
            if (areWeDebugging) allParams.Add("create");
            //
            //
            //
            if (allParams.Contains("readzip"))
            {
                listZIPContent(_XMLFolder, _ZIPFile, currentCustomer);
                Console.WriteLine(Environment.NewLine);
                validOptions++;
            }
            //
            if (allParams.Contains("readpbs"))
            {
                readPBFile(_XMLFolder, _XMLFile, currentCustomer);
                Console.WriteLine(Environment.NewLine);
                validOptions++;
            }
            //
            int checkRetval = 0;
            if (allParams.Contains("check"))
            {
                checkRetval = checkPBFile(_XMLFolder, _XMLFile, _ZIPFile, currentCustomer);
                Console.WriteLine(Environment.NewLine);
                validOptions++;
            }
            if (allParams.Contains("repair") || allParams.Contains("create"))
            {
                validOptions++;
                if (!allParams.Contains("check") || allParams.Contains("create"))
                {
                    checkRetval = checkPBFile(_XMLFolder, _XMLFile, _ZIPFile, currentCustomer);
                }
                if (checkRetval >= 0)
                {
                    if (missingFilesInZip.Count == 0)
                    {
                        Console.WriteLine("");
                        Console.WriteLine("--- ZIP file is OK");
                        Console.WriteLine("");
                        Console.WriteLine("---------------------------------");
                    }
                    else
                    {
                        if (allParams.Contains("create"))
                        {
                            checkRetval = createZIPFile(_XMLFolder, _XMLFile, _ZIPFile, currentCustomer);
                        }
                        checkRetval = repairZIPFile(_XMLFolder, _ZIPFile, currentCustomer);
                        Console.WriteLine("");
                        Console.WriteLine(string.Format("--- Number of added files: {0}", checkRetval));
                        Console.WriteLine("");
                        Console.WriteLine("---------------------------------");
                    }
                }
                Console.WriteLine(Environment.NewLine);
                //
                // After repair or create, CHECK the ZIP again...
                Console.WriteLine("--- Checking again after create or repair...");
                checkRetval = checkPBFile(_XMLFolder, _XMLFile, _ZIPFile, currentCustomer);
                //
            }
            //
            //
            //
            if (validOptions == 0)
            {
                showValidParams();
                return;
            }
            //
            if (areWeDebugging) Console.ReadLine();
            //
        }
        private static void showValidParams()
        {
            Console.WriteLine("Valid parameters:");
            Console.WriteLine("\treadzip\t-List the content of the ZIP file.");
            Console.WriteLine("\treadpbs\t-List the XML files to be generated.");
            Console.WriteLine("\tcheck\t-List the content of the ZIP file.");
            Console.WriteLine("\trepair\t-Add the missing XML files the ZIP file.");
            Console.WriteLine("\tcreate\t-Create a new ZIP and add the XML files.");
            Console.WriteLine("\t\t-(This will destroy the ZIP file if it exists)");
            Console.WriteLine(Environment.NewLine);
        }
        private static void listZIPContent(string xmlFolder, string zipFile, string clientCode)
        {
            string zipPath = string.Format("{0}{1}", xmlFolder, zipFile);
            zipPath = zipPath.Replace("_K#####", string.Format("_K{0}", clientCode));
            using (FileStream zipToOpen = new FileStream(zipPath, FileMode.Open))
            {
                using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Read))
                {
                    Console.WriteLine(string.Format("- Number of files to be listed: {0}", archive.Entries.Count));
                    int nfiles = 0;
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        Console.WriteLine(string.Format("FILE: {0}", entry.FullName));
                        nfiles++;
                    }
                    Console.WriteLine(string.Format("- Number of files finally listed: {0}", nfiles));
                }
            }
        }
        private static void readPBFile(string xmlFolder, string pbFile, string clientCode)
        {
            string pbPath = string.Format("{0}{1}", xmlFolder, pbFile);
            try
            {   // Open the text file using a stream reader.
                using (StreamReader sr = new StreamReader(pbPath))
                {
                    // Read the stream to a string, and write the string to the console.
                    String line = sr.ReadToEnd();
                    Console.WriteLine(line);
                    Console.WriteLine("------------------------");
                    //
                }
                // Open the text file using a stream reader.
                using (StreamReader sr = new StreamReader(pbPath))
                {
                    while (sr.Peek() >= 0)
                    {
                        string pbDetails = sr.ReadLine();
                        if (!pbDetails.StartsWith("PB") && !pbDetails.Contains(string.Format("|KN{0}", clientCode)))
                        {
                            continue;
                        }
                        pbDetails = string.Format("K{0}_{1}.xml", clientCode, pbDetails.Substring(0, 12));
                        Console.WriteLine(pbDetails);
                    }
                    Console.WriteLine("------------------------");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(Environment.NewLine);
                Console.WriteLine(@"The file " + pbPath + @"could not be read!");
                Console.WriteLine(e.Message);
                Console.WriteLine(Environment.NewLine);
            }
        }
        private static int checkPBFile(string xmlFolder, string pbFile, string zipFile, string clientCode)
        {
            int retval = 0;
            string pbPath = string.Format("{0}{1}", xmlFolder, pbFile);
            string zipPath = string.Format("{0}{1}", xmlFolder, zipFile);
            zipPath = zipPath.Replace("_K#####", string.Format("_K{0}", clientCode));
            //
            List<string> allXMLs = new List<string>();
            try
            {
                using (FileStream zipToOpen = new FileStream(zipPath, FileMode.Open))
                {
                    using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Read))
                    {
                        foreach (ZipArchiveEntry entry in archive.Entries)
                        {
                            allXMLs.Add(entry.FullName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("");
                Console.WriteLine(string.Format("--- ZIP file not found: {0}", zipPath));
                Console.WriteLine(ex.Message);
                Console.WriteLine("");
                Console.WriteLine("---------------------------------");
            }
            //
            try
            {   // Open the text file using a stream reader.
                using (StreamReader sr = new StreamReader(pbPath))
                {
                    int missingFiles = 0;
                    while (sr.Peek() >= 0)
                    {
                        string pbDetails = sr.ReadLine();
                        if (!pbDetails.StartsWith("PB") || !pbDetails.Contains(string.Format("|KN{0}", clientCode)))
                        {
                            continue;
                        }
                        pbDetails = string.Format("K{0}_{1}.xml", clientCode, pbDetails.Substring(0, 12));
                        if (!allXMLs.Contains(pbDetails))
                        {
                            Console.WriteLine(string.Format("--- The file {0} is not in the ZIP", pbDetails));
                            missingFiles++;
                            missingFilesInZip.Add(pbDetails);
                        }
                    }
                    Console.WriteLine("");
                    Console.WriteLine(string.Format("--- Number of missing files: {0}", missingFiles));
                    Console.WriteLine("");
                    Console.WriteLine("---------------------------------");
                    retval = missingFiles;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(Environment.NewLine);
                Console.WriteLine(@"The file " + pbPath + @"could not be read!");
                Console.WriteLine(e.Message);
                Console.WriteLine(Environment.NewLine);
                retval = -1;
            }
            //
            return retval;
        }
        private static int repairZIPFile(string xmlFolder, string zipFile, string clientCode)
        {
            int retval = 0;
            string zipPath = string.Format("{0}{1}", xmlFolder, zipFile);
            zipPath = zipPath.Replace("_K#####", string.Format("_K{0}", clientCode));
            using (FileStream zipToOpen = new FileStream(zipPath, FileMode.OpenOrCreate))
            {
                using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create))
                {
                    // Loop into the missing XML files
                    foreach (string missXML in missingFilesInZip)
                    {
                        // Locate the XML file inside the subfolders...
                        try
                        {
                            int initPos = missXML.IndexOf("_PB");
                            if (initPos < 0)
                            {
                                continue;
                            }
                            string subfolderPath = string.Format(@"{0}\{1}\20{2}\{3}\{4}\{5}"
                                                                , xmlFolder
                                                                , missXML.Substring(initPos + 1, 2)
                                                                , missXML.Substring(initPos + 3, 2)
                                                                , missXML.Substring(initPos + 5, 2)
                                                                , missXML.Substring(initPos + 7, 2)
                                                                , missXML);
                            ZipArchiveEntry readmeEntry = archive.CreateEntryFromFile(subfolderPath, missXML);
                            Console.WriteLine(string.Format("--- Adding {0} to the ZIP", subfolderPath));
                            retval++;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }
                }
            }
            //
            return retval;
        }
        private static int createZIPFile(string xmlFolder, string pbFile, string zipFile, string clientCode)
        {
            int retval = 0;
            string zipPath = string.Format("{0}{1}", xmlFolder, zipFile);
            zipPath = zipPath.Replace("_K#####", string.Format("_K{0}", clientCode));
            if (File.Exists(zipPath))
            {
                try
                {
                    File.Delete(zipPath);
                }
                catch (Exception e)
                {
                    Console.WriteLine(Environment.NewLine);
                    Console.WriteLine(@"The file " + zipPath + @"could not be deleted!");
                    Console.WriteLine(e.Message);
                    Console.WriteLine(Environment.NewLine);
                    return -1;
                }
            }
            //
            using (FileStream zipToOpen = new FileStream(zipPath, FileMode.Create))
            {
                Console.WriteLine(@"The file " + zipPath + @" is created!");
                using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
                {
                    Console.WriteLine(@"The file " + zipPath + @" is ready!");
                }
            }
            //
            return retval;
        }
    }
}

