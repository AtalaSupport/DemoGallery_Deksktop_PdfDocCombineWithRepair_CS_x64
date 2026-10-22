using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Atalasoft.PdfDoc;
using Atalasoft.PdfDoc.Repair;

/******************************************************************************
 * NOTICE- this was updated 2020/10/21 to add Proof of concept copyig boomarks 
 * if you want to enable that, edit the Combine method below to uncomment
 * 
 *  // this will shift the page index links only by the specified offset
 *   ShiftPageNumbers(inDoc.BookmarkTree.Bookmarks, bkmkOffset);
 *   // we then need to add the bookmarks to the output document
 *  foreach(PdfBookmark bm in inDoc.BookmarkTree.Bookmarks)
 *  {
 *      outDoc.BookmarkTree.Bookmarks.Add(bm);
 *  }
 * 
 * *****************************************************************************/

namespace AtalasoftSupportUtils
{
    /// <summary>
    /// PdfDocument.Combine does not have any overloads that take RepairOptions
    /// 
    /// NOTE: this can be used with a DotIamage Document Imaging license
    ///       OR a DotPdf license  
    /// 
    /// This static class can be used to neatly wrap that functionality and also
    /// provides a solid example of how to properly handle streams when dealing 
    /// with multiple PdfDocument instances.
    /// </summary>
    public static class PdfDocCombineWithRepair
    {

        /// <summary>
        /// Convenience wrapper for calling PdfDocumentCombineWithRepair without user/owner paswords
        /// </summary>
        /// <param name="outFile"></param>
        /// <param name="inFiles"></param>
        /// <param name="repairOpts"></param>
        public static void Combine(string outFile, string[] inFiles, RepairOptions repairOpts)
        {
            Combine(null, null, outFile, inFiles, repairOpts);
        }

        /// <summary>
        /// This just wraps the PdfDocumentCombineWithRepair(string, string, stream, stream[], RepairOptions)
        /// 
        /// It properly handles closing/disposing of streams.
        /// 
        /// If you need to work with streams directly, then use the wrapped method instead
        /// </summary>
        /// <param name="userPass"></param>
        /// <param name="ownerPass"></param>
        /// <param name="outFile"></param>
        /// <param name="inFiles"></param>
        /// <param name="repairOpts"></param>
        public static void Combine(string userPass, string ownerPass, string outFile, string[] inFiles, RepairOptions repairOpts)
        {
            if (inFiles != null && inFiles.Length > 0)
            {
                List<Stream> streams = new List<Stream>();
                foreach (string fil in inFiles)
                {
                    streams.Add(new FileStream(fil, FileMode.Open, FileAccess.Read, FileShare.Read));
                }
                FileStream outStream = new FileStream(outFile, FileMode.Create);

                Combine(userPass, ownerPass, outStream, streams.ToArray(), repairOpts);

                // cleaning up
                outStream.Close();
                outStream.Dispose();
                foreach (Stream stm in streams)
                {
                    stm.Close();
                    stm.Dispose();
                }
            }
        }

        /// <summary>
        /// Convenience wrapper for calling PdfDocumentCombineWithRepair using streams and no passwords
        /// </summary>
        /// <param name="outStream"></param>
        /// <param name="streams"></param>
        /// <param name="repairOpts"></param>
        public static void Combine(Stream outStream, Stream[] streams, RepairOptions repairOpts)
        {
            Combine(null, null, outStream, streams, repairOpts);
        }

        /// <summary>
        /// Mirrors the PdfDocument.Combine() method for a combine with RepairOptions
        /// 
        /// NOTE: we do NOT close  / dispose the streams it is the responsibility 
        /// of the calling party to handle them correctly
        /// </summary>
        /// <param name="userPass"></param>
        /// <param name="ownerPass"></param>
        /// <param name="outStream"></param>
        /// <param name="streams"></param>
        /// <param name="repairOpts"></param>
        public static void Combine(string userPass, string ownerPass, Stream outStream, Stream[] streams, RepairOptions repairOpts)
        {
            List<PdfDocument> docs = new List<PdfDocument>();
            foreach (Stream stm in streams)
            {
                stm.Seek(0, SeekOrigin.Begin);
                docs.Add(new PdfDocument(userPass, ownerPass, stm, null, repairOpts));
            }
            PdfDocument outDoc = new PdfDocument();


            //// we must add a bookmark tree to the pdf to serve as a destination for those bookmarks from source docs
            outDoc.BookmarkTree = new PdfBookmarkTree();
			
            foreach (PdfDocument inDoc in docs)
            {
                // update the offset for shifting bookmark pages to the count of the output doc
                // initially it will be 0 so first doc will not have its bookmarks modified 
                int bkmkOffset = outDoc.Pages.Count;
                foreach (PdfPage page in inDoc.Pages)
                {
                    outDoc.Pages.Add(page);
                }

                /** ***********************************************
                 * THE FOLLOWING SECTION IS PROOF OF CONCEPT / EXPERIMENTAL CODE FOR BOOKMARK HANDLING
                 * The original combine and repair code has no handling of bookmarks
                 * 
                 * The base PdfDocument.Combine only passes along the first files bookmark tree and ignores subsequent documents
                 * This POC code is provided as a guide and is not fully tested as production code
                 * It only fixes a specific kind of internal link
                 * ***********************************************
                 */
                //if (inDoc.BookmarkTree != null)
                //{
                //    ShiftPageNumbers(inDoc.BookmarkTree.Bookmarks, bkmkOffset);
                //    // we then need to add the bookmarks to the output document
                //    foreach (PdfBookmark bm in inDoc.BookmarkTree.Bookmarks)
                //    {
                //        outDoc.BookmarkTree.Bookmarks.Add(bm);
                //    }
                //} 
                /** ***********************************************
                 * END OPTIONAL POC CODE FOR HANDLING OF BOOKMARKS
                 */
            }
            outStream.Seek(0, SeekOrigin.Begin);
            outDoc.Save(outStream);
            outDoc.Close();

            foreach (PdfDocument inDoc in docs)
            {
                inDoc.Close();
            }
        }

        /// <summary>
        /// Iterates bookmark list and adjusts any PdfIndexedPageRference by a given offset 
        /// </summary>
        /// <param name="bkMks">The PdfBookmarkList to operate upon</param>
        /// <param name="pageOffset">The number of pages to shift the index by</param>
        private static void ShiftPageNumbers(PdfBookmarkList bkMks, int pageOffset)
        {
            // Skip if the offset is 0 - its basically a no-op
            if (pageOffset != 0 && bkMks != null)
            {
                foreach (PdfBookmark bm in bkMks)
                {
                    // Each boomark can have multiple click actions so we loop
                    foreach (PdfAction rawAction in bm.ClickAction)
                    {
                        // we only want to operate on a very specific subtype of a specific subtype
                        if (rawAction.GetType() == typeof(PdfGoToViewAction))
                        {
                            PdfGoToViewAction action = rawAction as PdfGoToViewAction;
                            PdfPageReference rawPgRef = action.Destination.Page;

                            if (rawPgRef.GetType() == typeof(PdfIndexedPageReference))
                            {
                                PdfIndexedPageReference indexedRef = rawPgRef as PdfIndexedPageReference;
                                indexedRef.PageIndex = indexedRef.PageIndex + pageOffset;
                            }
                        }
                    }
                }
            }
        }
    }
}
    