/* 
 * You may amend and distribute as you like, but don't remove this header!
 * 
 * EPPlus provides server-side generation of Excel 2007 spreadsheets.
 * See http://www.codeplex.com/EPPlus for details.
 * 
 * All rights reserved.
 * 
 * EPPlus is an Open Source project provided under the 
 * GNU General Public License (GPL) as published by the 
 * Free Software Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
 * 
 * The GNU General Public License can be viewed at http://www.opensource.org/licenses/gpl-license.php
 * If you unfamiliar with this license or have questions about it, here is an http://www.gnu.org/licenses/gpl-faq.html
 * 
 * The code for this project may be used and redistributed by any means PROVIDING it is 
 * not sold for profit without the author's written consent, and providing that this notice 
 * and the author's name and all copyright notices remain intact.
 * 
 * All code and executables are provided "as is" with no warranty either express or implied. 
 * The author accepts no liability for any damage or loss of business that this product may cause.
 *
 * Parts of the interface of this file comes from the Excelpackage project. http://www.codeplex.com/ExcelPackage
 * 
 * Code change notes:
 * 
 * Author							Change						Date
 * ******************************************************************************
 * Jan K�llman		                Initial Release		        2009-10-01
 * Jan K�llman                      Total rewrite               2010-03-01
 * *******************************************************************************/
using System;
using System.Xml;
using System.Text.RegularExpressions;
using System.Drawing;
using System.Collections.Generic;
using OfficeOpenXml.Drawing.Vml;
using System.IO;
using OfficeOpenXml.Drawing;
using System.IO.Packaging;

namespace OfficeOpenXml
{
    public enum PictureAlignment
    {
        /// <summary>
        /// The picture will be added to the left aligned text
        /// </summary>
        Left,
        /// <summary>
        /// The picture will be added to the centered text
        /// </summary>
        Centered,
        /// <summary>
        /// The picture will be added to the right aligned text
        /// </summary>
        Right
    }
    #region class ExcelHeaderFooterText
	/// <summary>
    /// Print header and footer 
    /// </summary>
	public class ExcelHeaderFooterText
	{
        ExcelWorksheet _ws;
        string _hf;
        internal ExcelHeaderFooterText(XmlNode TextNode, ExcelWorksheet ws, string hf)
        {
            _ws = ws;
            _hf = hf;
            if (TextNode == null || string.IsNullOrEmpty(TextNode.InnerText)) return;
            string text = TextNode.InnerText;
            string code = text.Substring(0, 2);  
            int startPos=2;
            for (int pos=startPos;pos<text.Length-2;pos++)
            {
                string newCode = text.Substring(pos, 2);
                if (newCode == "&C" || newCode == "&R")
                {
                    SetText(code, text.Substring(startPos, pos-startPos));
                    startPos = pos+2;
                    pos = startPos;
                    code = newCode;
                }
            }
            SetText(code, text.Substring(startPos, text.Length - startPos));
        }
        private void SetText(string code, string text)
        {
            switch (code)
            {
                case "&L":
                    LeftAlignedText=text;
                    break;
                case "&C":
                    CenteredText=text;
                    break;
                default:
                    RightAlignedText=text;
                    break;
            }
        }
		/// <summary>
		/// Get/set the text to appear on the left hand side of the header (or footer) on the worksheet.
		/// </summary>
		public string LeftAlignedText = null;
		/// <summary>
        /// Get/set the text to appear in the center of the header (or footer) on the worksheet.
		/// </summary>
		public string CenteredText = null;
		/// <summary>
        /// Get/set the text to appear on the right hand side of the header (or footer) on the worksheet.
		/// </summary>
		public string RightAlignedText = null;
        /// <summary>
        /// Inserts a picture at the end of the text in the header or footer
        /// </summary>
        /// <param name="Picture">The image object containing the Picture</param>
        /// <param name="Alignment">Alignment. The image object will be inserted at the end of the Text.</param>
        public ExcelVmlDrawingPicture InsertPicture(Image Picture, PictureAlignment Alignment)
        {
            string id = ValidateImage(Alignment);
            
            //Add the image
            ImageConverter ic = new ImageConverter();
            byte[] img = (byte[])ic.ConvertTo(Picture, typeof(byte[]));
            var ii = _ws.Workbook._package.AddImage(img);

            return AddImage(Picture, id, ii);
        }
        /// <summary>
        /// Inserts a picture at the end of the text in the header or footer
        /// </summary>
        /// <param name="PictureFile">The image object containing the Picture</param>
        /// <param name="Alignment">Alignment. The image object will be inserted at the end of the Text.</param>
        public ExcelVmlDrawingPicture InsertPicture(FileInfo PictureFile, PictureAlignment Alignment)
        {
            string id = ValidateImage(Alignment);

            Image Picture;
            try
            {
                if (!PictureFile.Exists)
                {
                    throw (new FileNotFoundException(string.Format("{0} is missing", PictureFile.FullName)));
                }
                Picture = Image.FromFile(PictureFile.FullName);
            }
            catch (Exception ex)
            {
                throw (new InvalidDataException("File is not a supported image-file or is corrupt", ex));
            }

            ImageConverter ic = new ImageConverter();
            string contentType = ExcelPicture.GetContentType(PictureFile.Extension);
            var uriPic = XmlHelper.GetNewUri(_ws.xlPackage.Package, "/xl/media/"+PictureFile.Name.Substring(0, PictureFile.Name.Length-PictureFile.Extension.Length) + "{0}" + PictureFile.Extension);
            byte[] imgBytes = (byte[])ic.ConvertTo(Picture, typeof(byte[]));
            var ii = _ws.Workbook._package.AddImage(imgBytes, uriPic, contentType);

            return AddImage(Picture, id, ii);
        }

