using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PDF_Compliance_Converter;

namespace PDF_Compliance_Converter_Tester
{
    [TestClass]
    public class UT_Converter
    {
        [TestMethod]
        public void ConvertPDFTest()
        {
            var inputFile = new FileInfo(@"X:\dev\TestData\PdfComplianceConverter\input2.pdf");
            var outputFile = new FileInfo(@"X:\dev\TestData\PdfComplianceConverter\output2.pdf");
            Converter converter = new Converter();
            converter.ConvertPDF(inputFile, outputFile);
        }
    }
}
