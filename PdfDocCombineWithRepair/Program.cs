using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Atalasoft.PdfDoc.Repair;
using Atalasoft.PdfDoc;


namespace PdfDocCombineWithRepair
{
    class Program
    { 
        static void Main(string[] args)
        {
            Console.WriteLine("PdfDocCombineWithRepair Starting...");
            string imgPath = GetWorkingDir();
            
            List<string> inFiles = new List<string>();
            inFiles.Add(imgPath + "file1.pdf");
            inFiles.Add(imgPath + "file2.pdf");


            Console.WriteLine("inFiles: " + inFiles.Count.ToString());
            foreach (string fil in inFiles)
            {
                Console.WriteLine("  file: " + Path.GetFileName(fil));
            }   

            string outFile = imgPath + "combined_out_PdfDocCombineWithRepair.pdf";
            Console.WriteLine("  outFileRepair: " + outFile);

            Console.WriteLine("BEGIN Processing...");

            // in this case, we are using the version that takes a filename for out file and a string[] 
            // of pdfs to combine
            //
            // NOTE that there are overloads that take user and owner passwords 
            //and also that work with streams instead of filenames
            AtalasoftSupportUtils.PdfDocCombineWithRepair.Combine(outFile, inFiles.ToArray(), new RepairOptions());

            Console.WriteLine("END Processing");

            Console.WriteLine("PdfDocCombineWithRepair finished... press RETURN to exit");
            Console.ReadLine();
        }



        /// <summary>
        /// Convenience method to get the root directory of the project - really only useful for debugging
        /// </summary>
        /// <returns></returns>
        private static string GetWorkingDir()
        {
            string cwd = System.IO.Directory.GetCurrentDirectory();
            //Console.WriteLine("cwd is '{0}'", cwd);

            if (cwd.EndsWith("\\bin\\Debug"))
            {
                cwd = cwd.Replace("\\bin\\Debug", "\\..\\..\\");
                //Console.WriteLine("updated cwd is '{0}'", cwd);
            }
            if (cwd.EndsWith("\\bin"))
            {
                cwd = cwd.Replace("\\bin", "\\..\\");
                //Console.WriteLine("updated cwd is '{0}'", cwd);
            }
            return cwd;
        }

    }
}