        private ExcelVmlDrawingPicture AddImage(Image Picture, string id, ExcelPackage.ImageInfo ii)
        {
            double width = Picture.Width * 72 / Picture.HorizontalResolution,      //Pixel --> Points
                   height = Picture.Height * 72 / Picture.VerticalResolution;      //Pixel --> Points
            //Add VML-drawing            
            return _ws.HeaderFooter.Pictures.Add(id, ii.Uri, "", width, height);
        }

        private string ValidateImage(PictureAlignment Alignment)
        {
            string id = string.Concat(Alignment.ToString()[0], _hf);
            foreach (ExcelVmlDrawingPicture image in _ws.HeaderFooter.Pictures)
            {
                if (image.Id == id)
                {
                    throw (new InvalidOperationException("A picture already exists in this section"));
                }
            }
            //Add the image placeholder to the end of the text
            switch (Alignment)
            {
                case PictureAlignment.Left:
                    LeftAlignedText += ExcelHeaderFooter.Image;
                    break;
                case PictureAlignment.Centered:
                    CenteredText += ExcelHeaderFooter.Image;
                    break;
                default:
                    RightAlignedText += ExcelHeaderFooter.Image;
                    break;
            }
            return id;
        }
	}
	#endregion

	#region ExcelHeaderFooter
	/// <summary>
	/// Represents the Header and Footer on an Excel Worksheet
	/// </summary>
	public sealed class ExcelHeaderFooter : XmlHelper
	{
		#region Static Properties
		/// <summary>
		/// Use this to insert the page number into the header or footer of the worksheet
		/// </summary>
		public const string PageNumber = @"&P";
		/// <summary>
		/// Use this to insert the number of pages into the header or footer of the worksheet
		/// </summary>
		public const string NumberOfPages = @"&N";
		/// <summary>
		/// Use this to insert the name of the worksheet into the header or footer of the worksheet
		/// </summary>
		public const string SheetName = @"&A";
		/// <summary>
		/// Use this to insert the full path to the folder containing the workbook into the header or footer of the worksheet
		/// </summary>
		public const string FilePath = @"&Z";
		/// <summary>
		/// Use this to insert the name of the workbook file into the header or footer of the worksheet
		/// </summary>
		public const string FileName = @"&F";
		/// <summary>
		/// Use this to insert the current date into the header or footer of the worksheet
		/// </summary>
		public const string CurrentDate = @"&D";
		/// <summary>
		/// Use this to insert the current time into the header or footer of the worksheet
		/// </summary>
		public const string CurrentTime = @"&T";
        /// <summary>
        /// Use this if you have an image in a template and want to rewrite the header containing the image.
        /// </summary>
        public const string Image = @"&G";
		#endregion

		#region ExcelHeaderFooter Private Properties
		internal ExcelHeaderFooterText _oddHeader;
        internal ExcelHeaderFooterText _oddFooter;
		internal ExcelHeaderFooterText _evenHeader;
        internal ExcelHeaderFooterText _evenFooter;
        internal ExcelHeaderFooterText _firstHeader;
        internal ExcelHeaderFooterText _firstFooter;
        private ExcelWorksheet _ws;
        #endregion

		#region ExcelHeaderFooter Constructor
		/// <summary>
		/// ExcelHeaderFooter Constructor
		/// </summary>
		/// <param name="nameSpaceManager"></param>
        /// <param name="topNode"></param>
		internal ExcelHeaderFooter(XmlNamespaceManager nameSpaceManager, XmlNode topNode, ExcelWorksheet ws) :
            base(nameSpaceManager, topNode)
		{
            _ws = ws;
		}
		#endregion

