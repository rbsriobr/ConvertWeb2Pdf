#region Imports (1)

using System.IO;

#endregion Imports (1)

namespace ConvertWeb2Pdf
{
	internal class DocInfo
	{
		#region Properties of DocInfo (6)

		public string DocFileName { get; set; }

		public string DocTitle { get; set; }

		public bool Exists
		{
			get
			{
				return File.Exists (DocFileName);
			}
		}

		public int NPags { get; set; }

		public int PagOffset { get; set; }

		public string Url { get; set; }

		#endregion Properties of DocInfo (6)

		#region Constructors of DocInfo (1)

		public DocInfo (int npags, int pagoffset, string docfilename, string url, string pagtitle)
		{
			DocFileName = docfilename;
			Url = url;
			NPags = npags;
			PagOffset = pagoffset;
			DocTitle = pagtitle;
		}

		#endregion Constructors of DocInfo (1)
	}
}