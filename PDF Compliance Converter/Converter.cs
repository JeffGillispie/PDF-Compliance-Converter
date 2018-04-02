using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using iTextSharp.text.pdf;
using NLog;

namespace PDF_Compliance_Converter
{
    public class Converter
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Rubber stamps an image based PDF as a PDF/A-1B pdf.
        /// </summary>
        /// <param name="oldFile">The original pdf file.</param>
        /// <param name="newFile">The new pdf file.</param>
        public void ConvertPDF(FileInfo oldFile, FileInfo newFile)
        {            
            using (FileStream fs = new FileStream(newFile.FullName, FileMode.Create, FileAccess.Write))
            using (PdfReader pdf = new PdfReader(oldFile.FullName))
            using (iTextSharp.text.Document doc = new iTextSharp.text.Document(pdf.GetPageSizeWithRotation(1)))
            using (PdfAWriter writer = PdfAWriter.GetInstance(doc, fs, PdfAConformanceLevel.PDF_A_1B))
            {
                doc.Open();
                ICC_Profile icc = ICC_Profile.GetInstance(Environment.GetEnvironmentVariable("SystemRoot") + @"\System32\spool\drivers\color\sRGB Color Space Profile.icm");
                writer.SetOutputIntents("Custom", "", "http://www.color.org", "sRGB IEC61966-2.1", icc);
                PdfContentByte contentByte = writer.DirectContent;

                for (int i = 1; i <= pdf.NumberOfPages; i++)
                {
                    doc.SetPageSize(pdf.GetPageSizeWithRotation(i));
                    doc.NewPage();
                    PdfImportedPage page = writer.GetImportedPage(pdf, i);
                    contentByte.AddTemplate(page, 0, 0);
                }
              
                writer.CreateXmpMetadata();                
                doc.Close();                
            }

            OnPdfConverted(oldFile);
        }

        /// <summary>
        /// Rubber stamps all PDFs in a set of folders as a PDF/A-1B PDF.
        /// </summary>
        /// <param name="folders">The target folders.</param>
        /// <param name="outFolder">The output folder</param>
        /// <param name="mirrorSource">Indicates if the folder structure should be mirrored.</param>
        public void ConvertPDFsInFolders(string[] folders, DirectoryInfo outFolder, bool mirrorSource)
        {
            IEnumerable<FileInfo> pdfs = folders
                .Select(folder => new DirectoryInfo(folder))
                .SelectMany(dir => dir.GetFiles("*.pdf", SearchOption.AllDirectories));
            int counter = 0;

            foreach (FileInfo pdf in pdfs)
            {
                try
                {
                    FileInfo oldPdf = pdf;
                    FileInfo newPdf = new FileInfo(Path.Combine(outFolder.FullName, pdf.Name));

                    if (mirrorSource)
                    {
                        newPdf = MirrorSourceFolder(pdf, outFolder, folders);
                    }

                    ConvertPDF(oldPdf, newPdf);
                    logger.Info($"Converted: {pdf.FullName}");
                }
                catch (Exception ex)
                {                    
                    logger.Error(pdf.FullName);
                    logger.Error(ex);
                    OnError(new Tuple<FileInfo, Exception>(pdf, ex));
                }

                int progress = (int)((double)++counter / (double)pdfs.Count() * 100.0f);
                OnReportFolderProgress(progress);
            }
        }

        /// <summary>
        /// Reports the progress of <see cref="ConvertPDFsInFolders(string[], DirectoryInfo, bool)"/>.
        /// </summary>
        public event EventHandler<int> ReportFolderProgress;

        /// <summary>
        /// Occurs on the completion of <see cref="ConvertPDF(FileInfo, FileInfo)"/>.
        /// </summary>
        public event EventHandler<FileInfo> PdfConverted;

        /// <summary>
        /// Occurs when an error happens in <see cref="ConvertPDFsInFolders(string[], DirectoryInfo, bool)"/>.
        /// </summary>
        public event EventHandler<Tuple<FileInfo, Exception>> Error;
                
        protected virtual void OnReportFolderProgress(int progress)
        {
            ReportFolderProgress?.Invoke(this, progress);
        }

        protected virtual void OnPdfConverted(FileInfo file)
        {
            PdfConverted?.Invoke(this, file);
        }

        protected virtual void OnError(Tuple<FileInfo, Exception> args)
        {
            Error?.Invoke(this, args);
        }

        /// <summary>
        /// Creates the internal folder structure for a mirrored folder.
        /// </summary>
        /// <param name="pdf">The original PDF.</param>
        /// <param name="folder">The destination folder.</param>
        /// <param name="sourceFolders">The collection of source folders being stamped.</param>
        /// <returns>Returns the updated destination PDF file.</returns>
        private FileInfo MirrorSourceFolder(FileInfo pdf, DirectoryInfo folder, string[] sourceFolders)
        {
            string sourceFolder = sourceFolders.First(f => pdf.FullName.Contains(f));
            string path = pdf.FullName.Substring(sourceFolder.Length).TrimStart('\\');
            string destination = Path.Combine(folder.FullName, path);
            FileInfo newPdf = new FileInfo(destination);

            if (newPdf.Directory.Exists == false)
                newPdf.Directory.Create();

            return newPdf;
        }
    }
}