		#region alignWithMargins
        const string alignWithMarginsPath="@alignWithMargins";
        /// <summary>
		/// Gets/sets the alignWithMargins attribute
		/// </summary>
		public bool AlignWithMargins
		{
			get
			{
                return GetXmlNodeBool(alignWithMarginsPath);
			}
			set
			{
                SetXmlNodeString(alignWithMarginsPath, value ? "1" : "0");
			}
		}
		#endregion

        #region differentOddEven
        const string differentOddEvenPath = "@differentOddEven";
        /// <summary>
		/// Gets/sets the flag that tells Excel to display different headers and footers on odd and even pages.
		/// </summary>
		public bool differentOddEven
		{
			get
			{
                return GetXmlNodeBool(differentOddEvenPath);
			}
			set
			{
                SetXmlNodeString(differentOddEvenPath, value ? "1" : "0");
			}
		}
		#endregion

		#region differentFirst
        const string differentFirstPath = "@differentFirst";

		/// <summary>
		/// Gets/sets the flag that tells Excel to display different headers and footers on the first page of the worksheet.
		/// </summary>
		public bool differentFirst
		{
			get
			{
                return GetXmlNodeBool(differentFirstPath);
			}
			set
			{
                SetXmlNodeString(differentFirstPath, value ? "1" : "0");
			}
		}
		#endregion

		#region ExcelHeaderFooter Public Properties
		/// <summary>
		/// Provides access to the header on odd numbered pages of the document.
		/// If you want the same header on both odd and even pages, then only set values in this ExcelHeaderFooterText class.
		/// </summary>
		public ExcelHeaderFooterText OddHeader 
        { 
            get 
            {
                if (_oddHeader == null)
                {
                    _oddHeader = new ExcelHeaderFooterText(TopNode.SelectSingleNode("d:oddHeader", NameSpaceManager), _ws, "H");
                }
                return _oddHeader; } 
        }
		/// <summary>
		/// Provides access to the footer on odd numbered pages of the document.
		/// If you want the same footer on both odd and even pages, then only set values in this ExcelHeaderFooterText class.
		/// </summary>
		public ExcelHeaderFooterText OddFooter 
        { 
            get 
            {
                if (_oddFooter == null)
                {
                    _oddFooter = new ExcelHeaderFooterText(TopNode.SelectSingleNode("d:oddFooter", NameSpaceManager), _ws, "F"); ;
                }
                return _oddFooter; 
            } 
        }
		// evenHeader and evenFooter set differentOddEven = true
		/// <summary>
		/// Provides access to the header on even numbered pages of the document.
		/// </summary>
		public ExcelHeaderFooterText EvenHeader 
        { 
            get 
            {
                if (_evenHeader == null)
                {
                    _evenHeader = new ExcelHeaderFooterText(TopNode.SelectSingleNode("d:evenHeader", NameSpaceManager), _ws, "HEVEN");
                    differentOddEven = true;
                }
                return _evenHeader; 
            } 
        }
		/// <summary>
		/// Provides access to the footer on even numbered pages of the document.
		/// </summary>
		public ExcelHeaderFooterText EvenFooter
        { 
            get 
            {
                if (_evenFooter == null)
                {
                    _evenFooter = new ExcelHeaderFooterText(TopNode.SelectSingleNode("d:evenFooter", NameSpaceManager), _ws, "FEVEN");
                    differentOddEven = true;
                }
                return _evenFooter ; 
            } 
        }
		/// <summary>
		/// Provides access to the header on the first page of the document.
		/// </summary>
		public ExcelHeaderFooterText FirstHeader
        { 
            get 
            {
                if (_firstHeader == null)
                {
                    _firstHeader = new ExcelHeaderFooterText(TopNode.SelectSingleNode("d:firstHeader", NameSpaceManager), _ws, "HFIRST"); 
                     differentFirst = true;
                }
                return _firstHeader; 
            } 
        }
		/// <summary>
		/// Provides access to the footer on the first page of the document.
		/// </summary>
		public ExcelHeaderFooterText FirstFooter
        { 
            get 
            {
                if (_firstFooter == null)
                {
                    _firstFooter = new ExcelHeaderFooterText(TopNode.SelectSingleNode("d:firstFooter", NameSpaceManager), _ws, "FFIRST"); 
                    differentFirst = true;
                }
                return _firstFooter; 
            } 
        }
        private ExcelVmlDrawingPictureCollection _vmlDrawingsHF = null;
        /// <summary>
        /// Vml drawings. Underlaying object for Header footer images
        /// </summary>
        public ExcelVmlDrawingPictureCollection Pictures
        {
            get
            {
                if (_vmlDrawingsHF == null)
                {
                    var vmlNode = _ws.WorksheetXml.SelectSingleNode("d:worksheet/d:legacyDrawingHF/@r:id", NameSpaceManager);
                    if (vmlNode == null)
                    {
                        _vmlDrawingsHF = new ExcelVmlDrawingPictureCollection(_ws.xlPackage, _ws, null);
                    }
                    else
                    {
                        if (_ws.Part.RelationshipExists(vmlNode.Value))
                        {
                            var rel = _ws.Part.GetRelationship(vmlNode.Value);
                            var vmlUri = PackUriHelper.ResolvePartUri(rel.SourceUri, rel.TargetUri);

                            _vmlDrawingsHF = new ExcelVmlDrawingPictureCollection(_ws.xlPackage, _ws, vmlUri);
                            _vmlDrawingsHF.RelId = rel.Id;
                        }
                    }
                }
                return _vmlDrawingsHF;
            }
        }
		#endregion
		#region Save  //  ExcelHeaderFooter
		/// <summary>
		/// Saves the header and footer information to the worksheet XML
		/// </summary>
		internal void Save()
		{
			if (_oddHeader != null)
			{
                SetXmlNodeString("d:oddHeader", GetHeaderFooterText(OddHeader));
			}
			if (_oddFooter != null)
			{
                SetXmlNodeString("d:oddFooter", GetHeaderFooterText(OddFooter));
			}

			// only set evenHeader and evenFooter 
			if (differentOddEven)
			{
				if (_evenHeader != null)
				{
                    SetXmlNodeString("d:evenHeader", GetHeaderFooterText(EvenHeader));
				}
				if (_evenFooter != null)
				{
                    SetXmlNodeString("d:evenFooter", GetHeaderFooterText(EvenFooter));
				}
			}

			// only set firstHeader and firstFooter
			if (differentFirst)
			{
				if (_firstHeader != null)
				{
                    SetXmlNodeString("d:firstHeader", GetHeaderFooterText(FirstHeader));
				}
				if (_firstFooter != null)
				{
                    SetXmlNodeString("d:firstFooter", GetHeaderFooterText(FirstFooter));
				}
			}
		}
        internal void SaveHeaderFooterImages()
        {
            if (_vmlDrawingsHF != null)
            {
                if (_vmlDrawingsHF.Count == 0)
                {
                    if (_vmlDrawingsHF.Uri != null)
                    {
                        _ws.Part.DeleteRelationship(_vmlDrawingsHF.RelId);
                        _ws.xlPackage.Package.DeletePart(_vmlDrawingsHF.Uri);
                    }
                }
                else
                {
                    if (_vmlDrawingsHF.Uri == null)
                    {
                        _vmlDrawingsHF.Uri = XmlHelper.GetNewUri(_ws.xlPackage.Package, @"/xl/drawings/vmlDrawing{0}.vml");
                    }
                    if (_vmlDrawingsHF.Part == null)
                    {
                        _vmlDrawingsHF.Part = _ws.xlPackage.Package.CreatePart(_vmlDrawingsHF.Uri, "application/vnd.openxmlformats-officedocument.vmlDrawing", _ws.xlPackage.Compression);
                        var rel = _ws.Part.CreateRelationship(PackUriHelper.GetRelativeUri(_ws.WorksheetUri, _vmlDrawingsHF.Uri), TargetMode.Internal, ExcelPackage.schemaRelationships + "/vmlDrawing");
                        _ws.SetHFLegacyDrawingRel(rel.Id);
                        _vmlDrawingsHF.RelId = rel.Id;
                        foreach (ExcelVmlDrawingPicture draw in _vmlDrawingsHF)
                        {
                            rel = _vmlDrawingsHF.Part.CreateRelationship(PackUriHelper.GetRelativeUri(_vmlDrawingsHF.Uri, draw.ImageUri), TargetMode.Internal, ExcelPackage.schemaRelationships + "/image");
                            draw.RelId = rel.Id;
                        }
                    }
                    _vmlDrawingsHF.VmlDrawingXml.Save(_vmlDrawingsHF.Part.GetStream());
                }
            }
        }
		/// <summary>
		/// Helper function to Save
		/// </summary>
		/// <param name="headerFooter"></param>
		/// <returns></returns>
		internal string GetHeaderFooterText(ExcelHeaderFooterText headerFooter)
		{
			string ret = "";
			if (headerFooter.LeftAlignedText != null)
				ret += "&L" + headerFooter.LeftAlignedText;
			if (headerFooter.CenteredText != null)
				ret += "&C" + headerFooter.CenteredText;
			if (headerFooter.RightAlignedText != null)
				ret += "&R" + headerFooter.RightAlignedText;
			return ret;
		}
		#endregion
	}
	#endregion

}
